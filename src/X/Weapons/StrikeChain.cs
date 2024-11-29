using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class StrikeChain : Weapon {

	public static StrikeChain netWeapon = new();
	public StrikeChain() : base() {
		shootSounds = new string[] { "strikeChain", "strikeChain", "strikeChain", "strikeChainCharged" };
		fireRate = 45;
		index = (int)WeaponIds.StrikeChain;
		weaponBarBaseIndex = 14;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 14;
		killFeedIndex = 20 + (index - 9);
		weaknessIndex = (int)WeaponIds.SonicSlicer;
		//switchCooldown = 0;
		damage = "2/4";
		effect = "Hooks enemies and items. Be Spider-Man.";
		hitcooldown = "0.5";
		Flinch = "Hooked Time";
		FlinchCD = "0";
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		int upOrDown = 0;
		if (player.input.isHeld(Control.Up, player)) upOrDown = -1;
		else if (player.input.isHeld(Control.Down, player)) upOrDown = 1;

		if (chargeLevel >= 3) {
			new StrikeChainProjCharged(this, pos, xDir, player, player.getNextActorNetId(), upOrDown, true);
		} else {
			new StrikeChainProj(this, pos, xDir, player, player.getNextActorNetId(), upOrDown, true);
		}
	}
}

public class StrikeChainPullToWall : CharState {
	Projectile scp;
	public bool isUp;
	public StrikeChainPullToWall(
		Projectile scp, string lastSprite, bool isUp
	) : base(
		string.IsNullOrEmpty(lastSprite) ? "shoot" : lastSprite, "fall_shoot", "", ""
	) {
		this.scp = scp;
		this.isUp = isUp;
		useDashJumpSpeed = true;
	}

	public override void update() {
		base.update();
		if (scp == null || scp.destroyed) {
			character.changeToLandingOrFall();
			return;
		}
	}

	public override bool canEnter(Character character) {
		if (character.isStatusImmune()) return false;
		return base.canEnter(character);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		player.character.useGravity = false;
		player.character.vel.y = 0;
		character.changeSpriteFromName(sprite, true);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		player.character.useGravity = true;
		if (scp != null && !scp.destroyed) scp.destroySelf();
	}
}

public class StrikeChainHooked : CharState {
	float stunTime;
	Projectile scp;
	Point lastScpPos;
	bool isDone;
	bool flinch;
	bool isWireSponge;
	public StrikeChainHooked(
		Projectile scp, bool flinch
	) : base(
		flinch ? "hurt" : "fall", flinch ? "" : "fall_shoot", flinch ? "" : "fall_attack"
	) {
		this.scp = scp;
		this.flinch = flinch;
		isGrabbedState = true;
		lastScpPos = scp.pos;
		isWireSponge = scp is WSpongeSideChainProj;
	}

	public override bool canEnter(Character character) {
		if (!character.ownedByLocalPlayer) return false;
		if (!base.canEnter(character)) return false;
		if (character.isStatusImmune()) return false;
		return !character.charState.invincible;
	}

	public override bool canExit(Character character, CharState newState) {
		if (newState is Die) return true;
		return isDone;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		player.character.useGravity = false;
		player.character.vel.y = 0;
		if (player.character is Vile vile) {
			vile.rideArmorPlatform = null;
		}
		character.isStrikeChainState = true;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		player.character.useGravity = true;
		character.isStrikeChainState = false;
	}

