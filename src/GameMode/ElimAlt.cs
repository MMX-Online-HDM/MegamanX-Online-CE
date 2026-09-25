using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class ElimAlt : GameMode {
	public RPCMatchOverResponse? roundResult;
	public Player? lastPlayer0 = null;
	public Player? lastPlayer1 = null;
	public float roundDrawTime;
	public float resultTime;
	public float resultMaxTime = 60 * 6;
	public float respawnWindow = 60 * 10;
	public float respawnMaxWindow = 60 * 6;
	public float roundTime;
	public float roundMaxTime;

	public ElimAlt(Level level, int playingTo, int? timeLimit) : base(level, null) {
		this.playingTo = playingTo;
		isElim = true;
		// Time stuff.
		finalZoneMaxTime1 = 30;
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
		if (isOver) {
			resultTime = 0;
		}
		base.update();
	}

	public override void render() {
		base.render();
		drawResult();
	}

	public void drawResult() {
		if (resultTime <= 0 || roundResult == null || isOver) {
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
		List<Player> activePlayers = Global.level.nonSpecPlayers();
		int maxPlayers = activePlayers.Count;
		Player mainPlayer = Global.level.mainPlayer;
		float mapOffset = shouldDrawRadar() ? 0 : 48;

		// Draw points.
		string teamText = $"P: {mainPlayer.score}";
		Fonts.drawText(
			FontType.Grey, teamText,
			Global.screenW - 56 + mapOffset, 7, Alignment.Right
		);
		int textSize = Fonts.measureText(FontType.Grey, teamText);
		// Get max score.
		int leaderScore = -1;
		foreach (Player player in activePlayers) {
			if (player.score >= leaderScore) {
				leaderScore = player.score;
			}
		}
		Fonts.drawText(
			FontType.Grey, $"L: {leaderScore} ",
			Global.screenW - 56 + mapOffset - textSize, 7, Alignment.Right
		);
		// Draw lives.
		Player[] playersAlive = activePlayers.Where(p => !p.isSpectator && p.elimAlive).ToArray();
		Fonts.drawText(
			FontType.Grey, $"Alive: {playersAlive.Length}",
			Global.screenW - 56 + mapOffset, 17, Alignment.Right
		);
		drawTimeIfSet(37, finalZoneTime);
	}

	public override void checkIfWinLogic() {
		roundWinLogic();
		checkIfWinLogicDM();
	}

	public void roundWinLogic() {
		// If we are respawning then we just continue.
		if (respawnWindow > 0 && resultTime > 0 && Global.level.time >= 10 || isOver) {
			return;
		}
		// Vars.
		List<Player> playersAliveList = new();
		int playersAlive = 0;
		int playersActive = 0;
		// Check what is alive.
		foreach (Player player in level.players) {
			if (!player.isSpectator && player.spawnedOnceAlt) {
				if (player.elimAlive) {
					playersAlive++;
					playersAliveList.Add(player);
				}
				playersActive++;
			}
		}
		// We wait if we are at 1 or less total teams.
		if (playersActive < 2) {
			return;
		}
		// Stalemate if we go over the theshold.
		// Entering draw time disables stalemate.
		if (remainingTime <= 0 && virusStarted >= 3 && roundDrawTime == 0) {
			addResult(new RPCMatchOverResponse() {
				winningAlliances = [nullAlliance],
				winMessage = "Stalemate!",
				loseMessage = "Stalemate!"
			}, true);
			return;
		}
		// If somehow everyone died during draw time then we go into draw.
		if (playersAlive == 0 && roundDrawTime > 0) {
			string messageSub = "No one won the round";
			if (lastPlayer0 != null && lastPlayer1 != null) {
				messageSub = (
					$"{lastPlayer0.name} and {lastPlayer1.name} wins the round"
				);
			}
			addResult(new RPCMatchOverResponse() {
				winningAlliances = [
					lastPlayer0?.alliance ?? nullAlliance,
					lastPlayer1?.alliance ?? nullAlliance
				],
				winMessage = "Draw!",
				loseMessage = "Draw!",
				winMessage2 = messageSub,
				loseMessage2 = messageSub
			}, true);
			return;
		}
		// Save the last 2 teams for draw conditions.
		if (playersAlive == 2) {
			lastPlayer0 = playersAliveList[0];
			lastPlayer1 = playersAliveList[1];
		}
		// Return if more than 1 team is alive.
		if (playersAlive > 1) {
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
		string message2 = $"{playersAliveList[0].name} wins the round";
		// Generate the standart response.
		addResult(new RPCMatchOverResponse() {
			winningAlliances = [playersAliveList[0].alliance],
			winMessage = "Victory!",
			winMessage2 = message2,
			loseMessage = "Defeat!",
			loseMessage2 = message2
		}, true);
	}

	public void addResult(RPCMatchOverResponse result, bool sendRpc = false) {
		roundResult = result;
		resultTime = resultMaxTime;
		respawnWindow = respawnMaxWindow;
		lastPlayer0 = null;
		lastPlayer1 = null;
		roundTime = roundMaxTime + finalZoneMaxTime1 + finalZoneMaxTime2;
		finalZoneTime = roundMaxTime;
		eliminationTime = 0;
		virusStarted = 0;

		foreach (Player player in Global.level.players) {
			if (player.character == null) {
				player.spawnedOnceAlt = false;
			}
			if (player.isSpectator) {
				continue;
			}
			if (!result.winningAlliances.Contains(player.alliance)) {
				player.awardCurrency();
				continue;
			}
			player.awardCurrency();
			player.awardCurrency();
			player.score++;

			if (player.character != null) {
				player.character.addHealth(player.character.maxHealth);
				player.character.addPercentAmmo(100);
			}
		}

		if (result.winningAlliances.Contains(Global.level.mainPlayer.alliance)) {
			Global.playSound("ching");
		} else {
			Global.playSound("error");
		}
		if (sendRpc) {
			RPC.syncEAltRound.sendRpc(result);
		}
	}
}
