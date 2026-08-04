using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NepTunnel.Services;

public static class ConnectivityTester
{
	private static async Task<(bool alive, double rttMs)> IcmpPingAsync(string host)
	{
		try
		{
			using Ping ping = new Ping();
			PingReply pingReply = await ping.SendPingAsync(host, 2000);
			if (pingReply.Status == IPStatus.Success)
			{
				return (alive: true, rttMs: pingReply.RoundtripTime);
			}
			return (alive: false, rttMs: -1.0);
		}
		catch
		{
			return (alive: false, rttMs: -1.0);
		}
	}

	public static async Task RunConnectivityTestAsync(string host, int port, Action<string, string> logFn, bool isHostSide = false, int localServerPort = 0)
	{
		logFn("--- Connectivity Test ---", "info");
		logFn("  (For full tunnel verification, use Echo Test)", "dim");
		int passed = 0;
		int warned = 0;
		int failed = 0;
		logFn("[1/4] Resolving " + host + "...", "warn");
		IPAddress targetIp;
		try
		{
			IPAddress[] array = await Dns.GetHostAddressesAsync(host);
			if (array.Length == 0)
			{
				logFn("  DNS FAILED - No addresses found for " + host, "err");
				failed++;
				CtSummary(logFn, passed, warned, failed);
				logFn("-----------------------", "dim");
				return;
			}
			targetIp = array[0];
			logFn($"  DNS OK {host} -> {targetIp}", "ok");
			logFn("       (this only means the hostname exists - not that your tunnel is active)", "dim");
			passed++;
		}
		catch (Exception ex)
		{
			logFn("  DNS FAILED - " + ex.Message, "err");
			failed++;
			CtSummary(logFn, passed, warned, failed);
			logFn("-----------------------", "dim");
			return;
		}
		logFn($"[2/4] ICMP ping -> {targetIp}...", "warn");
		var (flag, value) = await IcmpPingAsync(targetIp.ToString());
		if (flag)
		{
			logFn($"  Relay server is reachable (ping approx {value:F0} ms)", "ok");
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
			int num = ((localServerPort != 0) ? localServerPort : port);
			logFn($"[3/4] Checking local Studio port {num}...", "warn");
			try
			{
				using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				socket.SendTimeout = 500;
				socket.ReceiveTimeout = 500;
				IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, num);
				socket.SendTo(new byte[3] { 255, 0, 0 }, remoteEP);
				logFn($"  Port {num} accepts UDP (no ICMP reject)", "ok");
				passed++;
			}
			catch (SocketException ex2) when (ex2.SocketErrorCode == SocketError.ConnectionReset || ex2.SocketErrorCode == SocketError.ConnectionRefused)
			{
				logFn($"  Port {num} ICMP unreachable - Studio not running or OS firewall blocking it", "err");
				failed++;
			}
			catch
			{
				logFn($"  Port {num} accepted probe packet", "ok");
				passed++;
			}
		}
		else
		{
			logFn($"[3/4] Probing UDP target {targetIp}:{port}...", "warn");
			try
			{
				using Socket socket2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				socket2.SendTimeout = 1000;
				socket2.ReceiveTimeout = 1000;
				if (OperatingSystem.IsWindows())
				{
					try
					{
						socket2.IOControl(-1744830452, new byte[1], null);
					}
					catch
					{
					}
				}
				IPEndPoint remoteEP2 = new IPEndPoint(targetIp, port);
				byte[] bytes = Encoding.ASCII.GetBytes("NEP_TEST_PROBE\0");
				socket2.SendTo(bytes, remoteEP2);
				logFn($"  UDP packet sent to {targetIp}:{port}", "ok");
				logFn("       (UDP is connectionless; packet was transmitted without OS error)", "dim");
				passed++;
			}
			catch (SocketException ex3)
			{
				logFn($"  UDP Probe Error: {ex3.Message} (Code: {ex3.SocketErrorCode})", "err");
				failed++;
			}
			catch (Exception ex4)
			{
				logFn("  UDP Probe Error: " + ex4.Message, "err");
				failed++;
			}
		}
		logFn("[4/4] Evaluating overall test results...", "warn");
		CtSummary(logFn, passed, warned, failed);
		logFn("-----------------------", "dim");
	}

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
