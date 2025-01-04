namespace MMXOnline;

public class HadoukenWeapon : Weapon {
	public static HadoukenWeapon netWeapon = new(null!);

	public HadoukenWeapon(Player player) : base() {
		damager = new Damager(player, Damager.ohkoDamage, Global.defFlinch, 0.5f);
		ammo = 0;
		index = (int)WeaponIds.Hadouken;
		weaponBarBaseIndex = 19;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 14;
	}
}

public class HadoukenProj : Projectile {
	public HadoukenProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 250, Damager.ohkoDamage, player, "hadouken", 
		Global.defFlinch, 0.15f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.4f;
		fadeSprite = "hadouken_fade";
		reflectable = true;
		destroyOnHit = true;
		projId = (int)ProjIds.Hadouken;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new HadoukenProj(
			HadoukenWeapon.netWeapon, arg.pos, 
			arg.xDir, arg.player, arg.netId
		);
	}
}

public class Hadouken : CharState {
	bool fired = false;
	MegamanX? mmx;

	public Hadouken() : base("hadouken") {
		superArmor = true;
	}

	public override void update() {
		base.update();

		if (character.frameIndex >= 2 && !fired) {
			fired = true;

			Weapon weapon = new HadoukenWeapon(player);
			float x = character.pos.x;
			float y = character.pos.y;

			new HadoukenProj(weapon, new Point(x + (20 * character.xDir), y - 20), character.xDir, player, player.getNextActorNetId(), rpc: true);

			character.playSound("hadouken", sendRpc: true);
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX;
		character.stopCharge();
	}

	public override void onExit(CharState newState) {
		if (mmx != null) mmx.hadoukenCooldownTime = mmx.maxHadoukenCooldownTime;
		base.onExit(newState);
	}
}
