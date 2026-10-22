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

// Main HUD render functions.
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
			renderHealthAndWeapon(player, HUDHealthPosition.Left);
		}
		// Currency
		Point basePos = GameMode.getHUDHealthPosition(HUDHealthPosition.Left, true).addxy(-7, 6);
		Global.sprites["hud_scrap"].drawToHUD(0, basePos.x, basePos.y - 2);
		Fonts.drawText(FontType.Blue, $"x{player.currency}", basePos.x + 9 + 3, basePos.y, Alignment.Left);
		if (player.weapons?.Count > 1) {
			gameMode.drawWeaponSwitchHUD(player);
		}
	}

	public void renderHealthAndWeapons() {
		bool is1v1OrTraining = level.is1v1() || level.levelData.isTraining();
		if (!is1v1OrTraining) {
			renderHealthAndWeapon(level.mainPlayer, HUDHealthPosition.Left);
		} else {
			renderHealthAndWeapon(level.mainPlayer, HUDHealthPosition.Left);
			Player? rightPlayer = Global.level.players.FirstOrDefault(
				(Player player) => player.character != null && player != level.mainPlayer
			);
			if (rightPlayer != null) {
				renderHealthAndWeapon(rightPlayer, HUDHealthPosition.Right);
			}
		}
	}

	public void renderHealthAndWeapon(Player? player, HUDHealthPosition position) {
		if (player == null || level.is1v1() && player.deaths >= gameMode.playingTo) { return; }

		player.lastCharacter?.renderHUD(new Point(), position);
	}

	public bool shouldDrawRadar() {
		return !level.levelData.isTraining() && !level.is1v1();
	}

	public void drawRadar() {

	}
}

// Interface for HUD stuff.
public interface IHudDrawable {
	void renderHUD(Point offset, HUDHealthPosition position);
	void renderLifebar(Point offset, HUDHealthPosition position);
	void renderAmmo(Point offset, HUDHealthPosition position);
	Point renderName(Point offset);
	Point renderMiniHUD(Point offset);
}

// Hud drawing helpers functions.
public class HudDraw {
	// Hp and ammo render.
	public static void renderLifebar(
		Point pos, HUDHealthPosition position, bool alive,
		decimal health, decimal maxHealth,
		decimal shieldHealth, decimal damageSavings,
		string healthBaseSprite, int baseSpriteIndex,
		string healthTopSprite, int baseTopIndex,
		bool isRcx = false
	) {
		damageSavings = MathInt.Floor(damageSavings);
		if (!alive) {
			damageSavings = 0;
		}
		Point hudHealthPosition = GameMode.getHUDHealthPosition(position, true);
		float baseX = hudHealthPosition.x + pos.x;
		float baseY = hudHealthPosition.y + pos.y;

		Global.sprites[healthBaseSprite].drawToHUD(baseSpriteIndex, baseX, baseY);
		baseY -= 16;
		decimal modifier = (decimal)Player.getHealthModifier();
		if (modifier < 1) { modifier = 1; }
		decimal maxHP = Math.Ceiling(maxHealth / modifier);
		decimal curHP = Math.Floor(health / modifier);
		decimal ceilCurHP = Math.Ceiling(health / modifier);
		decimal floatCurHP = health / modifier;
		float fhpAlpha = (float)(floatCurHP - curHP);
		decimal greyHp = Math.Ceiling(0 / modifier);
		decimal savings = curHP + (damageSavings / modifier);
		decimal ceilSavings = curHP + Math.Ceiling(Math.Floor(damageSavings) / modifier);

		// Default bar index.
		int barIndex = 0;
		int rBarIndex = 1;
		int sBarIndex = 2;
		// Custom UPX logic.
		// TODO: Move this to a function.
		if (isRcx) {
			decimal hpPercent = Math.Floor(health / maxHealth * 100);
			barIndex = hpPercent switch {
				>= 75 => 1,
				>= 50 => 3,
				>= 25 => 4,
				_ => 5,
			};
			sBarIndex = 0;
			rBarIndex = 2;
		}

		for (var i = 0; i < Math.Ceiling(maxHP); i++) {
			// Draw HP
			if (i < curHP) {
				Global.sprites["hud_health_full"].drawToHUD(barIndex, baseX, baseY);
			} else if (i < savings) {
				Global.sprites["hud_health_full"].drawToHUD(rBarIndex, baseX, baseY);
			} else {
				if (i < greyHp) {
					Global.sprites["hud_health_full"].drawToHUD(GameMode.grayAmmoIndex, baseX, baseY);
				} else {
					Global.sprites["hud_health_empty"].drawToHUD(0, baseX, baseY);
				}
				if (i < ceilCurHP) {
					Global.sprites["hud_health_full"].drawToHUD(0, baseX, baseY, fhpAlpha);
				}
				else if (i < ceilSavings) {
					float alpha = (float)(savings % 1);
					Global.sprites["hud_health_full"].drawToHUD(rBarIndex, baseX, baseY, alpha);
				}
			}
			baseY -= 2;
		}
		Global.sprites[healthTopSprite].drawToHUD(baseTopIndex, baseX, baseY);
	}

