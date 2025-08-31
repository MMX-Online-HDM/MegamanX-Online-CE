using System;
using System.Collections.Generic;
using System.Text;
using SFML.Graphics;

namespace MMXOnline;

public class RCXParryStartState : CharState {
	public RCXParryStartState() : base("unpo_parry_start") {
		superArmor = true;
		stunImmune = true;
		pushImmune = true;
		invincible = true;
	}

	public override void update() {
		base.update();
		if (stateFrames == 0) {
			character.turnToInput(player.input, player);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

}
