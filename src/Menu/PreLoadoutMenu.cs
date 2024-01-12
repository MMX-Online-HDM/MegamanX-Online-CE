using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline;

public class PreLoadoutMenu : IMainMenu {
	public int selectY;
	public int[] optionPos = {
		70,
		90,
		110,
		130,
		150
	};
	public IMainMenu prevMenu;
	public string message;
	public Action yesAction;
	public bool inGame;
	public bool isAxl;
	public float startX = 150;

	public PreLoadoutMenu(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;
		selectY = Options.main.preferredCharacter;
	}

	public void update() {
		Helpers.menuUpDown(ref selectY, 0, 4);

		if (Global.input.isPressedMenu(Control.MenuSelectPrimary)) {
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
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
		}
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
			//DrawWrappers.DrawTextureMenu(Global.textures["cursor"], 20, topLeft.y + ySpace + (selectArrowPosY * ySpace));
			Global.sprites["cursor"].drawToHUD(0, startX - 10, 73 + (selectY * 20));
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
			Global.sprites["cursor"].drawToHUD(0, startX - 10, 73 + (selectY * 20));
		}

		Fonts.drawText(FontType.Golden, "SELECT CHARACTER LOADOUT", Global.screenW * 0.5f, 20, Alignment.Center);

		Fonts.drawText(FontType.DarkBlue, "X Loadout", startX, optionPos[0], selected: selectY == 0);
		Fonts.drawText(FontType.DarkBlue, "Zero Loadout", startX, optionPos[1], selected: selectY == 1);
		Fonts.drawText(FontType.DarkBlue, "Vile Loadout", startX, optionPos[2], selected: selectY == 2);
		Fonts.drawText(FontType.DarkBlue, "Axl Loadout", startX, optionPos[3], selected: selectY == 3);
		Fonts.drawText(FontType.DarkBlue, "Sigma Loadout", startX, optionPos[4], selected: selectY == 4);

		Fonts.drawText(FontType.Grey, "[X]: Choose, [Z]: Back", Global.halfScreenW, 200, Alignment.Center);
	}
}
