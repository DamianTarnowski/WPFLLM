using System.IO;
using Microsoft.SemanticKernel.Connectors.Onnx;
using WPFLLM.Models;

#pragma warning disable SKEXP0070 // Experimental API
#pragma warning disable CS0618 // Obsolete warning

namespace WPFLLM.Services;

public interface ILocalEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync();
    Task InitializeAsync(string modelId);
    void Dispose();
    int GetDimensions();
}

public class LocalEmbeddingService : ILocalEmbeddingService, IDisposable
{
    private BertOnnxTextEmbeddingGenerationService? _embeddingService;
    private string? _currentModelId;
    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public async Task InitializeAsync(string modelId)
    {
        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized && _currentModelId == modelId)
                return;

            Dispose();

            if (!EmbeddingModels.Available.TryGetValue(modelId, out var modelInfo))
                throw new ArgumentException($"Unknown model: {modelId}");

            var modelPath = EmbeddingModels.GetModelPath(modelId);
            var onnxPath = Path.Combine(modelPath, "model.onnx");
            var vocabPath = Path.Combine(modelPath, "vocab.txt");

            // Try tokenizer.json first, fallback to vocab.txt
            var tokenizerPath = Path.Combine(modelPath, "tokenizer.json");
            if (!File.Exists(tokenizerPath))
            {
                tokenizerPath = vocabPath;
            }

            if (!File.Exists(onnxPath))
                throw new FileNotFoundException($"Model file not found: {onnxPath}");

            if (!File.Exists(tokenizerPath))
                throw new FileNotFoundException($"Tokenizer/vocab file not found");

            _embeddingService = await BertOnnxTextEmbeddingGenerationService.CreateAsync(
                onnxModelPath: onnxPath,
                vocabPath: tokenizerPath);

            _currentModelId = modelId;
            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _embeddingService == null)
            throw new InvalidOperationException("Local embedding service not initialized");

        // E5 models require prefix
        var prefixedText = _currentModelId?.Contains("e5") == true 
            ? $"query: {text}" 
            : text;

        var embeddings = await _embeddingService.GenerateEmbeddingsAsync([prefixedText], cancellationToken: cancellationToken);
        
        if (embeddings.Count == 0)
            return [];

        return embeddings[0].ToArray();
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(_isInitialized && _embeddingService != null);
    }

    public int GetDimensions()
    {
        if (_currentModelId != null && EmbeddingModels.Available.TryGetValue(_currentModelId, out var info))
            return info.Dimensions;
        return 0;
    }

    public void Dispose()
    {
        _embeddingService?.Dispose();
        _embeddingService = null;
        _isInitialized = false;
        _currentModelId = null;
    }
}
