using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;

public class XHover : CharState {
	public SoundWrapper? sound;
	float hoverTime;
	int startXDir;
	public XHover() : base("hover", "hover_shoot") {
		airMove = true;
		attackCtrl = true;
		normalCtrl = true;
		useGravity = false;
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
		if (hoverTime > 2 || player.input.checkDoubleTap(Control.Dash) ||
			stateFrames > 12 && player.input.isPressed(Control.Jump, player)
		) {
			character.changeState(new Fall(), true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMovingWeak();
		startXDir = character.xDir;
		if (stateTime <= 0.1f) {
			sound = character.playSound("uahover", forcePlay: false, sendRpc: true);
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		if (sound != null && !sound.deleted) {
			sound.sound?.Stop();
		}
		RPC.stopSound.sendRpc("uahover", character.netId);
	}
}

public class LightDash : CharState {
	public float dashTime;
	public float dustTime;
	public string initialDashButton;
	public int dashDir;
	public bool stop;
	public Anim? dashSpark;
	public Anim? exaust;

	public LightDash(string initialDashButton) : base("dash", "dash_shoot", "attack_dash") {
		attackCtrl = true;
		normalCtrl = true;
		this.initialDashButton = initialDashButton;
		enterSound = "dash";
		enterSoundArgs = "larmor";
	}

	public override void update() {
		base.update();
		if (!player.isAI && !player.input.isHeld(initialDashButton, player) && !stop) {
			dashTime = 900;
		}
		int inputXDir = player.input.getXDir(player);
		bool dashHeld = player.input.isHeld(initialDashButton, player);

		if (dashTime > 32 && !stop) {
			if (exaust?.destroyed == false) {
				exaust.destroySelf();
			}
			dashTime = 0;
			stop = true;
			sprite = "dash_end";
			shootSprite = "dash_end_shoot";
			character.changeSpriteFromName(character.shootAnimTime > 0 ? shootSprite : sprite, true);
		}
		if (dashTime <= 3 || stop) {
			if (inputXDir != 0 && inputXDir != dashDir) {
				character.xDir = (int)inputXDir;
				dashDir = character.xDir;
			}
		}
		// Dash regular speed.
		if (dashTime > 3 && !stop) {
			character.move(new Point(character.getDashSpeed() * 1.15f * dashDir, 0));
		}
		// End move.
		else if (stop && inputXDir != 0) {
			character.move(new Point(character.getRunSpeed() * inputXDir * 1.15f, 0));
			character.changeState(new Run(), true);
			return;
		}
		// Speed at start and end.
		else if (!stop || dashHeld) {
			character.move(new Point(character.getRunSpeed() * dashDir * 1.15f, 0));
		}
		if (exaust == null && dashTime > 3 && !stop) {
			exaust = new Anim(
				character.pos.addxy(-15 * dashDir, -7),
				"fakezero_exhaust", dashDir, player.getNextActorNetId(),
				false, sendRpc: true, zIndex: character.zIndex - 100, host: character 
			);
		}
		// Dust effect.
		if (dustTime >= 6 && !character.isUnderwater()) {
			dustTime = 0;
			new Anim(
				character.getDashDustEffectPos(dashDir),
				"dust", dashDir, player.getNextActorNetId(), true,
				sendRpc: true
			);
		} else {
			dustTime += character.speedMul;
		}
		// Timer.
		dashTime += character.speedMul;
		// End.
		if (stop && character.isAnimOver()) {
			character.changeToIdleOrFall();
			return;
		}
		if (!character.grounded) {
			character.dashedInAir++;
			character.changeToIdleOrFall();
			return;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		dashDir = character.xDir;
		character.isDashing = true;
		dashSpark = new Anim(
			character.getDashSparkEffectPos(dashDir),
			"dash_sparks", dashDir, player.getNextActorNetId(),
			true, sendRpc: true
		);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		if (dashSpark?.destroyed == false) {
			dashSpark.destroySelf();
		}
		if (exaust?.destroyed == false) {
			exaust.destroySelf();
		}
	}
}

public class GigaAirDash : CharState {
	public float dashTime;
	public string initialDashButton;
	public int dashDir;
	public bool stop;
	public Anim? dashSpark;
	public Anim? exaust = null!;

	public GigaAirDash(string initialDashButton) : base("dash", "dash_shoot", "attack_dash") {
		attackCtrl = true;
		normalCtrl = true;
		useGravity = false;
		this.initialDashButton = initialDashButton;
		enterSound = "airdashX2";
		enterSoundArgs = "larmor";
	}

	public override void update() {
		base.update();
		if (!player.isAI && !player.input.isHeld(initialDashButton, player) && !stop) {
			dashTime = 900;
		}
		int inputXDir = player.input.getXDir(player);
		bool dashHeld = player.input.isHeld(initialDashButton, player);

		if (dashTime > 28 && !stop) {
			if (exaust?.destroyed == false) {
				exaust.destroySelf();
			}
			character.useGravity = true;
			dashTime = 0;
			stop = true;
			sprite = "dash_end";
			shootSprite = "dash_end_shoot";
			character.changeSpriteFromName(character.shootAnimTime > 0 ? shootSprite : sprite, true);
		}
		if (dashTime <= 3 || stop) {
			if (inputXDir != 0 && inputXDir != dashDir) {
				character.xDir = (int)inputXDir;
				dashDir = character.xDir;
			}
		}
		// Dash regular speed.
		if (dashTime > 3 && !stop || stop && dashHeld) {
			character.move(new Point(character.getDashSpeed() * 1.15f * dashDir, 0));
		}
		// Dash start and end while hold.
		else if (!stop) {
			character.move(new Point(character.getRunSpeed() * dashDir * 1.15f, 0));
		}
		// Air move.
		else if (inputXDir != 0) {
			character.move(new Point(character.getDashSpeed() * inputXDir, 0));
		}
		if (exaust == null && dashTime > 3 && !stop) {
			exaust = new Anim(
				character.pos.addxy(-15 * dashDir, -7),
				"fakezero_exhaust", dashDir, player.getNextActorNetId(),
				false, sendRpc: true, zIndex: character.zIndex - 100, host: character 
			);
		}
		dashTime += character.speedMul;

		if (stop && character.isAnimOver()) {
			character.changeToIdleOrFall();
			return;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.dashedInAir++;
		dashDir = character.xDir;
		character.isDashing = true;
		dashSpark = new Anim(
			character.getDashSparkEffectPos(dashDir),
			"dash_sparks", dashDir, player.getNextActorNetId(),
			true, sendRpc: true
		);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		if (dashSpark?.destroyed == false) {
			dashSpark.destroySelf();
		}
		if (exaust?.destroyed == false) {
			exaust.destroySelf();
		}
	}
}

public class UpDash : CharState {
	public float dashTime = 0;
	public string initialDashButton;

	public UpDash(string initialDashButton) : base("up_dash", "up_dash_shoot") {
		this.initialDashButton = initialDashButton;
		attackCtrl = true;
	}
	
	public override void update() {
		base.update();
		if (!player.input.isHeld(initialDashButton, player)) {
			changeToFall();
			return;
		}
		int xDir = player.input.getXDir(player);
		if (xDir != 0) {
			character.xDir = xDir;
			character.move(new Point(xDir * 60 * character.getRunDebuffs(), 0));
		}

		if (!once) {
			once = true;
			character.vel = new Point(0, -character.getJumpPower() * 1.125f);
			new Anim(
				character.pos.addxy(0, -10), "dash_sparks_up",
				character.xDir, player.getNextActorNetId(), true, sendRpc: true
			);
			character.playSound("airdashupX3", sendRpc: true);
		}

		dashTime += Global.spf;
		float maxDashTime = 0.4f;
		if (dashTime > maxDashTime || character.vel.y > -1) {
			changeToFall();
			return;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.vel = new Point(0, -4);
		character.dashedInAir++;
		character.frameSpeed = 1;
	}

	public void changeToFall() {
		if (character.vel.y < 0) {
			character.vel.y *= 0.4f;
			if (character.vel.y > -1) { character.vel.y = -1; }
		}
		bool animOver = character.isAnimOver();
		string fullSpriteName = character.sprite.name;
		string spriteName = character.sprite.name.RemovePrefix("mmx_");
		int currentFrame = character.frameIndex;
		float currentFrameTime = character.frameTime;

		character.changeState(new Fall() { transitionSprite = spriteName, sprite = spriteName });
		if (animOver) {
			if (character.shootAnimTime > 0) {
				character.changeSpriteFromName("fall_shoot", true);
			} else {
				character.changeSpriteFromName("fall", true);
			}
		} else {
			character.changeSprite(fullSpriteName, true);
			character.frameIndex = currentFrame;
			character.frameTime = currentFrameTime;
		}
	}
}

public class X2ChargeShot : CharState {
	bool fired;
	int shootNum;
	bool pressFire;
	Weapon? weaponOverride;
	Weapon weapon => (weaponOverride ?? mmx.currentWeapon ?? mmx.specialBuster);
	MegamanX mmx = null!;

	public X2ChargeShot(Weapon? weaponOverride, int shootNum) : base("cross_shot") {
		this.shootNum = shootNum;
		this.weaponOverride = weaponOverride;
		useDashJumpSpeed = true;
		airMove = true;
		canStopJump = true;
		landSprite = "cross_shot";
		airSprite = "cross_air_shot";
		canJump = true;
		if (shootNum == 1) {
			sprite = "cross_shot2";
			defaultSprite = sprite;
			landSprite = sprite;
			airSprite = "cross_air_shot2";
		}
	}

	public override void update() {
		base.update();
		if (!fired && character.currentFrame.getBusterOffset() != null) {
			fired = true;
			if (shootNum == 0) {
				weapon.shoot(mmx, [4, 0]);
				weapon.shootCooldown = weapon.fireRate;
				mmx.shootCooldown = weapon.fireRate;
				if (weapon.shootSounds[3] != "") {
					character.playSound(weapon.shootSounds[3], sendRpc: true);
				}
				weapon.addAmmo(-weapon.getAmmoUsageEX(3, character), player);
				mmx.stockedTime = 0;
			} else {
				mmx.stockedBuster = false;
				weapon.shoot(mmx, [4, 1]);
				weapon.shootCooldown = weapon.fireRate;
				mmx.shootCooldown = weapon.fireRate;
				if (weapon.shootSounds[3] != "") {
					character.playSound(weapon.shootSounds[3], sendRpc: true);
				}
				weapon.addAmmo(-weapon.getAmmoUsageEX(3, character), player);
				mmx.stockedTime = 0;
			}
		}
		if (character.isAnimOver()) {
			if (shootNum == 0 && pressFire) {
				fired = false;
				shootNum = 1;
				sprite = "cross_shot2";
				defaultSprite = sprite;
				landSprite = sprite;
				airSprite = "cross_air_shot2";
				if (!character.grounded || character.vel.y < 0) {
					sprite = "cross_air_shot2";
				}
				character.changeSpriteFromName(sprite, true);
			} else {
				character.changeToIdleOrFall();
			}
		} else if (
			!pressFire && stateTime > 6f / 60f && player.input.isPressed(Control.Shoot, player)
		) {
			pressFire = true;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();
		if (!character.grounded || character.vel.y > 0) {
			if (shootNum == 0) {
				sprite = "cross_air_shot";
			} else {
				sprite = "cross_air_shot2";
			}
		}
		if (mmx.chargedTornadoFang != null) {
			mmx.chargedTornadoFang = null;
		}
		character.changeSpriteFromName(sprite, true);
	}

	public override void onExit(CharState? newState) {
		if (mmx.hasLastingProj()) {
			character.shootAnimTime = 8;
		} else if (newState is not AirDash and not WallSlide) {
			character.shootAnimTime = 0;
		}  else {
			character.shootAnimTime = 20 - character.animTime;
		}
		mmx.lastShootPressed = 100;
		mmx.stockedTime = 0;
		base.onExit(newState);
	}
}

public class X3ChargeShot : CharState {
	bool fired;
	public int state = 0;
	bool pressFire;
	MegamanX mmx = null!;
	public HyperCharge? hyperBusterWeapon;

	public X3ChargeShot(HyperCharge? hyperBusterWeapon) : base("cross_shot") {
		this.hyperBusterWeapon = hyperBusterWeapon;
		useDashJumpSpeed = true;
		airMove = true;
		canStopJump = true;
		landSprite = "cross_shot";
		airSprite = "cross_air_shot";
		canJump = true;
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
				if (!mmx.hasUltimateArmor) {
					new Anim(
						shootPos, "buster4_x3_muzzle", shootDir,
						player.getNextActorNetId(), true, sendRpc: true
					);
					new Buster4MaxProj(
						shootPos, shootDir,
						mmx, player, player.getNextActorNetId(), true
					);
					if (!(player.weapon is HyperCharge)) {
						character.playSound("buster3X3", sendRpc: true);
					}
				} else {
					new Anim(shootPos, "buster4_muzzle_flash", shootDir, null, true);
					new BusterPlasmaProj(
						shootPos, shootDir, mmx,
						player, player.getNextActorNetId(), rpc: true
					);
					character.playSound("plasmaShot", sendRpc: true);
				}
				mmx.stockedTime = 0;
			} else {
				character.playSound("buster3X3", sendRpc: true);
				float xDir = character.getShootXDir();
				new BusterX3Proj2(
					character.getShootPos().addxy(6 * xDir, -2), character.getShootXDir(), 0, mmx,
					player, player.getNextActorNetId(), rpc: true
				);
				new BusterX3Proj2(
					character.getShootPos().addxy(6 * xDir, -2), character.getShootXDir(), 1, mmx,
					player, player.getNextActorNetId(), rpc: true
				);
				new BusterX3Proj2(
					character.getShootPos().addxy(6 * xDir, -2), character.getShootXDir(), 2, mmx,
					player, player.getNextActorNetId(), rpc: true
				);
				new BusterX3Proj2(
					character.getShootPos().addxy(6 * xDir, -2), character.getShootXDir(), 3, mmx,
					player, player.getNextActorNetId(), rpc: true
				);
			}
			mmx.stockedTime = 0;
		}
		if (character.isAnimOver()) {
			if (state == 0 && pressFire) {
				sprite = "cross_shot2";
				landSprite = "cross_shot2";
				if (!character.grounded || character.vel.y < 0) {
					sprite = "cross_air_shot2";
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
					sprite = "cross_air_shot";
					defaultSprite = sprite;
				} else {
					sprite = "cross_air_shot2";
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
		if (!mmx.stockedMaxBuster) {
			if (hyperBusterWeapon == null) {
				mmx.stockedMaxBuster = true;
			}
			sprite = "cross_shot";
			defaultSprite = sprite;
			landSprite = "cross_shot";
			if (!character.grounded) {
				sprite = "cross_air_shot";
			}
			character.changeSpriteFromName(sprite, true);
		} else {
			mmx.stockedMaxBuster = false;
			state = 1;
			sprite = "cross_shot2";
			defaultSprite = sprite;
			landSprite = "cross_shot2";
			if (!character.grounded) {
				sprite = "cross_air_shot2";
			}
			character.changeSpriteFromName(sprite, true);
		}
	}

	public override void onExit(CharState? newState) {
		if (state == 0) {
			mmx.stockedMaxBuster = true;
		} else {
			mmx.stockedMaxBuster = false;
		}
		character.shootAnimTime = 0;
		mmx.stockedTime = 0;
		base.onExit(newState);
	}
}
