using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MMXOnline;

public class GetRegionResponse {
	public Region region;
	public List<Server> servers;
	public GetRegionResponse(Region region, List<Server> servers) {
		this.region = region;
		this.servers = servers;
	}
}

public class JoinMenu : IMainMenu {
	public bool refreshing = false;
	public bool networkError = false;
	public List<Server> allServers = new List<Server>();
	public int refreshFrames;
	public bool joinedLateDone;
	public LANIPHelper lanIPHelper;
	public bool isLAN;

	public HashSet<Region> failedRegions = new HashSet<Region>();

	public List<Server> publicServers {
		get {
			return allServers.Where(s => !s.hidden).ToList();
		}
	}

	public JoinMenu(bool isLAN) {
		this.isLAN = isLAN;
		if (!isLAN && !Global.regionPingTask.IsCompleted) {
			Global.regionPingTask.Wait();
		}
		queueRefresh();
	}

	public void queueRefresh() {
		refreshing = true;
		refreshFrames = 2;
	}

	public List<Region> getRegions() {
		if (!isLAN) {
			return Global.regions;
		} else {
			lock (Global.lanRegions) {
				var tasks = new List<Task<Region>>();
				foreach (var lanIP in getLANIPs()) {
					if (!Global.lanRegions.Any(r => r.ip == lanIP)) {
						tasks.Add(Region.tryCreateWithPingClient("LAN", lanIP));
					}
				}

				Task.WaitAll(tasks.ToArray());

				foreach (var task in tasks) {
					if (task.Result != null) {
						Global.lanRegions.Add(task.Result);
					}
				}
			}
			Global.updateLANRegionPings();
			return Global.lanRegions;
		}
	}

	public List<string> getLANIPs() {
		if (lanIPHelper == null) lanIPHelper = new LANIPHelper();
		return lanIPHelper.getIps();
	}

	// null = fail.
	public GetRegionResponse getRegionServers(Region region) {
		byte[] response = Global.matchmakingQuerier.send(region.ip, "GetServers");
		if (response.IsNullOrEmpty()) {
			if (response == null && region.name != "LAN") {
				return new GetRegionResponse(region, null);
			} else {
				return new GetRegionResponse(region, new List<Server>());
			}
		} else {
			var serverList = Helpers.deserialize<List<Server>>(response);
			return new GetRegionResponse(region, serverList);
		}
	}

	public void refreshTaskMethod() {
		var tasks = new List<Task<GetRegionResponse>>();
		foreach (var region in getRegions()) {
			tasks.Add(Task.Run(() => getRegionServers(region)));
		}

		Task.WaitAll(tasks.ToArray());
		foreach (var task in tasks) {
			if (task.Result.servers == null) {
				failedRegions.Add(task.Result.region);
			} else {
				allServers.AddRange(task.Result.servers);
			}
		}

		refreshing = false;
	}

	Task refreshTask;
	public void refresh() {
		allServers.Clear();
		failedRegions.Clear();

		refreshing = true;
		if (refreshTask == null || refreshTask.IsCompleted) {
			refreshTask = Task.Run(refreshTaskMethod);
		}

		if (selServerIndex >= allServers.Count) {
			selServerIndex = Math.Clamp(allServers.Count - 1, 0, int.MaxValue);
		}
	}

