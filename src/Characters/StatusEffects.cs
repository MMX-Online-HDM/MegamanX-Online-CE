using System;
using System.Collections.Generic;

namespace MMXOnline;

public class Hurt : CharState {
	public float flinchYPos;
	public bool isCombo;
	public int hurtDir;
	public float hurtSpeed;
	public float flinchTime;
	public bool spiked;

	public Hurt(int dir, int flinchFrames, bool spiked = false, float? oldComboPos = null) : base("hurt") {
		this.flinchTime = flinchFrames;
		hurtDir = dir;
		hurtSpeed = dir * 1.6f;
		flinchTime = flinchFrames;
		this.spiked = spiked;
		if (oldComboPos != null) {
			isCombo = true;
			flinchYPos = oldComboPos.Value;
		}
	}

	public bool isMiniFlinch() {
		return flinchTime <= 6;
	}

	public override bool canEnter(Character character) {
		if (character.isCCImmune()) return false;
		if (character.vaccineTime > 0) return false;
		if (character.rideArmorPlatform != null) return false;
		return base.canEnter(character);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (player.isX && player.hasBodyArmor(1)) {
			flinchTime *= 0.75f;
			sprite = "hurt2";
			character.changeSpriteFromName("hurt2", true);
		}
		if (!spiked) {
			character.vel.y = (-0.125f * (flinchTime - 1)) * 60f;
			if (isCombo && character.pos.y < flinchYPos) {
				// Magic equation. Changing gravity from 0.25 probably super-break this.
				// That said, we do not change base gravity.
				character.vel.y *= (0.002f * flinchTime - 0.076f) * (flinchYPos - character.pos.y) + 1;
			}
		}
		if (!isCombo) {
			flinchYPos = character.pos.y;
		}
	}

	public override void update() {
		base.update();
		if (hurtSpeed != 0) {
			hurtSpeed = Helpers.toZero(hurtSpeed, 1.6f / flinchTime  * Global.speedMul, hurtDir);
			character.move(new Point(hurtSpeed * 60f, 0));
		}

		if (isMiniFlinch()) {
			character.frameSpeed = 0;
			if (Global.frameCount % 2 == 0) {
				if (player.charNum == 0) character.frameIndex = 3;
				if (player.charNum == 1) character.frameIndex = 3;
				if (player.charNum == 2) character.frameIndex = 0;
				if (player.charNum == 3) character.frameIndex = 3;
			} else {
				if (player.charNum == 0) character.frameIndex = 2;
				if (player.charNum == 1) character.frameIndex = 2;
				if (player.charNum == 2) character.frameIndex = 1;
				if (player.charNum == 3) character.frameIndex = 2;
			}
		}

		if (player.character is MegamanX or Zero &&
			player.character.canCharge() &&
			player.character.chargeButtonHeld()
		) {
			player.character.increaseCharge();
		}

		if (stateFrames >= flinchTime) {
			character.changeToLandingOrFall(false);
		}
	}
}

// Applies to freeze, stun, other effects.
public class GenericStun : CharState {
	public Anim? paralyzeAnim;
	public bool changeAnim = true;
	public bool canPlayFrozenSound = true;
	public bool canPlayStaticSound = true;
	public int hurtDir = 1;
	public float hurtSpeed;
	public float flinchYPos;

	public float flinchTime;
	public float flinchMaxTime;

	public GenericStun() : base("hurt") {

	}

	public override void update() {
		Helpers.decrementFrames(ref flinchTime);

		crystalizeLogic();
		paralizeAnimLogic();
		freezeLogic();

		if (hurtSpeed != 0) {
			hurtSpeed = Helpers.toZero(hurtSpeed, 1.6f / flinchMaxTime * Global.speedMul, hurtDir);
			character.move(new Point(hurtSpeed * 60f, 0));
		}

		if (changeAnim) {
			string stunAnim = getStunAnim();
			character.changeSpriteFromName(getStunAnim(), true);
			if (stunAnim == "idle") {
				character.sprite.frameSpeed = 0;
			}
		}

		if (character.frozenTime == 0 && character.crystalizedTime == 0 && character.paralyzedTime == 0) {
			if (flinchTime > 0) {
				character.changeState(
					new Hurt(hurtDir, MathInt.Ceiling(flinchTime), false, flinchYPos
					), true
				);
				return;
			}
			character.changeToIdleOrFall();
		}
	}
	
	public void freezeLogic() {
		if (character.frozenTime == 0) {
			return;
		}
		if (canPlayFrozenSound) {
			character.playSound("igFreeze", true);
			canPlayFrozenSound = false;
		}
		reduceStunFrames(ref character.frozenTime);
		character.freezeInvulnTime = 2;

		if (character.frozenTime == 0) {
			character.breakFreeze(player, sendRpc: true);
			canPlayFrozenSound = true;
			changeAnim = true;
		}
	}

	public void crystalizeLogic() {
		if (character.crystalizedTime == 0 && !character.isCrystalized) {
			return;
		}
		reduceStunFrames(ref character.crystalizedTime);
		character.crystalizeInvulnTime = 2;

		if (!character.isCrystalized && character.crystalizedTime > 0) {
			changeAnim = true;
			character.crystalizeStart();
			Global.serverClient?.rpc(RPC.playerToggle, (byte)character.player.id, (byte)RPCToggleType.StartCrystalize);
		}
		else if (character.isCrystalized && character.crystalizedTime == 0) {
			changeAnim = true;
			character.crystalizeEnd();
			Global.serverClient?.rpc(RPC.playerToggle, (byte)character.player.id, (byte)RPCToggleType.StopCrystalize);
		}
	}

