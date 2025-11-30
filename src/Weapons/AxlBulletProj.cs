using System.Collections.Generic;
namespace MMXOnline;
public class AxlBulletProj : Projectile {
	public AxlBulletProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId) :
		base(weapon, pos, 1, 600, 1, player, "axl_bullet", 0, 0, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "axl_bullet_fade";
		projId = (int)ProjIds.AxlBullet;
		angle = bulletDir.angle;
		vel.x = bulletDir.x * speed;
		vel.y = bulletDir.y * speed;
		maxTime = 0.22f;
		reflectable = true;
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		destroySelf();
	}
}

public class MettaurCrashProj : Projectile {
	public MettaurCrashProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool sendRpc = false) :
		base(weapon, pos, 1, 600, 1, player, "axl_bullet", 0, 0.1f, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "axl_bullet_fade";
		projId = (int)ProjIds.MetteurCrash;
		angle = bulletDir.angle;
		vel.x = bulletDir.x * speed;
		vel.y = bulletDir.y * speed;
		maxTime = 0.3f;
		reflectable = true;
		destroyOnHit = false;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}

	/*
	public override void onHitWall(CollideData other)
	{
		base.onHitWall(other);
		destroySelf();
	}
	*/

	public override void render(float x, float y) {
		DrawWrappers.DrawLine(pos.x, pos.y, pos.x + (deltaPos.normalize().x * 10), pos.y + (deltaPos.normalize().y * 10), SFML.Graphics.Color.Yellow, 2, 0, true);
	}
}

public class BeastKillerProj : Projectile {
	public BeastKillerProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool sendRpc = false) :
		base(weapon, pos, 1, 600, 1, player, "beastkiller_proj", 0, 0, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "axl_bullet_fade";
		projId = (int)ProjIds.BeastKiller;
		angle = bulletDir.angle;
		vel.x = bulletDir.x * speed;
		vel.y = bulletDir.y * speed;
		maxTime = 0.22f;
		reflectable = true;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		destroySelf();
	}
}

public class MachineBulletProj : Projectile {
	public MachineBulletProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool sendRpc = false) :
		base(weapon, pos, 1, 600, 1, player, "machinebullet_proj", 0, 0, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "axl_bullet_fade";
		projId = (int)ProjIds.MachineBullets;
		angle = bulletDir.angle;
		vel.x = bulletDir.x * speed;
		vel.y = bulletDir.y * speed;
		maxTime = 0.22f;
		reflectable = true;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		destroySelf();
	}
}

public class RevolverBarrelProj : Projectile {
	public RevolverBarrelProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool sendRpc = false) :
		base(weapon, pos, 1, 600, 0.5f, player, "revolverbarrel_proj", 0, 0, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "axl_bullet_fade";
		projId = (int)ProjIds.RevolverBarrel;
		angle = bulletDir.angle;
		vel.x = bulletDir.x * speed;
		vel.y = bulletDir.y * speed;
		maxTime = 0.22f;
		reflectable = true;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		destroySelf();
	}

	public override void update() {
		if (ownedByLocalPlayer && getHeadshotVictim(owner, out IDamagable? victim, out Point? hitPoint)) {
			if (hitPoint != null) changePos(hitPoint.Value);
			damager.applyDamage(victim, false, weapon, this, projId, overrideDamage: damager.damage * 3f);
			damager.damage = 0;
			playSound("hurt");
			destroySelf();
			return;
		}

		base.update();
	}
}

public class AncientGunProj : Projectile {
	public float sparkleTime = 0;
	public AncientGunProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool sendRpc = false) :
		base(weapon, pos, 1, 600, 1, player, "ancientgun_proj", 0, 0.075f, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "axl_bullet_fade";
		projId = (int)ProjIds.AncientGun;
		angle = bulletDir.angle;
		vel.x = bulletDir.x * speed;
		vel.y = bulletDir.y * speed;
		maxTime = 0.3f;
		destroyOnHit = false;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}

	public override void update() {
		sparkleTime += Global.spf;
		if (sparkleTime > 0.05) {
			sparkleTime = 0;
			new Anim(pos, "ancient_gun_sparkles", 1, null, true);
		}

		if (ownedByLocalPlayer && getHeadshotVictim(owner, out IDamagable? victim, out Point? hitPoint)) {
			//if (hitPoint != null) changePos(hitPoint.Value);
			damager.applyDamage(victim, false, weapon, this, projId, overrideDamage: damager.damage * 1.5f);
			//damager.damage = 0;
			playSound("hurt");
			//destroySelf();
			//return;
		}

		base.update();
	}
}

public class CopyShotProj : Projectile {
	public CopyShotProj(Weapon weapon, Point pos, int chargeLevel, Player player, Point bulletDir, ushort netProjId) :
		base(weapon, pos, 1, 250, 2, player, "axl_bullet_charged", 0, 0, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.CopyShot;

		xScale = 0.75f;
		yScale = 0.75f;

		reflectable = true;
		maxTime = 0.5f;

		/*
		if (player?.character?.isWhiteAxl() == true)
		{
			damager.flinch = Global.miniFlinch;
		}
		*/

		if (chargeLevel == 2) {
			damager.damage = 3;
			speed *= 1.5f;
			maxTime /= 1.5f;
			xScale = 1f;
			yScale = 1f;
		}
		if (chargeLevel >= 3) {
			damager.damage = 4;
			speed *= 2f;
			maxTime /= 2f;
			xScale = 1.25f;
			yScale = 1.25f;
		}
		/*
		if (chargeLevel == 4)
		{
			damager.damage = 5;
			speed *= 2.5f;
			maxTime /= 2.5f;
			xScale = 1.5f;
			yScale = 1.5f;
		}
		*/
		vel.x = bulletDir.x * speed;
		vel.y = bulletDir.y * speed;
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		destroySelf();
	}
}

public class CopyShotDamageEvent {
	public Character character;
	public float time;
	public CopyShotDamageEvent(Character character) {
		this.character = character;
		time = Global.time;
	}
}
