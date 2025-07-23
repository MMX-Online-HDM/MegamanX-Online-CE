using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class BoomerangKuwanger : Maverick {
	public BoomerangKBoomerangWeapon boomerangWeapon = new();
	public BoomerangKDeadLiftWeapon deadLiftWeapon;
	public bool bald;
	public float dashSoundCooldown;
	public float teleportCooldown;
	public float aiAproachCooldown;

	public BoomerangKuwanger(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
		base(player, pos, destPos, xDir, netId, ownedByLocalPlayer) {
		stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.75f));
		//stateCooldowns.Add(typeof(BoomerKDeadLiftState), new MaverickStateCooldown(false, true, 0.75f));
		deadLiftWeapon = new BoomerangKDeadLiftWeapon(player);
		gravityModifier = 1.25f;

		weapon = new Weapon(WeaponIds.BoomerangKGeneric, 97);

		awardWeaponId = WeaponIds.BoomerangCutter;
		weakWeaponId = WeaponIds.HomingTorpedo;
		weakMaverickWeaponId = WeaponIds.LaunchOctopus;

		netActorCreateId = NetActorCreateId.BoomerangKuwanger;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		usesAmmo = true;
		canHealAmmo = true;
		ammo = 32;
		maxAmmo = 32;
		grayAmmoLevel = 8;
		barIndexes = (58, 47);
	}

	public override void preUpdate() {
		base.preUpdate();
		Helpers.decrementFrames(ref aiAproachCooldown);
	}

	public override void update() {
		base.update();
		Helpers.decrementTime(ref dashSoundCooldown);
		Helpers.decrementTime(ref teleportCooldown);

		if (state is not BoomerKTeleportState) {
			rechargeAmmo(4);
		}

		if (sprite.name == "boomerk_catch" && sprite.isAnimOver()) {
			changeSpriteFromName(state.sprite, true);
		}

		if (aiBehavior != MaverickAIBehavior.Control || !state.attackCtrl) {
			return;
		}
		if (grounded) {
			if (state is not BoomerKDashState) {
				if (input.isHeld(Control.Left, player)) {
					xDir = -1;
					changeState(new BoomerKDashState(Control.Left));
					return;
				} else if (input.isHeld(Control.Right, player)) {
					xDir = 1;
					changeState(new BoomerKDashState(Control.Right));
					return;
				}
			}
			if (bald) {
				return;
			}

			if (shootPressed()) {
				changeState(getShootState());
			}
			else if (specialPressed()) {
				changeState(new BoomerKDeadLiftState());
			}
			else if (player.dashPressed(out _) && teleportCooldown == 0) {
				if (ammo >= 8) {
					deductAmmo(8);
					changeState(new BoomerKTeleportState());
				}
			}
		} else if (state is BoomerKTeleportState teleportState && teleportState.onceTeleportInSound) {
			if (specialPressed() && !bald) {
				changeState(new BoomerKDeadLiftState());
			}
		}
	}

	public override float getRunSpeed() {
		return 175;
	}

	public override string getMaverickPrefix() {
		return "boomerk";
	}

	public MaverickState getShootState() {
		return new MShoot((Point pos, int xDir) => {
			bald = true;
			int inputAngle = 25;
			var inputDir = !isAI ? input.getInputDir(player) : Point.zero;
			if (inputDir.x != 0 && inputDir.y == 0) inputAngle = 0;
			else if (inputDir.x != 0 && inputDir.y != 0) inputAngle = 30 * MathF.Sign(inputDir.y);
			else if (inputDir.x == 0 && inputDir.y != 0) inputAngle = 60 * MathF.Sign(inputDir.y);
			new BoomerangKBoomerangProj(pos, xDir, this, (int)inputAngle, this, player, player.getNextActorNetId(), rpc: true);
		}, "boomerkBoomerang");
	}

	public override MaverickState[] strikerStates() {
		return [
			getShootState(),
			new BoomerKDeadLiftState()
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		bool canGrabTarget = (
			target is Character chara && chara.canBeGrabbed()
		);
		List<MaverickState> aiStates = [];
		if (!bald && enemyDist <= 25 && canGrabTarget && aiAproachCooldown <= 0) {
			aiStates.Add(new BoomerKDeadLiftState());
		}
		if (!bald && enemyDist >= 40) {
			aiStates.Add(getShootState());
		}
		if (!bald && (
			enemyDist >= 26 && canGrabTarget ||
			enemyDist <= 80 && !canGrabTarget ||
			enemyDist <= 40 ||
			Helpers.randomRange(0, 1) == 1
		)) {
			aiStates.Add(new BoomerKTeleportState());
		}
		return aiStates.ToArray();
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		DeadLift,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"boomerk_deadlift" => MeleeIds.DeadLift,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.DeadLift => new GenericMeleeProj(
				deadLiftWeapon, pos, ProjIds.BoomerangKDeadLift, player,
				0, 0, 0, addToLevel: addToLevel
			),
			_ => null
		};
	}

	public void setTeleportCooldown() {
		teleportCooldown = 0.15f;
	}
}

