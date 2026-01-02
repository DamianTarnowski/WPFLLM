using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using WPFLLM.Models;

namespace WPFLLM.Tests.Unit;

/// <summary>
/// Tests for data validation and edge cases across models.
/// </summary>
[TestClass]
public class ValidationTests
{
    #region AppSettings Validation

    [TestMethod]
    public void AppSettings_Temperature_ShouldDefaultTo07()
    {
        var settings = new AppSettings();

        settings.Temperature.Should().Be(0.7);
    }

    [TestMethod]
    public void AppSettings_Temperature_ShouldAcceptValidRange()
    {
        var settings = new AppSettings { Temperature = 0.0 };
        settings.Temperature.Should().Be(0.0);

        settings.Temperature = 1.0;
        settings.Temperature.Should().Be(1.0);

        settings.Temperature = 0.5;
        settings.Temperature.Should().Be(0.5);
    }

    [TestMethod]
    public void AppSettings_MaxTokens_ShouldDefaultTo4096()
    {
        var settings = new AppSettings();

        settings.MaxTokens.Should().Be(4096);
    }

    [TestMethod]
    public void AppSettings_RagMinSimilarity_ShouldDefaultTo05()
    {
        var settings = new AppSettings();

        settings.RagMinSimilarity.Should().Be(0.5);
    }

    [TestMethod]
    public void AppSettings_RagTopK_ShouldDefaultTo3()
    {
        var settings = new AppSettings();

        settings.RagTopK.Should().Be(3);
    }

    [TestMethod]
    public void AppSettings_ApiEndpoint_ShouldDefaultToOpenRouter()
    {
        var settings = new AppSettings();

        settings.ApiEndpoint.Should().Be("https://openrouter.ai/api/v1");
    }

    [TestMethod]
    public void AppSettings_Model_ShouldDefaultToGpt4oMini()
    {
        var settings = new AppSettings();

        settings.Model.Should().Be("openai/gpt-4o-mini");
    }

    #endregion

    #region SavedModel Validation

    [TestMethod]
    public void SavedModel_IsFavorite_ShouldDefaultToFalse()
    {
        var model = new SavedModel();

        model.IsFavorite.Should().BeFalse();
    }

    [TestMethod]
    public void SavedModel_UseCount_ShouldDefaultToZero()
    {
        var model = new SavedModel();

        model.UseCount.Should().Be(0);
    }

    [TestMethod]
    public void SavedModel_LastUsed_ShouldDefaultToNull()
    {
        var model = new SavedModel();

        model.LastUsed.Should().BeNull();
    }

    [TestMethod]
    public void SavedModel_ContextLength_ShouldAcceptLargeValues()
    {
        var model = new SavedModel { ContextLength = 128000 };

        model.ContextLength.Should().Be(128000);
    }

    #endregion

    #region ChatMessage Validation

    [TestMethod]
    public void ChatMessage_Role_ShouldAcceptValidRoles()
    {
        var userMsg = new ChatMessage { Role = "user" };
        var assistantMsg = new ChatMessage { Role = "assistant" };
        var systemMsg = new ChatMessage { Role = "system" };

        userMsg.Role.Should().Be("user");
        assistantMsg.Role.Should().Be("assistant");
        systemMsg.Role.Should().Be("system");
    }

    [TestMethod]
    public void ChatMessage_Content_ShouldAcceptLongContent()
    {
        var longContent = new string('A', 100000);
        var message = new ChatMessage { Content = longContent };

        message.Content.Should().HaveLength(100000);
    }

    [TestMethod]
    public void ChatMessage_Content_ShouldAcceptUnicode()
    {
        var unicodeContent = "Polski: ąćęłńóśźż 日本語 🎉🚀✨";
        var message = new ChatMessage { Content = unicodeContent };

        message.Content.Should().Be(unicodeContent);
    }

    [TestMethod]
    public void ChatMessage_Embedding_ShouldAcceptJson()
    {
        var embedding = "[0.1, 0.2, 0.3, 0.4, 0.5]";
        var message = new ChatMessage { Embedding = embedding };

        message.Embedding.Should().Be(embedding);
    }

    #endregion

    #region Conversation Validation

    [TestMethod]
    public void Conversation_Title_ShouldAcceptEmptyString()
    {
        var conversation = new Conversation { Title = "" };

        conversation.Title.Should().BeEmpty();
    }

    [TestMethod]
    public void Conversation_Title_ShouldAcceptLongTitle()
    {
        var longTitle = new string('X', 1000);
        var conversation = new Conversation { Title = longTitle };

        conversation.Title.Should().HaveLength(1000);
    }

