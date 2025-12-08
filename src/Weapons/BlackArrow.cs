using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;
using SFML.Window;

namespace MMXOnline;

public class BlackArrow : AxlWeapon {
	public static BlackArrow netWeapon;
	public BlackArrow(int altFire) : base(altFire) {
		shootSounds = new string[] { "blackArrow", "blackArrow", "blackArrow", "blackArrow" };
		fireRate = 24;
		altFireCooldown = 48;
		index = (int)WeaponIds.BlackArrow;
		weaponBarBaseIndex = 33;
		weaponSlotIndex = 53;
		killFeedIndex = 68;
		rechargeAmmoCooldown = 120;
		altRechargeAmmoCooldown = 150;
		sprite = "axl_arm_blackarrow";
		if (altFire == 1) {
			altRechargeAmmoCooldown = 240;
		}
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel == 3) {
			if (altFire == 1) {
				return 6;
			}
			return 4;
		}
		return 2;
	}

	public override float whiteAxlFireRateMod() {
		return 2f;
	}
	public override void axlShoot(Character character, int[] args) {
		base.axlShoot(character, args);
	}
	public override void axlAltShoot(Character character, int[] args) {
		if (shootCooldown > 0) return;
		shootCooldown = fireRate;
		base.axlAltShoot(character, args);
	}
	public override void axlGetAltProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		if (!player.ownedByLocalPlayer) return;
		Point bulletDir = Point.createFromAngle(angle);
		if (altFire == 0) {
			new WindCutterProj(weapon, bulletPos, player, bulletDir, netId, rpc: true);
		} else {
			new BlackArrowProj2(weapon, bulletPos, player, bulletDir, netId, rpc: true);
			new BlackArrowProj2(weapon, bulletPos, player, Point.createFromAngle(angle - 30), player.getNextActorNetId(), rpc: true);
			new BlackArrowProj2(weapon, bulletPos, player, Point.createFromAngle(angle + 30), player.getNextActorNetId(), rpc: true);
		}
		if (player.character != null) RPC.playSound.sendRpc(shootSounds[0], player.character.netId);
		
	}
	public override void axlGetProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		if (!player.ownedByLocalPlayer) return;
		Point bulletDir = Point.createFromAngle(angle);	
		new BlackArrowProj(weapon, bulletPos, player, bulletDir, netId, rpc: true);
		if (player.character != null) RPC.playSound.sendRpc(shootSounds[0], player.character.netId);
	}
}

public class BlackArrowProj : Projectile {
	public bool landed;
	public Actor? target;
	public List<Point> lastPoses = new List<Point>();

