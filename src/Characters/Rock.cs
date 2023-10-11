namespace MMXOnline;

public class Rock : Character {
	public Rock(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	) {

	}

	public override bool canDash() {
		return false;
	}

	public override bool canWallClimb() {
		return false;
	}

	public override string getSprite(string spriteName) {
		return "rock_" + spriteName;
	}
}
