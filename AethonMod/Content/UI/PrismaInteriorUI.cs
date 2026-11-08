using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace AethonMod.Content.UI
{
    // ======================================================================
    //  PrismaInteriorUI — v6.115 — LA VENTANA DEL PRISMA.
    //
    //  LA LETRA DEL USUARIO: «este tiene la capacidad de enviar de
    //  regreso al jugador y ademas de invocar a aethon usando una
    //  ventana con botones para seleccionar lo que se quiere hacer».
    //
    //  LA VENTANA: el patrón de la casa (El Cielo del Despertar —
    //  LegacyGameInterfaceLayer antes de "Vanilla: Mouse Text", estado
    //  estático de cliente, la ley de la distancia a 280 px) vestida
    //  con EL CIELO ESTRELLADO del altar y DOS BOTONES grandes:
    //   · VOLVER AL MUNDO REAL (el oro del veredicto) — la puerta gira
    //     y el libro devuelve al portador junto al altar donde fue
    //     devorado.
    //   · INVOCAR A AETHON (el violeta del vacío) — LA LLAMADA nace del
    //     prisma: el altar de la consagración se materializa delante y
    //     EL GRIMORIO DEL ETERNO flota justo encima (la ceremonia
    //     completa); los DOS fragmentos del recuerdo llegan a por el
    //     portador. Si la familia ya camina por el interior, el botón
    //     lo dice y no se pulsa.
    // ======================================================================
    public class PrismaInteriorUI : ModSystem
    {
        // === EL ESTADO (cliente puro — el patrón El Cielo) ===
        private static bool _abierta;
        private static int _tileX, _tileY;
        private static bool _ultimoClicIzq;

        private static readonly Color TintaFondo = new Color(16, 8, 28);
        private static readonly Color TintaVioleta = new Color(185, 110, 234);
        private static readonly Color TintaOro = new Color(255, 214, 130);
        private static readonly Color TintaApagado = new Color(150, 130, 170);

        /// <summary>¿La ventana está abierta?</summary>
        public static bool Abierta => _abierta;

        /// <summary>EL PRISMA SE ABRE (o se cierra).</summary>
        public static void Alternar(int i, int j)
        {
            _abierta = !_abierta;
            _tileX = i;
            _tileY = j;
        }

        public override void OnWorldUnload() { _abierta = false; }
        public override void Unload() { _abierta = false; }

        public override void ModifyInterfaceLayers(System.Collections.Generic.List<GameInterfaceLayer> layers)
        {
            if (!_abierta) return;
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (idx < 0) idx = layers.Count;
            layers.Insert(idx, new LegacyGameInterfaceLayer(
                "AethonMod: PrismaInterior", DibujarPanel, InterfaceScaleType.UI));
        }

        private static bool DibujarPanel()
        {
            try
            {
                if (!_abierta || Main.gameMenu || Main.dedServ) return true;
                if (!Systems.SubmundoGrimorio.Activo) { _abierta = false; return true; }

                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active) { _abierta = false; return true; }

                // LA LEY DE LA DISTANCIA (la casa: 280 px — el prisma no
                // grita a quién ya se fue).
                float dist = Vector2.Distance(yo.Center,
                    new Vector2(_tileX * 16f + 24f, _tileY * 16f + 8f));
                if (dist > 280f) { _abierta = false; return true; }

                SpriteBatch sb = Main.spriteBatch;
                Texture2D pixel = TextureAssets.MagicPixel.Value;

                // === LA GEOMETRÍA DEL PANEL ===
                int w = 430;
                int h = 240;
                int x = Main.screenWidth / 2 - w / 2;
                int y = Main.screenHeight / 2 - h / 2;
                if (x < 10) x = 10;
                if (y < 8) y = 8;

                // EL FONDO — el cielo estrellado de la casa (el del altar).
                Texture2D cielo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/Investigacion/Espacio_1").Value;
                sb.Draw(pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaFondo * 0.94f);
                sb.Draw(cielo, new Rectangle(x, y, w, h), Color.White * 0.92f);
                sb.Draw(pixel, new Rectangle(x, y, w, h), TintaFondo * 0.60f);
                Marco(sb, pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaVioleta * 0.80f);

                // EL TITULAR.
                string titTxt = Language.GetTextValue("Mods.AethonMod.PrismaInterior.Titulo");
                string subTxt = Language.GetTextValue("Mods.AethonMod.PrismaInterior.Subtitulo");
                Utils.DrawBorderString(sb, titTxt, new Vector2(x + w / 2f, y + 26f),
                    TintaVioleta, 1.05f, 0.5f, 0.4f);
                Utils.DrawBorderString(sb, subTxt, new Vector2(x + w / 2f, y + 46f),
                    TintaApagado, 0.75f, 0.5f, 0.4f);

                // === LA INTERACCIÓN (el borde del clic — TODO con
                //     mouseInterface: el juego NO usa el item al clicar) ===
                bool clicAhora = Main.mouseLeft && !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                bool clicNuevo = clicAhora && !_ultimoClicIzq;
                _ultimoClicIzq = clicAhora;

                // === EL BOTÓN 1 — VOLVER AL MUNDO REAL (el oro) ===
                Rectangle btnVolver = new Rectangle(x + 40, y + 74, w - 80, 44);
                bool hoverVolver = btnVolver.Contains(Main.mouseX, Main.mouseY);
                sb.Draw(pixel, btnVolver,
                    (hoverVolver ? TintaOro : new Color(212, 168, 84)) * (hoverVolver ? 0.34f : 0.20f));
                Marco(sb, pixel, btnVolver, TintaOro * (hoverVolver ? 0.95f : 0.55f));
                Utils.DrawBorderString(sb, Language.GetTextValue("Mods.AethonMod.PrismaInterior.BotonVolver"),
                    new Vector2(btnVolver.Center.X, btnVolver.Center.Y),
                    hoverVolver ? TintaOro : new Color(232, 200, 140), 0.95f, 0.5f, 0.5f);

                // === EL BOTÓN 2 — INVOCAR A AETHON (el violeta) ===
                bool yaVive = YaViveLaFamilia();
                Rectangle btnInvocar = new Rectangle(x + 40, y + 132, w - 80, 44);
                bool hoverInvocar = btnInvocar.Contains(Main.mouseX, Main.mouseY) && !yaVive;
                sb.Draw(pixel, btnInvocar,
                    (hoverInvocar ? TintaVioleta : new Color(120, 74, 178)) * (hoverInvocar ? 0.36f : 0.20f));
                Marco(sb, pixel, btnInvocar,
                    (yaVive ? TintaApagado : TintaVioleta) * (hoverInvocar ? 0.95f : 0.55f));
                string txtInvocar = yaVive
                    ? Language.GetTextValue("Mods.AethonMod.PrismaInterior.YaVive")
                    : Language.GetTextValue("Mods.AethonMod.PrismaInterior.BotonInvocar");
                Utils.DrawBorderString(sb, txtInvocar,
                    new Vector2(btnInvocar.Center.X, btnInvocar.Center.Y),
                    yaVive ? TintaApagado : (hoverInvocar ? TintaVioleta : new Color(196, 160, 240)),
                    0.95f, 0.5f, 0.5f);

                // === LA LEYENDA DE LA CEREMONIA (lo que nace del botón) ===
                string leyenda = Language.GetTextValue("Mods.AethonMod.PrismaInterior.Leyenda");
                Utils.DrawBorderString(sb, leyenda, new Vector2(x + w / 2f, y + 200f),
                    TintaApagado, 0.72f, 0.5f, 0.5f);

                // === EL BOTÓN CERRAR (la esquina de siempre) ===
                Rectangle btnCerrar = new Rectangle(x + w - 34, y + 8, 26, 26);
                bool hoverCerrar = btnCerrar.Contains(Main.mouseX, Main.mouseY);
                sb.Draw(pixel, btnCerrar, (hoverCerrar ? new Color(220, 90, 90) : TintaApagado) * 0.25f);
                Utils.DrawBorderString(sb, "X", new Vector2(btnCerrar.Center.X, btnCerrar.Center.Y),
                    hoverCerrar ? new Color(220, 90, 90) : TintaApagado, 1.0f, 0.5f, 0.5f);

                // === EL MOTOR DEL CLIC (el patrón de la casa: el clic
                //     no usa el ítem — el player es quien calla) ===
                if (hoverVolver || hoverInvocar || hoverCerrar || PanelContiene(new Rectangle(x, y, w, h)))
                    Main.LocalPlayer.mouseInterface = true;

                if (clicNuevo)
                {
                    if (hoverCerrar)
                    {
                        _abierta = false;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuClose);
                    }
                    else if (hoverVolver)
                    {
                        _abierta = false;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74);
                        Systems.SubmundoGrimorio.Salir(Main.LocalPlayer);
                    }
                    else if (hoverInvocar)
                    {
                        _abierta = false;
                        InvocarLaLlamada(yo);
                    }
                }
            }
            catch { _abierta = false; }
            return true;
        }

        /// <summary>¿El cursor vive DENTRO del rectángulo?</summary>
        private static bool PanelContiene(Rectangle r)
            => r.Contains(Main.mouseX, Main.mouseY);

        /// <summary>EL MARCO DOBLE de la casa.</summary>
        private static void Marco(SpriteBatch sb, Texture2D pixel, Rectangle r, Color c)
        {
            sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, 2), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y + r.Height - 2, r.Width, 2), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y, 2, r.Height), c);
            sb.Draw(pixel, new Rectangle(r.X + r.Width - 2, r.Y, 2, r.Height), c);
        }

        /// <summary>¿Ya camina ALGUNO de los fragmentos del recuerdo por
        /// el interior? (no se dobla el llamado — la ley de la casa).</summary>
        private static bool YaViveLaFamilia()
        {
            int[] tipos =
            {
                ModContent.NPCType<NPCs.AethonBoss>(),
                ModContent.NPCType<NPCs.AethonBossOscuro>(),
                ModContent.NPCType<NPCs.AethonBossFusionado>(),
            };
            for (int t = 0; t < tipos.Length; t++)
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n != null && n.active && n.type == tipos[t]) return true;
                }
            return false;
        }

        /// <summary>
        /// LA LLAMADA NACE DEL PRISMA: la ceremonia se ancla en el suelo
        /// delante del prisma (mirando al jugador) — el ALTAR se
        /// materializa y EL GRIMORIO DEL ETERNO flota justo encima (la
        /// coreografía completa de la v6.114 vive en CeremoniaLlamado).
        /// </summary>
        private static void InvocarLaLlamada(Player yo)
        {
            // EL ANCLA: el suelo delante del prisma, del lado del jugador.
            int dirX = yo.Center.X < _tileX * 16f ? -1 : 1;
            int tileX = _tileX + dirX * 9;
            int tileY = _tileY - 2;
            int suelo = -1;
            for (int y = tileY; y < tileY + 60 && y < Main.maxTilesY - 10; y++)
            {
                if (WorldGen.SolidTile(tileX, y)) { suelo = y; break; }
            }
            Vector2 ancla = suelo >= 0
                ? new Vector2(tileX * 16f + 8f, suelo * 16f)
                : new Vector2(tileX * 16f + 8f, (_tileY + 2) * 16f);

            Projectile.NewProjectile(yo.GetSource_Misc("PrismaInterior"),
                ancla, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Definitivas.CeremoniaLlamado>(),
                0, 0f, yo.whoAmI);
        }
    }
}
