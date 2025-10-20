using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class BlastLauncher : AxlWeapon {
	public BlastLauncher(int altFire) : base(altFire) {
		shootSounds = new string[] { "grenadeShoot", "grenadeShoot", "grenadeShoot", "rocketShoot" };
		index = (int)WeaponIds.BlastLauncher;
		weaponBarBaseIndex = 29;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 29;
		killFeedIndex = 29;
		switchCooldown = 6;
		fireRate = 45;
		sprite = "axl_arm_blastlauncher";
		flashSprite = "axl_pistol_flash_charged";
		chargedFlashSprite = "axl_pistol_flash_charged";
		altFireCooldown = 90;
		rechargeAmmoCooldown = 240;
		altRechargeAmmoCooldown = 240;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel < 3) return 4;
		return 8;
	}

	public override float whiteAxlFireRateMod() {
		return 1.5f;
	}

	public override float whiteAxlAmmoMod() {
		return 1f;
	}
	public override void axlAltShoot(Character character, int[] args) {
		if (character is not Axl axl) return;
		if (axl.loadout.blastLauncherAlt == 0) {
			if (shootCooldown > 0) return;
		} else return;
		base.axlAltShoot(character, args);
	}

	public override void axlGetAltProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		if (player.character is not Axl axl) return;
		Point bulletDir = Point.createFromAngle(angle);
		Projectile grenade;
		grenade = new GreenSpinnerProj(weapon, bulletPos, xDir, player, bulletDir, target, netId);
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, grenade.projId, netId, bulletPos, xDir, angle);
		}
	}

	public override void axlGetProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile grenade;
		grenade = new GrenadeProj(weapon, bulletPos, xDir, player, bulletDir, target, cursorPos, 0, netId);
		player.grenades.Add(grenade as GrenadeProj);
		if (player.grenades.Count > 8) {
			player.grenades[0].destroySelf();
			player.grenades.RemoveAt(0);
		}
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, grenade.projId, netId, bulletPos, xDir, angle);
		}
	}
}

public class GrenadeProj : Projectile, IDamagable {
	public IDamagable target;
	int type = 0;
	bool planted;
	int framesNotMoved;
	// We use player here due to airblast reflect potential.
	// The explosion should still be the original owner's.
	Player? player;
	public GrenadeProj(
		Weapon weapon, Point pos, int xDir, Player player, Point bulletDir,
		IDamagable target, Point cursorPos, int chargeLevel, ushort netProjId
	) : base(
		weapon, pos, xDir, 300, 0, player, "axl_grenade", 0, 0, netProjId, player.ownedByLocalPlayer
	) {
		this.target = target;

		if (player?.character is Axl { loadout.blastLauncherAlt: 1 }) {
			type = 1;
			speed = 250;
		}

		if (type == 1) {
			projId = (int)ProjIds.BlastLauncherMineGrenadeProj;
			fadeSound = "explosion";
			fadeSprite = "explosion";
		}

		vel.x = speed * bulletDir.x;
		vel.y = speed * bulletDir.y;

		projId = (int)ProjIds.BlastLauncherGrenadeProj;
		useGravity = true;
		collider.wallOnly = true;
		destroyOnHit = false;
		shouldShieldBlock = false;
		reflectableFBurner = true;
		this.player = player;
		updateAngle();
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();

		updateAngle();
		if (MathF.Abs(vel.y) < 0.5f && grounded) {
			vel.y = 0;
			vel.x *= 0.5f;
		}
		if (MathF.Abs(vel.x) < 1) {
			vel.x = 0;
		}

		if (deltaPos.isCloseToZero()) {
			framesNotMoved++;
		}

		if (type == 1 && (vel.isZero() || framesNotMoved > 1) && !planted) {
			changeSprite("axl_grenade_mine", true);
			planted = true;
			stopMoving();
			useGravity = false;
		}

		if (planted) {
			if (!Global.level.hasMovingPlatforms) isStatic = true;
			moveWithMovingPlatform();
		}

		if (time > 2 && type == 0) {
			destroySelf(disableRpc: true);
		}
	}

