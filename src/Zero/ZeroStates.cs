using System;
using System.Diagnostics.CodeAnalysis;
using SFML.Graphics;

namespace MMXOnline;

public class ZeroState : CharState {
	public Zero zero = null!;

	public ZeroState(
		string sprite, string shootSprite = "", string attackSprite = "",
		string transitionSprite = "", string transShootSprite = ""
	) : base(
		sprite, shootSprite, attackSprite, transitionSprite, transShootSprite
	) {
	}

	public override void onEnter(CharState oldState) {
		zero = character as Zero ?? throw new NullReferenceException();
	}
}

public class HyperZeroStart : ZeroState {
	public float radius = 200;
	public float time;
	Anim? virusEffectParts;
	Anim?[] virusAnim = new Anim?[3];
	float[] delayedVirusTimer = { 0, 7, 14 };
	string virusAnimName = "";

	public HyperZeroStart() : base("hyper_start") {
		invincible = true;
	}

	public override void update() {
		base.update();
		if (virusAnimName != "") {
			int animCount = 0;
			for (int i = 0; i < virusAnim.Length; i++) {
				Anim? targetAnim = virusAnim[i];
				if (targetAnim != null) {
					if (targetAnim.pos == character.getCenterPos()) {
						targetAnim.destroySelf();
					}
					if (targetAnim.destroyed) {
						character.playSound("shingetsurinx5", true);
						if (stateFrames > 55) {
							virusAnim[i] = null;
							continue;
						}
						targetAnim = createVirusAnim();
						virusAnim[i] = targetAnim;
					} else {
						animCount++;
					}
					targetAnim.moveToPos(character.getCenterPos(), 300);
					if (targetAnim.pos.distanceTo(character.getCenterPos()) < 10) {
						targetAnim.destroySelf();
					}
				} else if (delayedVirusTimer[i] > 0) {
					delayedVirusTimer[i] -= Global.speedMul;
					if (delayedVirusTimer[i] <= 0) {
						delayedVirusTimer[i] = 0;
						virusAnim[i] = createVirusAnim();
					}
				}
				if (animCount == 0 && stateFrames > 55 && virusEffectParts != null) {
					virusEffectParts.destroySelf();
				}
			}
		}
		if (time == 0) {
			if (radius >= 0) {
				radius -= Global.spf * 200;
			} else {
				time = Global.spf;
				radius = 0;
				activateHypermode();
				character.playSound("ching");
				character.fillHealthToMax();
			}
		} else {
			time += Global.spf;
			if (time >= 1) {
				character.changeToIdleOrFall();
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel = new Point();
		character.player.currency -= 10;
		character.clenaseAllDebuffs();
		if (zero.hyperMode == 2) {
			zero.changeSpriteFromName("hyper_viral", true);
			virusAnimName = "sigmavirushead";
			virusAnim[0] = createVirusAnim();
		}
		if (zero.hyperMode == 1) {
			zero.changeSpriteFromName("hyper_awakened", true);
			virusAnimName = "zerovirus";
			virusAnim[0] = createVirusAnim();
			virusEffectParts = new Anim(character.pos.addxy(0, 4), "viruseffect", -character.xDir, null, false);
			virusEffectParts.blink = true;
		}
		if (zero.hyperMode == 0) {
			character.playSound("blackzeroentry", forcePlay: false, sendRpc: true);
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
		if (character != null) {
			character.invulnTime = 0.5f;
		}
		if (zero.isAwakened || zero.isBlack) {
			zero.hyperModeTimer = Zero.maxBlackZeroTime + 30;
		}
		virusEffectParts?.destroySelf();
		bool playedHitSound = false;
		for (int i = 0; i < virusAnim.Length; i++) {
			if (virusAnim[i]?.destroyed == false) {
				if (!playedHitSound) {
					character?.playSound("shingetsurinx5", true);
					playedHitSound = true;
				}
				virusAnim[i]?.destroySelf();
			}
		}
	}

	public Anim createVirusAnim() {
		float newAngle = Helpers.randomRange(0, 359);
		Point newPos = new(Helpers.cosd(newAngle) * 100, Helpers.sind(newAngle) * 100);
		float diplayAngle = newAngle;
		int xDir = -1;
		if (virusAnimName == "sigmavirushead") {
			xDir = 1;
		}
		if (diplayAngle > 90 && diplayAngle < 270) {
			diplayAngle = (diplayAngle + 180f) % 360f;
			xDir *= -1;
		}
		return new Anim(
			character.getCenterPos() + newPos,
			virusAnimName, xDir,
			player.getNextActorNetId(), false, sendRpc: true
		) {
			angle = diplayAngle
		};
	}

	public void activateHypermode() {
		if (zero.hyperMode == 1) {
			zero.awakenedPhase = 1;
			float storedAmmo = zero.gigaAttack.ammo;
			zero.gigaAttack = new ShinMessenkou();
			zero.gigaAttack.ammo = storedAmmo;
		} else if (zero.hyperMode == 2) {
			zero.isViral = true;
			float storedAmmo = zero.gigaAttack.ammo;
			zero.gigaAttack = new DarkHoldWeapon();
			zero.gigaAttack.ammo = storedAmmo;
			zero.freeBusterShots = 10;
		} else {
			zero.isBlack = true;
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		Point pos = character.getCenterPos();
		if (zero.hyperMode == 0) {
			DrawWrappers.DrawCircle(
				pos.x + x, pos.y + y, radius, false, Color.White, 5, character.zIndex + 1, true, Color.White
			);
		}
	}
}

public class SaberParryStartState : CharState {
	public SaberParryStartState() : base("parry_start") {
		superArmor = true;
	}

	public override void update() {
		base.update();

		if (stateTime < 0.1f) {
			character.turnToInput(player.input, player);
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public void counterAttack(Player damagingPlayer, Actor? damagingActor, float damage) {
		Actor? counterAttackTarget = null;
		bool stunnableParry = false;

		if (damagingActor is Projectile proj) {
			if (proj.owningActor != null) {
				counterAttackTarget = proj.owningActor;
			}
			stunnableParry = proj.canBeParried();
		}
		if (counterAttackTarget == null) {
			counterAttackTarget = damagingPlayer?.character ?? damagingActor;
		}

		if (counterAttackTarget != null && character.pos.distanceTo(counterAttackTarget.pos) < 75 &&
			counterAttackTarget is Character chr && stunnableParry
		) {
			if (!chr.ownedByLocalPlayer) {
				RPC.actorToggle.sendRpc(chr.netId, RPCActorToggleType.ChangeToParriedState);
			} else {
				chr.changeState(new ParriedState(), true);
			}
		}
		if (Helpers.randomRange(0, 10) < 10) {
			character.playSound("zeroParry", forcePlay: false, sendRpc: true);
		} else {
			character.playSound("zeroParry2", forcePlay: false, sendRpc: true);
		}
		character.changeState(new KKnuckleParryMeleeState(counterAttackTarget), true);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
	}

	public bool canParry(Actor? damagingActor) {
		if (damagingActor is not Projectile) {
			return false;
		}
		return character.frameIndex == 0;
	}
}

public class KKnuckleParryMeleeState : CharState {
	Actor? counterAttackTarget;
	Point counterAttackPos;
	public KKnuckleParryMeleeState(Actor? counterAttackTarget) : base("parry") {
		invincible = true;
		this.counterAttackTarget = counterAttackTarget;
	}

	public override void update() {
		base.update();

		if (counterAttackTarget != null) {
			character.turnToPos(counterAttackPos);
			float dist = character.pos.distanceTo(counterAttackPos);
			if (dist < 150) {
				if (character.frameIndex >= 1 && !once) {
					if (dist > 5) {
						var destPos = Point.lerp(character.pos, counterAttackPos, Global.spf * 5);
						character.changePos(destPos);
					}
				}
			}
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (counterAttackTarget != null) {
			counterAttackPos = counterAttackTarget.pos.addxy(character.xDir * 30, 0);
		}
	}
}

public class AwakenedTaunt : ZeroState {
	public AwakenedTaunt() : base("az_taunt") {
	}

	public override void update() {
		base.update();
		if (stateTime >= 150f / 60f && !Global.level.gameMode.playerWon(player)) {
			character.changeToIdleOrFall();
		}
		if (!once) {
			once = true;
			character.playSound("awakenedaura", forcePlay: true, sendRpc: true);
		}
	}

	public override void onEnter(CharState oldState) {
		zero = character as Zero ?? throw new NullReferenceException();
		base.onEnter(oldState);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		zero.tauntCooldown = 180;
	}
}

public class ZeroTaunt : CharState {
	public ZeroTaunt() : base("taunt") {
	}

	public override void update() {
		base.update();
		if (character.isAnimOver() && !Global.level.gameMode.playerWon(player)) {
			character.changeToIdleOrFall();
		}
		if (character.frameIndex == 6 && !once) {
			once = true;
			character.playSound("ching", sendRpc: true);
			new Anim(
				character.pos.addxy(character.xDir * -7, -28f),
				"zero_ching", -character.xDir,
				player.getNextActorNetId(),
				destroyOnEnd: true, sendRpc: true
			);
		}
	}
}

public class FallSaber : CharState {
	public float limboVehicleCheckTime;
	public Actor? limboVehicle;

	public FallSaber() : base("fall_saber", "fall_shoot", "attack_air", "fall_start_saber", "fall_start_shoot") {
		accuracy = 5;
		exitOnLanding = true;
		useDashJumpSpeed = true;
		airMove = true;
		canStopJump = false;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();
		if (limboVehicleCheckTime > 0) {
			limboVehicleCheckTime -= Global.spf;
			if (limboVehicle?.destroyed == true || limboVehicleCheckTime <= 0) {
				limboVehicleCheckTime = 0;
				character.useGravity = true;
				character.limboRACheckCooldown = 1;
				limboVehicleCheckTime = 0;
			}
		}
	}

	public void setLimboVehicleCheck(Actor limboVehicle) {
		if (limboVehicleCheckTime == 0 && character.limboRACheckCooldown == 0) {
			this.limboVehicle = limboVehicle;
			limboVehicleCheckTime = 1;
			character.stopMovingS();
			character.useGravity = false;
			if (limboVehicle is RideArmor ra) {
				RPC.checkRAEnter.sendRpc(player.id, ra.netId, ra.neutralId, ra.raNum);
			} else if (limboVehicle is RideChaser rc) {
				RPC.checkRCEnter.sendRpc(player.id, rc.netId, rc.neutralId);
			}
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}
