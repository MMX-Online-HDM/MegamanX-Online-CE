using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
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
	public VileLaser() : base() {
		index = (int)WeaponIds.VileLaser;
	}
}
public class RisingSpecter : VileLaser {
	public static RisingSpecter netWeapon = new();
	public RisingSpecter() : base() {
		index = (int)WeaponIds.RisingSpecter;
		type = (int)VileLaserType.RisingSpecter;
		displayName = "Rising Specter";
		vileAmmoUsage = 24;
		killFeedIndex = 120;
		vileWeight = 3;
		ammousage = vileAmmoUsage;
		damage = "6";
		hitcooldown = "0.5";
		flinch = "26";
		effect = "Insane Hitbox.";
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (vile.energy.ammo < vileAmmoUsage) return;
		vile.changeState(new LaserAttack(this), true);
	}
	
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vile) return;
		Point shootPos = vile.setCannonAim(new Point(1.5f, -1));
		new RisingSpecterProj(
			shootPos, vile.xDir, vile, vile.player,
			vile.player.getNextActorNetId(), rpc: true
		);
		vile.playSound("risingSpecter", sendRpc: true);
		vile.setVileShootTime(this);
		vile.tryUseVileAmmo(vileAmmoUsage);
	}
}
public class NecroBurst : VileLaser {
	public static NecroBurst netWeapon = new();
	public NecroBurst() : base() {
		index = (int)WeaponIds.NecroBurst;
		type = (int)VileLaserType.NecroBurst;
		displayName = "Necro Burst";
		vileAmmoUsage = 32;
		killFeedIndex = 75;
		vileWeight = 3;
		ammousage = vileAmmoUsage;
		damage = "6";
		hitcooldown = "0.5";
		flinch = "26";
		effect = "Self Damages.";
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (vile.energy.ammo < vileAmmoUsage) return;
		vile.changeState(new LaserAttack(this), true);
	}
	
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		Point shootPos = vava.setCannonAim(new Point(1, 0));
		new NecroBurstProj(
			shootPos, vava.xDir, vava, vava.player,
			vava.player.getNextActorNetId(), rpc: true
		);
		vava.applyDamage(8, vava.player, character, (int)WeaponIds.NecroBurst, (int)ProjIds.NecroBurst);
		vava.playSound("necroburst", sendRpc: true);
		vava.setVileShootTime(this);
		vava.tryUseVileAmmo(vileAmmoUsage);
	}
}
public class StraightNightmare : VileLaser {
	public static StraightNightmare netWeapon = new();
	public StraightNightmare() : base() {
		index = (int)WeaponIds.StraightNightmare;
		type = (int)VileLaserType.StraightNightmare;
		displayName = "Straight Nightmare";
		vileAmmoUsage = 24;
		killFeedIndex = 171;
		vileWeight = 3;
		ammousage = vileAmmoUsage;
		damage = "1";
		hitcooldown = "0.15";
		effect = "Won't destroy on hit.";
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (vile.energy.ammo < vileAmmoUsage) return;
		vile.changeState(new LaserAttack(this), true);
	}

	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		Point shootPos = vava.setCannonAim(new Point(1, 0));
		new StraightNightmareProj(
			shootPos.addxy(-8 * vava.xDir, 0), vava.xDir, vava,
			vava.player, vava.player.getNextActorNetId(), rpc: true
		);
		vava.playSound("straightNightmareShoot", sendRpc: true);
		vava.setVileShootTime(this);
		vava.tryUseVileAmmo(vileAmmoUsage);
	}
}
public class NoneLaser : VileLaser {
	public static NoneLaser netWeapon = new();
	public NoneLaser() : base() {
		type = (int)VileLaserType.None;
		displayName = "None";
		killFeedIndex = 126;
	}
}
#region States
public class LaserAttack : VileState {
	public bool shot;
	public int shootFrame = 0;
	public VileLaser weapon;
	public float shootTime;
	public int loopNum;
	public float airTimer;

	public LaserAttack(VileLaser weapon) : base("idle_shoot") {
		useDashJumpSpeed = true;
		airMove = true;
		canJump = true;
		canStopJump = true;
		airSprite = "cannon_air";
		landSprite = "idle_shoot";
		this.weapon = weapon;
	}

	public override void update() {
		base.update();
		if (weapon is RisingSpecter) {
			if (!vile.grounded && !shot && airTimer < 30) {
				stateFrames = 0;
				airTimer += vile.speedMul;
			} else {
				vile.cannonAimNum = 1;
				if (!shot) {
					shot = true;
					weapon.shoot(vile, []);
				}
				if (shot && stateFrames > 12) {
					character.changeToIdleOrFall();
				}
				
			}
			return;
		}
		character.turnToInput(player.input, player);
		if (character.frameIndex >= shootFrame && !shot) {
			shot = true;
			weapon.shoot(vile, []);
		}
		if (stateFrames > 30) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopCharge();
		if (!character.grounded) {
			sprite = "cannon_air";
			character.changeSpriteFromName(sprite, true);
			if (weapon is RisingSpecter) {
				character.stopMoving();
				character.vel = new Point();
				airMove = false;
			}
		}
	}
	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}
