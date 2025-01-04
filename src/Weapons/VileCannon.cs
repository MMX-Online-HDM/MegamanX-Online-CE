using System;
using System.Linq;

namespace MMXOnline;

public enum VileCannonType {
	None = -1,
	FrontRunner,
	FatBoy,
	LongshotGizmo
}

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
			Flinch = "26";
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
		bool isLongshotGizmo = type == (int)VileCannonType.LongshotGizmo;
		if (isLongshotGizmo && vile.gizmoCooldown > 0) return;

		Player player = vile.player;
		if (shootCooldown > 0 || !vile.missileWeapon.isCooldownPercentDone(0.8f)) return;
		if (vile.charState is MissileAttack || vile.charState is RocketPunchAttack) return;
		float overrideAmmoUsage = (isLongshotGizmo && vile.isVileMK2) ? 6 : vileAmmoUsage;

		if (isLongshotGizmo && vile.longshotGizmoCount > 0) {
			vile.usedAmmoLastFrame = true;
			if (vile.weaponHealAmount == 0) {
				player.vileAmmo -= vileAmmoUsage;
				if (player.vileAmmo < 0) player.vileAmmo = 0;
			}
		} else if (!vile.tryUseVileAmmo(overrideAmmoUsage)) return;

		if (isLongshotGizmo) {
			vile.isShootingLongshotGizmo = true;
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
						vile.changeState(new Fall(), true);
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
			if (vile.longshotGizmoCount >= 5 || player.vileAmmo <= 3) {
				vile.longshotGizmoCount = 0;
				vile.isShootingLongshotGizmo = false;
			}
		}
	}
}

public class VileCannonProj : Projectile {
	public VileCannonProj(
		VileCannon weapon, Point pos, float byteAngle, Player player,
		ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, 1, 300, 3, player, weapon.projSprite, 0, 0f, netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = weapon.fadeSprite;
		projId = (int)ProjIds.FrontRunner;
		maxTime = 0.5f;
		destroyOnHit = true;

		if (weapon.type == (int)VileCannonType.FrontRunner) {
			// Nothing.
		} else if (weapon.type == (int)VileCannonType.FatBoy) {
			xScale = xDir;
			damager.damage = 4;
			damager.flinch = Global.defFlinch;
			projId = (int)ProjIds.FatBoy;
			maxTime = 0.35f;
		} else if (weapon.type == (int)VileCannonType.LongshotGizmo) {
			damager.damage = 1;
			/*
			if (ownedByLocalPlayer) {
				if (player.vileAmmo >= 32 - weapon.vileAmmoUsage) { damager.damage = 3; }
				else if (player.vileAmmo >= 32 - weapon.vileAmmoUsage * 2) { damager.damage = 2; }
				else { damager.damage = 1; }
			}*/
			projId = (int)ProjIds.LongshotGizmo;
		}
		// Speed and angle.
		Point norm = Point.createFromByteAngle(byteAngle);
		this.vel.x = norm.x * speed * xDir;
		this.vel.y = norm.y * speed;
		this.byteAngle = byteAngle;

		if (rpc) {
			rpcCreateByteAngle(pos, player, netProjId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		VileCannon vileCannon;
		if (args.projId == (int)ProjIds.LongshotGizmo) {
			vileCannon = VileCannon.netWeaponLG;
		} else if (args.projId == (int)ProjIds.FatBoy) {
			vileCannon = VileCannon.netWeaponFB;
		} else {
			vileCannon = VileCannon.netWeaponFR;
		}
		return new VileCannonProj(
			vileCannon, args.pos, args.byteAngle, args.player, args.netId
		);
	}
}

public class CannonAttack : CharState {
	bool isGizmo;
	private Vile vile = null!;

	public CannonAttack(bool isGizmo, bool grounded) : base(getSprite(isGizmo, grounded)) {
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

		if (vile.isShootingLongshotGizmo) {
			if (vile.cannonWeapon.shootCooldown == 0) {
				vile.cannonWeapon.vileShoot(0, vile);
			}
			if (player.vileAmmo <= 0) {
				vile.isShootingLongshotGizmo = false;
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

		new VileCannonProj(
			vile.cannonWeapon,
			shootPos, MathF.Round(shootVel.byteAngle), //vile.longshotGizmoCount,
			player, player.getNextActorNetId(), rpc: true
		);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		vile = character as Vile ?? throw new NullReferenceException();
		shootLogic(vile);
		character.useGravity = false;
		character.stopMoving();
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		vile.isShootingLongshotGizmo = false;
		character.useGravity = true;
		if (isGizmo) {
			vile.gizmoCooldown = 0.5f;
		}
	}
}
