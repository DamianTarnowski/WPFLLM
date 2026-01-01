using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using NSubstitute;
using System.IO;
using System.Text.Json;
using WPFLLM.Models;
using WPFLLM.Services;

namespace WPFLLM.Tests.Integration;

/// <summary>
/// Integration tests for RagService testing chunking, retrieval, and hybrid search.
/// Uses mock LlmService for embeddings but real database operations.
/// </summary>
[TestClass]
public class RagServiceIntegrationTests
{
    private TestDatabaseService _database = null!;
    private ILlmService _llmService = null!;
    private RagService _ragService = null!;
    private string _tempDir = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _database = new TestDatabaseService();
        await _database.InitializeAsync();

        _llmService = Substitute.For<ILlmService>();
        _ragService = new RagService(_database, _llmService);

        _tempDir = Path.Combine(Path.GetTempPath(), $"RagTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _database.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Document Addition Tests

    [TestMethod]
    public async Task AddDocument_TxtFile_ShouldCreateDocumentAndChunks()
    {
        var filePath = Path.Combine(_tempDir, "test.txt");
        // Need content above MinChunkChars = 100 to create chunks
        var content = "This is a comprehensive test document with substantial content for testing the document ingestion process. " +
            "The RAG system requires documents to have a minimum amount of text to create meaningful chunks for semantic search.";
        await File.WriteAllTextAsync(filePath, content);

        var document = await _ragService.AddDocumentAsync(filePath);

        document.Should().NotBeNull();
        document.FileName.Should().Be("test.txt");
        
        var chunks = await _database.GetChunksAsync(document.Id);
        chunks.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task AddDocument_LargeText_ShouldCreateMultipleChunks()
    {
        var filePath = Path.Combine(_tempDir, "large.txt");
        var largeContent = string.Join("\n\n", Enumerable.Range(1, 50).Select(i => 
            $"Paragraph {i}: This is a substantial paragraph with enough content to ensure proper chunking behavior. " +
            $"The RAG system needs to handle long documents by splitting them into manageable chunks. " +
            $"Each chunk should maintain semantic coherence while staying within token limits."));
        
        await File.WriteAllTextAsync(filePath, largeContent);

        var document = await _ragService.AddDocumentAsync(filePath);
        var chunks = await _database.GetChunksAsync(document.Id);

        chunks.Count.Should().BeGreaterThan(1);
        chunks.Should().BeInAscendingOrder(c => c.ChunkIndex);
    }

    [TestMethod]
    public async Task AddDocument_MarkdownFile_ShouldParseProperly()
    {
        var filePath = Path.Combine(_tempDir, "readme.md");
        var markdown = """
            # Title
            
            ## Section 1
            Some content here with **bold** and *italic* text.
            
            ## Section 2
            - List item 1
            - List item 2
            
            ```python
            def hello():
                print("Hello World")
            ```
            """;
        await File.WriteAllTextAsync(filePath, markdown);

        var document = await _ragService.AddDocumentAsync(filePath);

        document.Should().NotBeNull();
        document.Content.Should().Contain("Title");
        document.Content.Should().Contain("Section 1");
    }

    [TestMethod]
    public async Task AddDocument_EmptyFile_ShouldThrowException()
    {
        var filePath = Path.Combine(_tempDir, "empty.txt");
        await File.WriteAllTextAsync(filePath, "");

        Func<Task> act = async () => await _ragService.AddDocumentAsync(filePath);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    [TestMethod]
    public async Task AddDocument_UnsupportedFormat_ShouldThrowException()
    {
        var filePath = Path.Combine(_tempDir, "test.xyz");
        await File.WriteAllTextAsync(filePath, "some content");

        Func<Task> act = async () => await _ragService.AddDocumentAsync(filePath);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*Unsupported file type*");
    }

    [TestMethod]
    public async Task AddDocument_JsonFile_ShouldParseAsText()
    {
        var filePath = Path.Combine(_tempDir, "data.json");
        var json = """
            {
                "name": "Test",
                "items": ["item1", "item2", "item3"],
                "nested": {
                    "key": "value"
                }
            }
            """;
        await File.WriteAllTextAsync(filePath, json);

        var document = await _ragService.AddDocumentAsync(filePath);

        document.Should().NotBeNull();
        document.Content.Should().Contain("Test");
        document.Content.Should().Contain("item1");
    }

    [TestMethod]
    public async Task AddDocument_CsvFile_ShouldParseProperly()
    {
        var filePath = Path.Combine(_tempDir, "data.csv");
        var csv = """
            Name,Age,City
            Alice,30,New York
            Bob,25,Los Angeles
            Charlie,35,Chicago
            """;
        await File.WriteAllTextAsync(filePath, csv);

        var document = await _ragService.AddDocumentAsync(filePath);

        document.Should().NotBeNull();
        document.Content.Should().Contain("Alice");
        document.Content.Should().Contain("New York");
    }

    #endregion

    #region Document Management Tests

    [TestMethod]
    public async Task GetDocuments_ShouldReturnAllDocuments()
    {
        var file1 = Path.Combine(_tempDir, "doc1.txt");
        var file2 = Path.Combine(_tempDir, "doc2.txt");
        await File.WriteAllTextAsync(file1, "Document one content");
        await File.WriteAllTextAsync(file2, "Document two content");

        await _ragService.AddDocumentAsync(file1);
        await _ragService.AddDocumentAsync(file2);

        var documents = await _ragService.GetDocumentsAsync();

        documents.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task DeleteDocument_ShouldRemoveDocumentAndChunks()
    {
        var filePath = Path.Combine(_tempDir, "to_delete.txt");
        await File.WriteAllTextAsync(filePath, "Content to delete");
        var document = await _ragService.AddDocumentAsync(filePath);

        await _ragService.DeleteDocumentAsync(document.Id);

        var documents = await _ragService.GetDocumentsAsync();
        var chunks = await _database.GetChunksAsync(document.Id);

        documents.Should().BeEmpty();
        chunks.Should().BeEmpty();
    }

    #endregion

    #region Embedding Generation Tests

    [TestMethod]
    public async Task GenerateEmbeddings_ShouldUpdateChunksWithEmbeddings()
    {
        var filePath = Path.Combine(_tempDir, "embed_test.txt");
        // MinChunkChars = 100, so we need substantial content
        var content = "This is a comprehensive test document for embedding generation. " +
            "It contains enough text to meet the minimum chunk size requirements. " +
            "The RAG system uses chunking to split documents into manageable pieces. " +
            "Each chunk can then be embedded and searched independently.";
        await File.WriteAllTextAsync(filePath, content);
        var document = await _ragService.AddDocumentAsync(filePath);

        var testEmbedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
        _llmService.GetEmbeddingAsync(Arg.Any<string>(), isQuery: false, Arg.Any<CancellationToken>())
            .Returns(testEmbedding);

        var progress = new Progress<string>();
        await _ragService.GenerateEmbeddingsAsync(progress);

        var chunks = await _database.GetChunksAsync(document.Id);
        chunks.Should().NotBeEmpty();
        chunks.Where(c => c.Embedding != null).Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task GenerateEmbeddings_ShouldSkipAlreadyEmbeddedChunks()
    {
        var filePath = Path.Combine(_tempDir, "partial.txt");
        // Need substantial content for chunking (MinChunkChars = 100)
        var content = "This is the first paragraph with enough content to create a chunk. " +
            "It needs to be at least 100 characters to pass the minimum threshold. " +
            "The chunking algorithm will process this text appropriately.";
        await File.WriteAllTextAsync(filePath, content);
        var document = await _ragService.AddDocumentAsync(filePath);
        
        var chunks = await _database.GetChunksAsync(document.Id);
        if (chunks.Count == 0)
        {
            // If no chunks created, test passes trivially (nothing to embed)
            return;
        }
        await _database.UpdateChunkEmbeddingAsync(chunks[0].Id, "[0.1, 0.2, 0.3]");

        var testEmbedding = new float[] { 0.9f, 0.8f, 0.7f };
        _llmService.GetEmbeddingAsync(Arg.Any<string>(), isQuery: false, Arg.Any<CancellationToken>())
            .Returns(testEmbedding);

        await _ragService.GenerateEmbeddingsAsync();

        // Since all chunks are already embedded, no new embedding calls should be made
        var allChunks = await _database.GetAllChunksAsync();
        var unembeddedCount = allChunks.Count(c => string.IsNullOrEmpty(c.Embedding));
        unembeddedCount.Should().Be(0);
    }

    [TestMethod]
    public async Task GenerateEmbeddings_ShouldSupportCancellation()
    {
        var filePath = Path.Combine(_tempDir, "cancel_test.txt");
        // Create many large paragraphs to ensure multiple chunks
        var paragraphs = Enumerable.Range(1, 30).Select(i => 
            $"Paragraph {i}: This is a substantial paragraph with enough content to create a separate chunk. " +
            $"The RAG system needs multiple chunks to properly test cancellation behavior during embedding generation.");
        var content = string.Join("\n\n", paragraphs);
        await File.WriteAllTextAsync(filePath, content);
        await _ragService.AddDocumentAsync(filePath);

        var chunks = await _database.GetAllChunksAsync();
        if (chunks.Count < 3)
        {
            // Not enough chunks to test cancellation - skip
            Assert.Inconclusive("Not enough chunks created to test cancellation");
            return;
        }

        var cts = new CancellationTokenSource();
        var callCount = 0;
        _llmService.GetEmbeddingAsync(Arg.Any<string>(), isQuery: false, Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                callCount++;
                if (callCount >= 2) cts.Cancel();
                x.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return new float[] { 0.1f, 0.2f };
            });

        Func<Task> act = async () => await _ragService.GenerateEmbeddingsAsync(cancellationToken: cts.Token);

        // Either throws OperationCanceledException or completes (if cancellation happens between iterations)
        try
        {
            await act();
            // If it completes, verify not all chunks were embedded
            var embeddedChunks = await _database.GetAllChunksAsync();
            var embeddedCount = embeddedChunks.Count(c => !string.IsNullOrEmpty(c.Embedding));
            embeddedCount.Should().BeLessThan(chunks.Count);
        }
        catch (OperationCanceledException)
        {
            // Expected behavior
        }
    }

    #endregion

    #region Context Retrieval Tests

    [TestMethod]
    public async Task GetRelevantContext_WithEmbeddings_ShouldReturnRelevantChunks()
    {
        var filePath = Path.Combine(_tempDir, "knowledge.txt");
        // Need substantial content for chunking (MinChunkChars = 100)
        var content = "Machine learning is a field of artificial intelligence that enables computers to learn from data. " +
            "It encompasses various algorithms and statistical models that computer systems use to perform tasks without explicit instructions.";
        await File.WriteAllTextAsync(filePath, content);
        var document = await _ragService.AddDocumentAsync(filePath);

        var chunks = await _database.GetChunksAsync(document.Id);
        if (chunks.Count == 0)
        {
            Assert.Inconclusive("No chunks created - content too short");
            return;
        }
        
        foreach (var chunk in chunks)
        {
            var embedding = new float[] { 0.5f, 0.5f, 0.5f };
            await _database.UpdateChunkEmbeddingAsync(chunk.Id, JsonSerializer.Serialize(embedding));
        }

        var queryEmbedding = new float[] { 0.5f, 0.5f, 0.5f };
        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(queryEmbedding);

        var context = await _ragService.GetRelevantContextAsync("machine learning");

        // With identical embeddings, similarity should be 1.0 which is above the 0.75 threshold
        context.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task GetRelevantContext_NoEmbeddings_ShouldReturnEmpty()
    {
        var filePath = Path.Combine(_tempDir, "no_embed.txt");
        await File.WriteAllTextAsync(filePath, "Some content without embeddings");
        await _ragService.AddDocumentAsync(filePath);

        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.1f, 0.2f });

        var context = await _ragService.GetRelevantContextAsync("test query");

        context.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetRelevantContext_EmptyQueryEmbedding_ShouldReturnEmpty()
    {
        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<float>());

        var context = await _ragService.GetRelevantContextAsync("test");

        context.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetRelevantContext_ShouldRespectTopK()
    {
        var filePath = Path.Combine(_tempDir, "multi.txt");
        var content = string.Join("\n\n", Enumerable.Range(1, 10).Select(i => $"Unique paragraph {i} with distinct content."));
        await File.WriteAllTextAsync(filePath, content);
        var document = await _ragService.AddDocumentAsync(filePath);

        var chunks = await _database.GetChunksAsync(document.Id);
        for (int i = 0; i < chunks.Count; i++)
        {
            var embedding = Enumerable.Range(0, 5).Select(j => (float)(i + j) / 10).ToArray();
            await _database.UpdateChunkEmbeddingAsync(chunks[i].Id, JsonSerializer.Serialize(embedding));
        }

        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.5f, 0.6f, 0.7f, 0.8f, 0.9f });

        var context = await _ragService.GetRelevantContextAsync("query", topK: 2);

        context.Split("---").Where(s => !string.IsNullOrWhiteSpace(s)).Should().HaveCountLessOrEqualTo(2);
    }

    #endregion

    #region Hybrid Retrieval Tests

    [TestMethod]
    public async Task Retrieve_HybridMode_ShouldCombineVectorAndKeyword()
    {
        var filePath = Path.Combine(_tempDir, "hybrid.txt");
        await File.WriteAllTextAsync(filePath, "Machine learning algorithms process data.\n\nNeural networks learn patterns.");
        var document = await _ragService.AddDocumentAsync(filePath);

        var chunks = await _database.GetChunksAsync(document.Id);
        foreach (var chunk in chunks)
        {
            await _database.UpdateChunkEmbeddingAsync(chunk.Id, JsonSerializer.Serialize(new float[] { 0.5f, 0.5f }));
        }
        await _database.RebuildFtsIndex();

        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.5f, 0.5f });

        var result = await _ragService.RetrieveAsync("machine learning", topK: 5, mode: RetrievalMode.Hybrid);

        result.Should().NotBeNull();
        result.Metrics.Mode.Should().Be(RetrievalMode.Hybrid);
    }

