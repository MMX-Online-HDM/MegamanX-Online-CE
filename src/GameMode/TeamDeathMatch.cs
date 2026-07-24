namespace MMXOnline;

public class TeamDeathMatch : GameMode {
	public TeamDeathMatch(Level level, int playingTo, int? timeLimit) : base(level, timeLimit) {
		this.playingTo = playingTo;
		isTeamMode = true;
		spawnOnAlly = true;
	}

	public override void drawTopHUD() {
		drawTeamTopHUD();
	}

	public override void checkIfWinLogic() {
		checkIfWinLogicTeams();
	}

	public override void drawScoreboard() {
		drawTeamScoreboard();
	}

	public override void reportKill(bool isAssist, Player killer, Player victim, bool isSummon = false) {
		// We do not count assists in TDM.
		if (!Global.isHost || isAssist) {
			return;
		}
		// We also ignore ally kills.
		// These are rare as it means the user was never hit by an enemy.
		if (killer.alliance != victim.alliance) {
			killToScore(killer, victim);
			Global.level.gameMode.teamPoints[killer.alliance]++;
			Global.level.gameMode.syncTeamScores();
		}
	}
}

