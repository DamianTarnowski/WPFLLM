using System.Diagnostics;
using Microsoft.ML.OnnxRuntimeGenAI;

Console.WriteLine("=== Local LLM Integration Test ===");
Console.WriteLine();

var modelPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "WPFLLM", "models", "phi-3-mini-4k-instruct");

Console.WriteLine($"Model path: {modelPath}");

if (!Directory.Exists(modelPath))
{
    Console.WriteLine("ERROR: Model not downloaded. Run the app and download the model first.");
    Console.WriteLine("Or manually download from: https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-onnx");
    return;
}

var requiredFiles = new[] { "genai_config.json", "tokenizer.json" };
foreach (var file in requiredFiles)
{
    var filePath = Path.Combine(modelPath, file);
    if (!File.Exists(filePath))
    {
        Console.WriteLine($"ERROR: Missing file: {file}");
        return;
    }
}

Console.WriteLine("Loading model...");
var sw = Stopwatch.StartNew();

try
{
    using var model = new Model(modelPath);
    using var tokenizer = new Tokenizer(model);
    
    Console.WriteLine($"Model loaded in {sw.ElapsedMilliseconds}ms");
    Console.WriteLine();

    // Test 1: Simple question
    await TestPrompt(model, tokenizer, "What is 2+2?", "You are a helpful math assistant. Be brief.");
    
    // Test 2: Polish question
    await TestPrompt(model, tokenizer, "Jak masz na imie?", "Jestes pomocnym asystentem AI o imieniu Phi.");

    Console.WriteLine();
    Console.WriteLine("=== All tests completed! ===");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

static async Task TestPrompt(Model model, Tokenizer tokenizer, string prompt, string system)
{
    Console.WriteLine($"--- Test: {prompt} ---");
    
    var nl = "\n";
    var fullPrompt = "<|system|>" + nl + system + "<|end|>" + nl + "<|user|>" + nl + prompt + "<|end|>" + nl + "<|assistant|>" + nl;
    
    using var sequences = tokenizer.Encode(fullPrompt);
    using var genParams = new GeneratorParams(model);
    genParams.SetSearchOption("max_length", 256);
    genParams.SetSearchOption("temperature", 0.7);
    
    using var generator = new Generator(model, genParams);
    generator.AppendTokenSequences(sequences);
    
    using var stream = tokenizer.CreateStream();
    var sw = Stopwatch.StartNew();
    var tokenCount = 0;
    
    Console.Write("Response: ");
    while (!generator.IsDone())
    {
        generator.GenerateNextToken();
        var tokens = generator.GetNextTokens();
        if (tokens.Length > 0)
        {
            var text = stream.Decode(tokens[0]);
            Console.Write(text);
            tokenCount++;
        }
        await Task.Yield();
    }
    
    var elapsed = sw.Elapsed.TotalSeconds;
    var tokPerSec = tokenCount / elapsed;
    Console.WriteLine();
    Console.WriteLine($"[{tokenCount} tokens in {elapsed:F2}s = {tokPerSec:F1} tok/s]");
    Console.WriteLine();
}
