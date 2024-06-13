namespace MMXOnline;

public class ZeroShoryukenWeapon : Weapon {
	public static ZeroShoryukenWeapon staticWeapon = new();

	public ZeroShoryukenWeapon() : base() {
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		index = (int)WeaponIds.ZeroShoryuken;
		killFeedIndex = 113;
	}
}

public class DropKickWeapon : Weapon {
	public static DropKickWeapon staticWeapon = new();

	public DropKickWeapon() : base() {
		//damager = new Damager(player, 4, 12, 0.5f);
		index = (int)WeaponIds.DropKick;
		rateOfFire = 0;
		killFeedIndex = 112;
		type = (int)ZeroDownthrustType.DropKick;
	}
}

public class MegaPunchWeapon : Weapon {
	public static MegaPunchWeapon staticWeapon = new();

	public MegaPunchWeapon() : base() {
		//damager = new Damager(player, 3, Global.defFlinch, 0.06f);
		index = (int)WeaponIds.MegaPunchWeapon;
		killFeedIndex = 106;
		type = (int)GroundSpecialType.MegaPunch;
	}
}

public class PZeroParryWeapon : Weapon {
	public PZeroParryWeapon() : base() {
		rateOfFire = 0.75f;
		index = (int)WeaponIds.KKnuckleParry;
		killFeedIndex = 172;
	}
}
