namespace MMXOnline;

public class AwakenedAura : Weapon {
	public AwakenedAura(Player player) : base() {
		index = (int)WeaponIds.AwakenedAura;
		killFeedIndex = 87;
		damager = new Damager(player, 2, 0, 0.5f);
	}

	public AwakenedAura() : base() {
		index = (int)WeaponIds.AwakenedAura;
		killFeedIndex = 87;
	}
}
