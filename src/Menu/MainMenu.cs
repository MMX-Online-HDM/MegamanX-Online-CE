using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SFML.Graphics;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public class MainMenu : IMainMenu {
	public const int startPos = 107;
	public const int yDistance = 17;
	public float Time;
	public float Time2;
	public bool Confirm = false;
	public bool Confirm2 = true;
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
		
		if (Time == 0) Helpers.menuUpDown(ref selectY, 0, 5);
		TimeUpdate();
		if (Time >= 1) {
			Time = 0;
			Confirm = false;
			// Before joining or creating make sure client is up to date
			if (selectY == 0 || selectY == 1) {
				Menu.change(new PreJoinOrHostMenu(this, selectY == 0));
			} else if (selectY == 2) {
				Menu.change(new HostMenu(this, null, true, true));
			} else if (selectY == 3) {
				Menu.change(new PreLoadoutMenu(this));
			//} else if (selectY == 4) {
			//	Menu.change(new PreControlMenu(this, false));
			} else if (selectY == 4) {
				Menu.change(new PreOptionsMenu(this, false));
			} else if (selectY == 5) {
				System.Environment.Exit(1);
			}
		}
		MenuConfirmSound();
		DebugVoid();
	}

	public void render() {
		float WD = Global.halfScreenW;

		//string selectionImage = "";
		/*
		if (selectY == 0) selectionImage = "joinserver";
		else if (selectY == 1) selectionImage = "hostserver";
		else if (selectY == 2) selectionImage = "vscpu";
		else if (selectY == 3) selectionImage = "loadout";
		else if (selectY == 4) selectionImage = "controls";
		else if (selectY == 5) selectionImage = "options";
		else if (selectY == 6) selectionImage = "quit";
		*/
		DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0, 0, 1);
		DrawWrappers.DrawTextureHUD(Global.textures["mainmenutitle"], 10, -3);
	//	DrawWrappers.DrawTextureHUD(Global.textures["cursor"], startX - 10, startPos - 2 + (selectY * yDistance));
	//	DrawWrappers.DrawTextureHUD(Global.textures[selectionImage], 208, 107);
	//	DrawWrappers.DrawTextureHUD(Global.textures["mainmenubox"], 199, 98);
		RenderCharacters();
		DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0,0, Time2);
		Fonts.drawText(FontType.BlueMenu, "JOIN MATCH", WD, optionPos[0].y, Alignment.Center, selected: selectY == 0);
		Fonts.drawText(FontType.BlueMenu, "CREATE MATCH", WD, optionPos[1].y,  Alignment.Center, selected: selectY == 1);
		Fonts.drawText(FontType.BlueMenu, "VS. CPU", WD, optionPos[2].y,  Alignment.Center, selected: selectY == 2);
		Fonts.drawText(FontType.BlueMenu, "LOADOUT", WD, optionPos[3].y, Alignment.Center, selected: selectY == 3);
	//	Fonts.drawText(FontType.BlueMenu, "Controls", WD, optionPos[4].y, selected: selectY == 4);
		Fonts.drawText(FontType.BlueMenu, "OPTION MODE", WD, optionPos[4].y, Alignment.Center, selected: selectY == 4);
		Fonts.drawText(FontType.BlueMenu, "QUIT", WD, optionPos[5].y, Alignment.Center, selected: selectY == 5);

		Fonts.drawTextEX(
			FontType.Grey, "[MUP]/[MDOWN]: Change selection, [OK]: Choose",
			Global.screenW / 2, Global.screenH - 9, Alignment.Center
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
			string versionText = Global.shortForkName + " v" + Global.version + " " + Global.subVersionShortName;
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
			offset += 10;
			if (Global.radminIP != "") {
				Fonts.drawText(FontType.DarkGreen, "Radmin", 2, offset);
			}
		}
		DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0,0, Time);
		DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0,0, Time2);
	}
	public void TimeUpdate() {
		if (Global.input.isPressedMenu(Control.MenuConfirm)) Confirm = true;
		if (Confirm == true) Time += Global.spf * 2;
		if (Confirm2 == false) Time2 -= Global.spf * 2;
		if (Time2 <= 0) {
			Confirm2 = false;
			Time2 = 0;
		}
	}
	public void RenderCharacters() {
		float WD = Global.halfScreenW-46;
		switch (Options.main.preferredCharacter) {
			case 0:
				Global.sprites["menu_megaman"].drawToHUD(0,WD - 42, startPos - 3 + (selectY * yDistance));
				break;
			case 1:
				Global.sprites["menu_zero"].drawToHUD(0,WD - 42, selectY == 5 ? startPos - 8 + (selectY * yDistance) 
				: startPos - 2 + (selectY * yDistance));
				break;
			case 2:
				Global.sprites["menu_vile"].drawToHUD(0,WD - 42,  selectY == 5 ? startPos - 8 + (selectY * yDistance) 
				: startPos - 2 + (selectY * yDistance));
				break;
			case 3:
				Global.sprites["menu_axl"].drawToHUD(0,WD - 42, selectY == 5 ? startPos - 8 + (selectY * yDistance) 
				: startPos + (selectY * yDistance));
				break;
			case 4:
				switch (Options.main.sigmaLoadout.sigmaForm) {
					case 0:
						Global.sprites["menu_sigma"].drawToHUD(0,WD - 42, selectY == 5 ? startPos - 16 + (selectY * yDistance) 
						: startPos + (selectY * yDistance));
						break;
					case 1:
						Global.sprites["menu_sigma"].drawToHUD(1,WD - 42, selectY == 5 ? startPos - 16 + (selectY * yDistance) 
						: startPos + (selectY * yDistance));
						break;
					case 2:
						Global.sprites["menu_sigma"].drawToHUD(2,WD - 42, selectY == 5 ? startPos - 17 + (selectY * yDistance) 
						: startPos + (selectY * yDistance));
						break;
				}
				break;
			default:
				Global.sprites["menu_megaman"].drawToHUD(0,WD - 42, startPos - 2 + (selectY * yDistance));
				break;
		}
	}
	public void MenuConfirmSound() {
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			switch (Options.main.preferredCharacter) {
				case 0: //X
					switch (Helpers.randomRange(0,2)) {
						case 0:
							Global.playSound("buster3", false);
							break;
						case 1:
							Global.playSound("buster2", false);
							break;
						case 2:
							Global.playSound("buster4", false);
							break;
					}
					break;
				case 1: //Z
					switch (Helpers.randomRange(0,2)) {
						case 0:
							Global.playSound("buster3X3", false);
							break;
						case 1:
							Global.playSound("zerosaberx3", false);
							break;
						case 2:
							Global.playSound("raijingeki", false);
							break;
					}
					break;
				case 2: //V
					switch (Helpers.randomRange(0,2)) {
						case 0:
							Global.playSound("frontrunner", false);
							break;
						case 1:
							Global.playSound("rocketPunch", false);
							break;
						case 2:
							Global.playSound("ridepunchX3", false);
							break;
					}
					break;
				case 3: //Axl
					switch (Helpers.randomRange(0,2)) {
						case 0:
							Global.playSound("assassinate", false);
							break;
						case 1:
							Global.playSound("axlBulletCharged", false);
							break;
						case 2:
							Global.playSound("rocketShoot", false);
							break;
					}
					break;
				case 4:
					switch (Options.main.sigmaLoadout.sigmaForm) {
						case 0: //Commander
							switch (Helpers.randomRange(0,3)) {
								case 0:
									Global.playSound("sigmaSaber", false);
									break;
								case 1:
									Global.playSound("energyBall", false);
									break;
								case 2:
									Global.playSound("warpIn", false);
									break;
								case 3:
									Global.playSound("wolfSigmaThunderX1", false);
									break;
							}
						break;
						case 1: //Neo
							switch (Helpers.randomRange(0,2)) {
								case 0:
									Global.playSound("sigma2start", false);
									break;
								case 1:
									Global.playSound("sigma2shoot", false);
									break;
								case 2:
									Global.playSound("sigma2slash", false);
									break;
							}
						break;
						case 2: //Doppma
							switch (Helpers.randomRange(0,1)) {
								case 0:
									Global.playSound("sigma3shoot", false);
									break;
								case 1:
									Global.playSound("kaiserSigmaCharge", false);
									break;
							}
						break;
					}
					break;
				default:
					Global.playSound("buster3", false);
					break;
			}
		}
	}
	public void DebugVoid() {
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
					var serverData = new Server(
						Global.version, Options.main.getRegion(),
						"testserver", Global.quickStartOnlineMap,
						Global.quickStartOnlineMap, Global.quickStartOnlineGameMode,
						100, Global.quickStartOnlineBotCount, 2, 300, false, false,
						Global.quickStartNetcodeModel, Global.quickStartNetcodePing,
						true, Global.quickStartMirrored, Global.quickStartTrainingLoadout,
						Global.checksum, null, null, SavedMatchSettings.mainOffline.extraCpuCharData, null,
						Global.quickStartDisableHtSt, Global.quickStartDisableVehicles, 2
					);
					HostMenu.createServer(
						Global.quickStartOnlineHostCharNum, serverData, null, false, new MainMenu(), out _
					);
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
				var serverData = new Server(
					Global.version, Options.main.getRegion(), "testserver",
					Global.quickStartOnlineMap, Global.quickStartOnlineMap,
					Global.quickStartOnlineGameMode, 100, Global.quickStartOnlineBotCount, 2, 300, false, false,
					Global.quickStartNetcodeModel, Global.quickStartNetcodePing,
					true, Global.quickStartMirrored, Global.quickStartTrainingLoadout,
					Global.checksum, null, null, SavedMatchSettings.mainOffline.extraCpuCharData,
					null, Global.quickStartDisableHtSt, Global.quickStartDisableVehicles, 2
				);
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

				var localServer = new Server(
					Global.version, null, null, selectedLevel.name, selectedLevel.shortName,
					gameMode, Global.quickStartPlayTo, botCount, selectedLevel.maxPlayers, 0, false, false,
					NetcodeModel.FavorAttacker, 200, true, Global.quickStartMirrored,
					Global.quickStartTrainingLoadout, Global.checksum, selectedLevel.checksum,
					selectedLevel.customMapUrl, SavedMatchSettings.mainOffline.extraCpuCharData, null,
					Global.quickStartDisableHtSt, disableVehicles,
					2
				);
				localServer.players = new List<ServerPlayer>() { me };
				Global.level = new Level(localServer.getLevelData(), SelectCharacterMenu.playerData, localServer.extraCpuCharData, false);
				Global.level.teamNum = localServer.teamNum;
				Global.level.startLevel(localServer, false);
			}
		}
	}
}
