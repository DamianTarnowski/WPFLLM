using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPFLLM.Models;
using WPFLLM.Services;

namespace WPFLLM.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly IChatService _chatService;
    private CancellationTokenSource? _streamCts;

    [ObservableProperty]
    private ObservableCollection<Conversation> _conversations = [];

    [ObservableProperty]
    private Conversation? _selectedConversation;

    [ObservableProperty]
    private ObservableCollection<ChatMessageViewModel> _messages = [];

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _showSearchResults;

    [ObservableProperty]
    private ObservableCollection<SearchResultViewModel> _searchResults = [];

    [ObservableProperty]
    private int _pendingEmbeddingsCount;

    [ObservableProperty]
    private bool _isGeneratingEmbeddings;

    [ObservableProperty]
    private string _embeddingProgress = string.Empty;

    public ChatViewModel(IChatService chatService)
    {
        _chatService = chatService;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadConversationsAsync();
        await UpdatePendingEmbeddingsCountAsync();
    }

    private async Task UpdatePendingEmbeddingsCountAsync()
    {
        PendingEmbeddingsCount = await _chatService.GetMessagesWithoutEmbeddingsCountAsync();
    }

    private async Task LoadConversationsAsync()
    {
        var conversations = await _chatService.GetConversationsAsync();
        Conversations = new ObservableCollection<Conversation>(conversations);
    }

    async partial void OnSelectedConversationChanged(Conversation? value)
    {
        Messages.Clear();
        if (value == null) return;

        var messages = await _chatService.GetMessagesAsync(value.Id);
        foreach (var msg in messages)
        {
            Messages.Add(new ChatMessageViewModel(msg));
        }
    }

    [RelayCommand]
    private async Task NewConversationAsync()
    {
        var conversation = await _chatService.CreateConversationAsync($"Chat {DateTime.Now:g}");
        Conversations.Insert(0, conversation);
        SelectedConversation = conversation;
    }

    [RelayCommand]
    private async Task DeleteConversationAsync()
    {
        if (SelectedConversation == null) return;
        
        await _chatService.DeleteConversationAsync(SelectedConversation.Id);
        Conversations.Remove(SelectedConversation);
        SelectedConversation = Conversations.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;

        if (SelectedConversation == null)
        {
            await NewConversationAsync();
        }

        var userMessage = InputText.Trim();
        InputText = string.Empty;

        var userMsgVm = new ChatMessageViewModel(new ChatMessage
        {
            Role = "user",
            Content = userMessage,
            ConversationId = SelectedConversation!.Id
        });
        Messages.Add(userMsgVm);

        var assistantMsgVm = new ChatMessageViewModel(new ChatMessage
        {
            Role = "assistant",
            Content = string.Empty,
            ConversationId = SelectedConversation.Id
        });
        Messages.Add(assistantMsgVm);

        IsStreaming = true;
        StatusText = "Streaming...";
        _streamCts = new CancellationTokenSource();

        var responseBuilder = new StringBuilder();

        try
        {
            await foreach (var chunk in _chatService.SendMessageAsync(
                SelectedConversation.Id, 
                userMessage, 
                _streamCts.Token))
            {
                responseBuilder.Append(chunk);
                assistantMsgVm.Content = responseBuilder.ToString();
            }

            var savedMessage = await _chatService.AddMessageAsync(
                SelectedConversation.Id, 
                "assistant", 
                responseBuilder.ToString());
            assistantMsgVm.Message = savedMessage;

            SelectedConversation.Title = userMessage.Length > 30 
                ? userMessage[..30] + "..." 
                : userMessage;
            await _chatService.UpdateConversationAsync(SelectedConversation);
        }
        catch (OperationCanceledException)
        {
            assistantMsgVm.Content += "\n[Cancelled]";
        }
        catch (Exception ex)
        {
            assistantMsgVm.Content += $"\n[Error: {ex.Message}]";
        }
        finally
        {
            IsStreaming = false;
            StatusText = "Ready";
            _streamCts?.Dispose();
            _streamCts = null;
        }
    }

    [RelayCommand]
    private void StopStreaming()
    {
        _streamCts?.Cancel();
    }

    [RelayCommand]
    private async Task DeleteMessageAsync(ChatMessageViewModel? messageVm)
    {
        if (messageVm?.Message.Id == 0) return;
        
        await _chatService.DeleteMessageAsync(messageVm!.Message.Id);
        Messages.Remove(messageVm);
    }

    [RelayCommand]
    private async Task SemanticSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsSearching = true;
        ShowSearchResults = true;
        SearchResults.Clear();
        StatusText = "Searching...";

        try
        {
            var results = await _chatService.SemanticSearchAsync(SearchQuery);
            foreach (var result in results)
            {
                SearchResults.Add(new SearchResultViewModel(result));
            }
            StatusText = $"Found {results.Count} results";
        }
        catch (Exception ex)
        {
            StatusText = $"Search error: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void GoToSearchResult(SearchResultViewModel? result)
    {
        if (result == null) return;

        ShowSearchResults = false;
        SearchQuery = string.Empty;

        var conv = Conversations.FirstOrDefault(c => c.Id == result.Result.Conversation.Id);
        if (conv != null)
        {
            SelectedConversation = conv;
        }
    }

    [RelayCommand]
    private void CloseSearch()
    {
        ShowSearchResults = false;
        SearchQuery = string.Empty;
        SearchResults.Clear();
    }

    [RelayCommand]
    private async Task GenerateEmbeddingsAsync()
    {
        if (IsGeneratingEmbeddings) return;

        IsGeneratingEmbeddings = true;
        var progress = new Progress<string>(p => EmbeddingProgress = p);

        try
        {
            await _chatService.GenerateMessageEmbeddingsAsync(progress);
            await UpdatePendingEmbeddingsCountAsync();
        }
        catch (Exception ex)
        {
            EmbeddingProgress = $"Error: {ex.Message}";
        }
        finally
        {
            IsGeneratingEmbeddings = false;
        }
    }
}

public partial class ChatMessageViewModel : ObservableObject
{
    [ObservableProperty]
    private ChatMessage _message;

    [ObservableProperty]
    private string _content;

    public string Role => Message.Role;
    public bool IsUser => Role == "user";
    public bool IsAssistant => Role == "assistant";

    public ChatMessageViewModel(ChatMessage message)
    {
        _message = message;
        _content = message.Content;
    }
}

public partial class SearchResultViewModel : ObservableObject
{
    public MessageSearchResult Result { get; }
    
    public string ConversationTitle => Result.Conversation.Title;
    public string MessageContent => Result.Message.Content.Length > 200 
        ? Result.Message.Content[..200] + "..." 
        : Result.Message.Content;
    public string Role => Result.Message.Role;
    public bool IsUser => Role == "user";
    public double Score => Result.Score;
    public string ScoreText => $"{Score:P0}";
    public DateTime CreatedAt => Result.Message.CreatedAt;

    public SearchResultViewModel(MessageSearchResult result)
    {
        Result = result;
    }
}
