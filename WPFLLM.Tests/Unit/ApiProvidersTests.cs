using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using WPFLLM.Models;

namespace WPFLLM.Tests.Unit;

/// <summary>
/// Tests for API providers configuration and endpoint resolution.
/// </summary>
[TestClass]
public class ApiProvidersTests
{
    #region Provider Registration Tests

    [TestMethod]
    public void NativeProviders_ShouldContainAllMajorProviders()
    {
        var providers = ApiProviders.NativeProviders;

        providers.Should().ContainKey("OpenAI");
        providers.Should().ContainKey("Anthropic");
        providers.Should().ContainKey("Google");
        providers.Should().ContainKey("Mistral");
        providers.Should().ContainKey("Groq");
        providers.Should().ContainKey("Together");
        providers.Should().ContainKey("Fireworks");
        providers.Should().ContainKey("DeepSeek");
        providers.Should().ContainKey("xAI");
        providers.Should().ContainKey("SambaNova");
        providers.Should().ContainKey("Perplexity");
        providers.Should().ContainKey("Cohere");
    }

    [TestMethod]
    public void NativeProviders_AllShouldHaveHttpsEndpoints()
    {
        foreach (var (name, info) in ApiProviders.NativeProviders)
        {
            info.Endpoint.Should().StartWith("https://", $"Provider {name} should use HTTPS");
        }
    }

    [TestMethod]
    public void NativeProviders_AllShouldHaveKeyUrls()
    {
        foreach (var (name, info) in ApiProviders.NativeProviders)
        {
            info.KeyUrl.Should().StartWith("https://", $"Provider {name} should have HTTPS key URL");
        }
    }

    [TestMethod]
    public void NativeProviders_AllShouldHaveDescriptions()
    {
        foreach (var (name, info) in ApiProviders.NativeProviders)
        {
            info.Description.Should().NotBeEmpty($"Provider {name} should have description");
        }
    }

    #endregion

    #region Endpoint Resolution Tests

    [TestMethod]
    public void GetEndpoint_OpenAI_ShouldReturnCorrectEndpoint()
    {
        var endpoint = ApiProviders.GetEndpoint("OpenAI");

        endpoint.Should().Be("https://api.openai.com/v1");
    }

    [TestMethod]
    public void GetEndpoint_Anthropic_ShouldReturnCorrectEndpoint()
    {
        var endpoint = ApiProviders.GetEndpoint("Anthropic");

        endpoint.Should().Be("https://api.anthropic.com/v1");
    }

    [TestMethod]
    public void GetEndpoint_UnknownProvider_ShouldReturnOpenRouter()
    {
        var endpoint = ApiProviders.GetEndpoint("NonExistentProvider");

        endpoint.Should().Be(ApiProviders.OpenRouterEndpoint);
    }

    [TestMethod]
    public void GetEndpoint_EmptyProvider_ShouldReturnOpenRouter()
    {
        var endpoint = ApiProviders.GetEndpoint("");

        endpoint.Should().Be(ApiProviders.OpenRouterEndpoint);
    }

    [TestMethod]
    public void OpenRouterEndpoint_ShouldBeCorrect()
    {
        ApiProviders.OpenRouterEndpoint.Should().Be("https://openrouter.ai/api/v1");
    }

    #endregion

    #region Provider Names Tests

    [TestMethod]
    public void GetProviderNames_ShouldReturnAllProviders()
    {
        var names = ApiProviders.GetProviderNames();

        names.Should().HaveCountGreaterOrEqualTo(12);
        names.Should().Contain("OpenAI");
        names.Should().Contain("Anthropic");
    }

    [TestMethod]
    public void GetProviderNames_ShouldMatchNativeProvidersKeys()
    {
        var names = ApiProviders.GetProviderNames();
        var keys = ApiProviders.NativeProviders.Keys.ToList();

        names.Should().BeEquivalentTo(keys);
    }

    #endregion

    #region ApiProviderInfo Record Tests

    [TestMethod]
    public void ApiProviderInfo_ShouldSupportEquality()
    {
        var info1 = new ApiProviderInfo("Test", "https://api.test.com", "Description", "https://keys.test.com");
        var info2 = new ApiProviderInfo("Test", "https://api.test.com", "Description", "https://keys.test.com");

        info1.Should().Be(info2);
    }

    [TestMethod]
    public void ApiProviderInfo_DifferentValues_ShouldNotBeEqual()
    {
        var info1 = new ApiProviderInfo("Test1", "https://api.test.com", "Description", "https://keys.test.com");
        var info2 = new ApiProviderInfo("Test2", "https://api.test.com", "Description", "https://keys.test.com");

        info1.Should().NotBe(info2);
    }

    [TestMethod]
    public void ApiProviderInfo_ShouldDeconstructCorrectly()
    {
        var info = new ApiProviderInfo("Name", "Endpoint", "Desc", "KeyUrl");
        var (name, endpoint, desc, keyUrl) = info;

        name.Should().Be("Name");
        endpoint.Should().Be("Endpoint");
        desc.Should().Be("Desc");
        keyUrl.Should().Be("KeyUrl");
    }

    #endregion

    #region Specific Provider Tests

    [TestMethod]
    public void OpenAI_ShouldHaveCorrectConfiguration()
    {
        var openai = ApiProviders.NativeProviders["OpenAI"];

        openai.Name.Should().Be("OpenAI");
        openai.Endpoint.Should().Be("https://api.openai.com/v1");
        openai.Description.Should().Contain("GPT");
    }

    [TestMethod]
    public void Anthropic_ShouldHaveCorrectConfiguration()
    {
        var anthropic = ApiProviders.NativeProviders["Anthropic"];

        anthropic.Name.Should().Be("Anthropic");
        anthropic.Endpoint.Should().Be("https://api.anthropic.com/v1");
        anthropic.Description.Should().Contain("Claude");
    }

    [TestMethod]
    public void Google_ShouldHaveCorrectConfiguration()
    {
        var google = ApiProviders.NativeProviders["Google"];

        google.Name.Should().Be("Google AI");
        google.Description.Should().Contain("Gemini");
    }

    [TestMethod]
    public void DeepSeek_ShouldHaveCorrectConfiguration()
    {
        var deepseek = ApiProviders.NativeProviders["DeepSeek"];

        deepseek.Endpoint.Should().Be("https://api.deepseek.com/v1");
        deepseek.Description.Should().Contain("DeepSeek");
    }

    [TestMethod]
    public void Groq_ShouldHaveCorrectConfiguration()
    {
        var groq = ApiProviders.NativeProviders["Groq"];

        groq.Endpoint.Should().Contain("groq.com");
        // Description contains "szybkie" (Polish for fast)
        (groq.Description.Contains("szybkie", StringComparison.OrdinalIgnoreCase) ||
         groq.Description.Contains("fast", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    #endregion
}
