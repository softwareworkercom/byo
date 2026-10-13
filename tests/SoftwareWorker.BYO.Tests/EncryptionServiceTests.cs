using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.Tests.Support;
using System.Security.Cryptography;
using System.Text;

namespace SoftwareWorker.BYO.Tests;

public sealed class EncryptionServiceTests : IDisposable
{
    private readonly TemporaryStorageScope _storage = new(isolateEncryptionKey: true);

    [Fact]
    public void EncryptAndDecrypt_ShouldRoundTripSmallValues_UsingDirectRsa()
    {
        var encrypted = EncryptionService.EncryptVaultEntry("small secret");

        Assert.Equal(0x00, Convert.FromBase64String(encrypted)[0]);
        Assert.Equal("small secret", EncryptionService.DecryptVaultEntry(encrypted));
    }

    [Fact]
    public void EncryptAndDecrypt_ShouldRoundTripLargeValues_UsingHybridEncryption()
    {
        var value = new string('x', 2000) + "✓";

        var encrypted = EncryptionService.EncryptVaultEntry(value);

        Assert.Equal(0x01, Convert.FromBase64String(encrypted)[0]);
        Assert.Equal(value, EncryptionService.DecryptVaultEntry(encrypted));
    }

    [Fact]
    public void Encrypt_ShouldProduceDifferentCiphertextForTheSameValue()
    {
        Assert.NotEqual(EncryptionService.EncryptVaultEntry("same"), EncryptionService.EncryptVaultEntry("same"));
    }

    [Fact]
    public void EncryptAndDecrypt_ShouldRejectEmptyValues()
    {
        Assert.Throws<ArgumentNullException>(() => EncryptionService.EncryptVaultEntry(""));
        Assert.Throws<ArgumentNullException>(() => EncryptionService.EncryptVaultEntry(null!));
        Assert.Throws<ArgumentNullException>(() => EncryptionService.DecryptVaultEntry(""));
    }

    [Fact]
    public void Decrypt_ShouldRejectUnknownEncryptionTypes()
    {
        var payload = Convert.ToBase64String([0x02, 1, 2, 3]);

        var exception = Assert.Throws<CryptographicException>(() => EncryptionService.DecryptVaultEntry(payload));

        Assert.Equal("Unknown encryption type: 2", exception.Message);
    }

    [Fact]
    public void Decrypt_ShouldFail_WhenTheValueWasEncryptedWithAnotherKey()
    {
        var otherKey = KeyManagementService.CreateEncryptionKey();

        var encrypted = EncryptionService.EncryptVaultEntry("value", otherKey);

        Assert.ThrowsAny<CryptographicException>(() => EncryptionService.DecryptVaultEntry(encrypted));
    }

    [Fact]
    public void Encrypt_ShouldAcceptLegacyXmlKeys_AndRejectBlankKeys()
    {
        using var rsa = RSA.Create(2048);
        var xmlKey = rsa.ToXmlString(includePrivateParameters: true);

        var encrypted = EncryptionService.EncryptVaultEntry("value", xmlKey);
        var decrypted = rsa.Decrypt(Convert.FromBase64String(encrypted)[1..], RSAEncryptionPadding.OaepSHA256);

        Assert.Equal("value", Encoding.UTF8.GetString(decrypted));
        Assert.Throws<CryptographicException>(() => EncryptionService.EncryptVaultEntry("value", "   "));
    }

    [Fact]
    public void SecureCompare_ShouldCompareInConstantTime()
    {
        Assert.True(EncryptionService.SecureCompare([1, 2, 3], [1, 2, 3]));
        Assert.False(EncryptionService.SecureCompare([1, 2, 3], [1, 2, 4]));
        Assert.False(EncryptionService.SecureCompare([1, 2], [1, 2, 3]));
    }

    [Fact]
    public void GenerateSecureRandomString_ShouldUseTheRequestedLengthAndAlphabet()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";

        var value = EncryptionService.GenerateSecureRandomString(64);

        Assert.Equal(64, value.Length);
        Assert.All(value, character => Assert.Contains(character, alphabet));
        Assert.Equal(string.Empty, EncryptionService.GenerateSecureRandomString(0));
        Assert.NotEqual(value, EncryptionService.GenerateSecureRandomString(64));
    }

    [Fact]
    public void KeyManagementService_ShouldCreateAndPersistPkcs8Keys()
    {
        var created = KeyManagementService.CreateEncryptionKey();
        Assert.StartsWith("pkcs8:", created);
        Assert.NotEmpty(Convert.FromBase64String(created["pkcs8:".Length..]));

        var stored = KeyManagementService.Get();
        Assert.NotNull(stored);
        Assert.StartsWith("pkcs8:", stored);
        Assert.Equal(stored, KeyManagementService.Get());

        KeyManagementService.Save(created);
        Assert.Equal(created, KeyManagementService.Get());

        KeyManagementService.Initialize();
        Assert.NotEqual(created, KeyManagementService.Get());
    }

    public void Dispose()
    {
        _storage.Dispose();
    }
}
