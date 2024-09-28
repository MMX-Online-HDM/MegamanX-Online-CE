using System;

namespace MMXOnline;

public class PreLoadoutMenu : IMainMenu {
	public int selectY;
	public int[] optionPos = {
		50,
		70,
		90,
		110,
		130,
		150
	};
	public MainMenu prevMenu;
	public string message;
	public Action yesAction;
	public bool inGame;
	public bool isAxl;
	public float startX = 150;
	public float Time = 1, Time2;
	public bool Confirm = false, Confirm2 = false;
	public PreLoadoutMenu(MainMenu prevMenu) {
		this.prevMenu = prevMenu;
		selectY = Options.main.preferredCharacter;
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
	public void update() {
		TimeUpdate();
		Helpers.menuUpDown(ref selectY, 0, 5);

		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			if (selectY == 0) {
				Menu.change(new SelectWeaponMenu(this, false));
			}
			if (selectY == 1) {
				Menu.change(new SelectZeroWeaponMenu(this, false));
			}
			if (selectY == 2) {
				Menu.change(new SelectVileWeaponMenu(this, false));
			}
			if (selectY == 3) {
				Menu.change(new SelectAxlWeaponMenu(this, false));
			}
			if (selectY == 4) {
				Menu.change(new SelectSigmaWeaponMenu(this, false));
			}
			if (selectY == (int)CharIds.PunchyZero) {
				Menu.change(new SelectPunchyZeroWeaponMenu(this, false));
			}
		} 
		if (Time2 >= 1) {
			Menu.change(prevMenu);
			prevMenu.Time = 0;
			prevMenu.Time2 = 1;
			prevMenu.Confirm = false;
			prevMenu.Confirm2 = false;
		}
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
			//DrawWrappers.DrawTextureMenu(Global.textures["cursor"], 20, topLeft.y + ySpace + (selectArrowPosY * ySpace));
			Global.sprites["cursor"].drawToHUD(0, startX - 10, 53 + (selectY * 20));
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
			Global.sprites["cursor"].drawToHUD(0, startX - 10, 53 + (selectY * 20));
		}

		Fonts.drawText(FontType.Golden, "SELECT CHARACTER LOADOUT", Global.screenW * 0.5f, 20, Alignment.Center);

		Fonts.drawText(FontType.DarkBlue, "X Loadout", startX, optionPos[0], selected: selectY == 0);
		Fonts.drawText(FontType.DarkBlue, "Zero Loadout", startX, optionPos[1], selected: selectY == 1);
		Fonts.drawText(FontType.DarkBlue, "Vile Loadout", startX, optionPos[2], selected: selectY == 2);
		Fonts.drawText(FontType.DarkBlue, "Axl Loadout", startX, optionPos[3], selected: selectY == 3);
		Fonts.drawText(FontType.DarkBlue, "Sigma Loadout", startX, optionPos[4], selected: selectY == 4);
		Fonts.drawText(FontType.DarkBlue, "KZero Loadout", startX, optionPos[5], selected: selectY == 5);

		Fonts.drawTextEX(FontType.Grey, "[OK]: Choose, [BACK]: Back", Global.halfScreenW, 200, Alignment.Center);
		DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0,0, Time);
		DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0,0, Time2);
	}
}
