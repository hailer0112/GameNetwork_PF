using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

// Same position protocol is used by interactive commands and load tests.
record Position(string Peer, long Sequence, int X, int Y)
{
    public string Encode() => FormattableString.Invariant($"POS1|{Peer}|{Sequence}|{X}|{Y}");
    public static bool TryParse(string text, out Position? value)
    {
        value = null;
        var f = text.Split('|');
        if (f.Length != 5 || f[0] != "POS1" || !ValidId(f[1]) ||
            !long.TryParse(f[2], NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n < 1 ||
            !int.TryParse(f[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(f[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y)) return false;
        value = new(f[1], n, x, y);
        return true;
    }
    public static bool ValidId(string id) => id.Length is > 0 and <= 32 && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length != 5 || !Position.ValidId(args[0]) || !IPAddress.TryParse(args[1], out var local) ||
            !int.TryParse(args[2], out var localPort) || localPort is < 1 or > 65535 ||
            !IPAddress.TryParse(args[3], out var remote) || !int.TryParse(args[4], out var remotePort) || remotePort is < 1 or > 65535)
        {
            Console.WriteLine("Usage: UdpPeer <id> <local-ip> <local-port> <remote-ip> <remote-port>");
            return 1;
        }
        try { await Run(args[0], new(local, localPort), new(remote, remotePort)); return 0; }
        catch (SocketException e) { Console.Error.WriteLine($"Socket error: {e.SocketErrorCode}"); return 2; }
    }

    static async Task Run(string id, IPEndPoint local, IPEndPoint remote)
    {
        using var udp = new UdpClient(local);
        using var stop = new CancellationTokenSource();
        var gate = new object();
        var peers = new Dictionary<string, Position>();
        var history = new Dictionary<long, Position>();
        long sequence = 0, sent = 0, received = 0, rejected = 0, ignored = 0;
        int x = 0, y = 0;
        var clock = Stopwatch.StartNew();
        void Log(string message) { lock (gate) Console.WriteLine($"[{clock.Elapsed.TotalSeconds:F3}s] {message}"); }
        void Status()
        {
            lock (gate)
            {
                Log($"SELF {id} ({x},{y}) seq={sequence} sent={sent} received={received} rejected={rejected} ignored={ignored}");
                if (peers.Count == 0) Log("REMOTE: not received");
                foreach (var p in peers.Values) Log($"REMOTE {p.Peer} ({p.X},{p.Y}) seq={p.Sequence}");
            }
        }
        async Task Send(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            await udp.SendAsync(bytes, remote, stop.Token);
            sent++;
        }
        async Task Move(int nx, int ny)
        {
            x = nx; y = ny;
            var p = new Position(id, checked(++sequence), x, y);
            history[p.Sequence] = p;
            if (history.Count > 20000) history.Remove(history.Keys.Min());
            await Send(p.Encode());
        }
        var receiver = Task.Run(async () =>
        {
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    var packet = await udp.ReceiveAsync(stop.Token);
                    lock (gate)
                    {
                        received++;
                        if (!packet.RemoteEndPoint.Equals(remote) || packet.Buffer.Length > 256 ||
                            !Position.TryParse(Encoding.UTF8.GetString(packet.Buffer), out var p) || p!.Peer == id)
                        { rejected++; Log("REJECT malformed, unexpected endpoint, or own peer ID"); continue; }
                        if (peers.TryGetValue(p.Peer, out var previous) && p.Sequence <= previous.Sequence)
                        { ignored++; Log($"IGNORE {p.Peer} seq={p.Sequence} latest={previous.Sequence}"); continue; }
                        peers[p.Peer] = p;
                        Log($"APPLY {p.Peer} ({p.X},{p.Y}) seq={p.Sequence}");
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (SocketException e) { if (!stop.IsCancellationRequested) Log($"RECEIVE ERROR {e.SocketErrorCode}"); }
            }
        });
        Log($"READY {id} local={local} remote={remote}");
        Log("Commands: move X Y | resend SEQ | raw TEXT | load COUNT INTERVAL_MS | status | quit");
        Status();
        try
        {
            while (await Console.In.ReadLineAsync() is { } line)
            {
                var f = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (f.Length == 0) continue;
                if (f[0] == "quit") break;
                try
                {
                    if (f.Length == 3 && f[0] == "move" && int.TryParse(f[1], out var nx) && int.TryParse(f[2], out var ny))
                    { await Move(nx, ny); Status(); }
                    else if (f.Length == 2 && f[0] == "resend" && long.TryParse(f[1], out var n) && history.TryGetValue(n, out var old))
                    { await Send(old.Encode()); Log($"RESENT seq={n} ({old.X},{old.Y})"); }
                    else if (line.StartsWith("raw ", StringComparison.Ordinal)) await Send(line[4..]);
                    else if (f[0] == "status") Status();
                    else if (f.Length == 3 && f[0] == "load" && int.TryParse(f[1], out var count) && count is > 0 and <= 10000 &&
                        int.TryParse(f[2], out var interval) && interval is >= 0 and <= 1000)
                    {
                        var timer = Stopwatch.StartNew(); var before = sent;
                        for (var i = 0; i < count; i++) { await Move(i, -i); if (interval > 0) await Task.Delay(interval); }
                        Log($"LOAD count={count} intervalMs={interval} sentDelta={sent-before} elapsedMs={timer.Elapsed.TotalMilliseconds:F2}");
                        Status();
                    }
                    else Log("Invalid command or sequence not in last 20000 messages.");
                }
                catch (Exception e) when (e is SocketException or OverflowException) { Log($"SEND ERROR {e.Message}"); }
            }
        }
        finally { stop.Cancel(); await receiver; Status(); }
    }
}