	public void paralizeAnimLogic() {
		if (character.paralyzedTime == 0) {
			return;
		}
		if (canPlayStaticSound) {
			character.playSound("voltcStatic");
			canPlayStaticSound = false;
		}
		reduceStunFrames(ref character.paralyzedTime);
		character.stunInvulnTime = 2;

		if (paralyzeAnim == null && character.paralyzedTime > 0) {
			paralyzeAnim = new Anim(
				character.getCenterPos(), "vile_stun_static",
				1, character.player.getNextActorNetId(), false,
				host: character, sendRpc: true
			);
			paralyzeAnim.setzIndex(character.zIndex + 100);
		}
		if (character.paralyzedTime == 0) {
			changeAnim = true;
			canPlayStaticSound = true;
			if (paralyzeAnim != null) {
				paralyzeAnim.destroySelf();
				paralyzeAnim = null;
			}
		}
	}

	public string getStunAnim() {
		if (character.frozenTime > 0) {
			return "frozen";
		}
		if (character.isCrystalized) {
			return "idle";
		}
		if (character.paralyzedTime > 0 && character.grounded) {
			return "lose";
		}
		return "hurt";
	}

	public void activateFlinch(int flinchFrames, int xDir) {
		hurtDir = xDir;
		if (player.isX && player.hasBodyArmor(1)) {
			flinchFrames = MathInt.Floor(flinchFrames * 0.75f);
		}
		if (flinchTime > flinchFrames) {
			return;
		}
		bool isCombo = (flinchTime != 0);
		hurtSpeed = 1.6f * xDir;
		if (!isCombo) {
			flinchYPos = character.pos.y;
		}
		if (flinchFrames >= 2) {
			character.vel.y = (-0.125f * (flinchFrames - 1)) * 60f;
			if (isCombo && character.pos.y < flinchYPos) {
				character.vel.y *= (0.002f * flinchTime - 0.076f) * (flinchYPos - character.pos.y) + 1;
			}
		}
		flinchTime = flinchFrames;
		flinchMaxTime = flinchFrames;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMovingWeak();
		hurtDir = -character.xDir;
		// To continue the flinch if was flinched before the stun.
		if (oldState is Hurt hurtState) {
			hurtDir = hurtState.hurtDir;
			hurtSpeed = hurtState.hurtSpeed;
			flinchTime = hurtState.flinchTime - hurtState.stateFrames;
			flinchYPos = hurtState.flinchYPos;
			if (flinchTime < 0) {
				flinchTime = 0;
			}
		}
	}

	public override void onExit(CharState newState) {
		if (paralyzeAnim != null) {
			paralyzeAnim.destroySelf();
			paralyzeAnim = null;
		}
		if (character.crystalizedTime != 0 || character.isCrystalized) {
			character.crystalizeEnd();
			Global.serverClient?.rpc(RPC.playerToggle, (byte)character.player.id, (byte)RPCToggleType.StopCrystalize);
		}
		character.paralyzedTime = 0;
		character.frozenTime = 0;
		character.crystalizedTime = 0;

		base.onExit(newState);
	}

	public void reduceStunFrames(ref float arg) {
		arg -= player.mashValue() * 60f;
		if (arg <= 0) {
			arg = 0;
		}
	}

	public float getTimerFalloff() {
		float healthPercent = 1 * (player.health / player.maxHealth);
		return (Global.speedMul * (2 + healthPercent));
	}
}

public class KnockedDown : CharState {
	public int hurtDir;
	public float hurtSpeed;
	public float flinchTime;
	public KnockedDown(int dir) : base("knocked_down") {
		hurtDir = dir;
		hurtSpeed = dir * 100;
		flinchTime = 0.5f;
	}

	public override bool canEnter(Character character) {
		if (character.isCCImmune()) return false;
		if (character.charState.superArmor || character.charState.invincible) return false;
		if (character.isInvulnerable()) return false;
		if (character.vaccineTime > 0) return false;
		return base.canEnter(character);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.vel.y = -100;
	}

	public override void update() {
		base.update();
		if (hurtSpeed != 0) {
			hurtSpeed = Helpers.toZero(hurtSpeed, 400 * Global.spf, hurtDir);
			character.move(new Point(hurtSpeed, 0));
		}

		if (player.character.canCharge() && player.input.isHeld(Control.Shoot, player)) {
			player.character.increaseCharge();
		}

		if (stateTime >= flinchTime) {
			character.changeState(new Idle());
		}
	}
}

public class GoliathDragged : CharState {
	public RideArmor goliath;
	public GoliathDragged(RideArmor goliath) : base("hurt") {
		this.goliath = goliath;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel.y = 0;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
	}

	public override void update() {
		base.update();

		var goliathDash = goliath.rideArmorState as RADash;
		if (goliathDash == null || !goliath.isAttacking()) {
			if (character.grounded) character.changeState(new Idle(), true);
			else character.changeState(new Fall(), true);
			return;
		}

		character.move(goliathDash.getDashVel());
	}
}
