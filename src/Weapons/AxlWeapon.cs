namespace MMXOnline;

public enum AxlBulletType {
	Normal,
	AltFire,
	Assassin,
	WhiteAxlCopyShot2
}

public class AxlWeapon : Weapon {
	public int altFire;
	public float altFireCooldown;
	public string? flashSprite;
	public string? chargedFlashSprite;
	public string? sprite;
	public bool isAxlBullets;
	public bool isCMWeapons => isAxlBullets && this is not AxlBullet;

	public AxlWeapon(int altFire) {
		this.altFire = altFire;
	}

	public bool isTwoHanded(bool includeDoubleBullet) {
		if (includeDoubleBullet && this is DoubleBullet) return true;
		return this is BlastLauncher || this is IceGattling || this is FlameBurner;
	}
	public bool isSecondShot;

	public override void update() {
		base.update();
	}

	public virtual float whiteAxlFireRateMod() {
		return 1;
	}

	public virtual float whiteAxlAmmoMod() {
		return 1;
	}

	public virtual float miscAmmoMod(Character character) {
		return 1;
	}
	public void stealthReveal(Character character) {
		if (character is not Axl axl) return;
		if (character.player.ping != null) {
			float ping = character.player.ping.Value;
			axl.stealthRevealTime = ping / Axl.stealthRevealPingDenom;
			if (axl.stealthRevealTime < Axl.maxStealthRevealTime) axl.stealthRevealTime = Axl.maxStealthRevealTime;
		} else {
			axl.stealthRevealTime = Axl.maxStealthRevealTime;
		}
	}
	

	public virtual void axlShoot(Character character, int[] args) {
		if (character is not Axl axl) return;
		if (shootCooldown > 0) return;
		int chargeLevel = args[0];

		float rateOfFireMode = (axl.isWhiteAxl() ? whiteAxlFireRateMod() : 1);
		shootCooldown = getFireRate(axl, 0, []) / rateOfFireMode;

		float ammoUsage = getAmmoUsage(0);
		ammo -= ammoUsage;
		if (ammo < 0) ammo = 0;

		if (type > 0 && !axl.isWhiteAxl()) 
			for (int i = 0; i < ammoUsage; i++)
				axl.ammoUsages.Add(1);

		Point bulletPos = axl.getAxlBulletPos();
		Point cursorPos = axl.getCorrectedCursorPos();
		var dirTo = bulletPos.directionTo(cursorPos);
		float aimAngle = dirTo.angle;

		axlGetProjectile(
			this, bulletPos, axl.axlXDir, character.player, aimAngle, axl.axlCursorTarget,
			axl.axlHeadshotTarget, cursorPos, chargeLevel, character.player.getNextActorNetId()
		);

		string? fs = flashSprite;
		if (!string.IsNullOrEmpty(fs)) {
			if (fs == "axl_raygun_flash" && Global.level.gameMode.isTeamMode &&
			 character.player.alliance == GameMode.redAlliance) 
				fs = "axl_raygun_flash2";
			axl.muzzleFlash.changeSprite(fs, true);
			axl.muzzleFlash.sprite.restart();
		}

		// Shoot sound.
		string soundToPlay = shootSounds[0];
		if (soundToPlay != "") axl.playSound(soundToPlay);

		float aimBackwardsAmount = axl.getShootBackwardsDebuff();
		shootCooldown *= (1 + aimBackwardsAmount * 0.25f);

		rechargeAmmoCustomSettingAxl = rechargeAmmoCooldown;

		stealthReveal(character);
		axl.afterAxlShoot(this);
		axl.recoilTime = 0.2f;
	}
	public virtual void axlAltShoot(Character character, int[] args) {
		if (character is not Axl axl) return;
		if (altShotCooldown > 0) return;
		int chargeLevel = args[0];

		float rateOfFireMode = (axl.isWhiteAxl() ? whiteAxlFireRateMod() : 1);
		altShotCooldown = altFireCooldown / rateOfFireMode;

		float ammoUsage = getAmmoUsage(3);
		ammo -= ammoUsage;
		if (ammo < 0) ammo = 0;

		Point bulletPos = axl.getAxlBulletPos();
		Point cursorPos = axl.getCorrectedCursorPos();
		var dirTo = bulletPos.directionTo(cursorPos);
		float aimAngle = dirTo.angle;		

		axlGetAltProjectile(
			this, bulletPos, axl.axlXDir, character.player, aimAngle, axl.axlCursorTarget,
			axl.axlHeadshotTarget, cursorPos, chargeLevel, character.player.getNextActorNetId()
		);

		string? fs = chargedFlashSprite;
		if (!string.IsNullOrEmpty(fs)) {
			if (fs == "axl_raygun_flash" && Global.level.gameMode.isTeamMode &&
			 character.player.alliance == GameMode.redAlliance) 
				fs = "axl_raygun_flash2";
			axl.muzzleFlash.changeSprite(fs, true);
			axl.muzzleFlash.sprite.restart();
		}

		// Shoot sound.
		string soundToPlay = shootSounds[3];
		if (soundToPlay != "") axl.playSound(soundToPlay);

		float aimBackwardsAmount = axl.getShootBackwardsDebuff();
		altShotCooldown *= (1 + aimBackwardsAmount * 0.25f);

		rechargeAmmoCustomSettingAxl2 = altRechargeAmmoCooldown;

		stealthReveal(character);
		axl.afterAxlShoot(this);
		axl.recoilTime = 0.2f;
    }