	int frameCount;
	public void update() {
		if (!refreshing) {
			frameCount++;
			if (frameCount > 240) {
				frameCount = 0;
				if (isLAN) {
					Global.updateLANRegionPings();
				} else {
					Global.updateRegionPings();
				}
			}
		}

		if (refreshFrames > 0) {
			refreshFrames--;
			if (refreshFrames <= 0) {
				refreshFrames = 0;
				refresh();
			}
		}

		if (publicServers.Count > 0) {
			Helpers.menuUpDown(ref selServerIndex, 0, publicServers.Count - 1);
			if (Global.input.isPressedMenu(Control.MenuConfirm) || Global.quickStartOnline) {
				var server = publicServers[selServerIndex];
				Menu.change(new SelectCharacterMenu(this, server.level.EndsWith("1v1"), false, false, false, GameMode.isStringTeamMode(server.gameMode), false, () => joinServer(server)));
			}
		}

		if (publicServers.Count > 10) {
			rowHeight2 = 9;
		} else {
			rowHeight2 = 14;
		}

		if (Global.input.isPressedMenu(Control.MenuAlt)) {
			queueRefresh();
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(new MainMenu());
		} else if (Global.input.isPressedMenu(Control.MenuPause)) {
			if (!isLAN) {
				Menu.change(new EnterTextMenu("Enter private server name", 10, (string text) => {
					Server server = allServers.Where(s => s.hidden && s.name == text).FirstOrDefault();
					if (server != null) {
						Menu.change(new SelectCharacterMenu(this, server.level.EndsWith("1v1"), false, false, false, GameMode.isStringTeamMode(server.gameMode), false, () => joinServer(server)));
						return;
					}

					// Fallback: try requesting the server name directly
					foreach (var region in getRegions()) {
						byte[] serverBytes = Global.matchmakingQuerier.send(region.ip, "GetServer:" + text);
						if (!serverBytes.IsNullOrEmpty()) {
							server = Helpers.deserialize<Server>(serverBytes);
						} else if (serverBytes == null) {
							Menu.change(new ErrorMenu(new string[] { "Error when looking up private match." }, this));
							return;
						}
						if (server != null) {
							Menu.change(new SelectCharacterMenu(this, server.level.EndsWith("1v1"), false, false, false, GameMode.isStringTeamMode(server.gameMode), false, () => joinServer(server)));
							return;
						}
					}

					Menu.change(new ErrorMenu(new string[] { "Private server not found.", "Note: match names are case sensitive." }, this));
				}));
			} else {
				Menu.change(new EnterTextMenu("Enter IP Address or URL", 30, (ipAddressStr) => {
					if (ipAddressStr.IsValidIpAddress()) {
						lock (Global.lanRegions) {
							if (!Global.lanRegions.Any(r => r.ip == ipAddressStr)) {
								Global.lanRegions.Add(new Region("LAN", ipAddressStr));
							}
						}
						queueRefresh();
						Menu.change(this);
					} else {
						string urlIP = null;
						try {
							urlIP = System.Net.Dns.GetHostAddresses(ipAddressStr)[0].ToString();
						} catch {
							// Do nothing if it's a invalid url.
						}
						if (String.IsNullOrEmpty(urlIP)) {
							Menu.change(new ErrorMenu(new string[] { "Invalid IP address or URL." }, this));
						} else {
							lock (Global.lanRegions) {
								if (!Global.lanRegions.Any(r => r.ip == urlIP)) {
									Global.lanRegions.Add(new Region("LAN", urlIP));
								}
							}
						}
					}
				}));
			}
		}
	}

