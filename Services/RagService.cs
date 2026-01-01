using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WPFLLM.Models;

namespace WPFLLM.Services;

public class RagService : IRagService
{
    private readonly IDatabaseService _database;
    private readonly ILlmService _llmService;
    
    // Chunking parameters - optimized for E5 embeddings (max 512 tokens ≈ 2000 chars)
    private const int MaxChunkChars = 1500;
    private const int ChunkOverlapChars = 200;
    private const int MinChunkChars = 100;

    public RagService(IDatabaseService database, ILlmService llmService)
    {
        _database = database;
        _llmService = llmService;
    }

    public async Task<RagDocument> AddDocumentAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        var content = extension switch
        {
            ".txt" or ".md" or ".markdown" or ".csv" or ".json" or ".xml" or ".html" or ".htm" 
                => await File.ReadAllTextAsync(filePath),
            _ => throw new NotSupportedException($"Unsupported file type: {extension}. Supported: .txt, .md, .csv, .json, .xml, .html")
        };
        
        // Clean up content
        content = CleanText(content);
        
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("File is empty or contains no readable text.");
        
        var document = await _database.AddDocumentAsync(fileName, content);
        var chunks = ChunkText(content);
        await _database.AddChunksAsync(document.Id, chunks);
        
        return document;
    }
    
    private static string CleanText(string text)
    {
        // Remove excessive whitespace
        text = Regex.Replace(text, @"\r\n|\r|\n", "\n");
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
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

            var embedding = await _llmService.GetEmbeddingAsync(chunk.Content, isQuery: false, cancellationToken);
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
        
        // Split by paragraphs first (preserve semantic boundaries)
        var paragraphs = text.Split(["\n\n"], StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();
        
        foreach (var para in paragraphs)
        {
            var trimmedPara = para.Trim();
            if (string.IsNullOrWhiteSpace(trimmedPara)) continue;
            
            // If adding this paragraph exceeds max, save current chunk and start new
            if (currentChunk.Length + trimmedPara.Length + 2 > MaxChunkChars && currentChunk.Length >= MinChunkChars)
            {
                chunks.Add(currentChunk.ToString().Trim());
                
                // Start new chunk with overlap from end of previous
                var overlapText = GetOverlapText(currentChunk.ToString(), ChunkOverlapChars);
                currentChunk.Clear();
                if (!string.IsNullOrEmpty(overlapText))
                {
                    currentChunk.Append(overlapText).Append(' ');
                }
            }
            
            // If single paragraph is too long, split by sentences
            if (trimmedPara.Length > MaxChunkChars)
            {
                var sentences = SplitIntoSentences(trimmedPara);
                foreach (var sentence in sentences)
                {
                    if (currentChunk.Length + sentence.Length + 1 > MaxChunkChars && currentChunk.Length >= MinChunkChars)
                    {
                        chunks.Add(currentChunk.ToString().Trim());
                        var overlapText = GetOverlapText(currentChunk.ToString(), ChunkOverlapChars);
                        currentChunk.Clear();
                        if (!string.IsNullOrEmpty(overlapText))
                        {
                            currentChunk.Append(overlapText).Append(' ');
                        }
                    }
                    currentChunk.Append(sentence).Append(' ');
                }
            }
            else
            {
                currentChunk.Append(trimmedPara).Append("\n\n");
            }
        }
        
        // Don't forget the last chunk
        if (currentChunk.Length >= MinChunkChars)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }
        
        return chunks;
    }
    
    private static string GetOverlapText(string text, int maxChars)
    {
        if (text.Length <= maxChars) return text;
        
        // Try to break at sentence boundary
        var lastPart = text[^maxChars..];
        var sentenceEnd = lastPart.IndexOfAny(['.', '!', '?', '\n']);
        if (sentenceEnd > 0 && sentenceEnd < lastPart.Length - 20)
        {
            return lastPart[(sentenceEnd + 1)..].Trim();
        }
        return lastPart.Trim();
    }
    
    private static List<string> SplitIntoSentences(string text)
    {
        // Simple sentence splitting - handles . ! ? followed by space and capital letter
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+(?=[A-ZĄĆĘŁŃÓŚŹŻ])");
        return sentences.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
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