	public virtual void axlGetProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		 IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
	}

	public virtual void axlGetAltProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		 IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
	}

	public float axlRechargeTime;
	public virtual void rechargeAxlBulletAmmo(Player player, Axl axl, bool shootHeld, float modifier) {
		if (shootCooldown == 0 && axl.shootAnimTime == 0 && !shootHeld && ammo < maxAmmo) {
			float waMod = axl.isWhiteAxl() ? 0 : 1;
			axlRechargeTime += Global.spf;
			if (axlRechargeTime > 0.1f * modifier * waMod) {
				axlRechargeTime = 0;

				bool canAddAmmo = true;
				if (type > 0) {
					int lastAmmoUsage = 1;
					if (axl.ammoUsages.Count > 0) {
						lastAmmoUsage = axl.ammoUsages.Pop();
					}

					if (lastAmmoUsage > 0) {
						float maxAmmo = player.axlBulletTypeAmmo[type];
						maxAmmo = Helpers.clampMin0(maxAmmo - 1);
						player.axlBulletTypeAmmo[type] = maxAmmo;
						if (maxAmmo <= 0) canAddAmmo = false;
					}
				}
				if (canAddAmmo) {
					addAmmo(1, player);
				}
			}
		}
	}
	public virtual void rechargeAmmoCustomSetting(Player player, Axl axl, bool shootHeld, float modifier, float ammo) {
		if (rechargeAmmoCustomSettingAxl <= 0 && rechargeAmmoCustomSettingAxl2 <= 0 && axl.shootAnimTime == 0 && !shootHeld && ammo < maxAmmo) {
			float waMod = axl.isWhiteAxl() ? 0 : 1;
			axlRechargeTime += Global.spf;
			if (axlRechargeTime > 0.1f * modifier * waMod) {
				axlRechargeTime = 0;
				bool canAddAmmo = true;
				if (type > 0) {
					int lastAmmoUsage = 1;
					if (axl.ammoUsages.Count > 0) {
						lastAmmoUsage = axl.ammoUsages.Pop();
					}
					if (lastAmmoUsage > 0) {
						float maxAmmo = player.axlBulletTypeAmmo[type];
						maxAmmo = Helpers.clampMin0(maxAmmo - 1);
						player.axlBulletTypeAmmo[type] = maxAmmo;
						if (maxAmmo <= 0) canAddAmmo = false;
					}
				}
				if (canAddAmmo) {
					addAmmo(ammo, player);
				}
			}
		}
	}
}
