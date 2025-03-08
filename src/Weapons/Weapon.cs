using System;
using System.Collections.Generic;

namespace MMXOnline;

public class Weapon {
	public static Weapon baseNetWeapon = new();
	public string[] shootSounds = { "", "", "", ""};
	public float ammo;
	public float maxAmmo;
	public float fireRate;
	public float shootCooldown;
	public float altShotCooldown;
	public float? switchCooldown;
	public float? switchCooldownFrames;
	public float soundTime = 0;
	public bool isStream = false;
	public string displayName = "";
	public string[] description = {""};
	public Damager? damager;
	public int type; // For "swappable category" weapons, like techniques, vile weapon sections, etc.

	public int streams;

	public int index;
	public int killFeedIndex;
	public int weaponBarBaseIndex;
	public int weaponBarIndex;
	public int weaponSlotIndex;
	public int weaknessIndex;
	public int vileWeight;

	public float rechargeTime;
	public float rechargeCooldown;
	public float? timeSinceLastShoot;

	// Ammo display vars.
	public bool allowSmallBar = true;
	public float ammoDisplayScale = 1;
	public float ammoDisplayScaleSmall = 2;

	// Ammo recharge vars.
	public float weaponHealAmount = 0;
	public float weaponHealTime = 0;
	public float weaponHealCount = 0;
	public float ammoGainMultiplier = 1;
	public bool canHealAmmo = true;
	
	// For double buster shenanigans.
	public bool forceDefaultXShot = false;

	// HUD related stuff.
	public bool drawCooldown = true;
	public bool drawAmmo = true;
	public bool drawRoundedDown = false;
	public bool drawGrayOnLowAmmo = false;
	public string damage = "";
	public string hitcooldown= "";
	public double ammousage;
	public string effect = "";
	public string Flinch = "";
	public string FlinchCD = "";
	public bool hasCustomChargeAnim;

	public Weapon() {
		ammo = 32;
		maxAmmo = 32;
		fireRate = 9;
		effect = "";
		damage = "0";
		hitcooldown = "0";
		Flinch = "0";
		FlinchCD = "0";
		ammousage = getAmmoUsage(0);
	}

	public Weapon(WeaponIds index, int killFeedIndex, Damager? damager = null) {
		this.index = (int)index;
		this.killFeedIndex = killFeedIndex;
		this.damager = damager;
	}

	public Weapon clone() {
		return MemberwiseClone() as Weapon ?? throw new Exception("Error while copying weapon object.");
	}

	public static List<Weapon> getAllSwitchableWeapons(AxlLoadout axlLoadout) {
		var weaponList = new List<Weapon>() {
			new GigaCrush(),
			new HyperCharge(),
			new HyperNovaStrike(),
			new DoubleBullet(),
			new DNACore(),
			new VileMissile(VileMissileType.ElectricShock),
			new VileCannon(VileCannonType.FrontRunner),
			new Vulcan(VulcanType.CherryBlast),
		};
		weaponList.AddRange(getAllXWeapons());
		weaponList.AddRange(getAllAxlWeapons(axlLoadout));
		weaponList.AddRange(getAllSigmaWeapons(null));
		return weaponList;
	}

	public static List<Weapon> getAllSigmaWeapons(Player? player, int? sigmaForm = null) {
		var weapons = new List<Weapon>()
		{
				new SigmaMenuWeapon(),
			};

		if (sigmaForm == null || sigmaForm == 0) {
			weapons.Add(new ChillPenguinWeapon(player));
			weapons.Add(new SparkMandrillWeapon(player));
			weapons.Add(new ArmoredArmadilloWeapon(player));
			weapons.Add(new LaunchOctopusWeapon(player));
			weapons.Add(new BoomerangKuwangerWeapon(player));
			weapons.Add(new StingChameleonWeapon(player));
			weapons.Add(new StormEagleWeapon(player));
			weapons.Add(new FlameMammothWeapon(player));
			weapons.Add(new VelguarderWeapon(player));
		}
		if (sigmaForm == null || sigmaForm == 1) {
			weapons.Add(new WireSpongeWeapon(player));
			weapons.Add(new WheelGatorWeapon(player));
			weapons.Add(new BubbleCrabWeapon(player));
			weapons.Add(new FlameStagWeapon(player));
			weapons.Add(new MorphMothWeapon(player));
			weapons.Add(new MagnaCentipedeWeapon(player));
			weapons.Add(new CrystalSnailWeapon(player));
			weapons.Add(new OverdriveOstrichWeapon(player));
			weapons.Add(new FakeZeroWeapon(player));
		}
		if (sigmaForm == null || sigmaForm == 2) {
			weapons.Add(new BlizzardBuffaloWeapon(player));
			weapons.Add(new ToxicSeahorseWeapon(player));
			weapons.Add(new TunnelRhinoWeapon(player));
			weapons.Add(new VoltCatfishWeapon(player));
			weapons.Add(new CrushCrawfishWeapon(player));
			weapons.Add(new NeonTigerWeapon(player));
			weapons.Add(new GravityBeetleWeapon(player));
			weapons.Add(new BlastHornetWeapon(player));
			weapons.Add(new DrDopplerWeapon(player));
		}

		return weapons;
	}

