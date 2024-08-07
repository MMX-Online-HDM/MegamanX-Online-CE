using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class ControlPoints : GameMode {
	Sprite controlPointSprite = new Sprite("hud_cp");
	Sprite controlPointBarSprite = new Sprite("hud_cp_bar");

	public ControlPoints(Level level, int? timeLimit) : base(level, timeLimit) {
		isTeamMode = true;
		playingTo = 100;
	}

	public override void render() {
		base.render();
		var currentControlPoint = level.getCurrentControlPoint();
		if (currentControlPoint != null) {
			drawObjectiveNavpoint(Global.level.mainPlayer.alliance == redAlliance ? "Defend" : "Attack", currentControlPoint.pos);
		}
	}

	public override void drawTopHUD() {
		//var redText = "CP " + bluePoints.ToString() + "%";

		int hudcpY = 0;
		foreach (var controlPoint in level.controlPoints) {
			var redText = "";

			FontType textColor = FontType.RedishOrange;
			if (controlPoint.captureTime == controlPoint.maxCaptureTime) {
				redText = "Lock";
				textColor = FontType.Blue;
			} else if (controlPoint.attacked()) {
				redText += string.Format("{0}x", controlPoint.getAttackerCount());
			} else if (controlPoint.contested()) {
				redText = "Block";
				textColor = FontType.Red;
			} else if (controlPoint.locked) {
				redText = "Lock";
				if (controlPoint.captureTime > 0) {
					textColor = FontType.Green;
				} else {
					textColor = FontType.Grey;
				}
			}

			int allianceFactor = (controlPoint.alliance == GameMode.redAlliance ? 0 : 2);
			controlPointSprite.drawToHUD((controlPoint.num - 1) + allianceFactor, 5, 5 + hudcpY);
			float perc = controlPoint.captureTime / controlPoint.maxCaptureTime;
			int barsFull = MathInt.Round(perc * 16f);
			for (int i = 0; i < barsFull; i++) {
				controlPointBarSprite.drawToHUD(1, 5 + 17 + (i * 2), 5 + 3 + hudcpY);
			}

			Fonts.drawText(textColor, redText, 38, 9 + hudcpY, Alignment.Center);
			hudcpY += 16;
		}

		drawTimeIfSet(4 + hudcpY);
	}

	public override void checkIfWinLogic() {
		if (level.controlPoints.All(c => c.captured)) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = new HashSet<int>() { blueAlliance },
				winMessage = "Victory!",
				winMessage2 = "Blue team wins",
				loseMessage = "You lost!",
				loseMessage2 = "Blue team wins"
			};
		} else if (remainingTime <= 0 && level.controlPoints.All(c => c.captured || c.captureTime <= 0)) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = new HashSet<int>() { redAlliance },
				winMessage = "Victory!",
				winMessage2 = "Red team wins",
				loseMessage = "You lost!",
				loseMessage2 = "Red team wins"
			};
		}
	}

	public override void drawScoreboard() {
		drawTeamScoreboard();
	}
}
