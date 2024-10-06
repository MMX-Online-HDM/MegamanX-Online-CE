using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;

public class Buster : Weapon {
	public List<BusterProj> lemonsOnField = new List<BusterProj>();
	public bool isUnpoBuster;

	public Buster() : base() {
		index = (int)WeaponIds.Buster;
		killFeedIndex = 0;
		weaponBarBaseIndex = 0;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 0;
		shootSounds = new string[] { "", "", "", "" };
		rateOfFire = 0.15f;
		canHealAmmo = false;
		drawAmmo = false;
		drawCooldown = false;
		effect = "You only need this to win any match.";
		hitcooldown = "0/0/0/1";
		damage = "1/2/3/4";
		Flinch = "0/0/13/26";
		FlinchCD = "0";
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
		bool hasUltArmor = ((player.character as MegamanX)?.hasUltimateArmor == true);
		bool isHyperX = ((player.character as MegamanX)?.isHyperX == true);

		if (isHyperX && chargeLevel > 0) {
			new BusterUnpoProj(this, pos, xDir, player, netProjId);
			new Anim(pos, "buster_unpo_muzzle", xDir, null, true);
			shootSound = "stockBuster";
		} else if (mmx.stockedX3Buster) {
			if (player.ownedByLocalPlayer) {
				if (player.character.charState is not WallSlide) shootTime = 0;			
				player.character.changeState(new X3ChargeShot(null), true);
				shootSound = "";
			}
		} else if (mmx.stockedCharge) {
			if (player.ownedByLocalPlayer) {
				if (player.character.charState is not WallSlide) shootTime = 0;
				player.character.changeState(new X2ChargeShot(1), true);
			}
		} else {
			switch (chargeLevel) {
				case 0: //Lemon
					lemonsOnField.Add(new BusterProj(this, pos, xDir, 0, player, netProjId));
					break;
				case 1: //LV 1 Green Buster
					new Buster2Proj(this, pos, xDir, player, netProjId);
					break;
				case 2: //LV2 Multiple Busters
					new Buster3Proj(this, pos, xDir, 0, player, netProjId);
					break;
				case >=3: //LV4 Busters
					//UAX
					if (hasUltArmor && !player.hasArmArmor(3)) {
						if (player.hasArmArmor(2)) {
							if (player.ownedByLocalPlayer) 
							player.character.changeState(new X2ChargeShot(2), true);
							shootSound = "";
						} 
						else {
							new Anim(pos.clone(), "buster4_muzzle_flash", xDir, null, true);
							new BusterPlasmaProj(this, pos, xDir, player, netProjId);
							shootSound = "plasmaShot";
						}
					}
					//Naked or Light Arm Armor
					else if (player.hasArmArmor(0) || player.hasArmArmor(1)) {
						new Anim(pos.clone(), "buster4_muzzle_flash", xDir, null, true);
						//Create the buster effect
						int xOff = xDir * -5;
						player.setNextActorNetId(netProjId);
						// Create first line instantly.
						createBuster4Line(pos.x + xOff, pos.y, xDir, player, 0f); // This one is Buster4Proj
						// Create 2nd with a delay.
						Global.level.delayedActions.Add(new DelayedAction(delegate {
							createBuster4Line(pos.x + xOff, pos.y, xDir, player, 10f / 60f);
						}, 2.8f / 60f));
						// Use smooth spawn on the 3rd.
						Global.level.delayedActions.Add(new DelayedAction(delegate {
							createBuster4Line(pos.x + xOff, pos.y, xDir, player, 5f / 60f, true);
						}, 5.8f / 60f));
					}
					//Second Arm Armor
					else if (player.hasArmArmor(2)) {
						if (player.ownedByLocalPlayer) {
							if (player.character.charState is not WallSlide) shootTime = 0;			
							player.character.changeState(new X2ChargeShot(0), true);
							shootSound = "";
						}
					} 
					//Max Arm Armor
					else if (player.hasArmArmor(3)) {
						if (player.ownedByLocalPlayer) {
							if (player.character.charState is not WallSlide) shootTime = 0;
							player.character.changeState(new X3ChargeShot(null), true);
							shootSound = "";
						}
					}
					break;
			}
		}
		if (player?.character?.ownedByLocalPlayer == true && shootSound != "") {
			player.character.playSound(shootSound, sendRpc: true);
		}
	}
	public static bool isNormalBuster(Weapon weapon) {
		return weapon is Buster buster && !buster.isUnpoBuster;
	}

	public static bool isWeaponUnpoBuster(Weapon weapon) {
		return weapon is Buster buster && buster.isUnpoBuster;
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

	public override float getAmmoUsage(int chargeLevel) {
		if (isUnpoBuster) {
			return 3;
		}
		return 0;
	}
	public void setUnpoBuster(MegamanX mmx) {
		isUnpoBuster = true;
		rateOfFire = 0.75f;
		weaponBarBaseIndex = 70;
		weaponBarIndex = 59;
		weaponSlotIndex = 121;
		killFeedIndex = 180;

		// Ammo variables
		maxAmmo = 12;
		ammo = maxAmmo;
		allowSmallBar = false;
		ammoGainMultiplier = 2;
		canHealAmmo = true;
		drawRoundedDown = true;
		
		// HUD.
		drawAmmo = true;
		drawCooldown = true;
		
		// Remove charge.
		mmx.stockedCharge = false;
		mmx.stockedX3Buster = false;
		mmx.stockedXSaber = false;
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
