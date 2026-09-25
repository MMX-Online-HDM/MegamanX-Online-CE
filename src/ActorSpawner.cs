using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ActorSpawner {
	public string[] types;
	public ActorLocalCreate createFunct;
	public Point pos;
	public float respawnTime = 60 * 60;
	public float time;
	public Actor? currentActor;
	public int teamSide;
	public int xDir;

	public ActorSpawner(string[] types, Point pos, int xDir, int teamSide, int? respawnTime) {
		this.types = types;
		this.pos = pos;
		this.xDir = xDir;
		this.teamSide = teamSide;

		if (teamSide < 0) {
			teamSide = GameMode.stageEnemyAlliance;
		}
		if (respawnTime != null) {
			this.respawnTime = MathF.Ceiling(respawnTime.Value * 60);
		}
		time = 2;

		// Default to met if we cannot find anything.
		createFunct = Met.localInvoke;

		// Iterate each subtype searching for a matching enemy.
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
			extraData = [teamSide]
		}, true);
	}


	public static Dictionary<string, ActorLocalCreate> functs = new() {
		{ "met", Met.localInvoke }
	};
}