	public static void joinServer(Server serverToJoin) {
		if (Helpers.compareVersions(Global.version, serverToJoin.gameVersion) == -1) {
			Menu.change(new ErrorMenu(new string[] { "Your game version is too old. Update to v" + serverToJoin.gameVersion.ToString() }, new MainMenu()));
			return;
		} else if (Helpers.compareVersions(Global.version, serverToJoin.gameVersion) == 1) {
			Menu.change(new ErrorMenu(new string[] { "The match game version (v" + serverToJoin.gameVersion.ToString() + ") is too old." }, new MainMenu()));
			return;
		} else if (Global.checksum != serverToJoin.gameChecksum) {
			Menu.change(new ErrorMenu(new string[] { "Client and server have different", "checksum version numbers.", "Yours: " + Global.checksum, "Theirs: " + serverToJoin.gameChecksum }, new MainMenu()));
			return;
		} else if (!string.IsNullOrEmpty(serverToJoin.customMapChecksum)) {
			var myLevelChecksum = LevelData.getChecksumFromName(serverToJoin.level);
			if (string.IsNullOrEmpty(myLevelChecksum)) {
				string customMapUrl = serverToJoin.customMapUrl;
				var errorLines = new List<string>()
				{
						"Custom map \"" + serverToJoin.level + "\"",
						"not found in maps_custom folder."
					};
				if (!string.IsNullOrEmpty(customMapUrl)) {
					errorLines.Add("Download the map below:");
					Menu.change(new TextExportMenu(errorLines.ToArray(), "customMapUrl", customMapUrl, new MainMenu(), textSize: 18));
				} else {
					Menu.change(new ErrorMenu(errorLines.ToArray(), new MainMenu()));
				}

				return;
			} else if (myLevelChecksum != serverToJoin.customMapChecksum) {
				string customMapUrl = serverToJoin.customMapUrl;
				var errorLines = new List<string>()
				{
						"Client and server custom map",
						"checksums do not match.",
					};
				if (!string.IsNullOrEmpty(customMapUrl)) {
					errorLines.Add("Re-download the map below:");
					Menu.change(new TextExportMenu(errorLines.ToArray(), "customMapUrl", customMapUrl, new MainMenu(), textSize: 18));
				} else {
					Menu.change(new ErrorMenu(errorLines.ToArray(), new MainMenu()));
				}
				return;
			}
		}

		if (!Global.debug && !serverToJoin.hidden && serverToJoin.playerJoinCount.ContainsKey(Global.deviceId) && serverToJoin.playerJoinCount[Global.deviceId] > Server.maxRejoinCount + 1) {
			DateTime nextJoinTime = serverToJoin.playerLastJoinTime[Global.deviceId].AddMinutes(5);
			double minutes = (nextJoinTime - DateTime.UtcNow).TotalMinutes;
			if (minutes > 0) {
				int min = (int)Math.Ceiling(minutes);
				Menu.change(new ErrorMenu(
					new string[] {
						"You have joined/disconnected too many times.",
						"Can rejoin in " + min.ToString() + " minutes." },
						new MainMenu())
					);
				return;
			}
		}

		foreach (var banEntry in serverToJoin.kickedPlayers) {
			if (!banEntry.isBanned(null, Global.deviceId, 0)) continue;

			string nextJoinTime = "You cannot rejoin.";
			if (banEntry.bannedUntil != null) {
				double minutes = (banEntry.bannedUntil.Value - DateTime.UtcNow).TotalMinutes;
				nextJoinTime = string.Format("Can rejoin in {0} minutes.", (int)Math.Ceiling(minutes));
			}
			Menu.change(new ErrorMenu(
				new string[] { "You were kicked from this server!", nextJoinTime }, new MainMenu())
			);
			return;
		}

		string playerName = Options.main.playerName;

		var inputServerPlayer = new ServerPlayer(
			playerName, -1, false, SelectCharacterMenu.playerData.charNum,
			null, Global.deviceId, null, serverToJoin.region.getPing()
		);
		Global.serverClient = ServerClient.Create(
			serverToJoin.region.ip, serverToJoin.name, serverToJoin.port,
			inputServerPlayer, out JoinServerResponse joinServerResponse,
			out string error
		);
		if (Global.serverClient == null) {
			Menu.change(new ErrorMenu(new string[] { error, "Please try rejoining." }, new JoinMenu(serverToJoin.isLAN)));
			return;
		}

		var players = joinServerResponse.server.players;
		var server = joinServerResponse.server;

		if (Global.serverClient.serverPlayer.joinedLate) {
			Global.level = new Level(server.getLevelData(), SelectCharacterMenu.playerData, server.extraCpuCharData, true);
			Global.level.teamNum = joinServerResponse.server.teamNum;

			Global.level.startLevel(joinServerResponse.server, true);
			/*
			while (!Global.level.started)
			{
				Global.serverClient.getMessages(out var messages, true);
				Thread.Sleep(100);
			}
			*/
		} else {
			Menu.change(new WaitMenu(new MainMenu(), server, false));
		}
	}

	public string topMsg = "";

	public float col1Pos = 20;
	public float col2Pos = 75;
	public float col3Pos = 145;
	public float col4Pos = 175;
	public float col5Pos = 225;
	public float col6Pos = 265;

	public float headerPos = 30;
	public float rowHeight = 20;
	public float rowHeight2 = 14;

	public int selServerIndex = 0;

