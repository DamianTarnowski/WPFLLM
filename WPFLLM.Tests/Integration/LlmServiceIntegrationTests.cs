using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using NSubstitute;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using WPFLLM.Models;
using WPFLLM.Services;

namespace WPFLLM.Tests.Integration;

/// <summary>
/// Integration tests for LlmService testing HTTP communication, streaming, and embeddings.
/// Uses mock HTTP handlers to simulate API responses.
/// </summary>
[TestClass]
public class LlmServiceIntegrationTests
{
    private ISettingsService _settingsService = null!;
    private ILocalEmbeddingService _localEmbeddingService = null!;
    private IRateLimiter _rateLimiter = null!;
    private ILoggingService _loggingService = null!;

    [TestInitialize]
    public void Setup()
    {
        _settingsService = Substitute.For<ISettingsService>();
        _localEmbeddingService = Substitute.For<ILocalEmbeddingService>();
        _rateLimiter = Substitute.For<IRateLimiter>();
        _loggingService = Substitute.For<ILoggingService>();

        _settingsService.GetSettingsAsync().Returns(new AppSettings
        {
            ApiKey = "test-api-key",
            ApiEndpoint = "https://api.test.com/v1",
            Model = "gpt-4",
            Temperature = 0.7,
            MaxTokens = 1000,
            SystemPrompt = "You are a helpful assistant.",
            UseLocalEmbeddings = false
        });

        _localEmbeddingService.IsAvailableAsync().Returns(false);
    }

    #region Configuration Tests

