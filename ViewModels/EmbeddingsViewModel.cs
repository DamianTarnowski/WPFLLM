using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPFLLM.Models;
using WPFLLM.Services;

namespace WPFLLM.ViewModels;

public partial class EmbeddingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IModelDownloadService _downloadService;
    private readonly ILocalEmbeddingService _localEmbeddingService;
    private CancellationTokenSource? _downloadCts;

    [ObservableProperty]
    private ObservableCollection<EmbeddingModelViewModel> _availableModels = [];

    [ObservableProperty]
    private EmbeddingModelViewModel? _selectedModel;

    [ObservableProperty]
    private bool _useLocalEmbeddings;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string _downloadStatus = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private long _downloadedBytes;

    [ObservableProperty]
    private long _totalBytes;

    [ObservableProperty]
    private string _currentModelStatus = "Nie załadowano";

    public EmbeddingsViewModel(
        ISettingsService settingsService,
        IModelDownloadService downloadService,
        ILocalEmbeddingService localEmbeddingService)
    {
        _settingsService = settingsService;
        _downloadService = downloadService;
        _localEmbeddingService = localEmbeddingService;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var settings = await _settingsService.GetSettingsAsync();
        UseLocalEmbeddings = settings.UseLocalEmbeddings;

        foreach (var (id, info) in EmbeddingModels.Available)
        {
            var status = await _downloadService.GetDownloadStatusAsync(id);
            var downloadedSize = await _downloadService.GetDownloadedSizeAsync(id);
            
            var vm = new EmbeddingModelViewModel
            {
                Id = info.Id,
                DisplayName = info.DisplayName,
                Description = info.Description,
                Dimensions = info.Dimensions,
                SizeBytes = info.SizeBytes,
                Languages = string.Join(", ", info.Languages),
                Status = status,
                DownloadedBytes = downloadedSize,
                IsSelected = info.Id == settings.LocalEmbeddingModel,
                QualityRating = info.QualityRating,
                RamRequired = info.RamRequired,
                InferenceSpeed = info.InferenceSpeed,
                RecommendedFor = info.RecommendedFor
            };
            
            AvailableModels.Add(vm);
            
            if (vm.IsSelected)
                SelectedModel = vm;
        }

        await UpdateCurrentModelStatusAsync();
    }

    private async Task UpdateCurrentModelStatusAsync()
    {
        if (await _localEmbeddingService.IsAvailableAsync())
        {
            var dims = _localEmbeddingService.GetDimensions();
            CurrentModelStatus = $"Załadowany ({dims} wymiarów)";
        }
        else if (UseLocalEmbeddings && SelectedModel != null)
        {
            if (SelectedModel.Status == ModelDownloadStatus.Downloaded)
                CurrentModelStatus = "Gotowy do załadowania";
            else
                CurrentModelStatus = "Model nie pobrany";
        }
        else
        {
            CurrentModelStatus = "Używam API";
        }
    }

    partial void OnUseLocalEmbeddingsChanged(bool value)
    {
        _ = SaveSettingsAsync();
        _ = UpdateCurrentModelStatusAsync();
    }

    partial void OnSelectedModelChanged(EmbeddingModelViewModel? value)
    {
        if (value != null)
        {
            foreach (var m in AvailableModels)
                m.IsSelected = m.Id == value.Id;
            
            _ = SaveSettingsAsync();
            _ = UpdateCurrentModelStatusAsync();
        }
    }

    private async Task SaveSettingsAsync()
    {
        var settings = await _settingsService.GetSettingsAsync();
        settings.UseLocalEmbeddings = UseLocalEmbeddings;
        if (SelectedModel != null)
            settings.LocalEmbeddingModel = SelectedModel.Id;
        await _settingsService.SaveSettingsAsync(settings);
    }

    [RelayCommand]
    private async Task DownloadModelAsync(EmbeddingModelViewModel? model)
    {
        if (model == null || IsDownloading) return;

        IsDownloading = true;
        DownloadProgress = 0;
        DownloadedBytes = 0;
        TotalBytes = model.SizeBytes;
        DownloadStatus = "Rozpoczynanie pobierania...";
        model.Status = ModelDownloadStatus.Downloading;

        _downloadCts = new CancellationTokenSource();
        var progress = new Progress<ModelDownloadProgress>(p =>
        {
            DownloadProgress = p.ProgressPercent;
            DownloadedBytes = p.BytesDownloaded;
            TotalBytes = p.TotalBytes > 0 ? p.TotalBytes : model.SizeBytes;
            DownloadStatus = p.Status;
            model.DownloadedBytes = p.BytesDownloaded;

            if (p.IsComplete)
            {
                model.Status = ModelDownloadStatus.Downloaded;
                StatusMessage = "Model pobrany pomyślnie!";
            }
            else if (p.Error != null)
            {
                model.Status = ModelDownloadStatus.Error;
                StatusMessage = $"Błąd: {p.Error}";
            }
        });

        try
        {
            await _downloadService.DownloadModelAsync(model.Id, progress, _downloadCts.Token);
            await RefreshModelStatusAsync(model);
        }
        catch (OperationCanceledException)
        {
            model.Status = ModelDownloadStatus.PartiallyDownloaded;
            StatusMessage = "Pobieranie anulowane - można wznowić";
        }
        catch (Exception ex)
        {
            model.Status = ModelDownloadStatus.Error;
            StatusMessage = $"Błąd: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
            await UpdateCurrentModelStatusAsync();
        }
    }

    [RelayCommand]
    private Task CancelDownloadAsync()
    {
        _downloadCts?.Cancel();
        DownloadStatus = "Anulowanie...";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task DeleteModelAsync(EmbeddingModelViewModel? model)
    {
        if (model == null) return;

        try
        {
            await _downloadService.DeleteModelAsync(model.Id);
            model.Status = ModelDownloadStatus.NotDownloaded;
            model.DownloadedBytes = 0;
            StatusMessage = "Model usunięty";
            await UpdateCurrentModelStatusAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd usuwania: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadModelAsync()
    {
        if (SelectedModel == null || SelectedModel.Status != ModelDownloadStatus.Downloaded)
        {
            StatusMessage = "Najpierw pobierz wybrany model";
            return;
        }

        StatusMessage = "Ładowanie modelu...";
        try
        {
            await _localEmbeddingService.InitializeAsync(SelectedModel.Id);
            StatusMessage = "Model załadowany!";
            await UpdateCurrentModelStatusAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd ładowania: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshStatusAsync()
    {
        foreach (var model in AvailableModels)
        {
            await RefreshModelStatusAsync(model);
        }
        await UpdateCurrentModelStatusAsync();
    }

    private async Task RefreshModelStatusAsync(EmbeddingModelViewModel model)
    {
        model.Status = await _downloadService.GetDownloadStatusAsync(model.Id);
        model.DownloadedBytes = await _downloadService.GetDownloadedSizeAsync(model.Id);
    }
}

public partial class EmbeddingModelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int _dimensions;

    [ObservableProperty]
    private long _sizeBytes;

    [ObservableProperty]
    private string _languages = string.Empty;

    [ObservableProperty]
    private ModelDownloadStatus _status;

    [ObservableProperty]
    private long _downloadedBytes;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int _qualityRating;

    [ObservableProperty]
    private string _ramRequired = string.Empty;

    [ObservableProperty]
    private string _inferenceSpeed = string.Empty;

    [ObservableProperty]
    private string _recommendedFor = string.Empty;

    public string SizeText => FormatSize(SizeBytes);
    public string DownloadedText => FormatSize(DownloadedBytes);
    public double DownloadPercent => SizeBytes > 0 ? (double)DownloadedBytes / SizeBytes * 100 : 0;

    public string QualityStars => new string('★', QualityRating) + new string('☆', 5 - QualityRating);

    public string StatusText => Status switch
    {
        ModelDownloadStatus.NotDownloaded => "Nie pobrany",
        ModelDownloadStatus.Downloading => "Pobieranie...",
        ModelDownloadStatus.PartiallyDownloaded => $"Częściowo ({DownloadPercent:F0}%)",
        ModelDownloadStatus.Downloaded => "✓ Pobrany",
        ModelDownloadStatus.Error => "Błąd",
        _ => "Nieznany"
    };

    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:F1} {sizes[order]}";
    }
}
