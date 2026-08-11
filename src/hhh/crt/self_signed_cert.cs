using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace hhh;

public sealed record cert_options {
    public string CommonName { get; init; } = "localhost";
    public IReadOnlyList<string> DnsNames { get; init; } = ["localhost"];
    public IReadOnlyList<string> IpAddresses { get; init; } = ["127.0.0.1", "::1"];
    public int RsaKeySize { get; init; } = 2048;
    public TimeSpan Lifetime { get; init; } = TimeSpan.FromDays(365);
    public bool AllowDirectTrust { get; init; } = true;
}


// Makes a certificate from cert_options and writes the PEM pair, no CA behind it, so a client either skips
// verification or is handed this certificate to trust directly
public static class self_signed_cert {
    public static (string CertificatePath, string KeyPath) Create(
        cert_options options, string certificatePath, string keyPath) {
        ArgumentNullException.ThrowIfNull(options);

        if (options.DnsNames.Count == 0 && options.IpAddresses.Count == 0) {
            throw new ArgumentException(
                "hhh: a certificate needs at least one DNS name or IP address.", nameof(options));
        }

        using var rsa = RSA.Create(options.RsaKeySize);
        var request = new CertificateRequest(
            $"CN={options.CommonName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var names = new SubjectAlternativeNameBuilder();
        foreach (string dns in options.DnsNames) {
            names.AddDnsName(dns);
        }
        
        foreach (string ip in options.IpAddresses) {
            names.AddIpAddress(IPAddress.Parse(ip));
        }
        
        request.CertificateExtensions.Add(names.Build());

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment |
                (options.AllowDirectTrust ? X509KeyUsageFlags.KeyCertSign : 0),
                critical: true));

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: options.AllowDirectTrust,
                hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
        
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], critical: false));

        using X509Certificate2 certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.Add(options.Lifetime));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(certificatePath))!);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(keyPath))!);

        File.WriteAllText(certificatePath, certificate.ExportCertificatePem());
        File.WriteAllText(keyPath, rsa.ExportPkcs8PrivateKeyPem());

        return (certificatePath, keyPath);
    }
}
