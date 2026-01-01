using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using WPFLLM.Models;
using WPFLLM.Services;

namespace WPFLLM.Tests.Integration.RealApi;

/// <summary>
/// Real integration tests against OpenRouter API.
/// These tests are SKIPPED if OPENROUTER_API_KEY is not available.
/// Run with: dotnet test --filter "Category=RealApi"
/// </summary>
[TestClass]
[TestCategory("RealApi")]
public class OpenRouterApiTests
{
    private static string? _apiKey;
    private static bool _hasApiKey;
    private HttpClient _httpClient = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        // Try to load API key from environment variable first
        _apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

        // If not found, try to load from .env file
        if (string.IsNullOrEmpty(_apiKey))
        {
            var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env");
            if (File.Exists(envPath))
            {
                var lines = File.ReadAllLines(envPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("OPENROUTER_API_KEY="))
                    {
                        _apiKey = line.Substring("OPENROUTER_API_KEY=".Length).Trim();
                        break;
                    }
                }
            }
        }

        _hasApiKey = !string.IsNullOrEmpty(_apiKey);
    }

    [TestInitialize]
    public void Setup()
    {
        if (!_hasApiKey)
        {
            Assert.Inconclusive("OPENROUTER_API_KEY not found. Skipping real API tests.");
            return;
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://openrouter.ai/api/v1/"),
            Timeout = TimeSpan.FromSeconds(60)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/wpfllm");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "WPFLLM Tests");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _httpClient?.Dispose();
    }

    #region Chat Completion Tests

    [TestMethod]
    public async Task ChatCompletion_SimpleMessage_ShouldReturnResponse()
    {
        if (!_hasApiKey) { Assert.Inconclusive("No API key"); return; }

        var request = new
        {
            model = "openai/gpt-4o-mini",
            messages = new[]
            {
                new { role = "user", content = "Say 'Hello' and nothing else." }
            },
            max_tokens = 10
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue($"API returned: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var message = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        message.Should().NotBeNullOrEmpty();
        message!.ToLower().Should().Contain("hello");
    }

    [TestMethod]
    public async Task ChatCompletion_SystemPrompt_ShouldFollowInstructions()
    {
        if (!_hasApiKey) { Assert.Inconclusive("No API key"); return; }

        var request = new
        {
            model = "openai/gpt-4o-mini",
            messages = new object[]
            {
                new { role = "system", content = "You are a helpful assistant. Always respond in Polish." },
                new { role = "user", content = "What is 2+2?" }
            },
            max_tokens = 50
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue($"API returned: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var message = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        message.Should().NotBeNullOrEmpty();
        // Should contain Polish word for "four" or the number
        (message!.Contains("cztery", StringComparison.OrdinalIgnoreCase) || 
         message.Contains("4")).Should().BeTrue($"Response was: {message}");
    }

    [TestMethod]
    public async Task ChatCompletion_MultiTurn_ShouldMaintainContext()
    {
        if (!_hasApiKey) { Assert.Inconclusive("No API key"); return; }

        var request = new
        {
            model = "openai/gpt-4o-mini",
            messages = new object[]
            {
                new { role = "user", content = "My name is TestUser123." },
                new { role = "assistant", content = "Nice to meet you, TestUser123!" },
                new { role = "user", content = "What is my name?" }
            },
            max_tokens = 30
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue();

        using var doc = JsonDocument.Parse(responseBody);
        var message = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        message.Should().Contain("TestUser123");
    }

    [TestMethod]
    public async Task ChatCompletion_Temperature_ShouldAffectCreativity()
    {
        if (!_hasApiKey) { Assert.Inconclusive("No API key"); return; }

        // Low temperature - more deterministic
        var request = new
        {
            model = "openai/gpt-4o-mini",
            messages = new[]
            {
                new { role = "user", content = "Complete this: The capital of France is" }
            },
            max_tokens = 10,
            temperature = 0.0
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue();

        using var doc = JsonDocument.Parse(responseBody);
        var message = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        message.Should().Contain("Paris");
    }

    #endregion

    #region Streaming Tests

    [TestMethod]
    public async Task ChatCompletion_Streaming_ShouldStreamChunks()
    {
        if (!_hasApiKey) { Assert.Inconclusive("No API key"); return; }

        var request = new
        {
            model = "openai/gpt-4o-mini",
            messages = new[]
            {
                new { role = "user", content = "Count from 1 to 5." }
            },
            max_tokens = 30,
            stream = true
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request2 = new HttpRequestMessage(HttpMethod.Post, "chat/completions") { Content = content };
        var response = await _httpClient.SendAsync(request2, HttpCompletionOption.ResponseHeadersRead);
        response.IsSuccessStatusCode.Should().BeTrue();

        var chunks = new List<string>();
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;
            if (line == "data: [DONE]") break;

            var data = line.Substring(6);
            try
            {
                using var doc = JsonDocument.Parse(data);
                if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("content", out var contentProp))
                {
                    var chunk = contentProp.GetString();
                    if (!string.IsNullOrEmpty(chunk))
                        chunks.Add(chunk);
                }
            }
            catch { }
        }

        chunks.Should().NotBeEmpty();
        var fullResponse = string.Join("", chunks);
        fullResponse.Should().ContainAny("1", "2", "3", "4", "5");
    }

    #endregion

    #region Model Tests

    [TestMethod]
    public async Task Models_List_ShouldReturnAvailableModels()
    {
        if (!_hasApiKey) { Assert.Inconclusive("No API key"); return; }

        var response = await _httpClient.GetAsync("models");
        var responseBody = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue();

        using var doc = JsonDocument.Parse(responseBody);
        var models = doc.RootElement.GetProperty("data");

        models.GetArrayLength().Should().BeGreaterThan(0);

        // Check if common models are available
        var modelIds = new List<string>();
        foreach (var model in models.EnumerateArray())
        {
            modelIds.Add(model.GetProperty("id").GetString()!);
        }

        modelIds.Should().Contain(m => m.Contains("gpt"));
        modelIds.Should().Contain(m => m.Contains("claude"));
    }

    [TestMethod]
    public async Task Model_GPT4oMini_ShouldBeAvailable()
    {
        if (!_hasApiKey) { Assert.Inconclusive("No API key"); return; }

        var request = new
        {
            model = "openai/gpt-4o-mini",
            messages = new[]
            {
                new { role = "user", content = "Hi" }
            },
            max_tokens = 5
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content);

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [TestMethod]
    public async Task Model_Claude_ShouldBeAvailable()
    {
        if (!_hasApiKey) { Assert.Inconclusive("No API key"); return; }

        var request = new
        {
            model = "anthropic/claude-3-haiku",
            messages = new[]
            {
                new { role = "user", content = "Hi" }
            },
            max_tokens = 5
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content);

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    public async Task ChatCompletion_InvalidModel_ShouldReturnError()
    {
        if (!_hasApiKey) { Assert.Inconclusive("No API key"); return; }

        var request = new
        {
            model = "nonexistent/fake-model-12345",
            messages = new[]
            {
                new { role = "user", content = "Hi" }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [TestMethod]
    public async Task ChatCompletion_EmptyMessages_ShouldReturnError()
    {
        if (!_hasApiKey) { Assert.Inconclusive("No API key"); return; }

        var request = new
        {
            model = "openai/gpt-4o-mini",
            messages = Array.Empty<object>()
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    #endregion

    #region RAG Context Tests

    [TestMethod]
    public async Task ChatCompletion_WithContext_ShouldUseProvidedInfo()
    {
        if (!_hasApiKey) { Assert.Inconclusive("No API key"); return; }

        var context = """
            DOCUMENT: Company Policy
            The company vacation policy states that employees receive 25 days of paid vacation per year.
            New employees start accruing vacation from their first day of work.
            """;

        var request = new
        {
            model = "openai/gpt-4o-mini",
            messages = new object[]
            {
                new { role = "system", content = $"Answer questions using ONLY the following context:\n\n{context}" },
                new { role = "user", content = "How many vacation days do employees get?" }
            },
            max_tokens = 50
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue();

        using var doc = JsonDocument.Parse(responseBody);
        var message = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        message.Should().Contain("25");
    }

    [TestMethod]
    public async Task ChatCompletion_PolishContext_ShouldHandlePolish()
    {
        if (!_hasApiKey) { Assert.Inconclusive("No API key"); return; }

        var context = """
            DOKUMENT: Regulamin firmy
            Pracownicy otrzymują 26 dni urlopu wypoczynkowego rocznie.
            Urlop można wykorzystać w dowolnym terminie po uzgodnieniu z przełożonym.
            """;

        var request = new
        {
            model = "openai/gpt-4o-mini",
            messages = new object[]
            {
                new { role = "system", content = $"Odpowiadaj po polsku na podstawie kontekstu:\n\n{context}" },
                new { role = "user", content = "Ile dni urlopu przysługuje pracownikom?" }
            },
            max_tokens = 50
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue();

        using var doc = JsonDocument.Parse(responseBody);
        var message = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        message.Should().Contain("26");
    }

    #endregion

    #region Performance Tests

    [TestMethod]
    public async Task ChatCompletion_ResponseTime_ShouldBeReasonable()
    {
        if (!_hasApiKey) { Assert.Inconclusive("No API key"); return; }

        var request = new
        {
            model = "openai/gpt-4o-mini",
            messages = new[]
            {
                new { role = "user", content = "Say OK" }
            },
            max_tokens = 5
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await _httpClient.PostAsync("chat/completions", content);
        sw.Stop();

        response.IsSuccessStatusCode.Should().BeTrue();
        sw.ElapsedMilliseconds.Should().BeLessThan(10000, "Response should come within 10 seconds");
    }

    #endregion
}
