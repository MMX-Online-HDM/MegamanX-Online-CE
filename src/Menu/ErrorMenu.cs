using SFML.Graphics;

namespace MMXOnline;

public class ErrorMenu : IMainMenu {
	string[] error;
	IMainMenu prevMenu;
	bool inGame;

	public ErrorMenu(string error, IMainMenu prevMenu, bool inGame = false) {
		this.error = new string[] { error };
		this.prevMenu = prevMenu;
		this.inGame = inGame;
	}

	public ErrorMenu(string[] error, IMainMenu prevMenu, bool inGame = false) {
		this.error = error;
		this.prevMenu = prevMenu;
		this.inGame = inGame;
	}

	public void update() {
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			Menu.change(prevMenu);
		}
	}

	public void render() {
		float top = 22;

		if (inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		}

		Fonts.drawText(
			FontType.Red, error[0], Global.screenW / 2, top,
			alignment: Alignment.Center
		);
		for (int i = 1; i < error.Length; i++) {
			Fonts.drawText(
				FontType.Blue, error[i],
				Global.screenW / 2, top + (i * 20),
				alignment: Alignment.Center
			);
		}
		Fonts.drawTextEX(
			FontType.Grey, "Press [OK] to continue",
			Global.screenW / 2, Global.screenH - 32, alignment: Alignment.Center
		);
	}
}
