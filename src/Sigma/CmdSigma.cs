using System;
using System.Collections.Generic;

namespace MMXOnline;

public class CmdSigma : BaseSigma {
	public float leapSlashCooldown;
	public float sigmaAmmoRechargeCooldown = 0;
	public float sigmaAmmoRechargeTime;
	public float sigmaHeadBeamRechargePeriod = 5;
	public float sigmaHeadBeamTimeBeforeRecharge = 20;

	public CmdSigma(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId,
		bool ownedByLocalPlayer, bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible,
		netId, ownedByLocalPlayer, isWarpIn
	) {
		sigmaSaberMaxCooldown = 1;
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) {
			return;
		}
		// Cooldowns.
		Helpers.decrementTime(ref leapSlashCooldown);
		Helpers.decrementFrames(ref sigmaAmmoRechargeCooldown);
		// Ammo reload.
		if (sigmaAmmoRechargeCooldown == 0) {
			Helpers.decrementFrames(ref sigmaAmmoRechargeTime);
			if (sigmaAmmoRechargeTime == 0) {
				player.sigmaAmmo = Helpers.clampMax(player.sigmaAmmo + 1, player.sigmaMaxAmmo);
				sigmaAmmoRechargeTime = sigmaHeadBeamRechargePeriod;
			}
		} else {
			sigmaAmmoRechargeTime = 0;
		}
		// For ladder and slide attacks.
		if (isAttacking() && charState is WallSlide or LadderClimb && !isSigmaShooting()) {
			if (isAnimOver() && charState != null && charState is not SigmaSlashState) {
				changeSprite(getSprite(charState.defaultSprite), true);
				if (charState is WallSlide && sprite != null) {
					frameIndex = sprite.totalFrameNum - 1;
				}
			} else if (grounded && sprite.name != "sigma_attack") {
				changeSprite("sigma_attack", false);
			}
		}
	}

	public override bool attackCtrl() {
		if (isAttacking()) {
			return false;
		}
		if (isInvulnerableAttack() || player.weapon is MaverickWeapon) {
			return false;
		}
		bool attackPressed = false;
		if (player.weapon is not AssassinBullet) {
			if (player.input.isPressed(Control.Shoot, player)) {
				attackPressed = true;
				lastAttackFrame = Global.level.frameCount;
			}
		}
		framesSinceLastAttack = Global.level.frameCount - lastAttackFrame;
		bool lenientAttackPressed = (attackPressed || framesSinceLastAttack < 5);

		if (lenientAttackPressed && saberCooldown == 0) {
			saberCooldown = sigmaSaberMaxCooldown;

			if (charState is WallSlide or LadderClimb) {
				if (charState is LadderClimb) {
					int inputXDir = player.input.getXDir(player);
					if (inputXDir != 0) {
						xDir = inputXDir;
					}
				}
				changeSprite(getSprite(charState.attackSprite), true);
				playSound("sigmaSaber", sendRpc: true);
				return true;
			}
			changeState(new SigmaSlashState(charState), true);
			return true;
		}
		if (charState is Dash dashState) {
			if (!dashState.stop && player.isSigma &&
				player.input.isPressed(Control.Special1, player) &&
				flag == null && leapSlashCooldown == 0
			) {
				changeState(new SigmaWallDashState(-1, true), true);
				return true;
			}
		}
		if (grounded && charState is Idle || charState is Run || charState is Crouch) {
			if (player.input.isHeld(Control.Special1, player) && player.sigmaAmmo > 0) {
				sigmaAmmoRechargeCooldown = 0.5f;
				changeState(new SigmaBallShoot(), true);
				return true;
			}
		}
		return base.attackCtrl();
	}

	public override Collider getBlockCollider() {
		Rect rect = Rect.createFromWH(0, 0, 16, 35);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override string getSprite(string spriteName) {
		return "sigma_" + spriteName;
	}

	// This can run on both owners and non-owners. So data used must be in sync
	public override Projectile? getProjFromHitbox(Collider collider, Point centerPoint) {
		Projectile? proj = sprite.name switch {
			"sigma_ladder_attack" => new GenericMeleeProj(
				SigmaSlashWeapon.netWeapon, centerPoint, ProjIds.SigmaSlash, player,
				3, 0, 0.25f
			),
			"sigma_wall_slide_attack" => new GenericMeleeProj(
				SigmaSlashWeapon.netWeapon, centerPoint, ProjIds.SigmaSlash, player,
				3, 0, 0.25f
			),
			_ => null
		};
		if (proj != null) {
			return proj;
		}
		return base.getProjFromHitbox(collider, centerPoint);
	}

	public override void addAmmo(float amount) {
		weaponHealAmount += amount;
	}

	public override void addPercentAmmo(float amount) {
		weaponHealAmount += amount * 0.32f;
	}

	public override bool canAddAmmo() {
		return (player.sigmaAmmo < player.sigmaMaxAmmo);
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();
		customData.Add((byte)MathF.Ceiling(player.sigmaAmmo));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-player data.
		player.sigmaAmmo = data[0];
	}
}
