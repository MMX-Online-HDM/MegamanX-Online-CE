using System;
using System.Diagnostics.CodeAnalysis;
using SFML.Graphics;

namespace MMXOnline;

public class BusterZeroState : CharState {
	public BusterZero zero = null!;

	public BusterZeroState(
		string sprite, string shootSprite = "", string attackSprite = "",
		string transitionSprite = "", string transShootSprite = ""
	) : base(
		sprite, shootSprite, attackSprite, transitionSprite, transShootSprite
	) {
	}

	public override void onEnter(CharState oldState) {
		zero = character as BusterZero ?? throw new NullReferenceException();
	}
}

public class BusterZeroMelee : BusterZeroState {
	bool fired;

	public BusterZeroMelee() : base("projswing") {
		landSprite = "projswing";
		airSprite = "projswing_air";
		useDashJumpSpeed = true;
		airMove = true;
		canJump = true;
		canStopJump = true;
	}

	public override void update() {
		base.update();
		if (character.frameIndex >= 3 && !fired) {
			fired = true;
			character.playSound("zerosaberx3", sendRpc: true);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (!character.grounded || character.vel.y < 0) {
			sprite = "projswing_air";
			character.changeSpriteFromName(sprite, true);
		}
		zero.zSaberCooldown = 56;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
	}
}


public class BusterZeroMeleeWall : BusterZeroState {
	bool fired;
	public int wallDir;
	public Collider wallCollider;

	public BusterZeroMeleeWall(int wallDir, Collider wallCollider) : base("wall_slide_attack") {
		this.wallDir = wallDir;
		this.wallCollider = wallCollider;

	}

