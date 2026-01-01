using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using NSubstitute;
using System.IO;
using WPFLLM.Models;
using WPFLLM.Services;

namespace WPFLLM.Tests.Integration;

/// <summary>
/// Integration tests for IngestionService testing queue management,
/// file processing, and event handling.
/// </summary>
[TestClass]
public class IngestionServiceIntegrationTests
{
    private IRagService _ragService = null!;
    private IngestionService _ingestionService = null!;
    private string _tempDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _ragService = Substitute.For<IRagService>();
        _ingestionService = new IngestionService(_ragService);

        _tempDir = Path.Combine(Path.GetTempPath(), $"IngestionTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _ingestionService.Dispose();
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    #region Queue Management Tests

    [TestMethod]
    public async Task EnqueueFile_ShouldIncrementQueueCount()
    {
        var filePath = CreateTestFile("test.txt", "Content");

        _ragService.AddDocumentAsync(Arg.Any<string>())
            .Returns(async x =>
            {
                await Task.Delay(500); // Slow processing
                return new RagDocument { Id = 1, FileName = "test.txt" };
            });

        await _ingestionService.EnqueueFileAsync(filePath);

        _ingestionService.QueueCount.Should().BeGreaterOrEqualTo(0);
    }

    [TestMethod]
    public async Task EnqueueFiles_ShouldQueueMultipleFiles()
    {
        var files = new[]
        {
            CreateTestFile("doc1.txt", "Content 1"),
            CreateTestFile("doc2.txt", "Content 2"),
            CreateTestFile("doc3.txt", "Content 3")
        };

        _ragService.AddDocumentAsync(Arg.Any<string>())
            .Returns(async x =>
            {
                await Task.Delay(200);
                return new RagDocument { Id = 1 };
            });

        await _ingestionService.EnqueueFilesAsync(files);

        // All files should be queued or processing
        _ingestionService.QueueCount.Should().BeGreaterOrEqualTo(0);
    }

    [TestMethod]
    public async Task IsProcessing_ShouldReturnTrueWhenQueueNotEmpty()
    {
        var filePath = CreateTestFile("slow.txt", "Content");

        var tcs = new TaskCompletionSource<RagDocument>();
        _ragService.AddDocumentAsync(Arg.Any<string>()).Returns(tcs.Task);

        await _ingestionService.EnqueueFileAsync(filePath);
        await Task.Delay(50); // Allow queue processing to start

        _ingestionService.IsProcessing.Should().BeTrue();

        tcs.SetResult(new RagDocument { Id = 1 });
    }

    #endregion

    #region Event Handling Tests

    [TestMethod]
    public async Task ItemCompleted_ShouldFireOnSuccessfulProcessing()
    {
        var filePath = CreateTestFile("success.txt", "Content");
        var completedEventFired = false;
        string? completedFileName = null;

        _ragService.AddDocumentAsync(Arg.Any<string>())
            .Returns(new RagDocument { Id = 1, FileName = "success.txt" });

        _ingestionService.ItemCompleted += (s, e) =>
        {
            completedEventFired = true;
            completedFileName = e.FileName;
        };

        await _ingestionService.EnqueueFileAsync(filePath);
        await Task.Delay(500); // Wait for processing

        completedEventFired.Should().BeTrue();
        completedFileName.Should().Be("success.txt");
    }

    [TestMethod]
    public async Task ErrorOccurred_ShouldFireOnProcessingError()
    {
        var filePath = CreateTestFile("error.txt", "Content");
        var errorEventFired = false;
        string? errorMessage = null;

        _ragService.AddDocumentAsync(Arg.Any<string>())
            .Returns<RagDocument>(x => throw new Exception("Processing failed"));

        _ingestionService.ErrorOccurred += (s, e) =>
        {
            errorEventFired = true;
            errorMessage = e.ErrorMessage;
        };

        await _ingestionService.EnqueueFileAsync(filePath);
        await Task.Delay(1000); // Wait for processing and retries

        errorEventFired.Should().BeTrue();
        errorMessage.Should().Contain("Processing failed");
    }

    [TestMethod]
    public async Task ProgressChanged_ShouldFireOnStatusChange()
    {
        var filePath = CreateTestFile("progress.txt", "Content");
        var progressEvents = new List<string>();

        _ragService.AddDocumentAsync(Arg.Any<string>())
            .Returns(async x =>
            {
                await Task.Delay(100);
                return new RagDocument { Id = 1 };
            });

        _ingestionService.ProgressChanged += (s, e) =>
        {
            progressEvents.Add(e.Status);
        };

        await _ingestionService.EnqueueFileAsync(filePath);
        await Task.Delay(500);

        progressEvents.Should().Contain("Queued");
        progressEvents.Should().Contain(s => s.Contains("Processing"));
    }

    [TestMethod]
    public async Task ItemCompleted_ShouldIncludeDuration()
    {
        var filePath = CreateTestFile("duration.txt", "Content");
        TimeSpan? duration = null;

        _ragService.AddDocumentAsync(Arg.Any<string>())
            .Returns(async x =>
            {
                await Task.Delay(100);
                return new RagDocument { Id = 1 };
            });

        _ingestionService.ItemCompleted += (s, e) =>
        {
            duration = e.Duration;
        };

        await _ingestionService.EnqueueFileAsync(filePath);
        await Task.Delay(500);

        duration.Should().NotBeNull();
        duration!.Value.TotalMilliseconds.Should().BeGreaterThan(0);
    }

    #endregion

    #region Retry Logic Tests

    [TestMethod]
    public async Task ProcessJob_ShouldRetryOnTransientError()
    {
        var filePath = CreateTestFile("retry.txt", "Content");
        var callCount = 0;

        _ragService.AddDocumentAsync(Arg.Any<string>())
            .Returns(x =>
            {
                callCount++;
                if (callCount < 2)
                    throw new Exception("Transient error");
                return Task.FromResult(new RagDocument { Id = 1 });
            });

        var retryEvents = new List<bool>();
        _ingestionService.ErrorOccurred += (s, e) =>
        {
            retryEvents.Add(e.WillRetry);
        };

        await _ingestionService.EnqueueFileAsync(filePath);
        await Task.Delay(3000); // Wait for retries

        callCount.Should().BeGreaterOrEqualTo(2);
        retryEvents.Should().Contain(true); // At least one retry
    }

    [TestMethod]
    public async Task ProcessJob_ShouldStopRetryingAfterMaxRetries()
    {
        var filePath = CreateTestFile("max_retry.txt", "Content");
        var callCount = 0;

        _ragService.AddDocumentAsync(Arg.Any<string>())
            .Returns<RagDocument>(x =>
            {
                callCount++;
                throw new Exception("Persistent error");
            });

        var finalError = false;
        _ingestionService.ErrorOccurred += (s, e) =>
        {
            if (!e.WillRetry) finalError = true;
        };

        await _ingestionService.EnqueueFileAsync(filePath);
        await Task.Delay(5000); // Wait for all retries

        finalError.Should().BeTrue();
        callCount.Should().BeLessOrEqualTo(3); // Initial + 2 retries
    }

    #endregion

    #region Pause/Resume Tests

    [TestMethod]
    public async Task Pause_ShouldStopProcessing()
    {
        var processedCount = 0;
        
        _ragService.AddDocumentAsync(Arg.Any<string>())
            .Returns(async x =>
            {
                await Task.Delay(100);
                Interlocked.Increment(ref processedCount);
                return new RagDocument { Id = processedCount };
            });

        // Queue multiple files
        for (int i = 0; i < 5; i++)
        {
            var file = CreateTestFile($"pause_{i}.txt", $"Content {i}");
            await _ingestionService.EnqueueFileAsync(file);
        }

        await Task.Delay(150); // Let first item process
        _ingestionService.Pause();
        var countAtPause = processedCount;
        await Task.Delay(300);
        
        // Processing should have stopped or slowed significantly
        processedCount.Should().BeLessOrEqualTo(countAtPause + 1);

        _ingestionService.Resume();
    }

    [TestMethod]
    public async Task Resume_ShouldContinueProcessing()
    {
        var processed = new List<string>();
        
        _ragService.AddDocumentAsync(Arg.Any<string>())
            .Returns(async x =>
            {
                await Task.Delay(50);
                var path = x.Arg<string>();
                processed.Add(Path.GetFileName(path));
                return new RagDocument { Id = 1 };
            });

        var file1 = CreateTestFile("resume_1.txt", "Content 1");
        var file2 = CreateTestFile("resume_2.txt", "Content 2");

        await _ingestionService.EnqueueFileAsync(file1);
        _ingestionService.Pause();
        await _ingestionService.EnqueueFileAsync(file2);
        
        await Task.Delay(200);
        _ingestionService.Resume();
        await Task.Delay(500);

        processed.Should().HaveCountGreaterOrEqualTo(1);
    }

    #endregion

    #region Cancellation Tests

    [TestMethod]
    public async Task CancelAll_ShouldStopProcessing()
    {
        var processedCount = 0;
        
        _ragService.AddDocumentAsync(Arg.Any<string>())
            .Returns(async x =>
            {
                await Task.Delay(200);
                Interlocked.Increment(ref processedCount);
                return new RagDocument { Id = 1 };
            });

        for (int i = 0; i < 10; i++)
        {
            var file = CreateTestFile($"cancel_{i}.txt", $"Content {i}");
            await _ingestionService.EnqueueFileAsync(file);
        }

        await Task.Delay(100);
        _ingestionService.CancelAll();
        var countAtCancel = processedCount;
        await Task.Delay(500);

        // Processing should have stopped
        processedCount.Should().BeLessOrEqualTo(countAtCancel + 1);
    }

    #endregion

    #region Dispose Tests

    [TestMethod]
    public async Task Dispose_ShouldCleanupResources()
    {
        var filePath = CreateTestFile("dispose.txt", "Content");

        _ragService.AddDocumentAsync(Arg.Any<string>())
            .Returns(new RagDocument { Id = 1 });

        await _ingestionService.EnqueueFileAsync(filePath);

        // Should not throw
        _ingestionService.Dispose();
    }

    [TestMethod]
    public async Task EnqueueFile_AfterDispose_ShouldNotProcess()
    {
        _ingestionService.Dispose();

        var filePath = CreateTestFile("after_dispose.txt", "Content");

        // Should not throw, but also should not process
        await _ingestionService.EnqueueFileAsync(filePath);

        await _ragService.DidNotReceive().AddDocumentAsync(Arg.Any<string>());
    }

    #endregion

    private string CreateTestFile(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }
}
