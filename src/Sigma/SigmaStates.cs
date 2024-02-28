using System;

namespace MMXOnline;

public class IssueGlobalCommand : CharState {
	public IssueGlobalCommand(string transitionSprite = "") : base("summon_maverick", "", "", transitionSprite) {
		superArmor = true;
	}

	public override void update() {
		base.update();

		if (character.isAnimOver()) {
			character.changeState(new Idle(), true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
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
			character.changeState(new Idle(), true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (!isNew) {
			if (maverick.state is not MExit) {
				maverick.changeState(new MExit(character.pos, isRecall));
			}
			else {
				maverick.changeState(new MEnter(character.pos));
			}
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
	}
}
