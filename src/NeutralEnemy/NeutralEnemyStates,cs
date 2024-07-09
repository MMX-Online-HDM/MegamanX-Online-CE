using System;

namespace MMXOnline;

public class NeutralEnemyState {
	public NeutralEnemy chara = null!;
	public string sprite;
	public bool normalCtrl;
	public bool attackCtrl;
	public float stateTime;

	public NeutralEnemyState(String sprite) {
		this.sprite = sprite;
	}

	public virtual void preUpdate() { }
	public virtual void update() { }
	public virtual void postUpdate() { }

	public virtual void onEnter(NeutralEnemyState oldState) { }
	public virtual void onExit(NeutralEnemyState newState) { }
}

public class NeIdle : NeutralEnemyState {
	public NeIdle() : base("idle") {
		normalCtrl = true;
		attackCtrl = true;
	}
}
