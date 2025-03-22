using System.Collections.Generic;
using System.Linq;
using System;
namespace MMXOnline;

public class ParasiticBomb : Weapon {
	public static ParasiticBomb netWeapon = new();
	public static float carryRange = 120;
	public static float beeRange = 120;

	public ParasiticBomb() : base() {
		shootSounds = new string[] { "", "", "", "" };
		fireRate = 60;
		switchCooldown = 45;
		index = (int)WeaponIds.ParasiticBomb;
		weaponBarBaseIndex = 18;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 18;
		killFeedIndex = 41;
		weaknessIndex = (int)WeaponIds.GravityWell;
		damage = "4/4";
		effect = "Slows enemies and slams them if detonated \nunless mashed off. Homing bees.";
		hitcooldown = "0/0.5";
		Flinch = "26-CarryT/26";
		FlinchCD = "iwish";
		maxAmmo = 16;
		ammo = maxAmmo;
	}
	
	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) { return 0.5f; }
		return 1;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		if (!base.canShoot(chargeLevel, player) || player.character is not MegamanX mmx) return false;

		return mmx.chargedParasiticBomb == null;
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		if (chargeLevel < 3) {
			character.playSound("busterX3");
			new ParasiticBombProj(pos, xDir, mmx, player, player.getNextActorNetId(), true);
		} else {
			if (character.ownedByLocalPlayer) {
				if (mmx.chargedParasiticBomb != null) {
					mmx.chargedParasiticBomb.destroy();
				}
				mmx.chargedParasiticBomb = new BeeSwarm(mmx);
			}
		}
	}
}

public class ParasiticBombProj : Projectile {
	public Character? host;
	public ParasiticBombProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "parasitebomb", netId, player	
	) {
		weapon = ParasiticBomb.netWeapon;
		vel = new Point(200 * xDir, 0);
		maxTime = 0.6f;
		projId = (int)ProjIds.ParasiticBomb;
		destroyOnHit = true;
		shouldShieldBlock = true;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ParasiticBombProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void render(float x, float y) {
		base.render(x, y);
		Global.sprites["parasitebomb_light"].draw(MathInt.Round(Global.frameCount * 0.25f) % 4, pos.x + x, pos.y + y, 1, 1, null, 1, 1, 1, zIndex);
	}
}

public class ParasiteCarry : CharState {
	bool flinch;
	bool isDone;
	Character otherChar;
	float moveAmount;
	float maxMoveAmount;
	public ParasiteCarry(Character otherChar, bool flinch) : 
	base(flinch ? "hurt" : "fall", flinch ? "" : "fall_shoot", flinch ? "" : "fall_attack"
	) {
		this.flinch = flinch;
		this.otherChar = otherChar;
	}

	public override bool canEnter(Character character) {
		if (!character.ownedByLocalPlayer) return false;
		if (!base.canEnter(character)) return false;
		if (character.isStatusImmune()) return false;
		if (character.isInvulnerable()) return false;
		if (character.charState.superArmor) return false;
		return !character.charState.invincible;
	}

	public override bool canExit(Character character, CharState newState) {
		if (newState is Hurt || newState is Die) return true;
		return isDone;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.grounded = false;
		character.vel.y = 0;
		maxMoveAmount = character.getCenterPos().distanceTo(otherChar.getCenterPos()) * 1.5f;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}

	public override void update() {
		base.update();

		if (!character.hasParasite || character.parasiteDamager == null) {
			isDone = true;
			character.changeToIdleOrFall();
			return;
		}

		Point amount = character.getCenterPos().directionToNorm(otherChar.getCenterPos()).times(250);

		/*
		var hit = Global.level.checkCollisionActor(character, amount.x * Global.spf, amount.y * Global.spf);
		if (hit?.gameObject is Wall)
		{
			character.parasiteDamager.applyDamage(character, player.weapon is FrostShield, new ParasiticBomb(), otherChar, (int)ProjIds.ParasiticBombExplode, overrideDamage: 4, overrideFlinch: Global.defFlinch);
			character.removeParasite(false, true);
			return;
		}
		*/

		character.move(amount);

		moveAmount += amount.magnitude * Global.spf;
		if (character.getCenterPos().distanceTo(otherChar.getCenterPos()) < 5) {
			var pd = character.parasiteDamager;
			pd.applyDamage(character, player.weapon is FrostShield, new ParasiticBomb(), otherChar, (int)ProjIds.ParasiticBombExplode, overrideDamage: 4, overrideFlinch: Global.defFlinch);
			pd.applyDamage(otherChar, player.weapon is FrostShield, new ParasiticBomb(), character, (int)ProjIds.ParasiticBombExplode, overrideDamage: 4, overrideFlinch: Global.defFlinch);
			character.removeParasite(false, true);
			return;
		} else if (moveAmount > maxMoveAmount) {
			character.removeParasite(false, false);
			return;
		}
	}
}

public class BeeSwarm {
	public MegamanX mmx;
	public List<BeeCursorAnim> beeCursors = new List<BeeCursorAnim>();
	int currentIndex;
	float currentTime = 0f;
	const float beeCooldown = 1f;

	public BeeSwarm(MegamanX mmx) {
		this.mmx = mmx;

		beeCursors = new List<BeeCursorAnim>() {
			new BeeCursorAnim(getCursorStartPos(0), mmx),
			new BeeCursorAnim(getCursorStartPos(1), mmx),
			new BeeCursorAnim(getCursorStartPos(2), mmx),
			new BeeCursorAnim(getCursorStartPos(3), mmx),
		};
	}

