using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class HexaInvoluteWeapon : Weapon {
	public static HexaInvoluteWeapon netWeapon = new();

	public HexaInvoluteWeapon() {
		index = (int)WeaponIds.HexaInvolute;
		killFeedIndex = 179;
	}
}

public class HexaInvoluteState : VileState {
	public HexaInvoluteProj? proj;
	public bool startGrounded;
	public float moveTime;
	public float ammoTime;
	public bool shot;

	public HexaInvoluteState() : base("super") {
		superArmor = true;
		pushImmune = true;
		invincible = true;
	}

	public override void update() {
		base.update();

		if (startGrounded && moveTime <= 16) {
			character.moveXY(0, -2);
			moveTime += character.speedMul;
		}

		if (!shot && character.frameIndex >= 2) {
			shot = true;
			proj = new HexaInvoluteProj(
				character.getCenterPos(),
				character.xDir, character, player.getNextActorNetId(),
				sendRpc: true
			);
		}

		if (proj != null) {
			vile.usedAmmoLastFrame = true;
			Helpers.decrementTime(ref ammoTime);
			if (ammoTime == 0) {
				ammoTime = 0.125f;
				vile.addAmmo(-1);
			}
		}

		if (vile.energy.ammo <= 0 || (player.input.isPressed(Control.Special1, player) && stateFrames >= 60)) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (character.grounded) {
			startGrounded = true;
		}
		character.clenaseDmgDebuffs();
		character.stopMovingS();
		vile.vileHoverTime = vile.vileMaxHoverTime;
		vile.getOffMK5Platform();
		vile.useGravity = false;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		proj?.destroySelf();
		vile.useGravity = true;
		vile.usedAmmoLastFrame = false;
	}
}

public class HexaInvolutePart {
	public float time;
	public Point pos;
	public float angle;
	public float baseRadius;
	public float maxTime = 0.15f;

	public HexaInvolutePart(Point pos) {
		time = 0;
		angle = Helpers.randomRange(0, 360);
		Point randInc = Point.createFromAngle(angle).times(Helpers.randomRange(0, 10));
		this.pos = pos.add(randInc);
		baseRadius = Helpers.randomRange(0.1f, 0.5f);
	}

	public float getAlpha() {
		return 1 - 0.5f * (time / maxTime);
	}

	public float getRadius() {
		float timeProcess = (time / maxTime);
		return baseRadius * (1 + timeProcess);
	}

	public Point getPos() {
		float partX = pos.x + (Helpers.cosd(angle) * time * 50);
		float partY = pos.y + (Helpers.sind(angle) * time * 50);
		return new Point(partX, partY);
	}
}

public class HexaInvoluteProj : Projectile {
	public Anim? muzzle;
	public int hitboxNum = 10;
	public float radius = 128;
	public SoundWrapper? sound;
	public float soundCooldown;
	public List<HexaInvolutePart> particles = [];
	public float partCooldown;
	public Point[] beamDest = new Point[6];

