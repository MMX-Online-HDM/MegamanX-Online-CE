using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;

public class Buster : Weapon {
	public List<BusterProj> lemonsOnField = new List<BusterProj>();
	public bool isUnpoBuster;
	public void setUnpoBuster() {
		isUnpoBuster = true;
		rateOfFire = 0.75f;
		weaponBarBaseIndex = 70;
		weaponBarIndex = 59;
		weaponSlotIndex = 121;
		killFeedIndex = 180;
		canHealAmmo = false;
	}

	public static bool isNormalBuster(Weapon weapon) {
		return weapon is Buster buster && !buster.isUnpoBuster;
	}

	public static bool isWeaponUnpoBuster(Weapon weapon) {
		return weapon is Buster buster && buster.isUnpoBuster;
	}

	public Buster() : base() {
		index = (int)WeaponIds.Buster;
		killFeedIndex = 0;
		weaponBarBaseIndex = 0;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 0;
		shootSounds = new List<string>() { "", "", "", "" };
		rateOfFire = 0.15f;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		if (!base.canShoot(chargeLevel, player)) return false;
		if (chargeLevel > 1) {
			return true;
		}
		for (int i = lemonsOnField.Count - 1; i >= 0; i--) {
			if (lemonsOnField[i].destroyed) {
				lemonsOnField.RemoveAt(i);
				continue;
			}
		}
		if ((player.character as MegamanX)?.isHyperX == true) {
			return true;
		}
		return lemonsOnField.Count < 3;
	}

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		string shootSound = "buster";
		if (player.character is not MegamanX mmx) {
			return;
		}
		if (player.hasArmArmor(ArmorId.Light) || player.hasArmArmor(ArmorId.None) || player.hasUltimateArmor())
			shootSound = chargeLevel switch {
				_ when (
						mmx.stockedCharge
				) => "",
				0 => "buster",
				1 => "buster2",
				2 => "buster3",
				3 => "buster4",
				_ => ""
			};
		if (player.hasArmArmor(ArmorId.Giga)) {
			shootSound = chargeLevel switch {
				_ when (
					mmx.stockedCharge
				) => "",
				0 => "buster",
				1 => "buster2X2",
				2 => "buster3X2",
				3 => "",
				_ => shootSound
			};
		} else if (player.hasArmArmor(ArmorId.Max)) {
			shootSound = chargeLevel switch {
				_ when (
					mmx.stockedCharge
				) => "",
				_ when (
					mmx.stockedX3Buster
				) => "",
				0 => "busterX3",
				1 => "buster2X3",
				2 => "buster3X3",
				3 => "buster3X3",
				_ => shootSound
			};
		}
		if (mmx.stockedX3Buster) {
			if (player.ownedByLocalPlayer) {
				if (player.character.charState is not WallSlide) {
					shootTime = 0;
				}
				player.character.changeState(new X3ChargeShot(null), true);
				shootSound = "";
			}
			return;
		}
		bool hasUltArmor = player.character.hasUltimateArmorBS.getValue();
		bool isHyperX = ((player.character as MegamanX)?.isHyperX == true);

