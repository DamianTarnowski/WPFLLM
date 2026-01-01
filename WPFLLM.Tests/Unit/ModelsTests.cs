using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using WPFLLM.Models;

namespace WPFLLM.Tests.Unit;

/// <summary>
/// Unit tests for data models (ChatMessage, Conversation, RagDocument, etc.)
/// </summary>
[TestClass]
public class ModelsTests
{
    #region ChatMessage Tests

    [TestMethod]
    public void ChatMessage_DefaultValues_ShouldBeCorrect()
    {
        var message = new ChatMessage();

        message.Id.Should().Be(0);
        message.ConversationId.Should().Be(0);
        message.Role.Should().BeEmpty();
        message.Content.Should().BeEmpty();
        message.Embedding.Should().BeNull();
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void ChatMessage_Properties_ShouldBeSettable()
    {
        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            Id = 123,
            ConversationId = 456,
            Role = "user",
            Content = "Hello world",
            Embedding = "[0.1, 0.2, 0.3]",
            CreatedAt = now
        };

        message.Id.Should().Be(123);
        message.ConversationId.Should().Be(456);
        message.Role.Should().Be("user");
        message.Content.Should().Be("Hello world");
        message.Embedding.Should().Be("[0.1, 0.2, 0.3]");
        message.CreatedAt.Should().Be(now);
    }

    #endregion

    #region Conversation Tests

