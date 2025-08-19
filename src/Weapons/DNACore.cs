using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MMXOnline;

public class UndisguiseWeapon : AxlWeapon {
	public UndisguiseWeapon() : base(0) {
		ammo = 0;
		index = (int)WeaponIds.Undisguise;
		weaponSlotIndex = 50;
		sprite = "axl_arm_pistol";
		drawAmmo = false;
		drawCooldown = false;

		drawAmmo = false;
		drawCooldown = false;
	}
}

public enum DNACoreHyperMode {
	None,
	VileMK2,
	VileMK5,
	BlackZero,
	WhiteAxl,
	AwakenedZero,
	NightmareZero,
	UltimateArmor,
}

public class DNACore : AxlWeapon {
	public int charNum;
	public LoadoutData loadout;
	public string name;
	public int alliance;
	[JsonIgnore] public int hyperModeTimer;
	[JsonIgnore] public byte hyperArmorBools;
	[JsonIgnore] public DNACoreHyperMode hyperMode;
	[JsonIgnore] public bool frozenCastle;
	[JsonIgnore] public bool speedDevil;
	[JsonIgnore] public bool ultimateArmor;
	[JsonIgnore] public float altCharAmmo;
	[JsonIgnore] public float hyperNovaAmmo;
	[JsonIgnore] public int armorFlag;
	[JsonIgnore] public float maxHealth;
	[JsonIgnore] public List<Weapon> weapons = [];
	[JsonIgnore] public bool usedOnce = false;

	public DNACore(Character character, Player player) : base(0) {
		charNum = (int)character.charId;
		loadout = (
			character.player.atransLoadout?.clone(player.id) ?? character.player.loadout.clone(player.id)
		);
		maxHealth = (float)Math.Ceiling(character.maxHealth);
		name = character.player.name;
		alliance = character.player.alliance;

		if (character is MegamanX mmx) {
			loadout.xLoadout = mmx.loadout.clone();
			weapons = loadout.xLoadout.getWeaponsFromLoadout(character.player);
			armorFlag = mmx.getArmorByte();
			ultimateArmor = mmx.hasUltimateArmor;
			if (mmx.hasUltimateArmor) {
				hyperMode = DNACoreHyperMode.UltimateArmor;
			}
			hyperArmorBools = Helpers.boolArrayToByte([
				mmx.hyperChestActive,
				mmx.hyperArmActive,
				mmx.hyperLegActive,
				mmx.hyperHelmetActive,
			]);
		}
		else if (character is Zero zero) {
			loadout.zeroLoadout = zero.loadout.clone();
			altCharAmmo = zero.gigaAttack.ammo;
			if (zero.hypermodeActive()) {
				hyperMode = zero.hyperMode switch {
					1 => DNACoreHyperMode.AwakenedZero,
					2 => DNACoreHyperMode.NightmareZero,
					_ => DNACoreHyperMode.BlackZero
				};
			}
		}
		else if (character is PunchyZero pzero) {
			loadout.pzeroLoadout = pzero.loadout.clone();
			altCharAmmo = pzero.gigaAttack.ammo;
			if (pzero.isBlack || pzero.isAwakened || pzero.isViral) {
				hyperMode = pzero.hyperMode switch {
					1 => DNACoreHyperMode.AwakenedZero,
					2 => DNACoreHyperMode.NightmareZero,
					_ => DNACoreHyperMode.BlackZero
				};
			}
		} else if (character is BusterZero bzero) {
			if (bzero.isBlackZero) {
				hyperMode = DNACoreHyperMode.BlackZero;
			}
		}
		else if (character is Axl axl) {
			loadout.axlLoadout = axl.loadout.clone();
			weapons = loadout.axlLoadout.getWeaponsFromLoadout();
			if (weapons.Count > 0 && character.player.axlBulletType > 0) {
				weapons[0] = character.player.getAxlBulletWeapon();
			}
		}
		else if (character is BaseSigma baseSigma) {
			loadout.sigmaLoadout = baseSigma.loadout.clone();
			loadout.sigmaLoadout.commandMode = (int)MaverickModeId.Puppeteer;
			if (character is CmdSigma cmdSigma) {
				cmdSigma.ballWeapon.ammo = altCharAmmo;
			} else if (character is NeoSigma neoSigma) {
				neoSigma.gigaAttack.ammo = altCharAmmo;
			}
		} else if (character is Vile vile) {
			loadout.vileLoadout = vile.loadout.clone();;
			frozenCastle = vile.hasFrozenCastle;
			speedDevil = vile.hasSpeedDevil;
		}
		// For any hyper modes added here.
		// be sure to de-apply them if "preserve undisguise" is used in: axl.updateDisguisedAxl()
		if (character.sprite.name.Contains("vilemk2")) {
			hyperMode = DNACoreHyperMode.VileMK2;
		} else if (character.sprite.name.Contains("vilemk5")) {
			hyperMode = DNACoreHyperMode.VileMK5;
		} else if (character is Axl axl && axl.isWhiteAxl()) {
			hyperMode = DNACoreHyperMode.WhiteAxl;
		}
		fireRate = 60;
		index = (int)WeaponIds.DNACore;
		weaponBarBaseIndex = 30 + charNum;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 30 + charNum;
		if (charNum == (int)CharIds.Sigma ||
			charNum == (int)CharIds.WolfSigma ||
			charNum == (int)CharIds.ViralSigma ||
			charNum == (int)CharIds.KaiserSigma
		) {
			weaponSlotIndex = 65;
		}
		if (charNum == (int)CharIds.BusterZero || charNum == (int)CharIds.PunchyZero) {
			weaponSlotIndex = 31;
		}

		sprite = "axl_arm_pistol";
		drawAmmo = false;
		drawCooldown = false;
	}

	public DNACore() : base(0) {
		charNum = 0;
		name = "error";
		loadout = null!;
		fireRate = 60;
		index = (int)WeaponIds.DNACore;
		weaponBarBaseIndex = 30 + charNum;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 30 + charNum;
		if (charNum == 4) weaponSlotIndex = 65;
		sprite = "axl_arm_pistol";
	}

	public override void axlShoot(
		Player player, AxlBulletType axlBulletType = AxlBulletType.Normal,
		int? overrideChargeLevel = null
	) {
		if (!player.ownedByLocalPlayer || player.character is not Axl axl) {
			return;
		}
		bool oldATrans = Global.level.server?.customMatchSettings?.oldATrans == true;

		if (axl.flag != null) {
			Global.level.gameMode.setHUDErrorMessage(player, "Cannot transform with flag");
			return;
		}
		if (!oldATrans && (!Global.level.isHyperMatch() && (axl.isWhiteAxl() || axl.isStealthMode()))) {
			Global.level.gameMode.setHUDErrorMessage(player, "Cannot transform as Hyper Axl");
			return;
		}
		if (oldATrans || !usedOnce) {
			if (player.currency < 1) {
				Global.level.gameMode.setHUDErrorMessage(player, "Transformation requires 1 Metal");
				return;
			}
			player.currency--;
		}

		player.lastDNACore = this;
		player.lastDNACoreIndex = axl.weaponSlot;
		player.savedDNACoreWeapons.Remove(this);
		if (oldATrans) {
			axl.weapons.RemoveAt(player.weaponSlot);
		}
		player.preTransformedChar = player.character;
		player.startAtransMain(this, player.getNextATransNetId());
		player.character.playSound("transform", sendRpc: true);
		player.character.undisguiseTime = 6;
	}
}
