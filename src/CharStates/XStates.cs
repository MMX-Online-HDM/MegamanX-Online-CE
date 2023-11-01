namespace MMXOnline;

public class XHover : CharState {
	float hoverTime;
	int startXDir;
	public XHover() : base("hover", "hover_shoot", "", "") {
		airMove = true;
	}

	public override void update() {
		base.update();

		character.xDir = startXDir;
		Point inputDir = player.input.getInputDir(player);

		if (inputDir.x == character.xDir) {
			if (!sprite.StartsWith("hover_forward")) {
				sprite = "hover_forward";
				shootSprite = sprite + "_shoot";
				character.changeSpriteFromName(sprite, true);
			}
		} else if (inputDir.x == -character.xDir) {
			if (player.input.isHeld(Control.Jump, player)) {
				if (!sprite.StartsWith("hover_backward")) {
					sprite = "hover_backward";
					shootSprite = sprite + "_shoot";
					character.changeSpriteFromName(sprite, true);
				}
			} else {
				character.xDir = -character.xDir;
				startXDir = character.xDir;
				if (!sprite.StartsWith("hover_forward")) {
					sprite = "hover_forward";
					shootSprite = sprite + "_shoot";
					character.changeSpriteFromName(sprite, true);
				}
			}
		} else {
			if (sprite != "hover") {
				sprite = "hover";
				shootSprite = sprite + "_shoot";
				character.changeSpriteFromName(sprite, true);
			}
		}

		if (character.vel.y < 0) {
			character.vel.y += Global.speedMul * character.getGravity();
			if (character.vel.y > 0) character.vel.y = 0;
		}

		if (character.gravityWellModifier > 1) {
			character.vel.y = 53;
		}

		hoverTime += Global.spf;
		if (hoverTime > 2 || character.player.input.isPressed(Control.Jump, character.player)) {
			character.changeState(new Fall(), true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel = new Point();
		startXDir = character.xDir;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}