	public HexaInvoluteProj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "empty", netId, altPlayer
	) {
		damager.damage = 1;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 9;
		projId = (int)ProjIds.HexaInvolute;
		zIndex = ZIndex.Backwall;
		setIndestructableProperties();
		sprite.hitboxes = popullateHitboxes();
		for (int i = 0; i < beamDest.Length; i++) {
			beamDest[i] = pos;
		}

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	
	public static Projectile rpcInvoke(ProjParameters args) {
		return new HexaInvoluteProj(
			args.pos, args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();

		if (ownerActor != null) {
			changePos(ownerActor.getCenterPos());
		}

		for (int i = particles.Count - 1; i >= 0; i--) {
			particles[i].time += Global.spf;
			if (particles[i].time > particles[i].maxTime) {
				particles.RemoveAt(i);
			}
		}

		if (partCooldown == 0) {
			partCooldown = 2;
		}
		Helpers.decrementFrames(ref soundCooldown);
		Helpers.decrementFrames(ref partCooldown);
		if (soundCooldown == 0) {
			sound = owner.character?.playSound("hexaInvolute");
			soundCooldown = 126;
		}

		byteAngle += speedMul * 0.6f * xDir;
		updateBeams();
	}

	public void updateBeams() {
		float drawAngle = angle;

		for (int i = 0; i < beamDest.Length; i++) {
			float offset = i * 60;
			Point dest = pos.addxy(
				Helpers.cosd(drawAngle + offset) * radius, Helpers.sind(drawAngle + offset) * radius
			);
			var hitPoint = Global.level.raycast(pos, dest, Helpers.wallTypeList);
			if (hitPoint != null) {
				dest = hitPoint.getHitPointSafe();
			}
			beamDest[i] = dest;
			updateHitboxes(i, dest - pos);

			if (partCooldown == 0) {
				if (!Options.main.lowQualityParticles()) {
					particles.Add(new HexaInvolutePart(dest));
				}
			}
		}
		if (partCooldown == 0) {
			if (!Options.main.lowQualityParticles()) {
				particles.Add(new HexaInvolutePart(pos));
			}
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		for (int i = 0; i < beamDest.Length; i++) {
			drawLine(pos, beamDest[i]);
		}
		foreach (HexaInvolutePart part in particles) {
			Point partPos = part.getPos();
			float partSize = part.getRadius();
			Global.sprites["vilemk5_super_part"].draw(
				0, partPos.x, partPos.y, 1, 1, null, part.getAlpha(), partSize, partSize, zIndex + 1
			);
		}
	}

	public Collider[] popullateHitboxes() {
		Collider[] retCol = new Collider[(6 * hitboxNum) + 1];
		for (int i = 0; i < retCol.Length; i++) {
			retCol[i] = new Collider(
				new Rect(0f, 0f, 16, 16).getPoints(),
				false, this, false, false,
				HitboxFlag.Hitbox, Point.zero
			);
		}
		return retCol;
	}


	public void updateHitboxes(int num, Point dest) {
		int start = num * hitboxNum;
		int end = start + hitboxNum;
		float xOff = dest.x / hitboxNum;
		float yOff = dest.y / hitboxNum;
		int j = 1;
		for (int i = start; i < end; i++) {
			Collider hitbox = sprite.hitboxes[i];
			hitbox.offset.x = xOff * j;
			hitbox.offset.y = yOff * j;
			j++;
		}
	}

	public List<Point> getNodes(Point origin, Point dest) {
		List<Point> nodes = [];
		int nodeCount = 8;
		Point dirTo = origin.directionTo(dest).normalize();
		float len = origin.distanceTo(dest);
		Point lastNode = origin;
		for (int i = 0; i <= nodeCount; i++) {
			Point node = i == 0 ? lastNode : lastNode.add(dirTo.times(len / 8));
			Point randNode = node;
			if (i > 0 && i < nodeCount) randNode = node.addRand(10, 10);
			nodes.Add(randNode);
			lastNode = node;
		}
		return nodes;
	}

	public void drawLine(Point origin, Point dest) {
		var col1 = new Color(74, 78, 221);
		var col2 = new Color(61, 113, 255);
		var col3 = new Color(245, 252, 255);

		float sin = MathF.Sin(Global.time * 30);
		var nodes = getNodes(origin, dest);

		for (int i = 1; i < nodes.Count; i++) {
			Point startPos = nodes[i - 1];
			Point endPos = nodes[i];
			if (Options.main.lowQualityParticles()) {
				DrawWrappers.DrawLine(startPos.x, startPos.y, endPos.x, endPos.y, col3, 2 + sin, zIndex, true);
			} else {
				DrawWrappers.DrawLine(startPos.x, startPos.y, endPos.x, endPos.y, col1, 4 + sin, zIndex, true);
				DrawWrappers.DrawLine(startPos.x, startPos.y, endPos.x, endPos.y, col2, 3 + sin, zIndex, true);
				DrawWrappers.DrawLine(startPos.x, startPos.y, endPos.x, endPos.y, col3, 2 + sin, zIndex, true);
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		muzzle?.destroySelf();
		if (sound != null && Global.sounds.Contains(sound)) {
			sound.sound.Stop();
			sound.sound.Dispose();
			Global.sounds.Remove(sound);
			sound = null;
		}
	}
}
