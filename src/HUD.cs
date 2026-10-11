using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;
using SFML.System;

namespace MMXOnline;

public class BatchDrawable : Transformable, IDrawable {
	public VertexArray vertices;
	public Texture texture;

	public BatchDrawable(Texture texture) {
		vertices = new VertexArray();
		vertices.PrimitiveType = PrimitiveType.Triangles;
		this.texture = texture;
	}

	public void Draw(IRenderTarget target, RenderStates states) {
		states.Transform *= Transform;
		states.Texture = texture;
		target.Draw(vertices, states);
	}
}

// Draw wrappers and font code.
public partial class DrawWrappers {
	public static View hudView = null!;
	public static RenderTexture renderTexture = null!;
	public static List<Action> deferredTextDraws = new List<Action>();

	public static void initHUD() {
		hudView = new View(
			new Vector2f(Global.halfScreenW, Global.halfScreenH),
			new Vector2f(Global.screenW, Global.screenH)
		);
	}
	public static void drawToHUD(IDrawable drawable) {
		renderTexture.Draw(drawable);
		renderTexture.Display();
	}

	public static void drawToHUD(Vertex[] vertices, PrimitiveType type) {
		renderTexture.Draw(vertices, type);
		renderTexture.Display();
	}

	public static void DrawTextureHUD(
		Texture texture, float sx, float sy, float sw, float sh, float dx, float dy, float alpha = 1
	) {
		if (texture == null) return;
		var sprite = new SFML.Graphics.Sprite(texture, new IntRect(((int)sx, (int)sy), ((int)sw, (int)sh)));
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

		Vertex vertexTL = new Vertex(new Vector2f(dx, dy), color);
		Vertex vertexBL = new Vertex(new Vector2f(dx, dy + height), color);
		Vertex vertexBR = new Vertex(new Vector2f(dx + width, dy + height), color);
		Vertex vertexTR = new Vertex(new Vector2f(dx + width, dy), color);

		vertexTL.TexCoords = new Vector2f(sx, sy);
		vertexBL.TexCoords = new Vector2f(sx, sy + sh);
		vertexBR.TexCoords = new Vector2f(sx + sw, sy + sh);
		vertexTR.TexCoords = new Vector2f(sx + sw, sy);
		// Top left.
		bd.vertices.Append(vertexTL);
		bd.vertices.Append(vertexBL);
		bd.vertices.Append(vertexBR);
		// Bottom rigth.
		bd.vertices.Append(vertexTR);
		bd.vertices.Append(vertexTL);
		bd.vertices.Append(vertexBR);
	}
}

public class HUD {
	public GameMode gameMode;
	public Level level;

	public HUD(Level level) {
		this.level = level;
		gameMode = new(level, 0);
	}

	public void render() {
		if (level.mainPlayer == null) return;
		if (DevConsole.showConsole) {
			return;
		}

		Player? targetPlayer = Global.level.mainPlayer;
		if (Global.level.mainPlayer.isSpectator || Global.level.mainPlayer.altSpectator) {
			targetPlayer = level.specPlayer;
		}
		if (targetPlayer != null) {
			drawPlayer(targetPlayer);
		}
		drawRadar();
		gameMode.render();
	}

	public void drawPlayer(Player player) {
		if (Global.level.mainPlayer == player) {
			renderHealthAndWeapons();
		} else {
			renderHealthAndWeapon(player, GameMode.HUDHealthPosition.Left);
		}
		// Currency
		Point basePos = GameMode.getHUDHealthPosition(GameMode.HUDHealthPosition.Left, true).addxy(-7, 6);
		Global.sprites["hud_scrap"].drawToHUD(0, basePos.x, basePos.y - 2);
		Fonts.drawText(FontType.Blue, $"x{player.currency}", basePos.x + 9 + 3, basePos.y, Alignment.Left);
		if (player.weapons?.Count > 1) {
			gameMode.drawWeaponSwitchHUD(player);
		}
	}

	public void renderHealthAndWeapons() {
		bool is1v1OrTraining = level.is1v1() || level.levelData.isTraining();
		if (!is1v1OrTraining) {
			renderHealthAndWeapon(level.mainPlayer, GameMode.HUDHealthPosition.Left);
		} else {
			renderHealthAndWeapon(level.mainPlayer, GameMode.HUDHealthPosition.Left);
			Player? rightPlayer = Global.level.players.FirstOrDefault(
				(Player player) => player.character != null && player != level.mainPlayer
			);
			if (rightPlayer != null) {
				renderHealthAndWeapon(rightPlayer, GameMode.HUDHealthPosition.Right);
			}
		}
	}

	public void renderHealthAndWeapon(Player? player, GameMode.HUDHealthPosition position) {
		if (player == null) return;
		if (level.is1v1() && player.deaths >= gameMode.playingTo) return;

		player.lastCharacter?.renderHUD(new Point(), position);
	}

	public bool shouldDrawRadar() {
		return !level.levelData.isTraining() && !level.is1v1();
	}

	public void drawRadar() {
		
	}
}
