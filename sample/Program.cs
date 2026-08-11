using hhh;
using ioxide.nghttp3;

await h3.Serve(new h3_config {
    Port = 8443,
    Reactors = 1,
    RingEntries = 8192,
    DualStack = false,                      // true = one IPv6 socket also takes IPv4-mapped clients

    UdpRecvSlots = 16,                      // datagrams the UDP ring may have outstanding
    Gro = true,                             // kernel coalesces datagrams into one read

    LocalCidLength = 8,                     // bytes of connection ID to hand out on every packet
    IdleTimeoutMs = 60_000,                 // how long a connection may sit idle before QUIC closes it
    MaxSendRetentionBytes = 16L << 20,      // sent-but-unacked data held per connection for resends
    Alpn = ["h3"],

    // set to null, a cert will be generated automatically
    CertificatePath = null,
    KeyPath = null,
}, 
    request => {
    bool root = request.Path.Span.SequenceEqual("/"u8);

    var response = new Nghttp3Response {
        Status = 200,
        Body = root ? StaticData.Greeting : request.Body,
    };

    response.Headers.Add(StaticData.ContentType, StaticData.TextPlain);
    response.Headers.Add(StaticData.ServedBy, StaticData.Name);

    return response;
});

static class StaticData {
    public static readonly byte[] ContentType = [.. "content-type"u8];
    public static readonly byte[] TextPlain = [.. "text/plain; charset=utf-8"u8];
    public static readonly byte[] ServedBy = [.. "x-served-by"u8];
    public static readonly byte[] Name = [.. "hhh"u8];
    public static readonly byte[] Greeting = [.. "hello from h3\n"u8];
}
