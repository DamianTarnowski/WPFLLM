using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using WPFLLM.Models;

namespace WPFLLM.Services;

public class LocalLlmService : ILocalLlmService, IDisposable
{
    private readonly HttpClient _httpClient;
    private string? _currentModelId;
    private bool _initialized;
    private bool _disposed;

    public LocalLlmService(HttpClient httpClient) => _httpClient = httpClient;
    public Task<bool> IsAvailableAsync() => Task.FromResult(_initialized);

    public Task<bool> IsModelDownloadedAsync(string modelId)
    {
        if (!LocalLlmModels.Available.TryGetValue(modelId, out var info)) return Task.FromResult(false);
        var path = LocalLlmModels.GetModelPath(modelId);
        if (!Directory.Exists(path)) return Task.FromResult(false);
        return Task.FromResult(info.RequiredFiles.All(f => File.Exists(Path.Combine(path, f))));
    }

    public async Task InitializeAsync(string modelId)
    {
        if (_currentModelId == modelId && _initialized) return;
        if (!await IsModelDownloadedAsync(modelId)) throw new InvalidOperationException("Model not downloaded");
        _currentModelId = modelId;
        _initialized = true;
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(string prompt, string? systemPrompt = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!_initialized) throw new InvalidOperationException("Not initialized");
        await Task.Yield();
        yield return "[Local LLM - TODO]";
    }

    public async Task DownloadModelAsync(string modelId, IProgress<(long downloaded, long total, string status)>? progress = null, CancellationToken ct = default)
    {
        if (!LocalLlmModels.Available.TryGetValue(modelId, out var info)) throw new ArgumentException("Unknown model");
        var modelPath = LocalLlmModels.GetModelPath(modelId);
        Directory.CreateDirectory(modelPath);
        var baseUrl = "https://huggingface.co/" + info.HuggingFaceRepo + "/resolve/main/cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4/";
        foreach (var file in info.RequiredFiles)
        {
            var filePath = Path.Combine(modelPath, file);
            if (File.Exists(filePath)) continue;
            progress?.Report((0, 0, "Downloading: " + file));
            using var response = await _httpClient.GetAsync(baseUrl + file, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? 0;
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            await using var fs = File.Create(filePath);
            var buffer = new byte[81920];
            long dl = 0; int r;
            while ((r = await stream.ReadAsync(buffer, ct)) > 0) { await fs.WriteAsync(buffer.AsMemory(0, r), ct); dl += r; progress?.Report((dl, total, file)); }
        }
        progress?.Report((0, 0, "Done!"));
    }

    public void Dispose() { if (_disposed) return; _disposed = true; GC.SuppressFinalize(this); }
}
