using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SFML.Graphics;
namespace MMXOnline;

public enum VileCutterType {
	None = -1,
	QuickHomesick,
	ParasiteSword,
	MaroonedTomahawk
}

public class VileCutter : Weapon {
	public float vileAmmoUsage;
	public VileCutter() : base() {
		index = (int)WeaponIds.Napalm;
		weaponBarBaseIndex = 0;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 30;
		vileWeight = 0;
	}
}
public class QuickHomesick : VileCutter {
	public static QuickHomesick netWeapon = new();
	public QuickHomesick() : base() {
		type = (int)VileCutterType.QuickHomesick;
		displayName = "Quick Homesick";
		vileAmmoUsage = 8;
		killFeedIndex = 114;
		vileWeight = 3;
		fireRate = 60;
		ammousage = vileAmmoUsage;
		damage = "2";
		hitcooldown = "0.5";
		effect = "Can carry items.";
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < vileAmmoUsage) return;
		vile.changeState(new CutterAttacks(this), true);
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		var poi = vava.sprite.getCurrentFrame().POIs[0];
		poi.x *= vava.xDir;
		var player = vava.player;
		int xDir = vava.xDir;
		Point muzzlePos = vava.pos.add(poi).addxy(14*xDir,2);
		new VileParasiteSword(
			muzzlePos, xDir, vava, player,
			player.getNextActorNetId(), rpc: true
		);
	}
}
public class ParasiteSword : VileCutter {
	public static ParasiteSword netWeapon = new();
	public ParasiteSword() : base() {
		type = (int)VileCutterType.ParasiteSword;
		displayName = "Parasite Sword";
		vileAmmoUsage = 8;
		killFeedIndex = 115;
		vileWeight = 3;
		fireRate = 60;
		ammousage = vileAmmoUsage;
		damage = "2";
		hitcooldown = "0.5";
		effect = "Won't Destroy on hit.";
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < vileAmmoUsage) return;
		vile.changeState(new CutterAttacks(this), true);
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		var poi = vava.sprite.getCurrentFrame().POIs[0];
		poi.x *= vava.xDir;
		var player = vava.player;
		int xDir = vava.xDir;
		Point muzzlePos = vava.pos.add(poi).addxy(14*xDir,2);
		new VileParasiteSword(
			muzzlePos, xDir, vava, player,
			player.getNextActorNetId(), rpc: true
		);
	}
}
public class MaroonedTomahawk : VileCutter {
	public static MaroonedTomahawk netWeapon = new();
	public MaroonedTomahawk() : base() {
		type = (int)VileCutterType.MaroonedTomahawk;
		displayName = "Marooned Tomahawk";
		vileAmmoUsage = 16;
		killFeedIndex = 116;
		vileWeight = 3;
		ammousage = vileAmmoUsage;
		fireRate = 60 * 2;
		damage = "1";
		hitcooldown = "0.33";
		effect = "Won't Destroy on hit.";
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < vileAmmoUsage) return;
		vile.changeState(new CutterAttacks(this), true);
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		var poi = vava.sprite.getCurrentFrame().POIs[0];
		poi.x *= vava.xDir;
		var player = vava.player;
		int xDir = vava.xDir;
		Point muzzlePos = vava.pos.add(poi).addxy(14 * xDir, 2);
		new VileParasiteSword(
			muzzlePos, xDir, vava, player,
			player.getNextActorNetId(), rpc: true
		);
	}
}
#region States
public class CutterAttacks : VileState {
	public bool shot;
	public int shootFrame = 0;
	public VileCutter weapon;
	public CutterAttacks(VileCutter weapon) : base("idle_shoot") {
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
		if (character.frameIndex >= shootFrame && !shot && vile.tryUseVileAmmo(weapon.vileAmmoUsage)) {
			shot = true;
			weapon.shoot(vile, []);
			vile.setVileShootTime(weapon);
			vile.playSound("frontrunner", sendRpc: true);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.turnToInput(player.input, player);
		if (!character.grounded) {
			sprite = "air_bomb_attack";
			character.changeSpriteFromName(sprite, true);
			character.useGravity = false;
			character.vel = new Point();
		}
	}
	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}
#endregion
#region Projectiles
public class VileParasiteSword : Projectile {
	float soundCooldown;
	public VileParasiteSword(
		Point pos, int xDir,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "cutter_ps", netId, player
	) {
		weapon = ParasiteSword.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 30;
		vel = new Point(250 * xDir, -250);
		maxTime = 1f;
		projId = (int)ProjIds.ParasiteSword;
		destroyOnHit = false;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new VileParasiteSword(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		soundCooldown -= Global.spf;
		if (soundCooldown <= 0) {
			soundCooldown = 0.3f;
			playSound("cutter", sendRpc: true);
		}
		if (xScale < 2) {
			xScale += Global.spf * 2;
			yScale += Global.spf * 2;			
		}
	}
}
public class VileMaroonedTomahawk : Projectile {
	float soundCooldown;
	public VileMaroonedTomahawk(
		Point pos, int xDir,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "cutter_mt", netId, player
	) {
		weapon = MaroonedTomahawk.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 20;
		vel = new Point(250*xDir, -125);
		maxTime = 3f;
		projId = (int)ProjIds.MaroonedTomahawk;
		destroyOnHit = false;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new VileMaroonedTomahawk(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		soundCooldown -= Global.spf;
		if (soundCooldown <= 0) {
			soundCooldown = 0.3f;
			playSound("cutter", sendRpc: true);
		}
		if (time > 6f/60f) {
			stopMoving();
		}
	}
}
public class VileQuickHomesick : Projectile {
	public float angleDist = 0;
	public float turnDir = 1;
	public Pickup? pickup;
	public float angle2;

