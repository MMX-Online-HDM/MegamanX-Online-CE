using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class TeamElimination : GameMode {
	public TeamElimination(Level level, int playingTo, int? timeLimit) : base(level, timeLimit) {
		this.playingTo = playingTo;
		isTeamMode = true;
		isElim = true;
		spawnOnAlly = true;

		if (remainingTime == null) {
			remainingTime = 300;
			startTimeLimit = remainingTime;
		}
	}

	public override void drawTopHUD() {
		/*Player[] enemyPlayersAlive = level.players.Where(
			p => !p.isSpectator && p.deaths < playingTo && p.alliance != Global.level.mainPlayer.alliance
		).ToArray();*/
		Player[] allyPlayersAlive = level.players.Where(
			p => !p.isSpectator && p.score < playingTo && p.alliance == Global.level.mainPlayer.alliance
		).ToArray();
		FontType fontColor = teamFontsSmall[mainPlayer.alliance];
		int lives = playingTo - level.mainPlayer.score;
		string topText = "Allies:" + (allyPlayersAlive.Length).ToString().PadLeft(2 ,' ');
		string botText = "Lives:" + lives.ToString().PadLeft(2 ,' ');
		float mapOffset = shouldDrawRadar() ? 0 : 48;
		Fonts.drawText(fontColor, topText,  Global.screenW - 56 + mapOffset, 7, Alignment.Right);
		Fonts.drawText(FontType.WhiteSmall, botText,  Global.screenW - 56 + mapOffset, 17, Alignment.Right);

		if (virusStarted != 1) {
			drawTimeIfSet(37);
		} else {
			drawVirusTime(37);
		}
	}

	public override void checkIfWinLogic() {
		// If we have a response we just continue.
		if (matchOverResponse != null) {
			return;
		}
		// 15s join window.
		if (level.time < 15) {
			return;
		}
		// Stalemate if we go over the theshold.
		// Entering draw time disables stalemate.
		if (remainingTime <= 0 && virusStarted >= 3 && drawTime == 0) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = [nullAlliance],
				winMessage = "Stalemate!",
				loseMessage = "Stalemate!"
			};
			return;
		}
		// Vars.
		bool[] teamsAlive = new bool[level.teamNum];
		int teamNumAlive = 0;
		// Check what is alive.
		foreach (Player player in level.players) {
			if (player.teamAlliance != null &&
				player.teamAlliance >= 0 && player.teamAlliance < level.teamNum &&
				!player.isSpectator && player.score < playingTo
			) {
				if (teamsAlive[player.teamAlliance.Value] != true) {
					teamsAlive[player.teamAlliance.Value] = true;
					teamNumAlive++;
				}
			}
		}
		// If somehow everyone died during draw time then we go into draw.
		if (teamNumAlive == 0 && drawTime > 0) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = [nullAlliance],
				winMessage = "Draw!",
				loseMessage = "Draw!"
			};
			return;
		}
		// Return if more than 1 team is alive.
		if (teamNumAlive > 1) {
			drawTime = 0;
			return;
		}
		// Give 1 second a frame window for draws.
		// This means a kill-sucide will not give you a win.
		drawTime += Global.speedMul;
		if (drawTime < drawMaxTime) {
			return;
		}
		drawTime = 0;
		// Get winning team id.
		int winningAlliance = teamsAlive.IndexOf(true);
		string message2 = $"{teamNames[winningAlliance]} team wins";
		// Generate the standart response.
		matchOverResponse = new RPCMatchOverResponse() {
			winningAlliances = [winningAlliance],
			winMessage = "Victory!",
			winMessage2 = message2,
			loseMessage = "You lost!",
			loseMessage2 = message2
		};
	}

	public override void drawScoreboard() {
		base.drawTeamScoreboard();
	}
}
