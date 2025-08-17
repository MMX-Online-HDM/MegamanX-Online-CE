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
	public bool doSelfDamage;
	public float selfDamageCooldown;
	public float selfDamageMaxCooldown = 120;
	public Projectile? absorbedProj;
	public RagingChargeBuster ragingBuster;
	public float[] chargeTimes = { 30, 105, 180, 255 };
	public const int ragingAmmo = 3;

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
		if (selfDamageCooldown <= 0) {
			selfDamageCooldown = selfDamageMaxCooldown;
		}
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
		if (!ownedByLocalPlayer) { return; }
		if (!isDecayImmune()) {
			Helpers.decrementFrames(ref selfDamageCooldown);
		}
		if (isDecayImmune() && selfDamageCooldown <= selfDamageMaxCooldown) {
			selfDamageCooldown = selfDamageMaxCooldown;
		}
		Helpers.decrementFrames(ref saberCooldown);
		Helpers.decrementFrames(ref punchCooldown);
		Helpers.decrementFrames(ref parryCooldown);
		Helpers.decrementFrames(ref shootCooldown);
		// Allow cancel normals into parry.
		if (player.input.isWeaponLeftOrRightPressed(player) &&
			parryCooldown == 0 &&
			charState is XUPPunchState or XUPGrabState or X6SaberState
		) {
			enterParry();
		}
		if (selfDamageCooldown == selfDamageMaxCooldown - 1) {
			applyDamage(1, player, this, null, (int)ProjIds.SelfDmg);
			playSound("hit", true, true);
		}
		ragingChargeLogic();
	}

	public override void postUpdate() {
		base.postUpdate();
	}

	public override bool normalCtrl() {
		if (player.input.isPressed(Control.Special1, player) && charState is Dash or AirDash) {
			charState.isGrabbing = true;
			changeSpriteFromName("unpo_grab_dash", true);
		}
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool specialPressed = player.input.isPressed(Control.Special1, player);
		if (shootPressed && currentWeapon?.ammo > 0 && shootCooldown <= 0) {
			shoot(0);
			return true;
		}
		if (player.input.isWeaponLeftOrRightPressed(player) && parryCooldown == 0) {
			enterParry();
			return true;
		}
		if (shootPressed && punchCooldown <= 0 && currentWeapon?.ammo <= 0) {
			punchCooldown = 30;
			changeState(new XUPPunchState(grounded), true);
			return true;
		}
		if (specialPressed && saberCooldown <= 0 && charState is not Dash) {
			saberCooldown = 60;
			changeState(new X6SaberState(grounded), true);
			return true;
		}
		return base.attackCtrl();
	}

	public void shoot(int chargeLevel) {
		string shootSprite = getSprite(charState.shootSpriteEx);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			if (grounded) { shootSprite = "shoot"; } else { shootSprite = "fall_shoot"; }
		}
		if (shootAnimTime == 0) {
			changeSprite(shootSprite, false);
		} else if (charState is Idle && !charState.inTransition()) {
			frameIndex = 0;
			frameTime = 0;
		}
		if (charState is LadderClimb) {
			if (player.input.isHeld(Control.Left, player)) {
				this.xDir = -1;
			} else if (player.input.isHeld(Control.Right, player)) {
				this.xDir = 1;
			}
		}
		Point shootPos = getShootPos();
		int xDir = getShootXDir();
		if (chargeLevel >= 0) {
			playSound("stockBuster", sendRpc: true);
			new RagingBusterProj(
				ragingBuster, shootPos, xDir, player,
				player.getNextActorNetId(), rpc: true
			);
			shootCooldown = 22;
			shootAnimTime = DefaultShootAnimTime;
			weaponAmmoadd(true);
		}
	}
	public void weaponAmmoadd(bool decrease) {
		currentWeapon?.addAmmo(decrease ? -ragingAmmo : ragingAmmo, player);
	}
	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}
	public override int getMaxChargeLevel() {
		return 4;
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
	public void ragingChargeLogic() {
		if (player.input.isHeld(Control.Shoot, player)) {
			increaseCharge();
			chargeGfx();
			if (chargeTimes.Contains(chargeTime)) {
				weaponAmmoadd(false);
				if (chargeTime < 255 && currentWeapon?.ammo < currentWeapon?.maxAmmo) {
					playSound("gigaCrushAmmoRecharge");
				} else {
					playSound("gigaCrushAmmoFull");
				}
			}
		} else {
			stopCharge();
		}
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
