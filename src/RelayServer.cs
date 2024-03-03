using Lidgren.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MMXOnline;

public class RelayServer {
	static TcpListener server = null;

	static NetPeerConfiguration config;
	static NetServer netServer;

	const string banListFile = "banlist.json";
	const string overrideVersionFile = "overrideversion.txt";
	const string encryptionKeyFile = "encryptionKey.txt";
	const string secretPrefixFile = "secretPrefix.txt";

	static void updateServer(bool isFirstTime) {
		try {
			if (File.Exists(banListFile)) {
				var banListJson = File.ReadAllText(banListFile);
				Global.banList = JsonConvert.DeserializeObject<List<BanEntry>>(banListJson);
				if (Global.banList == null) {
					Global.banList = new List<BanEntry>();
				}
				if (!isFirstTime) Console.WriteLine("Updated ban list...");
			}

			if (File.Exists(overrideVersionFile)) {
				string overrideVersionStr = File.ReadAllText(overrideVersionFile);
				overrideVersionStr = overrideVersionStr.Replace(",", ".");
				if (decimal.TryParse(
					overrideVersionStr, NumberStyles.Any, CultureInfo.InvariantCulture,
					out decimal overrideVersion
				)) {
					if (Helpers.compareVersions(overrideVersion, Global.version) == 1) {
						Global.version = overrideVersion;
						if (!isFirstTime) Console.WriteLine(
							string.Format("Updated version to v{0}...", Global.version)
						);
					}
				}
			}
		} catch (Exception e) {
			Console.WriteLine("updateServer failed. Exception: " + e.Message);
		}
	}

	public static void ServerMain(string[] args) {
		updateServer(true);

		Console.WriteLine(string.Format("Starting matchmaking server (v{0})...", Global.version));

		if (File.Exists(encryptionKeyFile)) {
			Global.encryptionKey = File.ReadAllText(encryptionKeyFile);
			if (string.IsNullOrEmpty(Global.encryptionKey)) {
				throw new Exception("encryption key can't be blank.");
			}
			Console.WriteLine(string.Format("Successfully read encryption key from encryptionKey.txt"));
		} else {
			Console.WriteLine(
				string.Format("No encryptionKey.txt file found. " +
				"Reports and bans will not work. If you want this functionality, " +
				"you should create one with a secure string as its content.")
			);
		}

		if (File.Exists(secretPrefixFile)) {
			Global.secretPrefix = File.ReadAllText(secretPrefixFile);
			if (string.IsNullOrEmpty(Global.secretPrefix)) {
				throw new Exception("secret prefix can't be blank.");
			}
			Console.WriteLine(string.Format("Successfully read secret prefix from secretPrefix.txt"));
		} else {
			Console.WriteLine(
				string.Format(
					"No secretPrefix.txt file found. " +
					"You should create one with a secure string as its content " +
					"if you are running this as an internet server, or it will be less secure. " +
					"Ignore if you are running this as a LAN relay server on a local area network."
				)
			);
		}

		Thread thread = new Thread(udpMain);
		thread.Start();
		server = new TcpListener(IPAddress.Any, Global.basePort);
		server.Start();

		while (true) {
			iteration();
		}
	}