	public void updateAngle() {
		if (vel.magnitude > 50) {
			angle = MathF.Atan2(vel.y, vel.x) * 180 / MathF.PI;
		}
		xDir = 1;
	}

	public override void onCollision(CollideData other) {
		if (planted) return;
		base.onCollision(other);
		if (ownedByLocalPlayer) {
			var damagable = other.gameObject as IDamagable;
			if (damagable != null && damagable.canBeDamaged(owner.alliance, owner.id, projId) &&
				!vel.isZero() && type == 0
			) {
				destroySelf();
				return;
			}
		}
		var wall = other.gameObject as Wall;
		if (wall != null) {
			Point? normal = other.hitData.normal;
			if (normal != null) {
				if (normal.Value.x != 0) vel.x *= -0.5f;
				if (normal.Value.y != 0) vel.y *= -0.5f;
				if (type == 1) {
					vel.x = 0;
					vel.y = MathF.Sign(vel.y);
				}
			}
		}
	}

	public override void onDestroy() {
		if (!ownedByLocalPlayer) return;
		if (type == 0 && player != null) {
			new GrenadeExplosionProj(
				weapon, pos, xDir, player, type, target, Math.Sign(vel.x), player.getNextActorNetId()
			);
		}
	}

	float health = 2;
	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (type == 1) {
			health -= damage;
			if (health < 0) {
				health = 0;
				destroySelf();
			}
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return sprite.name == "axl_grenade_mine" && owner.alliance != damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) { return false; }
	public bool canBeHealed(int healerAlliance) { return false; }
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }

	public void detonate() {
		playSound("detonate", sendRpc: true);
		if (player != null)
		new GrenadeExplosionProj(
			weapon, pos, xDir, player, type, target, Math.Sign(vel.x), player.getNextActorNetId()
		);
		destroySelfNoEffect();
	}

	public bool isPlayableDamagable() {
		return false;
	}
}

public class GrenadeExplosionProj : Projectile {
	public IDamagable directHit;
	public int directHitXDir;
	public int type;
	public List<int> rands;

