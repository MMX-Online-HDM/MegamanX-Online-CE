namespace MMXOnline;

public class SubTank {
	public float health;
	public const float maxHealth = 16;
	public bool isInUse;
	public Player player;
	public SubTank() {
	}

	public void use(Character character) {
		character.addHealth(health);
		character.usedSubtank = this;
		RPC.useSubtank.sendRpc(character.netId, (int)health);
	}

	public void use(Maverick maverick) {
		maverick.addHealth(health, false);
		maverick.usedSubtank = this;
		RPC.useSubtank.sendRpc(maverick.netId, (int)health);
	}
}
