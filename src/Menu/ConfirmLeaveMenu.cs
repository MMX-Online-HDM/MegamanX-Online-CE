using System;

namespace MMXOnline;

public class ConfirmLeaveMenu : IMainMenu {
	public int selectY;
	public Point optionPos1 = new Point(Global.halfScreenW, 70);
	public Point optionPos2 = new Point(Global.halfScreenW, 90);
	public Point optionPos5 = new Point(50, 170);
	public IMainMenu prevMenu;
	public string message;
	public Action yesAction;
	public uint fontSize;

	public ConfirmLeaveMenu(IMainMenu prevMenu, string message, Action yesAction, uint fontSize = 30) {
		this.prevMenu = prevMenu;
		this.message = message;
		this.yesAction = yesAction;
		this.fontSize = fontSize;
	}

	public void update() {
		Helpers.menuUpDown(ref selectY, 0, 1);
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			if (selectY == 0) {
				Menu.change(prevMenu);
			} else if (selectY == 1) {
				yesAction.Invoke();
			}
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
		}
	}

	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);

		if (message.Contains("\n")) {
			var lines = message.Split('\n');
			int i = 0;
			foreach (var line in lines) {
				Fonts.drawText(
					FontType.Yellow, line, Global.screenW * 0.5f, 20 + i * 10, Alignment.Center
				);
				i++;
			}
		} else {
			Fonts.drawText(
				FontType.Yellow, message, Global.screenW * 0.5f+7, 20, Alignment.Center
			);
		}
		//Global.sprites["cursor"].drawToHUD(0, 70, 76 + (selectY * 20) - 3);
		if (Global.frameCount % 60 < 30) {
			if (selectY == 0) {
				Fonts.drawText(FontType.Blue, "<", optionPos1.x - 15, optionPos1.y, Alignment.Center, selected: selectY == 0);
				Fonts.drawText(FontType.Blue, ">", optionPos1.x + 15, optionPos1.y, Alignment.Center, selected: selectY == 0);
			}
			if (selectY == 1) {
				Fonts.drawText(FontType.Blue, "<", optionPos2.x - 15, optionPos2.y, Alignment.Center, selected: selectY == 1);
				Fonts.drawText(FontType.Blue, ">", optionPos2.x + 15, optionPos2.y, Alignment.Center, selected: selectY == 1);
			}
		}
		Fonts.drawText(FontType.Blue, "No", optionPos1.x, optionPos1.y, Alignment.Center, selected: selectY == 0);
		Fonts.drawText(FontType.Blue, "Yes", optionPos2.x, optionPos2.y, Alignment.Center, selected: selectY == 1);

		Fonts.drawText(
			FontType.Grey, "[OK]: Choose, [BACK]: Back",
			Global.halfScreenW, Global.screenH - 20, Alignment.Center
		);
	}
}
