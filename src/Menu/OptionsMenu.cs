using System;
using System.Collections.Generic;
using SFML.Graphics;
using static SFML.Window.Keyboard;

namespace MMXOnline;
public class OptionsMenu : IMainMenu {
	public int selectedArrowPosY;
	public IMainMenu previous;

	public int startY = 40;
	public const int lineH = 9;
	public static int presetYPos = 8;
	public static int videoOffset = 10;
	public int frames;

	public List<MenuOption> menuOptions;

	public float blinkTime = 0;
	public bool isChangingName;
	public string playerName;
	public bool inGame;
	public int? charNum;
	public bool isGraphics;
	public bool isGameplay;

	public bool oldFullscreen;
	public uint oldWindowScale;
	public int oldMaxFPS;
	public bool oldDisableShaders;
	public bool oldEnablePostprocessing;
	public bool oldUseOptimizedAssets;
	private int oldParticleQuality;
	public bool oldIntegerFullscreen;
	public bool oldVsync;

	public FontType optionFontText = FontType.Blue;
	public FontType optionFontValue = FontType.Blue;
	public enum Language {
		English = 1,
		Spanish = 2,
		Portuguese = 3
	}	
	public static class Localization {
	private static readonly Dictionary<Language, Dictionary<string, string>> Translations =
		new Dictionary<Language, Dictionary<string, string>> {
			{
				Language.English, new Dictionary<string, string> {
					{ "You aint translating english", "to english" }
				}
			},
			{
				Language.Spanish, new Dictionary<string, string> {
					{ "Fullscreen:", "Pantalla completa:" },
					{ "Set to Yes to make the game render fullscreen.", "Activalo para que el juego se vea a pantalla completa." },

					{ "Windowed Resolution:", "Resolucion en ventana:" },
					{ "Change the windowed resolution of the game.", "Cambia la resolucion del juego en modo ventana." },

					{ "Show FPS:", "Mostrar FPS:" },
					{ "Show the frames per second (FPS) in the bottom right.", "Muestra los cuadros por segundo (FPS) \nen la esquina inferior derecha." },

					{ "Max FPS:", "FPS maximos:" },
					{ "Controls the max framerate the game can run.\nLower values are more choppy but use less GPU.", "Controla el limite de FPS. Valores bajos \nreducen uso de GPU pero hacen el juego menos fluido." },

					{ "Enable VSYNC:", "Activar VSYNC:" },
					{ "Set to Yes to enable vsync.\nMakes movement/scrolling smoother, but adds input lag.", "Activa VSYNC para hacer el movimiento mas suave.\nPuede causar retraso en los controles." },

					{ "Use optimized assets:", "Usar sprites optimizados:" },
					{ "Set to Yes to use optimized assets.\nThis can result in better performance.", "Activalo para mejorar el rendimiento del juego." },

					{ "Integer fullscreen:", "Pantalla completa entera:" },
					{ "Rounds down fullscreen pixels to the nearest integer.\nReduces distortion when going fullscreen.", "Usa multiplos enteros en pantalla completa.\nReduce distorsion." },

					{ "Enable small bars:", "Activar barras pequenas:" },
					{ "Makes some of the energy bars smaller.", "Reduce el tamano de algunas barras de energia." },

					{ "Preset Quality:", "Calidad predefinida:" },
					{ "Choose a pre-configured set of graphics settings.", "Elige un conjunto preconfigurado de ajustes graficos." },

					{ "-Enable shaders:", "-Activar sombreadores:" },
					{ "Enables special effects like weapon palettes.\nNot all PCs support this.", "Activa efectos especiales como las paletas de armas.\nNo todos los PCs lo soportan." },

					{ "-Enable post-processing:", "-Activar post-procesado:" },
					{ "Enables special screen distortion effects.\nNot all PCs support this.", "Activa efectos visuales de post-procesado.\nNo todos los PCs lo soportan." },

					{ "-Particle quality:", "-Calidad de particulas:" },
					{ "Set the particle effect quality.\nLower quality results in faster performance.", "Cambia la calidad de los efectos de particulas.\nA menor calidad, mayor rendimiento." },

					{ "-Enable map sprites:", "-Activar sprites del mapa:" },
					{ "Enable or disable map sprites.\nDisabling map sprites results in faster performance.", "Activa o desactiva sprites del mapa.\nDesactivarlos mejora el rendimiento." },

					{ "Music Volume:", "Volumen de Musica:" },
					{ "Adjust the game music volume.", "Ajusta el volumen de la musica del juego." },

					{ "Sound Volume:", "Volumen de Sonido:" },
					{ "Adjust the game sound volume.", "Ajusta el volumen de los efectos de sonido." },

					{ "Multiplayer name:", "Nombre Multijugador:" },
					{ "Your name that appears to others when you play online.", "Tu nombre visible para otros jugadores al jugar en linea." },

					{ "Multiplayer region:", "Region del Multijugador:" },
					{ "Region for RELAY type servers. You may need ask for the\n region on discord or is built in already on the game." , "Region para los servidores de tipo RELAY. Tal vez necesites\npedir la region en discord o ya este incluido en el juego."},

					{ "Show startup warnings:", "Mostrar avisos al iniciar:"},
					{ "On launch, check for system requirements\nand other startup warnings." , "Al iniciar, chequear los requisitos del sistema \n y otro tipo de avisos"},

					{ "Disable chat:" , "Desactivar chat:"},
					{ "Set to Yes to disable sending and receiving\nchat messages in online matches." , "Seleccionalo en Si para desactivar el envio y recibo \n de mensajes en las partidas en linea."},

					{ "Enable dev console:" , "Activar Consola:"},
					{ "If enabled, press F10 to open the dev-console in-match\nSee the game website for a list of commands." , "En activo, presiona F10 para abrir la Consola en una partida\nBusca los comandos en la web oficial del juego."},

					{ "Language setting:", "Opcion de lenguaje:"},
					{ "Change the language of the game.\nCurrent languages: English, Spanish, Portuguese." , "Cambia el idioma del juego.\nIdiomas disponibles: Ingles, Espanol, Portugues."},

					{ "Show mash progress:" , "Mostrar progreso de 'Mash':" },
					{ "When hit by moves that can be mashed out of,\nshows the mash progress above your head." , "Ciertos ataques se necesita de presionar teclas para \nsalir de ello. Muestra un progreso arriba del jugador."},

					{ "Matchmaking timeout:", "Tiempo de Busqueda:" },
					{ "How long match search will take before erroring out.\n If always erroring out in search, try increasing this." , "Cuanto tiempo durara la busqueda de partida antes de fallar.\n Si siempre falla, intenta aumentarlo." },

					{ "Weapon switch grid mode:", "Modo cuadricula de armas:" },
					{ "For weapon switch in certain or all modes.\nHold WEAPON L/R and use a directon to switch weapon.", "Para cambiar de arma en ciertos o todos los modos. Mantener \nWEAPON L/R y usar una direccion para cambiar de arma." },

					{ "Hyper Charge slot:", "Ranura de Hyper Charge:" },
					{ "Weapon slot number which Hyper Charge uses.", "Numero de ranura que usa Hyper Charge." },

					{ "Giga Attack down special:", "Giga Attack abajo especial:" },
					{ "Allows to perform Giga Crush by pressing DOWN + SPC,\nbut you lose the ability to switch to Giga Crush.", "Permite usar Giga Crush presionando ABAJO + SPC,\npero pierdes la habilidad de cambiar a Giga Crush." },

					{ "N.Strike side special:", "N.Strike lateral especial:" },
					{ "Allows to perform Nova Strike by pressing SPC,\nbut you lose the ability to switch to Nova Strike.", "Permite usar Nova Strike presionando SPC,\npero pierdes la habilidad de cambiar a Nova Strike." },

					{ "Swap air attacks:", "Cambiar ataques aereos:" },
					{ "Swaps the inputs for air slash attack,\nand Kuuenzan (or any other air special).", "Intercambia los controles del slash aereo\ny el Kuuenzan (u otro especial aereo)." },

					{ "Show giga cooldown:", "Mostrar cooldown de Giga:" },
					{ "Shows a cooldown circle for giga attacks.", "Muestra un circulo de cooldown para los Giga Attacks." },

					{ "Swap goliath shoot:", "cambiar disparo de goliath:" },
					{ "You can swap the inputs for\nGoliath buster and missiles.", "Puedes intercambiar los controles\n del buster y misiles de Goliath." },

					{ "Block mech scroll:", "Bloquear cambio a armadura:" },
					{ "Prevents ability to scroll to the Ride Armor slot.\nYou will only be able to switch to it by pressing 3.", "Evita cambiar al slot de armadura mecanica.\nSolo podras cambiar a ella presionando 3." },

					{ "Weapon order:", "Orden de armas:" },
					{ "Choose the order in which Vile's weapons are arranged.", "Elige el orden en el que se muestran las armas de Vile." },

					{ "Vile V ride control:", "Control del ride de Vile V:" },
					{ "If set to Hold, Vile V will control the Ride\nonly as long as WEAPON L/R is held.", "Si esta en Mantener, Vile V controlara el ride\nsolo mientras se mantenga WEAPON L/R." },

					{ "Lock in air cannon:", "Fijar canon en aire:" },
					{ "If No, Front Runner and Fat Boy cannons will not\nroot Vile in the air when shot.", "Si esta en No, los canones Front Runner y Fat Boy\nno fijaran a Vile en el aire al disparar." },

					{ "Aim mode:", "Modo de apuntado:" },
					{ "Change Axl's aim controls to either use\nARROW KEYS (Directional) or mouse aim (Cursor).", "Cambia el modo de apuntado de Axl a teclas \nde direccion o al cursor del raton." },

					{ "Aim sensitivity:", "Sensibilidad de apuntado:" },
					{ "Change aim sensitivity (for Cursor aim mode only.)", "Cambia la sensibilidad al apuntar (solo modo Cursor)." },

					{ "Auto aim:", "Autoapuntado:" },
					{ "Enable/disable auto-aim\n(For Directional aim mode only.)", "Activa o desactiva el autoapuntado (solo modo Direccional)." },

					{ "Analog stick aim:", "Mira con stick analogico:" },
					{ "Enables 360 degree aim if binding Axl aim controls\nto a controller analog stick.", "Permite apuntar en 360°\nsi se usa el stick analogico del mando." },

					{ "Aim key function:", "Tipo de tecla de apuntado:" },
					{ "Change the behavior of Axl's aim key.", "Cambia el comportamiento de la tecla de apuntado de Axl." },

					{ "Aim key behavior:", "Forma de tecla de apuntado:" },
					{ "Change whether Axl's aim key\nis toggle or hold based.", "Elige si la tecla de apuntado de Axl es mantener o alternar." },

					{ "Move in diagonal aim:", "Mover en apuntado diagonal:" },
					{ "Allows Axl to move when aiming diagonally,\notherwise he is locked in place when shooting.", "Permite mover a Axl al apuntar en diagonal,\nde lo contrario queda fijo al disparar." },

					{ "Aim down & crouch:", "Apuntar abajo y agacharse:" },
					{ "If mixed, Aim down and crouch will bind to\nthe same button and crouching will not aim down.", "Si es combinado, apuntar abajo y agacharse usaran \nel mismo boton, y al agacharse no apuntara hacia abajo." },

					{ "Enables Grid Mode for Axl\nwhich works the same way as X.", "Activa el modo de cuadricula para Axl,\nfunciona igual que con X." },

					{ "Show roll cooldown:", "Mostrar recarga de rodar:" },
					{ "If enabled, shows a cooldown circle above Axl head\nindicating Dodge Roll cooldown.", "Muestra un circulo de recarga sobre la cabeza de Axl\nindicando la recarga del rodar." },

					{ "Sigma slot:", "Ranura de Sigma:" },
					{ "Changes the position of the\nSigma slot in Sigma's hotbar.", "Cambia la posicion de la ranura de Sigma\nen la barra de armas de Sigma." },

					{ "Puppeteer control:", "Control de Marionetas:" },
					{ "If set to Hold, Puppeteer Sigma will control\na Maverick only as long as WEAPON L/R is held.", "Si se establece en Mantener, Sigma controlara a un Maverick\n solo mientras se mantenga pulsado WEAPON L/R." },

					{ "Maverick start mode:", "Modo inicial de Maverick:" },
					{ "Change whether Mavericks will follow Sigma,\nor hold position, after summoned.", "Cambia si los Mavericks seguiran a Sigma\no se mantendran en su lugar tras ser invocados." },

					{ "Puppeteer cancel:", "Cancelar marioneta:" },
					{ "If set to Yes, Mavericks will revert to\ntheir idle state when switched to in Puppeteer mode.", "Si se activa, los Mavericks volveran a su estado inactivo\n al cambiar de control en modo marioneta." },

					{ "Pup small energy bars:", "Barras pequenas Marioneta:" },
					{ "Makes the energy bars smaller for Puppeteer.\n Requires the small bars option to work.", "Reduce el tamano de las barras de energia para el modo\nmarioneta.Requiere activar la opcion de barras pequenas." },

					{ "Preferred character:", "Personaje preferido:" },
					{ "Choose a default character the game will\npre-select for you.", "Elige un personaje por defecto\n que el juego preseleccionara para ti." },

					{ "Disable double-tap dash:", "Desactivar doble dash:"},
					{ "Disables ability to dash by quickly\ntapping LEFT or RIGHT twice.", "Desactiva la posibilidad de hacer dash tocando\nIZQUIERDA o DERECHA dos veces rapidamente." },

					{ "Kill on loadout change:", "Morir por cambio de equipo:" },
					{ "If Yes, will instantly die on loadout change mid-match.\nIf No, on next death loadout changes will apply.", "Si activo, moriras al cambiar equipamiento si estas con vida.\nSi esta desactivado, el cambio se aplicara despues de morir." },

					{ "Kill on character change:", "Morir al cambiar personaje:" },
					{ "If Yes, will instantly die on character change.\nIf No, on next death character change will apply.", "Si activo, moriras al cambiar de personaje.\nSi estadesactivado, el cambio se aplicara despues de morir." },

					{ "Hold", "Mantener" },
					{ "Simul", "Simultaneo" },
					{ "seconds", "segundos" },
					{ "English", "Ingles" },
					{ "Spanish", "Espanol" },
					{ "Portuguese", "Portugues" },
					{ "Yes", "Si" },
					{ "No", "No" },
					{ "Directional", "Direccional" },
					{ "Cursor", "Cursor" },
					{ "Toggle", "Alternar" },
					{ "Separate", "Separado" },
					{ "Mixed", "Combinado" },
					{ "Follow", "Seguir" },
					{ "Hold Position", "Mantener posicion" },
					{ "Aim backwards/backpedal", "Apuntar hacia atras"},
					{ "Lock position" , "Mantener posicion"},
					{ "Lock aim" , "Mantener apuntado"},
					{ "1v1 Only" , "Solo en 1vs1"},
					{ "Always", "Siempre"},
					{ "Low", "Bajo"},
					{ "Medium", "Medio"},
					{ "High", "Alto"},
					{ "Custom", "Personalizado"},
					{ "Error: same weapon selected twice", "Error: Mismo tipo de arma seleccionada"},
					{ "Note: options were changed that require restart to apply.", "Nota: Ciertas opciones necesita reiniciar el juego"},
					{ "GENERAL SETTINGS","Ajustes Generales" },
					{ "GRAPHICS SETTINGS", "Ajustes de Graficos"},
					{ "X SETTINGS", "Ajustes de X"},
					{ "ZERO SETTINGS", "Ajustes de Zero"},
					{ "VILE SETTINGS", "Ajustes de Vile"},
					{ "AXL SETTINGS", "Ajustes de Axl"},
					{ "SIGMA SETTINGS", "Ajustes de Sigma"}, 
					{ "Type in a multiplayer name", "Escribe tu nombre de jugador"}
				}
			},
			{
				Language.Portuguese, new Dictionary<string, string> {
					{ "ou macaco", "macacooooo" }
				}
			},
		};

