using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using WPFLLM.Models;
using WPFLLM.Services;

namespace WPFLLM.Tests.Integration;

/// <summary>
/// Integration tests for DatabaseService using in-memory SQLite database.
/// Tests actual database operations including CRUD, relationships, and queries.
/// </summary>
[TestClass]
public class DatabaseServiceIntegrationTests
{
    private TestDatabaseService _database = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _database = new TestDatabaseService();
        await _database.InitializeAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _database.Dispose();
    }

    #region Conversation Tests

    [TestMethod]
    public async Task CreateConversation_ShouldReturnNewConversation()
    {
        var conversation = await _database.CreateConversationAsync("Test Conversation");

        conversation.Should().NotBeNull();
        conversation.Id.Should().BeGreaterThan(0);
        conversation.Title.Should().Be("Test Conversation");
        conversation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task GetConversations_ShouldReturnAllConversations()
    {
        await _database.CreateConversationAsync("Conv 1");
        await _database.CreateConversationAsync("Conv 2");
        await _database.CreateConversationAsync("Conv 3");

        var conversations = await _database.GetConversationsAsync();

        conversations.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task GetConversations_ShouldOrderByUpdatedAtDescending()
    {
        var conv1 = await _database.CreateConversationAsync("First");
        await Task.Delay(50);
        var conv2 = await _database.CreateConversationAsync("Second");
        await Task.Delay(50);
        var conv3 = await _database.CreateConversationAsync("Third");

        var conversations = await _database.GetConversationsAsync();

        conversations[0].Title.Should().Be("Third");
        conversations[2].Title.Should().Be("First");
    }

    [TestMethod]
    public async Task UpdateConversation_ShouldUpdateTitle()
    {
        var conversation = await _database.CreateConversationAsync("Original");
        conversation.Title = "Updated Title";

        await _database.UpdateConversationAsync(conversation);
        var conversations = await _database.GetConversationsAsync();

        conversations.First(c => c.Id == conversation.Id).Title.Should().Be("Updated Title");
    }

    [TestMethod]
    public async Task DeleteConversation_ShouldRemoveConversation()
    {
        var conversation = await _database.CreateConversationAsync("To Delete");

        await _database.DeleteConversationAsync(conversation.Id);
        var conversations = await _database.GetConversationsAsync();

        conversations.Should().NotContain(c => c.Id == conversation.Id);
    }

    [TestMethod]
    public async Task DeleteConversation_ShouldCascadeDeleteMessages()
    {
        var conversation = await _database.CreateConversationAsync("With Messages");
        await _database.AddMessageAsync(conversation.Id, "user", "Hello");
        await _database.AddMessageAsync(conversation.Id, "assistant", "Hi there");

        await _database.DeleteConversationAsync(conversation.Id);
        var messages = await _database.GetMessagesAsync(conversation.Id);

        messages.Should().BeEmpty();
    }

    #endregion

    #region Message Tests

    [TestMethod]
    public async Task AddMessage_ShouldReturnNewMessage()
    {
        var conversation = await _database.CreateConversationAsync("Test");

        var message = await _database.AddMessageAsync(conversation.Id, "user", "Hello World");

        message.Should().NotBeNull();
        message.Id.Should().BeGreaterThan(0);
        message.ConversationId.Should().Be(conversation.Id);
        message.Role.Should().Be("user");
        message.Content.Should().Be("Hello World");
    }

    [TestMethod]
    public async Task AddMessage_ShouldUpdateConversationTimestamp()
    {
        var conversation = await _database.CreateConversationAsync("Test");
        var originalUpdated = conversation.UpdatedAt;
        await Task.Delay(50);

        await _database.AddMessageAsync(conversation.Id, "user", "New message");
        var conversations = await _database.GetConversationsAsync();
        var updated = conversations.First(c => c.Id == conversation.Id);

        updated.UpdatedAt.Should().BeAfter(originalUpdated);
    }

    [TestMethod]
    public async Task GetMessages_ShouldReturnMessagesInOrder()
    {
        var conversation = await _database.CreateConversationAsync("Test");
        await _database.AddMessageAsync(conversation.Id, "user", "First");
        await Task.Delay(10);
        await _database.AddMessageAsync(conversation.Id, "assistant", "Second");
        await Task.Delay(10);
        await _database.AddMessageAsync(conversation.Id, "user", "Third");

        var messages = await _database.GetMessagesAsync(conversation.Id);

        messages.Should().HaveCount(3);
        messages[0].Content.Should().Be("First");
        messages[1].Content.Should().Be("Second");
        messages[2].Content.Should().Be("Third");
    }

    [TestMethod]
    public async Task UpdateMessage_ShouldUpdateContent()
    {
        var conversation = await _database.CreateConversationAsync("Test");
        var message = await _database.AddMessageAsync(conversation.Id, "user", "Original");
        message.Content = "Updated Content";

        await _database.UpdateMessageAsync(message);
        var messages = await _database.GetMessagesAsync(conversation.Id);

        messages.First().Content.Should().Be("Updated Content");
    }

    [TestMethod]
    public async Task DeleteMessage_ShouldRemoveMessage()
    {
        var conversation = await _database.CreateConversationAsync("Test");
        var message = await _database.AddMessageAsync(conversation.Id, "user", "To Delete");

        await _database.DeleteMessageAsync(message.Id);
        var messages = await _database.GetMessagesAsync(conversation.Id);

        messages.Should().BeEmpty();
    }

    [TestMethod]
    public async Task UpdateMessageEmbedding_ShouldStoreEmbedding()
    {
        var conversation = await _database.CreateConversationAsync("Test");
        var message = await _database.AddMessageAsync(conversation.Id, "user", "Test message");
        var embedding = JsonSerializer.Serialize(new float[] { 0.1f, 0.2f, 0.3f });

        await _database.UpdateMessageEmbeddingAsync(message.Id, embedding);
        var messagesWithEmbeddings = await _database.GetAllMessagesWithEmbeddingsAsync();

        messagesWithEmbeddings.Should().ContainSingle(m => m.Id == message.Id);
        messagesWithEmbeddings.First().Embedding.Should().Be(embedding);
    }

    [TestMethod]
    public async Task GetMessagesWithoutEmbeddings_ShouldReturnOnlyUnembeddedMessages()
    {
        var conversation = await _database.CreateConversationAsync("Test");
        var msg1 = await _database.AddMessageAsync(conversation.Id, "user", "With embedding");
        var msg2 = await _database.AddMessageAsync(conversation.Id, "user", "Without embedding");

        await _database.UpdateMessageEmbeddingAsync(msg1.Id, "[0.1, 0.2]");

        var withoutEmbeddings = await _database.GetMessagesWithoutEmbeddingsAsync();

        withoutEmbeddings.Should().ContainSingle(m => m.Id == msg2.Id);
        withoutEmbeddings.Should().NotContain(m => m.Id == msg1.Id);
    }

    #endregion

    #region Document Tests

    [TestMethod]
    public async Task AddDocument_ShouldReturnNewDocument()
    {
        var document = await _database.AddDocumentAsync("test.txt", "Test content");

        document.Should().NotBeNull();
        document.Id.Should().BeGreaterThan(0);
        document.FileName.Should().Be("test.txt");
        document.Content.Should().Be("Test content");
    }

    [TestMethod]
    public async Task GetDocuments_ShouldReturnAllDocuments()
    {
        await _database.AddDocumentAsync("doc1.txt", "Content 1");
        await _database.AddDocumentAsync("doc2.txt", "Content 2");

        var documents = await _database.GetDocumentsAsync();

        documents.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task DeleteDocument_ShouldCascadeDeleteChunks()
    {
        var document = await _database.AddDocumentAsync("test.txt", "Content");
        await _database.AddChunksAsync(document.Id, new List<string> { "Chunk 1", "Chunk 2" });

        await _database.DeleteDocumentAsync(document.Id);
        var chunks = await _database.GetChunksAsync(document.Id);

        chunks.Should().BeEmpty();
    }

    #endregion

    #region Chunk Tests

    [TestMethod]
    public async Task AddChunks_ShouldCreateChunksWithCorrectIndex()
    {
        var document = await _database.AddDocumentAsync("test.txt", "Content");

        await _database.AddChunksAsync(document.Id, new List<string> { "Chunk 0", "Chunk 1", "Chunk 2" });
        var chunks = await _database.GetChunksAsync(document.Id);

        chunks.Should().HaveCount(3);
        chunks[0].ChunkIndex.Should().Be(0);
        chunks[1].ChunkIndex.Should().Be(1);
        chunks[2].ChunkIndex.Should().Be(2);
    }

    [TestMethod]
    public async Task UpdateChunkEmbedding_ShouldStoreEmbedding()
    {
        var document = await _database.AddDocumentAsync("test.txt", "Content");
        await _database.AddChunksAsync(document.Id, new List<string> { "Chunk content" });
        var chunks = await _database.GetChunksAsync(document.Id);
        var embedding = "[0.1, 0.2, 0.3]";

        await _database.UpdateChunkEmbeddingAsync(chunks[0].Id, embedding);
        var updatedChunks = await _database.GetChunksAsync(document.Id);

        updatedChunks[0].Embedding.Should().Be(embedding);
    }

    [TestMethod]
    public async Task GetAllChunks_ShouldReturnChunksFromAllDocuments()
    {
        var doc1 = await _database.AddDocumentAsync("doc1.txt", "Content 1");
        var doc2 = await _database.AddDocumentAsync("doc2.txt", "Content 2");
        await _database.AddChunksAsync(doc1.Id, new List<string> { "Doc1 Chunk1", "Doc1 Chunk2" });
        await _database.AddChunksAsync(doc2.Id, new List<string> { "Doc2 Chunk1" });

        var allChunks = await _database.GetAllChunksAsync();

        allChunks.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task GetDocumentName_ShouldReturnCorrectName()
    {
        var document = await _database.AddDocumentAsync("my_document.pdf", "Content");

        var name = await _database.GetDocumentNameAsync(document.Id);

        name.Should().Be("my_document.pdf");
    }

    [TestMethod]
    public async Task GetDocumentName_ShouldReturnNullForNonExistent()
    {
        var name = await _database.GetDocumentNameAsync(99999);

        name.Should().BeNull();
    }

    #endregion

    #region Settings Tests

    [TestMethod]
    public async Task SaveSettings_ShouldPersistSettings()
    {
        var settings = new AppSettings
        {
            ApiKey = "test-key",
            Model = "gpt-4",
            Temperature = 0.5,
            MaxTokens = 2048
        };

        await _database.SaveSettingsAsync(settings);
        var loaded = await _database.GetSettingsAsync();

        loaded.ApiKey.Should().Be("test-key");
        loaded.Model.Should().Be("gpt-4");
        loaded.Temperature.Should().Be(0.5);
        loaded.MaxTokens.Should().Be(2048);
    }

    [TestMethod]
    public async Task GetSettings_ShouldReturnDefaultsWhenNotSaved()
    {
        var settings = await _database.GetSettingsAsync();

        settings.Should().NotBeNull();
        settings.Temperature.Should().Be(0.7); // Default value
    }

    #endregion

    #region SavedModel Tests

    [TestMethod]
    public async Task SaveModel_ShouldPersistModel()
    {
        var model = new SavedModel
        {
            ModelId = "gpt-4o",
            DisplayName = "GPT-4o",
            Provider = "OpenAI",
            ContextLength = 128000,
            CreatedAt = DateTime.UtcNow
        };

        await _database.SaveModelAsync(model);
        var saved = await _database.GetSavedModelAsync("gpt-4o");

        saved.Should().NotBeNull();
        saved!.ModelId.Should().Be("gpt-4o");
        saved.DisplayName.Should().Be("GPT-4o");
    }

    [TestMethod]
    public async Task UpdateModelUsage_ShouldIncrementUseCount()
    {
        var model = new SavedModel
        {
            ModelId = "test-model",
            DisplayName = "Test",
            Provider = "Test",
            ContextLength = 4096,
            UseCount = 0
        };
        await _database.SaveModelAsync(model);

        await _database.UpdateModelUsageAsync("test-model");
        await _database.UpdateModelUsageAsync("test-model");
        var updated = await _database.GetSavedModelAsync("test-model");

        updated!.UseCount.Should().Be(2);
        // Use larger tolerance to account for timezone differences in parsing
        updated.LastUsed.Should().NotBeNull();
        updated.LastUsed!.Value.Should().BeAfter(DateTime.UtcNow.AddHours(-2));
    }

    [TestMethod]
    public async Task ToggleFavoriteModel_ShouldToggleState()
    {
        var model = new SavedModel
        {
            ModelId = "fav-model",
            DisplayName = "Favorite",
            Provider = "Test",
            ContextLength = 4096,
            IsFavorite = false
        };
        await _database.SaveModelAsync(model);

        await _database.ToggleFavoriteModelAsync("fav-model");
        var toggled = await _database.GetSavedModelAsync("fav-model");

        toggled!.IsFavorite.Should().BeTrue();

        await _database.ToggleFavoriteModelAsync("fav-model");
        var toggledBack = await _database.GetSavedModelAsync("fav-model");

        toggledBack!.IsFavorite.Should().BeFalse();
    }

    [TestMethod]
    public async Task DeleteSavedModel_ShouldRemoveModel()
    {
        var model = new SavedModel
        {
            ModelId = "to-delete",
            DisplayName = "Delete Me",
            Provider = "Test",
            ContextLength = 4096
        };
        await _database.SaveModelAsync(model);

        await _database.DeleteSavedModelAsync("to-delete");
        var deleted = await _database.GetSavedModelAsync("to-delete");

        deleted.Should().BeNull();
    }

    #endregion

    #region ApiKey Tests

    [TestMethod]
    public async Task SaveApiKey_ShouldPersistKey()
    {
        await _database.SaveApiKeyAsync("OpenAI", "sk-test-key-123");

        var key = await _database.GetApiKeyAsync("OpenAI");

        key.Should().Be("sk-test-key-123");
    }

    [TestMethod]
    public async Task SaveApiKey_ShouldUpdateExistingKey()
    {
        await _database.SaveApiKeyAsync("OpenAI", "old-key");
        await _database.SaveApiKeyAsync("OpenAI", "new-key");

        var key = await _database.GetApiKeyAsync("OpenAI");

        key.Should().Be("new-key");
    }

    [TestMethod]
    public async Task GetApiKeys_ShouldReturnAllKeys()
    {
        await _database.SaveApiKeyAsync("OpenAI", "key1");
        await _database.SaveApiKeyAsync("Anthropic", "key2");

        var keys = await _database.GetApiKeysAsync();

        keys.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task DeleteApiKey_ShouldRemoveKey()
    {
        await _database.SaveApiKeyAsync("ToDelete", "key");

        await _database.DeleteApiKeyAsync("ToDelete");
        var key = await _database.GetApiKeyAsync("ToDelete");

        key.Should().BeNull();
    }

    #endregion

    #region FTS Search Tests

    [TestMethod]
    public async Task SearchChunksFts_ShouldFindMatchingChunks()
    {
        var document = await _database.AddDocumentAsync("test.txt", "Content");
        await _database.AddChunksAsync(document.Id, new List<string>
        {
            "Machine learning is a subset of artificial intelligence",
            "Deep learning uses neural networks",
            "Python is a programming language"
        });
        await _database.RebuildFtsIndex();

        var results = await _database.SearchChunksFtsAsync("machine learning", 10);

        results.Should().NotBeEmpty();
        results.First().Chunk.Content.Should().Contain("Machine learning");
    }

    [TestMethod]
    public async Task SearchChunksFts_ShouldReturnEmptyForNoMatch()
    {
        var document = await _database.AddDocumentAsync("test.txt", "Content");
        await _database.AddChunksAsync(document.Id, new List<string> { "Hello world" });
        await _database.RebuildFtsIndex();

        var results = await _database.SearchChunksFtsAsync("quantum physics", 10);

        results.Should().BeEmpty();
    }

    #endregion
}

/// <summary>
/// Test-specific DatabaseService that uses in-memory SQLite database.
/// </summary>
public class TestDatabaseService : IDatabaseService, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _connectionString;

    public TestDatabaseService()
    {
        _connectionString = "Data Source=:memory:";
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();
    }

    private SqliteConnection CreateConnection() => _connection;

    public async Task InitializeAsync()
    {
        var command = _connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Conversations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Messages (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ConversationId INTEGER NOT NULL,
                Role TEXT NOT NULL,
                Content TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                Embedding TEXT,
                FOREIGN KEY (ConversationId) REFERENCES Conversations(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Documents (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FileName TEXT NOT NULL,
                Content TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Chunks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DocumentId INTEGER NOT NULL,
                Content TEXT NOT NULL,
                ChunkIndex INTEGER NOT NULL,
                Embedding TEXT,
                FOREIGN KEY (DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Settings (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                Data TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS SavedModels (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ModelId TEXT NOT NULL UNIQUE,
                DisplayName TEXT NOT NULL,
                Provider TEXT NOT NULL,
                Description TEXT,
                ContextLength INTEGER,
                PricingInfo TEXT,
                IsFavorite INTEGER DEFAULT 0,
                LastUsed TEXT,
                UseCount INTEGER DEFAULT 0,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ApiKeys (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Provider TEXT NOT NULL UNIQUE,
                ApiKey TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE VIRTUAL TABLE IF NOT EXISTS ChunksFts USING fts5(
                Content,
                content='Chunks',
                content_rowid='Id'
            );
            """;
        
        await command.ExecuteNonQueryAsync();

        // Enable foreign keys
        var fkCommand = _connection.CreateCommand();
        fkCommand.CommandText = "PRAGMA foreign_keys = ON";
        await fkCommand.ExecuteNonQueryAsync();
    }

    public async Task RebuildFtsIndex()
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO ChunksFts(ChunksFts) VALUES('rebuild')";
        try { await cmd.ExecuteNonQueryAsync(); } catch { }
    }

    public async Task<List<Conversation>> GetConversationsAsync()
    {
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, CreatedAt, UpdatedAt FROM Conversations ORDER BY UpdatedAt DESC";
        
        var conversations = new List<Conversation>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            conversations.Add(new Conversation
            {
                Id = reader.GetInt64(0),
                Title = reader.GetString(1),
                CreatedAt = DateTime.Parse(reader.GetString(2)),
                UpdatedAt = DateTime.Parse(reader.GetString(3))
            });
        }
        return conversations;
    }

    public async Task<Conversation> CreateConversationAsync(string title)
    {
        var command = _connection.CreateCommand();
        var now = DateTime.UtcNow.ToString("o");
        command.CommandText = "INSERT INTO Conversations (Title, CreatedAt, UpdatedAt) VALUES (@title, @now, @now); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@now", now);
        
        var id = (long)(await command.ExecuteScalarAsync())!;
        return new Conversation { Id = id, Title = title, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
    }

    public async Task UpdateConversationAsync(Conversation conversation)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "UPDATE Conversations SET Title = @title, UpdatedAt = @updatedAt WHERE Id = @id";
        command.Parameters.AddWithValue("@title", conversation.Title);
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        command.Parameters.AddWithValue("@id", conversation.Id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteConversationAsync(long id)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM Messages WHERE ConversationId = @id; DELETE FROM Conversations WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(long conversationId)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT Id, ConversationId, Role, Content, CreatedAt FROM Messages WHERE ConversationId = @convId ORDER BY CreatedAt";
        command.Parameters.AddWithValue("@convId", conversationId);
        
        var messages = new List<ChatMessage>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(new ChatMessage
            {
                Id = reader.GetInt64(0),
                ConversationId = reader.GetInt64(1),
                Role = reader.GetString(2),
                Content = reader.GetString(3),
                CreatedAt = DateTime.Parse(reader.GetString(4))
            });
        }
        return messages;
    }

    public async Task<ChatMessage> AddMessageAsync(long conversationId, string role, string content)
    {
        var command = _connection.CreateCommand();
        var now = DateTime.UtcNow.ToString("o");
        command.CommandText = "INSERT INTO Messages (ConversationId, Role, Content, CreatedAt) VALUES (@convId, @role, @content, @now); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@convId", conversationId);
        command.Parameters.AddWithValue("@role", role);
        command.Parameters.AddWithValue("@content", content);
        command.Parameters.AddWithValue("@now", now);
        
        var id = (long)(await command.ExecuteScalarAsync())!;
        
        var updateCmd = _connection.CreateCommand();
        updateCmd.CommandText = "UPDATE Conversations SET UpdatedAt = @now WHERE Id = @convId";
        updateCmd.Parameters.AddWithValue("@now", now);
        updateCmd.Parameters.AddWithValue("@convId", conversationId);
        await updateCmd.ExecuteNonQueryAsync();
        
        return new ChatMessage { Id = id, ConversationId = conversationId, Role = role, Content = content, CreatedAt = DateTime.UtcNow };
    }

    public async Task UpdateMessageAsync(ChatMessage message)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "UPDATE Messages SET Content = @content WHERE Id = @id";
        command.Parameters.AddWithValue("@content", message.Content);
        command.Parameters.AddWithValue("@id", message.Id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteMessageAsync(long id)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM Messages WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateMessageEmbeddingAsync(long messageId, string embedding)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "UPDATE Messages SET Embedding = @embedding WHERE Id = @id";
        command.Parameters.AddWithValue("@embedding", embedding);
        command.Parameters.AddWithValue("@id", messageId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<ChatMessage>> GetAllMessagesWithEmbeddingsAsync()
    {
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT Id, ConversationId, Role, Content, CreatedAt, Embedding FROM Messages WHERE Embedding IS NOT NULL ORDER BY CreatedAt DESC";
        
        var messages = new List<ChatMessage>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(new ChatMessage
            {
                Id = reader.GetInt64(0),
                ConversationId = reader.GetInt64(1),
                Role = reader.GetString(2),
                Content = reader.GetString(3),
                CreatedAt = DateTime.Parse(reader.GetString(4)),
                Embedding = reader.GetString(5)
            });
        }
        return messages;
    }

    public async Task<List<ChatMessage>> GetMessagesWithoutEmbeddingsAsync()
    {
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT Id, ConversationId, Role, Content, CreatedAt FROM Messages WHERE Embedding IS NULL ORDER BY CreatedAt";
        
        var messages = new List<ChatMessage>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(new ChatMessage
            {
                Id = reader.GetInt64(0),
                ConversationId = reader.GetInt64(1),
                Role = reader.GetString(2),
                Content = reader.GetString(3),
                CreatedAt = DateTime.Parse(reader.GetString(4))
            });
        }
        return messages;
    }

    public async Task<List<RagDocument>> GetDocumentsAsync()
    {
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT Id, FileName, Content, CreatedAt FROM Documents ORDER BY CreatedAt DESC";
        
        var documents = new List<RagDocument>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            documents.Add(new RagDocument
            {
                Id = reader.GetInt64(0),
                FileName = reader.GetString(1),
                Content = reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3))
            });
        }
        return documents;
    }

    public async Task<RagDocument> AddDocumentAsync(string fileName, string content)
    {
        var command = _connection.CreateCommand();
        var now = DateTime.UtcNow.ToString("o");
        command.CommandText = "INSERT INTO Documents (FileName, Content, CreatedAt) VALUES (@fileName, @content, @now); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@fileName", fileName);
        command.Parameters.AddWithValue("@content", content);
        command.Parameters.AddWithValue("@now", now);
        
        var id = (long)(await command.ExecuteScalarAsync())!;
        return new RagDocument { Id = id, FileName = fileName, Content = content, CreatedAt = DateTime.UtcNow };
    }

    public async Task DeleteDocumentAsync(long id)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM Chunks WHERE DocumentId = @id; DELETE FROM Documents WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<RagChunk>> GetChunksAsync(long documentId)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT Id, DocumentId, Content, ChunkIndex, Embedding FROM Chunks WHERE DocumentId = @docId ORDER BY ChunkIndex";
        command.Parameters.AddWithValue("@docId", documentId);
        
        var chunks = new List<RagChunk>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            chunks.Add(new RagChunk
            {
                Id = reader.GetInt64(0),
                DocumentId = reader.GetInt64(1),
                Content = reader.GetString(2),
                ChunkIndex = reader.GetInt32(3),
                Embedding = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }
        return chunks;
    }

    public async Task<List<RagChunk>> GetAllChunksAsync()
    {
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT Id, DocumentId, Content, ChunkIndex, Embedding FROM Chunks ORDER BY DocumentId, ChunkIndex";
        
        var chunks = new List<RagChunk>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            chunks.Add(new RagChunk
            {
                Id = reader.GetInt64(0),
                DocumentId = reader.GetInt64(1),
                Content = reader.GetString(2),
                ChunkIndex = reader.GetInt32(3),
                Embedding = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }
        return chunks;
    }

    public async Task AddChunksAsync(long documentId, List<string> chunks)
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Chunks (DocumentId, Content, ChunkIndex) VALUES (@docId, @content, @index)";
            command.Parameters.AddWithValue("@docId", documentId);
            command.Parameters.AddWithValue("@content", chunks[i]);
            command.Parameters.AddWithValue("@index", i);
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task UpdateChunkEmbeddingAsync(long chunkId, string embedding)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "UPDATE Chunks SET Embedding = @embedding WHERE Id = @id";
        command.Parameters.AddWithValue("@embedding", embedding);
        command.Parameters.AddWithValue("@id", chunkId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<(RagChunk Chunk, double Score)>> SearchChunksFtsAsync(string query, int limit = 20)
    {
        var command = _connection.CreateCommand();
        
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length > 1)
            .Select(t => $"\"{t}\"*");
        var ftsQuery = string.Join(" OR ", terms);
        
        command.CommandText = """
            SELECT c.Id, c.DocumentId, c.Content, c.ChunkIndex, c.Embedding, bm25(ChunksFts) as score
            FROM ChunksFts fts
            JOIN Chunks c ON c.Id = fts.rowid
            WHERE ChunksFts MATCH @query
            ORDER BY score
            LIMIT @limit
            """;
        command.Parameters.AddWithValue("@query", ftsQuery);
        command.Parameters.AddWithValue("@limit", limit);

        var results = new List<(RagChunk, double)>();
        try
        {
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var chunk = new RagChunk
                {
                    Id = reader.GetInt64(0),
                    DocumentId = reader.GetInt64(1),
                    Content = reader.GetString(2),
                    ChunkIndex = reader.GetInt32(3),
                    Embedding = reader.IsDBNull(4) ? null : reader.GetString(4)
                };
                var score = Math.Abs(reader.GetDouble(5));
                results.Add((chunk, score));
            }
        }
        catch { }
        return results;
    }

    public async Task<string?> GetDocumentNameAsync(long documentId)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT FileName FROM Documents WHERE Id = @id";
        command.Parameters.AddWithValue("@id", documentId);
        
        var result = await command.ExecuteScalarAsync();
        return result as string;
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT Data FROM Settings WHERE Id = 1";
        
        var result = await command.ExecuteScalarAsync();
        if (result is string json)
        {
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        return new AppSettings();
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        var command = _connection.CreateCommand();
        command.CommandText = "INSERT OR REPLACE INTO Settings (Id, Data) VALUES (1, @data)";
        command.Parameters.AddWithValue("@data", json);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<SavedModel>> GetSavedModelsAsync()
    {
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT Id, ModelId, DisplayName, Provider, Description, ContextLength, PricingInfo, IsFavorite, LastUsed, UseCount, CreatedAt FROM SavedModels ORDER BY IsFavorite DESC, UseCount DESC, LastUsed DESC";
        
        var models = new List<SavedModel>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            models.Add(new SavedModel
            {
                Id = reader.GetInt64(0),
                ModelId = reader.GetString(1),
                DisplayName = reader.GetString(2),
                Provider = reader.GetString(3),
                Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                ContextLength = reader.GetInt32(5),
                PricingInfo = reader.IsDBNull(6) ? null : reader.GetString(6),
                IsFavorite = reader.GetInt32(7) == 1,
                LastUsed = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                UseCount = reader.GetInt32(9),
                CreatedAt = DateTime.Parse(reader.GetString(10))
            });
        }
        return models;
    }

    public async Task<SavedModel?> GetSavedModelAsync(string modelId)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT Id, ModelId, DisplayName, Provider, Description, ContextLength, PricingInfo, IsFavorite, LastUsed, UseCount, CreatedAt FROM SavedModels WHERE ModelId = @modelId";
        command.Parameters.AddWithValue("@modelId", modelId);
        
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new SavedModel
            {
                Id = reader.GetInt64(0),
                ModelId = reader.GetString(1),
                DisplayName = reader.GetString(2),
                Provider = reader.GetString(3),
                Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                ContextLength = reader.GetInt32(5),
                PricingInfo = reader.IsDBNull(6) ? null : reader.GetString(6),
                IsFavorite = reader.GetInt32(7) == 1,
                LastUsed = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                UseCount = reader.GetInt32(9),
                CreatedAt = DateTime.Parse(reader.GetString(10))
            };
        }
        return null;
    }

    public async Task SaveModelAsync(SavedModel model)
    {
        var command = _connection.CreateCommand();
        var now = DateTime.UtcNow.ToString("o");
        command.CommandText = """
            INSERT OR REPLACE INTO SavedModels (ModelId, DisplayName, Provider, Description, ContextLength, PricingInfo, IsFavorite, LastUsed, UseCount, CreatedAt)
            VALUES (@modelId, @displayName, @provider, @description, @contextLength, @pricingInfo, @isFavorite, @lastUsed, @useCount, @createdAt)
            """;
        command.Parameters.AddWithValue("@modelId", model.ModelId);
        command.Parameters.AddWithValue("@displayName", model.DisplayName);
        command.Parameters.AddWithValue("@provider", model.Provider);
        command.Parameters.AddWithValue("@description", model.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@contextLength", model.ContextLength);
        command.Parameters.AddWithValue("@pricingInfo", model.PricingInfo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@isFavorite", model.IsFavorite ? 1 : 0);
        command.Parameters.AddWithValue("@lastUsed", model.LastUsed?.ToString("o") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@useCount", model.UseCount);
        command.Parameters.AddWithValue("@createdAt", model.CreatedAt == default ? now : model.CreatedAt.ToString("o"));
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteSavedModelAsync(string modelId)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM SavedModels WHERE ModelId = @modelId";
        command.Parameters.AddWithValue("@modelId", modelId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateModelUsageAsync(string modelId)
    {
        var command = _connection.CreateCommand();
        var now = DateTime.UtcNow.ToString("o");
        command.CommandText = "UPDATE SavedModels SET LastUsed = @now, UseCount = UseCount + 1 WHERE ModelId = @modelId";
        command.Parameters.AddWithValue("@now", now);
        command.Parameters.AddWithValue("@modelId", modelId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task ToggleFavoriteModelAsync(string modelId)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "UPDATE SavedModels SET IsFavorite = CASE WHEN IsFavorite = 1 THEN 0 ELSE 1 END WHERE ModelId = @modelId";
        command.Parameters.AddWithValue("@modelId", modelId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<SavedApiKey>> GetApiKeysAsync()
    {
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT Id, Provider, ApiKey, CreatedAt, UpdatedAt FROM ApiKeys ORDER BY Provider";
        
        var keys = new List<SavedApiKey>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            keys.Add(new SavedApiKey
            {
                Id = reader.GetInt64(0),
                Provider = reader.GetString(1),
                ApiKey = reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3)),
                UpdatedAt = DateTime.Parse(reader.GetString(4))
            });
        }
        return keys;
    }

    public async Task<string?> GetApiKeyAsync(string provider)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT ApiKey FROM ApiKeys WHERE Provider = @provider";
        command.Parameters.AddWithValue("@provider", provider);
        
        var result = await command.ExecuteScalarAsync();
        return result as string;
    }

    public async Task SaveApiKeyAsync(string provider, string apiKey)
    {
        var command = _connection.CreateCommand();
        var now = DateTime.UtcNow.ToString("o");
        command.CommandText = """
            INSERT INTO ApiKeys (Provider, ApiKey, CreatedAt, UpdatedAt) VALUES (@provider, @apiKey, @now, @now)
            ON CONFLICT(Provider) DO UPDATE SET ApiKey = @apiKey, UpdatedAt = @now
            """;
        command.Parameters.AddWithValue("@provider", provider);
        command.Parameters.AddWithValue("@apiKey", apiKey);
        command.Parameters.AddWithValue("@now", now);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteApiKeyAsync(string provider)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM ApiKeys WHERE Provider = @provider";
        command.Parameters.AddWithValue("@provider", provider);
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
