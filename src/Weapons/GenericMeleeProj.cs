namespace MMXOnline;

public class GenericMeleeProj : Projectile {
	public GenericMeleeProj(
		Weapon weapon, Point pos, ProjIds projId, Player player,
		float? damage = null, int? flinch = null, float? hitCooldown = null,
		Actor? owningActor = null, bool isShield = false, bool isDeflectShield = false, bool isReflectShield = false,
		bool addToLevel = true
	) : base(
		weapon, pos, 1, 0, 2, player, "empty", 0, 0.25f, null, player.ownedByLocalPlayer, addToLevel: addToLevel
	) {
		destroyOnHit = false;
		shouldVortexSuck = false;
		shouldShieldBlock = false;
		this.projId = (int)projId;
		damager.damage = damage ?? weapon.damager.damage;
		damager.flinch = flinch ?? weapon.damager.flinch;
		damager.hitCooldown = hitCooldown ?? weapon.damager.hitCooldown;
		if (damager.hitCooldown == 0) {
			damager.hitCooldown = 0.5f;
		}
		this.owningActor = owningActor;
		this.xDir = owningActor?.xDir ?? player.character?.xDir ?? 1;
		this.isShield = isShield;
		this.isDeflectShield = isDeflectShield;
		this.isReflectShield = isReflectShield;
		isMelee = true;
	}

	public override void update() {
		base.update();
	}

	public void charGrabCode(
		CommandGrabScenario scenario, Character? grabber,
		IDamagable? damagable, CharState grabState, CharState grabbedState
	) {
		if (grabber != null && damagable is Character grabbedChar && grabbedChar.canBeGrabbed()) {
			if (!owner.isDefenderFavored) {
				if (ownedByLocalPlayer && !Helpers.isOfClass(grabber.charState, grabState.GetType())) {
					owner.character.changeState(grabState, true);
					if (Global.isOffline) {
						grabbedChar.changeState(grabbedState, true);
					} else {
						RPC.commandGrabPlayer.sendRpc(grabber.netId, grabbedChar.netId, scenario, false);
					}
				}
			} else {
				if (grabbedChar.ownedByLocalPlayer &&
					!Helpers.isOfClass(grabbedChar.charState, grabbedState.GetType())
				) {
					grabbedChar.changeState(grabbedState);
					if (Helpers.isOfClass(grabbedChar.charState, grabbedState.GetType())) {
						RPC.commandGrabPlayer.sendRpc(grabber.netId, grabbedChar.netId, scenario, true);
					}
				}
			}
		}
	}

	public void maverickGrabCode(CommandGrabScenario scenario, Maverick grabber, IDamagable damagable, CharState grabbedState) {
		if (damagable is Character chr && chr.canBeGrabbed()) {
			if (!owner.isDefenderFavored) {
				if (ownedByLocalPlayer && grabber.state.trySetGrabVictim(chr)) {
					if (Global.isOffline) {
						chr.changeState(grabbedState, true);
					} else {
						RPC.commandGrabPlayer.sendRpc(grabber.netId, chr.netId, scenario, false);
					}
				}
			} else {
				if (chr.ownedByLocalPlayer && !Helpers.isOfClass(chr.charState, grabbedState.GetType())) {
					chr.changeState(grabbedState);
					if (Helpers.isOfClass(chr.charState, grabbedState.GetType())) {
						RPC.commandGrabPlayer.sendRpc(grabber.netId, chr.netId, scenario, true);
					}
				}
			}
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);

		if (projId == (int)ProjIds.QuakeBlazer) {
			if (owner.character?.charState is ZeroDownthrust hyouretsuzanState) {
				hyouretsuzanState.quakeBlazerExplode(false);
			}
		}

		// Command grab section
		Character? grabberChar = owner.character;
		Character? grabbedChar = damagable as Character;
		switch (projId) {
			case (int)ProjIds.UPGrab:
				charGrabCode(CommandGrabScenario.UPGrab, grabberChar, damagable, new XUPGrabState(grabbedChar), new UPGrabbed(grabberChar));
				break;
			case (int)ProjIds.VileMK2Grab:
				charGrabCode(CommandGrabScenario.MK2Grab, grabberChar, damagable, new VileMK2GrabState(grabbedChar), new VileMK2Grabbed(grabberChar));
				break;
			case (int)ProjIds.LaunchODrain when owningActor is LaunchOctopus lo:
				maverickGrabCode(CommandGrabScenario.WhirlpoolGrab, lo, damagable, new WhirlpoolGrabbed(lo));
				break;
			case (int)ProjIds.FStagUppercut when owningActor is FlameStag fs:
				maverickGrabCode(CommandGrabScenario.FStagGrab, fs, damagable, new FStagGrabbed(fs));
				break;
			case (int)ProjIds.WheelGGrab when owningActor is WheelGator wg:
				maverickGrabCode(CommandGrabScenario.WheelGGrab, wg, damagable, new WheelGGrabbed(wg));
				break;
			case (int)ProjIds.MagnaCTail when owningActor is MagnaCentipede ms:
				maverickGrabCode(CommandGrabScenario.MagnaCGrab, ms, damagable, new MagnaCDrainGrabbed(ms));
				break;
			case (int)ProjIds.BoomerangKDeadLift when owningActor is BoomerangKuwanger bk:
				maverickGrabCode(CommandGrabScenario.DeadLiftGrab, bk, damagable, new DeadLiftGrabbed(bk));
				break;
			case (int)ProjIds.GBeetleLift when owningActor is GravityBeetle gb:
				maverickGrabCode(CommandGrabScenario.BeetleLiftGrab, gb, damagable, new BeetleGrabbedState(gb));
				break;
			case (int)ProjIds.CrushCGrab when owningActor is CrushCrawfish cc:
				maverickGrabCode(CommandGrabScenario.CrushCGrab, cc, damagable, new CrushCGrabbed(cc));
				break;
			case (int)ProjIds.BBuffaloDrag when owningActor is BlizzardBuffalo bb:
				maverickGrabCode(CommandGrabScenario.BBuffaloGrab, bb, damagable, new BBuffaloDragged(bb));
				break;
		}
	}