	public override void update() {
		base.update();
		if (character.frameIndex >= 2 && !fired) {
			fired = true;
			character.playSound("zerosaberx3", sendRpc: true);
		}
		if (character.isAnimOver()) {
			character.changeState(new WallSlide(wallDir, wallCollider) { enterSound = "" });
			character.sprite.frameIndex = character.sprite.totalFrameNum - 1;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		zero.zSaberCooldown = 56;
		character.useGravity = false;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}

public class BusterZeroDoubleBuster : BusterZeroState {
	public bool fired1;
	public bool fired2;
	public bool isSecond;
	public bool isPinkCharge;
	public bool shootPressedAgain;
	public int startStockLevel;

	public BusterZeroDoubleBuster(bool isSecond, int startstockLevel) : base("doublebuster") {
		this.isSecond = isSecond;
		this.startStockLevel = startstockLevel;
		useDashJumpSpeed = true;
		airMove = true;
		superArmor = false;
		canStopJump = true;
		canJump = true;
		landSprite = "doublebuster";
		airSprite = "doublebuster_air";
	}

	public override void update() {
		base.update();
		if (player.input.isPressed(Control.Shoot, player)) {
			shootPressedAgain = true;
		}
		if (!fired1 && character.frameIndex == 3) {
			fired1 = true;
			character.playSound("buster3X3", sendRpc: true);
			zero.shootSub(2);
			zero.stockedTime = 0;
		}
		if (!fired2 && character.frameIndex == 7) {
			fired2 = true;
			if (!isPinkCharge) {
				zero.stockedBusterLv = 0;
				character.playSound("buster3X3", sendRpc: true);
				zero.shootSub(2);
			} else {
				zero.stockedBusterLv = 0;
				character.playSound("buster2X3", sendRpc: true);
				zero.shootSub(1);
			}
			zero.stockedTime = 0;
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		} else if (!isSecond && character.frameIndex >= 4 && !shootPressedAgain) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		// For the starting buster;
		if (startStockLevel is 1 or 3) {
			isPinkCharge = true;
		}
		// Non-full charge.
		if (isPinkCharge) {
			zero.stockedBusterLv = 1;
			isPinkCharge = true;
		}
		// Full charge.
		else {
			// We add Z-Saber charge if we fire the full charge and we were at 0 charge before.
			if (startStockLevel == 4 || !isSecond) {
				zero.stockedSaber = true;
			}
			zero.stockedBusterLv = 2;
		}
		if (!character.grounded || character.vel.y < 0) {
			sprite = "doublebuster_air";
			character.changeSpriteFromName(sprite, true);
		}
		// For halfway shot.
		if (startStockLevel <= 2) {
			character.frameIndex = 4;
			fired1 = true;
		}
	}

	public override void onExit(CharState? newState) {
		zero.stockedTime = 0;
		base.onExit(newState);
		// We check if we fired the second shot. If not we add the stocked charge.
		if (!fired2) {
			if (isPinkCharge) {
				zero.stockedBusterLv = 1;
			} else {
				zero.stockedBusterLv = 2;
				zero.stockedSaber = true;
			}
		}
		if (!fired1) {
			if (isPinkCharge) {
				zero.stockedBusterLv = 3;
			} else {
				zero.stockedBusterLv = 4;
				zero.stockedSaber = true;
			}
		}
	}
}

public class BusterZeroHadangeki : BusterZeroState {
	public bool fired;
	public bool sound;

	public BusterZeroHadangeki() : base("projswing") {
		landSprite = "projswing";
		airSprite = "projswing_air";
		airMove = true;
		useDashJumpSpeed = true;
		superArmor = false;
		canStopJump = true;
		canJump = true;
	}

	public override void update() {
		base.update();

		if (character.frameIndex >= 3 && !sound) {
			sound = true;
			character.playSound("zerosaberx3", sendRpc: true);
		}
		if (character.frameIndex >= 6 && !fired) {
			zero.stockedSaber = false;
			zero.shootHadangeki();
			fired = true;
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (!character.grounded || character.vel.y < 0) {
			sprite = "projswing_air";
			defaultSprite = sprite;
			character.changeSpriteFromName(sprite, true);
		}
		zero.zSaberCooldown = 56;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		zero.stockedTime = 0;
	}
}

public class BusterZeroHadangekiWall : BusterZeroState {
	bool fired;
	public int wallDir;
	public Collider wallCollider;

	public BusterZeroHadangekiWall(int wallDir, Collider wallCollider) : base("wall_slide_attack") {
		this.wallDir = wallDir;
		this.wallCollider = wallCollider;
		useDashJumpSpeed = true;
		superArmor = true;
		useGravity = false;
	}

	public override void update() {
		base.update();
		if (character.frameIndex >= 2 && !fired) {
			character.playSound("zerosaberx3", sendRpc: true);
			zero.stockedSaber = false;
			zero.shootHadangeki();
			fired = true;
		}
		if (character.isAnimOver()) {
			character.changeState(new WallSlide(wallDir, wallCollider) { enterSound = "" });
			character.sprite.frameIndex = character.sprite.totalFrameNum - 1;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		zero.zSaberCooldown = 56;
		character.useGravity = false;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}

public class BZeroTaunt : CharState {
	public BZeroTaunt() : base("win") {

	}
	public override void update() {
		base.update();
		if (character.isAnimOver() && !Global.level.gameMode.playerWon(player)) {
			character.changeToIdleOrFall();
		}
		if (character.frameIndex == 1 && !once) {
			once = true;
			character.playSound("ching", sendRpc: true);
			new Anim(
				character.pos.addxy(character.xDir, -25f),
				"zero_ching", -character.xDir,
				player.getNextActorNetId(),
				destroyOnEnd: true, sendRpc: true
			);
		}
	}
}

public class HyperBusterZeroStart : BusterZeroState {
	public float radius = 200;
	public float time;
	Anim? LightX3;

	public HyperBusterZeroStart() : base("hyper_start") {
		invincible = true;
		immortal = true;
		statusEffectImmune = true;
	}

	public override void update() {
		base.update();
		if (time == 0) {
			if (radius >= 0) {
				radius -= Global.spf * 200;
			} else {
				time = Global.spf;
				radius = 0;
				HyperBusterZeroStart.activateHypermode(zero);
				character.playSound("ching");
				character.fillHealthToMax();
			}
		} else {
			time += Global.spf;
			if (time >= 1) {
				character.changeToLandingOrFall();
			}
		}
	}

	public static void activateHypermode(BusterZero zero) {
		if (zero.hyperMode == 1) {
			zero.isAwakened = true;
			if (zero.gigaAttack != null) {
				ShinMessenkou newWeapon = new ShinMessenkou();
				newWeapon.ammo = zero.gigaAttack?.ammo ?? MathF.Ceiling(newWeapon.maxAmmo / 2);
				zero.gigaAttack = newWeapon;
			}
		} else if (zero.hyperMode == 2) {
			zero.isViral = true;
			if (zero.gigaAttack != null) {
				DarkHoldWeapon newWeapon = new DarkHoldWeapon();
				newWeapon.ammo = zero.gigaAttack?.ammo ?? MathF.Ceiling(newWeapon.maxAmmo / 2);
				zero.gigaAttack = newWeapon;
			}
		} else {
			zero.isBlackZero = true;
			if (zero.gigaAttack != null) {
				RekkohaWeapon newWeapon = new RekkohaWeapon();
				newWeapon.ammo = zero.gigaAttack?.ammo ?? MathF.Ceiling(newWeapon.maxAmmo / 2);
				newWeapon.ammoCost = 14;
				zero.gigaAttack = newWeapon;
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.clenaseEverithing();
		character.useGravity = false;
		character.vel = new Point();
		LightX3 = new Anim(
				character.pos.addxy(50 * character.xDir, 0f),
				"LightX3", -character.xDir,
				player.getNextActorNetId(),
				destroyOnEnd: false, sendRpc: true, fadeIn: true
			);
		character.player.currency -= Player.zBusterZeroHyperCost;
		character.playSound("blackzeroentry", forcePlay: false, sendRpc: true);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		LightX3?.destroySelf();
		character.useGravity = true;
		if (character != null) {
			character.invulnTime = 0.5f;
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		Point pos = character.getCenterPos();
		DrawWrappers.DrawCircle(
			pos.x + x, pos.y + y, radius, false, Color.White, 5, character.zIndex + 1, true, Color.White
		);
	}
}

public class HyperBusterZeroStart2 : BusterZeroState {
	public float radius = 200;
	public float time;
	Anim? virusEffectParts;
	Anim[] virusAnim = new Anim[3];
	float[] delayedVirusTimer = { 0, 7, 14 };
	string virusAnimName = "";

	public HyperBusterZeroStart2() : base("hyper_start") {
		invincible = true;
		immortal = true;
		statusEffectImmune = true;
	}

	public override void update() {
		base.update();
		if (virusAnimName != "") {
			int animCount = 0;
			for (int i = 0; i < virusAnim.Length; i++) {
				if (virusAnim[i] != null) {
					if (virusAnim[i].pos == character.getCenterPos()) {
						virusAnim[i].destroySelf();
					}
					if (virusAnim[i].destroyed) {
						character.playSound("shingetsurinx5", true);
						if (stateFrames > 55) {
							virusAnim[i] = null!;
							continue;
						}
						virusAnim[i] = virusAnim[i] = createVirusAnim();
					} else {
						animCount++;
					}
					virusAnim[i].moveToPos(character.getCenterPos(), 300);
					if (virusAnim[i].pos.distanceTo(character.getCenterPos()) < 10) {
						virusAnim[i].destroySelf();
					}
				} else if (delayedVirusTimer[i] > 0) {
					delayedVirusTimer[i] -= Global.speedMul;
					if (delayedVirusTimer[i] <= 0) {
						delayedVirusTimer[i] = 0;
						virusAnim[i] = createVirusAnim();
					}
				}
				if (animCount == 0 && stateFrames > 55 && virusEffectParts != null) {
					virusEffectParts.destroySelf();
				}
			}
		}
		if (time == 0) {
			if (radius >= 0) {
				radius -= Global.spf * 200;
			} else {
				time = Global.spf;
				radius = 0;
				HyperBusterZeroStart.activateHypermode(zero);
				character.playSound("ching");
				character.fillHealthToMax();
			}
		} else {
			time += Global.spf;
			if (time >= 1) {
				character.changeToIdleOrFall();
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel = new Point();
		character.clenaseEverithing();
		if (zero == null) {
			throw new NullReferenceException();
		}
		character.player.currency -= Player.zBusterZeroHyperCost;
		if (zero.hyperMode == 2) {
			zero.changeSpriteFromName("hyper_viral", true);
			virusAnimName = "sigmavirushead";
			virusAnim[0] = createVirusAnim();
		}
		if (zero.hyperMode == 1) {
			zero.changeSpriteFromName("hyper_awakened", true);
			virusAnimName = "zerovirus";
			virusAnim[0] = createVirusAnim();
			virusEffectParts = new Anim(character.pos.addxy(0, 4), "viruseffect", -character.xDir, null, false);
			virusEffectParts.blink = true;
		}
		if (zero.hyperMode == 0) {
			character.playSound("blackzeroentry", forcePlay: false, sendRpc: true);
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
		if (character != null) {
			character.invulnTime = 0.5f;
		}
		virusEffectParts?.destroySelf();
		bool playedHitSound = false;
		for (int i = 0; i < virusAnim.Length; i++) {
			if (virusAnim[i]?.destroyed == false) {
				if (!playedHitSound) {
					character?.playSound("shingetsurinx5", true);
					playedHitSound = true;
				}
				virusAnim[i]?.destroySelf();
			}
		}
	}

	public Anim createVirusAnim() {
		float newAngle = Helpers.randomRange(0, 359);
		Point newPos = new(Helpers.cosd(newAngle) * 100, Helpers.sind(newAngle) * 100);
		float diplayAngle = newAngle;
		int xDir = -1;
		if (virusAnimName == "sigmavirushead") {
			xDir = 1;
		}
		if (diplayAngle > 90 && diplayAngle < 270) {
			diplayAngle = (diplayAngle + 180f) % 360f;
			xDir *= -1;
		}
		return new Anim(
			character.getCenterPos() + newPos,
			virusAnimName, xDir,
			player.getNextActorNetId(), false, sendRpc: true
		) {
			angle = diplayAngle
		};
	}

	public override void render(float x, float y) {
		base.render(x, y);
		Point pos = character.getCenterPos();
		if (zero.hyperMode == 0) {
			DrawWrappers.DrawCircle(
				pos.x + x, pos.y + y, radius, false, Color.White, 5, character.zIndex + 1, true, Color.White
			);
		}
	}
}