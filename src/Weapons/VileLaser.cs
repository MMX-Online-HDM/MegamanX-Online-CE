using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public enum VileLaserType {
	None = -1,
	RisingSpecter,
	NecroBurst,
	StraightNightmare,
}

public class VileLaser : Weapon {
	public float vileAmmoUsage;
	public VileLaser(VileLaserType vileLaserType) : base() {
		index = (int)WeaponIds.VileLaser;
		type = (int)vileLaserType;

		if (vileLaserType == VileLaserType.None) {
			displayName = "None";
			description = new string[] { "Do not equip a Laser." };
			killFeedIndex = 126;
		} else if (vileLaserType == VileLaserType.RisingSpecter) {
			index = (int)WeaponIds.RisingSpecter;
			displayName = "Rising Specter";
			vileAmmoUsage = 8;
			description = new string[] { "It cannot be aimed,", "but its wide shape covers a large area." };
			killFeedIndex = 120;
			vileWeight = 3;
			ammousage = 24;
			damage = "6";
			hitcooldown = "0.5";
			Flinch = "26";
			effect = "Insane Hitbox.";
		} else if (vileLaserType == VileLaserType.NecroBurst) {
			index = (int)WeaponIds.NecroBurst;
			displayName = "Necro Burst";
			vileAmmoUsage = 5;
			description = new string[] { "Use up all your energy at once to", "unleash a powerful energy burst." };
			killFeedIndex = 75;
			vileWeight = 3;
			ammousage = 32;
			damage = "6";
			hitcooldown = "0.5";
			Flinch = "26";
			effect = "No DMG inside Ride.";
		} else if (vileLaserType == VileLaserType.StraightNightmare) {
			index = (int)WeaponIds.StraightNightmare;
			displayName = "Straight Nightmare";
			vileAmmoUsage = 8;
			description = new string[] { "Though slow, this laser can burn", "through multiple enemies in a row." };
			killFeedIndex = 171;
			vileWeight = 3;
			ammousage = 24;
			damage = "1";
			hitcooldown = "0.15";
			effect = "Won't destroy on hit.";
		}
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (type == (int)VileLaserType.NecroBurst) {
			return 32;
		} else {
			return 24;
		}
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (type == (int)VileLaserType.NecroBurst && vile.charState is InRideArmor inRideArmor) {
			NecroBurstAttack.shoot(vile);
			vile.rideArmor?.explode(shrapnel: inRideArmor.isHiding);
		} else {
			if (type == (int)VileLaserType.NecroBurst) {
				vile.changeState(new NecroBurstAttack(vile.grounded), true);
			} else if (type == (int)VileLaserType.RisingSpecter) {
				vile.changeState(new RisingSpecterState(vile.grounded), true);
			} else if (type == (int)VileLaserType.StraightNightmare) {
				vile.changeState(new StraightNightmareAttack(vile.grounded), true);
			}
		}
	}
}

public class RisingSpecterState : CharState {
	bool shot = false;
	bool grounded;

	public RisingSpecterState(bool grounded) : base(grounded ? "idle_shoot" : "fall") {
		this.grounded = grounded;
	}

	public override void update() {
		base.update();

		if (!grounded) {
			if (!character.grounded) {
				stateTime = 0;
				return;
			} else {
				character.changeSpriteFromName("idle_shoot", true);
				grounded = true;
				return;
			}
		}

		if (!shot) {
			shot = true;
			if (character is Vile vile) {
				shoot(vile);
			}
		}

		if (stateTime > 0.5f) {
			character.changeToIdleOrFall();
		}
	}

	public void shoot(Vile vile) {
		Point shootPos = vile.setCannonAim(new Point(1.5f, -1));

		if (vile.tryUseVileAmmo(vile.laserWeapon.getAmmoUsage(0))) {
			new RisingSpecterProj(
				new VileLaser(VileLaserType.RisingSpecter), shootPos, vile.xDir,
				vile.player, vile.player.getNextActorNetId(), rpc: true
			);
			vile.playSound("risingSpecter", sendRpc: true);
		}
	}
}