	static void iteration() {
		using (TcpClient client = server.AcceptTcpClient()) {
			using NetworkStream networkStream = client.GetStream();

			client.ReceiveTimeout = 5000;
			client.SendTimeout = 5000;

			string message = "";
			try {
				message = client.ReadStringMessage(networkStream);
			} catch {
				client.Close();
				return;
			}
			Helpers.debugLog("Client sent data message: " + message);

			if (message.StartsWith("CheckBan:")) {
				msgCheckBan(client, networkStream, message);
			} else if (message == "GetServers") {
				msgGetServers(client, networkStream);
			} else if (message.StartsWith("GetServer:")) {
				msgGetServerEX(client, networkStream, message);
			} else if (message == "GetVersion") {
				msgGetVersion(client, networkStream);
			} else if (message.StartsWith("CreateServer")) {
				msgCreateServer(client, networkStream, message);
			} else if (Global.secretPrefix != null && message.StartsWith(Global.secretPrefix)) {
				if (message == Global.secretPrefix + "updateserver") {
					updateServer(isFirstTime: false);
				} else if (message == Global.secretPrefix + "getbanlist") {
					msgGetBanList(client, networkStream);
				} else if (message.StartsWith(Global.secretPrefix + "updatebanlist")) {
					msgUpdateBanList(client, networkStream, message);
				} else if (message.StartsWith(Global.secretPrefix + "updateversion")) {
					msgUpdateVersion(client, networkStream, message);
				} else if (message.StartsWith(Global.secretPrefix + "removeallmatches")) {
					msgRemoveAllMatches(client, networkStream);
				} else if (message.StartsWith(Global.secretPrefix + "getbanstatusdatablob")) {
					msgUnbanDatablob(client, networkStream, message);
				} else if (message.StartsWith(Global.secretPrefix + "bandatablob")) {
					msgBanDatablob(client, networkStream, message);
				} else if (message.StartsWith(Global.secretPrefix + "unbandatablob")) {
					msgUnbanDatablob(client, networkStream, message);
				}
			}
		}
	}

	private static void msgGetServers(TcpClient client, NetworkStream networkStream) {
		byte[] serverBytes = Helpers.serialize(Server.servers.Keys.ToList());
		client.SendMessage(serverBytes, networkStream);
	}

	private static void msgGetServerEX(TcpClient client, NetworkStream networkStream, string message) {
		string serverName = message.RemovePrefix("GetServer:");
		Server server = null;
		foreach (KeyValuePair<Server, bool> s in Server.servers) {
			if (s.Key.name == serverName) {
				server = s.Key;
				break;
			}
		}
		byte[] serverBytes = new byte[0];
		if (server != null) {
			serverBytes = Helpers.serialize(server);
		}
		client.SendMessage(serverBytes, networkStream);
	}

	private static void msgCheckBan(TcpClient client, NetworkStream networkStream, string message) {
		string deviceId = message.RemovePrefix("CheckBan:");
		string ipAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
		BanEntry banEntry = Global.banList.FirstOrDefault((BanEntry b) => b.isBanned(ipAddress, deviceId, 0));
		if (banEntry == null) {
			banEntry = Global.banList.FirstOrDefault((BanEntry b) => b.isBanned(ipAddress, deviceId, 1));
		}
		if (banEntry == null) {
			banEntry = Global.banList.FirstOrDefault((BanEntry b) => b.isBanned(ipAddress, deviceId, 2));
		}
		if (banEntry != null) {
			string banData = JsonConvert.SerializeObject(banEntry);
			client.SendStringMessage("CheckBan:" + banData, networkStream);
		} else {
			client.SendStringMessage("CheckBan:", networkStream);
		}
	}

	private static void msgGetVersion(TcpClient client, NetworkStream networkStream) {
		client.SendStringMessage("GetVersion:" + Global.version, networkStream);
	}

	private static void msgCreateServer(TcpClient client, NetworkStream networkStream, string message) {
		string requestServerJson = message.RemovePrefix("CreateServer:");
		Server requestServer = JsonConvert.DeserializeObject<Server>(requestServerJson);
		Server server = Server.servers.Keys.Where((Server s) => s.name == requestServer.name).FirstOrDefault();
		if (Helpers.compareVersions(requestServer.gameVersion, Global.version) == -1) {
			client.SendStringMessage("CreateServer:fail:Outdated game version (update to v" + Global.version + ")", networkStream);
		} else if (Server.servers.Count >= 5) {
			client.SendStringMessage("CreateServer:fail:Too many concurrent servers (max " + Server.servers.Count + ")", networkStream);
		} else if (server == null) {
			bool sameUserMatch = false;
			foreach (Server key in Server.servers.Keys) {
				string myServerIp = key?.host?.connection?.RemoteEndPoint?.Address?.ToString();
				string connectingIp = ((IPEndPoint)(client.Client?.RemoteEndPoint))?.Address?.ToString();
				if (myServerIp == connectingIp) {
					sameUserMatch = true;
					break;
				}
			}
			if (sameUserMatch) {
				client.SendStringMessage("CreateServer:fail:Same user can't create more than 1 match", networkStream);
				return;
			}
			server = new Server(
				Global.version, requestServer.region, requestServer.name,
				requestServer.level, requestServer.shortLevelName, requestServer.gameMode,
				requestServer.playTo, requestServer.botCount, requestServer.maxPlayers,
				requestServer.timeLimit.GetValueOrDefault(), requestServer.fixedCamera,
				requestServer.hidden, requestServer.netcodeModel, requestServer.netcodeModelPing,
				requestServer.isLAN, requestServer.mirrored, requestServer.useLoadout, requestServer.gameChecksum,
				requestServer.customMapChecksum, requestServer.customMapUrl, requestServer.extraCpuCharData,
				requestServer.customMatchSettings, requestServer.disableHtSt, requestServer.disableVehicles
			);
			server.teamNum = requestServer.teamNum;
			server.start();
			Server.servers[server] = true;
			client.SendStringMessage("CreateServer:" + JsonConvert.SerializeObject(server), networkStream);
		} else {
			client.SendStringMessage("CreateServer:fail:Server name already exists", networkStream);
		}
	}

