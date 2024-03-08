using System;
using SFML.Graphics;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public class EnterTextMenu : IMainMenu {
	public string message;
	public string text = "";
	public float blinkTime = 0;
	Action<string> submitAction;
	public int maxLength;
	public bool allowEmpty;

	public EnterTextMenu(string message, int maxLength, Action<string> submitAction, bool allowEmpty = false) {
		this.message = message;
		this.maxLength = maxLength;
		this.submitAction = submitAction;
		this.allowEmpty = allowEmpty;
	}

	public void update() {
		blinkTime += Global.spf;
		if (blinkTime >= 1f) blinkTime = 0;

		text = Helpers.getTypedString(text, maxLength);

		if (Global.input.isPressed(Key.Enter) && (allowEmpty || !string.IsNullOrWhiteSpace(text.Trim()))) {
			text = Helpers.censor(text);
			submitAction(text);
		}
	}

	public void render() {
		float top = Global.screenH * 0.4f;

		DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Color.Black, 0, ZIndex.HUD, false);
		Fonts.drawText(FontType.RedishOrange, message, Global.screenW / 2, top, alignment: Alignment.Center);

		float xPos = Global.screenW * 0.33f;
		Fonts.drawText(FontType.Green, text, xPos, 20 + top, alignment: Alignment.Left);
		if (blinkTime >= 0.5f) {
			float width = Fonts.measureText(FontType.Green, text);
			Fonts.drawText(FontType.Green, "|", xPos + width + 3, 20 + top, alignment: Alignment.Left);
		}

		Fonts.drawText(
			FontType.Grey, "Press Enter to continue",
			Global.screenW / 2, 40 + top, alignment: Alignment.Center
		);
	}

}
