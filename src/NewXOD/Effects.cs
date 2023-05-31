namespace MMXOnline;

public class ProjFadeAnim : Anim {
	public Point ogVel;
	
	public ProjFadeAnim(
		Point pos, Point vel, string spriteName, int frameNum, int xDir, long zIndex,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		pos, spriteName, xDir, netId, false, false, ownedByLocalPlayer, null, zIndex, null, false, false
	) {
		sprite.frameIndex = frameNum;
		this.vel = vel.times(0.5f);
		ogVel = this.vel;
	}

	public override void update() {
		base.update();
		alpha = 1f - time * 10f;
		vel = ogVel.times(1f - time * 10f);
		if (time > 0.1) {
			destroySelf(disableRpc: true);
		}
	}
}