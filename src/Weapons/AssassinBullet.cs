using System.Collections.Generic;
using System.Linq;
using SFML.Audio;
namespace MMXOnline;

public class AssassinBullet : AxlWeapon {
	public AssassinBullet() : base(0) {
		sprite = "axl_arm_pistol";
		flashSprite = "axl_pistol_flash_charged";
		chargedFlashSprite = "axl_pistol_flash_charged";
		shootSounds = new string[] { "assassinate", "assassinate", "assassinate", "assassinate" };
		index = (int)WeaponIds.AssassinBullet;
		weaponBarBaseIndex = 28;
		weaponBarIndex = 28;
		weaponSlotIndex = 47;
		killFeedIndex = 61;
		drawAmmo = false;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void axlGetProjectile(Weapon weapon, Point bulletPos, int xDir, Player player, float angle, IDamagable target, Character headshotTarget, Point cursorPos, int chargeLevel, ushort netId) {
		if (player.assassinHitPos == null) {
			// URGENT TODO
			// player.assassinHitPos = player.character.getFirstHitPos(AssassinBulletProj.range);
		}
		var bullet = new AssassinBulletProj(weapon, bulletPos, player.assassinHitPos.hitPos, xDir, player, target, headshotTarget, netId);
		bullet.applyDamage(player.assassinHitPos.hitGos.ElementAtOrDefault(0), player.assassinHitPos.isHeadshot);

		if (player.ownedByLocalPlayer) {
			AssassinBulletTrailAnim trail = new AssassinBulletTrailAnim(bulletPos, bullet);
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}
}

public class AssassinBulletProj : Projectile {
	public Player player;
	public Character headshotChar;
	public IDamagable target;
	public Point destroyPos;
	public float distTraveled;

	int frames;
	float dist;
	float maxDist;
	Point hitPos;
	public const float range = 150;
	public AssassinBulletProj(Weapon weapon, Point pos, Point hitPos, int xDir, Player player, IDamagable target, Character headshotChar, ushort netProjId) :
		base(weapon, pos, xDir, 1000, 8, player, "assassin_bullet_proj", 0, 0.5f, netProjId, player.ownedByLocalPlayer) {
		this.target = target;

		Point bulletDir = pos.directionToNorm(hitPos);

		vel.x = bulletDir.x * speed;
		vel.y = bulletDir.y * speed;
		this.player = player;
		this.headshotChar = headshotChar;

		fadeSprite = "axl_bullet_fade";
		projId = (int)ProjIds.AssassinBullet;

		if (player?.character?.isQuickAssassinate == true) {
			projId = (int)ProjIds.AssassinBulletQuick;
		}

		this.xDir = xDir;
		this.angle = bulletDir.angle;
		visible = false;
		reflectable = true;

		this.hitPos = hitPos;
		maxTime = float.MaxValue;
		maxDist = pos.distanceTo(hitPos);
	}

	public override void update() {
		base.update();

		angle = deltaPos.angle;
		if (!ownedByLocalPlayer) return;
		if (destroyed) return;

		// Zero reflect, rolling shield, ice shield
		frames++;
		if (frames > 1) {
			visible = true;
		}

		dist += deltaPos.magnitude;
		if (pos.distanceTo(hitPos) < speed * Global.spf || dist >= maxDist) {
			changePos(hitPos);
			destroySelf();
			return;
		}
	}

	public void applyDamage(IDamagable damagable, bool weakness) {
		float overrideDamage = weakness ? (damager.damage * Damager.headshotModifier) : damager.damage;
		if (weapon is AssassinBullet && weakness) {
			overrideDamage = Damager.ohkoDamage;
		}
		//DevConsole.log("Weakness: " + weakness.ToString() + ",bd:" + damager.damage.ToString() + ",");
		damager.applyDamage(damagable, false, weapon, this, projId, overrideDamage: overrideDamage);

		if (weakness) {
			//(damagable as Character).addDamageText("Headshot!", false);
			playSound("hurt");
		}
	}
}

public class Assassinate : CharState {
	public float time;
	bool fired;
	public Axl? axl;

	public Assassinate(bool isGrounded) : base(isGrounded ? "idle" : "fall", "shoot", "attack") {
		superArmor = true;
	}

	public override void update() {
		base.update();
		time += Global.spf;
		if (axl != null && !Options.main.useMouseAim && Options.main.lockOnSound && axl.assassinCursorPos != null) {
			axl.axlCursorPos = axl.assassinCursorPos.Value;
		}
		if (!fired) {
			fired = true;
			//player.character.axlCursorTarget = null;
			//player.character.axlHeadshotTarget = null;
			//player.character.updateAxlAim();
			(new AssassinBullet()).axlShoot(player, AxlBulletType.Assassin);
		}
		if (time > 0.5f) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel = new Point();
		axl = character as Axl;
		if (axl != null) {
			character.xDir = (character.pos.x > axl.axlGenericCursorWorldPos.x ? -1 : 1);
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}
public class AssassinBulletChar : Weapon {
	public static AssassinBulletChar netWeapon = new();

	public AssassinBulletChar() : base() {
		shootSounds = new string[] { "assassinate", "assassinate", "assassinate", "assassinate" };
		index = (int)WeaponIds.AssassinBullet;
		weaponBarBaseIndex = 26;
		weaponBarIndex = 26;
		weaponSlotIndex = 47;
		killFeedIndex = 61;

		drawAmmo = false;
		drawCooldown = false;
	}
	public override bool canShoot(int chargeLevel, Player player) {
		if (!base.canShoot(chargeLevel, player)) return false;
		if (chargeLevel >= 3) {
			return true;
		}
		return false;
	}
}
public class AssassinationProj : Projectile {
	bool once;
	float time1;
	public AssassinationProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "axl_bullet", netId, player
	) {
		weapon = AssassinBulletChar.netWeapon;
		damager.damage = 8;
		damager.hitCooldown = 0;
		damager.flinch = 0;
		vel = new Point(600 * xDir, 0);
		fadeSprite = "axl_bullet_fade";
		fadeOnAutoDestroy = true;		
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.AssassinBulletEX;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new AssassinationProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		destroySelf();
	}
	public override void update() {
		time1 += Global.spf;
		if (ownedByLocalPlayer && getHeadshotVictim(owner, out IDamagable? victim, out Point? hitPoint)) {
			if (hitPoint != null) changePos(hitPoint.Value);
			if (time1 >= 0.35f) {
				damager.applyDamage(victim, false, weapon, this, projId, 16, Global.defFlinch);
			} else if (time1 < 0.35f) damager.applyDamage(victim, false, weapon, this, projId, overrideDamage: Damager.ohkoDamage);
			damager.damage = 0;
			playSound("hurt");
			destroySelf();
			return;
		}
		if (!once) {
			once = true;
			playSound("assassinate", false, true);
		}
		base.update();
	}
}

public class AssassinateChar : CharState {
	bool fired;
	public AssassinateChar() : base("idle") {
		superArmor = true;
	}
	public override void update() {
		base.update();
		character.frameIndex = 0;
		if (!fired) {
			int xDir = character.xDir;
			fired = true;
			Global.level.delayedActions.Add(new DelayedAction(() => {
				new AssassinationProj(
					character.getCenterPos().addxy(16 * xDir, -5), xDir,
					character, player, player.getNextActorNetId(), true
				);
			}, 0.55f));
		}
		if (stateTime >= 0.75f) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.playSound("counters_usp_clipin", true, true);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}
