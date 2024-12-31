using System;
using System.Collections.Generic;

namespace MMXOnline;

public class NeoSigma : BaseSigma {
	public float normalAttackCooldown;
	public float sigmaUpSlashCooldown;
	public float sigmaDownSlashCooldown;

	public NeoSigma(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId,
		bool ownedByLocalPlayer, bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible,
		netId, ownedByLocalPlayer, isWarpIn
	) {
		sigmaSaberMaxCooldown = 0.5f;
	}

	public override void update() {
		// Call base update.
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		// Cooldowns.
		Helpers.decrementTime(ref normalAttackCooldown);
		Helpers.decrementTime(ref sigmaUpSlashCooldown);
		Helpers.decrementTime(ref sigmaDownSlashCooldown);
		Helpers.decrementFrames(ref aiAttackCooldown);
		// For ladder and slide attacks.
		if (isAttacking() && charState is WallSlide or LadderClimb) {
			if (isAnimOver() && charState != null && charState is not SigmaClawState) {
				changeSprite(getSprite(charState.defaultSprite), true);
				if (charState is WallSlide && sprite != null) {
					frameIndex = sprite.totalFrameNum - 1;
				}
			} else if (grounded && sprite?.name != "sigma2_attack" && sprite?.name != "sigma2_attack2") {
				changeSprite("sigma2_attack", false);
			}
		}
	}

	public override bool attackCtrl() {
		if (isInvulnerableAttack() || player.weapon is MaverickWeapon) {
			return false;
		}
		if (player.weapon is MaverickWeapon) {
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

		// Shoot button attacks.
		if (lenientAttackPressed && normalAttackCooldown == 0) {
			if (player.input.isHeld(Control.Up, player) && flag == null && grounded) {
				if (sigmaUpSlashCooldown == 0) {
					sigmaUpSlashCooldown = 0.75f;
					changeState(new SigmaUpDownSlashState(true), true);
				}
				return true;
			} else if (player.input.isHeld(Control.Down, player) && !grounded && getDistFromGround() > 25) {
				if (sigmaDownSlashCooldown == 0) {
					sigmaUpSlashCooldown += 0.5f;
					sigmaDownSlashCooldown = 1f;
					changeState(new SigmaUpDownSlashState(false), true);
				}
				return true;
			}
			normalAttackCooldown = sigmaSaberMaxCooldown;

			if (charState is WallSlide || charState is LadderClimb) {
				if (charState is LadderClimb) {
					int inputXDir = player.input.getXDir(player);
					if (inputXDir != 0) {
						xDir = inputXDir;
					}
				}
				changeSprite(getSprite(charState.attackSprite), true);
				playSound("sigma2slash", sendRpc: true);
				return true;
			}
			changeState(new SigmaClawState(charState, !grounded), true);
			return true;
		}
		if (grounded && player.input.isPressed(Control.Special1, player) &&
			flag == null && player.sigmaAmmo >= 14
		) {
			if (player.sigmaAmmo < 28) {
				player.sigmaAmmo -= 14;
				changeState(new SigmaElectricBallState(), true);
				return true;
			} else {
				player.sigmaAmmo = 0;
				changeState(new SigmaElectricBall2StateEX(), true);
				return true;
			}
		}
		return base.attackCtrl();
	}

	public override Collider getBlockCollider() {
		Rect rect = Rect.createFromWH(0, 0, 18, 50);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override string getSprite(string spriteName) {
		return "sigma2_" + spriteName;
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Guard,
		Slash1,
		Slash2,
		DashSlash,
		AirSlash,
		UpSlash,
		DownSlash,
		LadderSlash,
		WallSlash,
		GigaAttackSlash
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"sigma2_attack" => MeleeIds.Slash1,
			"sigma2_attack2" => MeleeIds.Slash2,
			"sigma2_attack_air" => MeleeIds.AirSlash,
			"sigma2_attack_dash" => MeleeIds.DashSlash,
			"sigma2_upslash" => MeleeIds.UpSlash,
			"sigma2_downslash" => MeleeIds.DownSlash,
			"sigma2_ladder_attack" => MeleeIds.LadderSlash,
			"sigma2_wall_slide_attack" => MeleeIds.WallSlash,
			"sigma2_shoot2" => MeleeIds.GigaAttackSlash,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Slash1 => new GenericMeleeProj(
				SigmaClawWeapon.netWeapon, pos, ProjIds.Sigma2Claw, player,
				2, 0, 12, addToLevel: addToLevel
			),
			MeleeIds.Slash2 => new GenericMeleeProj(
				SigmaClawWeapon.netWeapon, pos, ProjIds.Sigma2Claw2, player,
				2, Global.halfFlinch, 30, addToLevel: addToLevel
			),
			MeleeIds.AirSlash or MeleeIds.DashSlash => new GenericMeleeProj(
				SigmaClawWeapon.netWeapon, pos, ProjIds.Sigma2Claw, player,
				3, 0, 22, addToLevel: addToLevel
			),
			MeleeIds.UpSlash or MeleeIds.DownSlash => new GenericMeleeProj(
				SigmaClawWeapon.netWeapon, pos, ProjIds.Sigma2UpDownClaw, player,
				3, Global.defFlinch, 30, addToLevel: addToLevel
			),
			MeleeIds.WallSlash or MeleeIds.LadderSlash => new GenericMeleeProj(
				SigmaClawWeapon.netWeapon, pos, ProjIds.Sigma2Claw, player,
				3, 0, 15, addToLevel: addToLevel
			),
			MeleeIds.GigaAttackSlash => new GenericMeleeProj(
				new SigmaElectricBall2Weapon(), pos, ProjIds.Sigma2Ball2, player,
				6, Global.defFlinch, 15, addToLevel: addToLevel
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
		return (player.sigmaAmmo < 28);
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();
		customData.Add((byte)MathF.Floor(player.sigmaAmmo));

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
				int Neoattack = Helpers.randomRange(0, 5);
				if (charState?.isGrabbedState == false && !player.isDead
				    && !isInvulnerable() && aiAttackCooldown <= 0
					&& !(charState is CallDownMaverick or SigmaElectricBall2StateEX or SigmaElectricBallState)) {
					switch (Neoattack) {
						case 0 when isTargetClose:
							player.press(Control.Shoot);
							break;
						case 1 when sigmaDownSlashCooldown <= 0 && grounded && isTargetInAir:
							changeState(new SigmaUpDownSlashState(true), true);
							sigmaDownSlashCooldown = 1f;						
							break;
						case 2 when sigmaUpSlashCooldown <= 0 && !grounded:
							changeState(new SigmaUpDownSlashState(false), true);
							sigmaUpSlashCooldown = 0.75f;
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
					aiAttackCooldown = 14;
				}
			}
		base.aiAttack(target);
	}
	public override void aiDodge(Actor? target) {
		foreach (GameObject gameObject in getCloseActors(32, true, false, false)) {
			if (gameObject is Projectile proj && proj.damager.owner.alliance != player.alliance) {
				if (player.sigmaAmmo >= 16 && player.sigmaAmmo <= 24) {
					player.sigmaAmmo -= 16;
					changeState(new SigmaElectricBallState(), true);
				} else if (player.sigmaAmmo >= 28) {
					player.sigmaAmmo = 0;
					changeState(new SigmaElectricBall2StateEX(), true);
				}
			}
		}
		base.aiDodge(target);
	}
}
