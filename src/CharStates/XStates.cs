using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;

public class XHover : CharState {
	public SoundWrapper? sound;
	float hoverTime;
	int startXDir;
	public XHover() : base("hover", "hover_shoot", "", "") {
		airMove = true;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();

		character.xDir = startXDir;
		Point inputDir = player.input.getInputDir(player);

		if (inputDir.x == character.xDir) {
			if (!sprite.StartsWith("hover_forward")) {
				sprite = "hover_forward";
				shootSprite = sprite + "_shoot";
				character.changeSpriteFromName(sprite, true);
			}
		} else if (inputDir.x == -character.xDir) {
			if (player.input.isHeld(Control.Jump, player)) {
				if (!sprite.StartsWith("hover_backward")) {
					sprite = "hover_backward";
					shootSprite = sprite + "_shoot";
					character.changeSpriteFromName(sprite, true);
				}
			} else {
				character.xDir = -character.xDir;
				startXDir = character.xDir;
				if (!sprite.StartsWith("hover_forward")) {
					sprite = "hover_forward";
					shootSprite = sprite + "_shoot";
					character.changeSpriteFromName(sprite, true);
				}
			}
		} else {
			if (sprite != "hover") {
				sprite = "hover";
				shootSprite = sprite + "_shoot";
				character.changeSpriteFromName(sprite, true);
			}
		}

		if (character.vel.y < 0) {
			character.vel.y += Global.speedMul * character.getGravity();
			if (character.vel.y > 0) character.vel.y = 0;
		}

		if (character.gravityWellModifier > 1) {
			character.vel.y = 53;
		}

		hoverTime += Global.spf;
		if (hoverTime > 2 || player.input.checkDoubleTap(Control.Dash)) {
			character.changeState(new Fall(), true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel = new Point();
		startXDir = character.xDir;
		if (stateTime <= 0.1f) {
			sound = character.playSound("uahover", forcePlay: false, sendRpc: true);
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
		if (sound != null && !sound.deleted) {
			sound.sound?.Stop();
		}
		RPC.stopSound.sendRpc("uahover", character.netId);

	}
}

public class X2ChargeShot : CharState {
	bool fired;
	int type;
	bool pressFire;
	MegamanX mmx = null!;

	public X2ChargeShot(int type) : base(type == 0 || type == 2 ? "x2_shot" : "x2_shot2") {
		this.type = type;
		useDashJumpSpeed = true;
		airMove = true;
		landSprite = "x2_shot";
		airSprite = "x2_air_shot";
		if (type == 1) {
			landSprite = "x2_shot2";
			airSprite = "x2_air_shot2";
		}
	}

	public override void update() {
		base.update();
		if (!fired && character.currentFrame.getBusterOffset() != null) {
			fired = true;
			if (type == 0) {
				new Buster3Proj(
					player.weapon, character.getShootPos(), character.getShootXDir(), 1,
					player, player.getNextActorNetId(), rpc: true
				);
				character.playSound("buster4X2", sendRpc: true);
			} else if (type == 1) {
				new Buster3Proj(
					player.weapon, character.getShootPos(), character.getShootXDir(), 2,
					player, player.getNextActorNetId(), rpc: true
				);
				character.playSound("buster4X2", sendRpc: true);
			} else if (type == 2) {
				new BusterPlasmaProj(
					player.weapon, character.getShootPos(), character.getShootXDir(),
					player, player.getNextActorNetId(), rpc: true
				);
				character.playSound("plasmaShot", sendRpc: true);
			}
		}
		if (character.isAnimOver()) {
			if (type == 0 && pressFire) {
				fired = false;
				type = 1;
				mmx.stockedCharge = false;
				Global.serverClient?.rpc(RPC.playerToggle, (byte)player.id, (int)RPCToggleType.UnstockCharge);
				sprite = "x2_shot2";
				defaultSprite = sprite;
				landSprite = "x2_shot2";
				airSprite = "x2_shot2";
				if (!character.grounded || character.vel.y < 0) {
					sprite = "x2_air_shot2";
					defaultSprite = sprite;
				}
				character.changeSpriteFromName(sprite, true);
			} else {
				character.changeToIdleOrFall();
			}
		} else {
			if (!pressFire && stateTime > Global.spf && player.input.isPressed(Control.Shoot, player)) {
				pressFire = true;
			}
			if (character.grounded && player.input.isPressed(Control.Jump, player)) {
				character.vel.y = -character.getJumpPower();
				if (type == 0) {
					sprite = "x2_air_shot";
				} else {
					sprite = "x2_air_shot2";
				}	
				character.changeSpriteFromName(sprite, false);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();
		if (!character.grounded || character.vel.y > 0) {
			if (type == 0) {
				sprite = "x2_air_shot";
			} else {
				sprite = "x2_air_shot2";
			}
			character.changeSpriteFromName(sprite, true);
		}
		character.changeSpriteFromName(sprite, true);
	}

	public override void onExit(CharState newState) {
		if (newState is not AirDash && newState is not WallSlide) {
			character.shootAnimTime = 0;
		} else {
			character.shootAnimTime = 0.334f - character.animSeconds;
		}
		base.onExit(newState);
	}
}

public class X3ChargeShot : CharState {
	bool fired;
	public int state = 0;
	bool pressFire;
	MegamanX mmx = null!;
	public HyperBuster? hyperBusterWeapon;

	public X3ChargeShot(HyperBuster? hyperBusterWeapon) : base("x3_shot", "", "", "") {
		this.hyperBusterWeapon = hyperBusterWeapon;
		airMove = true;
		useDashJumpSpeed = true;
	}

	public override void update() {
		base.update();
		if (character.grounded) {
			character.turnToInput(player.input, player);
		}
		if (!fired && character.currentFrame.getBusterOffset() != null && player.ownedByLocalPlayer) {
			fired = true;
			if (state == 0) {
				Point shootPos = character.getShootPos();
				int shootDir = character.getShootXDir();
				if (!player.hasUltimateArmor()) {
					new Anim(
						shootPos, "buster4_x3_muzzle", shootDir,
						player.getNextActorNetId(), true, sendRpc: true
					);
					new Buster3Proj(
						player.weapon, shootPos, shootDir,
						3, player, player.getNextActorNetId(), rpc: true
					);
					if (!(player.weapon is HyperBuster)) {
						character.playSound("buster3X3", sendRpc: true);
					}
				} else {
					new Anim(shootPos, "buster4_muzzle_flash", shootDir, null, true);
					new BusterPlasmaProj(
						player.weapon, shootPos, shootDir,
						player, player.getNextActorNetId(), rpc: true
					);
					character.playSound("plasmaShot", sendRpc: true);
				}
			} else {
				if (hyperBusterWeapon != null) {
					hyperBusterWeapon.ammo -= hyperBusterWeapon.getChipFactoredAmmoUsage(player);
				}
				character.playSound("buster3X3", sendRpc: true);
				float xDir = character.getShootXDir();
				new BusterX3Proj2(
					player.weapon, character.getShootPos().addxy(6 * xDir, -2), character.getShootXDir(), 0,
					player, player.getNextActorNetId(), rpc: true
				);
				new BusterX3Proj2(
					player.weapon, character.getShootPos().addxy(6 * xDir, -2), character.getShootXDir(), 1,
					player, player.getNextActorNetId(), rpc: true
				);
				new BusterX3Proj2(
					player.weapon, character.getShootPos().addxy(6 * xDir, -2), character.getShootXDir(), 2,
					player, player.getNextActorNetId(), rpc: true
				);
				new BusterX3Proj2(
					player.weapon, character.getShootPos().addxy(6 * xDir, -2), character.getShootXDir(), 3,
					player, player.getNextActorNetId(), rpc: true
				);
			}
		}
		if (character.isAnimOver()) {
			if (state == 0 && pressFire) {
				if (hyperBusterWeapon != null) {
					if (hyperBusterWeapon.ammo < hyperBusterWeapon.getChipFactoredAmmoUsage(player)) {
						character.changeToIdleOrFall();
						return;
					}
				} else {
					mmx.stockedX3Buster = false;
				}
				sprite = "x3_shot2";
				landSprite = "x3_shot2";
				if (!character.grounded || character.vel.y < 0) {
					sprite = "x3_air_shot2";
					defaultSprite = sprite;
				}
				defaultSprite = sprite;
				character.changeSpriteFromName(sprite, true);
				state = 1;
				fired = false;
			} else {
				character.changeToIdleOrFall();
			}
		} else {
			if (!pressFire && stateTime > Global.spf && player.input.isPressed(Control.Shoot, player)) {
				pressFire = true;
			}
			if (character.grounded && player.input.isPressed(Control.Jump, player)) {
				character.vel.y = -character.getJumpPower();
				if (state == 0) {
					sprite = "x2_air_shot";
					defaultSprite = sprite;
				} else {
					sprite = "x2_air_shot2";
					defaultSprite = sprite;
				}
				character.changeSpriteFromName(sprite, false);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();
		if (mmx == null) {
			throw new NullReferenceException();
		}
		if (!mmx.stockedX3Buster) {
			if (hyperBusterWeapon == null) {
				mmx.stockedX3Buster = true;
			}
			sprite = "x3_shot";
			defaultSprite = sprite;
			landSprite = "x3_shot";
			if (!character.grounded) {
				sprite = "x3_air_shot";
			}
			character.changeSpriteFromName(sprite, true);
		} else {
			mmx.stockedX3Buster = false;
			state = 1;
			sprite = "x3_shot2";
			defaultSprite = sprite;
			landSprite = "x3_shot2";
			if (!character.grounded) {
				sprite = "x3_air_shot2";
			}
			character.changeSpriteFromName(sprite, true);
		}
	}

	public override void onExit(CharState newState) {
		if (state == 0) {
			mmx.stockedX3Buster = true;
		} else {
			mmx.stockedX3Buster = false;
		}
		character.shootAnimTime = 0;
		base.onExit(newState);
	}
}
