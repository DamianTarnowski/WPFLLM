using Xunit;
using WPFLLM.Models;

namespace WPFLLM.Tests;

public class EmbeddingModelsTests
{
    [Fact]
    public void Available_ContainsThreeModels()
    {
        // Assert
        Assert.Equal(3, EmbeddingModels.Available.Count);
    }

    [Theory]
    [InlineData("multilingual-e5-large")]
    [InlineData("multilingual-e5-base")]
    [InlineData("multilingual-e5-small")]
    public void Available_ContainsExpectedModels(string modelId)
    {
        // Assert
        Assert.True(EmbeddingModels.Available.ContainsKey(modelId));
    }

    [Fact]
    public void E5Large_HasCorrectDimensions()
    {
        // Arrange
        var model = EmbeddingModels.Available["multilingual-e5-large"];

        // Assert
        Assert.Equal(1024, model.Dimensions);
    }

    [Fact]
    public void E5Base_HasCorrectDimensions()
    {
        // Arrange
        var model = EmbeddingModels.Available["multilingual-e5-base"];

        // Assert
        Assert.Equal(768, model.Dimensions);
    }

    [Fact]
    public void E5Small_HasCorrectDimensions()
    {
        // Arrange
        var model = EmbeddingModels.Available["multilingual-e5-small"];

        // Assert
        Assert.Equal(384, model.Dimensions);
    }

    [Theory]
    [InlineData("multilingual-e5-large", 5)]
    [InlineData("multilingual-e5-base", 4)]
    [InlineData("multilingual-e5-small", 3)]
    public void Models_HaveCorrectQualityRating(string modelId, int expectedRating)
    {
        // Arrange
        var model = EmbeddingModels.Available[modelId];

        // Assert
        Assert.Equal(expectedRating, model.QualityRating);
    }

    [Fact]
    public void AllModels_HaveRequiredFields()
    {
        foreach (var (id, model) in EmbeddingModels.Available)
        {
            Assert.False(string.IsNullOrEmpty(model.Id), $"{id}: Id is empty");
            Assert.False(string.IsNullOrEmpty(model.DisplayName), $"{id}: DisplayName is empty");
            Assert.False(string.IsNullOrEmpty(model.Description), $"{id}: Description is empty");
            Assert.True(model.Dimensions > 0, $"{id}: Dimensions should be > 0");
            Assert.True(model.SizeBytes > 0, $"{id}: SizeBytes should be > 0");
            Assert.True(model.RequiredFiles.Length > 0, $"{id}: RequiredFiles is empty");
            Assert.False(string.IsNullOrEmpty(model.HuggingFaceRepo), $"{id}: HuggingFaceRepo is empty");
            Assert.True(model.Languages.Length > 0, $"{id}: Languages is empty");
            Assert.True(model.QualityRating >= 1 && model.QualityRating <= 5, $"{id}: QualityRating should be 1-5");
            Assert.False(string.IsNullOrEmpty(model.RamRequired), $"{id}: RamRequired is empty");
            Assert.False(string.IsNullOrEmpty(model.InferenceSpeed), $"{id}: InferenceSpeed is empty");
            Assert.False(string.IsNullOrEmpty(model.RecommendedFor), $"{id}: RecommendedFor is empty");
        }
    }

    [Fact]
    public void AllModels_SupportPolish()
    {
        foreach (var (id, model) in EmbeddingModels.Available)
        {
            Assert.Contains("pl", model.Languages);
        }
    }

    [Fact]
    public void GetModelsPath_ReturnsValidPath()
    {
        // Act
        var path = EmbeddingModels.GetModelsPath();

        // Assert
        Assert.False(string.IsNullOrEmpty(path));
        Assert.Contains("WPFLLM", path);
        Assert.Contains("models", path);
    }

    [Fact]
    public void GetModelPath_ReturnsPathWithModelId()
    {
        // Act
        var path = EmbeddingModels.GetModelPath("multilingual-e5-large");

        // Assert
        Assert.Contains("multilingual-e5-large", path);
    }

    [Fact]
    public void Models_AreSortedByQualityDescending()
    {
        // Arrange
        var models = EmbeddingModels.Available.Values.ToList();

        // Assert - Large should have highest quality
        var large = models.First(m => m.Id == "multilingual-e5-large");
        var small = models.First(m => m.Id == "multilingual-e5-small");
        
        Assert.True(large.QualityRating > small.QualityRating);
    }

    [Fact]
    public void Models_SizeCorrelatesWithQuality()
    {
        // Arrange
        var large = EmbeddingModels.Available["multilingual-e5-large"];
        var baseModel = EmbeddingModels.Available["multilingual-e5-base"];
        var small = EmbeddingModels.Available["multilingual-e5-small"];

        // Assert - Larger models should have more bytes
        Assert.True(large.SizeBytes > baseModel.SizeBytes);
        Assert.True(baseModel.SizeBytes > small.SizeBytes);
    }
}
