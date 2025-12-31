using WPFLLM.Models;

namespace WPFLLM.Services;

public interface IRagService
{
    Task<RagDocument> AddDocumentAsync(string filePath);
    Task<List<RagDocument>> GetDocumentsAsync();
    Task DeleteDocumentAsync(long documentId);
    Task<string> GetRelevantContextAsync(string query, int topK = 3);
    Task GenerateEmbeddingsAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}
