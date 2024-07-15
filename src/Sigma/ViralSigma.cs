using System;

namespace MMXOnline;

public class ViralSigma : Character {
	public string lastHyperSigmaSprite = "";
	public int lastHyperSigmaFrameIndex;
	public int lastViralSigmaAngle = 0;

	public ViralSigma(
		Player player, float x, float y, int xDir, bool isVisible,
		ushort? netId, bool ownedByLocalPlayer, bool isWarpIn = false
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	) { 
		charId = CharIds.WolfSigma;
	}
}
