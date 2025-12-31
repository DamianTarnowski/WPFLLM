using Moq;
using Xunit;
using WPFLLM.Models;
using WPFLLM.Services;

namespace WPFLLM.Tests;

public class LocalEmbeddingServiceTests
{
    [Fact]
    public void GetDimensions_WithoutInitialization_ReturnsZero()
    {
        // Arrange
        var service = new LocalEmbeddingService();

        // Act
        var dimensions = service.GetDimensions();

        // Assert
        Assert.Equal(0, dimensions);
    }

    [Fact]
    public async Task IsAvailableAsync_WithoutInitialization_ReturnsFalse()
    {
        // Arrange
        var service = new LocalEmbeddingService();

        // Act
        var isAvailable = await service.IsAvailableAsync();

        // Assert
        Assert.False(isAvailable);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithoutInitialization_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new LocalEmbeddingService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetEmbeddingAsync("test text"));
    }

    [Fact]
    public async Task InitializeAsync_WithUnknownModel_ThrowsArgumentException()
    {
        // Arrange
        var service = new LocalEmbeddingService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.InitializeAsync("unknown-model-id"));
    }

    [Fact]
    public async Task InitializeAsync_WithMissingModelFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var service = new LocalEmbeddingService();

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => service.InitializeAsync("multilingual-e5-large"));
    }

    [Fact]
    public void Dispose_ClearsState()
    {
        // Arrange
        var service = new LocalEmbeddingService();

        // Act
        service.Dispose();

        // Assert
        Assert.Equal(0, service.GetDimensions());
    }
}
