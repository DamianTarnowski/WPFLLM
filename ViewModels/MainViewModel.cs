using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace WPFLLM.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private int _selectedTabIndex;

    public ChatViewModel ChatViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public RagViewModel RagViewModel { get; }
    public EmbeddingsViewModel EmbeddingsViewModel { get; }

    public MainViewModel()
    {
        ChatViewModel = App.Services.GetRequiredService<ChatViewModel>();
        SettingsViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        RagViewModel = App.Services.GetRequiredService<RagViewModel>();
        EmbeddingsViewModel = App.Services.GetRequiredService<EmbeddingsViewModel>();
        CurrentView = ChatViewModel;
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        CurrentView = value switch
        {
            0 => ChatViewModel,
            1 => RagViewModel,
            2 => EmbeddingsViewModel,
            3 => SettingsViewModel,
            _ => ChatViewModel
        };
    }
}
