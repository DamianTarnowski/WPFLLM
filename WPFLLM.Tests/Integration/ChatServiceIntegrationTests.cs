using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using NSubstitute;
using System.Text.Json;
using WPFLLM.Models;
using WPFLLM.Services;

namespace WPFLLM.Tests.Integration;

/// <summary>
/// Integration tests for ChatService testing conversation management,
/// message handling, and semantic search functionality.
/// </summary>
[TestClass]
public class ChatServiceIntegrationTests
{
    private TestDatabaseService _database = null!;
    private ILlmService _llmService = null!;
    private IRagService _ragService = null!;
    private ISettingsService _settingsService = null!;
    private ChatService _chatService = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _database = new TestDatabaseService();
        await _database.InitializeAsync();

        _llmService = Substitute.For<ILlmService>();
        _ragService = Substitute.For<IRagService>();
        _settingsService = Substitute.For<ISettingsService>();

        _settingsService.GetSettingsAsync().Returns(new AppSettings
        {
            UseRag = false,
            RagTopK = 3,
            RagMinSimilarity = 0.75
        });

        _chatService = new ChatService(_database, _llmService, _ragService, _settingsService);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _database.Dispose();
    }

    #region Conversation Management Tests

    [TestMethod]
    public async Task GetConversations_ShouldDelegateToDatabase()
    {
        await _database.CreateConversationAsync("Test 1");
        await _database.CreateConversationAsync("Test 2");

        var conversations = await _chatService.GetConversationsAsync();

        conversations.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task CreateConversation_ShouldCreateNewConversation()
    {
        var conversation = await _chatService.CreateConversationAsync("New Chat");

        conversation.Should().NotBeNull();
        conversation.Title.Should().Be("New Chat");
        conversation.Id.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task UpdateConversation_ShouldUpdateTitle()
    {
        var conversation = await _chatService.CreateConversationAsync("Original");
        conversation.Title = "Updated";

        await _chatService.UpdateConversationAsync(conversation);
        var conversations = await _chatService.GetConversationsAsync();

        conversations.Should().Contain(c => c.Title == "Updated");
    }

    [TestMethod]
    public async Task DeleteConversation_ShouldRemoveConversation()
    {
        var conversation = await _chatService.CreateConversationAsync("To Delete");

        await _chatService.DeleteConversationAsync(conversation.Id);
        var conversations = await _chatService.GetConversationsAsync();

        conversations.Should().NotContain(c => c.Id == conversation.Id);
    }

    #endregion

    #region Message Management Tests

    [TestMethod]
    public async Task GetMessages_ShouldReturnMessagesForConversation()
    {
        var conversation = await _chatService.CreateConversationAsync("Test");
        await _chatService.AddMessageAsync(conversation.Id, "user", "Hello");
        await _chatService.AddMessageAsync(conversation.Id, "assistant", "Hi there!");

        var messages = await _chatService.GetMessagesAsync(conversation.Id);

        messages.Should().HaveCount(2);
        messages[0].Role.Should().Be("user");
        messages[1].Role.Should().Be("assistant");
    }

    [TestMethod]
    public async Task AddMessage_ShouldAddMessageToConversation()
    {
        var conversation = await _chatService.CreateConversationAsync("Test");

        var message = await _chatService.AddMessageAsync(conversation.Id, "user", "Test message");

        message.Should().NotBeNull();
        message.Content.Should().Be("Test message");
        message.Role.Should().Be("user");
    }

    [TestMethod]
    public async Task UpdateMessage_ShouldUpdateContent()
    {
        var conversation = await _chatService.CreateConversationAsync("Test");
        var message = await _chatService.AddMessageAsync(conversation.Id, "user", "Original");
        message.Content = "Edited content";

        await _chatService.UpdateMessageAsync(message);
        var messages = await _chatService.GetMessagesAsync(conversation.Id);

        messages.First().Content.Should().Be("Edited content");
    }

    [TestMethod]
    public async Task DeleteMessage_ShouldRemoveMessage()
    {
        var conversation = await _chatService.CreateConversationAsync("Test");
        var message = await _chatService.AddMessageAsync(conversation.Id, "user", "To delete");

        await _chatService.DeleteMessageAsync(message.Id);
        var messages = await _chatService.GetMessagesAsync(conversation.Id);

        messages.Should().BeEmpty();
    }

    #endregion

    #region SendMessage Integration Tests

    [TestMethod]
    public async Task SendMessage_ShouldAddUserMessage()
    {
        var conversation = await _chatService.CreateConversationAsync("Test");
        
        _llmService.StreamChatAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable(new[] { "Hello", " World" }));

        await foreach (var _ in _chatService.SendMessageAsync(conversation.Id, "Hi"))
        {
            // Consume the stream
        }

        var messages = await _chatService.GetMessagesAsync(conversation.Id);
        messages.Should().ContainSingle(m => m.Role == "user" && m.Content == "Hi");
    }

    // Test removed - uses deprecated GetRelevantContextAsync API
    // RAG now uses RetrieveWithTraceAsync which is tested via RagServiceTests

    [TestMethod]
    public async Task SendMessage_WithRagDisabled_ShouldNotRetrieveContext()
    {
        _settingsService.GetSettingsAsync().Returns(new AppSettings { UseRag = false });

        var conversation = await _chatService.CreateConversationAsync("No RAG");

        _llmService.StreamChatAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable(new[] { "Response" }));

        await foreach (var _ in _chatService.SendMessageAsync(conversation.Id, "query"))
        {
        }

        await _ragService.DidNotReceive().GetRelevantContextAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public async Task SendMessage_ShouldStreamResponse()
    {
        var conversation = await _chatService.CreateConversationAsync("Stream Test");
        var chunks = new[] { "Hello", " ", "World", "!" };

        _llmService.StreamChatAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable(chunks));

        var receivedChunks = new List<string>();
        await foreach (var chunk in _chatService.SendMessageAsync(conversation.Id, "Test"))
        {
            receivedChunks.Add(chunk);
        }

        receivedChunks.Should().BeEquivalentTo(chunks);
    }

    [TestMethod]
    public async Task SendMessage_ShouldPassPreviousMessages()
    {
        var conversation = await _chatService.CreateConversationAsync("History Test");
        await _chatService.AddMessageAsync(conversation.Id, "user", "First message");
        await _chatService.AddMessageAsync(conversation.Id, "assistant", "First response");

        List<ChatMessage>? capturedMessages = null;
        _llmService.StreamChatAsync(Arg.Do<List<ChatMessage>>(m => capturedMessages = m), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable(new[] { "Response" }));

        await foreach (var _ in _chatService.SendMessageAsync(conversation.Id, "Second message"))
        {
        }

        capturedMessages.Should().NotBeNull();
        capturedMessages.Should().HaveCount(3); // 2 previous + 1 new
        capturedMessages![2].Content.Should().Be("Second message");
    }

    #endregion

    #region Semantic Search Tests

    [TestMethod]
    public async Task SemanticSearch_WithEmbeddings_ShouldReturnResults()
    {
        var conversation = await _chatService.CreateConversationAsync("Search Test");
        var msg1 = await _chatService.AddMessageAsync(conversation.Id, "user", "Machine learning basics");
        var msg2 = await _chatService.AddMessageAsync(conversation.Id, "assistant", "ML is a subset of AI");

        await _database.UpdateMessageEmbeddingAsync(msg1.Id, JsonSerializer.Serialize(new float[] { 0.8f, 0.2f }));
        await _database.UpdateMessageEmbeddingAsync(msg2.Id, JsonSerializer.Serialize(new float[] { 0.9f, 0.1f }));

        _llmService.GetEmbeddingAsync("machine learning", Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.85f, 0.15f });

        var results = await _chatService.SemanticSearchAsync("machine learning", topK: 10);

        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Score.Should().BeGreaterThan(0));
    }

    [TestMethod]
    public async Task SemanticSearch_NoEmbeddings_ShouldReturnEmpty()
    {
        var conversation = await _chatService.CreateConversationAsync("No Embed");
        await _chatService.AddMessageAsync(conversation.Id, "user", "Test message");

        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.5f, 0.5f });

        var results = await _chatService.SemanticSearchAsync("test");

        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SemanticSearch_EmptyQueryEmbedding_ShouldReturnEmpty()
    {
        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<float>());

        var results = await _chatService.SemanticSearchAsync("query");

        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SemanticSearch_ShouldIncludeConversationInfo()
    {
        var conversation = await _chatService.CreateConversationAsync("Named Conversation");
        var msg = await _chatService.AddMessageAsync(conversation.Id, "user", "Test");
        await _database.UpdateMessageEmbeddingAsync(msg.Id, JsonSerializer.Serialize(new float[] { 1.0f }));

        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 1.0f });

        var results = await _chatService.SemanticSearchAsync("test");

        results.Should().NotBeEmpty();
        results.First().Conversation.Title.Should().Be("Named Conversation");
    }

    [TestMethod]
    public async Task SemanticSearch_ShouldRespectTopK()
    {
        var conversation = await _chatService.CreateConversationAsync("TopK Test");
        
        for (int i = 0; i < 10; i++)
        {
            var msg = await _chatService.AddMessageAsync(conversation.Id, "user", $"Message {i}");
            await _database.UpdateMessageEmbeddingAsync(msg.Id, JsonSerializer.Serialize(new float[] { i / 10.0f }));
        }

        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.5f });

        var results = await _chatService.SemanticSearchAsync("query", topK: 3);

        results.Should().HaveCount(3);
    }

    #endregion

    #region Message Embedding Generation Tests

    [TestMethod]
    public async Task GenerateMessageEmbeddings_ShouldEmbedUnembeddedMessages()
    {
        var conversation = await _chatService.CreateConversationAsync("Embed Gen");
        await _chatService.AddMessageAsync(conversation.Id, "user", "First");
        await _chatService.AddMessageAsync(conversation.Id, "user", "Second");

        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.1f, 0.2f });

        await _chatService.GenerateMessageEmbeddingsAsync();

        var withEmbeddings = await _database.GetAllMessagesWithEmbeddingsAsync();
        withEmbeddings.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GenerateMessageEmbeddings_ShouldSkipAlreadyEmbedded()
    {
        var conversation = await _chatService.CreateConversationAsync("Skip Test");
        var msg1 = await _chatService.AddMessageAsync(conversation.Id, "user", "Already embedded");
        var msg2 = await _chatService.AddMessageAsync(conversation.Id, "user", "Not embedded");

        await _database.UpdateMessageEmbeddingAsync(msg1.Id, "[0.5]");

        var callCount = 0;
        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                callCount++;
                return new float[] { 0.1f };
            });

        await _chatService.GenerateMessageEmbeddingsAsync();

        callCount.Should().Be(1); // Only called for the non-embedded message
    }

    [TestMethod]
    public async Task GenerateMessageEmbeddings_ShouldReportProgress()
    {
        var conversation = await _chatService.CreateConversationAsync("Progress Test");
        await _chatService.AddMessageAsync(conversation.Id, "user", "Msg 1");
        await _chatService.AddMessageAsync(conversation.Id, "user", "Msg 2");

        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.1f });

        var progressMessages = new List<string>();
        var progress = new Progress<string>(msg => progressMessages.Add(msg));

        await _chatService.GenerateMessageEmbeddingsAsync(progress);

        progressMessages.Should().Contain(m => m.Contains("Processing"));
        progressMessages.Should().Contain(m => m.Contains("Done"));
    }

    [TestMethod]
    public async Task GenerateMessageEmbeddings_ShouldSupportCancellation()
    {
        var conversation = await _chatService.CreateConversationAsync("Cancel Test");
        for (int i = 0; i < 10; i++)
        {
            await _chatService.AddMessageAsync(conversation.Id, "user", $"Message {i}");
        }

        var cts = new CancellationTokenSource();
        var callCount = 0;
        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                callCount++;
                if (callCount >= 3) cts.Cancel();
                return new float[] { 0.1f };
            });

        Func<Task> act = async () => await _chatService.GenerateMessageEmbeddingsAsync(cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task GetMessagesWithoutEmbeddingsCount_ShouldReturnCorrectCount()
    {
        var conversation = await _chatService.CreateConversationAsync("Count Test");
        var msg1 = await _chatService.AddMessageAsync(conversation.Id, "user", "1");
        var msg2 = await _chatService.AddMessageAsync(conversation.Id, "user", "2");
        var msg3 = await _chatService.AddMessageAsync(conversation.Id, "user", "3");

        await _database.UpdateMessageEmbeddingAsync(msg1.Id, "[0.1]");

        var count = await _chatService.GetMessagesWithoutEmbeddingsCountAsync();

        count.Should().Be(2);
    }

    #endregion

    private static async IAsyncEnumerable<string> AsyncEnumerable(string[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }
}
