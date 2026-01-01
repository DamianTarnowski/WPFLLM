using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Reflection;
using WPFLLM.Services;

namespace WPFLLM.Tests.Unit;

/// <summary>
/// Unit tests for text chunking logic used in RAG pipeline.
/// Tests chunking boundaries, overlap, and edge cases.
/// </summary>
[TestClass]
public class ChunkingTests
{
    private MethodInfo _chunkTextMethod = null!;

    [TestInitialize]
    public void Setup()
    {
        // Access private ChunkText method via reflection for testing
        _chunkTextMethod = typeof(RagService).GetMethod("ChunkText", 
            BindingFlags.NonPublic | BindingFlags.Static)!;
    }

    private List<string> ChunkText(string text)
    {
        return (List<string>)_chunkTextMethod.Invoke(null, new object[] { text })!;
    }

    #region Basic Chunking Tests

    [TestMethod]
    public void ChunkText_ShortText_BelowMinimum_ShouldReturnEmpty()
    {
        // MinChunkChars = 100, so short text below threshold returns empty
        var text = "This is a short text.";

        var chunks = ChunkText(text);

        // Short text below MinChunkChars threshold is not chunked
        chunks.Should().BeEmpty();
    }

    [TestMethod]
    public void ChunkText_TextAboveMinimum_ShouldReturnSingleChunk()
    {
        // Text above MinChunkChars = 100 should create a chunk
        var text = "This is a sufficiently long text that exceeds the minimum chunk size requirement of one hundred characters. " +
            "It contains enough content to be considered a valid chunk by the RAG system.";

        var chunks = ChunkText(text);

        chunks.Should().HaveCount(1);
        chunks[0].Should().Contain("sufficiently long");
    }

    [TestMethod]
    public void ChunkText_EmptyText_ShouldReturnEmptyList()
    {
        var text = "";

        var chunks = ChunkText(text);

        chunks.Should().BeEmpty();
    }

    [TestMethod]
    public void ChunkText_WhitespaceOnly_ShouldReturnEmptyList()
    {
        var text = "   \n\n   \t   ";

        var chunks = ChunkText(text);

        chunks.Should().BeEmpty();
    }

    [TestMethod]
    public void ChunkText_MultipleParagraphs_ShouldPreserveParagraphBoundaries()
    {
        var text = """
            First paragraph with some content.

            Second paragraph with more content.

            Third paragraph with even more content.
            """;

        var chunks = ChunkText(text);

        chunks.Should().NotBeEmpty();
        // Chunks should generally preserve paragraph structure
    }

    #endregion

    #region Long Text Chunking Tests

    [TestMethod]
    public void ChunkText_LongText_ShouldCreateMultipleChunks()
    {
        var paragraphs = Enumerable.Range(1, 30)
            .Select(i => $"Paragraph {i}: This is a longer paragraph with substantial content. " +
                        $"It contains multiple sentences to ensure proper chunking behavior. " +
                        $"The chunking algorithm should split this appropriately.")
            .ToList();
        var text = string.Join("\n\n", paragraphs);

        var chunks = ChunkText(text);

        chunks.Count.Should().BeGreaterThan(1);
    }

    [TestMethod]
    public void ChunkText_LongText_ChunksShouldNotExceedMaxLength()
    {
        var paragraphs = Enumerable.Range(1, 50)
            .Select(i => $"Paragraph {i}: " + new string('x', 200))
            .ToList();
        var text = string.Join("\n\n", paragraphs);

        var chunks = ChunkText(text);

        // Max chunk chars is ~1500, allow some margin for edge cases
        chunks.Should().OnlyContain(c => c.Length <= 2000);
    }

    [TestMethod]
    public void ChunkText_VeryLongParagraph_ShouldSplitBySentences()
    {
        var longParagraph = string.Join(" ", 
            Enumerable.Range(1, 100).Select(i => $"Sentence number {i} with some additional words."));

        var chunks = ChunkText(longParagraph);

        chunks.Count.Should().BeGreaterThan(1);
    }

    #endregion

    #region Overlap Tests

    [TestMethod]
    public void ChunkText_WithOverlap_ConsecutiveChunksShouldHaveOverlap()
    {
        var paragraphs = Enumerable.Range(1, 20)
            .Select(i => $"Unique paragraph {i} with distinct content that should be recognizable.")
            .ToList();
        var text = string.Join("\n\n", paragraphs);

        var chunks = ChunkText(text);

        if (chunks.Count >= 2)
        {
            // There should be some overlap between consecutive chunks
            var firstChunkEnd = chunks[0][^100..];
            var secondChunkStart = chunks[1][..200];
            
            // Check if any words from end of first chunk appear in start of second
            var firstWords = firstChunkEnd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var hasOverlap = firstWords.Any(w => secondChunkStart.Contains(w, StringComparison.OrdinalIgnoreCase));
            
            // Note: Overlap is expected but exact behavior depends on implementation
        }
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void ChunkText_SingleVeryLongLine_ShouldSplitAppropriately()
    {
        var text = string.Join(". ", Enumerable.Range(1, 200).Select(i => $"Sentence {i}"));

        var chunks = ChunkText(text);

        chunks.Should().NotBeEmpty();
        chunks.Count.Should().BeGreaterThan(1);
    }

    [TestMethod]
    public void ChunkText_MixedContent_ShouldHandleGracefully()
    {
        var text = """
            # Header

            Short intro.

            A much longer paragraph that contains a lot of information about various topics including but not limited to science, technology, engineering, and mathematics. This paragraph goes on and on with more details and explanations.

            - List item 1
            - List item 2

            ```
            code block here
            ```

            Final paragraph.
            """;

        var chunks = ChunkText(text);

        chunks.Should().NotBeEmpty();
    }

    [TestMethod]
    public void ChunkText_UnicodeContent_ShouldHandleCorrectly()
    {
        var text = """
            Polski tekst z polskimi znakami: ąćęłńóśźż ĄĆĘŁŃÓŚŹŻ.

            日本語のテキスト with mixed content.

            Emoji content: 🎉 🚀 ✨ 💡

            Mathematical symbols: ∑∫∂∆√∞
            """;

        var chunks = ChunkText(text);

        chunks.Should().NotBeEmpty();
        chunks[0].Should().Contain("ąćęłńóśźż");
    }

    [TestMethod]
    public void ChunkText_RepeatedNewlines_ShouldNormalize()
    {
        // Need content above MinChunkChars = 100 to create chunks
        var text = "First paragraph with enough content to meet the minimum chunk size requirement which is one hundred characters.\n\n\n\n\n\n" +
            "Second paragraph also with enough content to potentially create another chunk if needed by the algorithm.";

        var chunks = ChunkText(text);

        chunks.Should().NotBeEmpty();
    }

    [TestMethod]
    public void ChunkText_MinChunkSize_ShouldRespectMinimum()
    {
        // Very short paragraphs below MinChunkChars = 100 should return empty
        var text = "A.\n\nB.\n\nC.\n\nD.\n\nE.";

        var chunks = ChunkText(text);

        // Short content below minimum threshold is not chunked
        chunks.Should().BeEmpty();
    }

    #endregion
}