    [TestMethod]
    public void Conversation_Timestamps_ShouldBeSettable()
    {
        var created = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var updated = new DateTime(2024, 6, 15, 18, 30, 0, DateTimeKind.Utc);

        var conversation = new Conversation
        {
            CreatedAt = created,
            UpdatedAt = updated
        };

        conversation.CreatedAt.Should().Be(created);
        conversation.UpdatedAt.Should().Be(updated);
    }

    #endregion

    #region RagDocument Validation

    [TestMethod]
    public void RagDocument_FileName_ShouldAcceptVariousExtensions()
    {
        var extensions = new[] { ".txt", ".md", ".pdf", ".docx", ".json", ".csv", ".xml" };

        foreach (var ext in extensions)
        {
            var doc = new RagDocument { FileName = $"document{ext}" };
            doc.FileName.Should().EndWith(ext);
        }
    }

    [TestMethod]
    public void RagDocument_Content_ShouldAcceptLargeContent()
    {
        var largeContent = new string('A', 1000000);
        var doc = new RagDocument { Content = largeContent };

        doc.Content.Should().HaveLength(1000000);
    }

    #endregion

    #region RagChunk Validation

    [TestMethod]
    public void RagChunk_ChunkIndex_ShouldAcceptZero()
    {
        var chunk = new RagChunk { ChunkIndex = 0 };

        chunk.ChunkIndex.Should().Be(0);
    }

    [TestMethod]
    public void RagChunk_ChunkIndex_ShouldAcceptLargeValues()
    {
        var chunk = new RagChunk { ChunkIndex = 9999 };

        chunk.ChunkIndex.Should().Be(9999);
    }

    [TestMethod]
    public void RagChunk_Embedding_CanBeNull()
    {
        var chunk = new RagChunk { Embedding = null };

        chunk.Embedding.Should().BeNull();
    }

    #endregion

    #region RetrievalResult Validation

    [TestMethod]
    public void RetrievalResult_CombinedContext_ShouldJoinChunks()
    {
        var result = new RetrievalResult
        {
            Chunks = new List<RetrievedChunk>
            {
                new() { Content = "First chunk" },
                new() { Content = "Second chunk" },
                new() { Content = "Third chunk" }
            }
        };

        result.CombinedContext.Should().Contain("First chunk");
        result.CombinedContext.Should().Contain("Second chunk");
        result.CombinedContext.Should().Contain("Third chunk");
        result.CombinedContext.Should().Contain("---");
    }

    [TestMethod]
    public void RetrievalResult_EmptyChunks_ShouldReturnEmptyContext()
    {
        var result = new RetrievalResult { Chunks = new List<RetrievedChunk>() };

        result.CombinedContext.Should().BeEmpty();
    }

    [TestMethod]
    public void RetrievalMetrics_DefaultMode_ShouldBeHybrid()
    {
        var metrics = new RetrievalMetrics();

        metrics.Mode.Should().Be(RetrievalMode.Hybrid);
    }

    #endregion

    #region RetrievedChunk Validation

    [TestMethod]
    public void RetrievedChunk_Scores_ShouldAcceptFullRange()
    {
        var chunk = new RetrievedChunk
        {
            VectorScore = 0.0,
            KeywordScore = 100.0,
            FusedScore = 0.5
        };

        chunk.VectorScore.Should().Be(0.0);
        chunk.KeywordScore.Should().Be(100.0);
        chunk.FusedScore.Should().Be(0.5);
    }

    [TestMethod]
    public void RetrievedChunk_MatchedTerms_ShouldDefaultToEmpty()
    {
        var chunk = new RetrievedChunk();

        chunk.MatchedTerms.Should().NotBeNull();
        chunk.MatchedTerms.Should().BeEmpty();
    }

    #endregion

    #region EmbeddingModelInfo Validation

    [TestMethod]
    public void EmbeddingModelInfo_RequiredFiles_ShouldIncludeOnnxAndTokenizer()
    {
        foreach (var (id, model) in EmbeddingModels.Available)
        {
            model.RequiredFiles.Should().Contain("model.onnx", $"Model {id} should require model.onnx");
            model.RequiredFiles.Should().Contain("tokenizer.json", $"Model {id} should require tokenizer.json");
        }
    }

    [TestMethod]
    public void EmbeddingModelInfo_QualityRating_ShouldBeInValidRange()
    {
        foreach (var (id, model) in EmbeddingModels.Available)
        {
            model.QualityRating.Should().BeInRange(1, 5, $"Model {id} should have quality rating 1-5");
        }
    }

    [TestMethod]
    public void EmbeddingModelInfo_Languages_ShouldIncludePolish()
    {
        foreach (var (id, model) in EmbeddingModels.Available)
        {
            model.Languages.Should().Contain("pl", $"Model {id} should support Polish");
        }
    }

    #endregion
}
