namespace MMXOnline;

public class CTF : GameMode {
	int neutralKillLimit;

	public CTF(
		Level level, int playingTo,
		int? timeLimit, int neutralKillLimit = 100
	) : base(level, timeLimit) {
		this.playingTo = playingTo;
		this.neutralKillLimit = neutralKillLimit;
		isTeamMode = true;
		useTeamSpawns = true;
	}

	public override void render() {
		base.render();
		if (level.mainPlayer.character == null) return;
		bool isBlue = level.mainPlayer.alliance == blueAlliance;
		bool isRed = level.mainPlayer.alliance == redAlliance;

		// Blue flag.
		if (isBlue && level.redFlag.linkedChar == level.mainPlayer.character) {
			addMapNavpoint(level.blueFlag.pedestal.pos, ("hud_minimap_hill", 1));
		}
		else if (level.redFlag.linkedChar == null || Global.floorFrameCount % 8 < 4) {
			addMapNavpoint(level.redFlag.pos, ("hud_minimap_flag", 3));
		} else {
			addMapNavpoint(level.redFlag.pos, ("hud_minimap_flag", 1));
		}
		// Red flag.
		if (isRed && level.blueFlag.linkedChar == level.mainPlayer.character) {
			addMapNavpoint(level.redFlag.pedestal.pos, ("hud_minimap_hill", 1));
		}
		else if (level.blueFlag.linkedChar == null || Global.floorFrameCount % 8 < 4) {
			addMapNavpoint(level.blueFlag.pos, ("hud_minimap_flag", 2));
		} else {
			addMapNavpoint(level.redFlag.pos, ("hud_minimap_flag", 1));
		}

		if (level.mainPlayer.alliance > redAlliance) { return; }
		drawObjectiveNavpoint(
			"Capture",
			level.mainPlayer.alliance == redAlliance ? level.blueFlag.pos : level.redFlag.pos
		);
		if (level.mainPlayer.character?.flag != null) {
			drawObjectiveNavpoint(
				"Return",
				level.mainPlayer.alliance == redAlliance ?
				level.redFlag.pedestal.pos : level.blueFlag.pedestal.pos
			);
		} else {
			drawObjectiveNavpoint(
				"Defend", level.mainPlayer.alliance == redAlliance ?
				level.redFlag.pos : level.blueFlag.pos
			);
		}
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
}
