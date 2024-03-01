using Lidgren.Network;
using Newtonsoft.Json;
using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public class WaitMenu : IMainMenu {
	public IMainMenu previous;
	public Server server;

	public float autoRefreshTime;
	public float autoRefreshInterval = 0.5f;
	public int autoRefreshCount = 0;
	public const int maxAutoRefreshCount = 2;
	public float recreateWaitTime;
	public int selCursorY;

	public WaitMenu(IMainMenu prevMenu, Server server, bool isRecreate) {
		previous = prevMenu;
		Global.level = new Level(server.getLevelData(), SelectCharacterMenu.playerData, server.extraCpuCharData, false);
		this.server = server;
		if (isRecreate) {
			recreateWaitTime = 9;
		}
	}

	public void update() {
		recreateWaitTime -= Global.spf;
		if (recreateWaitTime < 0) recreateWaitTime = 0;

		// This section of code automatically invoke the start level RPC on the clients
		Global.serverClient.getMessages(out var messages, true);
		foreach (var message in messages) {
			if (message.StartsWith("joinserverresponse:")) {
				var player = JsonConvert.DeserializeObject<ServerPlayer>(message.RemovePrefix("joinserverresponse:"));
				if (!server.players.Any(p => p.id == player.id)) {
					server.players.Add(player);
				}
				autoRefreshTime = autoRefreshInterval;
				autoRefreshCount--;
			} else if (message.StartsWith("clientdisconnect:")) {
				var disconnectedPlayer = JsonConvert.DeserializeObject<ServerPlayer>(message.RemovePrefix("clientdisconnect:"));
				server.players.RemoveAll(p => p.id == disconnectedPlayer.id);
			} else if (message.StartsWith("hostdisconnect:")) {
				Global.serverClient = null;
				Menu.change(new ErrorMenu("The host cancelled the match.", new JoinMenu(false)));
				return;
			} else if (message.StartsWith(RPCSwitchTeam.prefix)) {
				RPCSwitchTeam.getMessageParts(message, out int playerId, out int alliance);
				for (int i = 0; i < server.players.Count; i++) {
					if (server.players[i].id == playerId) {
						server.players[i].alliance = alliance;
					}
				}
			}
		}

		if (Global.serverClient == null) return;

		if (server.players.Count > 10) rowHeight2 = 10;
		else rowHeight2 = 12;
		selCursorY = Helpers.clamp(selCursorY, 0, server.players.Count - 1);

		if (Global.serverClient.isHost) {
			if ((Global.input.isPressedMenu(Control.MenuConfirm) || Global.quickStartOnline) && recreateWaitTime <= 0) {
				Action onCreate = new Action(() => {
					if (server.players.Count > 1) {
						Logger.logEvent("host_2ormore", Logger.getMatchLabel(server.level, server.gameMode), server.players.Count);
					}

					Global.level.startLevel(server, false);
					var rpcStartLevelJson = new RPCStartLevelJson(server);
					Global.serverClient.rpc(RPC.startLevel, JsonConvert.SerializeObject(rpcStartLevelJson));
					Global.serverClient.flush();
				});

				if (Global.level.is1v1() && server.players.Count(p => !p.isSpectator) > 4) {
					Menu.change(new ConfirmLeaveMenu(this, "More than four combatants. Proceed anyway?", () => {
						onCreate();
					}, fontSize: 20));
					return;
				} else if (Global.level.is1v1() && server.players.Count(p => !p.isSpectator) < 2) {
					Menu.change(new ErrorMenu("Two combatants are required.", this));
					return;
				}

				onCreate();
				return;
			} else if (Global.input.isPressedMenu(Control.MenuBack)) {
				Menu.change(new ConfirmLeaveMenu(this, "Are you sure you want to cancel match?", () => {
					Global.serverClient.disconnect("Host cancelled before starting.");
					Global.serverClient = null;
					Menu.change(previous);
				}));
			}

			Helpers.menuUpDown(ref selCursorY, 0, server.players.Count - 1);

			bool isLeft = Global.input.isPressedMenu(Control.MenuLeft);
			bool isRight = Global.input.isPressedMenu(Control.MenuRight);
			if (isLeft || isRight) {
				var player = server.players[selCursorY];

				if (isTeamMode()) {
					if (isLeft) {
						if (player.isSpectator) {
							player.isSpectator = false;
							player.alliance = 1;
						} else if (player.alliance == 1) {
							player.alliance = 0;
						} else if (player.alliance == 0) {
							player.isSpectator = true;
						}
					} else {
						if (player.isSpectator) {
							player.isSpectator = false;
							player.alliance = 0;
						} else if (player.alliance == 0) {
							player.alliance = 1;
						} else if (player.alliance == 1) {
							player.isSpectator = true;
						}
					}
					RPC.makeSpectator.sendRpc(player.id, player.isSpectator);
					Global.serverClient.rpc(RPC.switchTeam, RPCSwitchTeam.getSendMessage(player.id, player.alliance));
					Global.serverClient.flush();
				} else {
					player.isSpectator = !player.isSpectator;
					RPC.makeSpectator.sendRpc(player.id, player.isSpectator);
					Global.serverClient.flush();
				}
			}
		} else {
			if (Global.input.isPressedMenu(Control.MenuBack)) {
				Global.serverClient.disconnect("Client disconnected before starting.");
				Global.serverClient = null;
				Thread.Sleep(200);
				Menu.change(new JoinMenu(false));
			}
		}

		autoRefreshTime += Global.spf;
		if (server.isP2P) {
			return;
		}
		if (Global.input.isPressedMenu(Control.MenuAlt) ||
			(autoRefreshTime > autoRefreshInterval && autoRefreshCount < maxAutoRefreshCount)
		) {
			autoRefreshTime = 0;
			autoRefreshCount++;
			byte[] serverBytes = Global.matchmakingQuerier.send(server.region.ip, "GetServer:" + server.name);
			if (!serverBytes.IsNullOrEmpty()) {
				server = Helpers.deserialize<Server>(serverBytes);
			}
		}
	}

	public string topMsg = "";

	public float col1Pos = 35;
	public float col2Pos = 130;
	public float col3Pos = 170;
	public float col4Pos = 230;

	public float headerPos = 60;
	public float rowHeight = 10;
	public float rowHeight2 = 10;

	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
		string titleText = "Waiting for host...";
		if (Global.serverClient.isHost) {
			titleText = "Waiting for players...";
		}
		Fonts.drawText(FontType.RedishOrange, titleText, col1Pos, 20);
		if (!server.isLAN) {
			Fonts.drawText(FontType.Orange, "Match Name: " + server.name, col1Pos, headerPos - 30);
			Fonts.drawText(FontType.Orange, "Map: " + server.getMapDisplayName(), col1Pos, headerPos - 20);
		} else {
			Fonts.drawText(FontType.Orange, "Match Name: " + server.name, col1Pos, headerPos - 30);
			Fonts.drawText(FontType.Orange, "Match IP: " + server.ip, col1Pos + 40, headerPos - 20);
			Fonts.drawText(FontType.Orange, "Map: " + server.getMapDisplayName(), col1Pos, headerPos - 20);
		}

		Fonts.drawText(FontType.Yellow, "Player", col1Pos, headerPos);
		Fonts.drawText(FontType.Yellow, "Id", col2Pos, headerPos);
		Fonts.drawText(FontType.Yellow, "Host?", col3Pos, headerPos);
		if (isTeamMode()) Fonts.drawText(FontType.Yellow, "Team", col4Pos, headerPos);
		else Fonts.drawText(FontType.Yellow, "Spec?", col4Pos, headerPos);

		var startServerRow = rowHeight + headerPos;

		if (Global.serverClient.isHost) {
			DrawWrappers.DrawTextureHUD(
				Global.textures["cursor"], 26, startServerRow - 2 + (selCursorY * rowHeight2)
			);
		}

		for (int i = 0; i < server.players.Count; i++) {
			var player = server.players[i];
			var color = Color.White;
			if (player.id == Global.serverClient.serverPlayer.id) {
				color = Color.Green;
			}

			Fonts.drawText(FontType.Blue, player.name, col1Pos, startServerRow + (i * rowHeight2));
			Fonts.drawText(FontType.Blue,player.id.ToString(), col2Pos, startServerRow + (i * rowHeight2));
			Fonts.drawText(
				FontType.Blue, player.isHost ? "yes" : "no",
				col3Pos, startServerRow + (i * rowHeight2)
			);
			if (isTeamMode()) {
				string team = player.alliance == GameMode.redAlliance ? "red" : "blue";
				if (player.isSpectator) team = "spec";
				Fonts.drawText(FontType.Blue, team, col4Pos, startServerRow + (i * rowHeight2));
			} else {
				Fonts.drawText(
					FontType.Blue, player.isSpectator ? "yes" : "no",
					col4Pos, startServerRow + (i * rowHeight2)
				);
			}
		}

		if (!Global.serverClient.isHost) {
			Fonts.drawTextEX(
				FontType.Grey, "[BACK]: Leave, [ALT]: Refresh",
				Global.halfScreenW, 190, Alignment.Center
			);
		} else {
			string helpLegend = "[OK]: Start, [BACK]: Leave, [ALT]: Refresh";
			if (recreateWaitTime > 0) {
				helpLegend = string.Format(
					"Can Start in {0}s, [BACK]: Leave, [ALT]: Refresh",
					MathF.Ceiling(recreateWaitTime)
				);
			}
			Fonts.drawTextEX(
				FontType.Grey, helpLegend,
				Global.halfScreenW, 190, Alignment.Center
			);
			if (isTeamMode() && Global.serverClient.isHost) {
				Fonts.drawTextEX(
					FontType.Grey, "[Left/Right: change player team]",
					Global.halfScreenW, 180, Alignment.Center
				);
			}
			else if (
				!isTeamMode() && Global.serverClient.isHost)
				Fonts.drawTextEX(
					FontType.Grey, "[Left/Right: change player spectator]",
					Global.halfScreenW, 180, Alignment.Center
				);
		}
	}

	public bool isTeamMode() {
		return GameMode.isStringTeamMode(server.gameMode);
	}
}
