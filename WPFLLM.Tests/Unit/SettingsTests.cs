using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using WPFLLM.Models;

namespace WPFLLM.Tests.Unit;

/// <summary>
/// Unit tests for Settings models and related configurations.
/// </summary>
[TestClass]
public class SettingsTests
{
    #region AppSettings Default Values Tests

    [TestMethod]
    public void AppSettings_DefaultValues_ShouldBeCorrect()
    {
        var settings = new AppSettings();

        settings.ApiEndpoint.Should().Be("https://openrouter.ai/api/v1");
        settings.Model.Should().Be("openai/gpt-4o-mini");
        settings.Temperature.Should().Be(0.7);
        settings.MaxTokens.Should().Be(4096);
        settings.UseRag.Should().BeFalse();
        settings.RagTopK.Should().Be(3);
        settings.RagMinSimilarity.Should().Be(0.75);
    }

    [TestMethod]
    public void AppSettings_UseLocalEmbeddings_DefaultsFalse()
    {
        var settings = new AppSettings();

        settings.UseLocalEmbeddings.Should().BeFalse();
        settings.LocalEmbeddingModel.Should().Be("multilingual-e5-large-instruct");
    }

    [TestMethod]
    public void AppSettings_UseLocalLlm_DefaultsFalse()
    {
        var settings = new AppSettings();

        settings.UseLocalLlm.Should().BeFalse();
        settings.LocalLlmModel.Should().Be("phi-3-mini-4k-instruct");
    }

    [TestMethod]
    public void AppSettings_Language_DefaultsEnglish()
    {
        var settings = new AppSettings();

        settings.Language.Should().Be("en-US");
    }

    [TestMethod]
    public void AppSettings_EncryptData_DefaultsFalse()
    {
        var settings = new AppSettings();

        settings.EncryptData.Should().BeFalse();
    }

    #endregion

    #region EmbeddingModels Tests

    [TestMethod]
    public void EmbeddingModels_Available_ShouldContainExpectedModels()
    {
        var models = EmbeddingModels.Available;

        models.Should().ContainKey("multilingual-e5-large-instruct");
        models.Should().ContainKey("multilingual-e5-large");
        models.Should().ContainKey("multilingual-e5-base");
        models.Should().ContainKey("multilingual-e5-small");
    }

    [TestMethod]
    public void EmbeddingModels_E5LargeInstruct_ShouldBeInstructModel()
    {
        var model = EmbeddingModels.Available["multilingual-e5-large-instruct"];

        model.IsInstructModel.Should().BeTrue();
        model.DefaultTaskInstruction.Should().NotBeEmpty();
    }

    [TestMethod]
    public void EmbeddingModels_AllModels_ShouldHaveRequiredFields()
    {
        foreach (var (id, model) in EmbeddingModels.Available)
        {
            model.Id.Should().NotBeEmpty($"Model {id} should have Id");
            model.DisplayName.Should().NotBeEmpty($"Model {id} should have DisplayName");
            model.Dimensions.Should().BeGreaterThan(0, $"Model {id} should have positive Dimensions");
            model.RequiredFiles.Should().NotBeEmpty($"Model {id} should have RequiredFiles");
            model.HuggingFaceRepo.Should().NotBeEmpty($"Model {id} should have HuggingFaceRepo");
        }
    }

    [TestMethod]
    public void EmbeddingModels_Dimensions_ShouldMatchExpected()
    {
        EmbeddingModels.Available["multilingual-e5-large-instruct"].Dimensions.Should().Be(1024);
        EmbeddingModels.Available["multilingual-e5-large"].Dimensions.Should().Be(1024);
        EmbeddingModels.Available["multilingual-e5-base"].Dimensions.Should().Be(768);
        EmbeddingModels.Available["multilingual-e5-small"].Dimensions.Should().Be(384);
    }

    [TestMethod]
    public void EmbeddingModels_GetModelsPath_ShouldReturnValidPath()
    {
        var path = EmbeddingModels.GetModelsPath();

        path.Should().NotBeEmpty();
        path.Should().Contain("WPFLLM");
        path.Should().Contain("models");
    }

    [TestMethod]
    public void EmbeddingModels_GetModelPath_ShouldIncludeModelId()
    {
        var path = EmbeddingModels.GetModelPath("test-model");

        path.Should().EndWith("test-model");
    }