	public override DamagerMessage? onDamage(IDamagable? damagable, Player? attacker) {	
		Point? hitPoint = (damagable as Actor)?.getCenterPos() ?? new Point(0,0);
		Collider? hitbox = getGlobalCollider();
		Collider? collider = (damagable as Actor)?.collider;
		if (hitbox?.shape != null && collider?.shape != null) {
			var hitboxCenter = hitbox.shape.getRect().center();
			var hitCenter = collider.shape.getRect().center();
			hitPoint = new Point((hitboxCenter.x + hitCenter.x) * 0.5f, (hitboxCenter.y + hitCenter.y) * 0.5f);
		}
		string SaberShotFade = "zsaber_shot_fade";
		string SaberSlashFade = "zsaber_slash_fade";
		string SparkVerticalFade = "sword_sparks_vertical";
		//string SparkElectricFade = "tunnelfang_sparks";
		//string PunchSpark = "sword_sparks_horizontal";
		if (ownedByLocalPlayer) {
			if (isZSaberEffect() || projId == (int)ProjIds.X6Saber || projId == (int)ProjIds.XSaber) {
				new Anim(hitPoint.Value, SaberShotFade, xDir,
					Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true);
			}
			switch (projId) {
				case (int)ProjIds.ZSaber1:
				case (int)ProjIds.ZSaberRollingSlash:
				case (int)ProjIds.ZSaberAir: 
				
					new Anim(hitPoint.Value, SaberSlashFade, xDir,
						Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true);
					break;
				case (int)ProjIds.ZSaber2: 
					new Anim(hitPoint.Value, SaberSlashFade, xDir*-1,
						Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true);
					break;
				case (int)ProjIds.Rakukojin: 
					new Anim(hitPoint.Value, SparkVerticalFade, xDir,
						Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true);
					break;
			/*	case (int)ProjIds.Raijingeki: We need better hit effect sprites
				case (int)ProjIds.Raijingeki2: 
					new Anim(hitPoint.Value, SparkElectricFade, xDir,
						Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true);
					break;
				case (int)ProjIds.PZeroPunch: 
				case (int)ProjIds.PZeroPunch2: 
					new Anim(hitPoint.Value, PunchSpark, xDir,
						Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true);
					break; */
			}
		}
		return null;
	}

	public bool isZSaberEffect() {
		return 
		/* projId == (int)ProjIds.ZSaber1 || */ projId == (int)ProjIds.ZSaber3 || /* projId == (int)ProjIds.ZSaber2 || */
		projId == (int)ProjIds.ZSaberCrouch || projId == (int)ProjIds.ZSaberDash ||	projId == (int)ProjIds.DZMelee ||
		projId == (int)ProjIds.ZSaberLadder || projId == (int)ProjIds.ZSaberslide || projId == (int)ProjIds.Shippuuga || 
		/*projId == (int)ProjIds.ZSaberAir || */ projId == (int)ProjIds.RisingFang /*|| projId == (int)ProjIds.ZSaberRollingSlash*/;
	}
	public static bool isZSaberClang(int projId) {
 		// Turns out that, isZSaber bool (now, isZSaberEffect) is used on clanging, and i modified it to match with the fancy looks
		// This one will be used for clanging purpose, also, original bool literally missed ZSaber2 and many others..
		// how do you.. clang on Ladder? or.. Wall slash???.
		return projId == (int)ProjIds.ZSaber1 || projId == (int)ProjIds.ZSaber2 || projId == (int)ProjIds.ZSaber3 ||
		 	   projId == (int)ProjIds.ZSaberAir || projId == (int)ProjIds.ZSaberCrouch || projId == (int)ProjIds.ZSaberDash || 
			   projId == (int)ProjIds.ZSaberLadder || projId == (int)ProjIds.ZSaberslide || projId == (int)ProjIds.ZSaberProjSwing ||
			   projId == (int)ProjIds.ZSaberRollingSlash || projId == (int)ProjIds.DZMelee; 
			   //i wonder if Shippuga could count as Z-Saber or Rising too, but this last wouldn't make sense as is an uppercut
	}

	public override void onDestroy() {
		base.onDestroy();
	}
}