	public BlackArrowProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool rpc = false) :
		base(weapon, pos, 1, 450, 1, player, "blackarrow_proj", 0, 0f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 0.5f;
		vel.x = bulletDir.x * speed;
		vel.y = bulletDir.y * speed;
		projId = (int)ProjIds.BlackArrow;
		useGravity = true;
		updateAngle();
		if (rpc) {
			rpcCreateAngle(pos, player, netProjId, bulletDir.angle);
		}
		canBeLocal = false;
	}
	public void updateAngle() {
		angle = vel.angle;
	}
	public override void update() {
		base.update();
		lastPoses.Add(pos);
		if (lastPoses.Count > 5) lastPoses.RemoveAt(0);

		if (ownedByLocalPlayer) {
				target = Global.level.getClosestTarget(pos, damager.owner.alliance, true);
				if (!Global.level.gameObjects.Contains(target)) {
					target = null;
				}
				if (target != null) {
					useGravity = false;
					var dTo = pos.directionTo(target.getCenterPos()).normalize();
					var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
					destAngle = Helpers.to360(destAngle);
					float distFactor = pos.distanceTo(target.getCenterPos()) / 100;
					angle = Helpers.moveAngle(angle, destAngle, Global.spf * 200 * distFactor);
					vel.x = Helpers.cosd(angle) * speed;
					vel.y = Helpers.sind(angle) * speed;
				} else {
					useGravity = true;
					updateAngle();
				} 
			if (getHeadshotVictim(owner, out IDamagable? victim, out Point? hitPoint)) {
				damager.applyDamage(victim, false, weapon, this, projId, overrideDamage: damager.damage * Damager.headshotModifier);
				damager.damage = 0;
				playSound("hurt");
				destroySelf();
				return;
			}
		}
	}
	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!ownedByLocalPlayer) return;
		var hitNormal = other.getNormalSafe();
		destroySelf();
		new BlackArrowGrounded(
			weapon, other.getHitPointSafe(), owner, hitNormal.byteAngle,
			owner.getNextActorNetId(), true
		);
	}
	public override void render(float x, float y) {
		base.render(x, y);
		if (Options.main.lowQualityParticles()) return;

		for (int i = lastPoses.Count - 1; i >= 1; i--) {
			Point head = lastPoses[i];
			Point outerTail = lastPoses[i - 1];
			Point innerTail = lastPoses[i - 1];
			if (i == 1) {
				innerTail = innerTail.add(head.directionToNorm(innerTail).times(5));
			}

			DrawWrappers.DrawLine(head.x, head.y, outerTail.x, outerTail.y, new Color(80, 59, 145, 64), 4, 0, true);
			DrawWrappers.DrawLine(head.x, head.y, innerTail.x, innerTail.y, new Color(24, 24, 32, 128), 2, 0, true);
		}
	}
}
public class BlackArrowProj2 : Projectile {
	public bool landed;
	public Actor? target;
	public List<Point> lastPoses = new List<Point>();

	public BlackArrowProj2(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool rpc = false) :
		base(weapon, pos, 1, 450, 1, player, "blackarrow_proj", 0, 0f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 0.5f;
		vel.x = bulletDir.x * speed;
		vel.y = bulletDir.y * speed;
		projId = (int)ProjIds.BlackArrow2;
		useGravity = true;
		updateAngle();
		if (rpc) {
			rpcCreateAngle(pos, player, netProjId, bulletDir.angle);
		}
	}
	public void updateAngle() {
		angle = vel.angle;
	}
	public override void update() {
		base.update();
		lastPoses.Add(pos);
		if (lastPoses.Count > 5) lastPoses.RemoveAt(0);
		if (ownedByLocalPlayer) {
			updateAngle();
			if (getHeadshotVictim(owner, out IDamagable? victim, out Point? hitPoint)) {
				damager.applyDamage(victim, false, weapon, this, projId, overrideDamage: damager.damage * Damager.headshotModifier);
				damager.damage = 0;
				playSound("hurt");
				destroySelf();
				return;
			}
		}
	}
	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!ownedByLocalPlayer) return;
		var hitNormal = other.getNormalSafe();
		destroySelf();
		new BlackArrowGrounded(
			weapon, other.getHitPointSafe(), owner, hitNormal.byteAngle,
			owner.getNextActorNetId(), true
		);
	}
	public override void render(float x, float y) {
		base.render(x, y);
		if (Options.main.lowQualityParticles()) return;

		for (int i = lastPoses.Count - 1; i >= 1; i--) {
			Point head = lastPoses[i];
			Point outerTail = lastPoses[i - 1];
			Point innerTail = lastPoses[i - 1];
			if (i == 1) {
				innerTail = innerTail.add(head.directionToNorm(innerTail).times(5));
			}

			DrawWrappers.DrawLine(head.x, head.y, outerTail.x, outerTail.y, new Color(80, 59, 145, 64), 4, 0, true);
			DrawWrappers.DrawLine(head.x, head.y, innerTail.x, innerTail.y, new Color(24, 24, 32, 128), 2, 0, true);
		}
	}
}