	public GrenadeExplosionProj(
		Weapon weapon, Point pos, int xDir, Player player, int type,
		IDamagable directHit, int directHitXDir, ushort netProjId
	) : base(
		weapon, pos, xDir, 0, 2, player, "axl_grenade_explosion2", 0, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
		this.xDir = xDir;
		this.directHit = directHit;
		this.directHitXDir = directHitXDir;
		this.type = type;
		destroyOnHit = false;
		projId = (int)ProjIds.BlastLauncherGrenadeSplash;
		playSound("grenadeExplode");
		shouldShieldBlock = false;
		rands = new List<int>();
		for (int i = 0; i < 8; i++) {
			rands.Add(Helpers.randomRange(-22, 22));
		}
		if (type == 1) {
			damager.damage = 4;
		}
		if (ownedByLocalPlayer) {
			rpcCreate(pos, owner, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();

		if (sprite.name == "axl_grenade_explosion_hyper") {
			xScale = 1.5f;
			yScale = 1.5f;
		}

		if (isAnimOver()) {
			destroySelf();
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		//float maxTime = type == 0 ? 0.15f : 0.2f;
		//float len = type == 0 ? 10 : 15;
		float maxTime = 0.175f;
		float len = 7;
		if (time < maxTime) {
			for (int i = 0; i < 8; i++) {
				float angle = (i * 45) + rands[i];
				float ox = (len * time * 25) * Helpers.cosd(angle);
				float oy = (len * time * 25) * Helpers.sind(angle);
				float ox2 = len * Helpers.cosd(angle);
				float oy2 = len * Helpers.sind(angle);
				DrawWrappers.DrawLine(
					pos.x + ox, pos.y + oy, pos.x + ox + ox2, pos.y + oy + oy2, Color.Yellow, 1, zIndex, true
				);
			}
		}
	}

	public override DamagerMessage? onDamage(IDamagable damagable, Player attacker) {
		if (damagable is not Character character) {
			return null;
		}
		bool directHit = this.directHit == character;

		Point victimCenter = character.getCenterPos();
		Point bombCenter = pos;
		if (directHit) {
			bombCenter.x = victimCenter.x - (directHitXDir * 5);
		}
		Point dirTo = bombCenter.directionTo(victimCenter);
		float distFactor = Helpers.clamp01(1 - (bombCenter.distanceTo(victimCenter) / 60f));

		character.pushEffect(new Point(0.3f, 0.3f) * dirTo * distFactor);

		if (character == attacker.character) {
			float damage = damager.damage;
			if ((character as Axl)?.isWhiteAxl() == true) {
				damage = 0;
			}
			return new DamagerMessage() {
				damage = damage,
				flinch = 0
			};
		}

		return null;
	}
}

public class GreenSpinnerProj : Projectile {
	IDamagable target;
	public GreenSpinnerProj(
		Weapon weapon, Point pos, int xDir, Player player, Point bulletDir, IDamagable target, ushort netProjId
	) : base(
		weapon, pos, xDir, 400, 0, player, "axl_rocket", 0, 0, netProjId, player.ownedByLocalPlayer
	) {
		this.target = target;
		vel.x = speed * bulletDir.x;
		vel.y = speed * bulletDir.y;
		collider.wallOnly = true;
		destroyOnHit = false;
		this.xDir = xDir;
		angle = MathF.Atan2(vel.y, vel.x) * 180 / MathF.PI;
		projId = (int)ProjIds.GreenSpinner;
		maxTime = 0.35f;
		shouldShieldBlock = false;
	}

	public override void update() {
		base.update();
		if (grounded) {
			destroySelf(disableRpc: true);
			return;
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (ownedByLocalPlayer) {
			var damagable = other.gameObject as IDamagable;
			if (damagable != null && damagable.canBeDamaged(owner.alliance, owner.id, projId)) {
				destroySelf();
				return;
			}
		}
		var wall = other.gameObject as Wall;
		if (wall != null) {
			destroySelf(disableRpc: true);
			return;
		}
	}

	public override void onDestroy() {
		if (!ownedByLocalPlayer) return;
		if (time >= maxTime) return;
		var netId = owner.getNextActorNetId();
		new GreenSpinnerExplosionProj(weapon, pos, xDir, owner, angle, target, Math.Sign(vel.x), netId);
	}
}

public class GreenSpinnerExplosionProj : Projectile {
	public IDamagable directHit;
	public int directHitXDir;
	public GreenSpinnerExplosionProj(
		Weapon weapon, Point pos, int xDir, Player player, float angle,
		IDamagable directHit, int directHitXDir, ushort netProjId
	) : base(
		weapon, pos, xDir, 0, 3, player, "axl_rocket_explosion", 13, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
		this.xDir = xDir;
		this.angle = angle;
		this.directHit = directHit;
		this.directHitXDir = directHitXDir;
		destroyOnHit = false;
		playSound("rocketExplode");
		projId = (int)ProjIds.GreenSpinnerSplash;
		shouldShieldBlock = false;
		if (ownedByLocalPlayer) {
			rpcCreate(pos, owner, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (isAnimOver()) {
			destroySelf();
		}
	}

	public override DamagerMessage? onDamage(IDamagable damagable, Player attacker) {
		if (damagable is not Character character) {
			return null;
		}
		bool directHit = this.directHit == character;
		int directHitXDir = this.directHitXDir;

		var victimCenter = character.getCenterPos();
		var bombCenter = pos;
		if (directHit) {
			bombCenter.x = victimCenter.x - (directHitXDir * 5);
		}
		Point dirTo = bombCenter.directionTo(victimCenter);
		float distFactor = Helpers.clamp01(1 - (bombCenter.distanceTo(victimCenter) / 60f));

		if (character == attacker.character) {
			character.pushEffect(new Point(0.6f, 0.6f) * dirTo * distFactor);
		} else {
			character.pushEffect(new Point(0.3f, -0.25f) * dirTo * distFactor);
		}

		if (character == attacker.character) {
			float damage = damager.damage;
			if ((character as Axl)?.isWhiteAxl() == true) damage = 0;
			return new DamagerMessage() {
				damage = damage,
				flinch = 0
			};
		}

		return null;
	}
}