		if (isHyperX && chargeLevel > 0) {
			new BusterUnpoProj(this, pos, xDir, player, netProjId);
			new Anim(pos, "buster_unpo_muzzle", xDir, null, true);
			shootSound = "buster2";
		} else if (mmx.stockedCharge) {
			if (player.ownedByLocalPlayer) {
				player.character.changeState(new X2ChargeShot(1), true);
			}
		} else if (chargeLevel == 0) {
			lemonsOnField.Add(new BusterProj(this, pos, xDir, 0, player, netProjId));
		} else if (chargeLevel == 1) {
			new Buster2Proj(this, pos, xDir, player, netProjId);
		} else if (chargeLevel == 2) {
			new Buster3Proj(this, pos, xDir, 0, player, netProjId);
		} else if (chargeLevel >= 3) {
			if (hasUltArmor && !player.hasArmArmor(3)) {
				if (player.hasArmArmor(2)) {
					if (player.ownedByLocalPlayer) {
						player.character.changeState(new X2ChargeShot(2), true);
					}
					shootSound = "";
				} else {
					new Anim(pos.clone(), "buster4_muzzle_flash", xDir, null, true);
					new BusterPlasmaProj(this, pos, xDir, player, netProjId);
					shootSound = "plasmaShot";
				}
			} else if (player.hasArmArmor(3)) {
				if (player.ownedByLocalPlayer) {
					if (player.character.charState is not WallSlide) {
						shootTime = 0;
					}
					player.character.changeState(new X3ChargeShot(null), true);
					shootSound = "";
				}
			} else if (player.hasArmArmor(0) || player.hasArmArmor(1)) {
				new Anim(pos.clone(), "buster4_muzzle_flash", xDir, null, true);
				//Create the buster effect
				int xOff = xDir * -5;
				player.setNextActorNetId(netProjId);
				// Create first line instantly.
				createBuster4Line(pos.x + xOff, pos.y, xDir, player, 0f);
				// Create 2nd with a delay.
				Global.level.delayedActions.Add(new DelayedAction(delegate {
					createBuster4Line(pos.x + xOff, pos.y, xDir, player, 10f / 60f);
				}, 2.8f / 60f));
				// Use smooth spawn on the 3rd.
				Global.level.delayedActions.Add(new DelayedAction(delegate {
					createBuster4Line(pos.x + xOff, pos.y, xDir, player, 5f / 60f, true);
				}, 5.8f / 60f));
			} else if (player.hasArmArmor(2)) {
				if (player.ownedByLocalPlayer) {
					if (player.character.charState is not WallSlide) {
						shootTime = 0;
					}
					player.character.changeState(new X2ChargeShot(0), true);
					shootSound = "";
				}
			}
		}

		if (player?.character?.ownedByLocalPlayer == true && shootSound != "") {
			player.character.playSound(shootSound, sendRpc: true);
		}
	}
	
	
	public void createBuster4Line(
		float x, float y, int xDir, Player player,
		float offsetTime, bool smoothStart = false
	) {
		new Buster4Proj(
			this, new Point(x + xDir, y), xDir,
			player, 0, offsetTime,
			player.getNextActorNetId(allowNonMainPlayer: true), smoothStart
		);
		Global.level.delayedActions.Add(new DelayedAction(delegate {
			new Buster4Proj(
				this, new Point(x + xDir, y), xDir,
				player, 1, offsetTime,
				player.getNextActorNetId(allowNonMainPlayer: true), smoothStart
			);
		}, 1.8f / 60f
		));
		Global.level.delayedActions.Add(new DelayedAction(delegate {
			new Buster4Proj(
				this, new Point(x + xDir, y), xDir,
				player, 2, offsetTime,
				player.getNextActorNetId(allowNonMainPlayer: true), smoothStart
			);
		}, 3.8f / 60f
		));
		Global.level.delayedActions.Add(new DelayedAction(delegate {
			new Buster4Proj(
				this, new Point(x + xDir, y), xDir,
				player, 2, offsetTime,
				player.getNextActorNetId(allowNonMainPlayer: true), smoothStart
			);
		}, 5.8f / 60f
		));
		Global.level.delayedActions.Add(new DelayedAction(delegate {
			new Buster4Proj(
				this, new Point(x + xDir, y), xDir,
				player, 3, offsetTime,
				player.getNextActorNetId(allowNonMainPlayer: true), smoothStart
			);
		}, 7.8f / 60f
		));
	}
}

public class BusterProj : Projectile {
	public BusterProj(
		Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 240, 1, player, "buster1", 0, 0, netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = "buster1_fade";
		reflectable = true;
		maxTime = 0.5175f;
		if (type == 0) projId = (int)ProjIds.Buster;
		else if (type == 1) projId = (int)ProjIds.ZBuster;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (System.MathF.Abs(vel.x) < 360) {
			vel.x += Global.spf * xDir * 900f;
			if (System.MathF.Abs(vel.x) >= 360) {
				vel.x = (float)xDir * 360;
			}
		}
	}
}

public class Buster2Proj : Projectile {
	public Buster2Proj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId) : base(weapon, pos, xDir, 350, 2, player, "buster2", 0, 0, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "buster2_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.Buster2;
		/*
		var busterWeapon = weapon as Buster;
		if (busterWeapon != null) {
			damager.damage = busterWeapon.getDamage(damager.damage);
		}
		*/
	}
}

