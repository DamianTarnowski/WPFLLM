using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Reflection;
using WPFLLM.Services;
using WPFLLM.Models;

namespace WPFLLM.Tests.Unit;

/// <summary>
/// Tests for E5 prefix handling in LocalEmbeddingService.
/// E5 models require specific prefixes for queries and passages.
/// </summary>
[TestClass]
public class LocalEmbeddingPrefixTests
{
    private MethodInfo _prepareE5TextMethod = null!;
    private LocalEmbeddingService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new LocalEmbeddingService();
        _prepareE5TextMethod = typeof(LocalEmbeddingService).GetMethod("PrepareE5Text",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _service.Dispose();
    }

    #region Standard E5 Prefix Tests

    [TestMethod]
    public void PrepareE5Text_Query_ShouldAddQueryPrefix()
    {
        // Set model to non-instruct (standard E5)
        SetCurrentModel("multilingual-e5-large");

        var result = InvokePrepareE5Text("What is machine learning?", isQuery: true);

        result.Should().StartWith("query: ");
        result.Should().Contain("What is machine learning?");
    }

    [TestMethod]
    public void PrepareE5Text_Passage_ShouldAddPassagePrefix()
    {
        SetCurrentModel("multilingual-e5-large");

        var result = InvokePrepareE5Text("Machine learning is a field of AI.", isQuery: false);

        result.Should().StartWith("passage: ");
        result.Should().Contain("Machine learning is a field of AI.");
    }

    [TestMethod]
    public void PrepareE5Text_AlreadyHasQueryPrefix_ShouldNotDoublePrefix()
    {
        SetCurrentModel("multilingual-e5-large");

        var result = InvokePrepareE5Text("query: What is AI?", isQuery: true);

        result.Should().Be("query: What is AI?");
        result.Should().NotStartWith("query: query:");
    }

    [TestMethod]
    public void PrepareE5Text_AlreadyHasPassagePrefix_ShouldNotDoublePrefix()
    {
        SetCurrentModel("multilingual-e5-large");

        var result = InvokePrepareE5Text("passage: AI is artificial intelligence.", isQuery: false);

        result.Should().Be("passage: AI is artificial intelligence.");
        result.Should().NotStartWith("passage: passage:");
    }

    #endregion

    #region Instruct Model Prefix Tests

    [TestMethod]
    public void PrepareE5Text_InstructModel_Query_ShouldUseInstructFormat()
    {
        SetCurrentModel("multilingual-e5-large-instruct");

        var result = InvokePrepareE5Text("What is deep learning?", isQuery: true);

        result.Should().StartWith("Instruct:");
        result.Should().Contain("Query:");
        result.Should().Contain("What is deep learning?");
    }

    [TestMethod]
    public void PrepareE5Text_InstructModel_Passage_ShouldNotAddPrefix()
    {
        SetCurrentModel("multilingual-e5-large-instruct");

        var result = InvokePrepareE5Text("Deep learning uses neural networks.", isQuery: false);

        // Instruct models don't add prefix for documents
        result.Should().Be("Deep learning uses neural networks.");
    }

    [TestMethod]
    public void PrepareE5Text_InstructModel_AlreadyHasInstructPrefix_ShouldNotDoublePrefix()
    {
        SetCurrentModel("multilingual-e5-large-instruct");

        var result = InvokePrepareE5Text("Instruct: Custom task\nQuery: My query", isQuery: true);

        result.Should().Be("Instruct: Custom task\nQuery: My query");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void PrepareE5Text_EmptyText_ShouldStillAddPrefix()
    {
        SetCurrentModel("multilingual-e5-large");

        var result = InvokePrepareE5Text("", isQuery: true);

        result.Should().Be("query: ");
    }

    [TestMethod]
    public void PrepareE5Text_WhitespaceText_ShouldAddPrefix()
    {
        SetCurrentModel("multilingual-e5-large");

        var result = InvokePrepareE5Text("   ", isQuery: true);

        result.Should().StartWith("query: ");
    }

    [TestMethod]
    public void PrepareE5Text_UnicodeText_ShouldPreserve()
    {
        SetCurrentModel("multilingual-e5-large");

        var result = InvokePrepareE5Text("Polskie pytanie: co to jest?", isQuery: true);

        result.Should().Contain("Polskie pytanie: co to jest?");
    }

    [TestMethod]
    public void PrepareE5Text_LongText_ShouldAddPrefix()
    {
        SetCurrentModel("multilingual-e5-large");

        var longText = new string('A', 5000);
        var result = InvokePrepareE5Text(longText, isQuery: false);

        result.Should().StartWith("passage: ");
        result.Should().HaveLength(longText.Length + "passage: ".Length);
    }

    #endregion

    #region Model Configuration Tests

    [TestMethod]
    public void EmbeddingModels_InstructModel_ShouldHaveIsInstructTrue()
    {
        var instructModel = EmbeddingModels.Available["multilingual-e5-large-instruct"];

        instructModel.IsInstructModel.Should().BeTrue();
    }

    [TestMethod]
    public void EmbeddingModels_StandardModels_ShouldHaveIsInstructFalse()
    {
        var standardModels = new[] { "multilingual-e5-large", "multilingual-e5-base", "multilingual-e5-small" };

        foreach (var modelId in standardModels)
        {
            var model = EmbeddingModels.Available[modelId];
            model.IsInstructModel.Should().BeFalse($"{modelId} should not be instruct model");
        }
    }

    [TestMethod]
    public void EmbeddingModels_InstructModel_ShouldHaveDefaultTaskInstruction()
    {
        var instructModel = EmbeddingModels.Available["multilingual-e5-large-instruct"];

        instructModel.DefaultTaskInstruction.Should().NotBeEmpty();
        instructModel.DefaultTaskInstruction.Should().Contain("query");
    }

    #endregion

    #region Helper Methods

    private void SetCurrentModel(string modelId)
    {
        var field = typeof(LocalEmbeddingService).GetField("_currentModelId",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(_service, modelId);
    }

    private string InvokePrepareE5Text(string text, bool isQuery)
    {
        return (string)_prepareE5TextMethod.Invoke(_service, new object[] { text, isQuery })!;
    }

    #endregion
}