    [TestMethod]
    public void Conversation_DefaultValues_ShouldBeCorrect()
    {
        var conversation = new Conversation();

        conversation.Id.Should().Be(0);
        conversation.Title.Should().BeEmpty();
        conversation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        conversation.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Conversation_Properties_ShouldBeSettable()
    {
        var created = DateTime.UtcNow.AddDays(-1);
        var updated = DateTime.UtcNow;
        
        var conversation = new Conversation
        {
            Id = 1,
            Title = "Test Conversation",
            CreatedAt = created,
            UpdatedAt = updated
        };

        conversation.Id.Should().Be(1);
        conversation.Title.Should().Be("Test Conversation");
        conversation.CreatedAt.Should().Be(created);
        conversation.UpdatedAt.Should().Be(updated);
    }

    #endregion

    #region RagDocument Tests

    [TestMethod]
    public void RagDocument_DefaultValues_ShouldBeCorrect()
    {
        var document = new RagDocument();

        document.Id.Should().Be(0);
        document.FileName.Should().BeEmpty();
        document.Content.Should().BeEmpty();
        document.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void RagDocument_Properties_ShouldBeSettable()
    {
        var document = new RagDocument
        {
            Id = 42,
            FileName = "document.pdf",
            Content = "Document content here",
            CreatedAt = DateTime.UtcNow
        };

        document.Id.Should().Be(42);
        document.FileName.Should().Be("document.pdf");
        document.Content.Should().Be("Document content here");
    }

    #endregion

    #region RagChunk Tests

    [TestMethod]
    public void RagChunk_DefaultValues_ShouldBeCorrect()
    {
        var chunk = new RagChunk();

        chunk.Id.Should().Be(0);
        chunk.DocumentId.Should().Be(0);
        chunk.Content.Should().BeEmpty();
        chunk.ChunkIndex.Should().Be(0);
        chunk.Embedding.Should().BeNull();
    }

    [TestMethod]
    public void RagChunk_Properties_ShouldBeSettable()
    {
        var chunk = new RagChunk
        {
            Id = 1,
            DocumentId = 10,
            Content = "Chunk content",
            ChunkIndex = 5,
            Embedding = "[0.5, 0.5]"
        };

        chunk.Id.Should().Be(1);
        chunk.DocumentId.Should().Be(10);
        chunk.Content.Should().Be("Chunk content");
        chunk.ChunkIndex.Should().Be(5);
        chunk.Embedding.Should().Be("[0.5, 0.5]");
    }

    #endregion

    #region MessageSearchResult Tests

    [TestMethod]
    public void MessageSearchResult_Properties_ShouldBeSettable()
    {
        var message = new ChatMessage { Id = 1, Content = "Test" };
        var conversation = new Conversation { Id = 1, Title = "Conv" };

        var result = new MessageSearchResult
        {
            Message = message,
            Conversation = conversation,
            Score = 0.95
        };

        result.Message.Should().Be(message);
        result.Conversation.Should().Be(conversation);
        result.Score.Should().Be(0.95);
    }

    #endregion

    #region RetrievalResult Tests

    [TestMethod]
    public void RetrievalResult_DefaultValues_ShouldBeInitialized()
    {
        var result = new RetrievalResult();

        result.Chunks.Should().NotBeNull();
        result.Metrics.Should().NotBeNull();
    }

    [TestMethod]
    public void RetrievedChunk_Properties_ShouldBeSettable()
    {
        var chunk = new RetrievedChunk
        {
            ChunkId = 1,
            DocumentId = 2,
            DocumentName = "doc.txt",
            Content = "Content",
            ChunkIndex = 3,
            VectorScore = 0.9,
            KeywordScore = 5.0,
            FusedScore = 0.8
        };

        chunk.ChunkId.Should().Be(1);
        chunk.DocumentId.Should().Be(2);
        chunk.DocumentName.Should().Be("doc.txt");
        chunk.VectorScore.Should().Be(0.9);
        chunk.KeywordScore.Should().Be(5.0);
        chunk.FusedScore.Should().Be(0.8);
    }

    [TestMethod]
    public void RetrievalMetrics_Properties_ShouldBeSettable()
    {
        var metrics = new RetrievalMetrics
        {
            Mode = RetrievalMode.Hybrid,
            TopK = 5,
            MinSimilarity = 0.7,
            TotalChunksSearched = 100,
            VectorMatches = 10,
            KeywordMatches = 15,
            FinalResults = 5,
            EmbeddingTimeMs = 50,
            RetrievalTimeMs = 100,
            TotalTimeMs = 150
        };

        metrics.Mode.Should().Be(RetrievalMode.Hybrid);
        metrics.TopK.Should().Be(5);
        metrics.TotalChunksSearched.Should().Be(100);
        metrics.VectorMatches.Should().Be(10);
        metrics.KeywordMatches.Should().Be(15);
        metrics.TotalTimeMs.Should().Be(150);
    }

    #endregion

    #region SavedApiKey Tests

    [TestMethod]
    public void SavedApiKey_Properties_ShouldBeSettable()
    {
        var now = DateTime.UtcNow;
        var key = new SavedApiKey
        {
            Id = 1,
            Provider = "OpenAI",
            ApiKey = "sk-test-key",
            CreatedAt = now,
            UpdatedAt = now
        };

        key.Id.Should().Be(1);
        key.Provider.Should().Be("OpenAI");
        key.ApiKey.Should().Be("sk-test-key");
        key.CreatedAt.Should().Be(now);
        key.UpdatedAt.Should().Be(now);
    }

    #endregion

    #region EmbeddingModelInfo Tests

    [TestMethod]
    public void EmbeddingModelInfo_DefaultValues_ShouldBeCorrect()
    {
        var info = new EmbeddingModelInfo();

        info.Id.Should().BeEmpty();
        info.DisplayName.Should().BeEmpty();
        info.Dimensions.Should().Be(0);
        info.RequiredFiles.Should().BeEmpty();
        info.Languages.Should().BeEmpty();
        info.IsInstructModel.Should().BeFalse();
    }

    [TestMethod]
    public void EmbeddingModelInfo_Properties_ShouldBeSettable()
    {
        var info = new EmbeddingModelInfo
        {
            Id = "test-model",
            DisplayName = "Test Model",
            Description = "A test model",
            Dimensions = 768,
            SizeBytes = 1_000_000_000,
            RequiredFiles = new[] { "model.onnx", "tokenizer.json" },
            HuggingFaceRepo = "test/model",
            Languages = new[] { "en", "pl" },
            QualityRating = 4,
            RamRequired = "2 GB",
            InferenceSpeed = "~100ms",
            RecommendedFor = "Testing",
            IsInstructModel = true,
            DefaultTaskInstruction = "Test instruction"
        };

        info.Id.Should().Be("test-model");
        info.Dimensions.Should().Be(768);
        info.IsInstructModel.Should().BeTrue();
        info.Languages.Should().Contain("pl");
    }

    #endregion

    #region ApiProviderInfo Tests

    [TestMethod]
    public void ApiProviderInfo_Record_ShouldWorkCorrectly()
    {
        var info = new ApiProviderInfo("TestProvider", "https://api.test.com", "Test API", "https://test.com/keys");

        info.Name.Should().Be("TestProvider");
        info.Endpoint.Should().Be("https://api.test.com");
        info.Description.Should().Be("Test API");
        info.KeyUrl.Should().Be("https://test.com/keys");
    }

    [TestMethod]
    public void ApiProviderInfo_Equality_ShouldWork()
    {
        var info1 = new ApiProviderInfo("Test", "https://api.test.com", "Desc", "https://keys");
        var info2 = new ApiProviderInfo("Test", "https://api.test.com", "Desc", "https://keys");

        info1.Should().Be(info2);
    }

    #endregion
}
