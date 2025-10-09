namespace MMXOnline;

public enum VulcanType {

	CherryBlast,
	DistanceNeedler,
	BuckshotDance
}
public class VileVulcan : Weapon {
	public float vileAmmoUsage;
	public VileVulcan() : base() {
		index = (int)WeaponIds.Vulcan;
		weaponBarBaseIndex = 26;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 62;
		weaponSlotIndex = 44;
	}
	public void ladderVoid(Vile vava) {
        if (vava.charState is LadderClimb) {
			if (vava.player.input.isHeld(Control.Left, vava.player)) {
				vava.xDir = -1;
			}
			if (vava.player.input.isHeld(Control.Right, vava.player)) {
				vava.xDir = 1;
			}
		}
    }
}
public class CherryBlast : VileVulcan {
	public static CherryBlast netWeapon = new();
	public CherryBlast() : base() {
		type = (int)VulcanType.CherryBlast;
		fireRate = 6;
		displayName = "Cherry Blast";
		vileAmmoUsage = 0.5f;
		vileWeight = 2;
		ammousage = vileAmmoUsage;
		damage = "1";
		effect = "None.";
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < vileAmmoUsage) return;
		shoot(vile, []);
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		vava.vulcanLingerTime = 0f;
		ladderVoid(vava);
		vava.playSound("vulcan", sendRpc: true);
		vava.changeSpriteFromName(vava.charState.shootSpriteEx, false);
		vava.setVileShootTime(this);
		vava.tryUseVileAmmo(vileAmmoUsage, true);
		new VulcanCherryBlast(
			vava.getShootPos(), vava.getShootXDir(), vava,
			vava.player, character.player.getNextActorNetId(), rpc: true
		);
	}
}
public class DistanceNeedler : VileVulcan {
	public static DistanceNeedler netWeapon = new();
	public DistanceNeedler() : base() {
		type = (int)VulcanType.DistanceNeedler;
		fireRate = 15;
		displayName = "Distance Needler";
		vileAmmoUsage = 6f;
		killFeedIndex = 88;
		weaponSlotIndex = 59;
		vileWeight = 2;
		ammousage = vileAmmoUsage;
		damage = "2";
		hitcooldown = "0.2";
		effect = "Won't destroy on hit.";
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < vileAmmoUsage) return;
		shoot(vile, []);
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		vava.vulcanLingerTime = 0f;
		ladderVoid(vava);
		vava.playSound("vulcan", sendRpc: true);
		vava.changeSpriteFromName(vava.charState.shootSpriteEx, false);
		vava.setVileShootTime(this);
		vava.tryUseVileAmmo(vileAmmoUsage, true);
		new VulcanDistanceNeedler(
			vava.getShootPos(), vava.getShootXDir(), vava,
			vava.player, character.player.getNextActorNetId(), rpc: true
		);
	}
}
public class BuckshotDance : VileVulcan {
	public static BuckshotDance netWeapon = new();
	public BuckshotDance() : base() {
		type = (int)VulcanType.BuckshotDance;
		fireRate = 8;
		displayName = "Buckshot Dance";
		vileAmmoUsage = 2f;
		killFeedIndex = 89;
		weaponSlotIndex = 60;
		vileWeight = 4;
		ammousage = vileAmmoUsage;
		damage = "1";
		effect = "Splits.";
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < vileAmmoUsage) return;
		shoot(vile, []);
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		vava.vulcanLingerTime = 0f;
		ladderVoid(vava);
		vava.playSound("vulcan", sendRpc: true);
		vava.changeSpriteFromName(vava.charState.shootSpriteEx, false);
		vava.setVileShootTime(this);
		vava.tryUseVileAmmo(vileAmmoUsage, true);
		new VulcanBuckshotDance(
			vava.getShootPos(), vava.getShootXDir(), vava,
			vava.player, character.player.getNextActorNetId(), rpc: true
		);
		if (Global.isOnFrame(3)) {
			new VulcanBuckshotDance(
				vava.getShootPos(), vava.getShootXDir(), vava,
				vava.player, character.player.getNextActorNetId(), rpc: true
			);
		}
	}
}
public class VulcanCherryBlast : Projectile {
	public VulcanCherryBlast(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "vulcan_proj", netId, player
	) {
		weapon = CherryBlast.netWeapon;
		damager.damage = 1;
		vel = new Point(500 * xDir, 0);
		destroyOnHit = true;
		reflectable = true;
		maxTime = 0.25f;
		projId = (int)ProjIds.Vulcan;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new VulcanCherryBlast(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}
public class VulcanDistanceNeedler : Projectile {
	public VulcanDistanceNeedler(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "vulcan_dn_proj", netId, player
	) {
		weapon = DistanceNeedler.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 12;
		vel = new Point(600 * xDir, 0);
		destroyOnHit = false;
		reflectable = true;
		maxTime = 0.3f;
		projId = (int)ProjIds.DistanceNeedler;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new VulcanDistanceNeedler(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}
public class VulcanBuckshotDance : Projectile {
	public VulcanBuckshotDance(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "vulcan_bd_proj", netId, player
	) {
		weapon = BuckshotDance.netWeapon;
		damager.damage = 1;
		destroyOnHit = true;
		reflectable = true;
		maxTime = 0.25f;
		projId = (int)ProjIds.BuckshotDance;
		int rand = 0;
		if (player.character is Vile vile) {
			rand = vile.buckshotDanceNum % 3;
			vile.buckshotDanceNum++;
		}
		float angle = 0;
		if (rand == 0) angle = 0;
		if (rand == 1) angle = -20;
		if (rand == 2) angle = 20;
		if (xDir == -1) angle += 180;
		vel = Point.createFromAngle(angle).times(500);
		projId = (int)ProjIds.BuckshotDance;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new VulcanBuckshotDance(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}
/*
public class VulcanMuzzleAnim : Anim {
	Character chr;
	public VulcanMuzzleAnim(Vulcan weapon, Point pos, int xDir, Character chr, ushort? netId = null, bool sendRpc = false, bool ownedByLocalPlayer = true) :
		base(pos, weapon.muzzleSprite ?? "empty", xDir, netId, true, sendRpc, ownedByLocalPlayer) {
		this.chr = chr;
	}

	public override void postUpdate() {
		if (chr.currentFrame.getBusterOffset() != null) {
			changePos(chr.getShootPos());
		}
	}
}

public class Vulcan : Weapon {
	public float vileAmmoUsage;
	public string? muzzleSprite;
	public string? projSprite;
	public static Vulcan netWeaponCB = new Vulcan(VulcanType.CherryBlast);
	public static Vulcan netWeaponDN = new Vulcan(VulcanType.DistanceNeedler);
	public static Vulcan netWeaponBD = new Vulcan(VulcanType.BuckshotDance);
	public Vulcan(VulcanType vulcanType) : base() {
		index = (int)WeaponIds.Vulcan;
		weaponBarBaseIndex = 26;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 62;
		weaponSlotIndex = 44;
		type = (int)vulcanType;
		if (vulcanType == VulcanType.None) {
			displayName = "None";
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmoUsage = 0;
			fireRate = 0;
			vileWeight = 0;
			effect = "";
		} else if (vulcanType == VulcanType.NoneCutter) {
			displayName = "None(Cutter)";
			description = new string[] { "Equip Missile." };
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmoUsage = 0;
			fireRate = 0;
			vileWeight = 0;
			effect = "Equip Cutter";
		} else if (vulcanType == VulcanType.NoneMissile) {
			displayName = "None(Missile)";
			description = new string[] { "Equip Missile." };
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmoUsage = 0;
			fireRate = 0;
			vileWeight = 0;
			effect = "Equip Missile";
		} else if (vulcanType == VulcanType.CherryBlast) {
			fireRate = 6;
			displayName = "Cherry Blast";
			vileAmmoUsage = 0.25f;
			muzzleSprite = "vulcan_muzzle";
			projSprite = "vulcan_proj";
			description = new string[] { "With a range of approximately 20 feet,", "this vulcan is easy to use." };
			vileWeight = 2;
			ammousage = vileAmmoUsage;
			damage = "1";
			effect = "None.";
		} else if (vulcanType == VulcanType.DistanceNeedler) {
			fireRate = 15;
			displayName = "Distance Needler";
			vileAmmoUsage = 6f;
			muzzleSprite = "vulcan_dn_muzzle";
			projSprite = "vulcan_dn_proj";
			killFeedIndex = 88;
			weaponSlotIndex = 59;
			description = new string[] { "This vulcan has good range and speed,", "but cannot fire rapidly." };
			vileWeight = 2;
			ammousage = vileAmmoUsage;
			damage = "2";
			hitcooldown = "0.2";
			effect = "Won't destroy on hit.";
		} else if (vulcanType == VulcanType.BuckshotDance) {
			fireRate = 8;
			displayName = "Buckshot Dance";
			vileAmmoUsage = 0.3f;
			muzzleSprite = "vulcan_bd_muzzle";
			projSprite = "vulcan_bd_proj";
			killFeedIndex = 89;
			weaponSlotIndex = 60;
			description = new string[] { "The scattering power of this vulcan", "results in less than perfect aiming." };
			vileWeight = 4;
			ammousage = vileAmmoUsage;
			damage = "1";
			effect = "Splits.";
		}
	}
	public static bool isVulcanTypes(Vile vile) {
		if (vile.vulcanWeapon.type == (int)VulcanType.BuckshotDance) return true;
		if (vile.vulcanWeapon.type == (int)VulcanType.DistanceNeedler) return true;
		return vile.vulcanWeapon.type == (int)VulcanType.CherryBlast;
	}
	public static bool isNotVulcanTypes(Vile vile) {
		if (vile.vulcanWeapon.type == (int)VulcanType.NoneMissile) return true;
		return vile.vulcanWeapon.type == (int)VulcanType.NoneCutter;
	}
	public static bool ladderVoid(Vile vile) {
		if (vile.charState is LadderClimb) {
			if (vile.player.input.isHeld(Control.Left, vile.player)) {
				vile.xDir = -1;
				return true;
			}
			if (vile.player.input.isHeld(Control.Right, vile.player)) {
				vile.xDir = 1;
				return true;
			}
		}
		return false;
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (vile.vulcanWeapon.type == (int)VulcanType.None) return;
		if (type == (int)VulcanType.DistanceNeedler && shootCooldown > 0) return;
		if (string.IsNullOrEmpty(vile.charState.shootSpriteEx)) return;
		if (isVulcanTypes(vile) && vile.tryUseVileAmmo(vileAmmoUsage, true)) {
			vile.changeSpriteFromName(vile.charState.shootSpriteEx, false);
			ladderVoid(vile);
			shootVulcan(vile);
		} else if (isNotVulcanTypes(vile) && vile.tryUseVileAmmo(vileAmmoUsage, false)) {
			shootVulcan(vile);
			vile.setVileShootTime(this);
		}
	}

	public void shootVulcan(Vile vile) {
		Player player = vile.player;
		if (vile.vulcanWeapon.type == (int)VulcanType.NoneMissile) {
			vile.missileWeapon.vileShoot(WeaponIds.ElectricShock, vile);		
			vile.setVileShootTime(this);
		} else if (vile.vulcanWeapon.type == (int)VulcanType.NoneCutter) {
			vile.cutterWeapon.vileShoot(WeaponIds.VileCutter, vile);		
			vile.setVileShootTime(this);
		} else {
			if (shootCooldown <= 0) {
				vile.vulcanLingerTime = 0f;
				new VulcanMuzzleAnim(this, vile.getShootPos(), vile.getShootXDir(), vile, player.getNextActorNetId(), true, true);
				if (vile.vulcanWeapon.type == (int)VulcanType.CherryBlast) {
					new VulcanCherryBlast(
						vile.getShootPos(), vile.getShootXDir(), vile,
						player, player.getNextActorNetId(), rpc: true
					);
				} else if (vile.vulcanWeapon.type == (int)VulcanType.DistanceNeedler) {
					new VulcanDistanceNeedler(
						vile.getShootPos(), vile.getShootXDir(), vile,
						player, player.getNextActorNetId(), rpc: true
					);
				} else if (vile.vulcanWeapon.type == (int)VulcanType.BuckshotDance) {
					new VulcanBuckshotDance(
						vile.getShootPos(), vile.getShootXDir(), vile,
						player, player.getNextActorNetId(), rpc: true
					);
					if (Global.isOnFrame(3)) {
						new VulcanBuckshotDance(
							vile.getShootPos(), vile.getShootXDir(), vile,
							player, player.getNextActorNetId(), rpc: true
						);
					}
				}
				vile.playSound("vulcan", sendRpc: true);
				shootCooldown = fireRate;
			}
		}
	}
}
*/
