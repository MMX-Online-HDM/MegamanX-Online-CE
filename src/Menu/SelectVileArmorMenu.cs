using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline;

public class SelectVileArmorMenu : IMainMenu {
	public int selectArrowPosY;
	public IMainMenu prevMenu;

	public int optionPosX = 20;
	public int[] optionPosY;

	public SelectVileArmorMenu(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;
		optionPosY = new int[] {
			40,
			50,
			80,
			90
		};
	}

	public void update() {
		var mainPlayer = Global.level.mainPlayer;

		if (!Global.level.server.disableHtSt && Global.input.isPressedMenu(Control.MenuLeft)) {
			UpgradeMenu.onUpgradeMenu = true;
			Menu.change(new UpgradeMenu(prevMenu));
			return;
		}

		Helpers.menuUpDown(ref selectArrowPosY, 0, 1);

		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			if (selectArrowPosY == 0) {
				if (!mainPlayer.frozenCastle && mainPlayer.currency >= Vile.frozenCastleCost) {
					mainPlayer.frozenCastle = true;
					Global.playSound("ching");
					mainPlayer.currency -= Vile.frozenCastleCost;
				}
			} else if (selectArrowPosY == 1) {
				if (!mainPlayer.speedDevil && mainPlayer.currency >= Vile.speedDevilCost) {
					mainPlayer.speedDevil = true;
					Global.playSound("ching");
					mainPlayer.currency -= Vile.speedDevilCost;
				}
			}
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
		}
	}

	public void render() {
		var mainPlayer = Global.level.mainPlayer;
		var gameMode = Global.level.gameMode;
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		Global.sprites["menu_viledefault"].drawToHUD(0, 310, 110);

		if (!Global.level.server.disableHtSt && Global.frameCount % 60 < 30) {
			Fonts.drawText(FontType.DarkPurple, "<", 18, Global.halfScreenH + 10, Alignment.Center);
			Fonts.drawText(FontType.DarkPurple, "Items", 18, Global.halfScreenH + 20, Alignment.Center);
		}

		if (mainPlayer.speedDevil) Global.sprites["menu_vilespeeddevil"].drawToHUD(0, 310, 110);
		if (mainPlayer.frozenCastle) Global.sprites["menu_vilefrozencastle"].drawToHUD(0, 310, 110);

		Global.sprites["cursor"].drawToHUD(0, optionPosX - 6, optionPosY[0] + selectArrowPosY * 40 + 3);

		Fonts.drawText(FontType.Yellow, "Vile Armor", Global.screenW * 0.5f, 10, Alignment.Center);
		Fonts.drawText(
			FontType.Golden,
			Global.nameCoins + ": " + mainPlayer.currency,
			Global.screenW * 0.5f, 20, Alignment.Center
		);

		Fonts.drawText(FontType.Blue, "Frozen Castle", optionPosX, optionPosY[0],
			selected: selectArrowPosY == 0
		);
		Fonts.drawText(
			FontType.Purple, $" ({Vile.frozenCastleCost} {Global.nameCoins})",
			optionPosX + 110, optionPosY[0]
		);
		Fonts.drawText(
			FontType.Green, "By utilizing a thin layer of ice," +
			"\nthis armor reduces damage by 12.5%",
			optionPosX, optionPosY[1]
		);

		Fonts.drawText(
			FontType.Blue, "Speed Devil", optionPosX, optionPosY[2],
			selected: selectArrowPosY == 1
		);
		Fonts.drawText(
			FontType.Purple, $" ({Vile.speedDevilCost} {Global.nameCoins})",
			optionPosX + 110, optionPosY[2]
		);
		Fonts.drawText(
			FontType.Green, "A layer of atmospheric pressure\nincreases movement speed by 10%.",
			optionPosX, optionPosY[3]
		);

		Fonts.drawTextEX(FontType.Grey, "[MLEFT]/[MRIGHT]: Change Armor", 40, 188);
		Fonts.drawTextEX(FontType.Grey,
			"[OK]: Upgrade, [ALT]: Unupgrade, [BACK]: Back", 40, 198
		);
	}

}
