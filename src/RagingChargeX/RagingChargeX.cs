using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class RagingChargeX : Character {
	public int shotCount;
	public float punchCooldown;
	public float saberCooldown;
	public float parryCooldown;
	public float maxParryCooldown = 30;
	public bool doSelfDamage;
	public float selfDamageCooldown;
	public float selfDamageMaxCooldown = 120;
	public Projectile? absorbedProj;
	public RagingChargeBuster ragingBuster;

	public RagingChargeX(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.RagingChargeX;

		// Start with 5s spawn leitency.
		selfDamageCooldown = selfDamageMaxCooldown * 4;

		// For easy HUD display we add it to weapon list.
		ragingBuster = new RagingChargeBuster();
		weapons.Add(ragingBuster);
		gameChar = GameChar.X1;
	}

	public override void preUpdate() {
		base.preUpdate();
		if (selfDamageCooldown > 0 && isDecayImmune()) {
			selfDamageCooldown -= speedMul;
			if (selfDamageCooldown <= 0) {
				selfDamageCooldown = 0;
				doSelfDamage = true;
			}
		}
	}

	public override void update() {
		base.update();
		if (musicSource == null) {
			addMusicSource("introStageBreisX4_JX", getCenterPos(), true);
		}
		if (!ownedByLocalPlayer) { return; }

		// Allow cancel normals into parry.
		if (player.input.isWeaponLeftOrRightPressed(player) &&
			parryCooldown == 0 &&
			charState is XUPPunchState or XUPGrabState or X6SaberState
		) {
			enterParry();
		}
		if (!isDecayImmune()) {
			if (doSelfDamage) {
				applyDamage(1, player, this, null, (int)ProjIds.SelfDmg);
			}
		} else {
			doSelfDamage = false;
		}
		shotCount = 0;
		shotCount = MathInt.Floor(ragingBuster.ammo / ragingBuster.getAmmoUsage(0));
	}

	public override void postUpdate() {
		base.preUpdate();
		if (!isDecayImmune() && selfDamageCooldown < selfDamageMaxCooldown) {
			selfDamageCooldown = selfDamageMaxCooldown;
		}
	}

	public override bool normalCtrl() {
		if (player.input.isPressed(Control.Special1, player) && charState is Dash or AirDash) {
			charState.isGrabbing = true;
			changeSpriteFromName("unpo_grab_dash", true);
		}
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		if (player.input.isWeaponLeftOrRightPressed(player) && parryCooldown == 0) {
			enterParry();
			return true;
		}
		if (player.input.isPressed(Control.Shoot, player) && shotCount <= 0) {
			punchCooldown = 0.5f;
			changeState(new XUPPunchState(grounded), true);
			return true;
		}
		if (player.input.isPressed(Control.Special1, player) && saberCooldown == 0) {
			saberCooldown = 60;
			changeState(new X6SaberState(grounded), true);
			return true;
		}
		return base.attackCtrl();
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
				3, Global.halfFlinch, 30, addToLevel: addToLevel
			),
			_ => null
		};
		return proj;
	}

	public enum MeleeIds {
		None = -1,
		DashGrab,
		ParryBlock,
		Punch,
		ZSaber,
	}
}
