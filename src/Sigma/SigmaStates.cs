namespace MMXOnline;

public class IssueGlobalCommand : CharState {
	public bool isAutoGuarding;

	public IssueGlobalCommand() : base("summon_maverick") {
		superArmor = true;
	}

	public override void update() {
		base.update();

		if (character.isAnimOver() || isAutoGuarding && stateFrames > 20) {
			if (isAutoGuarding && character.grounded && character.isControllingPuppet()) {
				character.changeState(new SigmaAutoBlock(), true);
				return;
			}
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (oldState is SigmaAutoBlock) {
			isAutoGuarding = true;
			sprite = "block_auto";
			character.changeSpriteFromName(sprite, true);
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
		airMove = true;
	}

	public override void update() {
		base.update();

		frame++;

		if (frame > 0 && frame < 10 && maverick.controlMode is MaverickModeId.Striker or MaverickModeId.Summoner) {
			/*if (player.input.isPressed(Control.Shoot, player) &&
				maverick.startMoveControl == Control.Special1
			) {
				maverick.startMoveControl = Control.Dash;
			} else if (
				player.input.isPressed(Control.Special1, player) &&
				maverick.startMoveControl == Control.Shoot
			) {
				maverick.startMoveControl = Control.Dash;
			} */
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
		stunResistant = true;
		immuneToWind = true;
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();
		bool isHoldingGuard = player.isCrouchHeld();
		int inputXDir = player.input.getXDir(player); 
		if (inputXDir != 0) {
			character.xDir = inputXDir;
		}
		if (!isHoldingGuard || Global.level.gameMode.isOver) {
			character.changeToIdleOrFall();
			return;
		}
	}
}

public class SigmaAutoBlock : CharState {
	public SigmaAutoBlock() : base("block_auto") {
		superArmor = true;
		immuneToWind = true;
		exitOnAirborne = true;
	}

	public override void update() {
		base.update();

		if (!character.isControllingPuppet() || Global.level.gameMode.isOver) {
			character.changeToIdleOrFall();
			return;
		}
	}
}
