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
		int place = playerList.IndexOf(level.mainPlayer) + 1;
		string placeStr = Helpers.getNthString(place);
		string topText = "Leader: 0";
		if (playerList.Count > 0) {
			topText = "Leader: " + playerList[0].score.ToString();
		}
		string botText = $"Kills: {level.mainPlayer.score} [{placeStr}]";
		Fonts.drawText(FontType.BlueMenu, topText, 5, 5, Alignment.Left);
		Fonts.drawText(FontType.BlueMenu, botText, 5, 15, Alignment.Left);

		drawTimeIfSet(25);
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
