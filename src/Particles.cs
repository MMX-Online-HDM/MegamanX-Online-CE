using System;
using System.Collections.Generic;
using System.Numerics;
using SFML.System;
using SFML.Graphics;
using System.Threading.Tasks;

namespace MMXOnline;

public class ParticleSystem : Transformable, IDrawable {
	public struct Particle {
		public Vector2f velocity;
		public float lifetime;
	}
	// Holds particle data.
	public Particle[] particles;
	// Vertex is his own list for drawing optimization.
	public Vertex[] vertices;

	public TimeSpan lifetime = TimeSpan.FromSeconds(3);
	public Vector2f emitter;
	public Random rng;
	public long zIndex;

	public ParticleSystem(int count) {
		// Init stuff.
		particles = new Particle[count];
		vertices = new Vertex[count];
		for (int i = 0; i < count; i++) {
			particles[i] = new();;
			vertices[i] = new();
			resetParticle(i);
			particles[i].lifetime = i % 60;
		}
		// We use the main seed for deteriministic replay purposes.
		rng = new Random(Helpers.randomRange(int.MinValue, int.MaxValue - 1));
	}

	public virtual void update(float speedMul) {
		int taskCount = MathInt.Floor(vertices.Length / 64f);
		int taskLength = MathInt.Ceiling(vertices.Length / (float)taskCount);

		List<Task> tasks = [];
		for (int i = 0; i < vertices.Length; i += taskLength) {
			int j = i;
			Task task = new Task(() => updateTask(j, taskLength, speedMul));
			tasks.Add(task);
			task.Start();
		}
		while (tasks.Count > 0) {
			for (int i = tasks.Count - 1; i >= 0; i--) {
				if (tasks[i].Status >= TaskStatus.RanToCompletion) {
					tasks.Remove(tasks[i]);
				}
			}
		}
	}

	public virtual void updateTask(int start, int count, float speedMul) {
		for (int j = 0; j < count; j++) {
			// Bound checks.
			int i = j + start;
			if (i >= vertices.Length) { break; }
			// Update the particle lifetime.
			particles[i].lifetime -= speedMul;

			// If the particle is dead, respawn it.
			if (particles[i].lifetime <= 0) {
				resetParticle(i);
			}

			// Update the position of the corresponding vertex.
			vertices[i].Position += particles[i].velocity * speedMul;
			particles[i].velocity.Y -= 0.05f;
			// Update the alpha (transparency) of the particle according to its lifetime
			float life = Math.Min(particles[i].lifetime, 40);
			vertices[i].Color = new Color(240, 32, 112, (byte)(255 * (life / 40)));
		}
	}

	public virtual void resetParticle(int index) {
		// Give a random velocity and lifetime to the particle
		float speedX = Helpers.randomRange(-150, 150) / 100f;
		float speedY = Helpers.randomRange(-150, 150) / 100f;

		particles[index].velocity = (speedX, speedY);
		particles[index].lifetime = 60;
		vertices[index].Position = emitter;
	}

	public void render() {
		DrawLayer drawLayer = DrawWrappers.getDrawLayer(zIndex);
		drawLayer.oneOffs.Add(new DrawableWrapper([], this, Color.White));
	}

	public void Draw(IRenderTarget target, RenderStates states) {
		// Get default pos;
		//Vector2f offset = (MathF.Round(-Global.level.camX), MathF.Round(-Global.level.camY));
		// Apply the transform.
		//states.Transform *= Transform;
		// Our particles don't use a texture.
		states.Texture = null;
		// Draw the vertex array.
		target.Draw(vertices, PrimitiveType.Points, states);
	}
}