	public static void renderAmmo(Point pos, HUDHealthPosition position, Weapon? weapon) {
		if (weapon == null) {
			return;
		}
		if (!weapon.drawAmmo) {
			return;
		}

		Point hudHealthPosition = GameMode.getHUDHealthPosition(position, false);
		float baseX = hudHealthPosition.x + pos.x;
		float baseY = hudHealthPosition.y + pos.y;

		string baseBarName = "hud_weapon_base";
		string fullBarName = "hud_weapon_full";

		float ammo = weapon.ammo;
		float maxAmmo = weapon.maxAmmo;

		GameMode.renderAmmo(
			baseX, baseY, weapon.weaponBarBaseIndex, weapon.weaponBarIndex,
			ammo, 0, maxAmmo, false, fullBarName, baseBarName,
			ammoDisplayScale: weapon.ammoDisplayScale
		);
	}

	// General buff bar drawing functions.
	public static void renderBuffs(Point pos, int drawDir, List<Buff> buffList) {
		foreach (Buff buff in buffList) {
			drawBuff(pos, buff);
			pos.x += 18 * drawDir;
		}
	}

	public static void drawBuff(Point drawPos, Buff buff) {
		Global.sprites[buff.iconName].drawToHUD(buff.iconIndex, drawPos.x, drawPos.y);
		float maxTime = buff.stackData.time ?? buff.maxTime;
		float cooldown = Math.Clamp(buff.time / maxTime, 0, 1);

		if (buff.stackData.time != null) {
			float stack = Math.Clamp(buff.stackProgress / buff.stackData.time.Value, 0, 1);
			int stackP = MathInt.Round((1 - stack) * 14);
			Point drawPostA = drawPos.addxy(-7, -7);
			Point drawPostB = drawPos.addxy(+6, -7);
			Point drawPostC = drawPos.addxy(+7, +6);
			if (stackP >= 14) {
				DrawWrappers.DrawRect(
					drawPostA.x, drawPostC.y, drawPostB.x, drawPostC.y + 1,
					true, Color.Yellow, 0,
					ZIndex.HUD, false
				);
			}
			DrawWrappers.DrawRect(
				drawPostA.x, drawPostA.y, drawPostA.x + 1, drawPostA.y + stackP,
				true, Color.Yellow, 0,
				ZIndex.HUD, false
			);
			DrawWrappers.DrawRect(
				drawPostB.x, drawPostA.y, drawPostB.x + 1, drawPostB.y + stackP,
				true, Color.Yellow, 0,
				ZIndex.HUD, false
			);
			if (stackP > 0) {
				DrawWrappers.DrawRect(
					drawPostA.x, drawPostA.y, drawPostB.x, drawPostA.y + 1,
					true, Color.Yellow, 0,
					ZIndex.HUD, false
				);
			}
		}
		if (buff.stackData.max != buff.stackData.min && buff.stacks > buff.stackData.min) {
			Fonts.drawText(
				FontType.Red, buff.stacks.ToString(),
				drawPos.x + 8, drawPos.y, Alignment.Right
			);
		}
		if (buff.isBuff) {
			GameMode.drawWeaponSlotCooldownR(drawPos.x, drawPos.y, cooldown);
		} else {
			GameMode.drawWeaponSlotCooldown(drawPos.x, drawPos.y, cooldown);
		}
	}

	// More generic variant of the draws.
	public static void drawBuff(Point pos, float cooldown, string sprite, int index) {
		Global.sprites[sprite].drawToHUD(index, pos.x, pos.y);
		GameMode.drawWeaponSlotCooldownR(pos.x, pos.y, cooldown);
	}
	public static void drawDebuff(Point pos, float cooldown, string sprite, int index) {
		Global.sprites[sprite].drawToHUD(index, pos.x, pos.y);
		GameMode.drawWeaponSlotCooldown(pos.x, pos.y, cooldown);
	}
	public static void drawBuffAlt(Point pos, float cooldown, string sprite, int index) {
		Global.sprites[sprite].drawToHUD(index, pos.x, pos.y);
		GameMode.drawWeaponSlotAmmo(pos.x, pos.y, cooldown);
	}

