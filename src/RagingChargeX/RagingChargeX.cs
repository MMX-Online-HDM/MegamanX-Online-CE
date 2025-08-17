using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class RagingChargeX : Character {
	public int shotCount;
	public float punchCooldown;
	public float saberCooldown;
	public float parryCooldown;
	public float shootCooldown;
	public float maxParryCooldown = 30;
	public float selfDamageCooldown;
	public float selfDamageMaxCooldown = 120;
	public float secondSoundCooldown;
	public int lastAmmoSound = -1; 
	public Projectile? absorbedProj;
	public RagingChargeBuster ragingBuster;

	public RagingChargeX(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true, int? heartTanks = null, bool isATrans = false
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, heartTanks, isATrans
	) {
		charId = CharIds.RagingChargeX;

		// Start with 5s spawn leitency.
		selfDamageCooldown = selfDamageMaxCooldown * 4;

		// For easy HUD display we add it to weapon list.
		ragingBuster = new RagingChargeBuster();
		weapons.Add(ragingBuster);
		altSoundId = AltSoundIds.X1;
	}

	public override void preUpdate() {
		base.preUpdate();
		if (!ownedByLocalPlayer) { return; }
		// Cooldowns.
		Helpers.decrementFrames(ref saberCooldown);
		Helpers.decrementFrames(ref punchCooldown);
		Helpers.decrementFrames(ref parryCooldown);
		Helpers.decrementFrames(ref shootCooldown);
		// For the shooting animation.
		if (shootAnimTime > 0) {
			shootAnimTime -= speedMul;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				if (sprite.name == getSprite(charState.shootSpriteEx)) {
					changeSpriteFromName(charState.defaultSprite, false);
					if (charState is WallSlide) {
						frameIndex = sprite.totalFrameNum - 1;
					}
				}
			}
		}
	}

	public override void update() {
		base.update();
		if (musicSource == null) {
			addMusicSource("introStageBreisX4_JX", getCenterPos(), true);
		}
		// Local-only starts here.
		if (!ownedByLocalPlayer) { return; }
		// Allow cancel normals into parry.
		if (player.input.isWeaponLeftOrRightPressed(player) &&
			parryCooldown == 0 &&
			charState is XUPPunchState or XUPGrabState or X6SaberState
		) {
			enterParry();
		}
		ragingBuster.update();
		ragingBuster.charLinkedUpdate(this, true);
		chargeBusterSound();
		chargeLogic(null);
	}

	public override void postUpdate() {
		base.postUpdate();
		// Local-only starts here.
		if (!ownedByLocalPlayer) { return; }
		// Decay damage.
		if (!isDecayImmune() && invulnTime == 0) {
			if (selfDamageCooldown <= 0) {
				applyDamage(1, player, this, null, (int)ProjIds.SelfDmg);
				selfDamageCooldown = selfDamageMaxCooldown;
				secondSoundCooldown = 60;
				playSound("hit", true, true);
			} else {
				Helpers.decrementFrames(ref selfDamageCooldown);
			}
		} else if (isDecayImmune() && selfDamageCooldown < selfDamageMaxCooldown) {
			selfDamageCooldown = selfDamageMaxCooldown;
		}
	}

	public override bool normalCtrl() {
		if (!charState.isGrabbing &&
			player.input.isPressed(Control.Special1, player) &&
			charState is Dash or AirDash
		) {
			charState.isGrabbing = true;
			changeSpriteFromName("unpo_grab_dash", true);
		}
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool upPressed = player.input.isPressed(Control.Up, player);
		bool downPressed = player.input.isPressed(Control.Down, player);
		if (player.input.isWeaponLeftOrRightPressed(player) && parryCooldown == 0) {
			enterParry();
			return true;
		}
		if (shootPressed && ragingBuster.ammo >= ragingBuster.getAmmoUsage(0) && shootCooldown <= 0) {
			shoot();
			return true;
		}
		if (punchCooldown <= 0 && (shootPressed && ragingBuster.ammo < ragingBuster.getAmmoUsage(0) || downPressed)) {
			punchCooldown = 30;
			changeState(new XUPPunchState(grounded), true);
			return true;
		}
		if (upPressed && saberCooldown <= 0) {
			saberCooldown = 60;
			changeState(new X6SaberState(grounded), true);
			return true;
		}
		return base.attackCtrl();
	}

	// Shoot and set animation if posible.
	public void shoot() {
		string shootSprite = getSprite(charState.shootSpriteEx);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			shootSprite = grounded ? getSprite("shoot") : getSprite("fall_shoot");
		}
		if (shootAnimTime == 0) {
			changeSprite(shootSprite, false);
		} else if (charState is Idle && !charState.inTransition()) {
			frameIndex = 0;
			frameTime = 0;
		}
		shootAnimTime = 20;

		ragingBuster.shoot(this, []);
		ragingBuster.addAmmo(-ragingBuster.getAmmoUsage(0), player);
		playSound(ragingBuster.shootSounds[0]);
		shootCooldown = ragingBuster.fireRate;
	}

	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}

	public override void increaseCharge() {
		chargeTime += Global.speedMul;
		if (isCharging()) {
			ragingBuster.addAmmo(ragingBuster.getAmmoUsage(0) * 1.075f * Global.spf, player);
		}
	}
	public void chargeBusterSound() {
		int busterAmmo = (int)ragingBuster.ammo;
		if (isCharging()) {
			if (busterAmmo % 3 == 0 && busterAmmo != lastAmmoSound) {
				if (busterAmmo == 12) {
					playSound("gigaCrushAmmoFull");
				} else if (busterAmmo > 0 && busterAmmo < 12) {
					playSound("gigaCrushAmmoRecharge");
				}
				lastAmmoSound = busterAmmo;
			}
		}
	}

	public override float getDashSpeed() {
		if (sprite.name == "mmx_unpo_grab_dash") {
			return 1.25f * base.getDashSpeed();
		}
		return base.getDashSpeed();
	}

	public override string getSprite(string spriteName) {
		return "mmx_" + spriteName;
	}

	public void enterParry() {
		if (absorbedProj != null) {
			changeState(new XUPParryProjState(absorbedProj, true, false), true);
			absorbedProj = null;
			return;
		}
		changeState(new XUPParryStartState(), true);
		return;
	}
	public override int getMaxChargeLevel() {
		return 4;
	}
	public override void chargeGfx() {
		if (ownedByLocalPlayer) {
			chargeEffect.stop();
		}
		if (isCharging()) {
			chargeSound.play();
			int chargeType = 0;
			chargeEffect.update(getChargeLevel(), chargeType);
		}
	}
	public override void addAmmo(float amount) {
		weaponHealAmount += amount;
		ragingBuster.addAmmoHeal(amount);
	}

	public override void addPercentAmmo(float amount) {
		weaponHealAmount += amount * 0.32f;
		ragingBuster.addAmmoPercentHeal(amount);
	}

	public override bool canAddAmmo() {
		return ragingBuster.ammo < ragingBuster.maxAmmo;
	}

	public override bool isNonDamageStatusImmune() {
		return true;
	}

	public bool isDecayImmune() {
		return (
			charState is XUPGrabState
			or XUPParryMeleeState
			or XUPParryProjState
			or Hurt
			or GenericStun
			or VileMK2Grabbed
			or GenericGrabbedState
			or XRevive
			or Die
		);
	}
	
	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"mmx_beam_saber2" or "mmx_beam_saber_air2" => MeleeIds.ZSaber,
			"mmx_unpo_grab_dash" => MeleeIds.DashGrab,
			"mmx_unpo_punch" or "mmx_unpo_air_punch" => MeleeIds.Punch,
			"mmx_unpo_parry_start" => MeleeIds.ParryBlock,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		Projectile? proj = id switch {
			(int)MeleeIds.DashGrab => new GenericMeleeProj(
				RCXGrab.netWeapon, projPos, ProjIds.UPGrab, player,
				0, 0, 0, addToLevel: addToLevel
			),
			(int)MeleeIds.ParryBlock => new GenericMeleeProj(
				RCXParry.netWeapon, projPos, ProjIds.UPParryBlock, player,
				0, 0, 60, addToLevel: addToLevel
			),
			(int)MeleeIds.Punch => new GenericMeleeProj(
				RCXPunch.netWeapon, projPos, ProjIds.UPPunch, player,
				3, Global.defFlinch, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.ZSaber => new GenericMeleeProj(
				ZXSaber.netWeapon, projPos, ProjIds.X6Saber, player,
				3, 0, 30, isZSaberEffect: true, addToLevel: addToLevel
			),
			_ => null
		};
		return proj;
	}
	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> baseShaders = base.getShaders();
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;
		if (Global.isOnFrameCycle(4)) {
			switch (getChargeLevel()) {
				case 1:
					palette = Player.XBlueC;
					break;
				case 2:
					palette = Player.XYellowC;
					break;
				case 3:
					palette = Player.XPinkC;
					break;
				case 4:
					palette = Player.XGreenC;
					break;
			}
		}
		if (palette != null) {
			shaders.Add(palette);
		}
		if (shaders.Count == 0) {
			return baseShaders;
		}
		shaders.AddRange(baseShaders);
		return shaders;
	}
	public enum MeleeIds {
		None = -1,
		DashGrab,
		ParryBlock,
		Punch,
		ZSaber,
	}
}
