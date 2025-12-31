using Moq;
using Xunit;
using System.Net.Http;
using WPFLLM.Models;
using WPFLLM.Services;

namespace WPFLLM.Tests;

public class ModelDownloadServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly ModelDownloadService _service;

    public ModelDownloadServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());
        _service = new ModelDownloadService(_httpClientFactoryMock.Object);
    }

    [Fact]
    public async Task IsModelDownloadedAsync_WithUnknownModel_ReturnsFalse()
    {
        // Act
        var result = await _service.IsModelDownloadedAsync("unknown-model");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsModelDownloadedAsync_WithNoDownloadedFiles_ReturnsFalse()
    {
        // Act
        var result = await _service.IsModelDownloadedAsync("multilingual-e5-large");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetDownloadStatusAsync_WithUnknownModel_ReturnsNotDownloaded()
    {
        // Act
        var status = await _service.GetDownloadStatusAsync("unknown-model");

        // Assert
        Assert.Equal(ModelDownloadStatus.NotDownloaded, status);
    }

    [Fact]
    public async Task GetDownloadedSizeAsync_WithNoFiles_ReturnsZero()
    {
        // Act
        var size = await _service.GetDownloadedSizeAsync("multilingual-e5-large");

        // Assert
        Assert.Equal(0, size);
    }

    [Fact]
    public async Task DownloadModelAsync_WithUnknownModel_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DownloadModelAsync("unknown-model"));
    }

    [Fact]
    public async Task CancelDownloadAsync_WithNoActiveDownload_CompletesSuccessfully()
    {
        // Act & Assert - should not throw
        await _service.CancelDownloadAsync("multilingual-e5-large");
    }

    [Fact]
    public async Task DeleteModelAsync_WithNoFiles_CompletesSuccessfully()
    {
        // Act & Assert - should not throw
        await _service.DeleteModelAsync("multilingual-e5-large");
    }
}
