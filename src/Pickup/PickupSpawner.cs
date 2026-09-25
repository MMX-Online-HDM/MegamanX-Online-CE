using System;
using System.Collections.Generic;

namespace MMXOnline;

public class PickupSpawner {
	public string[] types;
	public ActorLocalCreate createFunct;
	public Point pos;
	public float respawnTime = 60 * 15;
	public float time;
	public Actor? currentActor;
	public int xDir;

	public PickupSpawner(string[] types, Point pos, int xDir, int? respawnTime) {
		this.types = types;
		this.pos = pos;
		this.xDir = xDir;

		// Respawn stuff.
		if (respawnTime != null) {
			this.respawnTime = MathF.Ceiling(respawnTime.Value * 60);
		}
		time = 2;

		// Default to Small Hp if we cannot find anything.
		createFunct = SmallHealthPickup.pickupInvoke;

		// Iterate each subtype searching for a matching pickup.
		// Settle on the first found.
		foreach (string type in types) {
			if (functs.ContainsKey(type)) {
				createFunct = functs[type];
				break;
			}
		}
	}

	public void update() {
		if (!Global.isHost || currentActor?.destroyed == false) {
			time = respawnTime;
			return;
		}

		if (time > 0) {
			time -= Global.gameSpeed;
			return;
		}
		time = respawnTime;

		currentActor = createFunct(new ActorLocalParameters() {
			pos = pos,
			xDir = xDir,
			byteAngle = 0,
			player = Global.level.mainPlayer,
			netId = Global.level.mainPlayer.getNextActorNetId(),
		}, true);
	}


	public static Dictionary<string, ActorLocalCreate> functs = new() {
		// HP
		{ "largehealth", LargeHealthPickup.pickupInvoke },
		{ "smallhealth", SmallHealthPickup.pickupInvoke },
		{ "health", SmallHealthPickup.pickupInvoke },
		// Ammo
		{ "largeammo", LargeAmmoPickup.pickupInvoke },
		{ "smallammo", SmallAmmoPickup.pickupInvoke },
		{ "ammo", SmallAmmoPickup.pickupInvoke },
	};
}
