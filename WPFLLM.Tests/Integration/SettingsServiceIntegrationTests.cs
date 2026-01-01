using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using WPFLLM.Models;
using WPFLLM.Services;

namespace WPFLLM.Tests.Integration;

/// <summary>
/// Integration tests for SettingsService testing persistence and retrieval.
/// </summary>
[TestClass]
public class SettingsServiceIntegrationTests
{
    private TestDatabaseService _database = null!;
    private SettingsService _settingsService = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _database = new TestDatabaseService();
        await _database.InitializeAsync();
        _settingsService = new SettingsService(_database);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _database.Dispose();
    }

    #region Get Settings Tests

    [TestMethod]
    public async Task GetSettingsAsync_NoSavedSettings_ShouldReturnDefaults()
    {
        var settings = await _settingsService.GetSettingsAsync();

        settings.Should().NotBeNull();
        settings.Temperature.Should().Be(0.7);
        settings.MaxTokens.Should().Be(4096);
    }

    [TestMethod]
    public async Task GetSettingsAsync_SavedSettings_ShouldReturnSaved()
    {
        var saved = new AppSettings
        {
            ApiKey = "test-key",
            Temperature = 0.5,
            MaxTokens = 2048
        };
        await _database.SaveSettingsAsync(saved);

        var loaded = await _settingsService.GetSettingsAsync();

        loaded.ApiKey.Should().Be("test-key");
        loaded.Temperature.Should().Be(0.5);
        loaded.MaxTokens.Should().Be(2048);
    }

    #endregion

    #region Save Settings Tests

    [TestMethod]
    public async Task SaveSettingsAsync_ShouldPersist()
    {
        var settings = new AppSettings
        {
            ApiKey = "new-key",
            Model = "gpt-4",
            UseRag = true
        };

        await _settingsService.SaveSettingsAsync(settings);
        var loaded = await _settingsService.GetSettingsAsync();

        loaded.ApiKey.Should().Be("new-key");
        loaded.Model.Should().Be("gpt-4");
        loaded.UseRag.Should().BeTrue();
    }

    [TestMethod]
    public async Task SaveSettingsAsync_ShouldOverwrite()
    {
        var settings1 = new AppSettings { ApiKey = "first" };
        var settings2 = new AppSettings { ApiKey = "second" };

        await _settingsService.SaveSettingsAsync(settings1);
        await _settingsService.SaveSettingsAsync(settings2);
        var loaded = await _settingsService.GetSettingsAsync();

        loaded.ApiKey.Should().Be("second");
    }

    #endregion

    #region Complex Settings Tests

    [TestMethod]
    public async Task SaveSettingsAsync_AllFields_ShouldPersistAll()
    {
        var settings = new AppSettings
        {
            ApiKey = "sk-test",
            ApiEndpoint = "https://custom.api.com/v1",
            Model = "custom-model",
            UseOpenRouter = false,
            NativeProvider = "CustomProvider",
            Temperature = 0.3,
            MaxTokens = 8192,
            SystemPrompt = "Custom system prompt",
            UseRag = true,
            RagTopK = 5,
            RagMinSimilarity = 0.8,
            SidebarCollapsed = true,
            UseLocalEmbeddings = true,
            LocalEmbeddingModel = "custom-embedding",
            UseLocalLlm = true,
            LocalLlmModel = "custom-llm",
            Language = "pl-PL",
            EncryptData = true
        };

        await _settingsService.SaveSettingsAsync(settings);
        var loaded = await _settingsService.GetSettingsAsync();

        loaded.ApiKey.Should().Be("sk-test");
        loaded.ApiEndpoint.Should().Be("https://custom.api.com/v1");
        loaded.Model.Should().Be("custom-model");
        loaded.UseOpenRouter.Should().BeFalse();
        loaded.NativeProvider.Should().Be("CustomProvider");
        loaded.Temperature.Should().Be(0.3);
        loaded.MaxTokens.Should().Be(8192);
        loaded.SystemPrompt.Should().Be("Custom system prompt");
        loaded.UseRag.Should().BeTrue();
        loaded.RagTopK.Should().Be(5);
        loaded.RagMinSimilarity.Should().Be(0.8);
        loaded.SidebarCollapsed.Should().BeTrue();
        loaded.UseLocalEmbeddings.Should().BeTrue();
        loaded.LocalEmbeddingModel.Should().Be("custom-embedding");
        loaded.UseLocalLlm.Should().BeTrue();
        loaded.LocalLlmModel.Should().Be("custom-llm");
        loaded.Language.Should().Be("pl-PL");
        loaded.EncryptData.Should().BeTrue();
    }

    [TestMethod]
    public async Task SaveSettingsAsync_UnicodeSystemPrompt_ShouldPreserve()
    {
        var settings = new AppSettings
        {
            SystemPrompt = "Jesteś pomocnym asystentem. 日本語でも話せます。🤖"
        };

        await _settingsService.SaveSettingsAsync(settings);
        var loaded = await _settingsService.GetSettingsAsync();

        loaded.SystemPrompt.Should().Be("Jesteś pomocnym asystentem. 日本語でも話せます。🤖");
    }

    [TestMethod]
    public async Task SaveSettingsAsync_LongSystemPrompt_ShouldPreserve()
    {
        var longPrompt = new string('A', 10000);
        var settings = new AppSettings { SystemPrompt = longPrompt };

        await _settingsService.SaveSettingsAsync(settings);
        var loaded = await _settingsService.GetSettingsAsync();

        loaded.SystemPrompt.Should().HaveLength(10000);
    }

    #endregion
}
