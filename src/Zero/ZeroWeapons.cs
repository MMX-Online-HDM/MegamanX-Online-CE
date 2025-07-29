using System;

namespace MMXOnline;

public enum ZeroAttackLoadoutType {
	ZSaber,
	KKnuckle,
	ZBuster,
}

public class ZSaber : Weapon {
	public static ZSaber staticWeapon = new();

	public ZSaber() : base() {
		//damager = new Damager(player, 3, 0, 0.5f);
		index = (int)WeaponIds.ZSaber;
		weaponBarBaseIndex = 21;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 48;
		killFeedIndex = 9;
		type = (int)ZeroAttackLoadoutType.ZSaber;
		displayName = "Z-Saber";
		description = new string[] { "Zero's trusty beam saber." };

		drawAmmo = false;
		drawCooldown = false;
	}
}

public class ShippuugaWeapon : Weapon {
	public static ShippuugaWeapon staticWeapon = new();

	public ShippuugaWeapon() : base() {
		//damager = new Damager(player, 2, Global.defFlinch, 0.5f);
		index = (int)WeaponIds.Shippuuga;
		weaponBarBaseIndex = 21;
		killFeedIndex = 39;
		damage = "2";
		flinch = "13";
		effect = "None.";		
	}
}

public class KKnuckleWeapon : Weapon {
	public KKnuckleWeapon() : base() {
		//damager = new Damager(player, 3, Global.defFlinch, 0.25f);
		index = (int)WeaponIds.KKnuckle;
		weaponBarBaseIndex = 21;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 120;
		killFeedIndex = 106;
		type = (int)ZeroAttackLoadoutType.KKnuckle;
		displayName = "K-Knuckle";
		description = new string[] { "Use your fists to teach foes a lesson." };

		drawAmmo = false;
		drawCooldown = false;;
	}
}

public class Shingetsurin : Weapon {
	public static Shingetsurin netWeapon = new();

	public Shingetsurin() : base() {
		index = (int)WeaponIds.Shingetsurin;
		killFeedIndex = 85;
		damage = "2";
		flinch = "26";
		hitcooldown = "0.5";
		effect = "Homing Projectile, Time on screen: 3 seconds.";
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
	}
}

public class ZSaberProjSwing : Weapon {
	public ZSaberProjSwing() : base() {
		index = (int)WeaponIds.ZSaberProjSwing;
		killFeedIndex = 9;
		damage = "3";
		hitcooldown = "0.5";
		flinch = "26";
		effect = "None.";
		//damager = new Damager(player, 3, Global.defFlinch, 0.5f);
	}
}


public class RaijingekiWeapon : Weapon {
	public static RaijingekiWeapon staticWeapon = new();

	public RaijingekiWeapon() : base() {
		//damager = new Damager(player, 2, Global.defFlinch, 0.06f);
		index = (int)WeaponIds.Raijingeki;
		weaponBarBaseIndex = 22;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 10;
		type = (int)GroundSpecialType.Raijingeki;
		displayName = "Raijingeki";
		description = new string[] { "Powerful lightning attack." };
		damage = "2";
		hitcooldown = "0.06";
		flinch = "26";
		effect = "Ignores Defense.";
	}

	public static Weapon getWeaponFromIndex(int index) {
		return index switch {
			(int)GroundSpecialType.Raijingeki => new RaijingekiWeapon(),
			(int)GroundSpecialType.Suiretsusen => new SuiretsusenWeapon(),
			(int)GroundSpecialType.TBreaker => new TBreakerWeapon(),
			_ => throw new Exception("Invalid Zero air special weapon index!")
		};
	}

	public override void attack(Character character) {
		character.changeState(new Raijingeki(false), true);
	}

	public override void attack2(Character character) {
		character.changeState(new Raijingeki(true), true);
	}
}

public class Raijingeki2Weapon : Weapon {
	public static Raijingeki2Weapon staticWeapon = new();

	public Raijingeki2Weapon() : base() {
		//damager = new Damager(player, 2, Global.defFlinch, 0.06f);
		index = (int)WeaponIds.Raijingeki2;
		weaponBarBaseIndex = 40;
		killFeedIndex = 35;
		damage = "2";
		hitcooldown = "0.06";
		flinch = "26";
		effect = "Ignores Armor Defense.";
	}
}

public class TBreakerWeapon : Weapon {
	public static TBreakerWeapon staticWeapon = new();

	public TBreakerWeapon() : base() {
		//damager = new Damager(player, 3, Global.defFlinch, 0.06f);
		index = (int)WeaponIds.TBreaker;
		killFeedIndex = 107;
		type = (int)GroundSpecialType.TBreaker;
		displayName = "T-Breaker";
		description = new string[] { "A mighty hammer that can shatter barriers." };
		damage = "6";
		hitcooldown = "0.5";
		flinch = "26";
		effect = "Knocks Down the grounded enemy.";
	}

	public override void attack(Character character) {
		character.changeState(new TBreakerState(false), true);
	}

	public override void attack2(Character character) {
		character.changeState(new TBreakerState(true), true);
	}
}

public class SuiretsusenWeapon : Weapon {
	public static SuiretsusenWeapon staticWeapon = new();

	public SuiretsusenWeapon() : base() {
		//damager = new Damager(player, 3, Global.defFlinch, 0.06f);
		index = (int)WeaponIds.Suiretsusen;
		killFeedIndex = 110;
		type = (int)GroundSpecialType.Suiretsusen;
		displayName = "Suiretsusen";
		description = new string[] { "Water element glaive with good reach." };
		damage = "6";
		hitcooldown = "0.75";
		effect = "None.";
	}

	public override void attack(Character character) {
		character.changeState(new SuiretsusanState(false), true);
	}

	public override void attack2(Character character) {
		character.changeState(new SuiretsusanState(true), true);
	}
}

public class AwakenedAura : Weapon {
	public AwakenedAura() : base() {
		index = (int)WeaponIds.AwakenedAura;
		killFeedIndex = 87;
		//damager = new Damager(player, 2, 0, 0.5f);
		damage = "2/4";
		hitcooldown = "0.5";
		effect = "Passive AAura that damages the enemy.";
	}
}

public class Genmu : Weapon {
	public static Genmu netWeapon = new();

	public Genmu() : base() {
		index = (int)WeaponIds.Gemnu;
		killFeedIndex = 84;
	}
}