public class WindCutterProj : Projectile {
	Actor? target;
	public float angleDist = 0;
	public float turnDir = 1;
	public bool targetHit;
	public WindCutterProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool rpc = false) :
		base(weapon, pos, 1, 450, 2, player, "windcutter_proj", 0, 0.5f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 1f;
		vel.x = bulletDir.x * speed;
		vel.y = bulletDir.y * speed;
		projId = (int)ProjIds.WindCutter;
		updateAngle();
		destroyOnHit = true;

		int startXDir = MathF.Sign(bulletDir.x);
		if (bulletDir.y > 0.2f) turnDir = startXDir;
		else turnDir = -startXDir;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public void updateAngle() {
		angle = vel.angle;
	}

	public override void update() {
		if (!ownedByLocalPlayer) {
			base.update();
			return;
		}

		if (!targetHit) {
			target = Global.level.getClosestTarget(pos, damager.owner.alliance, true);
			if (!Global.level.gameObjects.Contains(target)) {
				target = null;
			}
		} else {
			target = null;
		}

		if (target != null) {
			var dTo = pos.directionTo(target.getCenterPos()).normalize();
			var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
			destAngle = Helpers.to360(destAngle);
			float distFactor = pos.distanceTo(target.getCenterPos()) / 100;
			if (MathF.Abs(angle - destAngle) > 5) {
				angle = Helpers.moveAngle(angle, destAngle, Global.spf * 400 * distFactor);
			}
			vel.x = Helpers.cosd(angle) * speed;
			vel.y = Helpers.sind(angle) * speed;
		} else {
			returnToSelf();
			updateAngle();
		}

		base.update();
	}

	public void returnToSelf() {
		if (time > 0.1f) {
			if (angleDist < 180) {
				var angInc = turnDir * Global.spf * 500;
				angle += angInc;
				angleDist += MathF.Abs(angInc);
				vel.x = Helpers.cosd(angle) * speed;
				vel.y = Helpers.sind(angle) * speed;
			} else if (owner.character != null) {
				Point destPos = owner.character.getCenterPos();
				var dTo = pos.directionTo(destPos).normalize();
				var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
				destAngle = Helpers.to360(destAngle);
				angle = Helpers.lerpAngle(angle, destAngle, Global.spf * 10);
				vel.x = Helpers.cosd(angle) * speed;
				vel.y = Helpers.sind(angle) * speed;
				if (pos.distanceTo(destPos) < 15) {
					onReturn();
				}
			} else {
				destroySelf();
			}
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;

		if (target != null && other.gameObject == target && !targetHit) {
			targetHit = true;
			maxTime = 1f;
		}
		var character = other.gameObject as Character;
		if (time > 0.22f && character != null && character.player == owner) {
			onReturn();
		}
	}

	public void onReturn() {
		if (!destroyed) {
			destroySelf();
			if (owner.weapon is BlackArrow) {
				owner.weapon.ammo += 2;
			}
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
	}
}

public class BlackArrowGrounded : Projectile {
	public Axl? axl;
	public BlackArrowGrounded(Weapon weapon, Point pos, Player player, float byteAngle, ushort netProjId, bool rpc = false) :
		base(weapon, pos, 1, 0, 1, player, "blackarrow_stuck_proj", 0, 0f, netProjId, player.ownedByLocalPlayer) {
		byteAngle = byteAngle % 256;
		this.byteAngle = byteAngle;
		maxTime = 4	;
		if (axl?.isWhiteAxl() == true) {
			maxTime = 10;
		}
		projId = (int)ProjIds.BlackArrowGround;
		destroyOnHit = true;
		playSound("minePlant");
		if (rpc) {
			rpcCreateByteAngle(pos, player, netProjId, byteAngle);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new BlackArrowGrounded(BlackArrow.netWeapon,
			args.pos, args.player, args.byteAngle, args.netId
		);
	}
	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		moveWithMovingPlatform();
		if (axl?.isWhiteAxl() == true) {
			maxTime = 10;
		}
	}
	public override void onDestroy() {
		base.onDestroy();
		new Anim(pos, "buster1_fade", xDir,
			axl?.player.getNextActorNetId(), true, sendRpc: true);
	}
	
}
