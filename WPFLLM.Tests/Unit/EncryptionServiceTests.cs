using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using WPFLLM.Services;

namespace WPFLLM.Tests.Unit;

/// <summary>
/// Comprehensive tests for EncryptionService covering AES-GCM encryption and DPAPI key protection.
/// </summary>
[TestClass]
public class EncryptionServiceTests
{
    private EncryptionService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        if (!OperatingSystem.IsWindows()) return;
        _service = new EncryptionService();
    }

    #region Enable/Disable Tests

    [TestMethod]
    public void IsEnabled_DefaultsFalse()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.IsEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void SetEnabled_True_ShouldEnableEncryption()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        
        _service.IsEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void SetEnabled_False_ShouldDisableEncryption()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        _service.SetEnabled(false);
        
        _service.IsEnabled.Should().BeFalse();
    }

    #endregion

    #region String Encryption Tests

    [TestMethod]
    public void Encrypt_WhenDisabled_ShouldReturnOriginal()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(false);
        var original = "Test String";

        var result = _service.Encrypt(original);

        result.Should().Be(original);
    }

    [TestMethod]
    public void Encrypt_WhenEnabled_ShouldReturnDifferentString()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        var original = "Test String";

        var encrypted = _service.Encrypt(original);

        encrypted.Should().NotBe(original);
    }

    [TestMethod]
    public void Encrypt_ShouldReturnBase64()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        var original = "Test String";

        var encrypted = _service.Encrypt(original);

        // Base64 should be decodable
        var action = () => Convert.FromBase64String(encrypted);
        action.Should().NotThrow();
    }

    [TestMethod]
    public void Encrypt_EmptyString_ShouldReturnEmpty()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);

        var result = _service.Encrypt("");

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void Encrypt_NullString_ShouldReturnNull()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);

        var result = _service.Encrypt(null!);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Encrypt_SameInputTwice_ShouldProduceDifferentOutputs()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        var original = "Test String";

        var encrypted1 = _service.Encrypt(original);
        var encrypted2 = _service.Encrypt(original);

        // Due to random nonce, same input should produce different ciphertext
        encrypted1.Should().NotBe(encrypted2);
    }

    #endregion

    #region String Decryption Tests

    [TestMethod]
    public void Decrypt_WhenDisabled_ShouldReturnOriginal()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(false);
        var original = "Test String";

        var result = _service.Decrypt(original);

        result.Should().Be(original);
    }

    [TestMethod]
    public void Decrypt_EncryptedString_ShouldReturnOriginal()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        var original = "Secret Message 123!@#";

        var encrypted = _service.Encrypt(original);
        var decrypted = _service.Decrypt(encrypted);

        decrypted.Should().Be(original);
    }

    [TestMethod]
    public void Decrypt_NonBase64String_ShouldReturnAsIs()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        var notBase64 = "This is not base64!!!";

        var result = _service.Decrypt(notBase64);

        result.Should().Be(notBase64);
    }

    [TestMethod]
    public void Decrypt_EmptyString_ShouldReturnEmpty()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);

        var result = _service.Decrypt("");

        result.Should().BeEmpty();
    }

    #endregion

    #region Byte Array Encryption Tests

    [TestMethod]
    public void EncryptBytes_WhenDisabled_ShouldReturnOriginal()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(false);
        var original = new byte[] { 1, 2, 3, 4, 5 };

        var result = _service.EncryptBytes(original);

        result.Should().BeEquivalentTo(original);
    }

    [TestMethod]
    public void EncryptBytes_WhenEnabled_ShouldReturnLargerArray()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        var original = new byte[] { 1, 2, 3, 4, 5 };

        var encrypted = _service.EncryptBytes(original);

        // Encrypted = nonce (12) + tag (16) + ciphertext (same as original)
        encrypted.Length.Should().Be(original.Length + 12 + 16);
    }

    [TestMethod]
    public void EncryptBytes_EmptyArray_ShouldReturnEmpty()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);

        var result = _service.EncryptBytes(Array.Empty<byte>());

        result.Should().BeEmpty();
    }

    #endregion

    #region Byte Array Decryption Tests

    [TestMethod]
    public void DecryptBytes_WhenDisabled_ShouldReturnOriginal()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(false);
        var original = new byte[] { 1, 2, 3, 4, 5 };

        var result = _service.DecryptBytes(original);

        result.Should().BeEquivalentTo(original);
    }

    [TestMethod]
    public void DecryptBytes_EncryptedData_ShouldReturnOriginal()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        var original = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        var encrypted = _service.EncryptBytes(original);
        var decrypted = _service.DecryptBytes(encrypted);

        decrypted.Should().BeEquivalentTo(original);
    }

    [TestMethod]
    public void DecryptBytes_TooShortArray_ShouldReturnAsIs()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        // Array shorter than nonce + tag (28 bytes)
        var tooShort = new byte[] { 1, 2, 3, 4, 5 };

        var result = _service.DecryptBytes(tooShort);

        result.Should().BeEquivalentTo(tooShort);
    }

    #endregion

    #region Roundtrip Tests

    [TestMethod]
    public void Roundtrip_UnicodeString_ShouldPreserve()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        var original = "Polskie znaki: ąćęłńóśźż ĄĆĘŁŃÓŚŹŻ 日本語 🎉🚀";

        var encrypted = _service.Encrypt(original);
        var decrypted = _service.Decrypt(encrypted);

        decrypted.Should().Be(original);
    }

    [TestMethod]
    public void Roundtrip_LongString_ShouldPreserve()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        var original = new string('A', 10000);

        var encrypted = _service.Encrypt(original);
        var decrypted = _service.Decrypt(encrypted);

        decrypted.Should().Be(original);
    }

    [TestMethod]
    public void Roundtrip_ApiKey_ShouldPreserve()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        var apiKey = "sk-proj-abc123XYZ_very-secret-api-key-12345";

        var encrypted = _service.Encrypt(apiKey);
        var decrypted = _service.Decrypt(encrypted);

        decrypted.Should().Be(apiKey);
    }

    [TestMethod]
    public void Roundtrip_BinaryData_ShouldPreserve()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        var original = new byte[256];
        for (int i = 0; i < 256; i++) original[i] = (byte)i;

        var encrypted = _service.EncryptBytes(original);
        var decrypted = _service.DecryptBytes(encrypted);

        decrypted.Should().BeEquivalentTo(original);
    }

    [TestMethod]
    public void Roundtrip_LargeBinaryData_ShouldPreserve()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        _service.SetEnabled(true);
        var original = new byte[100000];
        new Random(42).NextBytes(original);

        var encrypted = _service.EncryptBytes(original);
        var decrypted = _service.DecryptBytes(encrypted);

        decrypted.Should().BeEquivalentTo(original);
    }

    #endregion
}
