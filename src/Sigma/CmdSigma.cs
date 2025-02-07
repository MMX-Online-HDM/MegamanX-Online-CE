using System;
using System.Collections.Generic;

namespace MMXOnline;

public class CmdSigma : BaseSigma {
	public float saberCooldown;
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
		gameChar = GameChar.X1;
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) {
			return;
		}
		// Cooldowns.
		Helpers.decrementTime(ref saberCooldown);
		Helpers.decrementTime(ref leapSlashCooldown);
		Helpers.decrementFrames(ref sigmaAmmoRechargeCooldown);
		Helpers.decrementFrames(ref aiAttackCooldown);
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
			if (isAnimOver() && charState != null && charState is not SigmaSlashStateGround 
			or SigmaSlashStateDash or SigmaSlashStateAir
			) {
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
			if (grounded) 
				changeState(new SigmaSlashStateGround(), true);
			else 
				changeState(new SigmaSlashStateAir(), true);
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
				changeState(new SigmaBallShootEX(), true);
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

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Guard,
		GenericSlash,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"sigma_block" => MeleeIds.Guard,
			"sigma_ladder_attack" or "sigma_wall_slide_attack" => MeleeIds.GenericSlash,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Guard => new GenericMeleeProj(
				SigmaSlashWeapon.netWeapon, pos, ProjIds.SigmaSwordBlock, player,
				0, 0, 0, isDeflectShield: true, addToLevel: addToLevel
			) {
				highPiority = true
			},
			MeleeIds.GenericSlash => new GenericMeleeProj(
				SigmaSlashWeapon.netWeapon, pos, ProjIds.SigmaSlash, player, 3, 0,
				addToLevel: addToLevel
			),
			_ => null
		};
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
	public float aiAttackCooldown;
	public override void aiAttack(Actor? target) {
		bool isTargetInAir = pos.y < target?.pos.y - 20;
		bool isTargetClose = pos.x < target?.pos.x - 10;
		if (currentWeapon is MaverickWeapon mw &&
			mw.maverick == null && canAffordMaverick(mw)
		) {
			buyMaverick(mw);
			if (mw.maverick != null) {
				changeState(new CallDownMaverick(mw.maverick, true, false), true);
			}
			mw.summon(player, pos.addxy(0, -112), pos, xDir);
			player.changeToSigmaSlot();
		}
		if (charState is not LadderClimb) {
			int Sattack = Helpers.randomRange(0, 5);
			if (charState?.isGrabbedState == false && !player.isDead
				&& !isInvulnerable() && !(charState is CallDownMaverick or SigmaSlashStateGround)
				&& aiAttackCooldown <= 0) {
				switch (Sattack) {
					case 0 when isTargetClose:
						changeState(new SigmaSlashStateGround(), true);
						break;
					case 1 when isTargetInAir:
						changeState(new SigmaBallShootEX(), true);
						break;
					case 2 when charState is Dash && grounded:
						changeState(new SigmaWallDashState(xDir, true), true);
						break;
					case 3:
						player.changeWeaponSlot(1);						
						break;
					case 4:
						player.changeWeaponSlot(2);
						break;
					case 5:
						player.changeWeaponSlot(0);
						break;
				}
				aiAttackCooldown = 18;
			}
		}
		base.aiAttack(target);
	}
	public override void aiDodge(Actor? target) {
		foreach (GameObject gameObject in getCloseActors(32, true, false, false)) {
			if (gameObject is Projectile proj && proj.damager.owner.alliance != player.alliance) {
				if (!(proj.projId == (int)ProjIds.SwordBlock)) {
						changeState(new SigmaBlock(), true);
				}
			}
		}
		base.aiDodge(target);
	}
	public override void aiUpdate() {
		base.aiUpdate();
		if (charState is Die) {
			foreach (Weapon weapon in weapons) {
				if (weapon is MaverickWeapon mw && mw.maverick != null) {
					mw.maverick.changeState(new MExit(mw.maverick.pos, true), true);
				}
			}	
		}
	}
}
