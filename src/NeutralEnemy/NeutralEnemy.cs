using System;
using System.Collections.Generic;

namespace MMXOnline;

public class NeutralEnemy : Actor, IDamagable {
	public int alliance;
	public decimal health = 8;
	public decimal maxHealth = 8;
	public bool invincibleFlag;
	public int enemyId;
	public float wSize = 12;
	public float hSize = 12;
	public Action<Point>[] killDrops = new Action<Point>[0];

	public NeutralEnemyState state;

	public NeutralEnemy(
		Point pos, ushort netId, bool isLocal,
		int alliance = GameMode.freelanceAlliance,
		bool addToLevel = true
	) : base(
		null!, pos, netId, isLocal, !addToLevel
	) {
		// Forcefull change sprite to something before we crash.
		sprite = new Sprite("empty");
		// We do this to manually call the state change.
		// As oldState cannot be null because we do not want null crashes.
		state = new NeutralEnemyState("empty");
		state.chara = this;
		// Then now that we set up a dummy state we call the actual changeState.
		// Only do this for the local player as we do not want other player to run state code.
		if (ownedByLocalPlayer) {
			changeState(new NeIdle());
		}

		this.alliance = alliance;
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}
	// For state update.
	public override void statePreUpdate() {
		state.stateTime += Global.speedMul;
		state.preUpdate();
	}
	public override void stateUpdate() {
		state.update();
	}
	public override void statePostUpdate() {
		state.postUpdate();
	}

	// Sprite change override.
	public virtual string getSprite(string spriteName) {
		if (spriteName is null or "") {
			return "";
		}
		return spriteName;
	}

	public virtual void changeState(NeutralEnemyState newState) {
		// Set the character as soon as posible.
		newState.chara = this;
		// Sprites.
		string oldSprite = sprite.name;
		string newSprite = getSprite(sprite.name);
		if (newSprite != "" && Global.sprites.ContainsKey(newSprite)) {
			changeSprite(getSprite(newState.sprite), true);
		}
		// Exit/Enter shenanigans.
		NeutralEnemyState oldState = state;
		oldState.onExit(newState);
		state = newState;
		newState.onEnter(oldState);
	}

	// For normal collision.
	public override Collider? getGlobalCollider() {
		return new Collider(
			new Rect(0, 0, wSize, hSize).getPoints(),
			false, this, false, false,
			HitboxFlag.Hurtbox, Point.zero
		);
	}

	// For terrain collision.
	public override Collider? getTerrainCollider() {
		if (physicsCollider == null) {
			return null;
		}
		return new Collider(
			new Rect(0, 0, wSize, hSize).getPoints(),
			false, this, false, false,
			HitboxFlag.Hurtbox, Point.zero
		);
	}

	// IDamagable interface bello.
	public void applyDamage(float damage, Player owner, Actor actor, int? weaponIndex, int? projId) {
		health -= (decimal)damage;
		if (health <= 0) {
			health = 0;
			if (ownedByLocalPlayer) {
				destroySelf();
			}
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return (damagerAlliance != alliance && health > 0);
	}

	public bool canBeHealed(int healerAlliance) {
		return (healerAlliance == alliance && health < maxHealth);
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = true) {
		commonHealLogic(healer, healAmount, (float)health, (float)maxHealth, drawHealText);
		health += (decimal)healAmount;
		if (health > maxHealth) {
			health = maxHealth;
		}
	}

	public bool isInvincible(Player attacker, int? projId) {
		return invincibleFlag;
	}

	public bool isPlayableDamagable() {
		return true;
	}

	// Sends net data to update.
	public override List<byte> getCustomActorNetData() {
		List<byte> customData = new();
		customData.Add((byte)health);
		customData.Add((byte)(invincibleFlag ? 1 : 0));

		return customData;
	}

	// Updates data recieved.
	public override void updateCustomActorNetData(byte[] data) {
		health = (data[0]);
		invincibleFlag = (data[1] == 1);
	}
}
