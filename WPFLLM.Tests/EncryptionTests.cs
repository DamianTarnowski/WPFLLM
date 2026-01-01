using Microsoft.VisualStudio.TestTools.UnitTesting;
using WPFLLM.Services;
using System;

namespace WPFLLM.Tests;

[TestClass]
public class EncryptionTests
{
    [TestMethod]
    public void EncryptionRoundtripTest()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        var service = new EncryptionService();
        service.SetEnabled(true);
        var plain = "TestSecret123";
        var encrypted = service.Encrypt(plain);
        var decrypted = service.Decrypt(encrypted);
        
        Assert.AreEqual(plain, decrypted);
    }

    [TestMethod]
    public void EncryptionDisabledTest()
    {
        var service = new EncryptionService();
        service.SetEnabled(false);
        var plain = "TestSecret123";
        var result = service.Encrypt(plain);
        
        Assert.AreEqual(plain, result);
    }
}
