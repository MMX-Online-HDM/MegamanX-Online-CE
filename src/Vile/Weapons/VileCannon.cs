using System;
using System.Linq;
using System.Collections.Generic;
namespace MMXOnline;

public enum VileCannonType {
	None = -1,
	FrontRunner,
	FatBoy,
	LongshotGizmo
}

public class VileCannon : Weapon {
	public float vileAmmoUsage;
	public VileCannon() : base() {
		index = (int)WeaponIds.FrontRunner;
		weaponBarBaseIndex = 56;
		weaponBarIndex = 56;
		killFeedIndex = 56;
		weaponSlotIndex = 43;
		isStream = true;
	}
	public override void vileShoot(Vile vile) {
		if (shootCooldown > 0 || vile.energy.ammo < vileAmmoUsage || vile.missileCannonCooldown > 0) {
			return;
		}
		if (vile.charState is Crouch) {
			shoot(vile, []);
			return;
		}
		vile.changeState(new CannonAttack(this), true);
	}
}

public class FrontRunner : VileCannon {
	public static FrontRunner netWeapon = new();
	public FrontRunner() : base() {
		type = (int)VileCannonType.FrontRunner;
		index = (int)WeaponIds.FrontRunner;
		fireRate = 45;
		vileAmmoUsage = 8;
		ammousage = vileAmmoUsage;
		damage = "3";
		displayName = "Front Runner";
		vileWeight = 2;
		effect = "None.";
	}

	public override void shoot(Character character, int[] args) {
		if (character is not Vile vile) return;
		Point shootVel = vile.getVileShootVel(true);
		Point shootPos = vile.setCannonAim(new Point(shootVel.x, shootVel.y));

		if (vile.getShootXDir() == -1) {
			shootVel.x *= -1;
		}
		new FrontRunnerProj(
			shootPos, MathF.Round(shootVel.byteAngle), vile,
			vile.player.getNextActorNetId(), sendRpc: true
		);

		vile.setVileShootTime(this);
		vile.playSound("frontrunner", sendRpc: true);
		vile.tryUseVileAmmo(vileAmmoUsage);
	}
}

public class FatBoy : VileCannon {
	public static FatBoy netWeapon = new();
	public FatBoy() : base() {
		type = (int)VileCannonType.FatBoy;
		fireRate = 45;
		damage = "4";
		flinch = "26";
		vileAmmoUsage = 24;
		ammousage = vileAmmoUsage;
		displayName = "Fat Boy";
		killFeedIndex = 90;
		weaponSlotIndex = 61;
		vileWeight = 3;
		effect = "None.";
	}

	public override void shoot(Character character, int[] args) {
		if (character is not Vile vile) { return; }
		Point shootVel = vile.getVileShootVel(true);
		Point shootPos = vile.setCannonAim(new Point(shootVel.x, shootVel.y));
		if (vile.getShootXDir() == -1) {
			shootVel.x *= -1;
		}
		new FatBoyProj(
			shootPos, MathF.Round(shootVel.byteAngle), vile,
			vile.player.getNextActorNetId(), sendRpc: true
		);

		vile.setVileShootTime(this);
		vile.playSound("frontrunner", sendRpc: true);
		vile.tryUseVileAmmo(vileAmmoUsage);
	}
}

public class LongShotGizmo : VileCannon {
	public static LongShotGizmo netWeapon = new();
	public LongShotGizmo() : base() {
		type = (int)VileCannonType.LongshotGizmo;
		fireRate = 6;
		damage = "1";
		vileAmmoUsage = 4;
		ammousage = vileAmmoUsage;
		displayName = "Longshot Gizmo";
		killFeedIndex = 91;
		weaponSlotIndex = 62;
		vileWeight = 4;
		effect = "Burst of 5 shots.";
	}
	public override void vileShoot(Vile vile) {
		if (shootCooldown > 0 || vile.energy.ammo < vileAmmoUsage || vile.missileCannonCooldown > 0) {
			return;
		}
		vile.changeState(new CannonAttack(this), true);
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vile) { return; }
		Point shootVel = vile.getVileShootVel(true);
		Point shootPos = vile.setCannonAim(new Point(shootVel.x, shootVel.y));
		if (vile.getShootXDir() == -1) {
			shootVel.x *= -1;
		}
		new LongshotGizmoProj(
			shootPos, MathF.Round(shootVel.byteAngle), vile,
			vile.player.getNextActorNetId(), sendRpc: true
		);

