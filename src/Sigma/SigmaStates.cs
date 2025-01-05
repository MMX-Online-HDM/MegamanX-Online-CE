namespace MMXOnline;

public class IssueGlobalCommand : CharState {
	public IssueGlobalCommand(string transitionSprite = "") : base("summon_maverick", "", "", transitionSprite) {
		superArmor = true;
	}

	public override void update() {
		base.update();

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}

public class CallDownMaverick : CharState {
	Maverick maverick;
	bool isNew;
	bool isRecall;
	int frame;
	public CallDownMaverick(
		Maverick maverick, bool isNew, bool isRecall, string transitionSprite = ""
	) : base(
		"summon_maverick", "", "", transitionSprite
	) {
		this.maverick = maverick;
		this.isNew = isNew;
		this.isRecall = isRecall;
		superArmor = true;
	}

	public override void update() {
		base.update();

		frame++;

		if (frame > 0 && frame < 10 && (player.isStriker() || player.isSummoner())) {
			if (player.input.isPressed(Control.Shoot, player) &&
				maverick.startMoveControl == Control.Special1
			) {
				maverick.startMoveControl = Control.Dash;
			} else if (
				player.input.isPressed(Control.Special1, player) &&
				maverick.startMoveControl == Control.Shoot
			) {
				maverick.startMoveControl = Control.Dash;
			}
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (!isNew) {
			if (maverick.state is not MExit) {
				maverick.changeState(new MExit(character.pos, isRecall));
			} else {
				maverick.changeState(new MEnter(character.pos));
			}
		}
	}
}

public class SigmaBlock : CharState {
	public SigmaBlock() : base("block") {
		superArmor = true;
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
		stunResistant = true;
	}

	public override void update() {
		base.update();

		bool isHoldingGuard = player.isCrouchHeld();
		if (!player.isControllingPuppet()) {
			bool leftGuard = player.input.isHeld(Control.Left, player);
			bool rightGuard = player.input.isHeld(Control.Right, player);

			if (leftGuard) character.xDir = -1;
			else if (rightGuard) character.xDir = 1;
		}
		if (!isHoldingGuard) {
			character.changeToIdleOrFall();
			return;
		}
		if (Global.level.gameMode.isOver) {
			if (Global.level.gameMode.playerWon(player)) {
				if (!character.sprite.name.Contains("_win")) {
					character.changeSpriteFromName("win", true);
				}
			} else {
				if (!character.sprite.name.Contains("lose")) {
					character.changeSpriteFromName("lose", true);
				}
			}
		}
	}
}
