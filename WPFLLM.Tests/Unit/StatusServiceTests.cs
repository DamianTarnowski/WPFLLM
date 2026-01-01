using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using WPFLLM.Services;

namespace WPFLLM.Tests.Unit;

/// <summary>
/// Tests for StatusService covering status tracking and event notification.
/// </summary>
[TestClass]
public class StatusServiceTests
{
    private StatusService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new StatusService();
    }

    #region Initial State Tests

    [TestMethod]
    public void IsOfflineMode_Initially_ShouldBeTrue()
    {
        _service.IsOfflineMode.Should().BeTrue();
    }

    [TestMethod]
    public void IsEncrypted_Initially_ShouldBeFalse()
    {
        _service.IsEncrypted.Should().BeFalse();
    }

    [TestMethod]
    public void NetworkCallCount_Initially_ShouldBeZero()
    {
        _service.NetworkCallCount.Should().Be(0);
    }

    [TestMethod]
    public void CurrentStatus_Initially_ShouldBeReady()
    {
        _service.CurrentStatus.Should().Be("Ready");
    }

    #endregion

    #region Network Call Tests

    [TestMethod]
    public void IncrementNetworkCalls_ShouldIncrementCounter()
    {
        _service.IncrementNetworkCalls();
        _service.IncrementNetworkCalls();
        _service.IncrementNetworkCalls();

        _service.NetworkCallCount.Should().Be(3);
    }

    [TestMethod]
    public void IncrementNetworkCalls_ShouldSetOfflineModeFalse()
    {
        _service.IncrementNetworkCalls();

        _service.IsOfflineMode.Should().BeFalse();
    }

    [TestMethod]
    public void IncrementNetworkCalls_ShouldFireStatusChanged()
    {
        var eventFired = false;
        _service.StatusChanged += (s, e) => eventFired = true;

        _service.IncrementNetworkCalls();

        eventFired.Should().BeTrue();
    }

    [TestMethod]
    public void IncrementNetworkCalls_ThreadSafe_ShouldWorkCorrectly()
    {
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => _service.IncrementNetworkCalls()))
            .ToArray();

        Task.WaitAll(tasks);

        _service.NetworkCallCount.Should().Be(100);
    }

    #endregion

    #region Status Tests

    [TestMethod]
    public void SetStatus_ShouldUpdateCurrentStatus()
    {
        _service.SetStatus("Processing...");

        _service.CurrentStatus.Should().Be("Processing...");
    }

    [TestMethod]
    public void SetStatus_ShouldFireStatusChanged()
    {
        var eventFired = false;
        _service.StatusChanged += (s, e) => eventFired = true;

        _service.SetStatus("New Status");

        eventFired.Should().BeTrue();
    }

    [TestMethod]
    public void SetStatus_MultipleUpdates_ShouldReflectLatest()
    {
        _service.SetStatus("Status 1");
        _service.SetStatus("Status 2");
        _service.SetStatus("Status 3");

        _service.CurrentStatus.Should().Be("Status 3");
    }

    #endregion

    #region Encryption Status Tests

    [TestMethod]
    public void UpdateEncryptionStatus_True_ShouldUpdateIsEncrypted()
    {
        _service.UpdateEncryptionStatus(true);

        _service.IsEncrypted.Should().BeTrue();
    }

    [TestMethod]
    public void UpdateEncryptionStatus_False_ShouldUpdateIsEncrypted()
    {
        _service.UpdateEncryptionStatus(true);
        _service.UpdateEncryptionStatus(false);

        _service.IsEncrypted.Should().BeFalse();
    }

    [TestMethod]
    public void UpdateEncryptionStatus_ShouldFireStatusChanged()
    {
        var eventFired = false;
        _service.StatusChanged += (s, e) => eventFired = true;

        _service.UpdateEncryptionStatus(true);

        eventFired.Should().BeTrue();
    }

    #endregion

    #region Event Tests

    [TestMethod]
    public void StatusChanged_MultipleSubscribers_ShouldNotifyAll()
    {
        var count = 0;
        _service.StatusChanged += (s, e) => count++;
        _service.StatusChanged += (s, e) => count++;
        _service.StatusChanged += (s, e) => count++;

        _service.SetStatus("Test");

        count.Should().Be(3);
    }

    [TestMethod]
    public void StatusChanged_NoSubscribers_ShouldNotThrow()
    {
        var action = () => _service.SetStatus("Test");

        action.Should().NotThrow();
    }

    #endregion
}
