using System.Collections.Generic;
using Newtonsoft.Json;

namespace MMXOnline;

public class KickMenu : IMainMenu {
	public int selectArrowPosY;
	public IMainMenu previous;
	public bool listenForKey = false;
	public int kickReasonIndex;
	public List<string> kickReasons = new List<string>()
	{
			"(None specified)",
			"AFK player",
			"Toxicity",
			"Buggy/laggy player",
			"Cheater/exploiter",
			"Reserved match"
		};
	public int kickDuration = 1;
	Player player;

	public List<Point> optionPoses = new List<Point>()
	{
			new Point(30, 100),
			new Point(30, 120)
		};
	public int ySep = 10;

	public KickMenu(IMainMenu mainMenu, Player player) {
		previous = mainMenu;
		this.player = player;
	}

	public void update() {
		Helpers.menuUpDown(ref selectArrowPosY, 0, optionPoses.Count - 1);
		if (selectArrowPosY == 0) {
			if (Global.input.isPressedMenu(Control.MenuLeft)) {
				kickReasonIndex--;
				if (kickReasonIndex < 0) kickReasonIndex = kickReasons.Count - 1;
			} else if (Global.input.isPressedMenu(Control.MenuRight)) {
				kickReasonIndex++;
				if (kickReasonIndex > kickReasons.Count - 1) kickReasonIndex = 0;
			}
		}
		if (selectArrowPosY == 1) {
			if (Global.input.isPressedMenu(Control.MenuLeft)) {
				kickDuration--;
				if (kickDuration < 0) kickDuration = 1000;
			} else if (Global.input.isPressedMenu(Control.MenuRight)) {
				kickDuration++;
				if (kickDuration > 1000) kickDuration = 0;
			}
		}

		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			string voteKickPrefix = "";
			if (!hasDirectKickPower()) voteKickPrefix = "Start Vote ";
			string kickMsg = string.Format(voteKickPrefix + "Kick player {0}\nfor {1} minutes?", player.name, kickDuration);
			Menu.change(new ConfirmLeaveMenu(this, kickMsg, () => {
				if (hasDirectKickPower()) {
					var kickPlayerObj = new RPCKickPlayerJson(VoteType.Kick, player.name, player.serverPlayer.deviceId, kickDuration, kickReasons[kickReasonIndex]);
					Global.serverClient?.rpc(RPC.kickPlayerRequest, JsonConvert.SerializeObject(kickPlayerObj));
					Menu.exit();
				} else {
					VoteKick.initiate(player, VoteType.Kick, kickDuration, kickReasons[kickReasonIndex]);
					Menu.exit();
				}
			}));
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(previous);
		}
	}

	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		Global.sprites["cursor"].drawToHUD(0, 15, optionPoses[(int)selectArrowPosY].y + 5);

		string voteKickPrefix = "";
		if (!hasDirectKickPower()) voteKickPrefix = "Vote ";

		Fonts.drawText(
			FontType.Yellow, voteKickPrefix + "Kick Player " + player.name,
			Global.halfScreenW, 20, alignment: Alignment.Center
		);

		Fonts.drawText(
			FontType.Blue, "Kick reason: " + kickReasons[kickReasonIndex],
			optionPoses[0].x, optionPoses[0].y
		);
		Fonts.drawText(
			FontType.Blue, "Kick duration: " + kickDuration + " min",
			optionPoses[1].x, optionPoses[1].y
		);

		Fonts.drawTextEX(
			FontType.Grey, "[MLEFT]/[MRIGHT]: Change, [OK]: Kick, [BACK]: Back",
			Global.screenW * 0.5f, Global.screenH - 20, Alignment.Center
		);
	}

	public static bool hasDirectKickPower() {
		return Global.isHost && Global.level.server.hidden;
	}
}
