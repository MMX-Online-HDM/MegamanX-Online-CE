namespace MMXOnline;

public class VileMK2Grab : Weapon {
	public VileMK2Grab() : base() {
		rateOfFire = 0.75f;
		index = (int)WeaponIds.VileMK2Grab;
		killFeedIndex = 63;
	}
}

public class VileMK2GrabState : CharState {
	public Character? victim;
	float leechTime = 1;
	public bool victimWasGrabbedSpriteOnce;
	float timeWaiting;
	public VileMK2GrabState(Character? victim) : base("grab", "", "", "") {
		this.victim = victim;
		grabTime = VileMK2Grabbed.maxGrabTime;
	}

	public override void update() {
		base.update();
		grabTime -= Global.spf;
		leechTime += Global.spf;

		if (victimWasGrabbedSpriteOnce && !victim.sprite.name.EndsWith("_grabbed")) {
			character.changeToIdleOrFall();
			return;
		}

		if (victim.sprite.name.EndsWith("_grabbed") || victim.sprite.name.EndsWith("_die")) {
			victimWasGrabbedSpriteOnce = true;
		}
		if (!victimWasGrabbedSpriteOnce) {
			timeWaiting += Global.spf;
			if (timeWaiting > 1) {
				victimWasGrabbedSpriteOnce = true;
			}
			if (character.isDefenderFavored()) {
				if (leechTime > 0.5f) {
					leechTime = 0;
					character.addHealth(1);
				}
				return;
			}
		}

		if (leechTime > 0.5f) {
			leechTime = 0;
			character.addHealth(1);
			var damager = new Damager(player, 1, 0, 0);
			damager.applyDamage(victim, false, new VileMK2Grab(), character, (int)ProjIds.VileMK2Grab);
		}

		if (stateFrames >= 2 && player.input.isPressed(Control.Special1, player)) {
			character.changeToIdleOrFall();
			return;
		}

		if (grabTime <= 0) {
			character.changeToIdleOrFall();
			return;
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.grabCooldown = 1;
		if (newState is not VileMK2GrabState && victim != null) {
			victim.grabInvulnTime = 2;
			victim.stunInvulnTime = 1;
			victim?.releaseGrab(character, true);
		}
	}
}

public class VileMK2Grabbed : GenericGrabbedState {
	public const float maxGrabTime = 4;
	public VileMK2Grabbed(Character? grabber) : base(grabber, maxGrabTime, "vilemk2_grab") {
	}
}
