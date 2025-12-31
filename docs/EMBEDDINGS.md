# 🧠 System Embeddingów - Dokumentacja Techniczna

## Przegląd

WPFLLM wykorzystuje lokalne modele embeddingowe z rodziny **multilingual-E5** do generowania wektorów semantycznych. System został zoptymalizowany pod kątem **wysokiej jakości dyskryminacji** między tekstami semantycznie bliskimi i odległymi.

## Architektura

```
┌─────────────────────────────────────────────────────────────┐
│                    LocalEmbeddingService                     │
│   - Inicjalizacja modelu ONNX                               │
│   - Prefiksy E5 (query:/passage:)                           │
│   - Mean pooling + L2 normalizacja                          │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                    RustTokenizer (FFI)                       │
│   - HuggingFace Tokenizers (Rust)                           │
│   - add_special_tokens = true                               │
│   - Automatyczne <s> i </s>                                 │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                    ONNX Runtime                              │
│   - model.onnx (+ model.onnx_data dla large)                │
│   - GPU/CPU inference                                        │
└─────────────────────────────────────────────────────────────┘
```

## Tokenizer Rust FFI

### Problem

Biblioteki .NET do tokenizacji (Microsoft.ML.Tokenizers, Tokenizers.DotNet) nie obsługują poprawnie parametru `add_special_tokens=true`, który jest **krytyczny** dla modeli E5/XLM-RoBERTa. Bez specjalnych tokenów `<s>` (BOS) i `</s>` (EOS) embeddingi mają bardzo słabą dyskryminację.

### Rozwiązanie

Zaimplementowaliśmy natywny tokenizer w **Rust** używając oficjalnej biblioteki HuggingFace `tokenizers`:

```rust
// TokenizerRust/src/lib.rs
#[no_mangle]
pub extern "C" fn tokenizer_encode(text: *const c_char, out_ids: *mut c_int, max_len: usize) -> c_int {
    // ...
    // KRYTYCZNE: add_special_tokens = true
    let encoding = tokenizer.encode(text_str, true)?;
    // ...
}
```

### Wyniki

| Metryka | Przed (SentencePiece .NET) | Po (Rust HuggingFace) |
|---------|---------------------------|----------------------|
| Bliskie semantycznie | 83.9% | 85.4% |
| Dalekie semantycznie | 83.2% | 70.9% |
| **GAP (dyskryminacja)** | **0.7%** ❌ | **14.5%** ✅ |

**20x lepsza dyskryminacja!**

## Pliki modelu

Każdy model E5 wymaga następujących plików:

```
%LOCALAPPDATA%\WPFLLM\models\multilingual-e5-{size}\
├── model.onnx           # Model ONNX
├── model.onnx_data      # Wagi (tylko dla large, ~2GB)
└── tokenizer.json       # Tokenizer HuggingFace
```

## Prefiksy E5

Modele E5 wymagają specjalnych prefiksów:

| Typ tekstu | Prefiks | Przykład |
|------------|---------|----------|
| Zapytanie użytkownika | `query: ` | `query: Jak kupić samochód?` |
| Dokument/passage | `passage: ` | `passage: Porady przy zakupie auta...` |

```csharp
private string PrepareE5Text(string text, bool isQuery)
{
    var prefix = isQuery ? "query: " : "passage: ";
    if (text.StartsWith("query:") || text.StartsWith("passage:"))
        return text;
    return prefix + text;
}
```

## Mean Pooling

E5 wymaga **mean pooling** (NIE CLS pooling):

```csharp
private static float[] MeanPooling(Tensor<float> lastHiddenState, long[] attentionMask)
{
    var embedding = new float[hiddenSize];
    var sumMask = 0f;
    
    for (int i = 0; i < seqLen; i++)
    {
        if (attentionMask[i] == 1)
        {
            for (int j = 0; j < hiddenSize; j++)
                embedding[j] += lastHiddenState[0, i, j];
            sumMask += 1f;
        }
    }
    
    // Średnia po wszystkich tokenach
    for (int i = 0; i < hiddenSize; i++)
        embedding[i] /= sumMask;
        
    return embedding;
}
```

## Normalizacja L2

Po mean pooling stosujemy normalizację L2 (krytyczne dla cosine similarity):

```csharp
private static float[] L2Normalize(float[] vector)
{
    var norm = (float)Math.Sqrt(vector.Sum(x => x * x));
    if (norm < 1e-12f) return vector;
    return vector.Select(x => x / norm).ToArray();
}
```

## Modele

| Model | Wymiary | Rozmiar | Jakość | RAM |
|-------|---------|---------|--------|-----|
| multilingual-e5-small | 384 | ~470MB | ★★★☆☆ | 1-2 GB |
| multilingual-e5-base | 768 | ~1.1GB | ★★★★☆ | 2-3 GB |
| multilingual-e5-large | 1024 | ~2.2GB | ★★★★★ | 4-6 GB |

Wszystkie modele obsługują **100+ języków** w tym polski.

## Budowanie Tokenizera Rust

```bash
cd TokenizerRust
cargo build --release
```

Wynikowy plik: `target/release/hf_tokenizer.dll` (~3.7MB)

## Checklist dla poprawnych embeddingów

- [x] Tokenizer z `tokenizer.json` (nie sentencepiece.bpe.model)
- [x] `add_special_tokens = true` (tokeny `<s>` i `</s>`)
- [x] Prefiksy `query:` / `passage:`
- [x] Mean pooling (nie CLS)
- [x] Normalizacja L2
- [x] Max sequence length: 256 (zalecane), 512 (max)

## Troubleshooting

### Słaba dyskryminacja (GAP < 5%)
1. Sprawdź czy tokenizer dodaje specjalne tokeny (ID 0 na początku, ID 2 na końcu)
2. Upewnij się że używasz prefiksów `query:`/`passage:`
3. Zweryfikuj normalizację L2

### DllNotFoundException: hf_tokenizer
1. Skopiuj `hf_tokenizer.dll` do katalogu z aplikacją
2. Lub dodaj do projektu z `CopyToOutputDirectory`

### Wolna inference
1. Użyj mniejszego modelu (e5-small)
2. Zmniejsz `MaxSequenceLength`
3. Rozważ GPU acceleration (ONNX Runtime CUDA)
