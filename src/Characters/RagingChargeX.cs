using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class RagingChargeX : Character {

	public int unpoShotCount;
	public float upPunchCooldown;
	public float xSaberCooldown;
	public float parryCooldown;
	public float maxParryCooldown = 30;
	float UPDamageCooldown;
	public float unpoDamageMaxCooldown = 2;
	public Projectile? unpoAbsorbedProj;
	public RagingChargeX(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.RagingChargeX;
	}

	public override void update() {
		base.update();
	
		if (musicSource == null) {
			addMusicSource("introStageBreisX4_JX", getCenterPos(), true);
		}
		
		if (!ownedByLocalPlayer) return;

		if (!isInvulnerableAttack()) {
			if (charState.attackCtrl && player.input.isPressed(Control.Shoot, player)) {
				if (unpoShotCount <= 0) {
					upPunchCooldown = 0.5f;
					changeState(new XUPPunchState(grounded), true);
					return;
				}
			} else if (player.input.isPressed(Control.Special1, player) &&
				  (charState is Dash || charState is AirDash)) {
				charState.isGrabbing = true;
				changeSpriteFromName("unpo_grab_dash", true);
			} else if
			  (
				  player.input.isWeaponLeftOrRightPressed(player) && parryCooldown == 0 &&
				  (charState is Idle || charState is Run || charState is Fall || charState is Jump || charState is XUPPunchState || charState is XUPGrabState)
			  ) {
				if (unpoAbsorbedProj != null) {
					changeState(new XUPParryProjState(unpoAbsorbedProj, true, false), true);
					unpoAbsorbedProj = null;
					return;
				} else {
					changeState(new XUPParryStartState(), true);
				}
			}
		}

		if (charState.attackCtrl && canShoot() && canChangeWeapons() && 
			player.input.isPressed(Control.Special1, player) &&
			charState.normalCtrl && !charState.isGrabbing
		) {
			if (xSaberCooldown == 0) {
				xSaberCooldown = 60;
				changeState(new X6SaberState(grounded), true);
				return;
			}
		}

		if (charState is not XUPGrabState
			and not XUPParryMeleeState
			and not XUPParryProjState
			and not Hurt
			and not GenericStun
			and not VileMK2Grabbed
			and not GenericGrabbedState
		) {
			UPDamageCooldown += Global.spf;
			if (UPDamageCooldown > unpoDamageMaxCooldown) {
				UPDamageCooldown = 0;
				applyDamage(1, player, this, null, null);
			}
		}
		unpoShotCount = 0;
		if (player.weapon != null) {
			unpoShotCount = MathInt.Floor(player.weapon.ammo / player.weapon.getAmmoUsage(0));
		}
	}

	public override bool isCCImmuneHyperMode() {
		return true;
	}
}
