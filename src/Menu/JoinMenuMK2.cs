using System;
using System.Net;
using System.Collections.Generic;
using Lidgren.Network;
using ProtoBuf;
using Newtonsoft.Json;
using System.Linq;

namespace MMXOnline;

public class JoinMenuP2P : IMainMenu {
	public bool refreshing = true;
	public NetClient netClient;
	public long[] serverIndexes = new long[0];
	public Dictionary<long, (IPEndPoint intr, IPEndPoint extr)> serverList = new();
	public Dictionary<long, SimpleServerInfo> serverInfo = new();

	public int selServerIndex;

	public JoinMenuP2P() {
		var config = new NetPeerConfiguration("XOD-P2P");
		config.SetMessageTypeEnabled(NetIncomingMessageType.UnconnectedData, true);
		var netClient = new NetClient(config);
		this.netClient = netClient;

		netClient.Start();

		IPAddress adress = NetUtility.GetMyAddress(out _);
		getServer();
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
							netClient.Shutdown("Bye");
							System.Threading.Thread.Sleep(100);
							joinServer(serverData.Item1, serverData.Item2);
							return;
						}
						break;
				}
			}
		}
		// Return it pressed exit.
		if (Global.input.isPressedMenu(Control.MenuBack)) {
			exit();
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
			refreshing = true;
			requestServerDetails(serverIndexes[selServerIndex]);
		}
	}

	// Render server list code.
	public void render() {
		// Draw background.
		DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);

		Fonts.drawText(
			FontType.Grey, "ENTR: Join, SPC: Refresh, BSPC: Back",
			Global.halfScreenW, Global.screenH - 32, Alignment.Center
		);
		Fonts.drawText(
			FontType.OrangeMenu, " Name     Map        Plyrs Mode       Fork",
			22, 20
		);
		int offset = 0;
		DrawWrappers.DrawTextureHUD(Global.textures["cursor"], 21, 32 + (selServerIndex * 12));
		if (refreshing) {
			Fonts.drawText(FontType.Grey, "Refreshing...", 30, 32);
			return;
		}
		foreach (long serverId in serverIndexes) {
			Fonts.drawText(FontType.Grey, serverInfo[serverId].name, 30, 32 + offset);
			Fonts.drawText(FontType.Grey, serverInfo[serverId].map, 102, 32 + offset);
			Fonts.drawText(FontType.Grey, 
				serverInfo[serverId].playerCount + "/" + serverInfo[serverId].maxPlayer,
				190, 32
			);
			Fonts.drawText(FontType.Grey, serverInfo[serverId].mode, 238, 32 + offset);
			Fonts.drawText(FontType.Grey, serverInfo[serverId].fork, 326, 32 + offset);
			offset += 10;
		}
	}

	public void exit() {
		netClient.Shutdown("Bye");
		Menu.change(new MainMenu());
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

	public static void joinServer(long serverId, SimpleServerData serverdata) {
		if (Helpers.compareVersions(Global.version, serverdata.gameVersion) == -1) {
			Menu.change(
				new ErrorMenu(
					new string[] {
						"Your game netcode version is too old. Update to v" +
						serverdata.gameVersion.ToString()
					}, new MainMenu())
				);
			return;
		} else if (Helpers.compareVersions(Global.version, serverdata.gameVersion) == 1) {
			Menu.change(
				new ErrorMenu(
					new string[] {
						"The match game version (v" +
						serverdata.gameVersion.ToString() + ") is too old."
					}, new MainMenu())
				);
			return;
		} else if (Global.checksum != serverdata.gameChecksum) {
			Menu.change(
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
					Menu.change(new TextExportMenu(
						errorLines.ToArray(), "customMapUrl",
						customMapUrl, new MainMenu(), textSize: 18
					));
				} else {
					Menu.change(new ErrorMenu(errorLines.ToArray(), new MainMenu()));
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
					Menu.change(new TextExportMenu(
						errorLines.ToArray(), "customMapUrl",
						customMapUrl, new MainMenu(), textSize: 18)
					);
				} else {
					Menu.change(new ErrorMenu(errorLines.ToArray(), new MainMenu()));
				}
				return;
			}
		}
		string playerName = Options.main.playerName;

		var inputServerPlayer = new ServerPlayer(
			playerName, -1, false, SelectCharacterMenu.playerData.charNum,
			null, Global.deviceId, null, 0
		);
		Global.serverClient = ServerClient.CreateHolePunch(
			serverId, inputServerPlayer, out JoinServerResponse joinServerResponse,
			out string error
		);
		if (Global.serverClient == null) {
			Menu.change(new ErrorMenu(new string[] { error, "Please try rejoining." }, new MainMenu()));
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
	private static string _serverIp = null;
	public static string serverIp {
		get {
			if (_serverIp == null) {
				try {
					_serverIp = System.Net.Dns.GetHostAddresses(serverUrl)[0].ToString();
				} catch {
					_serverIp = "127.0.0.1";
				}
			}
			return _serverIp;
		}
	}
	public static string serverUrl = "127.0.0.1";
}

public enum MasterServerMsg {
	HostList,
	ConnectPeers,
	RequestDetails,
	RegisterHost,
	RegisterDetails,
	RegisterInfo,
	UpdatePlayerNum
}