public class RisingSpecterProj : Projectile {
	public Point destPos;
	public float sinDampTime = 1;
	public Anim muzzle;
	public RisingSpecterProj(Weapon weapon, Point poi, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, poi, xDir, 0, 6, player, "empty", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 0.5f;
		destroyOnHit = false;
		shouldShieldBlock = false;
		vel = new Point();
		projId = (int)ProjIds.RisingSpecter;
		shouldVortexSuck = false;
		float destX = xDir * 150;
		float destY = -100;
		Point toDestPos = new Point(destX, destY);
		pos = poi.addxy(destX * 0.0225f, destY * 0.0225f);
		destPos = pos.add(toDestPos);

		muzzle = new Anim(poi, "risingspecter_muzzle", xDir, null, false, host: player.character) {
			angle = xDir == 1 ? toDestPos.angle : toDestPos.angle + 180
		};

		float ang = poi.directionTo(destPos).angle;
		var points = new List<Point>();
		if (xDir == 1) {
			float sideY = 30 * Helpers.cosd(ang);
			float sideX = -30 * Helpers.sind(ang);
			points.Add(new Point(poi.x - sideX, poi.y - sideY));
			points.Add(new Point(destPos.x - sideX, destPos.y - sideY));
			points.Add(new Point(destPos.x + sideX, destPos.y + sideY));
			points.Add(new Point(poi.x + sideX, poi.y + sideY));
		} else {
			float sideY = 30 * Helpers.cosd(ang);
			float sideX = 30 * Helpers.sind(ang);
			points.Add(new Point(destPos.x - sideX, destPos.y + sideY));
			points.Add(new Point(destPos.x + sideX, destPos.y - sideY));
			points.Add(new Point(poi.x + sideX, poi.y - sideY));
			points.Add(new Point(poi.x - sideX, poi.y + sideY));
		}

		globalCollider = new Collider(points, true, null, false, false, 0, Point.zero);

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		muzzle?.destroySelf();
	}

	public override void update() {
		base.update();
		/*
		if (muzzle != null)
		{
			incPos(muzzle.deltaPos);
			destPos = destPos.add(muzzle.deltaPos);
		}
		*/
	}

	public override void render(float x, float y) {
		base.render(x, y);

		var col1 = new Color(116, 11, 237, 128);
		var col2 = new Color(250, 62, 244, 192);
		var col3 = new Color(240, 240, 240, 255);

		float sin = MathF.Sin(Global.time * 100);
		float sinDamp = Helpers.clamp01(1 - (time / maxTime));

		var dirTo = pos.directionToNorm(destPos);
		float jutX = dirTo.x;
		float jutY = dirTo.y;

		DrawWrappers.DrawLine(pos.x, pos.y, destPos.x, destPos.y, col1, (30 + sin * 15) * sinDamp, 0, true);
		DrawWrappers.DrawLine(
			pos.x - jutX * 2, pos.y - jutY * 2,
			destPos.x + jutX * 2, destPos.y + jutY * 2,
			col2, (20 + sin * 10) * sinDamp, 0, true
		);
		DrawWrappers.DrawLine(
			pos.x - jutX * 4, pos.y - jutY * 4,
			destPos.x + jutX * 4, destPos.y + jutY * 4,
			col3, (10 + sin * 5) * sinDamp, 0, true
		);
	}
}

public class NecroBurstAttack : CharState {
	bool shot = false;
	Vile vile = null!;

	public NecroBurstAttack(bool grounded) : base(grounded ? "idle_shoot" : "cannon_air") {
	}

	public override void update() {
		base.update();

		if (!shot) {
			shot = true;
			shoot(vile);
		}

		if (character.sprite.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public static void shoot(Vile vile) {
		if (vile.tryUseVileAmmo(vile.laserWeapon.getAmmoUsage(0))) {
			Point shootPos = vile.setCannonAim(new Point(1, 0));
			//character.vileAmmoRechargeCooldown = 3;
			new NecroBurstProj(
				new VileLaser(VileLaserType.NecroBurst), shootPos,
				vile.xDir, vile.player, vile.player.getNextActorNetId(), rpc: true
			);
			vile.playSound("necroburst", sendRpc: true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		vile = character as Vile ?? throw new NullReferenceException();
	}
}

public class NecroBurstProj : Projectile {
	public float radius = 10;
	public float attackRadius { get { return radius + 15; } }
	public NecroBurstProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 6, player, "empty", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 0.5f;
		destroyOnHit = false;
		shouldShieldBlock = false;
		vel = new Point();
		projId = (int)ProjIds.NecroBurst;
		shouldVortexSuck = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (isRunByLocalPlayer()) {
			foreach (var go in getCloseActors(MathInt.Ceiling(radius + 50))) {
				var chr = go as Character;
				bool isHurtSelf = chr?.player == damager.owner;
				if (go is not Actor actor) continue;
				if (go is not IDamagable damagable) continue;
				if (!isHurtSelf && !damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, null)) continue;

				float dist = actor.getCenterPos().distanceTo(pos);
				if (dist > attackRadius) continue;

				float overrideDamage = 4 + MathF.Round(4 * (1 - Helpers.clampMin0(dist / 200)));
				int overrideFlinch = Global.defFlinch;
				if (overrideDamage == 6) overrideFlinch = (int)(Global.defFlinch * 0.75f);
				if (overrideDamage <= 5) overrideFlinch = Global.defFlinch / 2;
				if (overrideDamage == 4) overrideFlinch = 0;
				if (isHurtSelf) overrideFlinch = 0;
				damager.applyDamage(damagable, false, weapon, this, projId, overrideDamage: overrideDamage, overrideFlinch: overrideFlinch);
			}
		}
		radius += Global.spf * 400;
	}

	public override void render(float x, float y) {
		base.render(x, y);
		var col = new Color(120, 232, 240, (byte)(164 - 164 * (time / maxTime)));
		var col2 = new Color(255, 255, 255, (byte)(164 - 164 * (time / maxTime)));
		var col3 = new Color(255, 255, 255, (byte)(224 - 224 * (time / maxTime)));
		DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, true, col, 5, zIndex + 1, true);
		DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius * 0.5f, true, col2, 5, zIndex + 1, true);
		DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, false, col3, 5, zIndex + 1, true, col3);
	}
}

