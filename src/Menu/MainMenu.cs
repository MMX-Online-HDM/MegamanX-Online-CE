using Newtonsoft.Json;
using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public class MainMenu : IMainMenu {
	public const int startPos = 107;
	public const int yDistance = 13;

	public int selectY;
	public Point[] optionPos = {
		new Point(90, startPos),
		new Point(90, startPos + yDistance),
		new Point(90, startPos + yDistance * 2),
		new Point(90, startPos + yDistance * 3),
		new Point(90, startPos + yDistance * 4),
		new Point(90, startPos + yDistance * 5),
		new Point(90, startPos + yDistance * 6)
	};

	public float blinkTime = 0;

	public string playerName = "";
	public int state;

	public MainMenu() {
		if (string.IsNullOrWhiteSpace(Options.main.playerName)) {
			state = 0;
		} else if (Options.main.regionIndex == null) {
			state = 1;
		} else {
			state = 3;
		}
	}

	//float state1Time;
	public void update() {
		if (state == 0) {
			blinkTime += Global.spf;
			if (blinkTime >= 1f) blinkTime = 0;

			playerName = Helpers.getTypedString(playerName, Global.maxPlayerNameLength);

			if (Global.input.isPressed(Key.Enter) && !string.IsNullOrWhiteSpace(playerName.Trim())) {
				Options.main.playerName = Helpers.censor(playerName).Trim();
				Options.main.saveToFile();
				state = 1;
			}
			return;
		} else if (state == 1) {
			state = 3;
			return;
		}

		if (Global.input.isPressed(Key.F1)) {
			Menu.change(new TextExportMenu(
				new string[] {
					"Below is your checksum versions.",
					"",
					"CRC32:",
					Global.CRC32Checksum,
					"MD5:"
				},
				"checksum", Global.MD5Checksum, this)
			);
			return;
		}

		Helpers.menuUpDown(ref selectY, 0, 6);

		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			// Before joining or creating make sure client is up to date
			if (selectY == 0 || selectY == 1) {
				Menu.change(new PreJoinOrHostMenu(this, selectY == 0));
			} else if (selectY == 2) {
				Menu.change(new HostMenu(this, null, true, true));
			} else if (selectY == 3) {
				Menu.change(new PreLoadoutMenu(this));
			} else if (selectY == 4) {
				Menu.change(new PreControlMenu(this, false));
			} else if (selectY == 5) {
				Menu.change(new PreOptionsMenu(this, false));
			} else if (selectY == 6) {
				System.Environment.Exit(1);
			}
		}

		if (Global.debug) {
			//DEBUGSTAGE
			if (Global.quickStartOnline) {
				List<Server> servers = new List<Server>();
				byte[] response = Global.matchmakingQuerier.send("127.0.0.1", "GetServers");
				if (response.IsNullOrEmpty()) {
					//networkError = true;
				} else {
					servers = Helpers.deserialize<List<Server>>(response);
				}
				if (servers == null || servers.Count == 0) {
					Global.skipCharWepSel = true;
					var hostmenu = new HostMenu(this, null, false, false);
					Menu.change(hostmenu);
					Options.main.soundVolume = Global.quickStartOnlineHostSound;
					Options.main.musicVolume = Global.quickStartOnlineHostMusic;
					var serverData = new Server(Global.version, Options.main.getRegion(), "testserver", Global.quickStartOnlineMap, Global.quickStartOnlineMap, Global.quickStartOnlineGameMode, 100, Global.quickStartOnlineBotCount, 2, 300, false, false,
						Global.quickStartNetcodeModel, Global.quickStartNetcodePing, true, Global.quickStartMirrored, Global.quickStartTrainingLoadout, Global.checksum, null, null, SavedMatchSettings.mainOffline.extraCpuCharData, null,
						Global.quickStartDisableHtSt, Global.quickStartDisableVehicles);
					HostMenu.createServer(Global.quickStartOnlineHostCharNum, serverData, null, false, new MainMenu(), out _);
				} else {
					Global.skipCharWepSel = true;
					Options.main.soundVolume = Global.quickStartOnlineClientSound;
					Options.main.musicVolume = Global.quickStartOnlineClientMusic;
					var joinmenu = new JoinMenu(false);
					Menu.change(joinmenu);
				}
			}

			if (Global.input.isPressed(Key.Num1)) {
				Global.skipCharWepSel = true;
				var hostmenu = new HostMenu(this, null, false, false);
				Menu.change(hostmenu);
				var serverData = new Server(Global.version, Options.main.getRegion(), "testserver", Global.quickStartOnlineMap, Global.quickStartOnlineMap, Global.quickStartOnlineGameMode, 100, Global.quickStartOnlineBotCount, 2, 300, false, false,
					Global.quickStartNetcodeModel, Global.quickStartNetcodePing, true, Global.quickStartMirrored, Global.quickStartTrainingLoadout, Global.checksum, null, null, SavedMatchSettings.mainOffline.extraCpuCharData, null, Global.quickStartDisableHtSt, Global.quickStartDisableVehicles);
				HostMenu.createServer(Global.quickStartCharNum, serverData, null, false, new MainMenu(), out _);
			} else if (Global.input.isPressed(Key.Num2)) {
				Global.skipCharWepSel = true;
				var joinmenu = new JoinMenu(false);
				Menu.change(joinmenu);
			} else if (Global.input.isPressed(Key.Num3)) {
				var offlineMenu = new HostMenu(this, null, true, false);
				offlineMenu.mapSizeIndex = 0;
				offlineMenu.mapIndex = offlineMenu.currentMapSizePool.IndexOf(offlineMenu.currentMapSizePool.FirstOrDefault(m => m.isTraining()));
				offlineMenu.botCount = 1;
				Menu.change(offlineMenu);
			} else if (Global.quickStart) {
				var selectedLevel = Global.levelDatas.FirstOrDefault(ld => ld.Key == Global.quickStartMap).Value;
				var scm = new SelectCharacterMenu(Global.quickStartCharNum);
				var me = new ServerPlayer(Options.main.playerName, 0, true, Global.quickStartCharNum, Global.quickStartTeam, Global.deviceId, null, 0);
				if (selectedLevel.name == "training" && GameMode.isStringTeamMode(Global.quickStartTrainingGameMode)) me.alliance = Global.quickStartTeam;
				if (selectedLevel.name != "training" && GameMode.isStringTeamMode(Global.quickStartGameMode)) me.alliance = Global.quickStartTeam;

				string gameMode = selectedLevel.name == "training" ? Global.quickStartTrainingGameMode : Global.quickStartGameMode;
				int botCount = selectedLevel.name == "training" ? Global.quickStartTrainingBotCount : Global.quickStartBotCount;
				bool disableVehicles = selectedLevel.name == "training" ? Global.quickStartDisableVehiclesTraining : Global.quickStartDisableVehicles;

				var localServer = new Server(Global.version, null, null, selectedLevel.name, selectedLevel.shortName, gameMode, Global.quickStartPlayTo, botCount, selectedLevel.maxPlayers, 0, false, false,
					NetcodeModel.FavorAttacker, 200, true, Global.quickStartMirrored, Global.quickStartTrainingLoadout, Global.checksum, selectedLevel.checksum, selectedLevel.customMapUrl, SavedMatchSettings.mainOffline.extraCpuCharData, null,
					Global.quickStartDisableHtSt, disableVehicles);
				localServer.players = new List<ServerPlayer>() { me };

				Global.level = new Level(localServer.getLevelData(), SelectCharacterMenu.playerData, localServer.extraCpuCharData, false);
				Global.level.startLevel(localServer, false);
			}
		}
	}

	public MMXFont getFont(int index) {
		if (selectY == index) return MMXFont.Select;
		return MMXFont.Menu;
	}

	public void render() {
		float startX = 75;

		string selectionImage = "";
		if (selectY == 0) selectionImage = "joinserver";
		else if (selectY == 1) selectionImage = "hostserver";
		else if (selectY == 2) selectionImage = "vscpu";
		else if (selectY == 3) selectionImage = "loadout";
		else if (selectY == 4) selectionImage = "controls";
		else if (selectY == 5) selectionImage = "options";
		else if (selectY == 6) selectionImage = "quit";

		DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
		DrawWrappers.DrawTitleTexture(Global.textures["mainmenutitle"]);
		DrawWrappers.DrawTextureHUD(Global.textures["cursor"], startX - 10, startPos - 2 + (selectY * yDistance));
		DrawWrappers.DrawTextureHUD(Global.textures[selectionImage], 208, 107);
		DrawWrappers.DrawTextureHUD(Global.textures["mainmenubox"], 199, 98);

		Fonts.drawText(FontType.BlueMenu, "Join Match", startX, optionPos[0].y, selected: selectY == 0);
		Fonts.drawText(FontType.BlueMenu, "Create Match", startX, optionPos[1].y, selected: selectY == 1);
		Fonts.drawText(FontType.BlueMenu, "VS. CPU", startX, optionPos[2].y, selected: selectY == 2);
		Fonts.drawText(FontType.BlueMenu, "Loadout", startX, optionPos[3].y, selected: selectY == 3);
		Fonts.drawText(FontType.BlueMenu, "Controls", startX, optionPos[4].y, selected: selectY == 4);
		Fonts.drawText(FontType.BlueMenu, "Settings", startX, optionPos[5].y, selected: selectY == 5);
		Fonts.drawText(FontType.BlueMenu, "Quit", startX, optionPos[6].y, selected: selectY == 6);

		Fonts.drawTextEX(
			FontType.Grey, "[MUP]/[MDOWN]: Change selection, [OK]: Choose",
			Global.screenW / 2, Global.screenH - 12, Alignment.Center
		);

		if (state == 0) {
			float top = Global.screenH * 0.4f;

			//DrawWrappers.DrawRect(
			//	5, top - 20, Global.screenW - 5, top + 60, true, new Color(0, 0, 0), 0, ZIndex.HUD, false
			//);
			DrawWrappers.DrawRect(
				5, 5, Global.screenW - 5, Global.screenH - 5,
				true, new Color(0, 0, 0), 0, ZIndex.HUD, false
			);
			Fonts.drawText(
				FontType.DarkBlue, "Type in a multiplayer name", Global.screenW / 2, top, alignment: Alignment.Center
			);

			float xPos = Global.screenW * 0.33f;
			Fonts.drawText(FontType.DarkGreen, playerName, xPos, 20 + top, alignment: Alignment.Left);
			if (blinkTime >= 0.5f) {
				int width = Fonts.measureText(FontType.DarkGreen, playerName);
				Fonts.drawText(FontType.DarkGreen, "|", xPos + width + 3, 20 + top, alignment: Alignment.Left);
			}

			Fonts.drawText(
				FontType.Grey,
				"Press Enter to continue", Global.screenW / 2, 40 + top, alignment: Alignment.Center
			);
		} else if (state == 1) {
			float top = Global.screenH * 0.25f;
			DrawWrappers.DrawRect(
				5, 5, Global.screenW - 5, Global.screenH - 5, true, new Color(0, 0, 0), 0, ZIndex.HUD, false
			);
			Fonts.drawText(
				FontType.Blue, "Loading...", Global.screenW / 2, top, alignment: Alignment.Center
			);
		} else {
			string versionText = "v" + "20" + " " + Global.shortForkName;
			/*
			if (Helpers.compareVersions(Global.version, Global.serverVersion) == -1 &&
				Global.serverVersion != decimal.MaxValue
			) {
				versionText += "(Update available)";
			}
			*/
			int offset = 2;
			if (Global.checksum != Global.prodChecksum) {
				Fonts.drawText(FontType.DarkPurple, Global.CRC32Checksum, 2, offset);
				offset += 10;
			}
			Fonts.drawText(FontType.DarkBlue, versionText, 2, offset);
			
		}
	}
}
