namespace hhh;

public sealed record h3_config {
    public ushort Port { get; init; } = 8443;
    public int Reactors { get; init; } = 1;
    public uint RingEntries { get; init; } = 8192;
    public bool DualStack { get; init; } = false;
    public int UdpRecvSlots { get; init; } = 16;
    public bool Gro { get; init; } = true;
    public int LocalCidLength { get; init; } = 8;
    public int IdleTimeoutMs { get; init; } = 60_000;
    public long MaxSendRetentionBytes { get; init; } = 16L << 20;
    public string[] Alpn { get; init; } = ["h3"];
    public string? CertificatePath { get; init; }
    public string? KeyPath { get; init; }
}
