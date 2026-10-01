using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Globals;
using AethonMod.Content.Weapons;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// ShardHUDSystem — LA BARRA DORADA: el nivel y la XP del Grimorio al
    /// lado del hotbar, visible siempre que el libro esté en la BARRA
    /// RÁPIDA (los slots 0–9, el inventario visible de las teclas de
    /// número — sostenerlo también cuenta). Con el inventario abierto no
    /// se dibuja: el tooltip del libro ya cuenta la XP entera.
    ///
    /// GEOMETRÍA (leída del IL de GUIHotbarDrawInner, el dibujo real del
    /// hotbar de vanilla): primer slot en (20, 20), slots de 56px con
    /// stride de +4 — nueve slots a escala 0.75 (42px) + el seleccionado
    /// a escala 1 (56px) ≈ 470px de fila, fondo ≈ y=76. La barra vive
    /// justo debajo: x=20, y=80.
    ///
    /// v6.50.48 — LA BARRA ADAPTATIVA (la letra del usuario: «la barra de
    /// experiencia del libro esta justo debajo del inventario, pero esta
    /// sobre la parte donde aparecen los buff y debuff del jugador, la
    /// barra de ex del grimorio deberia estar un poco mas abajo y deberia
    /// adaptarse al espacio en caso de que se coloque muchos buff o
    /// debuff»). LA GEOMETRÍA DE LOS BUFFS (leída del IL de
    /// DrawInterface_Resources_Buffs): fila de 11 iconos, x = 32 + slot*38,
    /// PRIMERA FILA y=76, +50 por fila extra. La barra XP ahora BAILA
    /// DEBAJO DE LA ÚLTIMA FILA de buffs: filas = ceil(activos/11),
    /// y = 76 + filas*50 + 4 — sin buffs baja a 80 (donde siempre vivió),
    /// con 12 buffs (2 filas) se muda a 180: NUNCA más pisa a los iconos.
    ///
    /// EL PULSO: al cobrar XP (MarcarGanancia, llamado desde
    /// GlobalNPCXP.OnKill del jugador local) la barra LATE 90 ticks —
    /// brillo senoidal sobre el relleno dorado — y un "+XP" flotante se
    /// eleva y se funde. DETERMINISTA: cero Main.rand, todo es función
    /// de la edad del estado; la fase del latido es el tiempo vivo del
    /// pulso, no azar.
    /// Chip "HM ×2" mientras Main.hardMode: el indicador de que las
    /// CRIATURAS pagan doble (los jefes son XP fija — el tooltip del
    /// libro lo aclara).
    /// </summary>
    public class ShardHUDSystem : ModSystem
    {
        // === ESTADO DEL HUD (cliente) ===
        private static int _pulso = 0;           // ticks restantes de latido
        private static int _edadGanancia = -1;   // edad del "+XP" flotante (-1 = inactivo)
        private static int _ultimaGanancia = 0;

        public const int AnchoBarra = 470;       // la fila del hotbar entera
        public const int AltoBarra = 10;
        public const float XBarra = 20f;         // el borde izquierdo del hotbar
        public const float YBarra = 80f;         // justo bajo la fila de slots
        public const float YTexto = 94f;         // la línea de texto bajo la barra
        public const int TicksPulso = 90;
        public const int TicksGanancia = 70;

        /// <summary>
        /// La XP cobrada por una kill: enciende el pulso y suelta el
        /// "+XP" flotante. La llama GlobalNPCXP.OnKill (en SP, el jugador
        /// local; en MP llega por EcoRed.MsgLatidoXp al portador).
        /// </summary>
        public static void MarcarGanancia(int xp)
        {
            _pulso = TicksPulso;
            _edadGanancia = 0;
            _ultimaGanancia = xp;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.gameMenu) return;
            if (_pulso > 0) _pulso--;
            if (_edadGanancia >= 0)
            {
                _edadGanancia++;
                if (_edadGanancia > TicksGanancia) _edadGanancia = -1;
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // Justo DESPUÉS del hotbar: la barra es el vecino de abajo.
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Hotbar");
            if (idx != -1)
            {
                layers.Insert(idx + 1, new LegacyGameInterfaceLayer(
                    "AethonMod: Barra XP Grimorio",
                    () => { DibujarBarra(); return true; },
                    InterfaceScaleType.UI));
            }
        }

        private static void DibujarBarra()
        {
            try
            {
                // Con el inventario abierto la fila 2 del inventario pisa
                // esta zona (DrawInventory: fila j=1 empieza en y≈68) y el
                // tooltip del libro ya muestra la XP: no se dibuja.
                if (Main.playerInventory || Main.gameMenu) return;
                Player p = Main.LocalPlayer;
                if (p == null || !p.active) return;

                // El libro debe estar en la BARRA RÁPIDA (0–9).
                ShardLevelItem sl = null;
                for (int i = 0; i < 10; i++)
                {
                    Item inv = p.inventory[i];
                    if (inv == null || inv.type != ModContent.ItemType<GrimoireEternal>())
                        continue;
                    sl = inv.GetGlobalItem<ShardLevelItem>();
                    break; // la primera copia es la que manda
                }
                if (sl == null) return;

                int xpNecesaria = sl.XPForNextLevel();
                float frac = xpNecesaria > 0 ? (float)sl.XP / xpNecesaria : 0f;
                frac = MathHelper.Clamp(frac, 0f, 1f);

                SpriteBatch sb = Main.spriteBatch;
                Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
                var font = Terraria.GameContent.FontAssets.MouseText.Value;

                int ancho = (int)MathHelper.Min(AnchoBarra, Main.screenWidth - 60);
                float x = XBarra;

                // === v6.50.48 — EL BAILE DE LA BARRA: debajo de la última
                //     fila de buffs/debuffs del jugador (y adaptándose si
                //     se colocan muchos). Filas de 11 (el vanilla de
                //     DrawInterface_Resources_Buffs), 50px por fila.
                int buffs = 0;
                for (int b = 0; b < p.buffType.Length; b++)
                    if (p.buffType[b] > 0) buffs++;
                int filas = buffs > 0 ? (buffs + 10) / 11 : 0;   // 0..11 -> 0; 12..22 -> 1...
                if (buffs > 11) filas = (buffs + 10) / 11;        // 12+ abren la segunda fila
                float y = filas > 0 ? 76f + filas * 50f + 4f : YBarra;
                float yTexto = y + 14f;

                // === v6.47 — LA PALIDEZ DEL HAMBRE ===
                // La Voz del Hambre: cada momento sin comer apaga el dorado
                // (gris pálido creciente); al matar, el color VUELVE (la
                // kill alimenta el libro — RegistrarKill pone la hambre a
                // cero y el próximo frame ya reluce).
                var spHambre = p.GetModPlayer<Players.ShardPlayer>();
                int hambre = spHambre != null ? spHambre.MomentosHambre : 0;
                float palidez = MathHelper.Clamp(hambre / 10f, 0f, 1f) * 0.72f;

                // === EL LATIDO: brillo senoidal mientras vive el pulso ===
                float fase = (TicksPulso - _pulso) * 0.22f;
                float latido = _pulso > 0
                    ? 0.5f + 0.5f * (float)System.Math.Sin(fase)
                    : 0f;

                // === FONDO y MARCO ===
                sb.Draw(pixel, new Rectangle((int)(x - 3), (int)(y - 3), ancho + 6, AltoBarra + 6),
                    new Color(12, 10, 20) * 0.62f);
                Color marco = new Color(120 + (int)(125 * latido), 96 + (int)(60 * latido), 40, 230);
                sb.Draw(pixel, new Rectangle((int)(x - 2), (int)(y - 2), ancho + 4, 1), marco);
                sb.Draw(pixel, new Rectangle((int)(x - 2), (int)(y + AltoBarra + 1), ancho + 4, 1), marco);
                sb.Draw(pixel, new Rectangle((int)(x - 2), (int)(y - 2), 1, AltoBarra + 4), marco);
                sb.Draw(pixel, new Rectangle((int)(x + ancho + 1), (int)(y - 2), 1, AltoBarra + 4), marco);

                // === RELLENO DORADO (dos tonos: cuerpo + brillo superior) ===
                int lleno = (int)(ancho * frac);
                if (lleno > 0)
                {
                    Color cuerpo = new Color(245, 196, 81);
                    Color brillo = new Color(255, 235, 150);
                    if (palidez > 0f)
                    {
                        // EL DORADO PALIDECE: hacia el gris ceniza del hambre
                        cuerpo = Color.Lerp(cuerpo, new Color(139, 136, 128), palidez);
                        brillo = Color.Lerp(brillo, new Color(168, 165, 158), palidez);
                    }
                    if (latido > 0f)
                    {
                        // el pulso empuja el dorado hacia el blanco-oro
                        cuerpo = Color.Lerp(cuerpo, new Color(255, 244, 190), latido * 0.6f);
                        brillo = Color.Lerp(brillo, Color.White, latido * 0.5f);
                    }
                    sb.Draw(pixel, new Rectangle((int)x, (int)y, lleno, AltoBarra), cuerpo);
                    sb.Draw(pixel, new Rectangle((int)x, (int)y, lleno, AltoBarra / 2), brillo * 0.85f);
                }

                // === TEXTO: nivel a la izquierda, XP a la derecha ===
                // v6.49 — LOCALIZADO + CACHEADO (hallazgo AUD-C: 5 textos
                // hardcodeados fuera del hjson y 3-4 strings nuevos por
                // frame en pleno HUD): las plantillas viven en el hjson
                // ({0}/{1}) y el string + la MEDIDA solo se reconstruyen
                // cuando cambia lo que dicen (nivel, XP, hambre, HM).
                string txtNivel = hambre >= 8
                    ? CacheTexto(ref _cacheNivelHambre, "Mods.AethonMod.HUD.NivelHambre", sl.Level, font)
                    : CacheTexto(ref _cacheNivel, "Mods.AethonMod.HUD.Nivel", sl.Level, font);
                Color tinteNivel = new Color(245, 196, 81) * 0.95f;
                if (hambre > 0)
                {
                    // el nombre también palidece… y con 8+ hambres, susurra
                    tinteNivel = Color.Lerp(tinteNivel, new Color(150, 148, 142) * 0.95f, palidez);
                }
                sb.DrawString(font, txtNivel, new Vector2(x + 2f, yTexto),
                    tinteNivel, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

                string txtXP = Main.hardMode
                    ? CacheTexto(ref _cacheXpHm, "Mods.AethonMod.HUD.XPHM", sl.XP, xpNecesaria, font)
                    : CacheTexto(ref _cacheXp, "Mods.AethonMod.HUD.XP", sl.XP, xpNecesaria, font);
                Vector2 medXP = MedidaDe(_cacheXpActiva); // la del formato ACTIVO
                sb.DrawString(font, txtXP, new Vector2(x + ancho - medXP.X, yTexto),
                    (Main.hardMode ? new Color(255, 160, 90) : new Color(220, 220, 220)) * 0.9f,
                    0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

                // === EL "+XP" FLOTANTE: nace sobre la barra, sube y se funde ===
                if (_edadGanancia >= 0 && _edadGanancia <= TicksGanancia)
                {
                    float alfaG = 1f - (float)_edadGanancia / TicksGanancia;
                    // v6.49 — localizado y cacheado por cantidad (solo
                    // cambia al cobrar una ganancia nueva).
                    string txtGan = CacheTexto(ref _cacheGanancia, "Mods.AethonMod.HUD.Ganancia",
                        _ultimaGanancia, font);
                    Vector2 posG = new Vector2(
                        x + ancho - 70f,
                        y - 6f - _edadGanancia * 0.45f);
                    sb.DrawString(font, txtGan, posG + new Vector2(1.5f, 1.5f),
                        Color.Black * (alfaG * 0.7f), 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                    sb.DrawString(font, txtGan, posG,
                        new Color(255, 222, 120) * alfaG, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                }
            }
            catch { }
        }

        // ================================================================
        //  v6.49 — EL CACHE DE TEXTOS DEL HUD (hallazgo AUD-C: el HUD
        //  interpolaba 3-4 strings y median MeasureString CADA frame).
        //  El texto y su MEDIDA solo se reconstruyen cuando cambia el
        //  argumento que alimenta la plantilla.
        // ================================================================

        private struct TextoCache
        {
            public long Argumento1, Argumento2;
            public string Texto;
            public Vector2 Medida; // medida ya ESCALADA (0.85)
        }

        private static TextoCache _cacheNivel, _cacheNivelHambre, _cacheXp, _cacheXpHm, _cacheGanancia;
        private static TextoCache _cacheXpActiva; // el formato que se está dibujando

        private static string CacheTexto(ref TextoCache cache, string clave, long arg, DynamicSpriteFont font)
            => CacheTexto(ref cache, clave, arg, 0, font);

        private static string CacheTexto(ref TextoCache cache, string clave, long arg1, long arg2,
            DynamicSpriteFont font)
        {
            if (cache.Texto == null || cache.Argumento1 != arg1 || cache.Argumento2 != arg2)
            {
                cache.Argumento1 = arg1;
                cache.Argumento2 = arg2;
                cache.Texto = Terraria.Localization.Language.GetTextValue(clave, arg1, arg2);
                cache.Medida = font.MeasureString(cache.Texto) * 0.85f;
            }
            if (clave.EndsWith("XP") || clave.EndsWith("XPHM")) _cacheXpActiva = cache;
            return cache.Texto;
        }

        private static Vector2 MedidaDe(TextoCache cache)
            => cache.Texto != null ? cache.Medida : Vector2.Zero;

        public override void OnWorldUnload() { _pulso = 0; _edadGanancia = -1; LimpiarCaches(); }
        public override void Unload() { _pulso = 0; _edadGanancia = -1; LimpiarCaches(); }

        private static void LimpiarCaches()
        {
            _cacheNivel = default; _cacheNivelHambre = default;
            _cacheXp = default; _cacheXpHm = default;
            _cacheGanancia = default; _cacheXpActiva = default;
        }
    }
}
