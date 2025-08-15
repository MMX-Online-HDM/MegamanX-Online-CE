using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class PreOptionsMenu : IMainMenu {
	public int selectY;
	public int[] optionPos = new int[10];
	public int lineH = 14;
	public MainMenu prevMenu1;
	public IMainMenu prevMenu;
	public string message;
	public Action yesAction;
	public bool inGame;
	public bool isAxl;
	public float startX = Global.halfScreenW;
	public float Time = 1, Time2;
	public bool Confirm = false, Confirm2 = false;
	public int[] cursorHandler = {
		(int)Global.halfScreenW - 60,
		(int)Global.halfScreenW - 60,
		(int)Global.halfScreenW - 60,
		(int)Global.halfScreenW - 65,
		(int)Global.halfScreenW - 40,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 50,
	};
	public int[] cursorHandler2 = {
		(int)Global.halfScreenW + 60,
		(int)Global.halfScreenW + 60,
		(int)Global.halfScreenW + 60,
		(int)Global.halfScreenW + 65,
		(int)Global.halfScreenW + 40,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 50,

	};
	public PreOptionsMenu(MainMenu prevMenu1, bool inGame) {
		this.prevMenu1 = prevMenu1;
		this.inGame = inGame;
		for (int i = 0; i < optionPos.Length; i++) {
			optionPos[i] = 45 + lineH * i;
		}
	}

	public void TimeUpdate() {
		if (!inGame) {
			if (Confirm == false) Time -= Global.spf * 2;
			if (Time <= 0) {
				Confirm = true;
				Time = 0;
			}
			if (Global.input.isPressedMenu(Control.MenuBack)) Confirm2 = true;
			if (Confirm2 == true) Time2 += Global.spf * 2;
		}
		if (inGame) {
			Time = 0;
			Time2 = 0;
		}
	}
	public void update() {
		Helpers.menuUpDown(ref selectY, 0, 5);
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			//if selectY is 1, it will throw gameplay option menu
			//if selectY is 2, it will throw graphics option menu
			//why would you hard code that
			Menu.change(new OptionsMenu(this, inGame, null, selectY));
			if (selectY == 3) {
				if (inGame) {
					Menu.change(new PreOptionsMenuCharacter(this, true));
				} else {
					Menu.change(new PreOptionsMenuCharacter(this, false));
				}
			}
			if (selectY == 4) {
				if (inGame) {
					Menu.change(new PreControlMenu(this, true));
				} else {
					Menu.change(new PreControlMenu(this, false));
				}
			}
			if (!inGame)
				if (selectY == 5) { Menu.change(new SoundMode(this, false)); }
		}
		if (Options.main.blackFade) {
			TimeUpdate();
			if (Time2 >= 1 && !inGame) {
				Menu.change(prevMenu1);
				if (prevMenu1 != null) {
					prevMenu1.Time = 0;
					prevMenu1.Time2 = 1;
					prevMenu1.Confirm = false;
					prevMenu1.Confirm2 = false;
				}
			}
		} else {
			if (Global.input.isPressedMenu(Control.MenuBack) && !inGame) {
				Menu.change(prevMenu1);
			}
		}
		if (Global.input.isPressedMenu(Control.MenuBack) && inGame) {
			Menu.change(prevMenu1);
		}
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
			//DrawWrappers.DrawTextureMenu(
			//Global.textures["cursor"], 20, topLeft.y + ySpace + (selectArrowPosY * ySpace)
			//);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		}
		//Global.sprites["cursor"].drawToHUD(0, startX - 60, 48 + (selectY * lineH));
		FontType tileFont = FontType.Purple;
		FontType menuFont = FontType.DarkBlue;
		if (inGame) {
			tileFont = FontType.Yellow;
			menuFont = FontType.Blue;
		}
		if (Global.flFrameCount % 60 < 30) {
			for (int i = 0; i < 6; i++) {
				if (selectY == i) {
					Fonts.drawText(FontType.Blue, "<", cursorHandler[i], optionPos[i], Alignment.Center, selected: selectY == i,
					selectedFont: inGame ? FontType.Orange : FontType.DarkOrange);
					Fonts.drawText(FontType.Blue, ">", cursorHandler2[i]-1, optionPos[i], Alignment.Center, selected: selectY == i,
					selectedFont: inGame ? FontType.Orange : FontType.DarkOrange);
				}
			}
		}
		Global.sprites["optionMode"].drawToHUD(inGame ? 2 : 2, Global.screenW * 0.5f + 1, 20);
		//Fonts.drawText(tileFont, "SETTINGS", Global.screenW * 0.5f, 20, Alignment.Center);

		Fonts.drawText(menuFont, "General settings", startX, optionPos[0], Alignment.Center, selected: selectY == 0);
		Fonts.drawText(menuFont, "Gameplay settings", startX, optionPos[1], Alignment.Center, selected: selectY == 1);
		Fonts.drawText(menuFont, "Graphics settings", startX, optionPos[2], Alignment.Center, selected: selectY == 2);
		Fonts.drawText(menuFont, "Character settings", startX, optionPos[3], Alignment.Center, selected: selectY == 3);
		Fonts.drawText(menuFont, "Controls", startX, optionPos[4], Alignment.Center, selected: selectY == 4);
		if (!inGame)
			Fonts.drawText(menuFont, "Sound Mode", startX, optionPos[5], Alignment.Center, selected: selectY == 5, selectedFont: FontType.Purple);

		Fonts.drawTextEX(FontType.Grey, "[OK]: Choose, [BACK]: Back", Global.halfScreenW, 198, Alignment.Center);
		if (Options.main.blackFade) {
			if (!inGame) {
				DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0, 0, Time);
				DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0, 0, Time2);
			}
		}
	}
}
public class PreOptionsMenuCharacter : IMainMenu {
	public int selArrowPosY;
	public Point optionPos1 = new Point(Global.halfScreenW, 70);
	public IMainMenu prevMenu;
	public int selectY;
	public bool inGame;
	public float startX = Global.halfScreenW;
	public int[] optionPos = new int[7];
	public int lineH = 14;
	public int[] cursorHandler = {
		(int)Global.halfScreenW - 40,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 45,
		(int)Global.halfScreenW - 52,
		(int)Global.halfScreenW - 40,
	};
	public int[] cursorHandler2 = {
		(int)Global.halfScreenW + 40,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 45,
		(int)Global.halfScreenW + 52,
		(int)Global.halfScreenW + 40,
	};

