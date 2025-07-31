using System;

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
			if (selectY == 3) { Menu.change(new PreOptionsMenuCharacter(this, false)); }
			if (selectY == 4) { Menu.change(new PreControlMenu(this, false)); }
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
		if (Global.frameCount % 60 < 30) {
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
		DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
		FontType menuFont = FontType.DarkBlue;
		Global.sprites["optionMode"].drawToHUD(inGame ? 2 : 2, Global.screenW * 0.5f + 1, 20);
		if (Global.frameCount % 60 < 30) {
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