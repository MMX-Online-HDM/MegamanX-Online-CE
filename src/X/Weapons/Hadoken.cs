using System;
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
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "hadouken", netId, player	
	) {
		weapon = HadoukenWeapon.netWeapon;
		damager.damage = Damager.ohkoDamage;
		damager.hitCooldown = 9;
		damager.flinch = Global.defFlinch;
		vel = new Point(250 * xDir, 0);
		maxTime = 0.4f;
		fadeSprite = "hadouken_fade";
		reflectable = true;
		destroyOnHit = true;
		projId = (int)ProjIds.Hadouken;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new HadoukenProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class Hadouken : CharState {
	bool fired = false;
	public MegamanX mmx = null!;

	public Hadouken() : base("hadouken") {
		superArmor = true;
	}

	public override void update() {
		base.update();
		if (character.frameIndex >= 2 && !fired) {
			fired = true;
			float x = character.pos.x;
			float y = character.pos.y;
			new HadoukenProj(new Point(x + (20 * character.xDir), y - 20), 
			character.xDir, mmx, player, player.getNextActorNetId(), rpc: true);
			character.playSound("hadouken", sendRpc: true);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
	public override bool canEnter(Character character) {
		if (!character.grounded) return false;
		return base.canEnter(character);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();
		character.stopCharge();
	}

	public override void onExit(CharState? newState) {
		if (mmx != null) mmx.hadoukenCooldownTime = mmx.maxHadoukenCooldownTime;
		base.onExit(newState);
	}
}
