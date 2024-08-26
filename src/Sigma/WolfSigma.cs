using System;

namespace MMXOnline;

public class WolfSigma : Character {
	public WolfSigmaHead head;
	public WolfSigmaHand leftHand;
	public WolfSigmaHand rightHand;

	public WolfSigma(
		Player player, float x, float y, int xDir, bool isVisible,
		ushort? netId, bool ownedByLocalPlayer, bool isWarpIn = false
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	) { 
		charId = CharIds.WolfSigma;
	}

	public override bool isSoundCentered() {
		return false;
	}

	public override Point getCenterPos() {
		return pos.addxy(0, -7);
	}

	public override float getLabelOffY() {
		return 25;
	}

	public override void destroySelf(
		string spriteName = null, string fadeSound = null, bool disableRpc = false,
		bool doRpcEvenIfNotOwned = false, bool favorDefenderProjDestroy = false
	) {
		head?.explode();
		leftHand?.destroySelf();
		rightHand?.destroySelf();

		base.destroySelf(spriteName, fadeSound, disableRpc, doRpcEvenIfNotOwned, favorDefenderProjDestroy);
	}
}
