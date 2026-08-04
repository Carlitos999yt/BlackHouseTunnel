using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NepTunnel.Services;

public static class EchoClient
{
	public static async Task RunEchoTestAsync(Action<string, string> logFn, string host, int port, int probeCount = 5)
	{
		logFn($"=== RUNNING ECHO TEST TO {host}:{port} ===", "info");
		logFn($"Sending {probeCount} test packets directly to tunnel...", "dim");
		IPAddress address;
		try
		{
			IPAddress[] array = await Dns.GetHostAddressesAsync(host);
			if (array.Length == 0)
			{
				logFn("DNS error: Could not resolve " + host, "err");
				return;
			}
			address = array[0];
		}
		catch (Exception ex)
		{
			logFn("DNS error resolving " + host + ": " + ex.Message, "err");
			return;
		}
		using Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		sock.SendTimeout = 2000;
		sock.ReceiveTimeout = 2000;
		if (OperatingSystem.IsWindows())
		{
			try
			{
				sock.IOControl(-1744830452, new byte[1], null);
			}
			catch
			{
			}
		}
		IPEndPoint targetEp = new IPEndPoint(address, port);
		int received = 0;
		List<double> rtts = new List<double>();
		byte[] respBuf = new byte[512];
		for (int i = 1; i <= probeCount; i++)
		{
			try
			{
				Stopwatch sw = Stopwatch.StartNew();
				await sock.SendToAsync(EchoServer.ECHO_REQ, SocketFlags.None, targetEp);
				using CancellationTokenSource cts = new CancellationTokenSource(2000);
				EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
				try
				{
					SocketReceiveFromResult obj2 = await sock.ReceiveFromAsync(respBuf, SocketFlags.None, remoteEndPoint, cts.Token);
					sw.Stop();
					if (obj2.ReceivedBytes >= EchoServer.ECHO_RESP.Length)
					{
						bool flag = true;
						for (int j = 0; j < EchoServer.ECHO_RESP.Length; j++)
						{
							if (respBuf[j] != EchoServer.ECHO_RESP[j])
							{
								flag = false;
								break;
							}
						}
						if (flag)
						{
							received++;
							double totalMilliseconds = sw.Elapsed.TotalMilliseconds;
							rtts.Add(totalMilliseconds);
							logFn($"  Probe {i}/{probeCount}: ECHO OK! rtt={totalMilliseconds:F1}ms", "ok");
						}
						else
						{
							logFn($"  Probe {i}/{probeCount}: Received unknown data", "warn");
						}
					}
				}
				catch (OperationCanceledException)
				{
					logFn($"  Probe {i}/{probeCount}: TIMEOUT (no response in 2.0s)", "err");
				}
			}
			catch (Exception ex3)
			{
				logFn($"  Probe {i}/{probeCount}: Error sending - {ex3.Message}", "err");
			}
			if (i < probeCount)
			{
				await Task.Delay(400);
			}
		}
		logFn("---------------------------------------", "dim");
		if (received > 0)
		{
			double value = ((rtts.Count > 0) ? rtts.Average() : 0.0);
			logFn($"ECHO SUCCESSFUL! Received {received}/{probeCount} responses (avg {value:F1}ms)", "ok");
			logFn("Your tunnel is active and accepting incoming UDP traffic.", "ok");
		}
		else
		{
			logFn($"ECHO FAILED: Received 0/{probeCount} responses.", "err");
			logFn("Possible causes:", "warn");
			logFn("  1. Host has not started session or Echo Server yet", "warn");
			logFn("  2. Host firewall is blocking UDP port", "warn");
			logFn("  3. Tunnel address or port is wrong", "warn");
		}
	}
}