	public PreOptionsMenuCharacter(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		for (int i = 0; i < optionPos.Length; i++) {
			optionPos[i] = 45 + lineH * i;
		}

	}

	public void update() {
		Helpers.menuUpDown(ref selectY, 0, 4);
		if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
		}
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			switch (selectY) {
				case 0:
					Menu.change(new OptionsMenu(this, inGame, 0, 0));
					break;
				case 1:
					Menu.change(new OptionsMenu(this, inGame, 1, 0));
					break;
				case 2:
					Menu.change(new OptionsMenu(this, inGame, 2, 0));
					break;
				case 3:
					Menu.change(new OptionsMenu(this, inGame, 3, 0));
					break;
				case 4:
					Menu.change(new OptionsMenu(this, inGame, 4, 0));
					break;
			}
		}
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		}
		FontType menuFont = inGame ? FontType.Blue : FontType.DarkBlue;
		Global.sprites["optionMode"].drawToHUD(inGame ? 2 : 2, Global.screenW * 0.5f + 1, 20);
		if (Global.flFrameCount % 60 < 30) {
			for (int i = 0; i < 5; i++) {
				if (selectY == i) {
					Fonts.drawText(FontType.Blue, "<", cursorHandler[i], optionPos[i], Alignment.Center, selected: selectY == i,
					selectedFont: inGame ? FontType.Orange : FontType.DarkOrange);
					Fonts.drawText(FontType.Blue, ">", cursorHandler2[i]-1, optionPos[i], Alignment.Center, selected: selectY == i,
					selectedFont: inGame ? FontType.Orange : FontType.DarkOrange);
				}
			}
		}
		Fonts.drawText(menuFont, "X settings", startX, optionPos[0], Alignment.Center, selected: selectY == 0);
		Fonts.drawText(menuFont, "Zero settings", startX, optionPos[1], Alignment.Center, selected: selectY == 1);
		Fonts.drawText(menuFont, "Vile settings", startX, optionPos[2], Alignment.Center, selected: selectY == 2);
		Fonts.drawText(menuFont, "Axl settings", startX, optionPos[3], Alignment.Center, selected: selectY == 3);
		Fonts.drawText(menuFont, "Sigma settings", startX, optionPos[4], Alignment.Center, selected: selectY == 4);

		Fonts.drawTextEX(FontType.Grey, "[OK]: Choose, [BACK]: Back", Global.halfScreenW, 198, Alignment.Center);
	}
}
public class PreCPUMenu : IMainMenu {
	public int selArrowPosY;
	public Point optionPos1 = new Point(Global.halfScreenW, 70);
	public MainMenu prevMenu;
	public int selectY;
	public bool inGame;
	public float startX = Global.halfScreenW;
	public int[] optionPos = new int[2];
	public int lineH = 24;
	public int[] cursorHandler = {
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 70,
	};
	public int[] cursorHandler2 = {
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 70,
	};
	public float Time = 1, Time2;
	public bool Confirm = false, Confirm2 = false;
	public PreCPUMenu(MainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		for (int i = 0; i < optionPos.Length; i++) {
			optionPos[i] = 95 + lineH * i;
		}

	}

