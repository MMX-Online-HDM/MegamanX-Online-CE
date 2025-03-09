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

	public virtual void axlShoot(Player player, AxlBulletType axlBulletType = AxlBulletType.Normal, int? overrideChargeLevel = null) {
		bool isWhiteAxlCopyShot = axlBulletType == AxlBulletType.WhiteAxlCopyShot2;
		if (axlBulletType == AxlBulletType.WhiteAxlCopyShot2) axlBulletType = AxlBulletType.AltFire;

		Axl? axl = player.character as Axl;
		if (axl != null) {

			if (player.ping != null) {
				float ping = player.ping.Value;
				axl.stealthRevealTime = ping / Axl.stealthRevealPingDenom;
				if (axl.stealthRevealTime < Axl.maxStealthRevealTime) axl.stealthRevealTime = Axl.maxStealthRevealTime;
			} else {
				axl.stealthRevealTime = Axl.maxStealthRevealTime;
			}
			int chargeLevel = axlBulletType == AxlBulletType.AltFire ? 3 : axl.getChargeLevel();
			if (chargeLevel == 3 && 
				(this is AxlBullet || this is DoubleBullet || this is MettaurCrash ||
				 this is BeastKiller || this is MachineBullets || this is RevolverBarrel || this is AncientGun))
			{
				chargeLevel = axl.getChargeLevel() + 1;
			}
			if (overrideChargeLevel != null) {
				chargeLevel = overrideChargeLevel.Value;
			}
			if (axl.isWhiteAxl()) {
				if (this is AxlBullet) {
					chargeLevel += 1;
					if (chargeLevel >= 3) chargeLevel = 3;
				}
			}

			float ammoUsage = getAmmoUsage(chargeLevel) * Global.level.gameMode.getAmmoModifier() * (axl.isWhiteAxl() ? whiteAxlAmmoMod() : 1) * miscAmmoMod(axl);
			ammo -= ammoUsage;
			if (ammo < 0) ammo = 0;

			if (player.weapon.type > 0 && !axl.isWhiteAxl()) {
				if (axlBulletType == AxlBulletType.AltFire && player.weapon is not DoubleBullet) {
					for (int i = 0; i < ammoUsage; i++) {
						axl.ammoUsages.Add(0);
					}
				} else {
					for (int i = 0; i < ammoUsage; i++) {
						axl.ammoUsages.Add(1);
					}
				}
			}

			bool isCharged = chargeLevel >= 3;

			Point bulletPos = axl.getAxlBulletPos();

			Point cursorPos = axl.getCorrectedCursorPos();

			var dirTo = bulletPos.directionTo(cursorPos);
			float aimAngle = dirTo.angle;

			if (isWhiteAxlCopyShot && isSecondShot) {
				bulletPos.inc(dirTo.normalize().times(-5));
			}

			Weapon weapon = player.weapon;
			if (axlBulletType == AxlBulletType.Assassin) weapon = new AssassinBulletChar();

			axlGetProjectile(weapon, bulletPos, axl.axlXDir, player, aimAngle, axl.axlCursorTarget, axl.axlHeadshotTarget, cursorPos, chargeLevel, player.getNextActorNetId());

			string? fs = !isCharged ? flashSprite : chargedFlashSprite;
			if (this is RayGun && axlBulletType == AxlBulletType.AltFire && player.axlLoadout.rayGunAlt == 0) fs = "";
			if (!string.IsNullOrEmpty(fs)) {
				if (fs == "axl_raygun_flash" && Global.level.gameMode.isTeamMode && player.alliance == GameMode.redAlliance) fs = "axl_raygun_flash2";
				axl.muzzleFlash.changeSprite(fs, true);
				axl.muzzleFlash.sprite.restart();
			}
			// Shoot sound.
			string soundToPlay = !isCharged ? shootSounds[0] : shootSounds[3];
			if (axlBulletType == AxlBulletType.AltFire && !isCharged) {
				soundToPlay = "axlBullet";
			}
			if (soundToPlay != "") {
				axl.playSound(soundToPlay);
			}

			float rateOfFireMode = (axl.isWhiteAxl() ? whiteAxlFireRateMod() : 1);
			shootCooldown = fireRate / rateOfFireMode;

			if (axlBulletType == AxlBulletType.AltFire) {
				altShotCooldown = altFireCooldown / rateOfFireMode;
			}

			float switchCooldown = 0.3f;
			float slowSwitchCooldown = 0.6f;

			axl.switchTime = switchCooldown;
			axl.altSwitchTime = switchCooldown;
			if (shootCooldown > 0.25f || altShotCooldown > 0.25f) {
				axl.switchTime = slowSwitchCooldown;
				axl.altSwitchTime = slowSwitchCooldown;
			}

			float aimBackwardsAmount = axl.getAimBackwardsAmount();
			shootCooldown *= (1 + aimBackwardsAmount * 0.25f);
			altShotCooldown *= (1 + aimBackwardsAmount * 0.25f);

			isSecondShot = !isSecondShot;
		}
	}

	public virtual void axlGetProjectile(
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
}
