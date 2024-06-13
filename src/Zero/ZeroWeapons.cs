namespace MMXOnline;

public enum ZeroAttackLoadoutType {
	ZSaber,
	KKnuckle,
	ZBuster,
}

public class ZSaber : Weapon {
	public static ZSaber staticWeapon = new();

	public ZSaber() : base() {
		//damager = new Damager(player, 3, 0, 0.5f);
		index = (int)WeaponIds.ZSaber;
		weaponBarBaseIndex = 21;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 48;
		killFeedIndex = 9;
		type = (int)ZeroAttackLoadoutType.ZSaber;
		displayName = "Z-Saber";
		description = new string[] { "Zero's trusty beam saber." };
	}
}

public class ShippuugaWeapon : Weapon {
	public static ShippuugaWeapon staticWeapon = new();

	public ShippuugaWeapon() : base() {
		//damager = new Damager(player, 2, Global.defFlinch, 0.5f);
		index = (int)WeaponIds.Shippuuga;
		weaponBarBaseIndex = 21;
		killFeedIndex = 39;
	}
}

public class KKnuckleWeapon : Weapon {
	public KKnuckleWeapon() : base() {
		//damager = new Damager(player, 3, Global.defFlinch, 0.25f);
		index = (int)WeaponIds.KKnuckle;
		weaponBarBaseIndex = 21;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 120;
		killFeedIndex = 106;
		type = (int)ZeroAttackLoadoutType.KKnuckle;
		displayName = "K-Knuckle";
		description = new string[] { "Use your fists to teach foes a lesson." };
	}
}

public class Shingetsurin : Weapon {
	public static Shingetsurin netWeapon = new();

	public Shingetsurin() : base() {
		index = (int)WeaponIds.Shingetsurin;
		killFeedIndex = 85;
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
	}
}

public class ZSaberProjSwing : Weapon {
	public ZSaberProjSwing() : base() {
		index = (int)WeaponIds.ZSaberProjSwing;
		killFeedIndex = 9;
		//damager = new Damager(player, 3, Global.defFlinch, 0.5f);
	}
}

public class ZeroSpinKickState : CharState {
	public float dashTime = 0;
	public float soundTime = 0;

	public ZeroSpinKickState() : base("spinkick") {
		exitOnAirborne = true;
		airMove = true;
		attackCtrl = false;
		normalCtrl = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.isDashing = true;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		if (character is Zero zero) {
			zero.dashAttackCooldown = 0.5f;
		}
	}

	public override void update() {
		base.update();
		soundTime -= Global.spf;
		if (soundTime <= 0) {
			soundTime = 0.15f;
			character.playSound("spinkick", sendRpc: true);
		}

		if (
			(player.input.isPressed(Control.Left, player) && character.xDir == 1) ||
			(player.input.isPressed(Control.Right, player) && character.xDir == -1) ||
			player.input.isPressed(Control.Dash, player)
		) {
			changeToIdle();
			return;
		}

		dashTime += Global.spf;
		float modifier = 1;
		if (dashTime > 0.6 * modifier) {
			changeToIdle();
			return;
		}
		var move = new Point(0, 0);
		move.x = character.getDashSpeed() * modifier * character.xDir;
		character.move(move);
	}
}