#region weapons
public class BoomerangKBoomerangWeapon : Weapon {
	public static BoomerangKBoomerangWeapon netWeapon = new();
	public BoomerangKBoomerangWeapon() {
		index = (int)WeaponIds.BoomerangKBoomerang;
		killFeedIndex = 97;
	}
}

public class BoomerangKDeadLiftWeapon : Weapon {
	public BoomerangKDeadLiftWeapon(Player player) {
		index = (int)WeaponIds.BoomerangKDeadLift;
		killFeedIndex = 97;
		damager = new Damager(player, 4, Global.defFlinch, 0.5f);
	}
}
#endregion

#region projectiles
public class BoomerangKBoomerangProj : Projectile {
	public float angleDist = 0;
	public float turnDir = 1;
	public Pickup? pickup;
	public float maxSpeed = 400;
	float returnTime = 0.15f;
	public BoomerangKuwanger BoomerangKuwanger;
	public BoomerangKBoomerangProj(
		Point pos, int xDir, BoomerangKuwanger BoomerangKuwanger,
		int throwDirAngle, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "boomerk_proj_horn", netId, player
	) {
		weapon = BoomerangKBoomerangWeapon.netWeapon;
		damager.damage = 3;
		damager.hitCooldown = 30;
		damager.flinch = Global.defFlinch;
		if (throwDirAngle >= 255) {
			throwDirAngle = 255;
		}
		projId = (int)ProjIds.BoomerangKBoomerang;
		angle = throwDirAngle;
		this.BoomerangKuwanger = BoomerangKuwanger;
		if (xDir == -1) angle = -180 - angle;

		destroyOnHit = false;
		shouldShieldBlock = false;
		shouldVortexSuck = false;

		if (rpc) {
			byte[] mavNetIdBytes = BitConverter.GetBytes(BoomerangKuwanger.netId ?? 0);
			byte[] mavthrowDirAngle = new byte[] { (byte)throwDirAngle };

			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, xDir,
			 new byte[] {mavthrowDirAngle[0], mavNetIdBytes[0], mavNetIdBytes[1]  }
			);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		ushort maverickId = BitConverter.ToUInt16(args.extraData, 0);
		BoomerangKuwanger? BoomerangKuwanger = Global.level.getActorByNetId(maverickId) as BoomerangKuwanger;
		return new BoomerangKBoomerangProj(
			args.pos, args.xDir, BoomerangKuwanger!, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;
		if (destroyed) return;

		if (other.gameObject is Pickup && pickup == null) {
			pickup = other.gameObject as Pickup;
			if (!pickup?.ownedByLocalPlayer == true) {
				pickup?.takeOwnership();
				RPC.clearOwnership.sendRpc(pickup?.netId);
			}
		}

		var bk = other.gameObject as BoomerangKuwanger;
		if (time > returnTime && bk != null && bk.player == damager.owner) {
			if (pickup != null) {
				pickup.changePos(bk.pos);
			}
			bk.bald = false;
			bk.changeSpriteFromName("catch", true);
			destroySelf();
		}
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (!destroyed && pickup != null) {
			if (pickup.collider != null) { pickup.collider.isTrigger = true; }
			pickup.useGravity = false;
			pickup.changePos(pos);
		}

		if (time > returnTime && angle != null) {
			if (angleDist < 180) {
				var angInc = (-xDir * turnDir) * Global.spf * maxSpeed;
				angle += angInc;
				angleDist += MathF.Abs(angInc);
				vel.x = Helpers.cosd((float)angle) * maxSpeed;
				vel.y = Helpers.sind((float)angle) * maxSpeed;
			} else if (BoomerangKuwanger != null && !BoomerangKuwanger.destroyed) {
				var dTo = pos.directionTo(BoomerangKuwanger.getCenterPos()).normalize();
				var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
				destAngle = Helpers.to360(destAngle);
				angle = Helpers.lerpAngle((float)angle, destAngle, Global.spf * 10);
			} else {
				destroySelf();
			}
		}
		if (angle != null) {
			vel.x = Helpers.cosd((float)angle) * maxSpeed;
			vel.y = Helpers.sind((float)angle) * maxSpeed;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (pickup != null) {
			pickup.useGravity = true;
			if (pickup.collider != null) { pickup.collider.isTrigger = false; }
		}
	}
}

#endregion

#region states

public class BoomerKTeleportState : MaverickState {
	public bool onceTeleportInSound;
	private bool isInvisible;
	private Actor? clone;
	private float aiPosOffset = Helpers.randomRange(0, 65);

	public BoomerKTeleportState() : base("teleport") {
		aiAttackCtrl = true;
	}

	public override void update() {
		base.update();

		if (!isInvisible && stateTime < 0.2f) {
			isInvisible = true;
			clone = new Actor("empty", maverick.pos, null, true, false);
			var rect = new Rect(0, 0, maverick.width, maverick.height);
			clone.spriteToCollider["teleport"] = new Collider(rect.getPoints(), false, clone, false, false, 0, new Point(0, 0));
			clone.changeSprite("boomerk_teleport", false);
			clone.alpha = 0.5f;
			clone.xDir = maverick.xDir;
			clone.visible = false;
			clone.useGravity = false;
			maverick.useGravity = false;
		}
		if (isInvisible && stateTime > 0.4f && clone != null) {
			isInvisible = false;
			if (canChangePos()) {
				Point? prevCamPos = null;
				if (player.character != null) {
					prevCamPos = player.character.getCamCenterPos();
					player.character.stopCamUpdate = true;
				}
				maverick.changePos(clone.pos);
				if (prevCamPos != null && maverick.controlMode == MaverickModeId.TagTeam) {
					Global.level.snapCamPos(player.character?.getCamCenterPos() ?? new Point(0,0), prevCamPos);
				}
			}
			clone?.destroySelf();
			clone = null;
		}

		if (isInvisible) {
			int dir = input.getXDir(player);
			if (maverick is BoomerangKuwanger kuwanger &&
				clone != null &&
				maverick.controlMode == (int)MaverickModeId.Summoner &&
				maverick.target != null
			) {
				float enemyDist = MathF.Abs(maverick.target.pos.x - clone.pos.x);

				if ((kuwanger.aiAproachCooldown <= 0 || enemyDist <= 40) && (
					maverick.target is Character chara &&
					chara.canBeGrabbed()
				)) {
					if (maverick.target.pos.x > clone.pos.x + 20) {
						dir = 1;
					} else if (maverick.target.pos.x < clone.pos.x - 20) {
						dir = -1;
					} else {
						dir = 0;
					}
				} else if (enemyDist <= 120 - aiPosOffset) {
					if (maverick.target.pos.x >= clone.pos.x) {
						dir = -1;
					} else {
						dir = 1;
					}
				} else if (enemyDist >= 125 - aiPosOffset) {
					if (maverick.target.pos.x >= clone.pos.x) {
						dir = 1;
					} else {
						dir = -1;
					}
				} else {
					dir = 0;
				}
			}

			float moveAmount = dir * 300 * Global.spf;
			if (clone != null) {
				var hitWall = Global.level.checkTerrainCollisionOnce(clone, moveAmount, -2);
				if (hitWall != null && hitWall.getNormalSafe().y == 0) {
					float rectW = hitWall.otherCollider.shape.getRect().w();
					if (rectW < 75) {
						float wallClipAmount = moveAmount + dir * (rectW + maverick.width);
						var hitWall2 = Global.level.checkTerrainCollisionOnce(clone, wallClipAmount, -2);
						if (hitWall2 == null && clone.pos.x + wallClipAmount > 0 && clone.pos.x + wallClipAmount < Global.level.width) {
							clone.incPos(new Point(wallClipAmount, 0));
							clone.visible = true;
						}
					}
				} else {
					if (MathF.Abs(moveAmount) > 0) clone.visible = true;
					clone.move(new Point(moveAmount, 0), useDeltaTime: false);
				}

				if (!canChangePos()) {
					var hits = Global.level.raycastAllSorted(clone.getCenterPos(), clone.getCenterPos().addxy(0, 200), new List<Type> { typeof(Wall) });
					var hit = hits.FirstOrDefault();
					if (hit != null) {
						clone.visible = true;
						clone.changePos(hit.getHitPointSafe());
					}
				}
				

				if (!canChangePos()) {
					var redXPos = clone.getCenterPos();
					DrawWrappers.DrawLine(redXPos.x - 10, redXPos.y - 10, redXPos.x + 10, redXPos.y + 10, Color.Red, 2, ZIndex.HUD);
					DrawWrappers.DrawLine(redXPos.x - 10, redXPos.y + 10, redXPos.x + 10, redXPos.y - 10, Color.Red, 2, ZIndex.HUD);
				}
			}
		}

		if (stateTime < 0.25f) {
			maverick.visible = Global.isOnFrameCycle(5);
		} else if (stateTime >= 0.2f && stateTime < 0.4f) {
			maverick.visible = false;
		} else if (stateTime > 0.6f) {
			if (!onceTeleportInSound) {
				onceTeleportInSound = true;
				maverick.playSound("boomerkTeleport", sendRpc: true);
			}
			maverick.visible = Global.isOnFrameCycle(5);
		}

		if (stateTime > 0.8f) {
			maverick.changeState(new MIdle());
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.playSound("boomerkTeleport", sendRpc: true);
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.visible = true;
		maverick.useGravity = true;
		if (maverick is BoomerangKuwanger bk) {
			bk.setTeleportCooldown();
		}
		if (clone != null) {
			clone.destroySelf();
		}
	}

	public bool canChangePos() {
		if (clone != null)
		if (Global.level.checkTerrainCollisionOnce(clone, 0, 5) == null) return false;
		if (clone != null) {
			var hits = Global.level.getTerrainTriggerList(clone, new Point(0, 5), typeof(KillZone));
			if (hits.Count > 0) return false;
		}
		return true;
	}
}

public class BoomerKDashState : MaverickState {
	public float dashTime = 0;
	public string initialDashButton;

	public BoomerKDashState(string initialDashButton) : base("dash") {
		this.initialDashButton = initialDashButton;
		normalCtrl = true;
		attackCtrl = true;
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		if (maverick is BoomerangKuwanger bk && bk.dashSoundCooldown == 0) {
			maverick.playSound("boomerkDash", sendRpc: true);
			bk.dashSoundCooldown = 0.25f;
		}
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
	}

	public override void update() {
		base.update();
		groundCode();

		dashTime += Global.spf;
		float modifier = 1;

		if (!input.isHeld(initialDashButton, player)) {
			maverick.changeState(new MIdle());
			return;
		}

		var move = new Point(0, 0);
		move.x = 300 * maverick.xDir * modifier;
		maverick.move(move);
	}
}

public class BoomerKDeadLiftState : MaverickState {
	private Character? grabbedChar;
	float timeWaiting;
	bool grabbedOnce;

	public BoomerKDeadLiftState() : base("deadlift") {
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
	}

	public override void update() {
		base.update();

		if (!grabbedOnce && grabbedChar != null && !grabbedChar.sprite.name.EndsWith("_grabbed") && maverick.frameIndex > 1 && timeWaiting < 0.5f) {
			maverick.frameSpeed = 0;
			timeWaiting += Global.spf;
		} else {
			maverick.frameSpeed = 1;
		}

		if (grabbedChar != null && grabbedChar.sprite.name.EndsWith("_grabbed")) {
			grabbedOnce = true;
			if (maverick is BoomerangKuwanger kuwanger) {
				kuwanger.aiAproachCooldown = 4 * 60;
			}
		}

		if (maverick.isAnimOver()) {
			maverick.changeState(new MIdle());
		}
	}

	public override bool trySetGrabVictim(Character grabbed) {
		if (grabbedChar == null) {
			grabbedChar = grabbed;
			return true;
		}
		return false;
	}
}

public class DeadLiftGrabbed : GenericGrabbedState {
	public Character? grabbedChar;
	public bool launched;
	float launchTime;
	public DeadLiftGrabbed(BoomerangKuwanger grabber) : base(grabber, 1, "") {
		customUpdate = true;
	}

	public override void update() {
		base.update();
		if (!character.ownedByLocalPlayer) { return; }

		if (launched) {
			launchTime += Global.spf;
			if (launchTime > 0.33f) {
				character.changeToIdleOrFall();
				return;
			}
			if (Global.level.checkTerrainCollisionOnce(character, 0, -1) != null) {
				new BoomerangKDeadLiftWeapon((grabber as Maverick)?.player ?? player).applyDamage(character, false, character, (int)ProjIds.BoomerangKDeadLift);
				character.playSound("crash", sendRpc: true);
				character.shakeCamera(sendRpc: true);
			}
			return;
		}

		if (grabber.sprite?.name.EndsWith("_deadlift") == true) {
			if (grabber.frameIndex < 4) {
				trySnapToGrabPoint(true);
			} else if (!launched) {
				launched = true;
				character.unstickFromGround();
				character.vel.y = -600;
			}
		} else {
			notGrabbedTime += Global.spf;
		}

		if (notGrabbedTime > 0.5f) {
			character.changeToIdleOrFall();
		}
	}
}
#endregion