	public float maxSpeed = 350;
	public float returnTime = 0.15f;
	public float turnSpeed = 300;
	public float maxAngleDist = 180;
	public float soundCooldown;
	public VileQuickHomesick(
		Point pos, int xDir,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "cutter_qh", netId, player
	) {
		weapon = QuickHomesick.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 30;
		vel = new Point(350 * xDir, 50);
		maxTime = 3f;
		projId = (int)ProjIds.QuickHomesick;
		angle2 = 0;
		if (xDir == -1) angle2 = -180;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new VileQuickHomesick(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;
		if (other.gameObject is Pickup && pickup == null) {
			pickup = other.gameObject as Pickup;
			if (!pickup?.ownedByLocalPlayer == true) {
				pickup?.takeOwnership();
				RPC.clearOwnership.sendRpc(pickup?.netId);
			}
		}

		if (time > returnTime && other.gameObject is Character character && character.player == damager.owner) {
			if (pickup != null) {
				pickup.changePos(character.getCenterPos());
			}
			destroySelf();
			character.addAmmo(8);
		}
	}
	public override void onDestroy() {
		base.onDestroy();
		if (pickup != null) {
			pickup.useGravity = true;
			pickup.collider.isTrigger = false;
		}
	}

	public override void update() {
		base.update();

		if (!destroyed && pickup != null) {
			pickup.collider.isTrigger = true;
			pickup.useGravity = false;
			pickup.changePos(pos);
		}

		soundCooldown -= Global.spf;
		if (soundCooldown <= 0) {
			soundCooldown = 0.3f;
			playSound("cutter", sendRpc: true);
		}


		if (time > returnTime) {
			if (angleDist < maxAngleDist) {
				var angInc = (-xDir * turnDir) * Global.spf * turnSpeed;
				angle2 += angInc;
				angleDist += MathF.Abs(angInc);
				vel.x = Helpers.cosd(angle2) * maxSpeed;
				vel.y = Helpers.sind(angle2) * maxSpeed;
			} else if (damager.owner.character != null) {
				var dTo = pos.directionTo(damager.owner.character.getCenterPos()).normalize();
				var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
				destAngle = Helpers.to360(destAngle);
				angle2 = Helpers.lerpAngle(angle2, destAngle, Global.spf * 10);
				vel.x = Helpers.cosd(angle2) * maxSpeed;
				vel.y = Helpers.sind(angle2) * maxSpeed;
			} else {
				destroySelf();
			}
		}
	}
}
#endregion
/*
public class VileCutter : Weapon {
	public float vileAmmoUsage;
	public static VileCutter netWeaponQH = new VileCutter(VileCutterType.QuickHomesick);
	public static VileCutter netWeaponPS = new VileCutter(VileCutterType.ParasiteSword);
	public static VileCutter netWeaponMT = new VileCutter(VileCutterType.MaroonedTomahawk);
	public VileCutter(VileCutterType vileCutterType) : base() {
		index = (int)WeaponIds.VileCutter;
		type = (int)vileCutterType;
		fireRate = 60;

		if (vileCutterType == VileCutterType.None) {
			displayName = "None";
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmoUsage = 0;
			fireRate = 0;
			vileWeight = 0;
		}
		if (vileCutterType == VileCutterType.QuickHomesick) {
			displayName = "Quick Homesick";
			vileAmmoUsage = 8;
			description = new string[] { "This cutter travels in an arc like a", "boomerang. Use it to pick up items!" };
			killFeedIndex = 114;
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "2";
			hitcooldown = "0.5";
			effect = "Can carry items.";
		} else if (vileCutterType == VileCutterType.ParasiteSword) {
			displayName = "Parasite Sword";
			vileAmmoUsage = 8;
			description = new string[] { "Fires cutters that grow as they fly", "and can pierce enemies." };
			killFeedIndex = 115;
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "2";
			hitcooldown = "0.5";
			effect = "Won't Destroy on hit.";
		} else if (vileCutterType == VileCutterType.MaroonedTomahawk) {
			displayName = "Marooned Tomahawk";
			vileAmmoUsage = 16;
			description = new string[] { "This long-lasting weapon spins", "in place and goes through objects." };
			killFeedIndex = 116;
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			fireRate = 60 * 2;
			damage = "1";
			hitcooldown = "0.33";
			effect = "Won't Destroy on hit.";
		}
	}

	public override float getAmmoUsage(int chargeLevel) {
		return vileAmmoUsage;
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (vile.cutterWeapon.type == (int)VileCutterType.None) return;
		if (shootCooldown > 0) return;
		if (vile.tryUseVileAmmo(vileAmmoUsage)) {
			vile.setVileShootTime(this);
			if (!vile.grounded) {
				vile.changeState(new CutterAttackState(grounded: false), true);
			} else {
				vile.changeState(new CutterAttackState(grounded: true), true);
			}
		}
	}
}
public class CutterAttackState : VileState {
	public CutterAttackState(bool grounded) : base(getSprite(grounded)) {
		useDashJumpSpeed = true;
		airMove = true;
		canJump = true;
		canStopJump = true;
		airSprite = "cannon_air";
		landSprite = "idle_shoot";
	}
	public static string getSprite(bool grounded) {
		return grounded ? "idle_shoot" : "cannon_air";
	}

	public override void update() {
		base.update();
		groundCodeWithMove();
		if (character.sprite.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public void shootLogic(Vile vile) {
		if (vile.sprite.getCurrentFrame().POIs.IsNullOrEmpty()) return;
		Point shootVel = vile.getVileShootVel(true);
		var poi = vile.sprite.getCurrentFrame().POIs[0];
		poi.x *= vile.xDir;
		var player = vile.player;
		int xDir = vile.xDir;
		Point muzzlePos = vile.pos.add(poi).addxy(14*xDir,2);
		vile.playSound("frontrunner", sendRpc: true);
		if (vile.cutterWeapon.type == ((int)VileCutterType.ParasiteSword)) {
			new VileParasiteSword(
				muzzlePos, xDir, vile, player,
				player.getNextActorNetId(), rpc: true
			);
		}
		else if (vile.cutterWeapon.type == ((int)VileCutterType.MaroonedTomahawk)) {
			new VileMaroonedTomahawk(
				muzzlePos, xDir, vile, player,
				player.getNextActorNetId(), rpc: true
			);
		}
		else if (vile.cutterWeapon.type == ((int)VileCutterType.QuickHomesick)) {
			new VileQuickHomesick(
				muzzlePos, xDir, vile, player,
				player.getNextActorNetId(), rpc: true
			);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)) {
			exitOnAirborne = true;
		}
		shootLogic(vile);
	}
}
*/