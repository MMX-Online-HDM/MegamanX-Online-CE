using System;
using System.Collections.Generic;

namespace MMXOnline;

public class UndisguiseWeapon : AxlWeapon {
	public UndisguiseWeapon() : base(0) {
		ammo = 0;
		index = (int)WeaponIds.Undisguise;
		weaponSlotIndex = 50;
		sprite = "axl_arm_pistol";
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
}

public class DNACore : AxlWeapon {
	public int charNum;
	public LoadoutData loadout;
	public float maxHealth;
	public string name;
	public int alliance;
	public int armorFlag;
	public byte hyperArmorBools;
	public bool frozenCastle;
	public bool speedDevil;
	public bool ultimateArmor;
	public DNACoreHyperMode hyperMode;
	public float rakuhouhaAmmo;
	public List<Weapon> weapons = new List<Weapon>();
	public bool usedOnce = false;

	public DNACore(Character character) : base(0) {
		charNum = (int)character.charId;
		loadout = character.player.atransLoadout ?? character.player.loadout;
		maxHealth = (float)Math.Ceiling(character.maxHealth);
		name = character.player.name;
		alliance = character.player.alliance;

		if (charNum == (int)CharIds.RagingChargeX) {
			charNum = (int)CharIds.X;
		}
		if (character is MegamanX mmx) {
			weapons = loadout.xLoadout.getWeaponsFromLoadout(character.player);
			armorFlag = mmx.getArmorByte();
			ultimateArmor = mmx.hasUltimateArmor;
			hyperArmorBools = Helpers.boolArrayToByte([
				mmx.hyperChestActive,
				mmx.hyperArmActive,
				mmx.hyperLegActive,
				mmx.hyperHelmetActive,
			]);
		}
		else if (character is Zero zero) {
			rakuhouhaAmmo = zero.gigaAttack.ammo;
			if (zero.hypermodeActive()) {
				hyperMode = zero.hyperMode switch {
					1 => DNACoreHyperMode.AwakenedZero,
					2 => DNACoreHyperMode.NightmareZero,
					_ => DNACoreHyperMode.BlackZero
				};
			}
		}
		else if (character is PunchyZero pzero) {
			rakuhouhaAmmo = pzero.gigaAttack.ammo;
			if (pzero.isBlack || pzero.isAwakened || pzero.isViral) {
				hyperMode = pzero.hyperMode switch {
					1 => DNACoreHyperMode.AwakenedZero,
					2 => DNACoreHyperMode.NightmareZero,
					_ => DNACoreHyperMode.BlackZero
				};
			}
		}
		else if (character is Axl) {
			weapons = loadout.axlLoadout.getWeaponsFromLoadout();
			if (weapons.Count > 0 && character.player.axlBulletType > 0) {
				weapons[0] = character.player.getAxlBulletWeapon();
			}
		}
		else if (character is BaseSigma) {
			rakuhouhaAmmo = character.player.sigmaAmmo;
		}
		else if (character is Vile vile) {
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
			weaponBarBaseIndex = 31;
		}
		sprite = "axl_arm_pistol";
		drawAmmo = false;
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
		if (!player.ownedByLocalPlayer) {
			return;
		}
		player.lastDNACore = this;
		player.lastDNACoreIndex = player.weaponSlot;
		player.savedDNACoreWeapons.Remove(this);
		player.weapons.RemoveAt(player.weaponSlot);
		player.preTransformedAxl = player.character;
		Global.level.removeGameObject(player.preTransformedAxl);
		player.transformAxl(this, player.getNextATransNetId());
	}
}
