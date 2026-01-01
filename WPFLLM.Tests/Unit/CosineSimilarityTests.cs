using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Reflection;
using WPFLLM.Services;

namespace WPFLLM.Tests.Unit;

/// <summary>
/// Unit tests for cosine similarity calculations used in semantic search.
/// </summary>
[TestClass]
public class CosineSimilarityTests
{
    private MethodInfo _cosineSimilarityMethod = null!;

    [TestInitialize]
    public void Setup()
    {
        // Access private CosineSimilarity method from RagService
        _cosineSimilarityMethod = typeof(RagService).GetMethod("CosineSimilarity",
            BindingFlags.NonPublic | BindingFlags.Static)!;
    }

    private double CosineSimilarity(float[] a, float[] b)
    {
        return (double)_cosineSimilarityMethod.Invoke(null, new object[] { a, b })!;
    }

    #region Basic Similarity Tests

    [TestMethod]
    public void CosineSimilarity_IdenticalVectors_ShouldReturnOne()
    {
        var a = new float[] { 1.0f, 2.0f, 3.0f };
        var b = new float[] { 1.0f, 2.0f, 3.0f };

        var similarity = CosineSimilarity(a, b);

        similarity.Should().BeApproximately(1.0, 0.0001);
    }

    [TestMethod]
    public void CosineSimilarity_OrthogonalVectors_ShouldReturnZero()
    {
        var a = new float[] { 1.0f, 0.0f, 0.0f };
        var b = new float[] { 0.0f, 1.0f, 0.0f };

        var similarity = CosineSimilarity(a, b);

        similarity.Should().BeApproximately(0.0, 0.0001);
    }

    [TestMethod]
    public void CosineSimilarity_OppositeVectors_ShouldReturnNegativeOne()
    {
        var a = new float[] { 1.0f, 0.0f, 0.0f };
        var b = new float[] { -1.0f, 0.0f, 0.0f };

        var similarity = CosineSimilarity(a, b);

        similarity.Should().BeApproximately(-1.0, 0.0001);
    }

    [TestMethod]
    public void CosineSimilarity_ScaledVectors_ShouldReturnSameAsOriginal()
    {
        var a = new float[] { 1.0f, 2.0f, 3.0f };
        var b = new float[] { 2.0f, 4.0f, 6.0f }; // a * 2

        var similarity = CosineSimilarity(a, b);

        similarity.Should().BeApproximately(1.0, 0.0001);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void CosineSimilarity_DifferentLengthVectors_ShouldReturnZero()
    {
        var a = new float[] { 1.0f, 2.0f };
        var b = new float[] { 1.0f, 2.0f, 3.0f };

        var similarity = CosineSimilarity(a, b);

        similarity.Should().Be(0);
    }

    [TestMethod]
    public void CosineSimilarity_ZeroVector_ShouldReturnZero()
    {
        var a = new float[] { 0.0f, 0.0f, 0.0f };
        var b = new float[] { 1.0f, 2.0f, 3.0f };

        var similarity = CosineSimilarity(a, b);

        similarity.Should().Be(0);
    }

    [TestMethod]
    public void CosineSimilarity_EmptyVectors_ShouldReturnZero()
    {
        var a = Array.Empty<float>();
        var b = Array.Empty<float>();

        var similarity = CosineSimilarity(a, b);

        similarity.Should().Be(0);
    }

    #endregion

    #region Realistic Embedding Tests

    [TestMethod]
    public void CosineSimilarity_SimilarEmbeddings_ShouldReturnHighScore()
    {
        // Simulated normalized embeddings that are similar
        var a = new float[] { 0.5f, 0.5f, 0.5f, 0.5f };
        var b = new float[] { 0.51f, 0.49f, 0.52f, 0.48f };

        var similarity = CosineSimilarity(a, b);

        similarity.Should().BeGreaterThan(0.95);
    }

    [TestMethod]
    public void CosineSimilarity_DissimilarEmbeddings_ShouldReturnLowScore()
    {
        var a = new float[] { 0.9f, 0.1f, 0.0f, 0.0f };
        var b = new float[] { 0.0f, 0.0f, 0.1f, 0.9f };

        var similarity = CosineSimilarity(a, b);

        similarity.Should().BeLessThan(0.3);
    }

    [TestMethod]
    public void CosineSimilarity_LargeVectors_ShouldComputeCorrectly()
    {
        var random = new Random(42);
        var a = Enumerable.Range(0, 1024).Select(_ => (float)random.NextDouble()).ToArray();
        var b = Enumerable.Range(0, 1024).Select(_ => (float)random.NextDouble()).ToArray();

        var similarity = CosineSimilarity(a, b);

        similarity.Should().BeInRange(-1.0, 1.0);
    }

    [TestMethod]
    public void CosineSimilarity_NormalizedVectors_ShouldWorkCorrectly()
    {
        // Pre-normalized vectors (norm = 1)
        var a = new float[] { 0.5773503f, 0.5773503f, 0.5773503f };
        var b = new float[] { 0.5773503f, 0.5773503f, 0.5773503f };

        var similarity = CosineSimilarity(a, b);

        similarity.Should().BeApproximately(1.0, 0.001);
    }

    #endregion

    #region Numerical Stability Tests

    [TestMethod]
    public void CosineSimilarity_VerySmallValues_ShouldNotOverflow()
    {
        var a = new float[] { 1e-20f, 1e-20f, 1e-20f };
        var b = new float[] { 1e-20f, 1e-20f, 1e-20f };

        var similarity = CosineSimilarity(a, b);

        similarity.Should().BeInRange(-1.0, 1.0);
    }

    [TestMethod]
    public void CosineSimilarity_VeryLargeValues_ShouldNotOverflow()
    {
        var a = new float[] { 1e10f, 1e10f, 1e10f };
        var b = new float[] { 1e10f, 1e10f, 1e10f };

        var similarity = CosineSimilarity(a, b);

        similarity.Should().BeApproximately(1.0, 0.0001);
    }

    #endregion
}