	public void update() {
		Helpers.menuUpDown(ref selectY, 0, 1);
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			switch (selectY) {
				case 0:
					Menu.change(new HostMenu(this, null, true, true, false));
					break;
				case 1:
					enterTraining();
					break;
			}
		}
		if (Options.main.blackFade) {
			TimeUpdate();
			if (Time2 >= 1) {
				Menu.change(prevMenu);
				prevMenu.Time = 0;
				prevMenu.Time2 = 1;
				prevMenu.Confirm = false;
				prevMenu.Confirm2 = false;
			}
		} else {
			if (Global.input.isPressedMenu(Control.MenuBack))
				Menu.change(prevMenu);
		}
	}
	public void enterTraining() {
		var selectedLevel = Global.levelDatas.FirstOrDefault(ld => ld.Key == Global.quickStartMap).Value;
		var scm = new SelectCharacterMenu(Global.quickStartCharNum);
		int spawnAsX = (int)CharIds.X;
		var me = new ServerPlayer(Options.main.playerName, 0, true, spawnAsX, Global.quickStartTeam, Global.deviceId, null, 0);
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
	public void TimeUpdate() {
		if (Confirm == false) Time -= Global.spf * 2;
		if (Time <= 0) {
			Confirm = true;
			Time = 0;
		}
		if (Global.input.isPressedMenu(Control.MenuBack)) Confirm2 = true;
		if (Confirm2 == true) Time2 += Global.spf * 2;
	}
	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
		FontType menuFont = FontType.DarkBlue;
		if (Global.flFrameCount % 60 < 30) {
			for (int i = 0; i < 2; i++) {
				if (selectY == i) {
					Fonts.drawText(FontType.Blue, "<", cursorHandler[i], optionPos[i], Alignment.Center, selected: selectY == i,
					selectedFont: inGame ? FontType.Orange : FontType.DarkOrange);
					Fonts.drawText(FontType.Blue, ">", cursorHandler2[i] - 1, optionPos[i], Alignment.Center, selected: selectY == i,
					selectedFont: inGame ? FontType.Orange : FontType.DarkOrange);
				}
			}
		}
		string Name = "Select Stage";
		Fonts.drawText(menuFont, Name, startX, optionPos[0], Alignment.Center, selected: selectY == 0);
		Fonts.drawText(menuFont, "Enter Fast Training", startX, optionPos[1], Alignment.Center, selected: selectY == 1);
		if (selectY == 1) {
			Fonts.drawText(menuFont, "You can enter through \n" + Name + "\nand customize your settings",
			 startX, optionPos[1] + 40, Alignment.Center, selected: selectY == 1, selectedFont: menuFont, alpha: 100);
		}
		Fonts.drawTextEX(FontType.Grey, "[OK]: Choose, [BACK]: Back", Global.screenW / 2, Global.screenH - 9, Alignment.Center);
		if (Options.main.blackFade) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0, 0, Time);
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0, 0, Time2);
		}
	}
}