#endregion
#region Projectiles
public class RisingSpecterProj : Projectile {
	public Point destPos;
	public float sinDampTime = 1;
	public Anim muzzle;
	public RisingSpecterProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "empty", netId, player
	) {
		weapon = RisingSpecter.netWeapon;
		damager.damage = 6;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 60;
		maxTime = 0.5f;
		destroyOnHit = false;
		shouldShieldBlock = false;
		vel = new Point();
		projId = (int)ProjIds.RisingSpecter;
		shouldVortexSuck = false;
		//float destX = xDir * 150;
		//float destY = -100;
		//this.pos = pos.addxy(destX * 0.0225f, destY * 0.0225f);
		destPos = new Point(150 * xDir, -100);

		muzzle = new Anim(pos, "risingspecter_muzzle", xDir, null, false, host: player.character) {
			angle = xDir == 1 ? destPos.angle : destPos.angle + 180
		};
		sprite.hitboxes = popullateHitboxes();
		Global.level.addToGrid(this);

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new RisingSpecterProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	private Collider[] popullateHitboxes() {
		Collider[] retCol = new Collider[7];
		Point jumps = destPos.normalize() * 24;
		Point offset = destPos.normalize() * 18;
		for (int i = 0; i < retCol.Length; i++) {
			retCol[i] = new Collider(
				new Rect(0 + offset.x, 0 + offset.y, 40 + offset.x, 40 + offset.y).getPoints(),
				false, this, false, false,
				HitboxFlag.Hitbox, Point.zero
			);
			offset += jumps;
		}
		return retCol;
	}

	public override void onDestroy() {
		base.onDestroy();
		muzzle?.destroySelf();
	}

	public override void update() {
		base.update();
		if (ownerActor != null && time < 4f/60f) {
			incPos(ownerActor.deltaPos);
		}
		muzzle?.changePos(pos);
	}

	public override void render(float x, float y) {
		base.render(x, y);

		var col1 = new Color(116, 11, 237, 128);
		var col2 = new Color(250, 62, 244, 192);
		var col3 = new Color(240, 240, 240, 255);

		float sin = MathF.Sin(Global.time * 100);
		float sinDamp = Helpers.clamp01(1 - (time / maxTime));

		Point dirTo = destPos.normalize();
		Point basePos = pos + dirTo * 5;
		float jutX = dirTo.x;
		float jutY = dirTo.y;

		DrawWrappers.DrawLine(
			basePos.x, basePos.y, pos.x + destPos.x, pos.y + destPos.y,
			col1, (30 + sin * 15) * sinDamp, 0, true
		);
		DrawWrappers.DrawLine(
			basePos.x - jutX * 2, basePos.y - jutY * 2,
			pos.x + destPos.x + jutX * 2, pos.y + destPos.y + jutY * 2,
			col2, (20 + sin * 10) * sinDamp, 0, true
		);
		DrawWrappers.DrawLine(
			basePos.x - jutX * 4, basePos.y - jutY * 4,
			pos.x + destPos.x + jutX * 4, pos.y + destPos.y + jutY * 4,
			col3, (10 + sin * 5) * sinDamp, 0, true
		);
	}
}
public class NecroBurstProj : Projectile {
	public float radius = 10;
	public float attackRadius { get { return radius + 15; } }
	public float overrideDamage;
	public NecroBurstProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "empty", netId, player
	) {
		weapon = NecroBurst.netWeapon;
		damager.damage = 6;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
		maxTime = 0.5f;
		destroyOnHit = false;
		shouldShieldBlock = false;
		vel = new Point();
		projId = (int)ProjIds.NecroBurst;
		shouldVortexSuck = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new NecroBurstProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
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
	public RAShrapnelProj(
		Point pos, string spriteName, int xDir, bool hasRaColorShader,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, spriteName, netId, player
	) {
		weapon = NecroBurst.netWeapon;
		damager.damage = 6;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
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
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] {
				spriteIndexBytes[0], spriteIndexBytes[1], hasRaColorShaderByte}
			);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		string spriteIndexBytes = Encoding.ASCII.GetString(args.extraData[0..]);
		return new RAShrapnelProj(
			args.pos, spriteIndexBytes, args.xDir,
			args.extraData[1] == 1, args.owner, args.player, args.netId
		);
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

	public StraightNightmareProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "straightnightmare_proj", netId, player
	) {
		weapon = StraightNightmare.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 9;
		vel = new Point (150 * xDir, 0);		
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

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new StraightNightmareProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
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
#endregion
/*
public class VileLaser : Weapon {
	public float vileAmmoUsage;
	public static VileLaser netWeaponRS = new VileLaser(VileLaserType.RisingSpecter);
	public static VileLaser netWeaponNB = new VileLaser(VileLaserType.NecroBurst);
	public static VileLaser netWeaponSN = new VileLaser(VileLaserType.StraightNightmare);
	public VileLaser(VileLaserType vileLaserType) : base() {
		index = (int)WeaponIds.VileLaser;
		type = (int)vileLaserType;

		if (vileLaserType == VileLaserType.None) {
			displayName = "None";
			description = new string[] { "Do not equip a Laser." };
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmoUsage = 0;
			fireRate = 0;
			vileWeight = 0;
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
			flinch = "26";
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
			flinch = "26";
			effect = "Self Damages.";
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
		if (type == (int)VileLaserType.None) return;

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
				shootPos, vile.xDir, vile, vile.player, 
				vile.player.getNextActorNetId(), rpc: true
			);
			vile.playSound("risingSpecter", sendRpc: true);
		}
	}
}



public class NecroBurstAttack : VileState {
	bool shot = false;

	public NecroBurstAttack(bool grounded) : base(grounded ? "idle_shoot" : "cannon_air") {
	}

	public override void update() {
		base.update();
		if (!shot) {
			shot = true;
			shoot(vile);
			character.applyDamage(8, player, character, (int)WeaponIds.NecroBurst, (int)ProjIds.NecroBurst);
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
				shootPos, vile.xDir, vile, vile.player,
				vile.player.getNextActorNetId(), rpc: true
			);
			vile.playSound("necroburst", sendRpc: true);
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
			new StraightNightmareProj(
				shootPos.addxy(-8 * vile.xDir, 0), vile.xDir, vile, 
				vile.player, vile.player.getNextActorNetId(), rpc: true
			);
		}
	}
}
*/
