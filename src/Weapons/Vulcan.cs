namespace MMXOnline;

public enum VulcanType {
	None = -1,
	CherryBlast,
	DistanceNeedler,
	BuckshotDance
}

public class Vulcan : Weapon {
	public float vileAmmoUsage;
	public string muzzleSprite;
	public string projSprite;

	public Vulcan(VulcanType vulcanType) : base() {
		index = (int)WeaponIds.Vulcan;
		weaponBarBaseIndex = 26;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 62;
		weaponSlotIndex = 44;
		type = (int)vulcanType;

		if (vulcanType == VulcanType.None) {
			displayName = "None";
			description = new string[] { "Do not equip a Vulcan." };
			killFeedIndex = 126;
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
			ammousage = 0.3;
			damage = "1";
			effect = "Splits.";
		}
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (type == (int)VulcanType.DistanceNeedler && shootCooldown > 0) return;
		if (string.IsNullOrEmpty(vile.charState.shootSprite)) return;

		Player player = vile.player;
		if (vile.tryUseVileAmmo(vileAmmoUsage, true)) {
			if (vile.charState is LadderClimb) {
				if (player.input.isHeld(Control.Left, player)) vile.xDir = -1;
				if (player.input.isHeld(Control.Right, player)) vile.xDir = 1;
			}
			vile.changeSpriteFromName(vile.charState.shootSprite, false);
			shootVulcan(vile);
		}
	}

	public void shootVulcan(Vile vile) {
		Player player = vile.player;
		if (shootCooldown <= 0) {
			vile.vulcanLingerTime = 0f;
			new VulcanMuzzleAnim(this, vile.getShootPos(), vile.getShootXDir(), vile, player.getNextActorNetId(), true, true);
			new VulcanProj(this, vile.getShootPos(), vile.getShootXDir(), player, player.getNextActorNetId(), rpc: true);
			if (type == (int)VulcanType.BuckshotDance && Global.isOnFrame(3)) {
				new VulcanProj(this, vile.getShootPos(), vile.getShootXDir(), player, player.getNextActorNetId(), rpc: true);
			}
			vile.playSound("vulcan", sendRpc: true);
			shootCooldown = fireRate;
		}
	}
}

public class VulcanProj : Projectile {
	public VulcanProj(Vulcan weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, weapon.type == (int)VulcanType.DistanceNeedler ? 600 : 500, 1, player, weapon.projSprite, 0, 0f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.Vulcan;
		maxTime = 0.25f;
		destroyOnHit = true;
		reflectable = true;

		if (weapon.type == (int)VulcanType.DistanceNeedler) {
			maxTime = 0.3f;
			destroyOnHit = false;
			damager.hitCooldown = 0.2f;
			damager.damage = 2;
			projId = (int)ProjIds.DistanceNeedler;
		} else if (weapon.type == (int)VulcanType.BuckshotDance) {
			//this.xDir = 1;
			//pixelPerfectRotation = true;
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
			vel = Point.createFromAngle(angle).times(speed);
			projId = (int)ProjIds.BuckshotDance;
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
	}
}

public class VulcanMuzzleAnim : Anim {
	Character chr;
	public VulcanMuzzleAnim(Vulcan weapon, Point pos, int xDir, Character chr, ushort? netId = null, bool sendRpc = false, bool ownedByLocalPlayer = true) :
		base(pos, weapon.muzzleSprite, xDir, netId, true, sendRpc, ownedByLocalPlayer) {
		this.chr = chr;
	}

	public override void postUpdate() {
		if (chr.currentFrame.getBusterOffset() != null) {
			changePos(chr.getShootPos());
		}
	}
}

public class VulcanCharState : CharState {
	bool isCrouch;
	public VulcanCharState(bool isCrouch) : base(isCrouch ? "crouch_shoot" : "idle_shoot", "", "", "") {
		useDashJumpSpeed = true;
		this.isCrouch = isCrouch;
	}

	public override void update() {
		base.update();

		if (isCrouch && !player.input.isHeld(Control.Down, player)) {
			character.changeToIdleOrFall();
			return;
		}

		if (!player.input.isHeld(Control.Shoot, player)) {
			if (isCrouch) {
				character.changeToCrouchOrFall();
			} else {
				character.changeToIdleOrFall();
			}
			return;
		}

		if (player.input.isHeld(Control.Left, player)) character.xDir = -1;
		if (player.input.isHeld(Control.Right, player)) character.xDir = 1;
	}
}
