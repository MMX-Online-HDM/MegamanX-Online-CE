using System;
using System.Collections.Generic;

namespace MMXOnline;

public class BoomerangCutter : Weapon {
	public static BoomerangCutter netWeapon = new BoomerangCutter();

	public BoomerangCutter() : base() {
		displayName = "Boomerang Cutter";
		index = (int)WeaponIds.BoomerangCutter;
		killFeedIndex = 7;
		weaponBarBaseIndex = 7;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 7;
		weaknessIndex = (int)WeaponIds.HomingTorpedo;
		shootSounds = new string[] { "boomerang", "boomerang", "boomerang", "buster3" };
		fireRate = 30;
		damage = "2/2";
		effect = "Charged: Doesn't destroy on hit.\nCharged won't give assists.";
		hitcooldown = "0/0.5";
		flinch = "0/26";
	}
	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			if (character.ownedByLocalPlayer) {
				new BoomerangProj(
					pos, xDir, mmx, player, player.getNextActorNetId(), character.grounded ? 1 : -1, rpc: true
				);
			}
		} else {
			player.setNextActorNetId(player.getNextActorNetId());

			var twin1 = new BoomerangProjCharged(pos.addxy(0, 5), null, xDir, mmx, player, 90, 1, player.getNextActorNetId(true), null, true);
			var twin2 = new BoomerangProjCharged(pos.addxy(5, 0), null, xDir, mmx, player, 0, 1, player.getNextActorNetId(true), null, true);
			var twin3 = new BoomerangProjCharged(pos.addxy(0, -5), null, xDir, mmx, player, -90, 1, player.getNextActorNetId(true), null, true);
			var twin4 = new BoomerangProjCharged(pos.addxy(-5, 0), null, xDir, mmx, player, -180, 1, player.getNextActorNetId(true), null, true);
			
			var a = new BoomerangProjCharged(pos.addxy(0, 5), pos.addxy(0, 35), xDir, mmx, player, 90, 0, player.getNextActorNetId(true), twin1, true);
			var b = new BoomerangProjCharged(pos.addxy(5, 0), pos.addxy(35, 0), xDir, mmx, player, 0, 0, player.getNextActorNetId(true), twin2, true);
			var c = new BoomerangProjCharged(pos.addxy(0, -5), pos.addxy(0, -35), xDir, mmx, player, -90, 0, player.getNextActorNetId(true), twin3, true);
			var d = new BoomerangProjCharged(pos.addxy(-5, 0), pos.addxy(-35, 0), xDir, mmx, player, -180, 0, player.getNextActorNetId(true), twin4, true);

			twin1.twin = a;
			twin2.twin = b;
			twin3.twin = c;
			twin4.twin = d;
		}
	}
}