    [TestMethod]
    public async Task StreamChatAsync_NoApiKey_ShouldReturnError()
    {
        _settingsService.GetSettingsAsync().Returns(new AppSettings { ApiKey = "" });

        var httpClientFactory = CreateMockHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK));
        var service = new LlmService(httpClientFactory, _settingsService, _localEmbeddingService, _rateLimiter, _loggingService);

        var messages = new List<ChatMessage> { new() { Role = "user", Content = "Hello" } };
        var chunks = new List<string>();

        await foreach (var chunk in service.StreamChatAsync(messages))
        {
            chunks.Add(chunk);
        }

        chunks.Should().ContainSingle();
        chunks[0].Should().Contain("Error");
        chunks[0].Should().Contain("API Key");
    }

    [TestMethod]
    public async Task StreamChatAsync_WithRagContext_ShouldIncludeInSystemPrompt()
    {
        string? capturedRequest = null;
        var handler = new MockHttpMessageHandler((request, ct) =>
        {
            capturedRequest = request.Content?.ReadAsStringAsync().Result;
            return Task.FromResult(CreateStreamingResponse(new[] { "Hello" }));
        });

        var httpClientFactory = CreateMockHttpClientFactory(handler);
        var service = new LlmService(httpClientFactory, _settingsService, _localEmbeddingService, _rateLimiter, _loggingService);

        var messages = new List<ChatMessage> { new() { Role = "user", Content = "Question" } };
        var ragContext = "Relevant context from documents";

        await foreach (var _ in service.StreamChatAsync(messages, ragContext))
        {
        }

        capturedRequest.Should().Contain("Relevant context from documents");
    }

    #endregion

    #region Streaming Tests

    [TestMethod]
    public async Task StreamChatAsync_ValidResponse_ShouldStreamChunks()
    {
        var expectedChunks = new[] { "Hello", " ", "World", "!" };
        var handler = new MockHttpMessageHandler((request, ct) =>
        {
            return Task.FromResult(CreateStreamingResponse(expectedChunks));
        });

        var httpClientFactory = CreateMockHttpClientFactory(handler);
        var service = new LlmService(httpClientFactory, _settingsService, _localEmbeddingService, _rateLimiter, _loggingService);

        var messages = new List<ChatMessage> { new() { Role = "user", Content = "Hi" } };
        var receivedChunks = new List<string>();

        await foreach (var chunk in service.StreamChatAsync(messages))
        {
            receivedChunks.Add(chunk);
        }

        receivedChunks.Should().BeEquivalentTo(expectedChunks);
    }

    [TestMethod]
    public async Task StreamChatAsync_HttpError_ShouldReturnErrorMessage()
    {
        var handler = new MockHttpMessageHandler((request, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Server Error")
            });
        });

        var httpClientFactory = CreateMockHttpClientFactory(handler);
        var service = new LlmService(httpClientFactory, _settingsService, _localEmbeddingService, _rateLimiter, _loggingService);

        var messages = new List<ChatMessage> { new() { Role = "user", Content = "Hi" } };
        var chunks = new List<string>();

        await foreach (var chunk in service.StreamChatAsync(messages))
        {
            chunks.Add(chunk);
        }

        chunks.Should().ContainSingle();
        chunks[0].Should().Contain("Error");
        // Error message contains either status code or status name
        (chunks[0].Contains("500") || chunks[0].Contains("InternalServerError")).Should().BeTrue();
    }

    [TestMethod]
    public async Task StreamChatAsync_NetworkException_ShouldReturnErrorMessage()
    {
        var handler = new MockHttpMessageHandler((request, ct) =>
        {
            throw new HttpRequestException("Network error");
        });

        var httpClientFactory = CreateMockHttpClientFactory(handler);
        var service = new LlmService(httpClientFactory, _settingsService, _localEmbeddingService, _rateLimiter, _loggingService);

        var messages = new List<ChatMessage> { new() { Role = "user", Content = "Hi" } };
        var chunks = new List<string>();

        await foreach (var chunk in service.StreamChatAsync(messages))
        {
            chunks.Add(chunk);
        }

        chunks.Should().ContainSingle();
        chunks[0].Should().Contain("Error");
    }

    [TestMethod]
    public async Task StreamChatAsync_Cancellation_ShouldStopStreaming()
    {
        var cts = new CancellationTokenSource();
        var chunkCount = 0;

        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            var chunks = Enumerable.Range(0, 100).Select(i => $"Chunk{i}").ToArray();
            return CreateStreamingResponse(chunks);
        });

        var httpClientFactory = CreateMockHttpClientFactory(handler);
        var service = new LlmService(httpClientFactory, _settingsService, _localEmbeddingService, _rateLimiter, _loggingService);

        var messages = new List<ChatMessage> { new() { Role = "user", Content = "Hi" } };

        try
        {
            await foreach (var chunk in service.StreamChatAsync(messages, null, cts.Token))
            {
                chunkCount++;
                if (chunkCount >= 3) cts.Cancel();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        chunkCount.Should().BeLessThan(100);
    }

    #endregion

    #region Embedding Tests

    [TestMethod]
    public async Task GetEmbeddingAsync_LocalEnabled_ShouldUseLocalService()
    {
        _settingsService.GetSettingsAsync().Returns(new AppSettings
        {
            ApiKey = "test",
            UseLocalEmbeddings = true,
            LocalEmbeddingModel = "test-model"
        });

        _localEmbeddingService.IsAvailableAsync().Returns(true);
        _localEmbeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.1f, 0.2f, 0.3f });

        var httpClientFactory = CreateMockHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK));
        var service = new LlmService(httpClientFactory, _settingsService, _localEmbeddingService, _rateLimiter, _loggingService);

        var embedding = await service.GetEmbeddingAsync("test text");

        embedding.Should().BeEquivalentTo(new float[] { 0.1f, 0.2f, 0.3f });
        await _localEmbeddingService.Received(1).GetEmbeddingAsync("test text", true, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetEmbeddingAsync_LocalNotAvailable_ShouldInitialize()
    {
        _settingsService.GetSettingsAsync().Returns(new AppSettings
        {
            ApiKey = "test",
            UseLocalEmbeddings = true,
            LocalEmbeddingModel = "test-model"
        });

        _localEmbeddingService.IsAvailableAsync().Returns(false);
        _localEmbeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.5f });

        var httpClientFactory = CreateMockHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK));
        var service = new LlmService(httpClientFactory, _settingsService, _localEmbeddingService, _rateLimiter, _loggingService);

        await service.GetEmbeddingAsync("test");

        await _localEmbeddingService.Received(1).InitializeAsync("test-model");
    }

    [TestMethod]
    public async Task GetEmbeddingAsync_LocalFails_ShouldFallbackToApi()
    {
        _settingsService.GetSettingsAsync().Returns(new AppSettings
        {
            ApiKey = "test-key",
            ApiEndpoint = "https://api.test.com/v1",
            UseLocalEmbeddings = true
        });

        _localEmbeddingService.IsAvailableAsync().Returns(true);
        _localEmbeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns<float[]>(x => throw new Exception("Local embedding failed"));

        var embeddingResponse = new
        {
            data = new[] { new { embedding = new[] { 0.1, 0.2, 0.3 } } }
        };

        var handler = new MockHttpMessageHandler((request, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(embeddingResponse))
            });
        });

        var httpClientFactory = CreateMockHttpClientFactory(handler);
        var service = new LlmService(httpClientFactory, _settingsService, _localEmbeddingService, _rateLimiter, _loggingService);

        var embedding = await service.GetEmbeddingAsync("test");

        embedding.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task GetEmbeddingAsync_NoApiKey_ShouldReturnEmpty()
    {
        _settingsService.GetSettingsAsync().Returns(new AppSettings
        {
            ApiKey = "",
            UseLocalEmbeddings = false
        });

        var httpClientFactory = CreateMockHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK));
        var service = new LlmService(httpClientFactory, _settingsService, _localEmbeddingService, _rateLimiter, _loggingService);

        var embedding = await service.GetEmbeddingAsync("test");

        embedding.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetEmbeddingAsync_ApiError_ShouldReturnEmpty()
    {
        var handler = new MockHttpMessageHandler((request, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        });

        var httpClientFactory = CreateMockHttpClientFactory(handler);
        var service = new LlmService(httpClientFactory, _settingsService, _localEmbeddingService, _rateLimiter, _loggingService);

        var embedding = await service.GetEmbeddingAsync("test");

        embedding.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetEmbeddingAsync_IsQuery_ShouldPassToLocalService()
    {
        _settingsService.GetSettingsAsync().Returns(new AppSettings
        {
            UseLocalEmbeddings = true
        });

        _localEmbeddingService.IsAvailableAsync().Returns(true);
        _localEmbeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.1f });

        var httpClientFactory = CreateMockHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK));
        var service = new LlmService(httpClientFactory, _settingsService, _localEmbeddingService, _rateLimiter, _loggingService);

        await service.GetEmbeddingAsync("test", isQuery: false, CancellationToken.None);

        await _localEmbeddingService.Received(1).GetEmbeddingAsync("test", false, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private static IHttpClientFactory CreateMockHttpClientFactory(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler((request, ct) => Task.FromResult(response));
        return CreateMockHttpClientFactory(handler);
    }

    private static IHttpClientFactory CreateMockHttpClientFactory(MockHttpMessageHandler handler)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));
        return factory;
    }

    private static HttpResponseMessage CreateStreamingResponse(string[] chunks)
    {
        var sb = new StringBuilder();
        foreach (var chunk in chunks)
        {
            var data = new
            {
                choices = new[]
                {
                    new { delta = new { content = chunk } }
                }
            };
            sb.AppendLine($"data: {JsonSerializer.Serialize(data)}");
        }
        sb.AppendLine("data: [DONE]");

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sb.ToString())
        };
    }

    #endregion
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handler(request, cancellationToken);
    }
}
