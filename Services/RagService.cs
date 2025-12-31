using System.IO;
using System.Text;
using System.Text.Json;
using WPFLLM.Models;

namespace WPFLLM.Services;

public class RagService : IRagService
{
    private readonly IDatabaseService _database;
    private readonly ILlmService _llmService;
    private const int ChunkSize = 500;
    private const int ChunkOverlap = 50;

    public RagService(IDatabaseService database, ILlmService llmService)
    {
        _database = database;
        _llmService = llmService;
    }

    public async Task<RagDocument> AddDocumentAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var content = await File.ReadAllTextAsync(filePath);
        
        var document = await _database.AddDocumentAsync(fileName, content);
        var chunks = ChunkText(content);
        await _database.AddChunksAsync(document.Id, chunks);
        
        return document;
    }

    public Task<List<RagDocument>> GetDocumentsAsync() => _database.GetDocumentsAsync();

    public Task DeleteDocumentAsync(long documentId) => _database.DeleteDocumentAsync(documentId);

    public async Task<string> GetRelevantContextAsync(string query, int topK = 3)
    {
        var queryEmbedding = await _llmService.GetEmbeddingAsync(query);
        if (queryEmbedding.Length == 0) return string.Empty;

        var allChunks = await _database.GetAllChunksAsync();
        var chunksWithEmbeddings = allChunks
            .Where(c => !string.IsNullOrEmpty(c.Embedding))
            .ToList();

        if (chunksWithEmbeddings.Count == 0) return string.Empty;

        var scored = new List<(RagChunk Chunk, double Score)>();
        foreach (var chunk in chunksWithEmbeddings)
        {
            var embedding = JsonSerializer.Deserialize<float[]>(chunk.Embedding!);
            if (embedding != null)
            {
                var score = CosineSimilarity(queryEmbedding, embedding);
                scored.Add((chunk, score));
            }
        }

        var topChunks = scored
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Chunk.Content);

        return string.Join("\n\n---\n\n", topChunks);
    }

    public async Task GenerateEmbeddingsAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var allChunks = await _database.GetAllChunksAsync();
        var chunksToProcess = allChunks.Where(c => string.IsNullOrEmpty(c.Embedding)).ToList();

        progress?.Report($"Processing {chunksToProcess.Count} chunks...");

        for (int i = 0; i < chunksToProcess.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var chunk = chunksToProcess[i];
            progress?.Report($"Embedding chunk {i + 1}/{chunksToProcess.Count}");

            var embedding = await _llmService.GetEmbeddingAsync(chunk.Content, cancellationToken);
            if (embedding.Length > 0)
            {
                var embeddingJson = JsonSerializer.Serialize(embedding);
                await _database.UpdateChunkEmbeddingAsync(chunk.Id, embeddingJson);
            }

            await Task.Delay(100, cancellationToken); // Rate limiting
        }

        progress?.Report("Done!");
    }

    private static List<string> ChunkText(string text)
    {
        var chunks = new List<string>();
        var words = text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length; i += ChunkSize - ChunkOverlap)
        {
            var chunkWords = words.Skip(i).Take(ChunkSize);
            var chunk = string.Join(" ", chunkWords);
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }
        }

        return chunks;
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;

        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denom = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denom == 0 ? 0 : dot / denom;
    }
}
