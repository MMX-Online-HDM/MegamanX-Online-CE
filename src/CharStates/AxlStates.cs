using System.Linq;
using System;

namespace MMXOnline;

public class HyperAxlStart : CharState {
	public float radius = 200;
	public float time;
	public Axl axl = null!;

	public HyperAxlStart(bool isGrounded) : base(isGrounded ? "hyper_start" : "hyper_start_air") {
		invincible = true;
		statusEffectImmune = true;
	}

	public override void update() {
		base.update();

		foreach (var weapon in character.weapons) {
			for (int i = 0; i < 10; i++) weapon.rechargeAmmo(0.1f);
		}

		if (character.loopCount > 8) {
			axl.whiteAxlTime = axl.maxHyperAxlTime;
			RPC.setHyperAxlTime.sendRpc(character.player.id, axl.whiteAxlTime, 1);
			axl.playSound("ching");
			if (player.input.isHeld(Control.Jump, player)) {
				axl.changeState(new Hover(), true);
			} else {
				character.changeToIdleOrFall();
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.clenaseAllDebuffs();
		axl = character as Axl ?? throw new NullReferenceException() ;
		if (!axl.hyperAxlUsed) {
			axl.hyperAxlUsed = true;
			axl.player.currency -= 10;
		}
		axl.useGravity = false;
		axl.vel = new Point();
		axl.fillHealthToMax();
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		axl.useGravity = true;
		if (axl != null) {
			axl.invulnTime = 0.5f;
		}
	}
}

public class Hover : CharState {
	public SoundWrapper? sound;
	float hoverTime;
	Anim? hoverExhaust;
	public Axl axl = null!;

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
		if (hoverExhaust != null) {
			hoverExhaust.changePos(exhaustPos());
			hoverExhaust.xDir = axl.getAxlXDir();
		}
		if ((hoverTime > 2 && !axl.isWhiteAxl()) ||
			!character.player.input.isHeld(Control.Jump, character.player)
		) {
			character.changeState(character.getFallState(), true);
		}
	}

	public Point exhaustPos() {
		if (character.currentFrame.POIs.Length == 0) return character.pos;
		Point exhaustPOI = character.currentFrame.POIs.Last();
		return character.pos.addxy(exhaustPOI.x * axl.getAxlXDir(), exhaustPOI.y);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		axl = character as Axl ?? throw new NullReferenceException() ;
		character.useGravity = false;
		character.vel = new Point();
		hoverExhaust = new Anim(
			exhaustPos(), "hover_exhaust", axl.getAxlXDir(), player.getNextActorNetId(), false, sendRpc: true
		);
		hoverExhaust.setzIndex(ZIndex.Character - 1);
		if (character.ownedByLocalPlayer) {
			sound = character.playSound("axlHover", forcePlay: false, sendRpc: false);
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
		hoverExhaust?.destroySelf();
		if (sound != null && !sound.deleted) {
			sound.sound?.Stop();
		}
		RPC.stopSound.sendRpc("axlHover", character.netId);
	}
}

public class DodgeRoll : CharState {
	public float dashTime = 0;
	public int initialDashDir;
	public Axl axl = null!;

	public DodgeRoll() : base("roll") {
		attackCtrl = true;
		normalCtrl = true;
		specialId = SpecialStateIds.AxlRoll;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		axl = character as Axl ?? throw new NullReferenceException() ;
		character.isDashing = true;
		character.burnTime -= 1;
		if (character.burnTime < 0) {
			character.burnTime = 0;
		}
		initialDashDir = character.xDir;
		if (player.input.isHeld(Control.Left, player)) initialDashDir = -1;
		else if (player.input.isHeld(Control.Right, player)) initialDashDir = 1;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		axl.dodgeRollCooldown = Global.customSettings?.axlDodgerollCooldown ?? Axl.maxDodgeRollCooldown;
	}

	public override void update() {
		base.update();
		axl.dodgeRollCooldown = Global.customSettings?.axlDodgerollCooldown ?? Axl.maxDodgeRollCooldown;

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
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
	public Axl axl = null!;

	public SniperAimAxl() : base("crouch") {

	}

	public override void update() {
		base.update();
		if (!axl?.isZooming() == true) {
			axl?.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		axl = character as Axl ?? throw new NullReferenceException() ;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		if (axl?.isZooming() == true) {
			axl?.zoomOut();
		}
	}
}
public class AxlTaunt : CharState {
	public AxlTaunt() : base("win") {

	}
	public override void update() {
		base.update();
		if (character.isAnimOver() && !Global.level.gameMode.playerWon(player)) {
			character.changeToIdleOrFall();
		}
		if (!once) {
			once = true;
			character.playSound("ching", sendRpc: true);
		}
	}
}