public class BusterUnpoProj : Projectile {
	public BusterUnpoProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId) :
		base(weapon, pos, xDir, 350, 3, player, "buster_unpo", Global.defFlinch, 0.01f, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.BusterUnpo;
	}
}

public class Buster3Proj : Projectile {
	public int type;
	public List<Sprite> spriteMids = new List<Sprite>();
	float partTime;

	public Buster3Proj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 350, 3, player, "buster3", Global.defFlinch, 0f, netProjId, player.ownedByLocalPlayer) {
		this.type = type;
		maxTime = 0.5f;
		fadeSprite = "buster3_fade";
		projId = (int)ProjIds.Buster3;
		reflectable = true;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)type);
		}

		// Regular yellow charge
		if (type == 0) {
			damager.flinch = Global.halfFlinch;
		}
		// Double buster part 1
		if (type == 1) {
			damager.damage = 4;
			changeSprite("buster3_x2", true);
			projId = (int)ProjIds.Buster4;
			reflectable = false;
		}
		// Double buster part 2
		if (type == 2) {
			damager.damage = 4;
			changeSprite("buster4_x2", true);
			fadeSprite = "buster4_x2_fade";
			for (int i = 0; i < 6; i++) {
				var midSprite = Global.sprites["buster4_x2_orbit"].clone();
				spriteMids.Add(midSprite);
			}
			projId = (int)ProjIds.Buster4;
			reflectable = false;
		}
		// X3 buster part 1
		if (type == 3) {
			damager.damage = 4;
			changeSprite("buster4_x3", true);
			fadeSprite = "buster4_x2_fade";
			vel.x = 0;
			maxTime = 0.75f;
			projId = (int)ProjIds.Buster4;
			reflectable = false;
		}
		/*var busterWeapon = weapon as Buster;
		if (busterWeapon != null) {
			damager.damage = busterWeapon.getDamage(damager.damage);
		}*/
		fadeOnAutoDestroy = true;
	}

	public override void update() {
		base.update();
		if (type == 3) {
			vel.x += Global.spf * xDir * 550;
			if (MathF.Abs(vel.x) > 300) vel.x = 300 * xDir;
			partTime += Global.spf;
			if (partTime > 0.05f) {
				partTime = 0;
				new Anim(pos.addRand(0, 16), "buster4_x3_part", 1, null, true) { acc = new Point(-vel.x * 3f, 0) };
			}
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		if (type == 2) {
			float piHalf = MathF.PI / 2;
			float xOffset = 8;
			float partTime = (time * 0.75f);
			for (int i = 0; i < 6; i++) {
				float t = 0;
				float xOff2 = 0;
				float sinX = 0;
				if (i < 3) {
					t = partTime - (i * 0.025f);
					xOff2 = i * xDir * 3;
					sinX = 5 * MathF.Cos(partTime * 20);
				} else {
					t = partTime + (MathF.PI / 4) - ((i - 3) * 0.025f);
					xOff2 = (i - 3) * xDir * 3;
					sinX = 5 * MathF.Sin((partTime) * 20);
				}
				float sinY = 15 * MathF.Sin(t * 20);
				long zIndexTarget = zIndex - 1;
				float currentOffset = (t * 20) % (MathF.PI * 2);
				if (currentOffset > piHalf && currentOffset < piHalf * 3) {
					zIndexTarget = zIndex + 1;
				}
				spriteMids[i].draw(
					spriteMids[i].frameIndex,
					pos.x + x + sinX - xOff2 + xOffset,
					pos.y + y + sinY, xDir, yDir,
					getRenderEffectSet(), 1, 1, 1, zIndexTarget
				);
				spriteMids[i].update();
			}
		}
	}
}

public class Buster4Proj : Projectile {
	public int type = 0;
	public float offsetTime = 0;
	public float initY = 0;
	bool smoothStart = false;