public class RAShrapnelProj : Projectile {
	public RAShrapnelProj(Weapon weapon, Point pos, string spriteName, int xDir, bool hasRaColorShader, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 4, player, spriteName, Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 0.35f;
		vel = new Point();
		projId = (int)ProjIds.NecroBurstShrapnel;

		var rect = new Rect(0, 0, 10, 10);
		globalCollider = new Collider(rect.getPoints(), true, this, false, false, 0, new Point(0, 0));

		if (hasRaColorShader) {
			setRaColorShader();
		}

		if (rpc) {
			byte[] spriteIndexBytes = BitConverter.GetBytes(
				Global.spriteIndexByName.GetValueOrCreate(spriteName, ushort.MaxValue)
			);
			byte hasRaColorShaderByte = hasRaColorShader ? (byte)1 : (byte)0;
			rpcCreate(pos, player, netProjId, xDir, spriteIndexBytes[0], spriteIndexBytes[1], hasRaColorShaderByte);
		}
	}
}

public class StraightNightmareAttack : CharState {
	bool shot = false;
	public StraightNightmareAttack(bool grounded) : base(grounded ? "idle_shoot" : "cannon_air") {
		enterSound = "straightNightmareShoot";
	}

	public override void update() {
		base.update();

		if (!shot) {
			shot = true;
			if (character is Vile vile) {
				shoot(vile);
			}
		}

		if (character.sprite.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public static void shoot(Vile vile) {
		if (vile.tryUseVileAmmo(vile.laserWeapon.getAmmoUsage(0))) {
			Point shootPos = vile.setCannonAim(new Point(1, 0));
			new StraightNightmareProj(new VileLaser(VileLaserType.StraightNightmare), shootPos.addxy(-8 * vile.xDir, 0), vile.xDir, vile.player, vile.player.getNextActorNetId(), sendRpc: true);
		}
	}
}

public class StraightNightmareProj : Projectile {
	public List<Sprite> spriteMids = new List<Sprite>();
	public float length = 4;
	const int maxLen = 8;
	public float maxSpeed = 400;
	public float tornadoTime;
	public float blowModifier = 0.25f;
	public float soundTime;

	public StraightNightmareProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
		base(weapon, pos, xDir, 150, 1, player, "straightnightmare_proj", 0, 0.15f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.StraightNightmare;
		maxTime = 2;
		sprite.visible = false;
		for (var i = 0; i < maxLen; i++) {
			var midSprite = new Sprite("straightnightmare_proj");
			midSprite.visible = false;
			spriteMids.Add(midSprite);
		}
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void render(float x, float y) {
		int spriteMidLen = 12;
		for (int i = 0; i < length; i++) {
			spriteMids[i].visible = true;
			spriteMids[i].draw(
				frameIndex, pos.x + x + (i * xDir * spriteMidLen), pos.y + y,
				xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex
			);
		}

		if (Global.showHitboxes && collider != null) {
			DrawWrappers.DrawPolygon(collider.shape.points, new Color(0, 0, 255, 128), true, ZIndex.HUD, isWorldPos: true);
		}
	}

	public override void update() {
		base.update();

		Helpers.decrementTime(ref soundTime);
		if (soundTime == 0) {
			playSound("straightNightmare");
			soundTime = 0.1f;
		}

		var topX = 0;
		var topY = 0;

		var spriteMidLen = 12;

		var botX = length * spriteMidLen;
		var botY = 40;

		var rect = new Rect(topX, topY, botX, botY);
		globalCollider = new Collider(rect.getPoints(), true, this, false, false, 0, new Point(0, 0));

		tornadoTime += Global.spf;
		if (tornadoTime > 0.05f) {
			if (length < maxLen) {
				length++;
			} else {
				//vel.x = maxSpeed * xDir;
			}
			tornadoTime = 0;
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!damagable.isPlayableDamagable()) { return; }
		if (damagable is not Actor actor || !actor.ownedByLocalPlayer) {
			return;
		}
		float modifier = 1;
		if (actor.grounded) { modifier = 0.5f; };
		if (damagable is Character character) {
			if (character.isPushImmune()) { return; }
			if (character.charState is Crouch) { modifier = 0.25f; }
			character.pushedByTornadoInFrame = true;
		}
		//character.damageHistory.Add(new DamageEvent(damager.owner, weapon.killFeedIndex, true, Global.frameCount));
		actor.move(new Point(maxSpeed * 0.9f * xDir * modifier * blowModifier, 0));
	}
}
