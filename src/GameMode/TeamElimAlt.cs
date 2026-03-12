using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class TeamElimAlt : GameMode {
	public RPCMatchOverResponse? roundResult;
	public int[] last2Teams = [nullAlliance, nullAlliance];
	public float roundDrawTime;
	public float resultTime;
	public float resultMaxTime = 60 * 6;
	public float respawnWindow = 60 * 10;
	public float respawnMaxWindow = 60 * 6;
	public float roundTime;
	public float roundMaxTime;

	public TeamElimAlt(Level level, int playingTo, int? timeLimit) : base(level, null) {
		this.playingTo = playingTo;
		isTeamMode = true;
		// Time stuff.
		finalZoneMaxTime2 = 60;
		timeLimit ??= 1;
		float timeLimitF = timeLimit.Value * 60;

		this.roundMaxTime = timeLimitF;
		roundTime = roundMaxTime + finalZoneMaxTime1 + finalZoneMaxTime2;
		finalZoneTime = roundMaxTime;
		startTimeLimit = roundTime;
	}

	public override bool canRespawn() => respawnWindow > 0 && resultTime < resultMaxTime - 60 * 2;
	public override bool forceRespawn() => (
		respawnWindow > 0
	);

	public override void update() {
		Helpers.decrementFrames(ref respawnWindow);
		Helpers.decrementFrames(ref resultTime);
		Helpers.decrementTime(ref roundTime);
		if (matchOverResponse != null) {
			resultTime = 0;
		} 

		base.update();
		if (Global.isHost) {
			roundWinLogic();
		}
	}


	public override void render() {
		base.render();
		drawResult();
	}

	public void drawResult() {
		if (resultTime <= 0 || roundResult == null) {
			return;
		}
		Player mainPlayer = Global.level.mainPlayer;
		string text;
		string subtitle;

		if (roundResult.winningAlliances.Contains(mainPlayer.alliance)) {
			text = roundResult.winMessage;
			subtitle = roundResult.winMessage2;
		} else {
			text = roundResult.loseMessage;
			subtitle = roundResult.loseMessage2;
		}

		// Title
		float titleY = Global.halfScreenH;
		// Subtitle
		float subtitleY = titleY + 16;
		// Offsets.
		float hh = 8;
		float hh2 = 16;
		if (subtitle == "") {
			subtitleY = titleY;
		}
		int offset = MathInt.Floor(((subtitleY + hh2) - (titleY - hh)) / 2);
		titleY -= offset;
		subtitleY -= offset;

		// Box
		DrawWrappers.DrawRect(
			0, titleY - hh,
			Global.screenW, subtitleY + hh2,
			true, new Color(0, 0, 0, 192), 1, ZIndex.HUD,
			isWorldPos: false, outlineColor: Color.White
		);
		// Title
		Fonts.drawText(
			FontType.Grey, text.ToUpperInvariant(),
			Global.halfScreenW, titleY, Alignment.Center
		);
		// Subtitle
		Fonts.drawText(
			FontType.Grey, subtitle,
			Global.halfScreenW, subtitleY, Alignment.Center
		);
	}

	public override void drawTopHUD() {
		int teamSide = Global.level.mainPlayer.teamAlliance ?? -1;
		if (teamSide < 0 || Global.level.mainPlayer.isSpectator) {
			drawAllTeamsHUD();
			return;
		}
		int maxTeams = Global.level.teamNum;
		Player mainPlayer = Global.level.mainPlayer;

		string teamText = $"{teamNames[teamSide]}: {teamPoints[teamSide]}";
		Fonts.drawText(teamFonts[teamSide], teamText, 5, 5);

		int leaderTeam = 0;
		int leaderScore = -1;
		bool moreThanOneLeader = false;
		for (int i = 0; i < Global.level.teamNum; i++) {
			if (teamPoints[i] >= leaderScore) {
				leaderTeam = i;
				if (leaderScore == teamPoints[i]) {
					moreThanOneLeader = true;
				}
				leaderScore = teamPoints[i];
			}
		}
		if (!moreThanOneLeader) {
			Fonts.drawText(teamFonts[leaderTeam], $"Leader: {leaderScore}", 5, 15);
		} else {
			Fonts.drawText(FontType.Grey, $"Leader: {leaderScore}", 5, 15);
		}
		// Draw lives.
		Player[] playersAlive = level.players.Where(p => !p.isSpectator && p.elimAlive).ToArray();
		Player[] alliesAlive = playersAlive.Where(p => p.alliance == mainPlayer.alliance).ToArray();

		Fonts.drawText(
			FontType.RedishOrange, $"Alive: {alliesAlive.Length} / {playersAlive.Length}", 5, 25
		);

		drawTimeIfSet(35, finalZoneTime);
	}

	public override void checkIfWinLogic() {
		checkIfWinLogicTeams();
	}

	public void roundWinLogic() {
		// If we are respawning then we just continue.
		if (respawnWindow > 0) {
			return;
		}
		// Stalemate if we go over the theshold.
		// Entering draw time disables stalemate.
		if (remainingTime <= 0 && virusStarted >= 3 && roundDrawTime == 0) {
			addResult(new RPCMatchOverResponse() {
				winningAlliances = [nullAlliance],
				winMessage = "Stalemate!",
				loseMessage = "Stalemate!"
			});
			return;
		}
		// Vars.
		bool[] teamsAlive = new bool[level.teamNum];
		bool[] teamsActive = new bool[level.teamNum];
		HashSet<int> teamsAliveHash = [];
		int teamNumAlive = 0;
		int teamNumActive = 0;
		// Check what is alive.
		foreach (Player player in level.players) {
			if (player.teamAlliance != null &&
				player.teamAlliance >= 0 && player.teamAlliance < level.teamNum &&
				!player.isSpectator 
			) {
				if (player.elimAlive && teamsAlive[player.teamAlliance.Value] != true) {
					teamsAlive[player.teamAlliance.Value] = true;
					teamsAliveHash.Add(player.teamAlliance.Value);
					teamNumAlive++;
				}
				if (teamsActive[player.teamAlliance.Value] != true) {
					teamsActive[player.teamAlliance.Value] = true;
					teamNumActive++;
				}
			}
		}
		// We wait if we are at 1 or less total teams.
		if (teamNumActive < 2) {
			return;
		}
		// If somehow everyone died during draw time then we go into draw.
		if (teamNumAlive == 0 && roundDrawTime > 0) {
			string messageSub = "No one won the round";
			if (last2Teams[0] != nullAlliance) {
				messageSub = (
					$"{teamNames[last2Teams[0]]} and {teamNames[last2Teams[1]]} wins the round"
				);
			}
			addResult(new RPCMatchOverResponse() {
				winningAlliances = [last2Teams[0], last2Teams[1]],
				winMessage = "Draw!",
				loseMessage = "Draw!",
				winMessage2 = messageSub,
				loseMessage2 = messageSub
			});
			return;
		}
		// Save the last 2 teams for draw conditions.
		if (teamNumAlive == 2) {
			int[] teamsAliveArray = teamsAliveHash.ToArray();
			last2Teams[0] = teamsAliveArray[0];
			last2Teams[1] = teamsAliveArray[1];
		}
		// Return if more than 1 team is alive.
		if (teamNumAlive > 1) {
			roundDrawTime = 0;
			return;
		}
		// Give 1 second a frame window for draws.
		// This means a kill-sucide will not give you a win.
		roundDrawTime += Global.speedMul;
		if (roundDrawTime < drawMaxTime) {
			return;
		}
		roundDrawTime = 0;
		// Get winning team id.
		int winningAlliance = teamsAlive.IndexOf(true);
		string message2 = $"{teamNames[winningAlliance]} wins the round";
		// Generate the standart response.
		addResult(new RPCMatchOverResponse() {
			winningAlliances = [winningAlliance],
			winMessage = "Victory!",
			winMessage2 = message2,
			loseMessage = "Defeat!",
			loseMessage2 = message2
		});
	}

	public void addResult(RPCMatchOverResponse result) {
		roundResult = result;
		resultTime = resultMaxTime;
		respawnWindow = respawnMaxWindow;
		last2Teams[0] = nullAlliance;
		last2Teams[1] = nullAlliance;
		roundTime = roundMaxTime + finalZoneMaxTime1 + finalZoneMaxTime2;
		finalZoneTime = roundMaxTime;
		eliminationTime = 0;
		virusStarted = 0;

		foreach (int alliance in result.winningAlliances) {
			if (alliance != nullAlliance) {
				Global.level.gameMode.teamPoints[alliance]++;
			}
		}

		foreach (Player player in Global.level.players) {
			if (player.isSpectator || !result.winningAlliances.Contains(player.alliance)) {
				continue;
			}
			player.awardCurrency();
			player.awardCurrency();
			player.awardCurrency();
			player.awardCurrency();
			player.awardCurrency();

			if (player.character != null) {
				player.character.addHealth(player.character.maxHealth, false);
				player.character.addPercentAmmo(100);
			}
		}

		if (result.winningAlliances.Contains(Global.level.mainPlayer.alliance)) {
			Global.playSound("ching");
		} else {
			Global.playSound("error");
		}
	}

	public override void drawScoreboard() {
		drawTeamScoreboard();
	}
}