	public override void update() {
		base.update();

		Character? scpChar = scp.damager?.owner?.character;
		if (scp is StrikeChainProj && scpChar != null && !isWireSponge) {
			if (scpChar.getShootXDir() == 1 && character.pos.x < scpChar.pos.x + 15) {
				stateTime = 5;
			} else if (scpChar.getShootXDir() == -1 && character.pos.x > scpChar.pos.x - 15) {
				stateTime = 5;
			}
		}

		if (scp.destroyed || stateTime >= 5) {
			player.character.useGravity = true;
			stunTime += Global.spf;
			if (!flinch || stunTime > 0.375f) {
				isDone = true;
				character.changeToLandingOrFall();
				return;
			}
		} else if (scpChar != null) {
			if (!isWireSponge) {
				Point newPos = character.pos.add(scp.deltaPos);
				if (newPos.distanceTo(scpChar.pos) < character.pos.distanceTo(scpChar.pos)) {
					character.move(scp.deltaPos, useDeltaTime: false);
				}
			} else {
				if (Math.Sign(scp.deltaPos.x) != scp.xDir) {
					character.move(scp.deltaPos, useDeltaTime: false);
				}
			}
		}
	}
}


public class StrikeChainProj : Projectile {

	int upOrDown;
	int startDir;
	MegamanX mmx;
	Player player;
	float dist;
	float distRetracted;
	float maxDist = 120;
	Point reverseVel;
	bool reversed; //Used for all cases (Hooking actors, pulling to walls or just returning to X)
	bool toWall;
	Point toWallVel;
	Actor? hookedActor;
	bool toActor;
	float hookWaitTime;

