using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace hhh;

// basic cert machinery
// not supporting mTLS, needs extra api surface from ngtcp2
// TODO: mTLS ngtcp2 api surface
public static class cert {
    
    // return the given pair or generate a self-signed one under the temp directory
    public static (string CertificatePath, string KeyPath) Ensure(string? certificatePath, string? keyPath) {
        
        if (certificatePath is not null && keyPath is not null) {
            return (certificatePath, keyPath);
        }

        if (certificatePath is not null || keyPath is not null) {
            throw new ArgumentException(
                "hhh: set both CertificatePath and KeyPath, or neither (which generates a dev certificate).");
        }

        string directory = Path.Combine(Path.GetTempPath(), "hhh-dev-cert");
        Directory.CreateDirectory(directory);

        string certificateFile = Path.Combine(directory, "hhh.crt");
        string keyFile = Path.Combine(directory, "hhh.key");

        if (File.Exists(certificateFile) && File.Exists(keyFile)) {
            return (certificateFile, keyFile);
        }
        
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Modern clients read identity from the SAN and ignore CN entirely so without this the certificate names nobody :)
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("localhost");
        names.AddIpAddress(System.Net.IPAddress.Loopback);
        names.AddIpAddress(System.Net.IPAddress.IPv6Loopback);
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], false));

        using X509Certificate2 certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        File.WriteAllText(certificateFile, certificate.ExportCertificatePem());
        File.WriteAllText(keyFile, rsa.ExportPkcs8PrivateKeyPem());

        return (certificateFile, keyFile);
    }
}
