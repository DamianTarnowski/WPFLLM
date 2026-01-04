# Instrukcja publikacji w Microsoft Store

## Krok 1: Przygotuj pakiet MSIX

### Opcja A: Przez Visual Studio (rekomendowane)
1. Otwórz `WPFLLM.sln` w Visual Studio 2022
2. Dodaj projekt `WPFLLM.Package` do solution:
   - Right-click na Solution → Add → Existing Project
   - Wybierz `WPFLLM.Package\WPFLLM.Package.wapproj`
3. Right-click na `WPFLLM.Package` → Publish → Create App Packages
4. Wybierz "Microsoft Store as a new app name" lub "Sideloading"
5. Zaloguj się do swojego konta MS Dev
6. Wybierz architekturę: x64
7. Kliknij Create

### Opcja B: Przez command line
```powershell
# Wymaga Windows SDK i VS Build Tools
msbuild WPFLLM.Package\WPFLLM.Package.wapproj /p:Configuration=Release /p:Platform=x64 /p:UapAppxPackageBuildMode=StoreUpload /p:AppxBundle=Always
```

---

## Krok 2: Zaloguj się do Partner Center

1. Idź do: https://partner.microsoft.com/dashboard
2. Zaloguj się kontem Microsoft Developer

---

## Krok 3: Utwórz nową aplikację

1. Kliknij **Apps and games** → **New product** → **App**
2. Zarezerwuj nazwę: `WPFLLM - AI Assistant`
3. Kliknij **Reserve product name**

---

## Krok 4: Uzupełnij informacje o aplikacji

### 4.1 Properties
- **Category**: Productivity
- **Subcategory**: Personal assistant
- **Privacy policy URL**: `https://github.com/DamianTarnowski/WPFLLM/blob/master/PRIVACY.md`
- **Support contact**: hdtdtr@gmail.com
- **Website**: https://github.com/DamianTarnowski/WPFLLM

### 4.2 Age ratings
- Wypełnij kwestionariusz IARC
- Aplikacja powinna otrzymać ocenę 3+ (brak treści dla dorosłych)

### 4.3 Pricing and availability
- **Base price**: Free (lub ustal cenę)
- **Markets**: All markets (lub wybrane)
- **Visibility**: Public

---

## Krok 5: Store listings

### Dla języka English (en-US):

**Product name:** 
```
WPFLLM - AI Assistant
```

**Short description (max 100 znaków):**
```
Powerful AI assistant with RAG knowledge base, local embeddings, and document analysis.
```

**Description:**
```
WPFLLM is a powerful desktop AI assistant that brings advanced AI capabilities directly to your Windows PC.

Key Features:

🤖 AI Chat
- Stream responses from OpenRouter API (GPT-4, Claude, Llama, and more)
- Conversation history with search
- Export chats to Markdown/JSON

📚 Knowledge Base (RAG)
- Import PDF, DOCX, TXT, MD, JSON, CSV documents
- Hybrid search combining vector similarity and keyword matching
- Automatic context retrieval for smarter AI responses

🧠 Local Embeddings
- Run embedding models locally for privacy
- No data sent to external servers
- Download and manage models within the app

📊 Document Analysis
- AI-powered document summarization
- Extract key points, intents, and insights

🌍 Multilingual - Interface in 11 languages

🔒 Privacy-Focused - All data stored locally, no telemetry

Requirements:
- Windows 10/11 (64-bit)
- OpenRouter API key (free tier available at openrouter.ai)
```

**Keywords:**
```
AI assistant; ChatGPT; RAG; document analysis; knowledge base; local LLM; embeddings
```

### Screenshots (wymagane):
Zrób screenshoty aplikacji (min. 1366x768):
1. Główny widok czatu z rozmową
2. Baza wiedzy z dokumentami
3. Analiza dokumentu
4. Ustawienia
5. Strona embeddingów

---

## Krok 6: Packages

1. Kliknij **Packages**
2. Wgraj plik `.msixupload` z folderu:
   ```
   WPFLLM.Package\AppPackages\WPFLLM.Package_X.X.X.0_x64.msixupload
   ```
3. Poczekaj na walidację

### Restricted Capabilities (runFullTrust)

Przy uploadzie pojawi się pytanie o `runFullTrust`. Wpisz:
```
WPF desktop app needs runFullTrust for: file access (user-selected docs for RAG), local SQLite database, HTTP calls to AI APIs, ONNX model loading for embeddings. No system changes, no telemetry.
```

---

## Krok 7: Submit

1. Przejrzyj wszystkie sekcje (powinny mieć zielone checkmarki)
2. Kliknij **Submit to the Store**
3. Poczekaj na review (1-3 dni robocze)

---

## Krok 8: Budowanie nowej wersji

Przy każdej aktualizacji:

1. Zwiększ wersję w `Package.appxmanifest`:
   ```xml
   Version="1.0.3.0"  <!-- zwiększ ostatnią cyfrę przed .0 -->
   ```

2. Zbuduj pakiet:
   ```powershell
   msbuild WPFLLM.Package\WPFLLM.Package.wapproj /p:Configuration=Release /p:Platform=x64 /p:AppxPackageDir=AppPackages\ /p:UapAppxPackageBuildMode=StoreUpload /restore
   ```

3. Wgraj nowy `.msixupload` do Partner Center

---

## Troubleshooting

### Problem: "Publisher ID mismatch"
W pliku `Package.appxmanifest` zmień:
```xml
Publisher="CN=YOURPUBLISHERID"
```
Na swój Publisher ID z Partner Center (znajdziesz w Account settings → Organization profile → Publisher ID)

### Problem: "Missing screenshots"
Dodaj minimum 1 screenshot dla każdego języka w Store listings.

### Problem: "Package validation failed"
- Upewnij się, że wersja w manifeście jest wyższa niż poprzednia
- Sprawdź czy wszystkie obrazy mają wymagane rozmiary

---

## Przydatne linki

- Partner Center: https://partner.microsoft.com/dashboard
- Dokumentacja MSIX: https://docs.microsoft.com/windows/msix/
- Store policies: https://docs.microsoft.com/windows/uwp/publish/store-policies

---

## Kontakt

Problemy? Napisz na hdtdtr@gmail.com
