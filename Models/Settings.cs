using System.IO;

namespace WPFLLM.Models;

public class AppSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; } = "https://openrouter.ai/api/v1";
    public string Model { get; set; } = "openai/gpt-4o-mini";
    public bool UseOpenRouter { get; set; } = true;
    public string NativeProvider { get; set; } = "OpenAI";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
    public string SystemPrompt { get; set; } = "You are a helpful assistant.";
    public bool UseRag { get; set; } = false;
    public int RagTopK { get; set; } = 3;
    public bool SidebarCollapsed { get; set; } = false;
    
    // Embedding settings
    public bool UseLocalEmbeddings { get; set; } = false;
    public string LocalEmbeddingModel { get; set; } = "multilingual-e5-large";
}

public class EmbeddingModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Dimensions { get; set; }
    public long SizeBytes { get; set; }
    public string[] RequiredFiles { get; set; } = [];
    public string HuggingFaceRepo { get; set; } = string.Empty;
    public string[] Languages { get; set; } = [];
    
    // New fields
    public int QualityRating { get; set; } // 1-5 stars
    public string RamRequired { get; set; } = string.Empty;
    public string InferenceSpeed { get; set; } = string.Empty;
    public string RecommendedFor { get; set; } = string.Empty;
}

public static class EmbeddingModels
{
    public static readonly Dictionary<string, EmbeddingModelInfo> Available = new()
    {
        ["multilingual-e5-large"] = new()
        {
            Id = "multilingual-e5-large",
            DisplayName = "Multilingual E5 Large",
            Description = "Najwyższa jakość embeddingów. Model oparty na XLM-RoBERTa-large, 24 warstwy transformera. Doskonały dla języka polskiego i innych języków słowiańskich. Prefix query:/passage: dodawany automatycznie.",
            Dimensions = 1024,
            SizeBytes = 2_200_000_000,
            RequiredFiles = ["model.onnx", "vocab.txt"],
            HuggingFaceRepo = "intfloat/multilingual-e5-large",
            Languages = ["pl", "en", "de", "fr", "es", "it", "pt", "nl", "ru", "zh", "ja", "ko", "+90 więcej"],
            QualityRating = 5,
            RamRequired = "4-6 GB RAM",
            InferenceSpeed = "~150-300ms/tekst",
            RecommendedFor = "Produkcja, wysoka precyzja wyszukiwania, dokumenty wielojęzyczne"
        },
        ["multilingual-e5-base"] = new()
        {
            Id = "multilingual-e5-base",
            DisplayName = "Multilingual E5 Base",
            Description = "Bardzo dobra jakość przy rozsądnym rozmiarze. Model oparty na XLM-RoBERTa-base, 12 warstw transformera. Świetny kompromis między jakością a wydajnością.",
            Dimensions = 768,
            SizeBytes = 1_100_000_000,
            RequiredFiles = ["model.onnx", "vocab.txt"],
            HuggingFaceRepo = "intfloat/multilingual-e5-base",
            Languages = ["pl", "en", "de", "fr", "es", "it", "pt", "nl", "ru", "zh", "ja", "ko", "+90 więcej"],
            QualityRating = 4,
            RamRequired = "2-3 GB RAM",
            InferenceSpeed = "~80-150ms/tekst",
            RecommendedFor = "Większość zastosowań, balans jakości i szybkości"
        },
        ["multilingual-e5-small"] = new()
        {
            Id = "multilingual-e5-small",
            DisplayName = "Multilingual E5 Small",
            Description = "Lekki i szybki model. Tylko 6 warstw transformera, idealny na słabszy sprzęt lub gdy priorytetem jest szybkość. Jakość niższa ale wystarczająca dla prostych zastosowań.",
            Dimensions = 384,
            SizeBytes = 470_000_000,
            RequiredFiles = ["model.onnx", "vocab.txt"],
            HuggingFaceRepo = "intfloat/multilingual-e5-small",
            Languages = ["pl", "en", "de", "fr", "es", "it", "pt", "nl", "ru", "zh", "ja", "ko", "+90 więcej"],
            QualityRating = 3,
            RamRequired = "1-2 GB RAM",
            InferenceSpeed = "~30-60ms/tekst",
            RecommendedFor = "Słabszy sprzęt, szybkie prototypy, duże ilości tekstów"
        }
    };

    public static string GetModelsPath() => 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WPFLLM", "models");
    
    public static string GetModelPath(string modelId) => 
        Path.Combine(GetModelsPath(), modelId);
}

public class SavedModel
{
    public long Id { get; set; }
    public string ModelId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ContextLength { get; set; }
    public string? PricingInfo { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UseCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SavedApiKey
{
    public long Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public record ApiProviderInfo(string Name, string Endpoint, string Description, string KeyUrl);

public static class ApiProviders
{
    public static readonly Dictionary<string, ApiProviderInfo> NativeProviders = new()
    {
        ["OpenAI"] = new("OpenAI", "https://api.openai.com/v1", "GPT-4o, GPT-4, o1, o3", "https://platform.openai.com/api-keys"),
        ["Anthropic"] = new("Anthropic", "https://api.anthropic.com/v1", "Claude 3.5/4 Sonnet, Opus, Haiku", "https://console.anthropic.com/"),
        ["Google"] = new("Google AI", "https://generativelanguage.googleapis.com/v1beta", "Gemini 2.0/1.5 Pro, Flash", "https://aistudio.google.com/apikey"),
        ["Mistral"] = new("Mistral", "https://api.mistral.ai/v1", "Mistral Large, Medium, Small", "https://console.mistral.ai/api-keys"),
        ["Groq"] = new("Groq", "https://api.groq.com/openai/v1", "Llama 3, Mixtral (bardzo szybkie)", "https://console.groq.com/keys"),
        ["Together"] = new("Together AI", "https://api.together.xyz/v1", "Llama, Qwen, DeepSeek", "https://api.together.ai/settings/api-keys"),
        ["Fireworks"] = new("Fireworks AI", "https://api.fireworks.ai/inference/v1", "Llama, Mixtral, DeepSeek", "https://fireworks.ai/account/api-keys"),
        ["DeepSeek"] = new("DeepSeek", "https://api.deepseek.com/v1", "DeepSeek V3, R1 Reasoner", "https://platform.deepseek.com/api_keys"),
        ["xAI"] = new("xAI (Grok)", "https://api.x.ai/v1", "Grok 2, Grok 3", "https://console.x.ai/"),
        ["SambaNova"] = new("SambaNova", "https://api.sambanova.ai/v1", "Llama (bardzo szybkie)", "https://cloud.sambanova.ai/apis"),
        ["Perplexity"] = new("Perplexity", "https://api.perplexity.ai", "Sonar (z dostępem do internetu)", "https://www.perplexity.ai/settings/api"),
        ["Cohere"] = new("Cohere", "https://api.cohere.ai/v1", "Command R+", "https://dashboard.cohere.com/api-keys"),
    };

    public const string OpenRouterEndpoint = "https://openrouter.ai/api/v1";
    
    public static string GetEndpoint(string providerName)
    {
        return NativeProviders.TryGetValue(providerName, out var info) ? info.Endpoint : OpenRouterEndpoint;
    }

    public static List<string> GetProviderNames() => [.. NativeProviders.Keys];
}
