using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;

namespace MMXOnline;

public class BatchDrawable : Transformable, Drawable {
	public VertexArray vertices;
	public Texture texture;

	public BatchDrawable(Texture texture) {
		vertices = new VertexArray();
		vertices.PrimitiveType = PrimitiveType.Quads;
		this.texture = texture;
	}

	public void Draw(RenderTarget target, RenderStates states) {
		states.Transform *= Transform;
		states.Texture = texture;
		target.Draw(vertices, states);
	}
}

// Draw wrappers and font code.
public partial class DrawWrappers {
	public static View hudView = null!;
	public static List<Action> deferredTextDraws = new List<Action>();
	public static void initHUD() {
		hudView = new View(
			new Vector2f(Global.halfScreenW, Global.halfScreenH),
			new Vector2f(Global.screenW, Global.screenH)
		);
	}
	public static void drawToHUD(Drawable drawable) {
		Global.window.SetView(hudView);
		Global.window.Draw(drawable);
		Global.window.SetView(Global.view);
	}

	public static void DrawTextureHUD(
		Texture texture, float sx, float sy, float sw, float sh, float dx, float dy, float alpha = 1
	) {
		if (texture == null) return;
		var sprite = new SFML.Graphics.Sprite(texture, new IntRect((int)sx, (int)sy, (int)sw, (int)sh));
		sprite.Position = new Vector2f(dx, dy);
		sprite.Color = new Color(255, 255, 255, (byte)(int)(alpha * 255));
		drawToHUD(sprite);
	}

	public static void DrawTextureHUD(Texture texture, float x, float y) {
		if (texture == null) return;
		var sprite = new SFML.Graphics.Sprite(texture);
		sprite.Position = new Vector2f(x, y);
		drawToHUD(sprite);
	}

	public static void addToVertexArray(BatchDrawable bd, SFML.Graphics.Sprite sprite) {
		float sx = sprite.TextureRect.Left;
		float sy = sprite.TextureRect.Top;
		float sw = sprite.TextureRect.Width;
		float sh = sprite.TextureRect.Height;
		float dx = sprite.Position.X;
		float dy = sprite.Position.Y;
		float scale = sprite.Scale.X;
		Color color = sprite.Color;

		float width = sw * scale;
		float height = sh * scale;

		Vertex vertex1 = new Vertex(new Vector2f(dx, dy), color);
		Vertex vertex2 = new Vertex(new Vector2f(dx, dy + height), color);
		Vertex vertex3 = new Vertex(new Vector2f(dx + width, dy + height), color);
		Vertex vertex4 = new Vertex(new Vector2f(dx + width, dy), color);

		vertex1.TexCoords = new Vector2f(sx, sy);
		vertex2.TexCoords = new Vector2f(sx, sy + sh);
		vertex3.TexCoords = new Vector2f(sx + sw, sy + sh);
		vertex4.TexCoords = new Vector2f(sx + sw, sy);

		bd.vertices.Append(vertex1);
		bd.vertices.Append(vertex2);
		bd.vertices.Append(vertex3);
		bd.vertices.Append(vertex4);
	}
}
