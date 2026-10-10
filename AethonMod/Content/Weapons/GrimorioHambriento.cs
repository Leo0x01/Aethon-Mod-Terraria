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
    /// EL ESTADO DE UN GRIMORIO (v6.50.86) — cada COPIA del libro anima su
    /// PROPIO ojo. La .82 guardaba el ciclo en statics de la CLASE: al llegar
    /// la tercera copia habría UN solo hambre compartido y los tres ojos
    /// marcarían el mismo paso. Ahora el estado vive en una instancia POR
    /// CLASE de ítem (GrimorioHambriento y sus dos copias declaran cada uno
    /// su `static EstadoGrimorio` y lo exponen vía la propiedad virtual
    /// `Estado`) — cada libro pasa hambre por su cuenta.
    ///
    /// El ciclo es el de la .82 (la letra del usuario): «el libro está normal
    /// y parpadea cada 10 a 20 segundos; cuando tiene hambre el parpadeo es
    /// más rápido; se agrega el movimiento del ojo; cuanto más hambre más
    /// rápido se mueve el ojo y más rápido parpadea, hasta alcanzar un
    /// parpadeo cada 2 segundos; además el grimorio hace que el ojo se vuelva
    /// rojo» — comprimido a UN MINUTO para la demo.
    ///
    /// v6.50.87 — LA MIRADA ERRÁTICA (la copia Errático marca su instancia
    /// con `Erratico = true`): sus cambios de mirada pasan de 3 s a 0,16 s,
    /// el resbalón se vuelve brusco (LERP 0,10 → 0,36 con el hambre) y cada
    /// meta lleva un tic que ninguna repetición comparte. Saciado es apenas
    /// más inquieto que el original; famélico es un ojo que no se queda quieto.
    /// El libro NORMAL no marca la bandera: su ciclo queda intacto.
    /// v6.50.90 — LA MIRADA NERVIOSA (la copia Nerviosa marca `Nervioso =
    /// true`): revisa cada 2,4 s → 0,42 s, sus miradas SALTAN a la meta
    /// (LERP 0,22 fijo — aterrizan de golpe), una de cada tres vuelve al
    /// CENTRO y la mitad de las demás se va de DARDO LATERAL. Miedo, no
    /// caos — sin el tic del Errático. El libro NORMAL tampoco marca esta
    /// bandera: su ciclo sigue intacto.
    /// </summary>
    public class EstadoGrimorio
    {
        // el viaje del iris (px de textura 36×49) y el resbalón de la mirada
        private static readonly Vector2 DESP_MAX = new Vector2(3.5f, 2.8f);
        private const float LERP_MIRADA = 0.10f;

        public float Hambre;
        public bool Erratico;        // v6.50.87 — la copia ERRÁTICA (mirada caótica)
        public bool Nervioso;        // v6.50.90 — la copia NERVIOSA (mirada ansiosa)
        public uint TickMarcado;
        public int TParpadeo = 180;
        public int Fase;                    // >0: secuencia del párpado (11→1)
        public int TMirada = 90;
        public Vector2 DespMeta = Vector2.Zero;
        public Vector2 DespActual = Vector2.Zero;
        public Vector2 PosOjoPantalla = Vector2.Zero;
        public bool PosValida;
        public uint UltimoDisparo;

        /// <summary>EL REINICIO del apetito (clic derecho — la demo se repite).</summary>
        public void Reiniciar()
        {
            Hambre = 0f;
            Fase = 0;
            TParpadeo = 60;
            TMirada = 30;
            DespMeta = DespActual = Vector2.Zero;
        }

        /// <summary>
        /// EL MINUTO — un tick de la simulación (el llamador ya filtró
        /// servidor / jugador ajeno / doble tick de copias del mismo ítem).
        /// Hambre 0→1 en 60 s; parpadeo 10-20 s → 2 s; mirada 4 s → 0,35 s.
        /// </summary>
        public void Paso()
        {
            Hambre = Math.Min(1f, Hambre + 1f / 3600f);
            float h = Hambre;

            // --- PARPADEO: 10-20 s (saciado) → 2 s exactos (hambre total) ---
            if (Fase > 0)
            {
                Fase--;
            }
            else if (--TParpadeo <= 0)
            {
                Fase = 11; // 4t medio · 4t cerrado · 3t medio (≈0,18 s)
                float periodo = MathHelper.Lerp(15f * 60f, 2f * 60f, h);
                periodo *= 1f + (Main.rand.NextFloat() - 0.5f) * 0.66f * (1f - h); // ±33 % → 10-20 s
                TParpadeo = Math.Max(20, (int)periodo);
            }

            // --- MIRADA: 4 s (perezoso) → 0,35 s (frenético);
            //     errática (v6.50.87): 3 s → 0,16 s — la mirada desenfrenada;
            //     nerviosa (v6.50.90): 2,4 s → 0,42 s — revisa seguido, sin
            //     llegar al frenesí de la errática: es miedo, no caos ---
            if (--TMirada <= 0)
            {
                ElegirMirada();
                float dwell = MathHelper.Lerp(4f * 60f, 0.35f * 60f, h);
                if (Erratico)
                    dwell = MathHelper.Lerp(3f * 60f, 0.16f * 60f, h);
                else if (Nervioso)
                    dwell = MathHelper.Lerp(2.4f * 60f, 0.42f * 60f, h);
                TMirada = Math.Max(8, (int)(dwell * (0.7f + Main.rand.NextFloat() * 0.7f)));
            }
            // v6.50.87 — la errática resbala MÁS BRUSCO con el hambre (0,10 → 0,36);
            // v6.50.90 — la nerviosa SALTA a su meta (0,22 fijo): miradas que
            // aterrizan de golpe — sin el resbalón suave del original ni la
            // brutalidad creciente de la errática
            DespActual = Vector2.Lerp(DespActual, DespMeta,
                Erratico ? 0.10f + 0.26f * h
                : Nervioso ? 0.22f
                : LERP_MIRADA);
        }

        private void ElegirMirada()
        {
            Vector2 meta = Vector2.Zero;
            bool seguir = false;
            if (PosValida)
            {
                Vector2 delta = Main.MouseScreen - PosOjoPantalla;
                float d = delta.Length();
                if (d < 34f)
                {
                    meta = Vector2.Zero;      // el cursor ENCIMA: te mira fijo a los ojos
                    seguir = true;
                }
                else if (d < 480f)
                {
                    meta = SnapDireccion(delta) * new Vector2(3.5f, 2.8f);  // te sigue por la pantalla
                    seguir = true;
                }
            }
            if (!seguir)
            {
                // vaga: el ojo explora solo (el centro entra con peso bajo)
                // v6.50.90 — la nerviosa: 1 de cada 3 miradas vuelve al
                // CENTRO (la revisada ansiosa de la casa) y la mitad de las
                // otras se va de DARDO LATERAL (el barrido
                // izquierda-derecha del susto). Con la bandera apagada la
                // secuencia de Main.rand es la MISMA de la .89 (los otros
                // libros no cambian ni un tiro de dado).
                bool alCentro = Nervioso ? Main.rand.Next(3) == 0
                                          : Main.rand.Next(10) == 0;
                if (alCentro)
                    meta = Vector2.Zero;
                else
                {
                    double ang = Main.rand.NextDouble() * Math.PI * 2.0;
                    if (Nervioso && Main.rand.Next(2) == 0)
                        ang = Main.rand.Next(2) == 0 ? 0.0 : Math.PI; // el dardo lateral
                    meta = SnapDireccion(new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * 100f)
                           * new Vector2(3.5f, 2.8f);
                }
            }
            // v6.50.87 — el tic errático: ninguna mirada se repite
            // (±0,35 / ±0,28 px escalados por el hambre — un temblor de meta)
            if (Erratico)
                meta += new Vector2((Main.rand.NextFloat() - 0.5f) * 0.7f,
                                    (Main.rand.NextFloat() - 0.5f) * 0.56f) * Hambre;
            DespMeta = meta;
        }

        // 8 sectores de 45°: 0=D · 1=AbD · 2=Ab · 3=AbI · 4=I · 5=ArI · 6=Ar · 7=ArD
        private static Vector2 SnapDireccion(Vector2 delta)
        {
            int s = (int)Math.Round(Math.Atan2(delta.Y, delta.X) / (Math.PI / 4.0));
            s &= 7;
            return new Vector2((float)Math.Cos(s * Math.PI / 4.0), (float)Math.Sin(s * Math.PI / 4.0));
        }

        /// <summary>La rampa del rojo: 20 %→80 % de hambre.</summary>
        public float NivelRojo() =>
            Math.Max(0f, Math.Min(1f, (Hambre - 0.2f) / 0.6f));
    }

    /// <summary>
    /// EL GRIMORIO HAMBRIENTO (v6.50.82) — el arma DEMO del Códice Vivo: los
    /// sprites del usuario convertidos en CAPAS (la base es el socket VACÍO;
    /// el iris es una capa 32×32 recortada de su arte; el párpado son sus
    /// sprites de ojo medio cerrado y cerrado).
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
    ///
    /// v6.50.85 — LA BASE LIMPIA + EL ROJO SOLO EN EL IRIS. La letra del
    /// usuario: «cuando recortas el iris toda la esclerotica queda con el
    /// agujero del iris en ves de estar completamente blanco como el resto
    /// de la esclerotica… lo que se pone rojo es solo el iris, el libro se
    /// debe quedar de color normal… cuando remuevas el iris has que los
    /// pixeles donde estaba el iris tomen el color de la esclerotica» + el
    /// sprite sin iris en código. La base ES el sprite sin iris EXACTO
    /// (socket blanco); el rojo vive SOLO en la capa IrisRojo.
    ///
    /// v6.50.86 — LOS PÁRPADOS PUROS DE CÓDIGO. La letra del usuario: «al
    /// usar el gif para abrir y cerrar el ojo hace que se note el cambio en
    /// cuanto a calidad, el codigo puro da mejor calidad ya que al ser
    /// codigo puedes recrear los pixeles fielmente, asi que te dare el
    /// codigo del ojo entrecerrado y cerrado». Verificado: los códigos del
    /// ojo medio cerrado y del cerrado son la MISMA corrida que la base
    /// (|Δ| = 0,0000 y 0 px distintos fuera del ojo) — los párpados son
    /// ahora LANCZOS puro de la MISMA fuente que la base (la .85 compostaba
    /// la zona del GIF con pluma sobre la base: esa costura era el salto de
    /// calidad). El parpadeo queda píxel sobre píxel con el libro. Además el
    /// estado pasó a EstadoGrimorio POR CLASE (ver arriba) para que las
    /// copias animen cada una su ojo.
    ///
    /// v6.50.87 — EL LIBRO NORMAL QUEDA TAL CUAL (la letra del usuario:
    /// «deja solo el grimorio hambriento normal y modifica las dos copias
    /// con otros efectos, borra los anteriores, esta vez que sean mas suave
    /// los efectos»). Este archivo SOLO GANA aditivos para las copias: las
    /// constantes pasan a protected (las copias anclan sus capas al mismo
    /// socket y al mismo iris), el draw en mundo se extrae a
    /// PostDrawInWorldCore (recibe un corrimiento: la Temblorosa le pasa su
    /// temblor — con Zero el dibujo del libro NORMAL es bit-idéntico) y
    /// PivoteEnMundo/Capa exponen la convención vanilla v6.50.84.
    /// </summary>
    public class GrimorioHambriento : ModItem
    {
        // === GEOMETRÍA (v6.50.85: el iris del ARTE — tools/gen_grimorio_hambriento_v65085.py) ===
        // protected: las copias anclan sus capas al MISMO socket (v6.50.86).
        protected static readonly Vector2 OJO = new Vector2(19.09f, 22.45f);   // centro del ojo en 36×49

        // v6.50.84 — la capa del iris es un DISCO 32×32 (recorte circular del
        // sprite exacto) que vive en una caja fuente de 174 px → 8,87 px de
        // juego: esta constante mapea la textura al espacio del libro.
        // v6.50.87: protected — las copias dibujan su iris con la misma escala.
        protected const float IRIS_ESC = 0.2773f;

        // === EL ESTADO POR COPIA (v6.50.86) ===
        private static readonly EstadoGrimorio _estado = new EstadoGrimorio();
        protected virtual EstadoGrimorio Estado => _estado;

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
                    Estado.Reiniciar();
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
            if (Main.GameUpdateCount == Estado.UltimoDisparo)
                return false;
            Estado.UltimoDisparo = Main.GameUpdateCount;

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
            if (Main.GameUpdateCount == Estado.TickMarcado)
                return;                       // 2 copias del mismo ítem: 1 tick total
            Estado.TickMarcado = Main.GameUpdateCount;
            Estado.Paso();
        }

        // v6.50.87 — protected: las copias tiñen su capa IrisRojo igual que el original.
        protected static Color Alfa(float a) => new Color(255, 255, 255, (int)(255 * a));

        /// <summary>Una capa del grimorio (Medio/Cerrado/Iris/IrisRojo) — el
        /// asset compartido de la familia.</summary>
        protected static Texture2D Capa(string nombre) =>
            ModContent.Request<Texture2D>("AethonMod/Content/Weapons/GrimorioHambriento_" + nombre).Value;

        /// <summary>LA CONVENCIÓN VANILLA del ítem en el suelo (v6.50.84):
        /// centrado en el hitbox y asentado en su fondo — la usan las copias
        /// para dibujar su libro en el mismo lugar exacto.</summary>
        protected Vector2 PivoteEnMundo(out Rectangle frame, out Vector2 origen)
        {
            Main.GetItemDrawFrame(Item.type, out var _, out frame);
            origen = frame.Size() * 0.5f;
            return Item.Bottom - Main.screenPosition - new Vector2(0f, origen.Y);
        }

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
            Estado.PosOjoPantalla = posOjo;
            Estado.PosValida = true;

            float rojo = Estado.NivelRojo();

            // --- PARPADEANDO: el párpado tapa el socket (v6.50.86: los
            //     sprites PUROS del usuario — misma corrida que la base) ---
            if (Estado.Fase > 0)
            {
                bool medio = Estado.Fase > 7 || Estado.Fase < 4;
                var tex = ModContent.Request<Texture2D>(medio
                    ? "AethonMod/Content/Weapons/GrimorioHambriento_Medio"
                    : "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                spriteBatch.Draw(tex, position, frame, drawColor, 0f, origin, scale,
                    SpriteEffects.None, 0f);
                // v6.50.85: el libro queda a color normal al parpadear — el
                // rojo vive SOLO en la capa del iris.
                return;
            }

            // --- ABIERTO: el iris (capa del arte) se desliza por el socket ---
            Vector2 posIris = posOjo + Estado.DespActual * scale;
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
            => PostDrawInWorldCore(spriteBatch, lightColor, alphaColor, rotation, scale, whoAmI,
                Vector2.Zero);

        /// <summary>v6.50.87 — el corazón del draw en mundo CON CORRIMIENTO:
        /// las capas del ojo (párpado o iris) van a pivote + corr. El libro
        /// normal llama con Zero (dibujo bit-idéntico al de la .86); la copia
        /// Temblorosa le pasa su temblor — el ojo tiembla JUNTO al libro.</summary>
        protected void PostDrawInWorldCore(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, float rotation, float scale, int whoAmI, Vector2 corr)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // v6.50.84 — LA CONVENCIÓN VANILLA (decompile de Main.DrawItem): el
            // ítem en el suelo se dibuja CENTRADO en el hitbox y ASENTADO EN SU
            // FONDO — pivote = Item.Bottom − (0, altoFrame/2), origen en el
            // centro del frame, rotación = item.velocity.X·0,2 (¡los ítems
            // giran mientras vuelan!).
            Main.GetItemDrawFrame(Item.type, out var _, out var frame);
            Vector2 origen = frame.Size() * 0.5f;
            Vector2 pivote = Item.Bottom - Main.screenPosition - new Vector2(0f, origen.Y);

            float rojo = Estado.NivelRojo();

            if (Estado.Fase > 0)
            {
                // el párpado REPLICA el draw vanilla del libro (píxel sobre píxel)
                bool medio = Estado.Fase > 7 || Estado.Fase < 4;
                var tex = ModContent.Request<Texture2D>(medio
                    ? "AethonMod/Content/Weapons/GrimorioHambriento_Medio"
                    : "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                spriteBatch.Draw(tex, pivote + corr, frame, lightColor, rotation, origen, scale,
                    SpriteEffects.None, 0f);
                // v6.50.85: el libro queda a color normal al parpadear.
                return;
            }

            // el iris: el texel del ojo GIRA con el libro mientras vuela
            Vector2 alOjo = (OJO + Estado.DespActual - origen) * scale;
            Vector2 posOjo = pivote + corr + alOjo.RotatedBy(rotation);
            Estado.PosOjoPantalla = posOjo;
            Estado.PosValida = true;

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

            if (Estado.Fase > 0)
            {
                // el párpado: MISMA transform que el libro — píxel sobre píxel
                bool medio = Estado.Fase > 7 || Estado.Fase < 4;
                var tex = ModContent.Request<Texture2D>(medio
                    ? "AethonMod/Content/Weapons/GrimorioHambriento_Medio"
                    : "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                drawInfo.DrawDataCache.Add(new DrawData(tex, drawData.position,
                    drawData.sourceRect, drawData.color, drawData.rotation,
                    drawData.origin, drawData.scale, drawData.effect));
                // v6.50.85: el libro queda a color normal al parpadear.
                return false;
            }

            // el iris: el texel del ojo, con el espejo aplicado AL DESPLAZA-
            // MIENTO para que SIGA mirando al cursor aunque el libro esté
            // reflejado (XNA espeja la textura alrededor del origen)
            float rojo = Estado.NivelRojo(); // v6.50.85: SOLO el iris se tiñe, nunca el libro
            Rectangle fr = drawData.sourceRect ?? new Rectangle(0, 0, 36, 49);
            Vector2 texel = OJO + new Vector2(
                Estado.DespActual.X * (espejoX ? -1f : 1f),
                Estado.DespActual.Y * (espejoY ? -1f : 1f));
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
                Estado.PosOjoPantalla = posOjo; // el ojo en mano también sigue al cursor
                Estado.PosValida = true;
            }

            return false; // ya agregamos la DrawData nosotros, con las capas en orden
        }

        // =================================================================
        // TOOLTIP VIVO — se actualiza CADA FRAME mientras lo señalas: la
        // barra de hambre sube en vivo y el color va del dorado al rojo.
        // =================================================================
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            float h = Estado.Hambre;
            int llenos = (int)Math.Round(h * 10f);
            char[] barra = new char[10];
            for (int i = 0; i < 10; i++)
                barra[i] = i < llenos ? '✦' : '—';
            string textoBarra = $"[{new string(barra)}] {(int)(h * 100f)}%";

            float rojo = Estado.NivelRojo();
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
