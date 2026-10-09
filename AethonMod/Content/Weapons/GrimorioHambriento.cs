using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// EL GRIMORIO HAMBRIENTO (v6.50.82) — el arma DEMO del Códice Vivo: los
    /// sprites del usuario convertidos en CAPAS (la base es el socket VACÍO;
    /// el iris es una capa 8×8 recortada de su arte; el párpado son los
    /// frames de su GIF de parpadeo).
    ///
    /// LA LETRA DEL USUARIO: «el libro está normal y parpadea cada 10 a 20
    /// segundos; cuando tiene hambre el parpadeo es más rápido; se agrega el
    /// movimiento del ojo; cuanto más hambre más rápido se mueve el ojo y
    /// más rápido parpadea, hasta alcanzar un parpadeo cada 2 segundos;
    /// además el grimorio hace que el ojo se vuelva rojo» — con el ciclo de
    /// hambre comprimido a UN MINUTO para ver el efecto.
    ///
    /// · HAMBRE 0→1 en 60 s (3600 ticks): parpadeo 10-20 s → 2 s exactos;
    ///   mirada 4 s → 0,35 s entre cambios; el iris dorado se enciende ROJO
    ///   (rampa 20 %→80 % de hambre).
    /// · EL OJO: sigue al CURSOR cuando está cerca (8 direcciones + centro),
    ///   vaga cuando no; el desplazamiento resbala (LERP 0,10/tick — nunca
    ///   salta). El viaje del iris en el arte original era de 0,7 px a
    ///   escala de juego (invisible) — por eso la capa: la mueve el código
    ///   (±3,5 px), visible y viva.
    /// · ARMA: clic izq dispara la descarga perseguidora (Nightglow 931, la
    ///   del Grimorio del Eterno); clic der REINICIA el apetito (demo
    ///   repetible). Sin maná (objeto de pruebas).
    /// </summary>
    public class GrimorioHambriento : ModItem
    {
        // === GEOMETRÍA (medida del arte: tools/gen_grimorio_hambriento_v65082.py) ===
        private static readonly Vector2 OJO = new Vector2(19.27f, 22.70f);   // centro del ojo en 36×49
        private static readonly Vector2 DESP_MAX = new Vector2(3.5f, 2.8f);  // viaje del iris (px de textura)
        private const float LERP_MIRADA = 0.10f;

        // === ESTADO DEMO (por máquina — objeto de pruebas, sin red) ===
        public static float Hambre;
        private static uint _tickMarcado;
        private static int _tParpadeo = 180;
        private static int _fase;                    // >0: secuencia del párpado (11→1)
        private static int _tMirada = 90;
        private static Vector2 _despMeta = Vector2.Zero;
        private static Vector2 _despActual = Vector2.Zero;
        private static Vector2 _posOjoPantalla = Vector2.Zero;
        private static bool _posValida;
        private static uint _ultimoDisparo;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 42;
            Item.damage = 30;
            Item.DamageType = DamageClass.Magic;
            Item.useStyle = ItemUseStyleID.HoldUp;    // el libro se alza (la familia del Grimorio)
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.mana = 0;                            // objeto de pruebas
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.autoReuse = true;
            Item.shoot = 931;        // Nightglow (fix 48688dd): el hook Shoot necesita shoot>0
            Item.shootSpeed = 12f;
            Item.UseSound = SoundID.Item4; // el sonido de la familia del grimorio
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) => true;

        public override bool? UseItem(Player player)
        {
            // === CLIC DERECHO: REINICIAR EL APETITO (la demo se repite) ===
            if (player.altFunctionUse == 2)
            {
                if (Main.myPlayer == player.whoAmI)
                {
                    Hambre = 0f;
                    _fase = 0;
                    _tParpadeo = 60;
                    _tMirada = 30;
                    _despMeta = _despActual = Vector2.Zero;
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Hambre.Reinicio"),
                        new Color(198, 200, 206));
                }
                return true;
            }
            return null;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
                return false; // el clic derecho no dispara

            // v5.18 (lección del Grimorio del Eterno): anti-doble por frame.
            if (Main.GameUpdateCount == _ultimoDisparo)
                return false;
            _ultimoDisparo = Main.GameUpdateCount;

            // La descarga perseguidora del Grimorio del Eterno (931 — homing vanilla).
            Projectile.NewProjectile(source, position, velocity, 931,
                damage, knockback, player.whoAmI);
            return false; // ya la spawneé yo: nada de doble vanilla
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }

        // =================================================================
        // EL CORAZÓN DE LA DEMO — corre SOLO en la máquina local (es 100 %
        // visual: el hambre no viaja por red ni afecta gameplay ajeno).
        // =================================================================
        public override void UpdateInventory(Player player)
        {
            if (Main.netMode == NetmodeID.Server)
                return;                       // el servidor no anima ojos
            if (player.whoAmI != Main.myPlayer)
                return;                       // solo el dueño local del libro
            if (Main.GameUpdateCount == _tickMarcado)
                return;                       // 2 copias del ítem: 1 tick total
            _tickMarcado = Main.GameUpdateCount;

            // --- EL MINUTO: hambre 0→1 en 3600 ticks ---
            Hambre = Math.Min(1f, Hambre + 1f / 3600f);
            float h = Hambre;

            // --- PARPADEO: 10-20 s (saciado) → 2 s exactos (hambre total) ---
            if (_fase > 0)
            {
                _fase--;
            }
            else if (--_tParpadeo <= 0)
            {
                _fase = 11; // 4t medio · 4t cerrado · 3t medio (≈0,18 s)
                float periodo = MathHelper.Lerp(15f * 60f, 2f * 60f, h);
                periodo *= 1f + (Main.rand.NextFloat() - 0.5f) * 0.66f * (1f - h); // ±33 % → 10-20 s
                _tParpadeo = Math.Max(20, (int)periodo);
            }

            // --- MIRADA: 4 s (perezoso) → 0,35 s (frenético) ---
            if (--_tMirada <= 0)
            {
                ElegirMirada();
                float dwell = MathHelper.Lerp(4f * 60f, 0.35f * 60f, h);
                _tMirada = Math.Max(8, (int)(dwell * (0.7f + Main.rand.NextFloat() * 0.7f)));
            }
            _despActual = Vector2.Lerp(_despActual, _despMeta, LERP_MIRADA);
        }

        private static void ElegirMirada()
        {
            Vector2 meta = Vector2.Zero;
            bool seguir = false;
            if (_posValida)
            {
                Vector2 delta = Main.MouseScreen - _posOjoPantalla;
                float d = delta.Length();
                if (d < 34f)
                {
                    meta = Vector2.Zero;      // el cursor ENCIMA: te mira fijo a los ojos
                    seguir = true;
                }
                else if (d < 480f)
                {
                    meta = SnapDireccion(delta) * DESP_MAX;  // te sigue por la pantalla
                    seguir = true;
                }
            }
            if (!seguir)
            {
                // vaga: el ojo explora solo (el centro entra con peso bajo)
                if (Main.rand.Next(10) == 0)
                    meta = Vector2.Zero;
                else
                {
                    double ang = Main.rand.NextDouble() * Math.PI * 2.0;
                    meta = SnapDireccion(new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * 100f)
                           * DESP_MAX;
                }
            }
            _despMeta = meta;
        }

        private static Vector2 SnapDireccion(Vector2 delta)
        {
            // 8 sectores de 45°: 0=D · 1=AbD · 2=Ab · 3=AbI · 4=I · 5=ArI · 6=Ar · 7=ArD
            int s = (int)Math.Round(Math.Atan2(delta.Y, delta.X) / (Math.PI / 4.0));
            s &= 7;
            return new Vector2((float)Math.Cos(s * Math.PI / 4.0), (float)Math.Sin(s * Math.PI / 4.0));
        }

        private static float NivelRojo() =>
            Math.Max(0f, Math.Min(1f, (Hambre - 0.2f) / 0.6f));

        private static Color Alfa(float a) => new Color(255, 255, 255, (int)(255 * a));

        // =================================================================
        // EL DIBUJO — la vanilla ya dibujó la base (el socket vacío es la
        // textura del ítem); aquí van ENCIMA el párpado o el iris.
        // =================================================================
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // El centro del ojo en pantalla: el texel OJO apunta a position
            // (XNA: el origin del draw es el pivote — mismo cálculo que vanilla).
            Vector2 posOjo = position + (OJO - origin) * scale;
            _posOjoPantalla = posOjo;
            _posValida = true;

            float rojo = NivelRojo();

            // --- PARPADEANDO: el párpado tapa el socket (frames del GIF) ---
            if (_fase > 0)
            {
                bool medio = _fase > 7 || _fase < 4;
                var tex = ModContent.Request<Texture2D>(medio
                    ? "AethonMod/Content/Weapons/GrimorioHambriento_Medio"
                    : "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                spriteBatch.Draw(tex, position, frame, drawColor, 0f, origin, scale,
                    SpriteEffects.None, 0f);
                if (rojo > 0f)
                {
                    var texRojo = ModContent.Request<Texture2D>(medio
                        ? "AethonMod/Content/Weapons/GrimorioHambriento_Rojo_Medio"
                        : "AethonMod/Content/Weapons/GrimorioHambriento_Rojo_Cerrado").Value;
                    spriteBatch.Draw(texRojo, position, frame, Alfa(rojo), 0f, origin, scale,
                        SpriteEffects.None, 0f);
                }
                return;
            }

            // --- ABIERTO: el iris (capa del arte) se desliza por el socket ---
            Vector2 posIris = posOjo + _despActual * scale;
            var iris = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
            var irisRojo = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_IrisRojo").Value;
            spriteBatch.Draw(iris, posIris, null, drawColor, 0f, iris.Size() * 0.5f, scale,
                SpriteEffects.None, 0f);
            if (rojo > 0f)
                spriteBatch.Draw(irisRojo, posIris, null, Alfa(rojo), 0f,
                    irisRojo.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, float rotation, float scale, int whoAmI)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Aproximación: el ojo a OJO·escala desde la esquina del ítem
            // (el bob de vanilla en el suelo es ±2 px — imperceptible aquí).
            Vector2 posOjo = Item.position - Main.screenPosition + OJO * scale;
            _posOjoPantalla = posOjo;
            _posValida = true;

            float rojo = NivelRojo();

            if (_fase > 0)
            {
                bool medio = _fase > 7 || _fase < 4;
                var tex = ModContent.Request<Texture2D>(medio
                    ? "AethonMod/Content/Weapons/GrimorioHambriento_Medio"
                    : "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                Vector2 o = tex.Size() * 0.5f;
                Vector2 p = Item.position - Main.screenPosition;
                spriteBatch.Draw(tex, p, null, lightColor, rotation, o, scale,
                    SpriteEffects.None, 0f);
                if (rojo > 0f)
                {
                    var texRojo = ModContent.Request<Texture2D>(medio
                        ? "AethonMod/Content/Weapons/GrimorioHambriento_Rojo_Medio"
                        : "AethonMod/Content/Weapons/GrimorioHambriento_Rojo_Cerrado").Value;
                    spriteBatch.Draw(texRojo, p, null, Alfa(rojo), rotation, o, scale,
                        SpriteEffects.None, 0f);
                }
                return;
            }

            Vector2 posIris = posOjo + _despActual * scale;
            var iris = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
            var irisRojo = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_IrisRojo").Value;
            spriteBatch.Draw(iris, posIris, null, lightColor, rotation, iris.Size() * 0.5f, scale,
                SpriteEffects.None, 0f);
            if (rojo > 0f)
                spriteBatch.Draw(irisRojo, posIris, null, Alfa(rojo), rotation,
                    irisRojo.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        // =================================================================
        // TOOLTIP VIVO — se actualiza CADA FRAME mientras lo señalas: la
        // barra de hambre sube en vivo y el color va del dorado al rojo.
        // =================================================================
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            float h = Hambre;
            int llenos = (int)Math.Round(h * 10f);
            char[] barra = new char[10];
            for (int i = 0; i < 10; i++)
                barra[i] = i < llenos ? '✦' : '—';
            string textoBarra = $"[{new string(barra)}] {(int)(h * 100f)}%";

            float rojo = NivelRojo();
            var color = Color.Lerp(new Color(198, 160, 78), new Color(235, 70, 55),
                Math.Max(0.25f, rojo));

            tooltips.Add(new TooltipLine(Mod, "HambreBarra",
                Language.GetTextValue("Mods.AethonMod.Hambre.Barra", textoBarra)) { OverrideColor = color });

            string estado = h >= 1f ? "Maximo"
                : h >= 0.6f ? "Furioso"
                : h >= 0.2f ? "Inquieto"
                : "Calmado";
            tooltips.Add(new TooltipLine(Mod, "HambreEstado",
                Language.GetTextValue("Mods.AethonMod.Hambre." + estado)) { OverrideColor = color });
        }
    }
}
