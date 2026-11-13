#region Usings
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
#endregion

namespace SF.App.Services.Updates;

/// <summary>
/// The release workflow signs SHA256SUMS.txt with The Sharp Factory's private key (ECDSA P-256, SHA-256, the DER
/// signature openssl writes) and publishes the signature as SHA256SUMS.txt.sig. The app trusts the checksums, and so
/// the download, only when that signature matches the public key built into it: a release someone else made, even
/// on the real GitHub page, is refused.
/// </summary>
internal static class UpdateSignature
{
    public const string SignatureFileName = UpdateChecker.ChecksumsFileName + ".sig";

    private const string _keyResourceName = "update-signing-key.pem";

    /// <summary>
    /// The public key (PEM) that release downloads are signed for: Packaging/update-signing-key.pem.
    /// </summary>
    public static string ReleaseKey
    {
        get
        {
            using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(_keyResourceName))
            {
                using(var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

    /// <summary>
    /// True when <paramref name="signature"/> is a signature of <paramref name="data"/> by the private key of
    /// <paramref name="publicKeyPem"/>. A signature that cannot be read is not valid.
    /// </summary>
    public static bool IsValid(byte[] data, byte[] signature, string publicKeyPem)
    {
        if(data == null || signature == null || string.IsNullOrWhiteSpace(publicKeyPem))
            return false;

        using(var key = ECDsa.Create())
        {
            key.ImportFromPem(publicKeyPem);
            try
            {
                return key.VerifyData(data, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
            }
            catch(CryptographicException)
            {
                return false;
            }
        }
    }
}