public class BoomerangProj : Projectile {
	public float angleDist = 0;
	public float turnDir = 1;
	public Pickup? pickup;
	public float maxSpeed = 250;
	public BoomerangProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, int turnDir, bool rpc = false
	) : base(
		pos, xDir, owner, "boomerang", netId, player	
	) {
		weapon = BoomerangCutter.netWeapon;
		damager.damage = 2;
		vel = new Point(250 * xDir, 0);
		projId = (int)ProjIds.Boomerang;
		customAngleRendering = true;
		angle = 0;
		if (xDir == -1) angle = -180;
		this.turnDir = turnDir;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] { (byte)(turnDir + 1) });
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;
		if (destroyed) return;

		if (other.gameObject is Pickup && pickup == null) {
			pickup = other.gameObject as Pickup;
			if (pickup != null) {
				if (!pickup.ownedByLocalPlayer) {
					pickup.takeOwnership();
					RPC.clearOwnership.sendRpc(pickup.netId);
				}
			}
			
		}

		var character = other.gameObject as Character;
		if (time > 0.22 && character != null && character.player == damager.owner) {
			if (pickup != null) {
				pickup.changePos(character.getCenterPos());
			}
			destroySelf();
			if (character.currentWeapon is BoomerangCutter bcWeapon) {
				if (character is MegamanX mmx && mmx.hyperArmArmor == ArmorId.Max) {
					bcWeapon.ammo += 0.5f;
				} else {
					bcWeapon.ammo++;
				}
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (pickup != null) {
			pickup.useGravity = true;
			if (pickup.collider != null) {
				pickup.collider.isTrigger = false;
			}
		}
	}

	public override void renderFromAngle(float x, float y) {
		sprite.draw(frameIndex, pos.x + x, pos.y + y, 1, 1, getRenderEffectSet(), 1, 1, 1, zIndex, actor: this);
	}

	public override void update() {
		base.update();

		if (!destroyed && pickup != null) {
			if (pickup.collider != null) {
				pickup.collider.isTrigger = true;
			}
			pickup.useGravity = false;
			pickup.changePos(pos);
		}

		if (time > 0.22) {
			if (angleDist < 180) {
				var angInc = (-xDir * turnDir) * Global.spf * 300;
				angle += angInc;
				angleDist += MathF.Abs(angInc);
				vel.x = Helpers.cosd((float)angle!) * maxSpeed;
				vel.y = Helpers.sind((float)angle) * maxSpeed;
			} else if (damager.owner.character != null) {
				var dTo = pos.directionTo(damager.owner.character.getCenterPos()).normalize();
				var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
				destAngle = Helpers.to360(destAngle);
				angle = Helpers.lerpAngle((float)angle!, destAngle, Global.spf * 10);
				vel.x = Helpers.cosd((float)angle) * maxSpeed;
				vel.y = Helpers.sind((float)angle) * maxSpeed;
			} else {
				destroySelf();
			}
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new BoomerangProj(
			args.pos, args.xDir, args.owner, args.player, args.netId, args.extraData[0] - 1
		);
	}
}

public class BoomerangProjCharged : Projectile {
	public float angleDist = 0;
	public float turnDir = 1;
	public Pickup? pickup;
	public float maxSpeed = 400;
	public int type = 0;
	public Point blurPosOffset;
	public BoomerangProjCharged? twin;

	public Point lerpOffset;
	public float lerpTime;

	public BoomerangProjCharged(
		Point pos, Point? lerpToPos, int xDir, Actor owner, Player player,
		float angle, int type, ushort? netProjId, BoomerangProjCharged? twin, bool rpc = false
	) : base(
		pos, xDir, owner, type == 0 ? "boomerang_charge" : "boomerang_charge2", netProjId, player 
	) {
		weapon = BoomerangCutter.netWeapon;
		damager.damage = 2;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
		vel = new Point(240 * xDir, 0);
		projId = (int)ProjIds.BoomerangCharged;
		maxTime = 1.2f;
		customAngleRendering = true;
		this.angle = angle;
		this.type = type;
		shouldShieldBlock = false;
		this.twin = twin;
		destroyOnHit = false;
		canBeLocal = false;

		if (lerpToPos != null) {
			lerpOffset = lerpToPos.Value.subtract(pos);
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new BoomerangProjCharged(
			args.pos, null, args.xDir, args.owner, args.player,
			args.angle, args.extraData[0], args.netId, null
		);
	}

	public override void renderFromAngle(float x, float y) {
		sprite.draw(frameIndex, pos.x + x, pos.y + y, 1, 1, getRenderEffectSet(), 1, 1, 1, zIndex);
	}

	public override void update() {
		base.update();
		if (lerpTime < 1) {
			incPos(lerpOffset.times(Global.spf * 15));
			lerpTime += Global.spf * 15;
		}
		vel.x = Helpers.cosd((float)angle!) * maxSpeed;
		vel.y = Helpers.sind((float)angle) * maxSpeed;
		if (type == 0) {
			if (time > 0.1 && time < 0.72) {
				angle += Global.spf * 500;
			}
		} else {
			if (time > 0.15 && time < 0.77) {
				angle += Global.spf * 500;
			}
		}
	}

	public override void onDestroy() {
		if (twin != null) twin.destroySelfNoEffect();
	}
}
