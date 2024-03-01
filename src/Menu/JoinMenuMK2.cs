using System;
using System.Net;
using System.Collections.Generic;
using Lidgren.Network;
using ProtoBuf;
using Newtonsoft.Json;
using System.Linq;
using System.Threading;

namespace MMXOnline;

public class JoinMenuP2P : IMainMenu {
	public bool refreshing = true;
	public NetClient netClient;
	public long[] serverIndexes = new long[0];
	public Dictionary<long, (IPEndPoint intr, IPEndPoint extr)> serverList = new();
	public Dictionary<long, SimpleServerInfo> serverInfo = new();

	public int selServerIndex;

	public JoinMenuP2P(bool isMenu) {
		NetPeerConfiguration config = new NetPeerConfiguration("XOD-P2P");
		config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
		config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
		config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
		config.AutoFlushSendQueue = false;
		config.ConnectionTimeout = Server.connectionTimeoutSeconds;
		// Create client.
		var netClient = new NetClient(config);
		this.netClient = netClient;
		netClient.Start();

		if (isMenu) {
			getServer();
		}
	}

	public void getServer() {
		NetOutgoingMessage regMsg = netClient.CreateMessage();
		regMsg.Write((byte)MasterServerMsg.HostList);
		IPEndPoint masterServerLocation = NetUtility.Resolve(
			MasterServerData.serverIp, MasterServerData.serverPort
		);
		netClient.SendUnconnectedMessage(regMsg, masterServerLocation);
	}

