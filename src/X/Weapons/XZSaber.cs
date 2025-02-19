using System;
namespace MMXOnline;

public class ZXSaber : Weapon {
	public static ZXSaber netWeapon = new();
	public ZXSaber() : base() {
		//damager = new Damager(player, 4, Global.defFlinch, 0.25f);
		index = (int)WeaponIds.XSaber;
		weaponBarBaseIndex = 21;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 66;
	}
}

public class XSaberProj : Projectile {
	public XSaberProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "zsaber_shot", netId, player
	) {
		weapon = ZXSaber.netWeapon;
		damager.damage = 4;
		damager.hitCooldown = 30;
		vel = new Point(300 * xDir, 0);
		reflectable = true;
		projId = (int)ProjIds.XSaberProj;
		maxTime = 0.5f;
		fadeOnAutoDestroy = true;
		fadeSprite = "zsaber_shot_fade";
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new XSaberProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class XSaberState : CharState {
	bool fired;
	bool grounded;
	MegamanX mmx = null!;
	public XSaberState(bool grounded) : base(grounded ? "beam_saber" : "beam_saber_air") {
		this.grounded = grounded;
		landSprite = "beam_saber";
		airSprite = "beam_saber_air";
		airMove = true;
		useDashJumpSpeed = true;
		canJump = true;
	}

	public override void update() {
		base.update();
		if (character.frameIndex >= 7 && !fired) {
			fired = true;
			character.playSound("zerosaberx3");
			new XSaberProj(
				character.pos.addxy(28 * character.xDir, -17), character.xDir,
				mmx, player, player.getNextActorNetId(), rpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
	public override void onEnter(CharState oldState) {
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		base.onEnter(oldState);
	}
}

public class X6SaberState : CharState {
	bool fired;
	bool grounded;
	public X6SaberState(bool grounded) : base(grounded ? "beam_saber2" : "beam_saber_air2") {
		this.grounded = grounded;
		airSprite = "beam_saber_air2";
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

		if (player?.character?.canCharge() == true && player.input.isHeld(Control.Shoot, player)) {
			player.character.increaseCharge();
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}