	private static void msgGetBanList(TcpClient client, NetworkStream networkStream) {
		try {
			string banListJson = File.ReadAllText("banlist.json");
			client.SendStringMessage("getbanlist:" + banListJson, networkStream);
		} catch (Exception ex) {
			client.SendStringMessage("getbanlist:fail:" + ex.GetType().Name, networkStream);
		}
	}

	private static void msgUpdateBanList(TcpClient client, NetworkStream networkStream, string message) {
		string banListJson = message.RemovePrefix(Global.secretPrefix + "updatebanlist:");
		try {
			JsonConvert.DeserializeObject<List<BanEntry>>(banListJson);
			File.WriteAllText("banlist.json", banListJson);
			updateServer(isFirstTime: false);
		} catch (Exception ex) {
			client.SendStringMessage("updatebanlist:fail:" + ex.GetType().Name, networkStream);
		}
		client.SendStringMessage("updatebanlist:Success", networkStream);
	}

	private static void msgUpdateVersion(TcpClient client, NetworkStream networkStream, string message) {
		string versionString = message.RemovePrefix(Global.secretPrefix + "updateversion:");
		try {
			decimal version = decimal.Parse(versionString);
			File.WriteAllText("overrideversion.txt", versionString);
			updateServer(isFirstTime: false);
		} catch (Exception ex) {
			client.SendStringMessage("updateversion:fail:" + ex.GetType().Name, networkStream);
		}
		client.SendStringMessage("updateversion:Success", networkStream);
	}

	private static void msgRemoveAllMatches(TcpClient client, NetworkStream networkStream) {
		foreach (Server server in Server.servers.Keys) {
			lock (server) {
				server.killServer = true;
			}
		}
		client.SendStringMessage("removeallmatches:Success:", networkStream);
	}

	private static void msgGetBanStatusDatablob(TcpClient client, NetworkStream networkStream, string message) {
		try {
			string dataBlobStr = message.RemovePrefix(Global.secretPrefix + "getbanstatusdatablob:");
			ReportedPlayerDataBlob dataBlob = JsonConvert.DeserializeObject<ReportedPlayerDataBlob>(AesOperation.DecryptString(Global.encryptionKey, dataBlobStr));
			BanEntry bannedPlayer = Global.banList.FirstOrDefault((BanEntry b) => b.ipAddress == dataBlob.ipAddress || b.deviceId == dataBlob.deviceId);
			if (bannedPlayer != null) {
				BanResponse bannedPlayerResponse = new BanResponse(bannedPlayer.banType, bannedPlayer.reason, bannedPlayer.bannedUntil);
				client.SendStringMessage("getbanstatusdatablob:Success:" + JsonConvert.SerializeObject(bannedPlayerResponse), networkStream);
			} else {
				client.SendStringMessage("getbanstatusdatablob:Success:", networkStream);
			}
		} catch (Exception ex) {
			client.SendStringMessage("getbanstatusdatablob:Error:" + ex.GetType().Name, networkStream);
		}
	}

