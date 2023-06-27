namespace MMXOnline;

public class ProjFadeAnim : Anim {
	public Point ogVel;
	Rect drawRect;
	
	public ProjFadeAnim(
		Point pos, Point vel, string spriteName, int frameNum, int xDir, long zIndex,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		pos, spriteName, xDir, netId, false, false, ownedByLocalPlayer, null, zIndex, null, false, false
	) {
		sprite.frameIndex = frameNum;
		this.vel = vel.times(0.5f);
		ogVel = this.vel;
		drawRect = currentFrame.rect;
	}

	public override void update() {
		base.update();
		alpha = 1f - time * 15f;
		vel = ogVel.times(1f - time * 7.5f);
		if (time > Global.spf * 4) {
			destroySelf(disableRpc: true);
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		/*if (sprite == null ||
			currentFrame == null ||
			!shouldDraw() ||
			!sprite.visible
		) {
			return;
		}

		var offsetX = xDir * currentFrame.offset.x;
		var offsetY = yDir * currentFrame.offset.y;
		drawRect = currentFrame.rect;

		// Don't draw actors out of the screen for optimization
		var alignOffset = sprite.getAlignOffset(frameIndex, xDir, yDir);
		var rx = pos.x + alignOffset.x;
		var ry = pos.y + alignOffset.y;
		var rect = new Rect(rx, ry, rx + currentFrame.rect.w(), ry + currentFrame.rect.h());
		var camRect = new Rect(
			Global.level.camX, Global.level.camY,
			Global.level.camX + Global.viewScreenW,
			Global.level.camY + Global.viewScreenH
		);
		if (!rect.overlaps(camRect)) {
			return;
		}
		var drawX = pos.x + x + offsetX;
		var drawY = pos.y + y + offsetY;

		sprite.drawWindow(
			frameIndex, drawX, drawY, xDir, yDir, alpha,
			xScale, yScale, zIndex, drawRect, actor: this
		);*/
	}
}