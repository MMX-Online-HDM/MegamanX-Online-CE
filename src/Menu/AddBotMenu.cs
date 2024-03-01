using Newtonsoft.Json;
using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public class AddBotMenu : IMainMenu {
	public int selectArrowPosY;
	public IMainMenu previous;

	public static int botCharNum = -1;
	public static int botTeamNum = -1;

	public List<Point> optionPoses = new List<Point>()
	{
			new Point(30, 100),
			new Point(30, 120),
		};
	public int ySep = 10;

	public AddBotMenu(IMainMenu mainMenu) {
		previous = mainMenu;
	}

	public void update() {
		Helpers.menuUpDown(ref selectArrowPosY, 0, optionPoses.Count - 1);

		if (selectArrowPosY == 0) {
			if (Global.input.isPressedMenu(Control.MenuLeft)) {
				botCharNum--;
				if (botCharNum < -1) botCharNum = -1;
			} else if (Global.input.isPressedMenu(Control.MenuRight)) {
				botCharNum++;
				if (botCharNum >= 210) botCharNum = 4;
			}
		}
		if (selectArrowPosY == 1 && teamOptionEnabled()) {
			if (Global.input.isPressedMenu(Control.MenuLeft)) {
				botTeamNum--;
				if (botTeamNum < -1) botTeamNum = -1;
			} else if (Global.input.isPressedMenu(Control.MenuRight)) {
				botTeamNum++;
				if (botTeamNum > 1) botTeamNum = 1;
			}
		}

		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			if (Global.serverClient != null) {
				RPC.addBot.sendRpc(botCharNum, botTeamNum);
			} else {
				int id = 0;
				int charNum = botCharNum;
				int alliance = botTeamNum;
				if (charNum == -1) charNum = Helpers.randomRange(0, 4);
				if (alliance == -1) {
					if (Global.level.gameMode.isTeamMode) {
						GameMode.getAllianceCounts(Global.level.players, out int redCount, out int blueCount);
						if (blueCount > redCount) alliance = GameMode.redAlliance;
						else alliance = GameMode.blueAlliance;
					}
				}

				for (int i = 0; i < Global.level.players.Count + 1; i++) {
					if (!Global.level.players.Any(p => p.id == i)) {
						id = i;
						if (!Global.level.gameMode.isTeamMode) {
							alliance = id;
						}
						break;
					}
				}

				var cpu = new Player("CPU" + id.ToString(), id, charNum, null, true, true, alliance, new Input(true), null);
				Global.level.players.Add(cpu);
				Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry("Added bot " + cpu.name + ".", null, null, true));
			}
			Menu.change(previous);
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(previous);
		}
	}

	public void render() {
		DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
		Global.sprites["cursor"].drawToHUD(0, 15, optionPoses[(int)selectArrowPosY].y + 5);

		Fonts.drawText(FontType.Orange, "Add Bot", Global.halfScreenW, 15, alignment: Alignment.Center);

		string botTeamStr = "(Auto)";
		if (botTeamNum == GameMode.blueAlliance) botTeamStr = "Blue";
		else if (botTeamNum == GameMode.redAlliance) botTeamStr = "Red";

		string botCharStr = "(Random)";
		if (botCharNum == 0) botCharStr = "Mega Man X";
		else if (botCharNum == 1) botCharStr = "Zero";
		else if (botCharNum == 2) botCharStr = "Vile";
		else if (botCharNum == 3) botCharStr = "Axl";
		else if (botCharNum == 4) botCharStr = "Sigma";

		Fonts.drawText(
			FontType.Blue, "Character: " + botCharStr,
			optionPoses[0].x, optionPoses[0].y, selected: selectArrowPosY == 0
		);
		Fonts.drawText(
			teamOptionEnabled() ? FontType.Green: FontType.DarkGreen, "Team: " + botTeamStr,
			optionPoses[1].x, optionPoses[1].y,
			selected: selectArrowPosY == 1
		);

		Fonts.drawTextEX(
			FontType.Grey, "Left/Right: Change, [OK]: Add, [BACK]: Back",
			Global.screenW * 0.5f, 200, Alignment.Center
		);
	}

	public bool teamOptionEnabled() {
		if (!Global.level.gameMode.isTeamMode) return false;
		if (Global.serverClient == null) return true;
		if (Global.level?.server != null && Global.level.server.hidden) return true;
		return false;
	}
}
