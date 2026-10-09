using System.Collections.Generic;

namespace MMXOnline;

public interface IDamagable {
	void applyDamage(float damage, Player owner, Actor? actor, int? weaponIndex, int? projId);
	Dictionary<string, float> projectileCooldown { get; set; }
	bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId);
	bool isInvincible(Player attacker, int? projId) => false;
	bool canBeHealed(int healerAlliance) => canBeHealed();
	bool canBeHealed() => false;
	void heal(Player healer, float healAmount, bool drawHealText = true) {}
	bool isPlayableDamagable() => false;
}

public class DamageText {
	public string text;
	public float time;
	public Point pos;
	public Point offset;
	public Point vel;
	public int color;

	public DamageText(string text, float time, Point pos, Point offset, int color) {
		this.text = text;
		this.time = time;
		this.pos = pos;
		this.offset = offset;
		this.color = color;
		this.vel = new Point(0, 0);
	}
}