	public static List<Weapon> getAllXWeapons() {
		return new List<Weapon>()
		{
				new XBuster(),
				new HomingTorpedo(),
				new ChameleonSting(),
				new RollingShield(),
				new FireWave(),
				new StormTornado(),
				new ElectricSpark(),
				new BoomerangCutter(),
				new ShotgunIce(),
				new CrystalHunter(),
				new BubbleSplash(),
				new SilkShot(),
				new SpinWheel(),
				new SonicSlicer(),
				new StrikeChain(),
				new MagnetMine(),
				new SpeedBurner(),
				new AcidBurst(),
				new ParasiticBomb(),
				new TriadThunder(),
				new SpinningBlade(),
				new RaySplasher(),
				new GravityWell(),
				new FrostShield(),
				new TornadoFang(),
			};
	}

	public static List<Weapon> getAllAxlWeapons(AxlLoadout axlLoadout) {
		return new List<Weapon>()
		{
				new AxlBullet(),
				new RayGun(axlLoadout.rayGunAlt),
				new BlastLauncher(axlLoadout.blastLauncherAlt),
				new BlackArrow(axlLoadout.blackArrowAlt),
				new SpiralMagnum(axlLoadout.spiralMagnumAlt),
				new BoundBlaster(axlLoadout.boundBlasterAlt),
				new PlasmaGun(axlLoadout.plasmaGunAlt),
				new IceGattling(axlLoadout.iceGattlingAlt),
				new FlameBurner(axlLoadout.flameBurnerAlt),
			};
	}

	// friendlyIndex is 0-8.
	// Don't use this to generate weapons for use as they don't come with the right alt fire
	public static AxlWeapon fiToAxlWep(int friendlyIndex) {
		return friendlyIndex switch {
			1 => new RayGun(0),
			2 => new BlastLauncher(0),
			3 => new BlackArrow(0),
			4 => new SpiralMagnum(0),
			5 => new BoundBlaster(0),
			6 => new PlasmaGun(0),
			7 => new IceGattling(0),
			8 => new FlameBurner(0),
			_ => new AxlBullet()
		};
	}

	public static int wiToFi(int weaponIndex) {
		return weaponIndex switch {
			(int)WeaponIds.AxlBullet => 0,
			(int)WeaponIds.RayGun => 1,
			(int)WeaponIds.BlastLauncher => 2,
			(int)WeaponIds.BlackArrow => 3,
			(int)WeaponIds.SpiralMagnum => 4,
			(int)WeaponIds.BoundBlaster => 5,
			(int)WeaponIds.PlasmaGun => 6,
			(int)WeaponIds.IceGattling => 7,
			(int)WeaponIds.FlameBurner => 8,
			_ => 0
		};
	}

	public static List<int> getWeaponPool(bool includeBuster) {
		List<int> weaponPool;
		weaponPool = new List<int>() {
			1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24
		};
		if (includeBuster) weaponPool.Insert(0, 0);
		return weaponPool;
	}

	public static List<int> getRandomXWeapons() {
		return Helpers.getRandomSubarray(getWeaponPool(true), 3);
	}