	public Buster4Proj(
		Weapon weapon, Point pos, int xDir, Player player,
		int type, float offsetTime, ushort netProjId,
		bool smoothStart = false
	) : base(
		weapon, pos, xDir, 396, 4, player, "buster4",
		Global.defFlinch, 1f, netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = "buster4_fade";
		this.type = type;
		//this.vel.x = 0;
		initY = this.pos.y;
		this.offsetTime = offsetTime;
		this.smoothStart = smoothStart;
		maxTime = 0.6f;
		projId = (int)ProjIds.Buster4;
		/*var busterWeapon = weapon as Buster;
		if (busterWeapon != null) {
			damager.damage = busterWeapon.getDamage(damager.damage);
		}*/
	}

	public override void update() {
		base.update();
		base.frameIndex = type;
		float currentOffsetTime = offsetTime;
		if (smoothStart && time < 5f / 60f) {
			currentOffsetTime *= (time / 5f) * 60f;
		}
		float y = initY + (MathF.Sin((time + currentOffsetTime) * (MathF.PI * 6)) * 15f);
		changePos(new Point(pos.x, y));
	}
}

public class X2ChargeShot : CharState {
	bool fired;
	int type;
	bool pressFire;
	MegamanX mmx = null!;

	public X2ChargeShot(int type) : base(type == 0 ? "x2_shot" : "x2_shot2") {
		this.type = type;
		useDashJumpSpeed = true;
		airMove = true;
		landSprite = "x2_shot";
		airSprite = "x2_air_shot";

		if (type == 1) {
			landSprite = "x2_shot2";
			airSprite = "x2_air_shot2";
		}
	}

