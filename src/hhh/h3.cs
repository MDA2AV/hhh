using ioxide;
using ioxide.nghttp3;
using ioxide.ngtcp2;

namespace hhh;

// smol h3 server
public static class h3
{
    public static Task Serve(ushort port, Func<Nghttp3Request, Nghttp3Response> handler,
        CancellationToken cancellationToken = default)
        => Serve(new h3_config { Port = port }, handler, cancellationToken);
    
    public static Task Serve(h3_config config, Func<Nghttp3Request, Nghttp3Response> handler,
        CancellationToken cancellationToken = default)
        => Serve(config, request => new ValueTask<Nghttp3Response>(handler(request)), cancellationToken);
    
    public static async Task Serve(h3_config config, Func<Nghttp3Request, ValueTask<Nghttp3Response>> handler,
        CancellationToken cancellationToken = default) {
        (string certificatePath, string keyPath) = cert.Ensure(config.CertificatePath, config.KeyPath);

        using var engine = new QuicEngine(certificatePath, keyPath, (uint)config.LocalCidLength, config.Alpn,
            config.MaxSendRetentionBytes);

        var server = new ServerConfig {
            ReactorCount = config.Reactors,
            RingEntries = config.RingEntries,
            DualStack = config.DualStack,
            Tcp = null,                       // not listening on tcp, h3 only (QUIC is UDP)
            Udp = new UdpOptions {
                RecvSlots = config.UdpRecvSlots,
                Gro = config.Gro,
            },
            Quic = new QuicOptions {
                Port = config.Port,
                LocalCidLength = config.LocalCidLength,
                IdleTimeoutMs = config.IdleTimeoutMs,
                ConnectionFactory = engine.CreateFactory(),
            },
        };

        var threads = new Thread[server.ReactorCount];

        for (int i = 0; i < threads.Length; i++) {
            var reactor = new Reactor(i, server) {
                // buffered dispatch
                QuicHandle = (_, connection) =>
                    new Nghttp3Connection(connection).RunBufferedAsync(handler)
            };

            threads[i] = new Thread(reactor.Run) { Name = $"hhh-{i}", IsBackground = true };
            threads[i].Start();
        }

        // TODO:
        try {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }catch (OperationCanceledException) {
            // Cancellation is how this is meant to end.
        }
    }
}
