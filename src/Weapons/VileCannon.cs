using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SFML.Graphics;
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
			if (vile.longshotGizmoCount >= 5 || player.vileAmmo <= 3) {
				vile.longshotGizmoCount = 0;
				vile.isShootingLongshotGizmo = false;
			}
		}
	}
}

public class VileCannonProj : Projectile {
	public int type = 0;
	public VileCannonProj(
		Point pos, int xDir, int type, float byteAngle, string sprite,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, sprite , netId, player
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

public class CannonAttack : VileState {
	bool isGizmo;

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
		shootLogic(vile);
		if (!isGizmo && (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player))) {
			exitOnAirborne = true;
		} else {
			exitOnAirborne = false;
			character.useGravity = false;
			character.stopMoving();
		}
		
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		vile.isShootingLongshotGizmo = false;
		character.useGravity = true;
		if (isGizmo) {
			vile.gizmoCooldown = 0.5f;
		}
	}
}