	private static void msgBanDatablob(TcpClient client, NetworkStream networkStream, string message) {
		try {
			string requestJson = message.RemovePrefix(Global.secretPrefix + "bandatablob:");
			BanRequest banRequest = JsonConvert.DeserializeObject<BanRequest>(requestJson);
			ReportedPlayerDataBlob dataBlob = JsonConvert.DeserializeObject<ReportedPlayerDataBlob>(AesOperation.DecryptString(Global.encryptionKey, banRequest.dataBlobStr));
			string banListJson = File.ReadAllText("banlist.json");
			List<BanEntry> banList = JsonConvert.DeserializeObject<List<BanEntry>>(banListJson) ?? new List<BanEntry>();
			BanEntry bannedPlayer = banList.FirstOrDefault((BanEntry b) => b.ipAddress == dataBlob.ipAddress || b.deviceId == dataBlob.deviceId);
			if (bannedPlayer != null) {
				client.SendStringMessage("bandatablob:Success", networkStream);
				return;
			}
			banList.Add(new BanEntry(dataBlob.ipAddress, dataBlob.deviceId, banRequest.reason, banRequest.bannedUntil, banRequest.banType));
			File.WriteAllText("banlist.json", JsonConvert.SerializeObject(banList));
			updateServer(isFirstTime: false);
			client.SendStringMessage("bandatablob:Success", networkStream);
		} catch (Exception ex) {
			client.SendStringMessage("bandatablob:Error:" + ex.GetType().Name, networkStream);
		}
	}

	private static void msgUnbanDatablob(TcpClient client, NetworkStream networkStream, string message) {
		try {
			string request = message.RemovePrefix(Global.secretPrefix + "unbandatablob:");
			ReportedPlayerDataBlob dataBlob = JsonConvert.DeserializeObject<ReportedPlayerDataBlob>(AesOperation.DecryptString(Global.encryptionKey, request));
			string banListJson = File.ReadAllText("banlist.json");
			List<BanEntry> banList = JsonConvert.DeserializeObject<List<BanEntry>>(banListJson) ?? new List<BanEntry>();
			BanEntry bannedPlayer = banList.FirstOrDefault((BanEntry b) => b.ipAddress == dataBlob.ipAddress || b.deviceId == dataBlob.deviceId);
			if (bannedPlayer == null) {
				client.SendStringMessage("unbandatablob:Success", networkStream);
				return;
			}
			banList.RemoveAll((BanEntry b) => b == bannedPlayer);
			File.WriteAllText("banlist.json", JsonConvert.SerializeObject(banList));
			updateServer(isFirstTime: false);
			client.SendStringMessage("unbandatablob:Success", networkStream);
		} catch (Exception ex) {
			client.SendStringMessage("unbandatablob:Error:" + ex.GetType().Name, networkStream);
		}
	}

	static void udpMain() {
		config = new NetPeerConfiguration("XOD-Matchmaking");
		config.MaximumConnections = 10000;
		config.MaximumTransmissionUnit = 8191;
		config.Port = 14242;
		config.EnableUPnP = true;
		config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
		netServer = new NetServer(config);
		netServer.Start();
		while (true) {
			Helpers.tryWrap(udpIteration, isServer: true);
		}
	}

	static void udpIteration() {
		netServer.MessageReceivedEvent.WaitOne();
		NetIncomingMessage im;
		while ((im = netServer.ReadMessage()) != null) {
			// Handle incoming message
			NetIncomingMessageType messageType = im.MessageType;
			NetIncomingMessageType netIncomingMessageType = messageType;
			if (netIncomingMessageType == NetIncomingMessageType.UnconnectedData) {
				string message = im.ReadString();
				Helpers.debugLog("Client sent data message: " + message);
				if (message == "GetVersion") {
					NetOutgoingMessage om = netServer.CreateMessage();
					om.Write("GetVersion:" + Global.version);
					netServer.SendUnconnectedMessage(om, im.SenderEndPoint.Address.ToString(), im.SenderEndPoint.Port);
				}
			}
			netServer.Recycle(im);
		}
	}
}
