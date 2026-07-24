using System.Collections.Generic;

namespace MMXOnline;

public class FFADeathMatch : GameMode {
	public FFADeathMatch(Level level, int killsToWin, int? timeLimit) : base(level, timeLimit) {
		playingTo = killsToWin;
	}

	public override void render() {
		base.render();
	}

	public override void checkIfWinLogic() {
		checkIfWinLogicDM();
	}

	public override void drawTopHUD() {
		List<Player> playerList = getOrderedPlayerList();
		string topText = "Leader: 0";
		if (playerList.Count > 0) {
			topText = "Leader:" + playerList[0].score.ToString().PadLeft(2 ,' ');
		}
		string botText = "Kills:" + level.mainPlayer.score.ToString().PadLeft(2 ,' ');
		float mapOffset = shouldDrawRadar() ? 0 : 48;
		Fonts.drawText(FontType.WhiteSmall, botText, Global.screenW - 56 + mapOffset, 7, Alignment.Right);
		Fonts.drawText(FontType.WhiteSmall, topText, Global.screenW - 56 + mapOffset, 17, Alignment.Right);

		drawTimeIfSet(37);
	}

	public override void drawScoreboard() {
		base.drawScoreboard();
	}

	public override void reportKill(bool isAssist, Player killer, Player victim, bool isSummon = false) {
		// We ignore suicides.
		if (killer.alliance != victim.alliance) {
			killToScore(killer, victim);
		}
	}
}
