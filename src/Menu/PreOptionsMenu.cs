using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline;

public class PreOptionsMenu : IMainMenu {
	public int selectY;
	public Point optionPos1;
	public Point optionPos2;
	public Point optionPos3;
	public Point optionPos4;
	public Point optionPos5;
	public Point optionPos6;
	public Point optionPos7;
	public const int lineH = 15;
	public IMainMenu prevMenu;
	public string message;
	public Action yesAction;
	public bool inGame;
	public bool isAxl;
	public float startX = 140;

	public PreOptionsMenu(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		optionPos1 = new Point(40, 70);
		optionPos2 = new Point(40, 70 + lineH);
		optionPos3 = new Point(40, 70 + lineH * 2);
		optionPos4 = new Point(40, 70 + lineH * 3);
		optionPos5 = new Point(40, 70 + lineH * 4);
		optionPos6 = new Point(40, 70 + lineH * 5);
		optionPos7 = new Point(40, 70 + lineH * 6);
	}

	public void update() {
		Helpers.menuUpDown(ref selectY, 0, 6);
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			int? charNum = null;
			bool isGraphics = selectY == 1;
			if (selectY == 2) charNum = 0;
			if (selectY == 3) charNum = 1;
			if (selectY == 4) charNum = 2;
			if (selectY == 5) charNum = 3;
			if (selectY == 6) charNum = 4;

			Menu.change(new OptionsMenu(this, inGame, charNum, isGraphics));
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
		}
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
			//DrawWrappers.DrawTextureMenu(
				//Global.textures["cursor"], 20, topLeft.y + ySpace + (selectArrowPosY * ySpace)
			//);
			Global.sprites["cursor"].drawToHUD(0, startX - 10, 73 + (selectY * lineH));
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
			Global.sprites["cursor"].drawToHUD(0, startX - 10, 73 + (selectY * lineH));
		}
		FontType tileFont = FontType.Golden;
		FontType menuFont = FontType.DarkBlue;
		if (inGame) {
			tileFont = FontType.Yellow;
			menuFont = FontType.Blue;
		}

		Fonts.drawText(tileFont, "SELECT SETTINGS TO CONFIGURE", Global.screenW * 0.5f, 20, Alignment.Center);

		Fonts.drawText(menuFont, "General settings", startX, optionPos1.y, selected: selectY == 0);
		Fonts.drawText(menuFont, "Graphics settings", startX, optionPos2.y, selected: selectY == 1);
		Fonts.drawText(menuFont, "X settings", startX, optionPos3.y, selected: selectY == 2);
		Fonts.drawText(menuFont, "Zero settings", startX, optionPos4.y, selected: selectY == 3);
		Fonts.drawText(menuFont, "Vile settings", startX, optionPos5.y, selected: selectY == 4);
		Fonts.drawText(menuFont, "Axl settings", startX, optionPos6.y, selected: selectY == 5);
		Fonts.drawText(menuFont, "Sigma settings", startX, optionPos7.y, selected: selectY == 6);

		Fonts.drawTextEX(FontType.Grey, "[X]: Choose, [Z]: Back", Global.halfScreenW, 198, Alignment.Center);
	}
}
