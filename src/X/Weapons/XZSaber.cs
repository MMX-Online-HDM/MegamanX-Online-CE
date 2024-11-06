namespace MMXOnline;

public class ZXSaber : Weapon {

	public static ZXSaber netWeapon = new(null!);
	public ZXSaber(Player player) : base() {
		damager = new Damager(player, 4, Global.defFlinch, 0.25f);
		index = (int)WeaponIds.XSaber;
		weaponBarBaseIndex = 21;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 66;
	}
}

public class XSaberProj : Projectile {
	public XSaberProj(
		Weapon weapon, Point pos, int xDir,
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 300, 4, player, "zsaber_shot", 
		0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		reflectable = true;
		projId = (int)ProjIds.XSaberProj;
		maxTime = 0.5f;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new XSaberProj(
			ZXSaber.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}
}

public class XSaberState : CharState {
	bool fired;
	bool grounded;
	public XSaberState(bool grounded) : base(grounded ? "beam_saber" : "beam_saber_air", "", "", "") {
		this.grounded = grounded;
		landSprite = "beam_saber";
		airMove = true;
		useDashJumpSpeed = true;
	}

	public override void update() {
		base.update();
		if (character.frameIndex >= 6 && !fired) {
			fired = true;
			character.playSound("zerosaberx3");
			new XSaberProj(
				new ZXSaber(player), character.pos.addxy(20 * character.xDir, -20), 
				character.xDir, player, player.getNextActorNetId(), rpc: true
			);
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}

public class X6SaberState : CharState {
	bool fired;
	bool grounded;
	public X6SaberState(bool grounded) : base(grounded ? "beam_saber2" : "beam_saber_air2", "", "", "") {
		this.grounded = grounded;
		landSprite = "beam_saber2";
		airMove = true;
		useDashJumpSpeed = true;
	}

	public override void update() {
		base.update();
		int frameSound = 1;
		if (character.frameIndex >= frameSound && !fired) {
			fired = true;
			character.playSound("raijingeki");
			//new XSaberProj(new XSaber(player), character.pos.addxy(30 * character.xDir, -29), character.xDir, player, player.getNextActorNetId(), rpc: true);
		}

		if (player.character.canCharge() && player.input.isHeld(Control.Shoot, player)) {
			player.character.increaseCharge();
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}
