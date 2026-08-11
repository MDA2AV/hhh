using hhh;
using ioxide.nghttp3;

// Making the certificate yourself, with your own parameters.
// curl --http3 --cacert /tmp/hhh-cert/server.crt https://127.0.0.1:8443/      verifies
// curl --http3 -k                                https://127.0.0.1:8443/      skips

(string certificate, string key) = self_signed_cert.Create(
    new cert_options {
        CommonName = "hhh.local",
        DnsNames = ["hhh.local", "localhost"],
        IpAddresses = ["127.0.0.1"],
        Lifetime = TimeSpan.FromDays(30),
    },
    certificatePath: "/tmp/hhh-cert/server.crt",
    keyPath: "/tmp/hhh-cert/server.key");

Console.WriteLine($"[hhh] generated {certificate}");

await h3.Serve(
    new h3_config { Port = 8443, Reactors = 1, CertificatePath = certificate, KeyPath = key },
    request => {
        var response = new Nghttp3Response {
            Status = 200,
            Body = "served with a certificate we made\n"u8.ToArray(),
        };

        response.Headers.Add("content-type"u8.ToArray(), "text/plain; charset=utf-8"u8.ToArray());

        return response;
    });