	public static List<int> getRandomAxlWeapons() {
		List<int> weaponPool = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8 };
		List<int> selWepIndices = Helpers.getRandomSubarray(weaponPool, 2);
		return new List<int>()
		{
				0,
				selWepIndices[0],
				selWepIndices[1],
			};
	}

	// GM19:
	// Friendly reminder that this method MUST be deterministic across all clients,
	// i.e. don't vary it on a field that could vary locally.
	// Gacel:
	// Using this as a deterministic generator is a horrible idea.
	// As a lot of X's projectiles have state changes and other thing that this legacy funtion
	// cannot use at all. Better to NOT use this at all.
	// ToDo: Eventually remove this.
	public virtual void getProjectile(
		Point pos, int xDir, Player player, float chargeLevel, ushort netProjId
	) {
	}

	public virtual void vileShoot(WeaponIds weaponInput, Vile vile) {
	}

	// For melee / zero weapons, etc.
	public virtual void attack(Character character) {

	}

	// Raijingeki2, etc.
	public virtual void attack2(Character character) {

	}

	// Gacel:
	// This is to be used locally to get projectiles.
	// A replacement of the above. Remeber to send RPCs when using this one.
	public virtual void shoot(Actor actor, int[] args) {
	}
	public virtual void shoot(Character character, int[] args) {
	}
	public virtual void shootLight(Character character, int[] args) {
		shoot(character, args);
	}
	public virtual void shootSecond(Character character, int[] args) {
		shoot(character, args);
	}
	public virtual void shootMax(Character character, int[] args) {
		shoot(character, args);
	}
	public virtual void shootHypercharge(Character character, int[] args) {
		shoot(character, args);
	}

	// ToDo: Remove default values from this.
	public virtual float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) return 8;
		else return 1;
	}

	public virtual float getAmmoUsageEX(int chargeLevel, Character character) {
		return getAmmoUsage(chargeLevel);
	}

	public virtual void rechargeAmmo(float maxRechargeTime) {
		rechargeCooldown -= Global.spf;
		if (rechargeCooldown < 0) {
			rechargeCooldown = 0;
		}
		if (rechargeCooldown == 0) {
			rechargeTime += Global.spf;
			if (rechargeTime > maxRechargeTime) {
				rechargeTime = 0;
				ammo++;
				if (ammo > maxAmmo) ammo = maxAmmo;
			}
		}
	}

	public bool isCooldownPercentDone(float percent) {
		if (fireRate == 0) { return true; }
		return (shootCooldown / fireRate) <= (1 - percent);
	}

	public void addAmmo(float amount, Player player) {
		if (player.character is MegamanX mmx && mmx.hyperArmArmor == ArmorId.Max && amount < 0) amount *= 0.5f;
		ammo += amount;
		ammo = Helpers.clamp(ammo, 0, maxAmmo);
	}

	public virtual bool noAmmo() {
		return ammo <= 0;
	}

	public virtual bool canShoot(int chargeLevel, Player player) {
		return ammo > 0;
	}

	public virtual bool applyDamage(
		IDamagable victim, bool weakness, Actor actor,
		int projId, float? overrideDamage = null,
		int? overrideFlinch = null, bool sendRpc = true
	) {
		return damager?.applyDamage(
			victim, weakness, this, actor, projId,
			overrideDamage, overrideFlinch, sendRpc
		) ?? false;
	}

	public bool isCmWeapon() {
		return type > 0 && (this is AxlBullet || this is DoubleBullet 
		|| this is MettaurCrash || this is BeastKiller || this is MachineBullets
		|| this is RevolverBarrel || this is AncientGun);
	}
	
	public virtual void update() {
		Helpers.decrementFrames(ref soundTime);
		Helpers.decrementFrames(ref shootCooldown);
		Helpers.decrementFrames(ref altShotCooldown);
		if (timeSinceLastShoot != null) {
			timeSinceLastShoot += Global.speedMul;
		}
	}

	public void charLinkedUpdate(Character character, bool isAlwaysOn) {
		if (ammo >= maxAmmo || weaponHealAmount <= 0) {
			weaponHealAmount = 0f;
			weaponHealTime = 0;
			return;
		}
		weaponHealTime += 1;
		if (weaponHealTime >= 4) {
			weaponHealCount += ammoDisplayScale;
			weaponHealTime = 0;
			weaponHealAmount -= ammoDisplayScale;
			ammo = Helpers.clampMax(ammo + ammoDisplayScale, maxAmmo);
			if (weaponHealCount >= 1) {
				weaponHealCount = 0;
				if (isAlwaysOn || character.currentWeapon == this) {
					character.playAltSound("subtankFull", altParams: "aarmor");
				}
			}
		}
	}

	public void addAmmoHeal(float ammoAdd) {
		if (ammoAdd < 0 || ammo >= maxAmmo) {
			return;
		}
		weaponHealAmount += MathF.Ceiling(ammoAdd * ammoGainMultiplier);
		weaponHealAmount = Helpers.clampMax(weaponHealAmount, maxAmmo);
	}

	public void addAmmoPercentHeal(float ammoAdd) {
		if (ammoAdd < 0 || ammo >= maxAmmo) {
			return;
		}
		weaponHealAmount += MathF.Ceiling(maxAmmo * ammoAdd * ammoGainMultiplier / 100f);
		weaponHealAmount = Helpers.clampMax(weaponHealAmount, maxAmmo);
	}

	public static void gigaAttackSoundLogic(
		Actor actor, float oldAmmo, float newAmmo, float steps, float maxAmmo,
		string normalSound = "gigaCrushAmmoRecharge", string maxSound = "gigaCrushAmmoFull"
	) {
		if (oldAmmo >= newAmmo) {
			return;
		}
		float nextCharge = MathF.Ceiling(oldAmmo / steps) * steps;
		if (newAmmo >= maxAmmo) {
			actor.playSound(maxSound);
		} else {
			if (oldAmmo < nextCharge && newAmmo >= nextCharge) {
				actor.playSound(normalSound);
			}
		}
	}

	public virtual float getFireRate(Character character, int chargeLevel, int[] args) {
		return fireRate;
	}
}
