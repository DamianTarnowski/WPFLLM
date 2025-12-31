using System.IO;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using WPFLLM.Models;

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
    private InferenceSession? _session;
    private SentencePieceTokenizer? _tokenizer;
    private string? _currentModelId;
    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private const int MaxSequenceLength = 512;

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
            var tokenizerPath = Path.Combine(modelPath, "sentencepiece.bpe.model");

            if (!File.Exists(onnxPath))
                throw new FileNotFoundException($"Model file not found: {onnxPath}");

            if (!File.Exists(tokenizerPath))
                throw new FileNotFoundException($"Tokenizer file not found: {tokenizerPath}");

            // Load ONNX model
            var sessionOptions = new SessionOptions();
            sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            _session = new InferenceSession(onnxPath, sessionOptions);

            // Load SentencePiece tokenizer
            using var tokenizerStream = File.OpenRead(tokenizerPath);
            _tokenizer = SentencePieceTokenizer.Create(tokenizerStream);

            _currentModelId = modelId;
            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _session == null || _tokenizer == null)
            throw new InvalidOperationException("Local embedding service not initialized");

        // E5 models require prefix
        var prefixedText = _currentModelId?.Contains("e5") == true 
            ? $"query: {text}" 
            : text;

        // Tokenize
        var encoding = _tokenizer.EncodeToIds(prefixedText, MaxSequenceLength, out _, out _);
        var inputIds = encoding.ToArray();
        var attentionMask = Enumerable.Repeat(1L, inputIds.Length).ToArray();

        // Create tensors
        var inputIdsTensor = new DenseTensor<long>(inputIds.Select(x => (long)x).ToArray(), [1, inputIds.Length]);
        var attentionMaskTensor = new DenseTensor<long>(attentionMask, [1, attentionMask.Length]);

        // Run inference
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor)
        };

        // Check if model needs token_type_ids
        var inputNames = _session.InputMetadata.Keys.ToList();
        if (inputNames.Contains("token_type_ids"))
        {
            var tokenTypeIds = new DenseTensor<long>(new long[inputIds.Length], [1, inputIds.Length]);
            inputs.Add(NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds));
        }

        using var results = _session.Run(inputs);
        
        // Get the output - usually "last_hidden_state" or "sentence_embedding"
        var output = results.FirstOrDefault(r => r.Name == "last_hidden_state") 
                  ?? results.FirstOrDefault(r => r.Name == "sentence_embedding")
                  ?? results.First();

        var outputTensor = output.AsTensor<float>();
        
        // Mean pooling over sequence dimension
        var embedding = MeanPooling(outputTensor, attentionMask);
        
        // L2 normalize
        var normalized = L2Normalize(embedding);
        
        return Task.FromResult(normalized);
    }

    private static float[] MeanPooling(Tensor<float> lastHiddenState, long[] attentionMask)
    {
        var dimensions = lastHiddenState.Dimensions.ToArray();
        
        // Shape: [batch, seq_len, hidden_size] or [batch, hidden_size]
        if (dimensions.Length == 2)
        {
            // Already pooled, just return
            var result = new float[dimensions[1]];
            for (int i = 0; i < dimensions[1]; i++)
                result[i] = lastHiddenState[0, i];
            return result;
        }

        var seqLen = dimensions[1];
        var hiddenSize = dimensions[2];
        var embedding = new float[hiddenSize];
        var validTokens = attentionMask.Sum();

        for (int i = 0; i < seqLen; i++)
        {
            if (attentionMask[i] == 0) continue;
            for (int j = 0; j < hiddenSize; j++)
            {
                embedding[j] += lastHiddenState[0, i, j];
            }
        }

        for (int j = 0; j < hiddenSize; j++)
            embedding[j] /= validTokens;

        return embedding;
    }

    private static float[] L2Normalize(float[] vector)
    {
        var norm = (float)Math.Sqrt(vector.Sum(x => x * x));
        if (norm < 1e-12f) return vector;
        return vector.Select(x => x / norm).ToArray();
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(_isInitialized && _session != null && _tokenizer != null);
    }

    public int GetDimensions()
    {
        if (_currentModelId != null && EmbeddingModels.Available.TryGetValue(_currentModelId, out var info))
            return info.Dimensions;
        return 0;
    }

    public void Dispose()
    {
        _session?.Dispose();
        _session = null;
        _tokenizer = null;
        _isInitialized = false;
        _currentModelId = null;
    }
}
