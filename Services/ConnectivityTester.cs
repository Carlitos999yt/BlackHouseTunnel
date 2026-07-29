using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NepTunnel.Services
{
    public static class ConnectivityTester
    {
        public const int TEST_PROBE_COUNT = 5;

        private static async Task<(bool alive, double rttMs)> IcmpPingAsync(string host)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 2000);
                if (reply.Status == IPStatus.Success)
                {
                    return (true, reply.RoundtripTime);
                }
                return (false, -1);
            }
            catch
            {
                return (false, -1);
            }
        }

        public static async Task RunConnectivityTestAsync(string host, int port, Action<string, string> logFn, bool isHostSide = false, int localServerPort = 0)
        {
            logFn("─── Connectivity Test ───", "info");
            logFn("  (For full tunnel verification, use Echo Test)", "dim");
            int passed = 0, warned = 0, failed = 0;

            logFn($"[1/4]  Resolving  {host} …", "warn");
            IPAddress targetIp;
            try
            {
                IPAddress[] addrs = await Dns.GetHostAddressesAsync(host);
                if (addrs.Length == 0)
                {
                    logFn($"  ✗  DNS FAILED — No addresses found for {host}", "err");
                    failed++;
                    CtSummary(logFn, passed, warned, failed);
                    logFn("───────────────────────", "dim");
                    return;
                }
                targetIp = addrs[0];
                logFn($"  ✓  DNS OK   {host} → {targetIp}", "ok");
                logFn("       (this only means the hostname exists — not that your tunnel is active)", "dim");
                passed++;
            }
            catch (Exception ex)
            {
                logFn($"  ✗  DNS FAILED — {ex.Message}", "err");
                failed++;
                CtSummary(logFn, passed, warned, failed);
                logFn("───────────────────────", "dim");
                return;
            }

            logFn($"[2/4]  ICMP ping → {targetIp} …", "warn");
            var (alive, pingRtt) = await IcmpPingAsync(targetIp.ToString());
            if (alive)
            {
                logFn($"  ✓  Relay server is reachable  (ping ≈ {pingRtt:F0} ms)", "ok");
                logFn("       (this proves the relay IP is up — not that your tunnel is forwarding)", "dim");
                passed++;
            }
            else
            {
                logFn($"  ✗  ICMP ping failed — {targetIp} unreachable", "err");
                logFn("    → Tunnel address may be wrong, or relay is offline", "err");
                logFn("    → (Some relays block ICMP — continue to check UDP)", "warn");
                warned++;
            }

            if (isHostSide)
            {
                int lp = localServerPort != 0 ? localServerPort : port;
                logFn($"[3/4]  Checking local Studio port {lp} …", "warn");
                try
                {
                    using var pb = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    pb.SendTimeout = 500;
                    pb.ReceiveTimeout = 500;
                    var localEp = new IPEndPoint(IPAddress.Loopback, lp);

                    pb.SendTo(new byte[] { 0xFF, 0x00, 0x00 }, localEp);
                    logFn($"  ✓  Port {lp} accepts UDP (no ICMP reject)", "ok");
                    passed++;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset || ex.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    logFn($"  ✗  Port {lp} ICMP unreachable — Studio not running or OS firewall blocking it", "err");
                    failed++;
                }
                catch (Exception ex)
                {
                    logFn($"  △  Port check inconclusive — {ex.Message}", "warn");
                    warned++;
                }
            }
            else
            {
                logFn($"[3/4]  Checking local proxy on 127.0.0.1:{UdpProxy.PROXY_PORT} …", "warn");
                if (UdpProxy.IsRunning)
                {
                    logFn($"  ✓  Proxy active on port {UdpProxy.PROXY_PORT}", "ok");
                    passed++;
                }
                else
                {
                    logFn("  ✗  Proxy is NOT running — Connect first", "err");
                    failed++;
                }
            }

            IPAddress destIp = isHostSide ? targetIp : IPAddress.Loopback;
            int destPort = isHostSide ? port : UdpProxy.PROXY_PORT;

            logFn($"[4/4]  UDP probe burst → {destIp}:{destPort} ({TEST_PROBE_COUNT} packets) …", "warn");
            int sentOk = 0;
            bool icmpErr = false;
            try
            {
                using var pb2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                pb2.SendTimeout = 350;
                pb2.ReceiveTimeout = 350;
                var destEp = new IPEndPoint(destIp, destPort);

                var rng = new Random();
                for (int i = 0; i < TEST_PROBE_COUNT; i++)
                {
                    byte[] payload = new byte[16];
                    payload[0] = 0xFF;
                    payload[1] = 0x00;
                    payload[2] = 0xAA;
                    payload[3] = (byte)(i & 0xFF);
                    rng.NextBytes(new Span<byte>(payload, 4, 12));

                    try
                    {
                        pb2.SendTo(payload, destEp);
                        sentOk++;
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset || ex.SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        icmpErr = true;
                        break;
                    }
                    catch
                    {
                        break;
                    }

                    await Task.Delay(100);
                }
            }
            catch { }

            if (icmpErr)
            {
                logFn($"  ✗  ICMP Port Unreachable — port {destPort} is actively closed", "err");
                failed++;
            }
            else if (sentOk == TEST_PROBE_COUNT)
            {
                logFn($"  ✓  {sentOk}/{TEST_PROBE_COUNT} probes sent, no ICMP errors", "ok");
                logFn("  △  No reply expected here — use Echo Test to confirm end-to-end path", "dim");
                passed++;
            }
            else
            {
                logFn($"  ✗  Only {sentOk}/{TEST_PROBE_COUNT} probes sent", "err");
                failed++;
            }

            CtSummary(logFn, passed, warned, failed);
            logFn("───────────────────────", "dim");
        }

        private static void CtSummary(Action<string, string> logFn, int passed, int warned, int failed)
        {
            string verdict, tag;
            if (failed > 0)
            {
                verdict = "ISSUES DETECTED";
                tag = "err";
            }
            else if (warned > 0)
            {
                verdict = "PARTIALLY OK — check warnings";
                tag = "warn";
            }
            else
            {
                verdict = "ALL CLEAR";
                tag = "ok";
            }
            logFn($"Result: {verdict}  ({passed} passed · {warned} warnings · {failed} failed)", tag);
        }
    }
}
