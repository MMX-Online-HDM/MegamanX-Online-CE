using System;

namespace MMXOnline;

public enum RisingType {
	Ryuenjin,
	Denjin,
	RisingFang
}

public class RyuenjinWeapon : Weapon {
	public static RyuenjinWeapon staticWeapon = new();

	public RyuenjinWeapon() : base() {
		//damager = new Damager(player, 4, 0, 0.2f);
		index = (int)WeaponIds.Ryuenjin;
		rateOfFire = 0.25f;
		weaponBarBaseIndex = 23;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 11;
		type = (int)RisingType.Ryuenjin;
		displayName = "Ryuenjin";
		description = new string[] { "A fiery uppercut that burns enemies." };
	}

	public static Weapon getWeaponFromIndex(int index) {
		if (index == (int)RisingType.Ryuenjin) return new RyuenjinWeapon();
		else if (index == (int)RisingType.Denjin) return new DenjinWeapon();
		else if (index == (int)RisingType.RisingFang) return new RisingFangWeapon();
		else throw new Exception("Invalid Zero ryuenjin weapon index!");
	}
}

public class DenjinWeapon : Weapon {
	public static DenjinWeapon staticWeapon = new();

	public DenjinWeapon() : base() {
		//damager = new Damager(player, 3, Global.defFlinch, 0.1f);
		index = (int)WeaponIds.EBlade;
		rateOfFire = 0.25f;
		weaponBarBaseIndex = 41;
		killFeedIndex = 36;
		type = (int)RisingType.Denjin;
		displayName = "Denjin";
		description = new string[] { "An electrical uppercut that flinches enemies", "and can hit multiple times." };
	}
}

public class RisingFangWeapon : Weapon {
	public static RisingFangWeapon staticWeapon = new();

	public RisingFangWeapon() : base() {
		//damager = new Damager(player, 2, 0, 0.5f);
		index = (int)WeaponIds.Rising;
		rateOfFire = 0.1f;
		weaponBarBaseIndex = 41;
		killFeedIndex = 83;
		type = (int)RisingType.RisingFang;
		displayName = "Rising";
		description = new string[] { "A fast, element-neutral uppercut.", "Can be used in the air to gain height." };
	}
}

public class ZeroUppercut : CharState {
	bool jumpedYet;
	float timeInWall;
	bool isUnderwater;
	public bool isHeld = true;
	public float holdTime;
	public RisingType type;
	public Zero? zero = null!;
	int jumpFrame;

	public ZeroUppercut(RisingType type, bool isUnderwater) : base(getSprite(type, isUnderwater), "", "") {
		this.type = type;
		this.isUnderwater = type == RisingType.Ryuenjin && isUnderwater;
		setStartupFrame(type);
	}

	public ZeroUppercut(int type, bool isUnderwater) : base(getSprite((RisingType)type, isUnderwater), "", "") {
		this.type = (RisingType)type;
		this.isUnderwater = this.type == RisingType.Ryuenjin && isUnderwater;
		setStartupFrame(this.type);
	}

	public void setStartupFrame(RisingType type) {
		jumpFrame = type switch {
			RisingType.Ryuenjin => 4,
			RisingType.Denjin => 5,
			_ => 4
		};
	}

	public static string getSprite(RisingType type, bool isUnderwater) {
		return type switch {
			RisingType.Ryuenjin when isUnderwater => "ryuenjin_underwater",
			RisingType.Ryuenjin => "ryuenjin",
			RisingType.Denjin => "eblade",
			_ => "rising"
		};
	}

	public override void update() {
		base.update();

		if (character.sprite.frameIndex >= jumpFrame && !jumpedYet) {
			jumpedYet = true;
			character.dashedInAir++;
			float ySpeedMod = 1.2f;
			character.vel.y = -character.getJumpPower() * ySpeedMod;
			if (!isUnderwater) {
				string saberSound = type switch {
					RisingType.Ryuenjin => "ryuenjin",
					RisingType.Denjin => "raijingeki",
					_ => "saber1"
				};
				character.playSound(saberSound, sendRpc: true);
			}
		}

		if (!player.input.isHeld(Control.Special1, player) && !player.input.isHeld(Control.Shoot, player)) {
			isHeld = false;
		}

		if (character.sprite.frameIndex == 6 && type == RisingType.RisingFang) {
			if (isHeld && holdTime < 0.2f) {
				holdTime += Global.spf;
				character.frameSpeed = 0;
				character.frameIndex = 6;
			} else {
				character.frameSpeed = 1;
				character.frameIndex = 6;
			}
		}

		if (character.sprite.frameIndex >= 3 && character.sprite.frameIndex < 6) {
			float speed = 100;
			if (type == RisingType.Denjin) {
				speed = 120;
			}
			character.move(new Point(character.xDir * speed, 0));
		}

		var wallAbove = Global.level.checkCollisionActor(character, 0, -10);
		if (wallAbove != null && wallAbove.gameObject is Wall) {
			timeInWall += Global.spf;
			if (timeInWall > 0.1f) {
				character.changeState(new Fall());
				return;
			}
		}

		if (canDownSpecial()) {
			if (player.input.isPressed(Control.Shoot, player) && player.input.isHeld(Control.Down, player)) {
				if (zero != null) {
					character.changeState(new ZeroDownthrust(zero.downThrustA.type), true);
					return;
				}
			} else if (player.input.isPressed(Control.Special1, player) && player.input.isHeld(Control.Down, player)) {
				if (zero != null) {
					character.changeState(new ZeroDownthrust(zero.downThrustS.type), true);
					return;
				}
			}
		}

		if (character.isAnimOver()) {
			character.changeState(new Fall());
		}
	}

	public bool canDownSpecial() {
		if (character is not Zero zero) {
			return false;
		}
		int fc = character.sprite.frames.Count;
		if (type == RisingType.RisingFang) {
			return character.sprite.frameIndex >= fc - 1;
		}
		return character.sprite.frameIndex >= fc - 3;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (!character.grounded) {
			character.sprite.frameIndex = 3;
		}
		zero = character as Zero ?? throw new NullReferenceException();
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
	}
}