	public StrikeChainProj(
		Weapon weapon, Point pos, int xDir, Player player,
		ushort? netId, int upOrDown = 0, bool rpc = false
	) : base (
		weapon, pos, 1, 400, 2,
		player, "strikechain_proj", 0, 0.5f,
		netId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.StrikeChain;
		destroyOnHit = false;
		maxTime = 4;
		startDir = xDir;
		//xScale = 1;

		//Set character and player
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		mmx.strikeChainProj = this;
		this.player = player;

		//Reduce range if carrying a flag.
		if (mmx.flag != null) maxDist /= 2;

		//Set angle and speed.
		this.upOrDown = upOrDown;
		byteAngle = upOrDown * 32;
		if (xDir < 0) byteAngle = -byteAngle + 128;
		//if (byteAngle < 0) byteAngle += 256;
		vel = Point.createFromByteAngle(byteAngle.Value).times(speed);

		canBeLocal = false;

		if (rpc) {
			rpcCreateByteAngle(pos, player, netId, byteAngle.Value);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new StrikeChainProj(
			StrikeChain.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		//We destroy the chain if X faces to the opposite xDir
		if (mmx.xDir != startDir && mmx.charState is not WallSlide) {
			destroySelf();
			return;
		}

		// Hooked character? Wait for them to become hooked before pulling back. Wait a max of 200 ms
		var hookedChar = hookedActor as Character;
		if (hookedChar != null && !hookedChar.ownedByLocalPlayer && !hookedChar.isStrikeChainState) {
			hookWaitTime += Global.speedMul;
			if (hookWaitTime < 12) return;
		}
		
		//If pulled towards a wall
		if (toWall) {
			mmx.move(toWallVel);
			var collision = Global.level.checkTerrainCollisionOnce(mmx, (toWallVel.x * Global.spf), (toWallVel.y * Global.spf));
			if (collision?.gameObject is Wall) {
				destroySelf();
				float momentum = 0.25f * (distRetracted / maxDist);
				mmx.xSwingVel = toWallVel.x * (0.25f + momentum) * 0.5f;
				if (mmx.isDashing && mmx.player.hasBootsArmor(ArmorId.Giga) && mmx.flag == null) mmx.xSwingVel *= 1.1f;
				mmx.vel.y = toWallVel.y;
				//Yes, X2 Boots increase it.
			}	
		} 
		//Actor hooked
		//This only runs if hookedActor is a Pickup.
		//Characters uses StrikeChainHooked state.
		else if (toActor) {
			if (hookedActor != null && !(hookedActor is Character)) {
				if (!hookedActor.ownedByLocalPlayer) {
					hookedActor.takeOwnership();
					RPC.clearOwnership.sendRpc(hookedActor.netId);
				}
				hookedActor.useGravity = false;
				hookedActor.grounded = false;
				hookedActor.move(hookedActor.pos.directionTo(player.character.getCenterPos()).normalize().times(speed));
			}
			if (distRetracted >= dist + 10) {
				if (hookedActor != null && !(hookedActor is Character)) {
					hookedActor.changePos(player.character.getCenterPos());
					hookedActor.useGravity = true;
				}
				destroySelf();
			}
		}
		else {
			//If the chain reaches max range and didn't catch anything, it turns back.
			if (!reversed) {
				dist += Math.Abs(Global.speedMul * vel.magnitude) / 60;
				if (dist >= maxDist) reverse(vel);
			}
		}

		// We check if the chain came back to X or viceversa
		if (reversed) {
			distRetracted += Math.Abs(Global.speedMul * reverseVel.magnitude) / 60;
			if (distRetracted >= dist) {
				destroySelf();
				return;
			} 
		}
	}

	public override void postUpdate() {
		base.postUpdate();
		//Syncs with current buster position.
		Point shootPos = mmx.getShootPos();
		if (byteAngle != null) {
			changePos(
				new Point(
					shootPos.x + (dist - distRetracted) * Helpers.cosb(byteAngle.Value), 
					shootPos.y + (dist - distRetracted) * Helpers.sinb(byteAngle.Value)
				)
			);
		}
		
	}

	public void reverse(Point newVel) {
		vel.x *= -1;
		vel.y *= -1;
		reversed = true;
		reverseVel = newVel;
	}

	public void hookActor(Actor? actor) {
		toActor = true;
		hookedActor = actor;
		reverse(vel);
	}

	public override void onDestroy() {
		base.onDestroy();
		if (hookedActor != null) hookedActor.useGravity = true;
		mmx.strikeChainProj = null;
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (reversed) return;

		var wall = other.gameObject as Wall;

		if (wall != null && wall.collider.isClimbable && !toWall) {
			toWall = true;
			toWallVel = vel;
			if (mmx.flag != null) toWallVel.multiply(0.5f);
			stopMoving();
			reverse(toWallVel);
			if (mmx.grounded) mmx.incPos(new Point(0, -4));
			mmx.changeState(new StrikeChainPullToWall(this, mmx.charState.shootSprite, toWallVel.y < 0), true);
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer || hookedActor != null || reversed) return;
	
		var actor = other.gameObject as Actor;

		if (actor != null) {
			var chr = actor as Character;
			var pickup = actor as Pickup;
			if (chr == null && pickup == null) {
				//Lo quitamo' o miedo- I mean
				//It doesn't hook neither Mavericks nor Ride Armors.
				if (actor is Maverick || actor is RideArmor) {
					hookActor(null);
				}
				return;
			}
			//Character specific code.
			if (chr != null) {
				if (!chr.canBeDamaged(player.alliance, player.id, projId)) return;
				hookActor(actor);

				if (Global.serverClient != null) {
					RPC.commandGrabPlayer.sendRpc(netId, chr.netId, CommandGrabScenario.StrikeChain, false);
				}
				chr.hook(this);
			}
			//If chr is null, then it hooks a pickup
			else if (pickup != null) hookActor(actor);
		}
	}

	public override void render(float x, float y) {
		base.render(x,y);
		if (byteAngle == null) return;

		float length = dist - distRetracted;
		int pieceSize = 8;
		int maxI = MathInt.Floor(length / pieceSize);
		int chainFrame = Helpers.clamp((int)(5 * length / maxDist), 0, 5);
		float xOff = (length) * Helpers.cosb(byteAngle.Value);
		float yOff = (length) * Helpers.cosb(byteAngle.Value);

		DrawWrappers.DrawLine(
			pos.x, pos.y, mmx.getShootPos().x, mmx.getShootPos().y,
			new Color(206, 123, 239, 255), 4, ZIndex.Character - 2
		);

		for (int i = 0; i < maxI; i++) {
			xOff = (length - (pieceSize * i)) * Helpers.cosb(byteAngle.Value);
			yOff = (length - (pieceSize * i)) * Helpers.sinb(byteAngle.Value);
			
			Global.sprites["strikechain_chain"].draw(
				chainFrame, pos.x - xOff, pos.y - yOff, 
				xDir, yDir, getRenderEffectSet(), 1, 1, 1, 
				ZIndex.Character - 1, angle: byteAngle.Value * 1.40625f
			);
		}
	}
}


public class StrikeChainProjCharged : Projectile {

	int upOrDown;
	int startDir;
	MegamanX mmx;
	Player player;
	float dist;
	float distRetracted;
	float maxDist = 180;
	Point reverseVel;
	bool reversed; //Used for all cases (Hooking actors, pulling to walls or just returning to X)
	bool toWall;
	Point toWallVel;
	Actor? hookedActor;
	bool toActor;
	float hookWaitTime;

	public StrikeChainProjCharged(
		Weapon weapon, Point pos, int xDir, Player player,
		ushort? netId, int upOrDown = 0, bool rpc = false
	) : base (
		weapon, pos, 1, 600, 4,
		player, "strikechain_charged", 0, 0.5f,
		netId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.StrikeChain;
		destroyOnHit = false;
		maxTime = 4;
		startDir = xDir;
		//xScale = 1;

		//Set character and player
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		mmx.strikeChainChargedProj = this;
		this.player = player;

		//Reduce range if carrying a flag.
		if (mmx.flag != null) maxDist /= 2;

		//Set angle and speed.
		this.upOrDown = upOrDown;
		byteAngle = upOrDown * 32;
		if (xDir < 0) byteAngle = -byteAngle + 128;
		//if (byteAngle < 0) byteAngle += 256;
		vel = Point.createFromByteAngle(byteAngle.Value).times(speed);

		canBeLocal = false;

		if (rpc) {
			rpcCreateByteAngle(pos, player, netId, byteAngle.Value);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new StrikeChainProjCharged(
			StrikeChain.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		//We destroy the chain if X faces to the opposite xDir
		if (mmx.xDir != startDir && mmx.charState is not WallSlide) {
			destroySelf();
			return;
		}

		// Hooked character? Wait for them to become hooked before pulling back. Wait a max of 200 ms
		var hookedChar = hookedActor as Character;
		if (hookedChar != null && !hookedChar.ownedByLocalPlayer && !hookedChar.isStrikeChainState) {
			hookWaitTime += Global.speedMul;
			if (hookWaitTime < 12) return;
		}
		
		//If pulled towards a wall
		if (toWall) {
			mmx.move(toWallVel);
			var collision = Global.level.checkTerrainCollisionOnce(mmx, (toWallVel.x * Global.spf), (toWallVel.y * Global.spf));
			if (collision?.gameObject is Wall) {
				destroySelf();
				float momentum = 0.25f * (distRetracted / maxDist);
				mmx.xSwingVel = toWallVel.x * (0.25f + momentum) * 0.5f;
				if (mmx.isDashing && mmx.player.hasBootsArmor(ArmorId.Giga) && mmx.flag == null) mmx.xSwingVel *= 1.1f;
				mmx.vel.y = toWallVel.y;
				//Yes, X2 Boots increase it.
			}	
		} 
		//Actor hooked
		//This only runs if hookedActor is a Pickup.
		//Characters uses StrikeChainHooked state.
		else if (toActor) {
			if (hookedActor != null && !(hookedActor is Character)) {
				if (!hookedActor.ownedByLocalPlayer) {
					hookedActor.takeOwnership();
					RPC.clearOwnership.sendRpc(hookedActor.netId);
				}
				hookedActor.useGravity = false;
				hookedActor.grounded = false;
				hookedActor.move(hookedActor.pos.directionTo(player.character.getCenterPos()).normalize().times(speed));
			}
			if (distRetracted >= dist + 10) {
				if (hookedActor != null && !(hookedActor is Character)) {
					hookedActor.changePos(player.character.getCenterPos());
					hookedActor.useGravity = true;
				}
				destroySelf();
			}
		}
		else {
			//If the chain reaches max range and didn't catch anything, it turns back.
			if (!reversed) {
				dist += Math.Abs(Global.speedMul * vel.magnitude) / 60;
				if (dist >= maxDist) reverse(vel);
			}
		}

		// We check if the chain came back to X or viceversa
		if (reversed) {
			distRetracted += Math.Abs(Global.speedMul * reverseVel.magnitude) / 60;
			if (distRetracted >= dist) {
				destroySelf();
				return;
			} 
		}
	}

	public override void postUpdate() {
		base.postUpdate();
		//Syncs with current buster position.
		Point shootPos = mmx.getShootPos();
		if (byteAngle != null) {
			changePos(
				new Point(
					shootPos.x + (dist - distRetracted) * Helpers.cosb(byteAngle.Value), 
					shootPos.y + (dist - distRetracted) * Helpers.sinb(byteAngle.Value)
				)
			);
		}
		
	}

	public void reverse(Point newVel) {
		vel.x *= -1;
		vel.y *= -1;
		reversed = true;
		reverseVel = newVel;
	}

	public void hookActor(Actor? actor) {
		toActor = true;
		hookedActor = actor;
		reverse(vel);
	}

	public override void onDestroy() {
		base.onDestroy();
		if (hookedActor != null) hookedActor.useGravity = true;
		mmx.strikeChainChargedProj = null;
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (reversed) return;

		var wall = other.gameObject as Wall;

		if (wall != null && wall.collider.isClimbable && !toWall) {
			toWall = true;
			toWallVel = vel;
			if (mmx.flag != null) toWallVel.multiply(0.5f);
			stopMoving();
			reverse(toWallVel);
			if (mmx.grounded) mmx.incPos(new Point(0, -4));
			mmx.changeState(new StrikeChainPullToWall(this, mmx.charState.shootSprite, toWallVel.y < 0), true);
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer || hookedActor != null || reversed) return;
	
		var actor = other.gameObject as Actor;

		if (actor != null) {
			var chr = actor as Character;
			var pickup = actor as Pickup;
			if (chr == null && pickup == null) {
				//Lo quitamo' o miedo- I mean
				//It doesn't hook neither Mavericks nor Ride Armors.
				if (actor is Maverick || actor is RideArmor) {
					hookActor(null);
				}
				return;
			}
			//Character specific code.
			if (chr != null) {
				if (!chr.canBeDamaged(player.alliance, player.id, projId)) return;
				hookActor(actor);

				if (Global.serverClient != null) {
					RPC.commandGrabPlayer.sendRpc(netId, chr.netId, CommandGrabScenario.StrikeChain, false);
				}
				chr.hook(this);
			}
			//If chr is null, then it hooks a pickup
			else if (pickup != null) hookActor(actor);
		}
	}

	public override void render(float x, float y) {
		base.render(x,y);
		if (byteAngle == null) return;

		float length = dist - distRetracted;
		int pieceSize = 8;
		int maxI = MathInt.Floor(length / pieceSize);
		int chainFrame = Helpers.clamp((int)(5 * length / maxDist), 0, 5);
		float xOff = (length) * Helpers.cosb(byteAngle.Value);
		float yOff = (length) * Helpers.cosb(byteAngle.Value);

		DrawWrappers.DrawLine(
			pos.x, pos.y, mmx.getShootPos().x, mmx.getShootPos().y,
			new Color(206, 123, 239, 255), 4, ZIndex.Character - 2
		);

		for (int i = 0; i < maxI; i++) {
			xOff = (length - (pieceSize * i)) * Helpers.cosb(byteAngle.Value);
			yOff = (length - (pieceSize * i)) * Helpers.sinb(byteAngle.Value);
			
			Global.sprites["strikechain_charged_chain"].draw(
				chainFrame, pos.x - xOff, pos.y - yOff, 
				xDir, yDir, getRenderEffectSet(), 1, 1, 1, 
				ZIndex.Character - 1, angle: byteAngle.Value * 1.40625f
			);
		}
	}
}