	public void update() {
		NetIncomingMessage msg;
		// Respond to connection messages.
		while ((msg = netClient.ReadMessage()) != null) {
			if (msg.MessageType == NetIncomingMessageType.UnconnectedData) {
				byte msgByte = msg.ReadByte();
				switch (msgByte) {
					case 100:
						receiveHostList(msg);
						refreshing = false;
						break;
					// Recieve server details to connect.
					case 101:
						(long, SimpleServerData) serverData = receiveServerDetails(msg);
						if (serverData.Item2 != null) {
							joinServer(serverData.Item1, serverData.Item2);
							return;
						}
						break;
				}
			}
		}
		// Return it pressed exit.
		if (Global.input.isPressedMenu(Control.MenuBack)) {
			exit(new MainMenu());
			return;
		}
		int serverCount = serverIndexes.Length;
		if (serverCount <= 0) {
			return;
		}
		// To move the cursor.
		Helpers.menuUpDown(ref selServerIndex, 0, serverCount - 1);
		// We pick a server with this.
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			Action exitAction = () =>  {
				Menu.change(new ConnectionWaitMenuP2P(this, serverIndexes[selServerIndex]));
			};
			Menu.change(
				new SelectCharacterMenu(
					new MainMenu(), false, false, false,
					false, false, false, exitAction
				),
				false
			);
		}
	}

	// Render server list code.
	public void render() {
		// Draw background.
		DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);

		Fonts.drawTextEX(
			FontType.Grey, "[OK]: Join, [BACK]: Back",
			Global.halfScreenW, Global.screenH - 32, Alignment.Center
		);
		Fonts.drawText(FontType.Orange, "Name", 30, 22);
		Fonts.drawText(FontType.Orange, "Map", 102, 22);
		Fonts.drawText(FontType.Orange, "PNJ", 190, 22);
		Fonts.drawText(FontType.Orange, "Mode", 238, 22);
		Fonts.drawText(FontType.Orange, "Fork", 326, 22);
		int offset = 0;
		DrawWrappers.DrawTextureHUD(Global.textures["cursor"], 21, 30 + (selServerIndex * 10));
		if (refreshing) {
			Fonts.drawText(FontType.RedishOrange, "Refreshing...", 30, 32);
			return;
		}
		if (serverIndexes.Length == 0) {
			Fonts.drawText(FontType.Blue, "No servers found.", 30, 32);
			return;
		}
		foreach (long serverId in serverIndexes) {
			Fonts.drawText(FontType.Blue, serverInfo[serverId].name, 30, 32 + offset);
			Fonts.drawText(FontType.Blue, serverInfo[serverId].map, 102, 32 + offset);
			Fonts.drawText(FontType.Blue, 
				serverInfo[serverId].playerCount + "/" + serverInfo[serverId].maxPlayer,
				190, 32
			);
			Fonts.drawText(FontType.Blue, serverInfo[serverId].mode, 238, 32 + offset);
			Fonts.drawText(FontType.Blue, serverInfo[serverId].fork, 326, 32 + offset);
			offset += 10;
		}
	}

	public void exit(IMainMenu menu) {
		netClient.Shutdown("Bye");
		Menu.change(menu);
	}

	public void onExit(IMainMenu newMenu) {
		if (newMenu is not WaitMenu &&
			newMenu is not HostMenu &&
			newMenu is not PreJoinOrHostMenu
		) {
			netClient.Shutdown("Bye");
		}
	}

	public void receiveHostList(NetIncomingMessage msg) {
		List<long> serverKeys = new();
		while (msg.ReadByte() == 1) {
			long key = msg.ReadInt64();
			serverList[key] = (msg.ReadIPEndPoint(), msg.ReadIPEndPoint());
			serverInfo[key] = new SimpleServerInfo(
				name: msg.ReadString(),
				maxPlayer: msg.ReadByte(),
				playerCount: msg.ReadByte(),
				mode: msg.ReadString(),
				map: msg.ReadString(),
				fork: msg.ReadString()
			);
			serverKeys.Add(key);
		}
		serverIndexes = serverKeys.ToArray();
	}

	public (long, SimpleServerData) receiveServerDetails(NetIncomingMessage msg) {
		long severId = msg.ReadInt64();
		string jsonString = msg.ReadString();
		SimpleServerData serverDetails = JsonConvert.DeserializeObject<SimpleServerData>(jsonString);
		return (severId, serverDetails);
	}

	public void requestServerDetails(long serverId) {
		NetOutgoingMessage regMsg = netClient.CreateMessage();
		regMsg.Write((byte)MasterServerMsg.RequestDetails);
		regMsg.Write(serverId);
		IPEndPoint masterServerLocation = NetUtility.Resolve(
			MasterServerData.serverIp, MasterServerData.serverPort
		);
		netClient.SendUnconnectedMessage(regMsg, masterServerLocation);
	}

	public void joinServer(long serverId, SimpleServerData serverdata) {
		if (Helpers.compareVersions(Global.version, serverdata.gameVersion) == -1) {
			exit(
				new ErrorMenu(
					new string[] {
						"Your game netcode version is too old. Update to v" +
						serverdata.gameVersion.ToString()
					}, new MainMenu())
				);
			return;
		} else if (Helpers.compareVersions(Global.version, serverdata.gameVersion) == 1) {
			exit(
				new ErrorMenu(
					new string[] {
						"The match game version (v" +
						serverdata.gameVersion.ToString() + ") is too old."
					}, new MainMenu())
				);
			return;
		} else if (Global.checksum != serverdata.gameChecksum) {
			exit(
				new ErrorMenu(new string[] {
					"Client and server have different",
					"checksum version numbers.",
					"Yours: " + Global.checksum,
					"Theirs: " + serverdata.gameChecksum },
					new MainMenu())
				);
			return;
		} else if (!string.IsNullOrEmpty(serverdata.customMapChecksum)) {
			var myLevelChecksum = LevelData.getChecksumFromName(serverdata.level);
			if (string.IsNullOrEmpty(myLevelChecksum)) {
				string customMapUrl = serverdata.customMapUrl;
				var errorLines = new List<string>()
				{
						"Custom map \"" + serverdata.level + "\"",
						"not found in maps_custom folder."
					};
				if (!string.IsNullOrEmpty(customMapUrl)) {
					errorLines.Add("Download the map below:");
					exit(new TextExportMenu(
						errorLines.ToArray(), "customMapUrl",
						customMapUrl, new MainMenu(), textSize: 18
					));
				} else {
					exit(new ErrorMenu(errorLines.ToArray(), new MainMenu()));
				}

				return;
			} else if (myLevelChecksum != serverdata.customMapChecksum) {
				string customMapUrl = serverdata.customMapUrl;
				var errorLines = new List<string>(){
					"Client and server custom map",
					"checksums do not match.",
				};
				if (!string.IsNullOrEmpty(customMapUrl)) {
					errorLines.Add("Re-download the map below:");
					exit(new TextExportMenu(
						errorLines.ToArray(), "customMapUrl",
						customMapUrl, new MainMenu(), textSize: 18)
					);
				} else {
					exit(new ErrorMenu(errorLines.ToArray(), new MainMenu()));
				}
				return;
			}
		}
		string playerName = Options.main.playerName;

		var inputServerPlayer = new ServerPlayer(
			playerName, -1, false, SelectCharacterMenu.playerData.charNum,
			null, Global.deviceId, null, 0
		);
		Global.serverClient = ServerClient.CreateHolePunchAlt(
			netClient, serverId, inputServerPlayer, out JoinServerResponse joinServerResponse,
			out string error
		);
		if (Global.serverClient == null) {
			exit(new ErrorMenu(new string[] { error, "Please try rejoining." }, new MainMenu()));
			return;
		}
		var players = joinServerResponse.server.players;
		var server = joinServerResponse.server;

		if (Global.serverClient.serverPlayer.joinedLate) {
			Global.level = new Level(
				server.getLevelData(), SelectCharacterMenu.playerData, server.extraCpuCharData, true
			);
			Global.level.startLevel(joinServerResponse.server, true);
		} else {
			Menu.change(new WaitMenu(new MainMenu(), server, false));
		}
	}
}

