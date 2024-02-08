using System;
using System.Linq;

namespace MMXOnline;

public class HyperAxlStart : CharState {
	public float radius = 200;
	public float time;
	public Axl axl;

	public HyperAxlStart(bool isGrounded) : base(isGrounded ? "hyper_start" : "hyper_start_air", "", "", "") {
		invincible = true;
	}

	public override void update() {
		base.update();

		foreach (var weapon in player.weapons) {
			for (int i = 0; i < 10; i++) weapon.rechargeAmmo(0.1f);
		}

		if (character.loopCount > 8) {
			axl.whiteAxlTime = axl.maxHyperAxlTime;
			RPC.setHyperZeroTime.sendRpc(character.player.id, axl.whiteAxlTime, 1);
			axl.playSound("ching");
			if (player.input.isHeld(Control.Jump, player)) {
				axl.changeState(new Hover(), true);
			} else {
				character.changeState(new Idle(), true);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		axl = character as Axl;
		if (!axl.hyperAxlUsed) {
			axl.hyperAxlUsed = true;
			axl.player.scrap -= 10;
		}
		axl.useGravity = false;
		axl.vel = new Point();
		axl.fillHealthToMax();
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		axl.useGravity = true;
		if (axl != null) {
			axl.invulnTime = 0.5f;
		}
	}
}

public class Hover : CharState {
	public SoundWrapper sound;
	float hoverTime;
	Anim hoverExhaust;
	Axl axl;

	public Hover() : base("hover", "hover", "hover", "hover") {
		exitOnLanding = true;
		airMove = true;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();

		accuracy = 0;
		Point prevPos = character.pos;
		
		if (character.pos.x != prevPos.x) {
			accuracy = 5;
		}

		if (character.vel.y < 0) {
			character.vel.y += Global.speedMul * character.getGravity();
			if (character.vel.y > 0) character.vel.y = 0;
		}

		if (character.gravityWellModifier > 1) {
			character.vel.y = 53;
		}

		hoverTime += Global.spf;
		hoverExhaust.changePos(exhaustPos());
		hoverExhaust.xDir = axl.getAxlXDir();
		if ((hoverTime > 2 && !axl.isWhiteAxl()) ||
			!character.player.input.isHeld(Control.Jump, character.player)
		) {
			character.changeState(new Fall(), true);
		}
	}

	public Point exhaustPos() {
		if (character.currentFrame.POIs.Count == 0) return character.pos;
		Point exhaustPOI = character.currentFrame.POIs.Last();
		return character.pos.addxy(exhaustPOI.x * axl.getAxlXDir(), exhaustPOI.y);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		axl = character as Axl;
		character.useGravity = false;
		character.vel = new Point();
		hoverExhaust = new Anim(
			exhaustPos(), "hover_exhaust", axl.getAxlXDir(), player.getNextActorNetId(), false, sendRpc: true
		);
		hoverExhaust.setzIndex(ZIndex.Character - 1);
		if (character.ownedByLocalPlayer) {
			sound = character.playSound("Axlhover", forcePlay: false, sendRpc: true);
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
		hoverExhaust.destroySelf();
		if (sound != null && !sound.deleted)
		{
			sound.sound?.Stop();
		}
		RPC.stopSound.sendRpc("Axlhover", character.netId);
	}
}

public class DodgeRoll : CharState {
	public float dashTime = 0;
	public int initialDashDir;
	Axl axl;

	public DodgeRoll() : base("roll", "", "") {
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		axl = character as Axl;
		character.isDashing = true;
		character.burnTime -= 2;
		if (character.burnTime < 0) {
			character.burnTime = 0;
		}

		initialDashDir = character.xDir;
		if (player.input.isHeld(Control.Left, player)) initialDashDir = -1;
		else if (player.input.isHeld(Control.Right, player)) initialDashDir = 1;
		character.specialState = (int)SpecialStateIds.AxlRoll;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		axl.dodgeRollCooldown = Axl.maxDodgeRollCooldown;
		character.specialState = (int)SpecialStateIds.None;
	}

	public override void update() {
		base.update();

		if (character.isAnimOver()) {
			character.changeState(new Idle(), true);
			return;
		}

		if (character.frameIndex >= 4) return;

		dashTime += Global.spf;

		var move = new Point(0, 0);
		move.x = character.getDashSpeed() * initialDashDir;
		character.move(move);
		if (stateTime > 0.1) {
			stateTime = 0;
			//new Anim(this.character.pos.addxy(0, -4), "dust", this.character.xDir, null, true);
		}
	}
}

public class SniperAimAxl : CharState {
	public Axl axl;

	public SniperAimAxl() : base("crouch", "", "") {

	}

	public override void update() {
		base.update();
		if (!axl.isZooming()) {
			axl.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		axl = character as Axl;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		if (axl.isZooming()) {
			axl.zoomOut();
		}
	}
}

