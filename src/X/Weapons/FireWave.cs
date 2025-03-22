using System;
using System.Collections.Generic;

namespace MMXOnline;

public class FireWave : Weapon {
	public static FireWave netWeapon = new();
	
	public FireWave() : base() {
		index = (int)WeaponIds.FireWave;
		killFeedIndex = 4;
		weaponBarBaseIndex = 4;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 4;
		weaknessIndex = (int)WeaponIds.StormTornado;
		shootSounds = new string[] { "fireWave", "fireWave", "fireWave", "fireWave" };
		fireRate = 4;
		isStream = true;
		switchCooldown = 15;
		damage = "1/1";
		ammousage = 0.5;
		effect = "Inflicts burn to enemies. DOT: 0.5/2 seconds.\nBurn won't give assists.";
		hitcooldown = "0.2/0.33";
		maxAmmo = 28;
		ammo = maxAmmo;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) {
			return 7;
		}
		return 0.15f;
	}

	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (character != null) {
			if (character.isUnderwater()) return;
			if (chargeLevel < 3) {
				var proj = new FireWaveProj( pos, xDir, mmx, player, player.getNextActorNetId(), true);
				proj.vel.inc(character.vel.times(-0.5f));
			} else {
				new FireWaveProjChargedStart(pos, xDir, mmx, player, player.getNextActorNetId(), true);
			}
		}
	}
}

public class FireWaveProj : Projectile {
	public FireWaveProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "fire_wave", netId, player	
	) {
		weapon = FireWave.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 12;
		vel = new Point(400 * xDir, 0);
		projId = (int)ProjIds.FireWave;
		fadeSprite = "fire_wave_fade";
		maxTime = 0.1f;
		destroyOnHit = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new FireWaveProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class FireWaveProjChargedStart : Projectile {
	public FireWaveProjChargedStart(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "fire_wave_charge", netId, player	
	) {
		weapon = FireWave.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 13;
		vel = new Point(150 * xDir, 0);
		projId = (int)ProjIds.FireWaveChargedStart;
		if (collider != null) { collider.wallOnly = true; }
		destroyOnHit = false;
		shouldShieldBlock = false;
		maxTime = 8; // WDYM IT WAS INFINITE BEFORE
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new FireWaveProjChargedStart(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (isUnderwater()) {
			destroySelf(disableRpc: true);
			return;
		}
		incPos(new Point(0, Global.spf * 100));
		if (grounded) {
			destroySelf();
			if (ownedByLocalPlayer) {
				new FireWaveProjCharged(
					pos, xDir, this, damager.owner, 0,
					Global.level.mainPlayer.getNextActorNetId(), 0, rpc: true
				);
				playSound("fireWave");
			}
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		var character = damagable as Character;
	}

	public void putOutFire() {
		base.destroySelf("", "", false, true);
	}
}

public class FireWaveProjCharged : Projectile {
	public Sprite spriteMid;
	public Sprite spriteTop;
	public float riseY = 0;
	public float parentTime = 0;
	public FireWaveProjCharged? child;
	public bool reversedOnce;
	public int timesReversed;
	float soundCooldown;
	public FireWaveProjCharged(
		Point pos, int xDir, Actor owner, Player player, float parentTime,
		ushort? netId, int timesReversed, bool rpc = false
	) : base(
		pos, xDir, owner, "fire_wave_charge", netId, player	
	) {
		weapon = FireWave.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 19;
		vel = new Point(0 * xDir, 0);
		projId = (int)ProjIds.FireWaveCharged;
		spriteMid = new Sprite("fire_wave_charge");
		spriteMid.visible = false;
		spriteTop = new Sprite("fire_wave_charge");
		spriteTop.visible = false;
		useGravity = true;
		if (collider != null) { collider.wallOnly = true; }
		frameSpeed = 0;
		this.parentTime = parentTime;
		destroyOnHit = false;
		shouldShieldBlock = false;
		this.timesReversed = timesReversed;
		new Anim(this.pos.clone(), "fire_wave_charge_flash", 1, null, true);

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		maxTime = 0.48f;
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new FireWaveProjCharged(
		args.pos, args.xDir, args.owner, args.player, 0, args.netId, 0
		);
	}

	public override void render(float x, float y) {
		sprite.draw(frameIndex, pos.x + x, pos.y + y - riseY, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		spriteMid.draw((int)MathF.Round(frameIndex + (sprite.totalFrameNum / 3)) % sprite.totalFrameNum, pos.x + x, pos.y + y - 6 - riseY, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		spriteTop.draw((int)MathF.Round(frameIndex + (sprite.totalFrameNum / 2)) % sprite.totalFrameNum, pos.x + x, pos.y + y - 12 - riseY, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
	}

	public override void update() {
		base.update();
		if (isUnderwater()) {
			destroySelf(disableRpc: true);
			return;
		}
		if (soundCooldown > 0) {
			soundCooldown = Helpers.clampMin0(soundCooldown - Global.spf);
		}
		frameSpeed = 1;
		if (time >= 0.16f) {
			spriteTop.visible = true;
			spriteMid.visible = true;
			riseY += (Global.spf * 75);
		}
		if (time > 0.2f && child == null && parentTime < 3) {
			if (soundCooldown == 0) {
				playSound("fireWave");
				soundCooldown = 0.25f;
			}

			if (ownedByLocalPlayer) {
				var wall = Global.level.checkTerrainCollisionOnce(this, 16 * xDir, -4);
				var sign = 1;
				if (wall != null && wall.gameObject is Wall && wall.hitData.normal != null && !wall.hitData.normal.Value.isAngled()) {
					sign = -1;
					timesReversed++;
				} else {
				}

				if (timesReversed > 0) {
					destroySelf();
					return;
				}
				child = new FireWaveProjCharged(
					pos.addxy(16 * xDir, 0), xDir * sign, this,
					damager.owner, time + parentTime, Global.level.mainPlayer.getNextActorNetId(),
					timesReversed, rpc: true
				);
			}
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		var character = damagable as Character;
	}

	public override void onDestroy() {
		var newPos = pos.addxy(0, -24 - riseY);
		new Anim(newPos, "fire_wave_charge_fade", 1, null, true);
	}

	public void putOutFire() {
		base.destroySelf("", "", false, true);
	}
}
