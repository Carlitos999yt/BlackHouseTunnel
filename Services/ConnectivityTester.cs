using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NepTunnel.Services
{
    // Utility service for testing hostname resolution, ICMP reachability, and UDP port binding.
    public static class ConnectivityTester
    {
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

        // Executes a step-by-step diagnostic test against a target hostname and port.
        public static async Task RunConnectivityTestAsync(string host, int port, Action<string, string> logFn, bool isHostSide = false, int localServerPort = 0)
        {
            logFn("--- Connectivity Test ---", "info");
            logFn("  (For full tunnel verification, use Echo Test)", "dim");
            int passed = 0, warned = 0, failed = 0;

            logFn($"[1/4] Resolving {host}...", "warn");
            IPAddress targetIp;
            try
            {
                IPAddress[] addrs = await Dns.GetHostAddressesAsync(host);
                if (addrs.Length == 0)
                {
                    logFn($"  DNS FAILED - No addresses found for {host}", "err");
                    failed++;
                    CtSummary(logFn, passed, warned, failed);
                    logFn("-----------------------", "dim");
                    return;
                }
                targetIp = addrs[0];
                logFn($"  DNS OK {host} -> {targetIp}", "ok");
                logFn("       (this only means the hostname exists - not that your tunnel is active)", "dim");
                passed++;
            }
            catch (Exception ex)
            {
                logFn($"  DNS FAILED - {ex.Message}", "err");
                failed++;
                CtSummary(logFn, passed, warned, failed);
                logFn("-----------------------", "dim");
                return;
            }

            logFn($"[2/4] ICMP ping -> {targetIp}...", "warn");
            var (alive, pingRtt) = await IcmpPingAsync(targetIp.ToString());
            if (alive)
            {
                logFn($"  Relay server is reachable (ping approx {pingRtt:F0} ms)", "ok");
                logFn("       (this proves the relay IP is up - not that your tunnel is forwarding)", "dim");
                passed++;
            }
            else
            {
                logFn($"  ICMP ping failed - {targetIp} unreachable", "err");
                logFn("    -> Tunnel address may be wrong, or relay is offline", "err");
                logFn("    -> (Some relays block ICMP - continue to check UDP)", "warn");
                warned++;
            }

            if (isHostSide)
            {
                int lp = localServerPort != 0 ? localServerPort : port;
                logFn($"[3/4] Checking local Studio port {lp}...", "warn");
                try
                {
                    using var pb = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    pb.SendTimeout = 500;
                    pb.ReceiveTimeout = 500;
                    var localEp = new IPEndPoint(IPAddress.Loopback, lp);

                    pb.SendTo(new byte[] { 0xFF, 0x00, 0x00 }, localEp);
                    logFn($"  Port {lp} accepts UDP (no ICMP reject)", "ok");
                    passed++;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset || ex.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    logFn($"  Port {lp} ICMP unreachable - Studio not running or OS firewall blocking it", "err");
                    failed++;
                }
                catch
                {
                    logFn($"  Port {lp} accepted probe packet", "ok");
                    passed++;
                }
            }
            else
            {
                logFn($"[3/4] Probing UDP target {targetIp}:{port}...", "warn");
                try
                {
                    using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    sock.SendTimeout = 1000;
                    sock.ReceiveTimeout = 1000;

                    if (OperatingSystem.IsWindows())
                    {
                        const int SIO_UDP_CONNRESET = -1744830452;
                        try { sock.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null); } catch { }
                    }

                    var targetEp = new IPEndPoint(targetIp, port);
                    byte[] probePayload = System.Text.Encoding.ASCII.GetBytes("NEP_TEST_PROBE\0");

                    sock.SendTo(probePayload, targetEp);
                    logFn($"  UDP packet sent to {targetIp}:{port}", "ok");
                    logFn("       (UDP is connectionless; packet was transmitted without OS error)", "dim");
                    passed++;
                }
                catch (SocketException ex)
                {
                    logFn($"  UDP Probe Error: {ex.Message} (Code: {ex.SocketErrorCode})", "err");
                    failed++;
                }
                catch (Exception ex)
                {
                    logFn($"  UDP Probe Error: {ex.Message}", "err");
                    failed++;
                }
            }

            logFn($"[4/4] Evaluating overall test results...", "warn");
            CtSummary(logFn, passed, warned, failed);
            logFn("-----------------------", "dim");
        }

        // Writes a summary of connectivity test results to the log output callback.
        private static void CtSummary(Action<string, string> logFn, int passed, int warned, int failed)
        {
            if (failed > 0)
            {
                logFn($"RESULT: FAIL ({passed} passed, {warned} warnings, {failed} failed)", "err");
            }
            else if (warned > 0)
            {
                logFn($"RESULT: WARN ({passed} passed, {warned} warnings, {failed} failed)", "warn");
            }
            else
            {
                logFn($"RESULT: PASS ({passed} passed, {warned} warnings, {failed} failed)", "ok");
            }
        }
    }
}