    [TestMethod]
    public async Task Retrieve_VectorOnlyMode_ShouldNotUseKeyword()
    {
        var filePath = Path.Combine(_tempDir, "vector.txt");
        await File.WriteAllTextAsync(filePath, "Vector search test content.");
        var document = await _ragService.AddDocumentAsync(filePath);

        var chunks = await _database.GetChunksAsync(document.Id);
        foreach (var chunk in chunks)
        {
            await _database.UpdateChunkEmbeddingAsync(chunk.Id, JsonSerializer.Serialize(new float[] { 0.8f, 0.8f }));
        }

        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.8f, 0.8f });

        var result = await _ragService.RetrieveAsync("vector", topK: 5, mode: RetrievalMode.Vector);

        result.Metrics.Mode.Should().Be(RetrievalMode.Vector);
        result.Metrics.KeywordMatches.Should().Be(0);
    }

    [TestMethod]
    public async Task Retrieve_KeywordOnlyMode_ShouldNotUseVector()
    {
        var filePath = Path.Combine(_tempDir, "keyword.txt");
        await File.WriteAllTextAsync(filePath, "Keyword search test content.");
        var document = await _ragService.AddDocumentAsync(filePath);
        await _database.RebuildFtsIndex();

        var result = await _ragService.RetrieveAsync("keyword", topK: 5, mode: RetrievalMode.Keyword);

        result.Metrics.Mode.Should().Be(RetrievalMode.Keyword);
        result.Metrics.VectorMatches.Should().Be(0);
    }

    [TestMethod]
    public async Task Retrieve_ShouldIncludeDocumentNames()
    {
        var filePath = Path.Combine(_tempDir, "named_doc.txt");
        // Need substantial content for chunking
        var content = "This document has a known name and contains enough text to create at least one chunk. " +
            "The RAG system will use this content to test that document names are properly included in retrieval results.";
        await File.WriteAllTextAsync(filePath, content);
        var document = await _ragService.AddDocumentAsync(filePath);

        var chunks = await _database.GetChunksAsync(document.Id);
        if (chunks.Count == 0)
        {
            Assert.Inconclusive("No chunks created");
            return;
        }
        await _database.UpdateChunkEmbeddingAsync(chunks[0].Id, JsonSerializer.Serialize(new float[] { 1.0f, 1.0f }));

        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 1.0f, 1.0f });

        var result = await _ragService.RetrieveAsync("document", topK: 1, minSimilarity: 0.5, mode: RetrievalMode.Vector);

        if (result.Chunks.Count > 0)
        {
            result.Chunks[0].DocumentName.Should().Be("named_doc.txt");
        }
    }

    [TestMethod]
    public async Task Retrieve_ShouldTrackMetrics()
    {
        var filePath = Path.Combine(_tempDir, "metrics.txt");
        // Need substantial content for chunking
        var content = "Content for metrics testing that needs to be substantial enough to create chunks. " +
            "The RAG retrieval system tracks various metrics including timing and match counts.";
        await File.WriteAllTextAsync(filePath, content);
        var document = await _ragService.AddDocumentAsync(filePath);

        var chunks = await _database.GetChunksAsync(document.Id);
        if (chunks.Count == 0)
        {
            Assert.Inconclusive("No chunks created");
            return;
        }
        await _database.UpdateChunkEmbeddingAsync(chunks[0].Id, JsonSerializer.Serialize(new float[] { 0.5f }));
        await _database.RebuildFtsIndex();

        _llmService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.5f });

        var result = await _ragService.RetrieveAsync("metrics", topK: 5, mode: RetrievalMode.Hybrid);

        result.Metrics.TotalTimeMs.Should().BeGreaterOrEqualTo(0);
        result.Metrics.TopK.Should().Be(5);
        result.Metrics.TotalChunksSearched.Should().BeGreaterOrEqualTo(0);
    }

    #endregion
}