	public static string Translate(string key, int languageSetting) {
		Language lang = (Language)languageSetting;
		if (Translations.TryGetValue(lang, out var langDict)) {
			if (langDict.TryGetValue(key, out string translated)) {
				return translated;
			}
		}
		// Fallback: return key itself
		return key;
	}
}

	public OptionsMenu(IMainMenu mainMenu, bool inGame, int? charNum, int selectY) {
		previous = mainMenu;
		this.inGame = inGame;
		if (selectY == 1) {
			isGameplay = true;
		}
		if (selectY == 2) {
			isGraphics = true;
		}

		oldIntegerFullscreen = Options.main.integerFullscreen;
		oldFullscreen = Options.main.fullScreen;
		oldWindowScale = Options.main.windowScale;
		oldDisableShaders = Options.main.disableShaders;
		oldMaxFPS = Options.main.maxFPS;
		oldEnablePostprocessing = Options.main.enablePostProcessing;
		oldUseOptimizedAssets = Options.main.useOptimizedAssets;
		oldParticleQuality = Options.main.particleQuality;
		oldVsync = Options.main.vsync;

		playerName = Options.main.playerName;
		this.charNum = charNum;

		if (!isGraphics && charNum == null) {
			startY = 35;
		}
		if (!inGame) {
			optionFontText = FontType.DarkBlue;
			optionFontValue = FontType.DarkBlue;
		}

		if (isGraphics) {
			menuOptions = new List<MenuOption>() {
				// Full screen
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							if (Options.main.fullScreen) Options.main.fullScreen = false;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							if (!Options.main.fullScreen) Options.main.fullScreen = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Fullscreen:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Options.main.fullScreen ? "Yes" : "No",
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Set to Yes to make the game render fullscreen.", Options.main.languageSetting)
				),

				// Windowed resolution
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.windowScale = (uint)Helpers.clamp((int)Options.main.windowScale - 1, 1, 6);
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.windowScale = (uint)Helpers.clamp((int)Options.main.windowScale + 1, 1, 6);
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Windowed Resolution:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, getWindowedResolution(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Change the windowed resolution of the game.", Options.main.languageSetting)
				),

				// Show FPS
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.showFPS = false;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.showFPS = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Show FPS:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.showFPS),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Show the frames per second (FPS) in the bottom right.", Options.main.languageSetting)
				),