	public override void update() {
		base.update();
		if (!fired && character.currentFrame.getBusterOffset() != null) {
			fired = true;
			if (type == 0) {
				new Buster3Proj(
					player.weapon, character.getShootPos(), character.getShootXDir(), 1,
					player, player.getNextActorNetId(), rpc: true
				);
				character.playSound("buster4X2", sendRpc: true);
			} else if (type == 1) {
				new Buster3Proj(
					player.weapon, character.getShootPos(), character.getShootXDir(), 2,
					player, player.getNextActorNetId(), rpc: true
				);
				character.playSound("buster4X2", sendRpc: true);
			} else if (type == 2) {
				new BusterPlasmaProj(
					player.weapon, character.getShootPos(), character.getShootXDir(),
					player, player.getNextActorNetId(), rpc: true
				);
				character.playSound("plasmaShot", sendRpc: true);
			}
		}
		if (character.isAnimOver()) {
			if (type == 0 && pressFire) {
				fired = false;
				type = 1;
				mmx.stockedCharge = false;
				Global.serverClient?.rpc(RPC.playerToggle, (byte)player.id, (int)RPCToggleType.UnstockCharge);
				sprite = "x2_shot2";
				defaultSprite = sprite;
				landSprite = "x2_shot2";
				airSprite = "x2_shot2";
				if (!character.grounded || character.vel.y < 0) {
					sprite = "x2_air_shot2";
					defaultSprite = sprite;
				}
				character.changeSpriteFromName(sprite, true);
			} else {
				character.changeToIdleOrFall();
			}
		} else {
			if (!pressFire && stateTime > Global.spf && player.input.isPressed(Control.Shoot, player)) {
				pressFire = true;
			}
			if (character.grounded && player.input.isPressed(Control.Jump, player)) {
				character.vel.y = -character.getJumpPower();
				if (type == 0) {
					sprite = "x2_air_shot";
				} else {
					sprite = "x2_air_shot2";
				}	
				character.changeSpriteFromName(sprite, false);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();
		if (!character.grounded || character.vel.y > 0) {
			if (type == 0) {
				sprite = "x2_air_shot";
			} else {
				sprite = "x2_air_shot2";
			}
			character.changeSpriteFromName(sprite, true);
		}
		character.changeSpriteFromName(sprite, true);
	}

	public override void onExit(CharState newState) {
		if (newState is not AirDash && newState is not WallSlide) {
			character.shootAnimTime = 0;
		} else {
			character.shootAnimTime = 0.334f - character.animTime;
		}
		base.onExit(newState);
	}
}

public class X3ChargeShot : CharState {
	bool fired;
	int state = 0;
	bool pressFire;
	MegamanX mmx = null!;
	public HyperBuster? hyperBusterWeapon;

	public X3ChargeShot(HyperBuster? hyperBusterWeapon) : base("x3_shot", "", "", "") {
		this.hyperBusterWeapon = hyperBusterWeapon;
		airMove = true;
		useDashJumpSpeed = true;
	}

	public override void update() {
		base.update();
		if (character.grounded) {
			character.turnToInput(player.input, player);
		}
		if (!fired && character.currentFrame.getBusterOffset() != null && player.ownedByLocalPlayer) {
			fired = true;
			if (state == 0) {
				Point shootPos = character.getShootPos();
				int shootDir = character.getShootXDir();
				if (!player.hasUltimateArmor()) {
					new Anim(
						shootPos, "buster4_x3_muzzle", shootDir,
						player.getNextActorNetId(), true, sendRpc: true
					);
					new Buster3Proj(
						player.weapon, shootPos, shootDir,
						3, player, player.getNextActorNetId(), rpc: true
					);
					if (!(player.weapon is HyperBuster)) {
						character.playSound("buster3X3", sendRpc: true);
					}
				} else {
					new Anim(shootPos, "buster4_muzzle_flash", shootDir, null, true);
					new BusterPlasmaProj(
						player.weapon, shootPos, shootDir,
						player, player.getNextActorNetId(), rpc: true
					);
					character.playSound("plasmaShot", sendRpc: true);
				}
			} else {
				if (hyperBusterWeapon != null) {
					hyperBusterWeapon.ammo -= hyperBusterWeapon.getChipFactoredAmmoUsage(player);
				}
				character.playSound("buster3X3", sendRpc: true);
				float xDir = character.getShootXDir();
				new BusterX3Proj2(
					player.weapon, character.getShootPos().addxy(6 * xDir, -2), character.getShootXDir(), 0,
					player, player.getNextActorNetId(), rpc: true
				);
				new BusterX3Proj2(
					player.weapon, character.getShootPos().addxy(6 * xDir, -2), character.getShootXDir(), 1,
					player, player.getNextActorNetId(), rpc: true
				);
				new BusterX3Proj2(
					player.weapon, character.getShootPos().addxy(6 * xDir, -2), character.getShootXDir(), 2,
					player, player.getNextActorNetId(), rpc: true
				);
				new BusterX3Proj2(
					player.weapon, character.getShootPos().addxy(6 * xDir, -2), character.getShootXDir(), 3,
					player, player.getNextActorNetId(), rpc: true
				);
			}
		}
		if (character.isAnimOver()) {
			if (state == 0 && pressFire) {
				if (hyperBusterWeapon != null) {
					if (hyperBusterWeapon.ammo < hyperBusterWeapon.getChipFactoredAmmoUsage(player)) {
						if (character.grounded) character.changeState(new Idle(), true);
						else character.changeState(new Fall(), true);
						return;
					}
				} else {
					mmx.stockedX3Buster = false;
				}
				sprite = "x3_shot2";
				landSprite = "x3_shot2";
				if (!character.grounded || character.vel.y < 0) {
					sprite = "x3_air_shot2";
					defaultSprite = sprite;
				}
				defaultSprite = sprite;
				character.changeSpriteFromName(sprite, true);
				state = 1;
				fired = false;
			} else {
				character.changeToIdleOrFall();
			}
		} else {
			if (!pressFire && stateTime > Global.spf && player.input.isPressed(Control.Shoot, player)) {
				pressFire = true;
			}
			if (character.grounded && player.input.isPressed(Control.Jump, player)) {
				character.vel.y = -character.getJumpPower();
				if (state == 0) {
					sprite = "x2_air_shot";
					defaultSprite = sprite;
				} else {
					sprite = "x2_air_shot2";
					defaultSprite = sprite;
				}
				character.changeSpriteFromName(sprite, false);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();
		if (mmx == null) {
			throw new NullReferenceException();
		}
		if (!mmx.stockedX3Buster) {
			if (hyperBusterWeapon == null) {
				mmx.stockedX3Buster = true;
			}
			sprite = "x3_shot";
			defaultSprite = sprite;
			landSprite = "x3_shot";
			if (!character.grounded) {
				sprite = "x3_air_shot";
			}
			character.changeSpriteFromName(sprite, true);
		} else {
			mmx.stockedX3Buster = false;
			state = 1;
			sprite = "x3_shot2";
			defaultSprite = sprite;
			landSprite = "x3_shot2";
			if (!character.grounded) {
				sprite = "x3_air_shot2";
			}
			character.changeSpriteFromName(sprite, true);
		}
	}

	public override void onExit(CharState newState) {
		if (state == 0) {
			mmx.stockedX3Buster = true;
		} else {
			mmx.stockedX3Buster = false;
		}
		character.shootAnimTime = 0;
		base.onExit(newState);
	}
}

public class BusterX3Proj2 : Projectile {
	public int type = 0;
	public List<Point> lastPositions = new List<Point>();
	public BusterX3Proj2(
		Weapon weapon, Point pos, int xDir, int type,
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 400, 1,
		player, type == 0 || type == 3 ? "buster4_x3_orbit" : "buster4_x3_orbit2",
		0, 0, netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = "buster4_fade";
		this.type = type;
		reflectable = true;
		maxTime = 0.675f;
		projId = (int)ProjIds.BusterX3Proj2;
		if (type == 0) vel = new Point(-200 * xDir, -100);
		if (type == 1) vel = new Point(-150 * xDir, -50);
		if (type == 2) vel = new Point(-150 * xDir, 50);
		if (type == 3) vel = new Point(-200 * xDir, 100);
		frameSpeed = 0;
		frameIndex = 0;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)type);
		}
	}

	public override void update() {
		base.update();
		float maxSpeed = 600;
		vel.inc(new Point(Global.spf * 1500 * xDir, 0));
		if (MathF.Abs(vel.x) > maxSpeed) vel.x = maxSpeed * xDir;
		lastPositions.Add(pos);
		if (lastPositions.Count > 4) lastPositions.RemoveAt(0);
	}

	public override void render(float x, float y) {
		string spriteName = type == 0 || type == 3 ? "buster4_x3_orbit" : "buster4_x3_orbit2";
		//if (lastPositions.Count > 3) Global.sprites[spriteName].draw(1, lastPositions[3].x + x, lastPositions[3].y + y, 1, 1, null, 1, 1, 1, zIndex);
		if (lastPositions.Count > 2) Global.sprites[spriteName].draw(2, lastPositions[2].x + x, lastPositions[2].y + y, 1, 1, null, 1, 1, 1, zIndex);
		if (lastPositions.Count > 1) Global.sprites[spriteName].draw(3, lastPositions[1].x + x, lastPositions[1].y + y, 1, 1, null, 1, 1, 1, zIndex);
		base.render(x, y);
	}
}

public class BusterPlasmaProj : Projectile {
	public HashSet<IDamagable> hitDamagables = new HashSet<IDamagable>();
	public BusterPlasmaProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 400, 4, player, "buster_plasma", Global.defFlinch, 0.25f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 0.5f;
		projId = (int)ProjIds.BusterX3Plasma;
		destroyOnHit = false;
		xScale = 0.75f;
		yScale = 0.75f;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (ownedByLocalPlayer && hitDamagables.Count < 1) {
			if (!hitDamagables.Contains(damagable)) {
				hitDamagables.Add(damagable);
				float xThreshold = 10;
				Point targetPos = damagable.actor().getCenterPos();
				float distToTarget = MathF.Abs(pos.x - targetPos.x);
				Point spawnPoint = pos;
				if (distToTarget > xThreshold) spawnPoint = new Point(targetPos.x + xThreshold * Math.Sign(pos.x - targetPos.x), pos.y);
				new BusterPlasmaHitProj(weapon, spawnPoint, xDir, owner, owner.getNextActorNetId(), rpc: true);
			}
		}
	}
}

public class BusterPlasmaHitProj : Projectile {
	public BusterPlasmaHitProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 1, player, "buster_plasma_hit", 0, 0.25f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 2f;
		projId = (int)ProjIds.BusterX3PlasmaHit;
		destroyOnHit = false;
		netcodeOverride = NetcodeModel.FavorDefender;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}
}
