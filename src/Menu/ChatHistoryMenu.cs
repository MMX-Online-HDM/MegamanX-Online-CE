using System;

namespace MMXOnline;

public class ChatHistoryMenu : IMainMenu {
	IMainMenu prevMenu;
	int yOffset = 0;
	int lines = 14;
	public ChatHistoryMenu(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;
	}

	public int maxYOffset {
		get {
			return Math.Max(0, Global.level.gameMode.chatMenu.chatHistory.Count - lines);
		}
	}

	public void update() {
		if (Global.input.isPressedMenu(Control.MenuUp)) {
			yOffset++;
			if (yOffset > maxYOffset) yOffset = maxYOffset;
		} else if (Global.input.isPressedMenu(Control.MenuDown)) {
			yOffset--;
			if (yOffset < 0) yOffset = 0;
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
		}
	}

	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		Fonts.drawText(
			FontType.Yellow, "Chat History", Global.screenW * 0.5f, 16, Alignment.Center
		);

		var ch = Global.level.gameMode.chatMenu.chatHistory;
		if (ch.Count > lines) {
			if (yOffset == maxYOffset) {
				Fonts.drawText(FontType.Purple, "[Match Start]", 20, 32, Alignment.Left);
			} else {
				Fonts.drawText(FontType.Purple, "...", 20, 32, Alignment.Left);
			}
		}
		if (ch.Count > lines) {
			if (yOffset == 0) {
				Fonts.drawText(FontType.Purple, "[End]", 20, 182, Alignment.Left);
			} else {
				Fonts.drawText(FontType.Purple, "...", 20, 182, Alignment.Left);
			}
		}

		int y = 0;
		for (int i = Math.Max(0, ch.Count - lines - yOffset); i < ch.Count - yOffset; i++) {
			string line = ch[i].getDisplayMessage();
			Fonts.drawText(FontType.Blue, line, 20, 42 + (y * 10));
			y++;
		}

		Fonts.drawTextEX(
			FontType.Grey, "[MUP]/[MDOWN]: Scroll, [BACK]: Back",
			Global.halfScreenW, Global.screenH - 18, Alignment.Center
		);
	}
}