	public void render() {
		string joinMenuImage = isLAN ? "joinlanmenutitle" : "joinmenutitle";
		DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
		// DrawWrappers.DrawTextureHUD(Global.textures[joinMenuImage], 0, 0);
		//DrawWrappers.DrawTextureHUD(Global.textures["joinborder"], 0, 30);

		Fonts.drawText(FontType.Yellow, "Join Match", Global.halfScreenW, 20, alignment: Alignment.Center);

		Fonts.drawText(FontType.Orange, "Name", col1Pos, headerPos);
		Fonts.drawText(FontType.Orange, "Map", col2Pos, headerPos);
		Fonts.drawText(FontType.Orange, "Mode", col3Pos, headerPos);
		Fonts.drawText(FontType.Orange, "Players", col4Pos, headerPos);
		Fonts.drawText(FontType.Orange, "Region", col5Pos, headerPos);
		Fonts.drawText(FontType.Orange, "Ping", col6Pos, headerPos);

		if (refreshing) {
			Fonts.drawText(
				FontType.Grey, "Searching...", col1Pos, headerPos + rowHeight);
		} else if (networkError) {
			Fonts.drawText(
				FontType.Grey, "Could not contact server.", col1Pos, headerPos + rowHeight
			);
		} else if (publicServers.Count == 0) {
			Fonts.drawText(
				FontType.Grey, isLAN ? "(No LAN matches found)" : "(No matches found)",
				col1Pos, headerPos + rowHeight
			);
			if (isLAN) {
				Fonts.drawText(
					FontType.Grey, " Try connecting by IP (press ESC)",
					col1Pos, headerPos + rowHeight * 2
				);
			}
		} else {
			var startServerRow = rowHeight + headerPos;
			for (int i = 0; i < publicServers.Count; i++) {
				var server = publicServers[i];
				Region region = null;
				if (!isLAN) region = Global.regions.FirstOrDefault(r => r.ip == server.region.ip);
				else region = Global.lanRegions.FirstOrDefault(r => r.ip == server.region.ip);
				Fonts.drawText(FontType.Blue, server.name, col1Pos, startServerRow + (i * rowHeight2), selected: selServerIndex == i);
				Fonts.drawText(FontType.Blue, server.getMapShortName(), col2Pos, startServerRow + (i * rowHeight2), selected: selServerIndex == i);
				Fonts.drawText(FontType.Blue, GameMode.abbreviatedMode(server.gameMode), col3Pos, startServerRow + (i * rowHeight2), selected: selServerIndex == i);
				Fonts.drawText(FontType.Blue, getPlayerCountStr(server), col4Pos, startServerRow + (i * rowHeight2), selected: selServerIndex == i);
				Fonts.drawText(FontType.Blue, server.region.name, col5Pos, startServerRow + (i * rowHeight2), selected: selServerIndex == i);
				if (region != null) {
					int ping = region.ping ?? -1;
					string displayPint = ping.ToString();
					FontType pingColor = FontType.Blue;
					if (ping > 200) {
						pingColor = FontType.Red;
					}
					if (ping < 0) {
						displayPint = "N/A";
						pingColor = FontType.Grey;
					}
					Fonts.drawText(
						pingColor, region.getDisplayPing(),
						col6Pos, startServerRow + (i * rowHeight2)
					);
				}
			}
			DrawWrappers.DrawTextureHUD(
				Global.textures["cursor"], 12, startServerRow - 2 + (selServerIndex * rowHeight2)
			);
		}

		if (failedRegions.Count > 0) {
			string[] failedRegionsList = failedRegions.Select(r => r.name).ToArray();
			//failedRegionsList = new List<string>() { "EastUS", "WestUS", "Brazil" };
			string failedRegionsText = string.Join(", ", failedRegionsList);
			if (failedRegionsText == "") {
				failedRegionsText = "N/A";
			}
			Fonts.drawText(
				FontType.Red,
				"Failed to get match list from regions: " + failedRegionsText + ".",
				col1Pos, 60
			);
		}

		if (!refreshing) {
			string escText = isLAN ? "[ESC]: Search by IP" : "[ESC]: Join private match";
			Fonts.drawTextEX(
				FontType.Grey, "[OK]: Join, [ALT]: Refresh, [BACK]: Back",
				Global.halfScreenW, Global.screenH - 36, Alignment.Center
			);
			Fonts.drawTextEX(
				FontType.Grey, escText,
				Global.halfScreenW, Global.screenH - 26, Alignment.Center
			);
		}
	}

	public string getPlayerCountStr(Server server) {
		var players = server.players;
		int botCount = players.Count(p => p.isBot);
		int humanCount = players.Count(p => !p.isBot);
		if (botCount == 0) {
			return humanCount + "/" + server.maxPlayers;
		} else {
			return players.Count + "(" + botCount + "b)/" + server.maxPlayers;
		}
	}
}
