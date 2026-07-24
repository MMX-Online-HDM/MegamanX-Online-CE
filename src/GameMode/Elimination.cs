using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class Elimination : GameMode {
	public Elimination(Level level, int lives, int? timeLimit) : base(level, timeLimit) {
		playingTo = lives;
		isElim = true;
		if (remainingTime == null && !level.is1v1()) {
			remainingTime = 300;
			startTimeLimit = remainingTime;
		}
	}

	public override void render() {
		base.render();
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
		// Check what is alive.
		List<Player> playersAlive = [];
		foreach (Player player in level.players) {
			if (!player.isSpectator && player.score < playingTo) {
				playersAlive.Add(player);
			}
		}
		// If somehow everyone died during draw time then we go into draw.
		if (playersAlive.Count == 0 && drawTime > 0) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = [nullAlliance],
				winMessage = "Draw!",
				loseMessage = "Draw!"
			};
			return;
		}
		// Return if more than 1 team is alive.
		if (playersAlive.Count > 1) {
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
		int winningAlliance = playersAlive[0].alliance;
		string message2 = $"{playersAlive[0].name} wins";
		// Generate the standart response.
		matchOverResponse = new RPCMatchOverResponse() {
			winningAlliances = [winningAlliance],
			winMessage = "Victory!",
			winMessage2 = message2,
			loseMessage = "You lost!",
			loseMessage2 = message2
		};
	}

	public override void drawTopHUD() {
		Player[] playersStillAlive = level.players.Where(p => !p.isSpectator && p.score < playingTo).ToArray();
		int lives = playingTo - level.mainPlayer.score;
		string topText = "Lives:" + lives.ToString().PadLeft(2 ,' ');
		string botText = "Alive:" + (playersStillAlive.Length).ToString().PadLeft(2 ,' ');
		float mapOffset = shouldDrawRadar() ? 0 : 48;
		Fonts.drawText(FontType.WhiteSmall, botText,  Global.screenW - 56 + mapOffset, 7, Alignment.Right);
		Fonts.drawText(FontType.WhiteSmall, topText,  Global.screenW - 56 + mapOffset, 17, Alignment.Right);

		if (virusStarted != 1) {
			drawTimeIfSet(37);
		} else {
			drawVirusTime(37);
		}
	}

	public override void drawScoreboard() {
		base.drawScoreboard();
	}

	public override void reportKill(bool isAssist, Player killer, Player victim, bool isSummon = false) {
		// We ignore assist, as are reported alonside kills.
		// Same with summons, we only count if the main entity dies.
		// Elim score works backwards, so we add kills.
		if (!isAssist && !isSummon) {
			deathToScore(killer, victim);
		}
	}
}