		vile.setVileShootTime(this);
		vile.playSound("frontrunner", sendRpc: true);
		vile.tryUseVileAmmo(vileAmmoUsage);
	}
}

public class CannonAttack : VileState {
	public bool shot;
	public int shootFrame = 0;
	public VileCannon weapon;
	public int loopNum;
	public bool lockAir => Options.main.lockInAirCannon;
	public float shootTime;
	
	public CannonAttack(VileCannon weapon) : base("idle_shoot") {
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
		character.turnToInput(player.input, player);

		if (vile.energy.ammo < weapon.vileAmmoUsage && !lockAir && !character.grounded && character.isAnimOver()) {
			character.changeToCrouchOrFall();
			return;
		}

		if (character.frameIndex >= shootFrame && !shot) {
			shot = true;
			weapon.shoot(vile, []);
		}

		shootTime += Global.speedMul;
		if (weapon is LongShotGizmo) {
			if (shootTime == 6) {
				shootTime = 0;
				loopNum++;
				weapon.shoot(vile, []);
			}
			if (loopNum >= 4) {
				character.changeToIdleOrFall();
			}
			if (vile.energy.ammo < weapon.vileAmmoUsage) {
				character.changeToIdleOrFall();
				return;
            }
        }

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (!character.grounded) {
			sprite = "cannon_air";
			character.changeSpriteFromName(sprite, true);
			if (lockAir) {
				character.useGravity = false;
				character.stopMoving();
				airMove = false;
				canStopJump = false;
				canJump = false;
				character.useGravity = false;
			}
		}
		if (weapon is LongShotGizmo) {
			airMove = false;	
			if (character.grounded) sprite = "idle_gizmo";
			else sprite = "cannon_gizmo_air";
			character.changeSpriteFromName(sprite, true);
			vile.isShootingGizmo = true;
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
		if (weapon is LongShotGizmo) {
			vile.isShootingGizmo = false;
			weapon.fireRate = 30;
        }
	}
}

public class FrontRunnerProj : Projectile {
	public FrontRunnerProj(
		Point pos, float byteAngle, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, 1, owner, "vile_mk2_proj", netId, altPlayer
	) {
		weapon = FrontRunner.netWeapon;
		projId = (int)ProjIds.FrontRunner;
		maxTime = 0.5f;
		destroyOnHit = true;
		fadeSprite = "vile_mk2_proj_fade";
		fadeOnAutoDestroy = true;
		damager.damage = 3;
		byteAngle = Helpers.to256(byteAngle);
		this.byteAngle = byteAngle;
		vel = 5 * 60 * Point.createFromByteAngle(byteAngle);

		if (sendRpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new FrontRunnerProj(
			args.pos, args.byteAngle, args.owner, args.netId, altPlayer: args.player
		);
	}
}

public class FatBoyProj : Projectile {
	public FatBoyProj(
		Point pos, float byteAngle, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, 1, owner, "vile_mk2_fb_proj", netId, altPlayer
	) {
		weapon = FatBoy.netWeapon;
		projId = (int)ProjIds.FatBoy;
		fadeSprite = "vile_mk2_fb_proj_fade";
		fadeOnAutoDestroy = true;
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		maxTime = 0.35f;
		byteAngle = Helpers.to256(byteAngle);
		this.byteAngle = byteAngle;
		vel = 5 * 60 * Point.createFromByteAngle(byteAngle);

		if (sendRpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new FatBoyProj(
			args.pos, args.byteAngle, args.owner, args.netId, altPlayer: args.player
		);
	}
}

public class LongshotGizmoProj : Projectile {
	public LongshotGizmoProj(
		Point pos, float byteAngle, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, 1, owner, "vile_mk2_lg_proj", netId, altPlayer
	) {
		weapon = LongShotGizmo.netWeapon;
		xScale = xDir;
		fadeSprite = "vile_mk2_lg_proj_fade";
		fadeOnAutoDestroy = true;
		damager.damage = 1;
		projId = (int)ProjIds.LongshotGizmo;
		maxTime = 30 / 60f;
		byteAngle = Helpers.to256(byteAngle);
		this.byteAngle = byteAngle;
		vel = 5 * 60 * Point.createFromByteAngle(byteAngle);
		if (sendRpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new LongshotGizmoProj(
			args.pos, args.byteAngle, args.owner, args.netId, altPlayer: args.player
		);
	}
}

#region OldStuff
/*
public class VileCannon : Weapon {
	public string projSprite = "";
	public string fadeSprite = "";
	public float vileAmmoUsage;
	public static VileCannon netWeaponFR = new VileCannon(VileCannonType.FrontRunner);
	public static VileCannon netWeaponLG = new VileCannon(VileCannonType.LongshotGizmo);
	public static VileCannon netWeaponFB = new VileCannon(VileCannonType.FatBoy);

	public VileCannon(VileCannonType vileCannonType) : base() {
		index = (int)WeaponIds.FrontRunner;
		weaponBarBaseIndex = 56;
		weaponBarIndex = 56;
		killFeedIndex = 56;
		weaponSlotIndex = 43;
		type = (int)vileCannonType;

		if (vileCannonType == VileCannonType.None) {
			displayName = "None";
			description = new string[] { "Do not equip a cannon." };
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmoUsage = 0;
			fireRate = 0;
			vileWeight = 0;
		} else if (vileCannonType == VileCannonType.FrontRunner) {
			fireRate = 45;
			vileAmmoUsage = 8;
			ammousage = vileAmmoUsage;
			damage = "3";
			displayName = "Front Runner";
			projSprite = "vile_mk2_proj";
			fadeSprite = "vile_mk2_proj_fade";
			description = new string[] { "This cannon not only offers power,", "but can be aimed up and down." };
			vileWeight = 2;
			effect = "None.";
		} else if (vileCannonType == VileCannonType.FatBoy) {
			fireRate = 45;
			damage = "4";
			flinch = "26";
			vileAmmoUsage = 24;
			ammousage = vileAmmoUsage;
			displayName = "Fat Boy";
			projSprite = "vile_mk2_fb_proj";
			fadeSprite = "vile_mk2_fb_proj_fade";
			killFeedIndex = 90;
			weaponSlotIndex = 61;
			description = new string[] { "The most powerful cannon around,", "it consumes a lot of energy." };
			vileWeight = 3;
			effect = "None.";
		}
		if (vileCannonType == VileCannonType.LongshotGizmo) {
			fireRate = 6;
			damage = "1";
			vileAmmoUsage = 4;
			ammousage = vileAmmoUsage;
			displayName = "Longshot Gizmo";
			projSprite = "vile_mk2_lg_proj";
			fadeSprite = "vile_mk2_lg_proj_fade";
			killFeedIndex = 91;
			weaponSlotIndex = 62;
			description = new string[] { "This cannon fires 5 shots at once,", "but leaves you open to attack." };
			vileWeight = 4;
			effect = "Burst of 5 shots.";
		}
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (vile.cannonWeapon.type == (int)VileCannonType.None) return;

		bool isLongshotGizmo = type == (int)VileCannonType.LongshotGizmo;
		if (isLongshotGizmo && vile.gizmoCooldown > 0) return;

		Player player = vile.player;
		if (shootCooldown > 0 || !vile.missileWeapon.isCooldownPercentDone(0.8f)) return;
		if (vile.charState is MissileAttack || vile.charState is RocketPunchAttack) return;
		float overrideAmmoUsage = (isLongshotGizmo && vile.isVileMK2) ? 6 : vileAmmoUsage;

		if (isLongshotGizmo && vile.longshotGizmoCount > 0) {
			vile.usedAmmoLastFrame = true;
			if (vile.weaponHealAmount == 0) {
				vile.addAmmo(-vileAmmoUsage);
			}
		} else if (!vile.tryUseVileAmmo(overrideAmmoUsage)) return;

		if (isLongshotGizmo) {
			vile.isShootingGizmo = true;
		}

		bool gizmoStart = (isLongshotGizmo && vile.charState is not CannonAttack);
		if (gizmoStart || vile.charState is Idle || vile.charState is Run || vile.charState is Dash || vile.charState is VileMK2GrabState) {
			vile.setVileShootTime(this);
			vile.changeState(new CannonAttack(isLongshotGizmo, vile.grounded), true);
		} else {
			if (vile.charState is LadderClimb) {
				if (player.input.isHeld(Control.Left, player)) vile.xDir = -1;
				if (player.input.isHeld(Control.Right, player)) vile.xDir = 1;
				vile.changeSpriteFromName("ladder_shoot2", true);
			}

			if (vile.charState is Jump || vile.charState is Fall || vile.charState is WallKick || vile.charState is VileHover || vile.charState is AirDash) {
				vile.setVileShootTime(this);
				if (!Options.main.lockInAirCannon) {
					if (vile.charState is AirDash) {
						vile.changeState(vile.getFallState(), true);
					}
					vile.changeSpriteFromName("cannon_air", true);
					CannonAttack.shootLogic(vile);
				} else {
					vile.changeState(new CannonAttack(false, false), true);
				}
			} else {
				vile.setVileShootTime(this);
				CannonAttack.shootLogic(vile);
			}
		}

		if (isLongshotGizmo) {
			vile.longshotGizmoCount++;
			if (vile.longshotGizmoCount >= 5 || vile.energy.ammo <= 3) {
				vile.longshotGizmoCount = 0;
				vile.isShootingGizmo = false;
			}
		}
	}
}

public class CannonAttack : CharState {
	bool isGizmo;
	public Vile vile = null!;
	public CannonAttack(bool isGizmo, bool grounded) : base(getSprite(isGizmo, grounded)) {
		useDashJumpSpeed = true;
		this.isGizmo = isGizmo;
	}

	public static string getSprite(bool isGizmo, bool grounded) {
		if (isGizmo) {
			return grounded ? "idle_gizmo" : "cannon_gizmo_air";
		}
		return grounded ? "idle_shoot" : "cannon_air";
	}

	public override void update() {
		base.update();

		if (vile.isShootingGizmo) {
			if (vile.cannonWeapon.shootCooldown == 0) {
				vile.cannonWeapon.vileShoot(0, vile);
			}
			if (vile.energy.ammo <= 0) {
				vile.isShootingGizmo = false;
			}
			return;
		}
		//groundCodeWithMove();

		if (character.sprite.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public static void shootLogic(Vile vile) {
		if (vile.sprite.getCurrentFrame().POIs.IsNullOrEmpty()) {
			return;
		}
		Point shootVel = vile.getVileShootVel(true);

		var player = vile.player;
		vile.playSound("frontrunner", sendRpc: true);

		string muzzleSprite = "cannon_muzzle";
		if (vile.cannonWeapon.type == (int)VileCannonType.FatBoy) muzzleSprite += "_fb";
		if (vile.cannonWeapon.type == (int)VileCannonType.LongshotGizmo) muzzleSprite += "_lg";

		Point shootPos = vile.setCannonAim(new Point(shootVel.x, shootVel.y));
		if (vile.sprite.name.EndsWith("_grab")) {
			shootPos = vile.getFirstPOIOrDefault("s");
		}

		var muzzle = new Anim(
			shootPos, muzzleSprite, vile.getShootXDir(), player.getNextActorNetId(), true, true, host: vile
		);
		muzzle.angle = new Point(shootVel.x, vile.getShootXDir() * shootVel.y).angle;
		if (vile.getShootXDir() == -1) {
			shootVel = new Point(shootVel.x * vile.getShootXDir(), shootVel.y);
		}
		if (vile.cannonWeapon.type == (int)VileCannonType.FrontRunner) {
			new VileCannonProj(
				shootPos, vile.xDir, 0, MathF.Round(shootVel.byteAngle), vile.cannonWeapon.projSprite,
				vile, player, player.getNextActorNetId(), rpc: true
			);
		}
		else if (vile.cannonWeapon.type == (int)VileCannonType.FatBoy) {
			new VileCannonProj(
				shootPos, vile.xDir, 1, MathF.Round(shootVel.byteAngle), vile.cannonWeapon.projSprite,
				vile, player, player.getNextActorNetId(), rpc: true
			);
		}
		else if (vile.cannonWeapon.type == (int)VileCannonType.LongshotGizmo) {
			new VileCannonProj(
				shootPos, vile.xDir, 2, MathF.Round(shootVel.byteAngle), vile.cannonWeapon.projSprite,
				vile, player, player.getNextActorNetId(), rpc: true
			);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		vile = character as Vile ?? throw new NullReferenceException();
		shootLogic(vile);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		vile.isShootingGizmo = false;
		if (isGizmo) {
			vile.gizmoCooldown = 0.5f;
		}
	}
}
public class VileCannonProj : Projectile {
	public int type = 0;
	public VileCannonProj(
		Point pos, int xDir, int type, float byteAngle, string sprite,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, sprite, netId, player
	) {
		xScale = xDir;
		maxTime = 0.5f;
		destroyOnHit = true;
		this.type = type;
		if (type == (int)VileCannonType.FrontRunner) {
			weapon = VileCannon.netWeaponFR;
			sprite = "vile_mk2_proj";
			fadeSprite = "vile_mk2_proj_fade";
			fadeOnAutoDestroy = true;
			damager.damage = 3;
			projId = (int)ProjIds.FrontRunner;
		} else if (type == (int)VileCannonType.FatBoy) {
			weapon = VileCannon.netWeaponFB;
			sprite = "vile_mk2_fb_proj";
			fadeSprite = "vile_mk2_fb_proj_fade";
			fadeOnAutoDestroy = true;
			damager.damage = 4;
			damager.flinch = Global.defFlinch;
			projId = (int)ProjIds.FatBoy;
			maxTime = 0.35f;
		} else if (type == (int)VileCannonType.LongshotGizmo) {
			weapon = VileCannon.netWeaponLG;
			sprite = "vile_mk2_lg_proj";
			fadeSprite = "vile_mk2_lg_proj_fade";
			fadeOnAutoDestroy = true;
			damager.damage = 1;
			projId = (int)ProjIds.LongshotGizmo;
		}
		byteAngle = byteAngle % 256;
		this.byteAngle = byteAngle;
		vel.x = 300 * Helpers.cosb(byteAngle);
		vel.y = 300 * Helpers.sinb(byteAngle);


		if (rpc) {
			List<Byte> extraBytes = new List<Byte> {
			};
			extraBytes.Add((byte)type);
			extraBytes.AddRange(Encoding.ASCII.GetBytes(sprite));
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle, extraBytes.ToArray());

		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		string sprite = Encoding.ASCII.GetString(args.extraData[1..]);
		return new VileCannonProj(
			args.pos, args.xDir, args.extraData[0], args.byteAngle, sprite, args.owner, args.player, args.netId
		);
	}
}
*/
#endregion
