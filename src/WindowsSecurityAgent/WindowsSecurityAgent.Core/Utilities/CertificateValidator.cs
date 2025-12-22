using System.Security.Cryptography.X509Certificates;

namespace WindowsSecurityAgent.Core.Utilities;

/// <summary>
/// Validates digital signatures and certificates
/// </summary>
public static class CertificateValidator
{
    /// <summary>
    /// Checks if a file is digitally signed
    /// </summary>
    public static bool IsFileSigned(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            using var certificate = new X509Certificate2(filePath);
            return certificate != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the publisher name from a signed file
    /// </summary>
    public static string? GetPublisher(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            using var certificate = new X509Certificate2(filePath);
            if (certificate == null)
                return null;

            // Extract the subject common name
            var subject = certificate.Subject;
            var cnStart = subject.IndexOf("CN=", StringComparison.OrdinalIgnoreCase);
            if (cnStart == -1)
                return subject;

            cnStart += 3;
            var cnEnd = subject.IndexOf(',', cnStart);
            if (cnEnd == -1)
                cnEnd = subject.Length;

            return subject.Substring(cnStart, cnEnd - cnStart).Trim();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the certificate thumbprint from a signed file
    /// </summary>
    public static string? GetCertificateThumbprint(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            using var certificate = new X509Certificate2(filePath);
            return certificate?.Thumbprint;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Validates if a certificate is trusted
    /// </summary>
    public static bool IsCertificateTrusted(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            using var certificate = new X509Certificate2(filePath);
            if (certificate == null)
                return false;

            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

            return chain.Build(certificate);
        }
        catch
        {
            return false;
        }
    }
}
