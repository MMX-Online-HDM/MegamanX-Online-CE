namespace MMXOnline;

public class LhHeadbutt : Weapon {
	public static LhHeadbutt netWeapon = new();

	public LhHeadbutt() : base() {
		//damager = new Damager(player, 2, 13, 0.5f);
		index = (int)WeaponIds.LigthHelmetHeadbutt;
		killFeedIndex = 64;
	}
}