    #endregion

    #region LocalLlmModels Tests

    [TestMethod]
    public void LocalLlmModels_Available_ShouldContainPhi3()
    {
        var models = LocalLlmModels.Available;

        models.Should().ContainKey("phi-3-mini-4k-instruct");
    }

    [TestMethod]
    public void LocalLlmModels_Phi3_ShouldHaveCorrectConfig()
    {
        var phi3 = LocalLlmModels.Available["phi-3-mini-4k-instruct"];

        phi3.ContextLength.Should().Be(4096);
        phi3.ChatTemplate.Should().Be("phi3");
        phi3.RequiredFiles.Should().NotBeEmpty();
    }

    #endregion

    #region ApiProviders Tests

    [TestMethod]
    public void ApiProviders_NativeProviders_ShouldContainMajorProviders()
    {
        var providers = ApiProviders.NativeProviders;

        providers.Should().ContainKey("OpenAI");
        providers.Should().ContainKey("Anthropic");
        providers.Should().ContainKey("Google");
        providers.Should().ContainKey("Mistral");
        providers.Should().ContainKey("Groq");
    }

    [TestMethod]
    public void ApiProviders_AllProviders_ShouldHaveValidEndpoints()
    {
        foreach (var (name, info) in ApiProviders.NativeProviders)
        {
            info.Endpoint.Should().StartWith("https://", $"Provider {name} should have HTTPS endpoint");
            info.Name.Should().NotBeEmpty($"Provider {name} should have Name");
        }
    }

    [TestMethod]
    public void ApiProviders_GetEndpoint_ShouldReturnCorrectEndpoint()
    {
        var openAiEndpoint = ApiProviders.GetEndpoint("OpenAI");

        openAiEndpoint.Should().Be("https://api.openai.com/v1");
    }

    [TestMethod]
    public void ApiProviders_GetEndpoint_UnknownProvider_ShouldReturnOpenRouter()
    {
        var endpoint = ApiProviders.GetEndpoint("UnknownProvider");

        endpoint.Should().Be(ApiProviders.OpenRouterEndpoint);
    }

    [TestMethod]
    public void ApiProviders_GetProviderNames_ShouldReturnAllProviders()
    {
        var names = ApiProviders.GetProviderNames();

        names.Should().HaveCountGreaterOrEqualTo(10);
        names.Should().Contain("OpenAI");
        names.Should().Contain("Anthropic");
    }

    #endregion

    #region SavedModel Tests

    [TestMethod]
    public void SavedModel_DefaultValues_ShouldBeCorrect()
    {
        var model = new SavedModel();

        model.IsFavorite.Should().BeFalse();
        model.UseCount.Should().Be(0);
        model.LastUsed.Should().BeNull();
    }

    [TestMethod]
    public void SavedModel_Properties_ShouldBeSettable()
    {
        var model = new SavedModel
        {
            ModelId = "test-model",
            DisplayName = "Test Model",
            Provider = "TestProvider",
            ContextLength = 8192,
            IsFavorite = true,
            UseCount = 5
        };

        model.ModelId.Should().Be("test-model");
        model.DisplayName.Should().Be("Test Model");
        model.ContextLength.Should().Be(8192);
        model.IsFavorite.Should().BeTrue();
        model.UseCount.Should().Be(5);
    }

    #endregion

    #region RetrievalMode Tests

    [TestMethod]
    public void RetrievalMode_ShouldHaveExpectedValues()
    {
        Enum.GetValues<RetrievalMode>().Should().Contain(RetrievalMode.Vector);
        Enum.GetValues<RetrievalMode>().Should().Contain(RetrievalMode.Keyword);
        Enum.GetValues<RetrievalMode>().Should().Contain(RetrievalMode.Hybrid);
    }

    [TestMethod]
    public void RetrievalMode_PatternMatch_ShouldWorkCorrectly()
    {
        var mode = RetrievalMode.Hybrid;

        var useVector = mode is RetrievalMode.Vector or RetrievalMode.Hybrid;
        var useKeyword = mode is RetrievalMode.Keyword or RetrievalMode.Hybrid;

        useVector.Should().BeTrue();
        useKeyword.Should().BeTrue();
    }

    #endregion
}
