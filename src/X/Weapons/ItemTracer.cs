using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ItemTracer : Weapon {
	public static ItemTracer netWeapon = new();

	public ItemTracer() : base() {
		shootSounds = new string[] { "", "", "", "" };
		fireRate = 60;
		index = (int)WeaponIds.ItemTracer;
		weaponBarBaseIndex = 26;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 26;
		killFeedIndex = 20 + (index - 9);
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		Character? target = null;
		character.playSound("itemTracer", sendRpc: true);
		CollideData hit = Global.level.raycast(
			character.pos, character.pos.addxy(150 * character.xDir, 0), new List<Type>() { typeof(Actor) }
		);
		if (hit?.gameObject is Character chr && chr.player.alliance != player.alliance && !chr.player.scanned) {
			target = chr;
		}
		new ItemTracerProj(
			character.getHeadPos() ?? pos, xDir, mmx,
			player, target, player.getNextActorNetId(), rpc: true
		);
	}
}

public class ItemTracerProj : Projectile {
	public Character? target;
	public Character? scannedChar;
	public ItemTracerProj(
		Point pos, int xDir, Actor owner, Player player, Character? target, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "itemscan_proj", netId, player	
	) {
		weapon = ItemTracer.netWeapon;
		damager.hitCooldown = 30;
		vel = new Point(300 * xDir, 0);
		maxTime = 1f;
		destroyOnHit = false;
		shouldShieldBlock = false;
		frameSpeed = 0;
		projId = (int)ProjIds.ItemTracer;
		this.target = target;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}

		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ItemTracerProj(
			args.pos, args.xDir, args.owner, args.player, null, args.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		if (scannedChar != null) {
			changePos(scannedChar.getCenterPos());
		}
		if (target != null) {
			vel = pos.directionTo(target.getCenterPos()).normalize().times(300);
		}
		if (isAnimOver()) {
			destroySelf();
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!ownedByLocalPlayer) return;
		var chr = damagable as Character;
		if (scannedChar == null && chr != null && !chr.isStealthy(damager.owner.alliance)) {
			scannedChar = chr;
			if (damager.owner == Global.level.mainPlayer) {
				if (scannedChar.player.scanned) {
					foreach (var player in Global.level.players) {
						player.tagged = false;
					}
					scannedChar.player.tagged = true;
					playSound("itemTracerTarget", sendRpc: true);
				}
				scannedChar.player.scanned = true;
			}
			frameSpeed = 1;
			time = 0;
		}
	}
}
