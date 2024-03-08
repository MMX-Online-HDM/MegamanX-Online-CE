using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class Race : GameMode {
	public Race(Level level) : base(level, null) {
		isTeamMode = false;
		playingTo = 100;
		setupTime = Global.quickStart ? 2 : 12;
	}

	public override void render() {
		base.render();
		if (level?.goal == null) return;

		drawObjectiveNavpoint("Goal", level.goal.pos);
	}

	public List<Player> getSortedPlayers() {
		return level.nonSpecPlayers().OrderBy(p => {
			if (p.character != null) {
				return p.character.pos.distanceTo(Global.level.goal.pos);
			} else {
				return (p.lastDeathPos ?? new Point()).distanceTo(Global.level.goal.pos);
			}
		}).ToList();
	}

	public int getPlace(Player player) {
		var sortedPlayers = getSortedPlayers();
		int place = sortedPlayers.IndexOf(player) + 1;
		return place;
	}

	public override void drawTopHUD() {
		int place = getPlace(Global.level.mainPlayer);
		string placeStr = Helpers.getNthString(place);
		var topText = "Place: " + placeStr + "/" + level.nonSpecPlayers().Count;
		Fonts.drawText(FontType.BlueMenu, topText, 5, 5, Alignment.Left);
		drawTimeIfSet(15);
	}
}