	public Point getCursorStartPos(int index) {
		Point cPos = mmx.getCenterPos();
		if (index == 0) return cPos.addxy(-15, -17);
		else if (index == 1) return cPos.addxy(15, -17);
		else if (index == 2) return cPos.addxy(-15, 17);
		else return cPos.addxy(15, 17);
	}

	public Actor? getAvailableTarget() {
		Point centerPos = mmx.getCenterPos();
		var targets = Global.level.getTargets(centerPos, mmx.player.alliance, true, ParasiticBomb.beeRange);

		foreach (var target in targets) {
			if (beeCursors.Any(b => b.target == target)) {
				continue;
			}
			return target;
		}

		return null;
	}

	public void update() {
		currentTime -= Global.spf;
		if (currentTime <= 0) {
			var target = getAvailableTarget();
			if (target != null) {
				beeCursors[currentIndex].target = target;
				currentTime = beeCooldown;
				currentIndex++;
				if (currentIndex > 3) currentIndex = 0;
			}
		}

		for (int i = 0; i < beeCursors.Count; i++) {
			if (beeCursors[i].state < 2) {
				beeCursors[i].pos = getCursorStartPos(i);
			}
			if (beeCursors[i].state == 4) {
				beeCursors[i] = new BeeCursorAnim(getCursorStartPos(i), mmx);
			}
		}

		if (shouldDestroy()) {
			destroy();
		}
	}

	public void reset(bool isMiniFlinch) {
		currentTime = 1;
		if (!isMiniFlinch) {
			foreach (var beeCursor in beeCursors) {
				beeCursor.reset();
			}
		}
	}

	public bool shouldDestroy() {
		if (mmx.player.weapon is not ParasiticBomb) return true;
		var pb = mmx.player.weapon as ParasiticBomb;
		if (pb?.ammo <= 0) return true;
		return false;
	}

	public void destroy() {
		foreach (var beeCursor in beeCursors) {
			beeCursor.destroySelf();
		}
		beeCursors.Clear();
		mmx.chargedParasiticBomb = null;
	}
}

public class BeeCursorAnim : Anim {
	public int state = 0;
	MegamanX? character;
	Player player;
	public Actor? target;
	public BeeCursorAnim(Point pos, Character character)
		: base(pos, "parasite_cursor_start", 1, character.player.getNextActorNetId(), false, true, character.ownedByLocalPlayer) {
		this.character = character as MegamanX;
		player = character.player;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (state == 0) {
			if (sprite.name == "parasite_cursor_start" && sprite.isAnimOver()) {
				changeSprite("parasite_cursor", true);
				state = 1;
				time = 0;
			}
		} else if (state == 1) {
			if (target != null) {
				state = 2;
			}
		} else if (state == 2) {
			if (target!.destroyed) {
				state = 3;
				return;	
			}
			move(pos.directionToNorm(target.getCenterPos()).times(350));
			if (pos.distanceTo(target.getCenterPos()) < 5) {
				state = 3;
				changeSprite("parasite_cursor_lockon", true);
			}		
		} else if (state == 3) {
			pos = target!.getCenterPos();
			
			if (isAnimOver()) {
				state = 4;
				destroySelf();
				if (!target!.destroyed) {
					if (character != null) {
						//character.chargeTime = character.charge3Time;
						//character.shoot(character.getChargeLevel());
						if (character.charState.attackCtrl) {
							character.setShootAnim();
							character.shootAnimTime = Character.DefaultShootAnimTime;
						}
						if (character.currentWeapon is ParasiticBomb) {
							character.currentWeapon.addAmmo(-character.currentWeapon.getAmmoUsage(3), player);
						}
						character.chargeTime = 0;
						new ParasiticBombProjCharged(character.getShootPos(), character.pos.x - target.getCenterPos().x < 0 ? 1 : -1,
						character, character.player, character.player.getNextActorNetId(), target, rpc: true);
					}	
				}
			}
		}
	}

	public void reset() {
		state = 0;
		changeSpriteIfDifferent("parasite_cursor_start", true);
		target = null;
	}
}

public class ParasiticBombProjCharged : Projectile, IDamagable {
	public Actor host;
	public Point lastMoveAmount;
	const float maxSpeed = 150;
	public ParasiticBombProjCharged(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, Actor host, bool rpc = false
	) : base(
		pos, xDir, owner, "parasitebomb_bee", netId, player	
	) {
		weapon = ParasiticBomb.netWeapon;
		damager.damage = 4;
		damager.hitCooldown = 30;
		damager.flinch = Global.defFlinch;
		vel = new Point(0 * xDir, 0);
		this.host = host;
		fadeSprite = "explosion";
		fadeSound = "explosionX3";
		maxTime = 3f;
		projId = (int)ProjIds.ParasiticBombCharged;
		destroyOnHit = true;
		shouldShieldBlock = true;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ParasiticBombProjCharged(
			args.pos, args.xDir, args.owner, args.player, args.netId, null!
		);
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (!host.destroyed) {
			Point amount = pos.directionToNorm(host.getCenterPos()).times(150);
			vel = Point.lerp(vel, amount, Global.spf * 4);
			if (vel.magnitude > maxSpeed) vel = vel.normalize().times(maxSpeed);
		} else {
		}
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (damage > 0) {
			destroySelf();
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return damager.owner.alliance != damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
	}

	public bool isPlayableDamagable() { return false; }
}