public class SimpleServerData {
	[ProtoMember(1)] public string name;
	[ProtoMember(2)] public string level;
	[ProtoMember(3)] public decimal gameVersion;
	[ProtoMember(4)] public string gameChecksum;
	[ProtoMember(5)] public string customMapChecksum;
	[ProtoMember(6)] public string customMapUrl;

	public SimpleServerData() {

	}

	public SimpleServerData(
		string name, string level, decimal gameVersion,
		string gameChecksum, string customMapChecksum, string customMapUrl
	) {
		this.name = name;
		this.level = level;
		this.gameVersion = gameVersion;
		this.gameChecksum = gameChecksum;
		this.customMapChecksum = customMapChecksum;
		this.customMapUrl = customMapUrl;
	}
}

public class SimpleServerInfo {
	public string name;
	public byte playerCount;
	public byte maxPlayer;
	public string mode;
	public string map;
	public string fork;

	public SimpleServerInfo(string name, byte playerCount, byte maxPlayer, string mode, string map, string fork) {
		this.name = name;
		this.playerCount = playerCount;
		this.maxPlayer = maxPlayer;
		this.mode = mode;
		this.map = map;
		this.fork = fork;
	}
}

public static class MasterServerData {
	public static int serverPort = 17788;
	public static string serverIp = "127.0.0.1";

	#pragma warning disable SYSLIB0014
	public static void updateMasterServerURL() {
		if (Helpers.FileExists("./serverurl.txt")) {
			string contents = Helpers.ReadFromFile("./serverurl.txt");
			string[] portUrl = contents.Split(":");
			serverIp = System.Net.Dns.GetHostAddresses(portUrl[0])[0].ToString();
			serverPort = Int32.Parse(portUrl[1]);
			return;
		}
		try {
			string contents;
			using (var wc = new System.Net.WebClient()) {
				contents = wc.DownloadString(
					"http://mmx-online-hdm.github.io/serverinfo/serverurl.txt"
				);
			}
			string[] portUrl = contents.Split(":");
			serverIp = System.Net.Dns.GetHostAddresses(portUrl[0])[0].ToString();
			serverPort = Int32.Parse(portUrl[1]);
		} catch {
			return;
		}
	}
	#pragma warning restore  SYSLIB0014
}

public enum MasterServerMsg {
	HostList,
	ConnectPeers,
	RequestDetails,
	RegisterHost,
	RegisterDetails,
	RegisterInfo,
	UpdatePlayerNum,
	DeleteHost
}

public class ConnectionWaitMenuP2P : IMainMenu {
	public JoinMenuP2P joinMenu;
	public long serverID;
	public bool triedJoinOnce;

	public ConnectionWaitMenuP2P(JoinMenuP2P joinMenu, long serverID) {
		this.joinMenu = joinMenu;
		this.serverID = serverID;
	}

	public void update() {
		if (!triedJoinOnce) {
			joinMenu.requestServerDetails(serverID);
			joinMenu.netClient.FlushSendQueue();
		}
		joinServerLoop();
	}
	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
		Fonts.drawText(
			FontType.Grey, "Connecting... ",
			Global.screenH - 12, Global.screenW - 8, alignment: Alignment.Right
		);
	}

	public void joinServerLoop() {
		NetIncomingMessage msg;
		// Respond to connection messages.
		for (int i = 0; i <= 50; i++) {
			while ((msg = joinMenu.netClient.ReadMessage()) != null) {
				if (msg.MessageType == NetIncomingMessageType.UnconnectedData &&
					msg.ReadByte() == 101
				) {
					(long, SimpleServerData) serverData = joinMenu.receiveServerDetails(msg);
					if (serverData.Item2 != null) {
						joinMenu.joinServer(serverData.Item1, serverData.Item2);
						return;
					}
				}
			}
			if (i == 20) {
				joinMenu.requestServerDetails(serverID);
				joinMenu.netClient.FlushSendQueue();
			}
			Thread.Sleep(100);
		}
	}
}
