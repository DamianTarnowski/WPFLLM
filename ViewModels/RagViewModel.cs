using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WPFLLM.Models;
using WPFLLM.Services;

namespace WPFLLM.ViewModels;

public partial class RagViewModel : ObservableObject
{
    private readonly IRagService _ragService;
    private CancellationTokenSource? _embeddingCts;

    [ObservableProperty]
    private ObservableCollection<RagDocument> _documents = [];

    [ObservableProperty]
    private RagDocument? _selectedDocument;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    public RagViewModel(IRagService ragService)
    {
        _ragService = ragService;
        _ = LoadDocumentsAsync();
    }

    private async Task LoadDocumentsAsync()
    {
        var docs = await _ragService.GetDocumentsAsync();
        Documents = new ObservableCollection<RagDocument>(docs);
    }

    [RelayCommand]
    private async Task AddDocumentAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Text files (*.txt;*.md;*.json;*.csv)|*.txt;*.md;*.json;*.csv|All files (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            IsProcessing = true;
            foreach (var file in dialog.FileNames)
            {
                StatusMessage = $"Adding {System.IO.Path.GetFileName(file)}...";
                var doc = await _ragService.AddDocumentAsync(file);
                Documents.Add(doc);
            }
            StatusMessage = "Documents added. Run 'Generate Embeddings' to enable RAG.";
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync()
    {
        if (SelectedDocument == null) return;

        await _ragService.DeleteDocumentAsync(SelectedDocument.Id);
        Documents.Remove(SelectedDocument);
        SelectedDocument = null;
        StatusMessage = "Document deleted.";
    }

    [RelayCommand]
    private async Task GenerateEmbeddingsAsync()
    {
        IsProcessing = true;
        _embeddingCts = new CancellationTokenSource();

        var progress = new Progress<string>(msg => StatusMessage = msg);

        try
        {
            await _ragService.GenerateEmbeddingsAsync(progress, _embeddingCts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Embedding generation cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            _embeddingCts?.Dispose();
            _embeddingCts = null;
        }
    }

    [RelayCommand]
    private void CancelEmbeddings()
    {
        _embeddingCts?.Cancel();
    }
}
