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
    ///
    /// v6.50.84 — EL IRIS CIRCULAR + EL OJO EN TODAS PARTES. La letra del
    /// usuario: «cuando el item esta en el mundo no se ve bien el ojo, pero
    /// cuando el item esta en el inventario si se ve bien… cuando esta en la
    /// mano se ve mal al igual que cuando esta en el mundo suelto… te dare
    /// el mismo sprite y te lo dare en codigo para que uses la tecnica y
    /// puedas recortar bien el ojo, ya que lo recortaste en un cuadrado en
    /// ves de un circulo». TRES CURAS: (1) el iris ahora es un DISCO de 32×32
    /// recortado CIRCULARMENTE del sprite EXACTO en código del usuario
    /// (tools/gen_iris_circular_v65084.py — la .82 recortaba un cuadrado y
    /// las esquinas arrastraban esclera y fragmentos del anillo); (2) EN EL
    /// MUNDO la .83 dibujaba desde Item.position como si fuera la esquina de
    /// la textura — la convención vanilla (decompile de Main.DrawItem) es
    /// CENTRADO en el hitbox y ASENTADO EN EL FONDO: pivote = Item.Bottom −
    /// (0, altoFrame/2), con rotación item.velocity.X·0,2 (¡gira al volar!) —
    /// el ojo caía (+3, +7) px fuera del socket; (3) EN LA MANO el libro se
    /// dibuja dentro del proceso del JUGADOR (DrawPlayer_27_HeldItem) donde
    /// no corren los hooks de inventario ni mundo — ModifyItemDraw recibe la
    /// DrawData final y ahí se montan las capas, pegadas a la MISMA
    /// transform (incluido el espejo de mirar a la izquierda).
    /// </summary>
    public class GrimorioHambriento : ModItem
    {
        // === GEOMETRÍA (medida del arte: tools/gen_grimorio_hambriento_v65082.py) ===
        private static readonly Vector2 OJO = new Vector2(19.27f, 22.70f);   // centro del ojo en 36×49
        private static readonly Vector2 DESP_MAX = new Vector2(3.5f, 2.8f);  // viaje del iris (px de textura)
        private const float LERP_MIRADA = 0.10f;

        // v6.50.84 — la capa del iris es un DISCO 32×32 (recorte circular del
        // sprite exacto) que vive en una caja fuente de 174 px → 8,87 px de
        // juego: esta constante mapea la textura al espacio del libro.
        private const float IRIS_ESC = 0.2773f;

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
            spriteBatch.Draw(iris, posIris, null, drawColor, 0f, iris.Size() * 0.5f,
                scale * IRIS_ESC, SpriteEffects.None, 0f);
            if (rojo > 0f)
                spriteBatch.Draw(irisRojo, posIris, null, Alfa(rojo), 0f,
                    irisRojo.Size() * 0.5f, scale * IRIS_ESC, SpriteEffects.None, 0f);
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, float rotation, float scale, int whoAmI)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // v6.50.84 — LA CONVENCIÓN VANILLA (decompile de Main.DrawItem): el
            // ítem en el suelo se dibuja CENTRADO en el hitbox y ASENTADO EN SU
            // FONDO — pivote = Item.Bottom − (0, altoFrame/2), origen en el
            // centro del frame, rotación = item.velocity.X·0,2 (¡los ítems
            // giran mientras vuelan!). La .83 usaba Item.position como esquina:
            // el ojo caía (+3, +7) px fuera del socket.
            Main.GetItemDrawFrame(Item.type, out var _, out var frame);
            Vector2 origen = frame.Size() * 0.5f;
            Vector2 pivote = Item.Bottom - Main.screenPosition - new Vector2(0f, origen.Y);

            float rojo = NivelRojo();

            if (_fase > 0)
            {
                // el párpado REPLICA el draw vanilla del libro (píxel sobre píxel)
                bool medio = _fase > 7 || _fase < 4;
                var tex = ModContent.Request<Texture2D>(medio
                    ? "AethonMod/Content/Weapons/GrimorioHambriento_Medio"
                    : "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                spriteBatch.Draw(tex, pivote, frame, lightColor, rotation, origen, scale,
                    SpriteEffects.None, 0f);
                if (rojo > 0f)
                {
                    var texRojo = ModContent.Request<Texture2D>(medio
                        ? "AethonMod/Content/Weapons/GrimorioHambriento_Rojo_Medio"
                        : "AethonMod/Content/Weapons/GrimorioHambriento_Rojo_Cerrado").Value;
                    spriteBatch.Draw(texRojo, pivote, frame, Alfa(rojo), rotation, origen,
                        scale, SpriteEffects.None, 0f);
                }
                return;
            }

            // el iris: el texel del ojo GIRA con el libro mientras vuela
            Vector2 alOjo = (OJO + _despActual - origen) * scale;
            Vector2 posOjo = pivote + alOjo.RotatedBy(rotation);
            _posOjoPantalla = posOjo;
            _posValida = true;

            var iris = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
            var irisRojo = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_IrisRojo").Value;
            spriteBatch.Draw(iris, posOjo, null, lightColor, rotation, iris.Size() * 0.5f,
                scale * IRIS_ESC, SpriteEffects.None, 0f);
            if (rojo > 0f)
                spriteBatch.Draw(irisRojo, posOjo, null, Alfa(rojo), rotation,
                    irisRojo.Size() * 0.5f, scale * IRIS_ESC, SpriteEffects.None, 0f);
        }

        // =================================================================
        // v6.50.84 — EN LA MANO. El libro sostenido se dibuja dentro del
        // proceso del JUGADOR (DrawPlayer_27_HeldItem): ahí NO corren ni
        // PostDrawInInventory ni PostDrawInWorld — el ojo quedaba VACÍO.
        // ModifyItemDraw recibe la DrawData FINAL del libro: la agregamos
        // nosotros (return false) y encima van el párpado o el iris, pegados
        // a la MISMA transform — incluido el espejo de mirar a la izquierda
        // (itemEffect) y la gravedad invertida (FlipVertically).
        // =================================================================
        public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData,
            ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
        {
            if (Main.netMode == NetmodeID.Server)
                return true; // el servidor no dibuja: que vanilla haga lo suyo

            // el libro primero (la base es el socket vacío), las capas encima
            drawInfo.DrawDataCache.Add(drawData);
            if (coloredDrawData.HasValue)
                drawInfo.DrawDataCache.Add(coloredDrawData.Value);
            if (glowMaskDrawData.HasValue)
                drawInfo.DrawDataCache.Add(glowMaskDrawData.Value);

            bool espejoX = (drawData.effect & SpriteEffects.FlipHorizontally) != 0;
            bool espejoY = (drawData.effect & SpriteEffects.FlipVertically) != 0;
            float rojo = NivelRojo();

            if (_fase > 0)
            {
                // el párpado: MISMA transform que el libro — píxel sobre píxel
                bool medio = _fase > 7 || _fase < 4;
                var tex = ModContent.Request<Texture2D>(medio
                    ? "AethonMod/Content/Weapons/GrimorioHambriento_Medio"
                    : "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                drawInfo.DrawDataCache.Add(new DrawData(tex, drawData.position,
                    drawData.sourceRect, drawData.color, drawData.rotation,
                    drawData.origin, drawData.scale, drawData.effect));
                if (rojo > 0f)
                {
                    var texRojo = ModContent.Request<Texture2D>(medio
                        ? "AethonMod/Content/Weapons/GrimorioHambriento_Rojo_Medio"
                        : "AethonMod/Content/Weapons/GrimorioHambriento_Rojo_Cerrado").Value;
                    drawInfo.DrawDataCache.Add(new DrawData(texRojo, drawData.position,
                        drawData.sourceRect, Alfa(rojo), drawData.rotation,
                        drawData.origin, drawData.scale, drawData.effect));
                }
                return false;
            }

            // el iris: el texel del ojo, con el espejo aplicado AL DESPLAZA-
            // MIENTO para que SIGA mirando al cursor aunque el libro esté
            // reflejado (XNA espeja la textura alrededor del origen)
            Rectangle fr = drawData.sourceRect ?? new Rectangle(0, 0, 36, 49);
            Vector2 texel = OJO + new Vector2(
                _despActual.X * (espejoX ? -1f : 1f),
                _despActual.Y * (espejoY ? -1f : 1f));
            Vector2 q = new Vector2(
                (espejoX ? fr.Width - texel.X : texel.X) - drawData.origin.X,
                (espejoY ? fr.Height - texel.Y : texel.Y) - drawData.origin.Y);
            Vector2 posOjo = drawData.position + new Vector2(
                q.X * drawData.scale.X, q.Y * drawData.scale.Y).RotatedBy(drawData.rotation);

            var iris = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
            drawInfo.DrawDataCache.Add(new DrawData(iris, posOjo, null, drawData.color,
                drawData.rotation, iris.Size() * 0.5f, drawData.scale * IRIS_ESC,
                drawData.effect));
            if (rojo > 0f)
            {
                var irisRojo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_IrisRojo").Value;
                drawInfo.DrawDataCache.Add(new DrawData(irisRojo, posOjo, null, Alfa(rojo),
                    drawData.rotation, irisRojo.Size() * 0.5f, drawData.scale * IRIS_ESC,
                    drawData.effect));
            }

            if (drawInfo.drawPlayer.whoAmI == Main.myPlayer)
            {
                _posOjoPantalla = posOjo; // el ojo en mano también sigue al cursor
                _posValida = true;
            }

            return false; // ya agregamos la DrawData nosotros, con las capas en orden
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