	// General mini-lifebar helper functions.
	public static Point renderMiniHUD(
		Point pos, long zIndex,
		float health, float maxHealth,
		float ammo, float maxAmmo,
		int alliance = -1, float? maxAmmoLength = null,
		float redHp = 0, float shieldHp = 0,
		Color? color = null
	) {
		// Values.
		float scale = getMiniHudScale();
		float maxBarAmmo = maxAmmoLength ?? maxAmmo;
		float hp = health / scale;
		float rHp = redHp / scale;
		float sHp = shieldHp / scale;
		float mHp = maxHealth / scale;
		if (color == null && alliance >= 0 &&
			Global.level.gameMode.isTeamMode &&
			alliance < Global.level.teamNum &&
			alliance < Global.level.gameMode.teamColors.Length
		) {
			color = Global.level.gameMode.teamColors[alliance];
		}
		color ??= Color.White;

		// Weapon.
		if (maxBarAmmo > 0) {
			// Border.
			DrawWrappers.DrawRect(
				pos.x + 1, pos.y - 4, pos.x + maxBarAmmo * 2, pos.y - 1,
				true, new Color(49, 49, 49), 1, zIndex, outlineColor: new Color(0, 0, 0)
			);
			// Weapon.
			if (maxAmmo != 0) {
				renderMiniHudBorder(pos, color, maxBarAmmo, zIndex);
				pos = renderMiniBar(pos, 5, ammo / scale, maxAmmo / scale, zIndex);
			} else {
				renderMiniHudBorder(pos, color, maxBarAmmo, zIndex);
				pos = renderMiniBar(pos, 1, maxBarAmmo, maxBarAmmo, zIndex);
			}
		}
		// Health.
		renderMiniHudBorder(pos, color, mHp, zIndex);
		renderMiniBar(pos, 2, hp, mHp, zIndex); // Main
		renderMiniBar(pos, 4, sHp, sHp, zIndex); // Shield
		pos = renderMiniBar(pos, 3, rHp, rHp, zIndex); // Red
		// Return offset.
		return new Point(pos.x, pos.y);
	}

	public static Point renderMiniBar(Point pos, int color, float ammo, float maxAmmo, long zIndex) {
		float cAmmo = MathF.Ceiling(ammo);
		float fAmmo = MathF.Floor(ammo);
		float aAlpha = ammo - fAmmo;
		float mAmmo = MathF.Floor(maxAmmo);
		if (ammo == maxAmmo) {
			fAmmo = mAmmo;
		}
		Point lpos = pos.addxy(0, -5);
		for (int i = 0; i < mAmmo; i++) {
			int id = i < fAmmo ? color : 0;
			Global.sprites["hud_health_mini"].draw(
				id, lpos.x + i, lpos.y, 1, 1, null, 1, 1, 1, zIndex
			);
			if (i >= fAmmo && i < cAmmo) {
				Global.sprites["hud_health_mini"].draw(
					color, lpos.x + i, lpos.y, 1, 1, null, aAlpha, 1, 1, zIndex
				);
			}
			lpos.x += 1;
		}

		return pos.addxy(0, -4);
	}

	public static void renderMiniHudBorder(Point pos, Color? color, float ammo, long zIndex) {
		if (color == null) {
			return;
		}
		DrawWrappers.DrawRect(
			pos.x, pos.y - 5, pos.x + 1 + ammo * 2, pos.y,
			false, color.Value, 1, zIndex - 1
		);
		DrawWrappers.DrawRect(
			pos.x - 1, pos.y - 6, pos.x + 2 + ammo * 2, pos.y + 1,
			false, Color.Black, 1, zIndex - 2
		);
	}

	public static float getMiniHudScale() {
		float scale = 1;
		if (Global.customSettings != null) {
			float mod = Global.customSettings.healthModifier;
			if (mod > 1) {
				return scale * (Global.customSettings.healthModifier);
			}
		}
		return scale;
	}

	public static Point renderName(Point pos, string name, long zIndex, int alliance = -1) {
		Color nameColor = Color.White;

		if (alliance >= 0 && Global.level.gameMode.isTeamMode &&
			alliance < Global.level.teamNum &&
			alliance < Global.level.gameMode.teamColors.Length
		) {
			nameColor = Global.level.gameMode.teamColors[alliance];
		}
		Point textPos = pos.addxy(0, -7);

		Fonts.drawText(
			FontType.Grey, name, textPos.x, textPos.y,
			Alignment.Left, true, depth: zIndex, color: nameColor
		);
		return pos.addxy(0, -12);
	}
}

public enum HUDHealthPosition {
	Left,
	Right,
}