				// Lock FPS
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.maxFPS = 30;
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.maxFPS = Global.fpsCap;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Max FPS:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Options.main.maxFPS.ToString(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Controls the max framerate the game can run.\nLower values are more choppy but use less GPU.", Options.main.languageSetting)
				),

				// VSYNC
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						Helpers.menuLeftRightBool(ref Options.main.vsync);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Enable VSYNC:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.vsync),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Set to Yes to enable vsync.\nMakes movement/scrolling smoother, but adds input lag.", Options.main.languageSetting)
				),

				// Use optimized sprites
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						Helpers.menuLeftRightBool(ref Options.main.useOptimizedAssets);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Use optimized assets:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.useOptimizedAssets),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Set to Yes to use optimized assets.\nThis can result in better performance.", Options.main.languageSetting)
				),

				// Full screen integer
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.integerFullscreen);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Integer fullscreen:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.integerFullscreen),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Rounds down fullscreen pixels to the nearest integer.\nReduces distortion when going fullscreen.", Options.main.languageSetting)
				),

				// Small Bars
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.enableSmallBars = false;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.enableSmallBars = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Enable small bars:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.enableSmallBars),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Makes some of the energy bars smaller.", Options.main.languageSetting)
				),

				// Preset
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							if (Options.main.graphicsPreset > 0) {
								Options.main.graphicsPreset--;
								setPresetQuality(Options.main.graphicsPreset.Value);
							}
						}
						else if (Global.input.isPressedMenu(Control.MenuRight)) {
							if (Options.main.graphicsPreset < 3) {
								Options.main.graphicsPreset++;
								setPresetQuality(Options.main.graphicsPreset.Value);
							}
						}
					},
					(Point pos, int index) => {
						FontType color = optionFontText;
						if (!inGame) {
							color = FontType.Grey;
						}
						Fonts.drawText(
							optionFontText, Localization.Translate("Preset Quality:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontText, qualityToString(Options.main.graphicsPreset.Value),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Choose a pre-configured set of graphics settings.", Options.main.languageSetting)
				),

				// Shaders
				new MenuOption(40, startY,
					() => {
						if (inGame) return;
						if (Options.main.graphicsPreset < 3) return;
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.disableShaders = true;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.disableShaders = false;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							getVideoSettingColor(), Localization.Translate("-Enable shaders:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							getVideoSettingColor(), Helpers.boolYesNo(!Options.main.disableShaders),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Enables special effects like weapon palettes.\nNot all PCs support this.", Options.main.languageSetting)
				),

				// Post processing
				new MenuOption(40, startY,
					() => {
						if (inGame) return;
						if (Options.main.graphicsPreset < 3) return;
						Helpers.menuLeftRightBool(ref Options.main.enablePostProcessing);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							getVideoSettingColor(), Localization.Translate("-Enable post-processing:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							getVideoSettingColor(), Helpers.boolYesNo(Options.main.enablePostProcessing),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Enables special screen distortion effects.\nNot all PCs support this.", Options.main.languageSetting)
				),

				// Particle quality
				new MenuOption(40, startY,
					() => {
						if (inGame) return;
						if (Options.main.graphicsPreset < 3) return;
						Helpers.menuLeftRightInc(ref Options.main.particleQuality, 0, 2);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							getVideoSettingColor(), Localization.Translate("-Particle quality:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							getVideoSettingColor(), qualityToString(Options.main.particleQuality),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Set the particle effect quality.\nLower quality results in faster performance.", Options.main.languageSetting)
				),

				// Map sprites
				new MenuOption(40, startY,
					() => {
						if (inGame) return;
						if (Options.main.graphicsPreset < 3) return;
						Helpers.menuLeftRightBool(ref Options.main.enableMapSprites);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							getVideoSettingColor(), Localization.Translate("-Enable map sprites:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							getVideoSettingColor(), Helpers.boolYesNo(Options.main.enableMapSprites),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Enable or disable map sprites.\nDisabling map sprites results in faster performance.", Options.main.languageSetting)
				),

				// fontType
				/*new MenuOption(40, startY,
					() => {
						if (inGame) return;
						if (Options.main.graphicsPreset < 3) return;
						Helpers.menuLeftRightInc(ref Options.main.fontType, 0, 2);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							getVideoSettingColor(), "Font type:",
							pos.x + videoOffset, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							getVideoSettingColor(), fontTypeToString(Options.main.fontType),
							pos.x + videoOffset + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Set the font type. Bitmap uses PNG, Vector uses TFF.\n" +
					"Hybrid will use Bitmap in menus and Vector in-game."
				),*/
			};
		} else if (isGameplay) {
			menuOptions = new List<MenuOption>() {
				// Preferred character
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.preferredCharacter, 0, 4);
					},
					(Point pos, int index) => {
						string preferredChar = Character.charDisplayNames[Options.main.preferredCharacter];
						Fonts.drawText(
							optionFontText, Localization.Translate("Preferred character:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, preferredChar,
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Choose a default character the game will\npre-select for you.", Options.main.languageSetting)
				),

				// Disable double-tap dash
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.disableDoubleDash = false;
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.disableDoubleDash = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Disable double-tap dash:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Localization.Translate(Helpers.boolYesNo(Options.main.disableDoubleDash), Options.main.languageSetting),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Disables ability to dash by quickly\ntapping LEFT or RIGHT twice.", Options.main.languageSetting)
				),

				// Kill on loadout change
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.killOnLoadoutChange);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Kill on loadout change:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Localization.Translate(Helpers.boolYesNo(Options.main.killOnLoadoutChange), Options.main.languageSetting),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("If Yes, will instantly die on loadout change mid-match.\nIf No, on next death loadout changes will apply.", Options.main.languageSetting)
				),

				// Kill on character change
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.killOnCharChange);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Kill on character change:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Localization.Translate(Helpers.boolYesNo(Options.main.killOnCharChange), Options.main.languageSetting),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("If Yes, will instantly die on character change.\nIf No, on next death character change will apply.", Options.main.languageSetting)
				),

			};
		} else if (charNum == null) {
			if (!Global.regionPingTask.IsCompleted) {
				Global.regionPingTask.Wait();
			}

			menuOptions = new List<MenuOption>() {
				// Music volume
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.musicVolume = Helpers.clamp(Options.main.musicVolume - 0.01f, 0, 1);
							Global.music.updateVolume();
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.musicVolume = Helpers.clamp(Options.main.musicVolume + 0.01f, 0, 1);
							Global.music.updateVolume();
						}
					},
					(Point pos, int index) => {
						var musicVolume100 = (int)Math.Round(Options.main.musicVolume * 100);
						Fonts.drawText(
							optionFontText,
							Localization.Translate("Music Volume:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, musicVolume100.ToString(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Adjust the game music volume.", Options.main.languageSetting)
				),
				// Sound volume
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.soundVolume = Helpers.clamp(Options.main.soundVolume - 0.01f, 0, 1);
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.soundVolume = Helpers.clamp(Options.main.soundVolume + 0.01f, 0, 1);
						}
					},
					(Point pos, int index) => {
						var soundVolume100 = (int)Math.Round(Options.main.soundVolume * 100);
						Fonts.drawText(
							optionFontText,
							Localization.Translate("Sound Volume:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, soundVolume100.ToString(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Adjust the game sound volume.", Options.main.languageSetting)
				),
				// Multiplayer Name
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						if (!isChangingName && (
								Global.input.isPressedMenu(Control.MenuLeft) ||
								Global.input.isPressedMenu(Control.MenuRight) ||
								Global.input.isPressedMenu(Control.MenuConfirm)
							)
						) {
							isChangingName = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText,
							Localization.Translate("Multiplayer name:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, playerName,
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Your name that appears to others when you play online.", Options.main.languageSetting)
				),
				// Multiplayer region
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.regionIndex--;
							if (Options.main.regionIndex < 0) Options.main.regionIndex = 0;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.regionIndex++;
							if (Options.main.regionIndex > Global.regions.Count - 1) {
								Options.main.regionIndex = Global.regions.Count - 1;
							}
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText,
							Localization.Translate("Multiplayer region:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue,
							Options.main.getRegion().name + (
								"(" + Options.main.getRegion().getDisplayPing() + " ping)"
							),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Region for RELAY type servers. You may need ask for the\n region on discord or is built in already on the game."
					, Options.main.languageSetting)				
				),
				// Hide Menu Helper Text
				/*new MenuOption(
					30, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.showInGameMenuHUD = false;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.showInGameMenuHUD = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Show in-game menu keys:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.showInGameMenuHUD),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Show or hide additional menu help text in\nbottom right of the in-match HUD."
				),*/
				// System requirements check
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.showSysReqPrompt = false;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.showSysReqPrompt = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Show startup warnings:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.showSysReqPrompt),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					}, Localization.Translate("On launch, check for system requirements\nand other startup warnings.", Options.main.languageSetting)
				),
				// Disable Chat
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.disableChat = false;
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.disableChat = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Disable chat:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.disableChat),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Set to Yes to disable sending and receiving\nchat messages in online matches.", Options.main.languageSetting)
				),
				// Mash progress
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.showMashProgress);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Show mash progress:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.showMashProgress),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("When hit by moves that can be mashed out of,\nshows the mash progress above your head." , Options.main.languageSetting)
				),
				// Matchmaking Timeout
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.networkTimeoutSeconds = Helpers.clamp(
								Options.main.networkTimeoutSeconds - 0.1f, 1, 5
							);
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.networkTimeoutSeconds = Helpers.clamp(
								Options.main.networkTimeoutSeconds + 0.1f, 1, 5
							);
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText,
							Localization.Translate("Matchmaking timeout:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Options.main.networkTimeoutSeconds.ToString("0.0") + " seconds",
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate(
						"How long match search will take before erroring out.\n If always erroring out in search, try increasing this.",
						Options.main.languageSetting
					)
				),
				// Dev console.
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.enableDeveloperConsole);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Enable dev console:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.enableDeveloperConsole),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate(
						"If enabled, press F10 to open the dev-console in-match\nSee the game website for a list of commands.",
						Options.main.languageSetting
					)
				),
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.languageSetting, 1, 3, true, true);
					},
					(Point pos, int index) => {
						string language = Localization.Translate(
							Options.main.languageSetting == 1 ? "English" :
							Options.main.languageSetting == 2 ? "Spanish" :
							Options.main.languageSetting == 3 ? "Portugues" : "English",
							Options.main.languageSetting
						);
						Fonts.drawText(
							optionFontText,
							Localization.Translate("Language setting:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue,
							language,
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate(
						"Change the language of the game.\nCurrent languages: English, Spanish, Portuguese.",
						Options.main.languageSetting
					)
				),
			};
		} else if (charNum == 0) {
			menuOptions = new List<MenuOption>() {
				// Weapon switch grid mode
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.gridModeX, 0, 2);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Weapon switch grid mode:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, gridModeToStr(Options.main.gridModeX),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("For weapon switch in certain or all modes.\nHold WEAPON L/R and use a directon to switch weapon.", Options.main.languageSetting)
				),

				// Hyper Charge slot
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.hyperChargeSlot, 0, 2);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Hyper Charge slot:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, (Options.main.hyperChargeSlot + 1).ToString(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Weapon slot number which Hyper Charge uses.", Options.main.languageSetting)
				),

				// Giga Attack down special
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.gigaCrushSpecial);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Giga Attack down special:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.gigaCrushSpecial),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Allows to perform Giga Crush by pressing DOWN + SPC,\nbut you lose the ability to switch to Giga Crush.", Options.main.languageSetting)
				),

				// Nova Strike side special
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.novaStrikeSpecial);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("N.Strike side special:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.novaStrikeSpecial),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Allows to perform Nova Strike by pressing SPC,\nbut you lose the ability to switch to Nova Strike.", Options.main.languageSetting)
				),
				/*
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.xSpecialOrdering, 0, 5);
					},
					(Point pos, int index) => {
						string s = "H.Buster,G.Crush,N.Strike";
						if (Options.main.xSpecialOrdering == 1) s = "G.Crush,H.Buster,N.Strike";
						if (Options.main.xSpecialOrdering == 2) s = "G.Crush,N.Strike,H.Buster";
						if (Options.main.xSpecialOrdering == 3) s = "H.Buster,N.Strike,G.Crush";
						if (Options.main.xSpecialOrdering == 4) s = "N.Strike,G.Crush,H.Buster";
						if (Options.main.xSpecialOrdering == 5) s = "N.Strike,H.Buster,G.Crush";
						Fonts.drawText(
							optionFontText, "Special Slot Order:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, s,
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					}
				)
				*/
			};
		} else if (charNum == 1) {
			menuOptions = new List<MenuOption>() {
				// Swap Air Attacks
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.swapAirAttacks);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Swap air attacks:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.swapAirAttacks),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Swaps the inputs for air slash attack,\nand Kuuenzan (or any other air special).", Options.main.languageSetting)
				),

				// Show Giga Cooldown
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.showGigaAttackCooldown);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Show giga cooldown:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.showGigaAttackCooldown),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Shows a cooldown circle for giga attacks.", Options.main.languageSetting)
				),
			};
		} else if (charNum == 2) {
			menuOptions = new List<MenuOption>() {
				// Swap Goliath Inputs
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.swapGoliathInputs);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Swap goliath shoot:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.swapGoliathInputs),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("You can swap the inputs for\nGoliath buster and missiles.", Options.main.languageSetting)
				),
				/*
				// Block Ride Armor Scroll
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.blockMechSlotScroll);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Block mech scroll:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.blockMechSlotScroll),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Prevents ability to scroll to the Ride Armor slot.\nYou will only be able to switch to it by pressing 3.", Options.main.languageSetting)
				),
				/*
				// Weapon Ordering
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.weaponOrderingVile, 0, 1);
					},
					(Point pos, int index) => {
						string str = Options.main.weaponOrderingVile == 1 ?
							Localization.Translate("Vulcan,F.Runner,R.Armors", Options.main.languageSetting) :
							Localization.Translate("F.Runner,Vulcan,R.Armors", Options.main.languageSetting);
						Fonts.drawText(
							optionFontText, Localization.Translate("Weapon order:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, str,
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Choose the order in which Vile's weapons are arranged.", Options.main.languageSetting)
				),
				*/
				// MK5 Ride Control
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.mk5PuppeteerHoldOrToggle);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Vile V ride control:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Options.main.mk5PuppeteerHoldOrToggle ? Localization.Translate("Hold", Options.main.languageSetting) : Localization.Translate("Simul", Options.main.languageSetting),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("If set to Hold, Vile V will control the Ride\nonly as long as WEAPON L/R is held.", Options.main.languageSetting)
				),

				// Lock Cannon Air
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.lockInAirCannon);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Lock in air cannon:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Options.main.lockInAirCannon ? Localization.Translate("Yes", Options.main.languageSetting) : Localization.Translate("No", Options.main.languageSetting),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("If No, Front Runner and Fat Boy cannons will not\nroot Vile in the air when shot.", Options.main.languageSetting)
				),
			};
		} else if (charNum == 3) {
			menuOptions = new List<MenuOption>() {
				// Axl Use Mouse Aim
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.axlAimMode = 0;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.axlAimMode = 2;
						}
					},
					(Point pos, int index) => {
						string aimMode = "Directional";
						if (Options.main.axlAimMode == 1) aimMode = "Directional";
						else if (Options.main.axlAimMode == 2) aimMode = "Cursor";
						Fonts.drawText(
							optionFontText, Localization.Translate("Aim mode:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, aimMode,
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Change Axl's aim controls to either use\nARROW KEYS (Directional) or mouse aim (Cursor).", Options.main.languageSetting)
				),
				// Axl Mouse sensitivity
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.aimSensitivity = Helpers.clamp(Options.main.aimSensitivity - 0.01f, 0, 1);
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.aimSensitivity = Helpers.clamp(Options.main.aimSensitivity + 0.01f, 0, 1);
						}
					},
					(Point pos, int index) => {
						var str = (int)Math.Round(Options.main.aimSensitivity * 100);
						Fonts.drawText(
							optionFontText, Localization.Translate("Aim sensitivity:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, str.ToString(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Change aim sensitivity (for Cursor aim mode only.)", Options.main.languageSetting)
				),
				// Axl Lock On
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.lockOnSound = false;
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.lockOnSound = true;
						}
					},
					(Point pos, int index) => {
						//ToDo: Add an actual option for the sound.
						Fonts.drawText(
							optionFontText, Localization.Translate("Auto aim:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.lockOnSound),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Enable/disable auto-aim\n(For Directional aim mode only.)", Options.main.languageSetting)
				),
				// Axl Backwards Aim Invert
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.aimAnalog = false;
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.aimAnalog = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Analog stick aim:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.aimAnalog),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Enables 360 degree aim if binding Axl aim controls\nto a controller analog stick.", Options.main.languageSetting)
				),
				// Aim key function
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.aimKeyFunction, 0, 2);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Aim key function:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, aimKeyFunctionToStr(Options.main.aimKeyFunction),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Change the behavior of Axl's aim key.", Options.main.languageSetting)
				),
				// Aim key toggle
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.aimKeyToggle);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Aim key behavior:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Options.main.aimKeyToggle ? Localization.Translate("Toggle", Options.main.languageSetting) : Localization.Translate("Hold", Options.main.languageSetting),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Change whether Axl's aim key\nis toggle or hold based.", Options.main.languageSetting)
				),
				// Diag aim movement
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.moveInDiagAim);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Move in diagonal aim:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.moveInDiagAim),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Allows Axl to move when aiming diagonally,\notherwise he is locked in place when shooting.", Options.main.languageSetting)
				),
				// Axl Separate aim crouch
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.axlSeparateAimDownAndCrouch = false;
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.axlSeparateAimDownAndCrouch = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Aim down & crouch:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Options.main.axlSeparateAimDownAndCrouch ? Localization.Translate("Separate", Options.main.languageSetting) : Localization.Translate("Mixed", Options.main.languageSetting),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("If mixed, Aim down and crouch will bind to\nthe same button and crouching will not aim down.", Options.main.languageSetting)
				),
				// Grid mode Axl
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.gridModeAxl, 0, 2);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText,  Localization.Translate("Weapon switch grid mode:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, gridModeToStr(Options.main.gridModeAxl),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Enables Grid Mode for Axl\nwhich works the same way as X.", Options.main.languageSetting)
				),
				// Roll Cooldown HUD.
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.showRollCooldown);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Show roll cooldown:", Options.main.languageSetting),
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.showRollCooldown),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("If enabled, shows a cooldown circle above Axl head\nindicating Dodge Roll cooldown.", Options.main.languageSetting)
				),
			};
		} else if (charNum == 4) {
			menuOptions = new List<MenuOption>() {
				// Sigma Slot
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.sigmaWeaponSlot, 0, 2);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Sigma slot:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, (Options.main.sigmaWeaponSlot + 1).ToString(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Changes the position of the\nSigma slot in Sigma's hotbar.", Options.main.languageSetting)
				),
				// Puppeteer Control
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.puppeteerHoldOrToggle);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Puppeteer control:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Localization.Translate(Options.main.puppeteerHoldOrToggle ? "Hold" : "Toggle", Options.main.languageSetting),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("If set to Hold, Puppeteer Sigma will control\na Maverick only as long as WEAPON L/R is held.", Options.main.languageSetting)
				),
				// Maverick Start Mode
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.maverickStartFollow);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Maverick start mode:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Localization.Translate(Options.main.maverickStartFollow ? "Follow" : "Hold Position", Options.main.languageSetting),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Change whether Mavericks will follow Sigma,\nor hold position, after summoned.", Options.main.languageSetting)
				),
				// Puppeteer Cancel
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.puppeteerCancel);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Puppeteer cancel:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Localization.Translate(Options.main.puppeteerCancel ? "Yes" : "No", Options.main.languageSetting),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("If set to Yes, Mavericks will revert to\ntheir idle state when switched to in Puppeteer mode.", Options.main.languageSetting)
				),
				// Pup Small Energy Bars
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.smallBarsEx);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, Localization.Translate("Pup small energy bars:", Options.main.languageSetting),
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Localization.Translate(Options.main.smallBarsEx ? "Yes" : "No", Options.main.languageSetting),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					Localization.Translate("Makes the energy bars smaller for Puppeteer.\nRequires the small bars option to work.", Options.main.languageSetting)
				),
			};
		}
		for (int i = 0; i < menuOptions.Count; i++) {
			menuOptions[i].pos.y = startY + lineH * i;
		}
	}

	private string fontTypeToString(int fontType) {
		if (fontType == 0) return "Bitmap";
		else if (fontType == 1) return "Hybrid";
		else return "Vector";
	}

	public static void setPresetQuality(int graphicsPreset) {
		if (graphicsPreset >= 3) return;
		Options.main.graphicsPreset = graphicsPreset;
		Options.main.fontType = 0; //(graphicsPreset == 0 ? 0 : 1);
		Options.main.particleQuality = graphicsPreset;
		Options.main.enablePostProcessing = (graphicsPreset > 0);
		Options.main.disableShaders = (graphicsPreset == 0);
		Options.main.useOptimizedAssets = (graphicsPreset <= 1);
		Options.main.enableMapSprites = (graphicsPreset > 0);
		Options.main.saveToFile();
	}

	public static void inferPresetQuality(uint textureSize) {
		string presetMessage = (
			"Based on your detected video card texture size of {0}, " +
			"a Graphics setting of {1} has been automatically selected.\n\n" +
			"You can change this in the Settings menu."
		);
		if (textureSize <= 1024) {
			setPresetQuality(0);
			Helpers.showMessageBox(string.Format(presetMessage, textureSize, "Low"), "Graphics settings preset");
		} else if (textureSize <= 2048) {
			setPresetQuality(1);
			Helpers.showMessageBox(string.Format(presetMessage, textureSize, "Medium"), "Graphics settings preset");
		} else {
			setPresetQuality(2);
			Helpers.showMessageBox(string.Format(presetMessage, textureSize, "High"), "Graphics settings preset");
		}
	}

	private string qualityToString(int quality) {
		if (quality == 0) return Localization.Translate("Low", Options.main.languageSetting);
		else if (quality == 1) return Localization.Translate("Medium", Options.main.languageSetting);
		else if (quality == 2) return Localization.Translate("High", Options.main.languageSetting);
		else return Localization.Translate("Custom", Options.main.languageSetting);
	}

	private string aimKeyFunctionToStr(int aimKeyFunction) {
		if (aimKeyFunction == 0) return Localization.Translate("Aim backwards/backpedal", Options.main.languageSetting);
		else if (aimKeyFunction == 1) return Localization.Translate("Lock position", Options.main.languageSetting);
		else return Localization.Translate("Lock aim", Options.main.languageSetting);
	}

	string gridModeToStr(int gridMode) {
		if (gridMode == 0) return Localization.Translate("No", Options.main.languageSetting);
		if (gridMode == 1) return Localization.Translate("1v1 Only", Options.main.languageSetting);
		if (gridMode == 2) return Localization.Translate("Always", Options.main.languageSetting);
		return "Error";
	}

	public bool isRegionDisabled() {
		if (inGame) return true;
		return Global.regions == null || Global.regions.Count < 2;
	}

	public Color getColor() {
		return inGame ? Helpers.Gray : Color.White;
	}

	public FontType getVideoSettingColor() {
		if (!inGame) {
			if (Options.main.graphicsPreset < 3) {
				return FontType.Grey;
			}
			return FontType.DarkBlue; ;
		}
		if (Options.main.graphicsPreset < 3) {
			return FontType.Grey;
		}
		return FontType.Blue;
	}

	public void update() {
		if (!isGraphics && charNum == null) {
			frames++;
			if (frames > 240) {
				frames = 0;
				Global.updateRegionPings();
			}
		}

		if (isChangingName) {
			blinkTime += Global.spf;
			if (blinkTime >= 1f) {
				blinkTime = 0;
			}
			playerName = Helpers.getTypedString(playerName, Global.maxPlayerNameLength);

			if (Global.input.isPressed(Key.Enter) && !string.IsNullOrWhiteSpace(playerName.Trim())) {
				isChangingName = false;
				Options.main.playerName = Helpers.censor(playerName).Trim();
				Options.main.saveToFile();
			}

			return;
		}

		if (Global.input.isPressedMenu(Control.MenuUp)) {
			selectedArrowPosY--;
			if (selectedArrowPosY < 0) {
				selectedArrowPosY = menuOptions.Count - 1;
				if (isGraphics && Options.main.graphicsPreset < 3) {
					selectedArrowPosY = presetYPos;
				}
			}
			Global.playSound("menu");
		} else if (Global.input.isPressedMenu(Control.MenuDown)) {
			selectedArrowPosY++;
			if (selectedArrowPosY > menuOptions.Count - 1) {
				selectedArrowPosY = 0;
			}
			if (isGraphics && Options.main.graphicsPreset < 3 && selectedArrowPosY > presetYPos) {
				selectedArrowPosY = 0;
			}
			Global.playSound("menu");
		}

		menuOptions[selectedArrowPosY].update();
		helpText = menuOptions[selectedArrowPosY].configureMessage;

		if (Global.input.isPressedMenu(Control.MenuBack)) {
			if ((Options.main.xLoadout.weapon1 == Options.main.xLoadout.weapon2 && Options.main.xLoadout.weapon1 >= 0) ||
				(Options.main.xLoadout.weapon1 == Options.main.xLoadout.weapon3 && Options.main.xLoadout.weapon2 >= 0) ||
				(Options.main.xLoadout.weapon2 == Options.main.xLoadout.weapon3 && Options.main.xLoadout.weapon3 >= 0)) {
				Menu.change(new ErrorMenu(new string[] {
					Localization.Translate("Error: same weapon selected twice", Options.main.languageSetting)
				}, this));
				return;
			}

			if (Options.main.axlLoadout.weapon2 == Options.main.axlLoadout.weapon3) {
				Menu.change(new ErrorMenu(new string[] {
					Localization.Translate("Error: same weapon selected twice", Options.main.languageSetting)
				}, this));
				return;
			}

			Options.main.saveToFile();
			if (oldWindowScale != Options.main.windowScale) {
				Global.changeWindowSize(Options.main.windowScale);
			}

			if (oldFullscreen != Options.main.fullScreen ||
				//oldWindowScale != Options.main.windowScale ||
				oldMaxFPS != Options.main.maxFPS ||
				oldDisableShaders != Options.main.disableShaders ||
				oldEnablePostprocessing != Options.main.enablePostProcessing ||
				oldUseOptimizedAssets != Options.main.useOptimizedAssets ||
				oldParticleQuality != Options.main.particleQuality ||
				//oldIntegerFullscreen != Options.main.integerFullscreen ||
				oldVsync != Options.main.vsync
			) {
				Menu.change(new ErrorMenu(new string[] {
					Localization.Translate("Note: options were changed that require restart to apply.", Options.main.languageSetting)				
				}, previous));
			} else {
				Menu.change(previous);
			}
		}
	}

	public string getWindowedResolution() {
		return $"{Global.screenW * Options.main.windowScale}x{Global.screenH * Options.main.windowScale}";
	}

	public string helpText = "";
	public void render() {
		float cursorPos = 24;
		if (isGraphics && selectedArrowPosY > presetYPos && selectedArrowPosY < presetYPos + 6) {
			cursorPos = 24;
		}
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
			DrawWrappers.DrawTextureHUD(
				Global.textures["cursor"], cursorPos, 35 + (selectedArrowPosY * 10) - 2
			);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
			Global.sprites["cursor"].drawToHUD(0, cursorPos, 35 + (selectedArrowPosY * 10) + 3);
		}
		string subtitle = Localization.Translate("GENERAL SETTINGS", Options.main.languageSetting);
		if (isGraphics) subtitle = Localization.Translate("GRAPHICS SETTINGS", Options.main.languageSetting);
		else if (charNum == 0) subtitle = Localization.Translate("X SETTINGS", Options.main.languageSetting);
		else if (charNum == 1) subtitle = Localization.Translate("ZERO SETTINGS", Options.main.languageSetting);
		else if (charNum == 2) subtitle = Localization.Translate("VILE SETTINGS", Options.main.languageSetting);
		else if (charNum == 3) subtitle = Localization.Translate("AXL SETTINGS", Options.main.languageSetting);
		else if (charNum == 4) subtitle = Localization.Translate("SIGMA SETTINGS", Options.main.languageSetting);
		Fonts.drawText(FontType.Yellow, subtitle, Global.halfScreenW, 20, Alignment.Center);
		Fonts.drawTextEX(
			FontType.Grey, "[MLEFT]/[MRIGHT]: Change, [BACK]: Save and Back",
			Global.halfScreenW, 198, Alignment.Center
		);

		for (int i = 0; i < menuOptions.Count; i++) {
			menuOptions[i].render(new Point(32, 35 + (i * 10)), i);
		}

		float rectY = 170;
		if (!string.IsNullOrEmpty(helpText)) {
			DrawWrappers.DrawRect(
				8, rectY, Global.screenW - 8, rectY + 24, true,
				new Color(0, 0, 0, 224), 1, ZIndex.HUD, false, outlineColor: Color.White
			);
			Fonts.drawText(
				FontType.Green, helpText, Global.halfScreenW, rectY + 4,
				alignment: Alignment.Center
			);
		}
		if (isChangingName) {
			float top = Global.screenH * 0.4f;

			/*DrawWrappers.DrawRect(
				5, top - 20, Global.screenW - 5, top + 60,
				true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false
			);*/
			DrawWrappers.DrawRect(
				5, 5, Global.screenW - 5, Global.screenH - 5,
				true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false
			);
			Fonts.drawText(
				FontType.Orange, Localization.Translate("Type in a multiplayer name", Options.main.languageSetting),
				Global.screenW / 2, top, alignment: Alignment.Center
			);
			int xPos = MathInt.Round(Global.screenW * 0.33f);
			Fonts.drawText(FontType.Green, playerName, xPos, 20 + top);
			if (blinkTime >= 0.5f) {
				int width = Fonts.measureText(FontType.Green, playerName);
				Fonts.drawText(FontType.Grey, "<", xPos + width + 3, 20 + top);
			}

			Fonts.drawText(
				FontType.Grey, "Press Enter to continue",
				Global.screenW / 2, 40 + top, alignment: Alignment.Center
			);
		}
	}
}
