using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.Ojos
{
    /// <summary>
    /// ElOjoQueMira — v6.141 — LA NUEVA ARMA (la letra completa: «creo
    /// que seria buena idea hacer que todo gire a su ojo ya que es la
    /// ventana del grimorio el puede ver con el ojo y su magia se
    /// ejecuta desde su ojo, asi que EL OJO QUE MIRA, aqui el ojo
    /// aparece cerca del enemigo, LA MARCA SILENCIOSA para esto quien
    /// reciba un golpe de EL OJO QUE MIRA recibe LA MARCA SILENCIOSA
    /// asegurate de que la marca silenciosa active el debuff del vacio
    /// que ya tenemos, de esa forma en caso de multijugador el jugador
    /// enemigo recibe el daño del debuff, para todo esto crea un arma
    /// nueva, no olvides darsela al jugador al terminarla, de momento
    /// no toques el grimorio para esto»).
    ///
    /// EL DISEÑO (la idea aprobada del doc — la recomendación de la
    /// casa, HECHA ARMA):
    ///  · EL USO: un arma de MAGIA pura (clic = lanzar LA MIRADA). El
    ///    ojo NO nace del jugador: APARECE CERCA DEL ENEMIGO apuntado
    ///    (la letra: «aqui el ojo aparece cerca del enemigo») — se
    ///    materializa con su estallido de tinta, MIRA a la presa un
    ///    instante (la carga: el iris se abre) y DISPARA SU MIRADA:
    ///    un rayo corto violeta-dorado que golpea TODO lo que cruza.
    ///  · LA MARCA SILENCIOSA: TODO lo golpeado por la mirada recibe
    ///    LA MARCA (el glifo sobre la cabeza — 2 s) y la marca enciende
    ///    LA QUEMADURA DEL VACÍO (el debuff de la casa:
    ///    QuemaduraVacioExterior — 1% de vida máxima por medio
    ///    segundo, 5 s). EN PvP: el jugador enemigo marcado ARDE (el
    ///    Update(Player) nuevo de v6.141).
    ///  · LA TEXTURA: EL OJO DE LOS MINIONS (OjoEterno_Abierto — la
    ///    letra: «ahora el ojo que mira al jugador es el mismo ojo de
    ///    los minions usa ese ya tenemos todo lo que se necesita»).
    ///  · MEJORA CON EL TIEMPO (la letra del ataque básico): el daño
    ///    escala con EL NIVEL DEL LIBRO del portador (ShardLevelItem —
    ///    +1,5% por nivel: a nivel 100 pega el doble que al 1) y el
    ///    maná baja con él. NADA del grimorio se toca: es un arma
    ///    HERMANA que lee el nivel del libro.
    /// </summary>
    public class ElOjoQueMira : ModItem
    {
        // v6.153 — EL SPRITE PROPIO (la letra: «mejora el sprite del
        // item Ojo que mira»): el ojo del grimorio FORJADO — la almendra
        // de pergamino, el iris dorado con la pupila rasgada violeta y
        // su aro rúnico (ya NO el sprite prestado del escuadrón).
        public override string Texture => "AethonMod/Content/Weapons/Ojos/ElOjoQueMira";

        public override void SetStaticDefaults()
        {
            // EL ARMA NUEVA DE LA CASA: magia pura del ojo.
        }

        public override void SetDefaults()
        {
            Item.damage = 24;                   // la base (escala con el nivel del libro)
            Item.DamageType = DamageClass.Magic;
            Item.mana = 8;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Purple;
            Item.UseSound = SoundID.Item8;      // el destello mágico
            Item.shoot = ModContent.ProjectileType<LaMiradaProyectil>();
            Item.shootSpeed = 1f;               // (la mirada nace en el enemigo: la velocidad no manda)
            Item.autoReuse = true;
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            // LA MEJORA: −0,5% de costo por nivel del libro (tope −50%).
            int nivel = NivelDelLibro(player);
            mult *= Math.Max(0.5f, 1f - 0.005f * nivel);
        }

        /// <summary>EL NIVEL DEL LIBRO del portador (0 si no lo lleva):
        /// la mira crece con él — la letra: «un ataque que pueda
        /// mejorar con el tiempo».</summary>
        private static int NivelDelLibro(Player player)
        {
            try
            {
                for (int i = 0; i < 58; i++)
                {
                    Item it = player.inventory[i];
                    if (it == null || it.IsAir) continue;
                    if (it.type == ModContent.ItemType<GrimoireEternal>())
                    {
                        var s = it.GetGlobalItem<global::AethonMod.Content.Globals.ShardLevelItem>();
                        if (s != null) return Math.Max(0, s.Level - 1);
                    }
                }
            }
            catch { }
            return 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // ============================================================
            //  EL OJO APARECE CERCA DEL ENEMIGO (la letra): NO nace del
            //  jugador — busca la presa BAJO EL CURSOR (o la más cerca
            //  delante) y EL OJO se materializa a su lado, la mira YA
            //  cargando. Si no hay enemigo, el ojo nace delante del
            //  jugador mirando al cursor (la práctica al aire).
            // ============================================================
            Vector2 ancla = Main.MouseWorld;
            NPC presa = PresaBajoElCursor();
            if (presa != null)
                ancla = presa.Center + new Vector2(
                    (presa.Center.X < player.Center.X ? -1f : 1f) * 140f, -60f);
            else
            {
                // al aire: a media pantalla hacia el cursor
                Vector2 dir = Main.MouseWorld - player.Center;
                if (dir.LengthSquared() > 1f) dir.Normalize();
                ancla = player.Center + dir * 220f;
            }

            // EL DAÑO CON LA MEJORA: +1,5% por nivel del libro.
            int nivel = NivelDelLibro(player);
            int dano = (int)(damage * (1f + 0.015f * nivel));

            Projectile.NewProjectile(source, ancla.X, ancla.Y, 0f, 0f,
                type, dano, knockback, player.whoAmI);
            return false;   // el disparo manual muere: el OJO es el arma
        }

        /// <summary>La presa bajo el cursor (la más cercana al puntero).</summary>
        private static NPC PresaBajoElCursor()
        {
            NPC mejor = null;
            float mejorD = 700f * 700f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.friendly || n.townNPC || n.dontTakeDamage) continue;
                float d = n.Center.DistanceSQ(Main.MouseWorld);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        public override void AddRecipes()
        {
            // LA VÍA DE REPUESTO (la regla de la casa es ENTREGA DIRECTA —
            // la receta es para quien la pierda): la forja mágica mínima.
            CreateRecipe()
                .AddIngredient<global::AethonMod.Content.Items.Altar.ItemCorrupto>(15)
                .AddTile(Terraria.ID.TileID.Bookcases)
                .Register();
        }
    }

    /// <summary>
    /// LaMiradaProyectil — EL OJO QUE APARECE: se materializa cerca del
    /// enemigo, MIRA un instante (el iris se abre — la carga) y dispara
    /// LA MIRADA (el rayo corto que atraviesa). Usa LA TEXTURA DEL OJO
    /// DE LOS MINIONS (OjoEterno_Abierto — la letra).
    /// </summary>
    public class LaMiradaProyectil : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/OjoEterno_Abierto";

        // v6.153 — EL MINION TOTALMENTE ANIMADO (la letra: «hacer que
        // sea un minion totalmente animado lo que ataca ya que ahora
        // mismo solo sale un punto amarillo»): el ojo vive MÁS (mira,
        // dispara y SE QUEDA un instante vigilando — como un minion),
        // parpadea TODO el tiempo (el ciclo de 3 texturas del
        // escuadrón), su iris PERSIGUE a la presa y DILATA con la carga
        // y la muerte es un CIERRE de párpado (no un pop seco).
        private const int CARGA = 26;      // ticks mirando (el iris se abre)
        private const int VIDA = 110;      // vive: mira, dispara, vigila, cierra

        private int _tick;
        private Vector2 _presa;           // a quién mira
        private bool _disparado;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            // v6.144 — EL TAMAÑO DE UN MINION (la letra: «el ojo que
            // mira es muy grande, debe ser del tamaño de un minion»):
            // 22×22 — el cuerpo del escuadrón a ESCALA 0.375.
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VIDA;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            _tick++;

            // ¿A QUIÉN MIRA? la presa más cercana (de nuevo cada pocos
            // ticks — el mundo se mueve)
            if (_tick % 8 == 1)
            {
                NPC presa = PresaMasCerca(Projectile.Center);
                _presa = presa != null ? presa.Center :
                    (Main.player[Projectile.owner]?.Center ?? Projectile.Center);
            }

            // v6.153 — LA FLOTACIÓN DEL ESCUADRÓN (el bobbing del
            // OjoEternoMinion: el vaivén SUAVE de quien vela — con su
            // deriva lateral respirada, no solo el salto vertical)
            float bob = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.2f
                + Projectile.whoAmI) * 0.35f;
            float lado = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.1f
                + Projectile.whoAmI * 0.7f) * 0.18f;
            Projectile.velocity = new Vector2(lado, bob);

            // EL DISPARO: tras la carga, LA MIRADA
            // v6.154 — EL RAYO NACE DEL OJO Y SE QUEDA EN ÉL (la letra:
            // «el ojo se queda quieto flotando y ataque, el ataque sale
            // del ojo y da al enemigo, si el enemigo se mueve lo que se
            // mueve es la punta del rayo de perlin siguiendo al
            // enemigo»): el proyectil del rayo NO VUELA — nace EN el
            // ojo con velocidad CERO y le PASA su presa (ai[0] = el
            // whoAmI+1 de la víctima): el rayo se dibuja DEL ojo A la
            // presa, y la PUNTA la persigue si se mueve.
            if (_tick == CARGA && !_disparado)
            {
                _disparado = true;

                // ¿QUIÉN ES LA PRESA HOY? (la más cercana — la mirada
                // elige en el instante del disparo, no antes)
                NPC presaViva = PresaMasCerca(Projectile.Center);
                if (presaViva != null) _presa = presaViva.Center;
                Vector2 dir = _presa - Projectile.Center;
                if (dir.LengthSquared() > 1f) dir.Normalize();

                int idxPresa = 0;
                if (presaViva != null) idxPresa = presaViva.whoAmI + 1;

                // EL RAYO: anclado al ojo — su vida es la mirada
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<LaMiradaRayo>(),
                    Projectile.damage, Projectile.knockBack, Projectile.owner,
                    idxPresa);

                SoundEngine.PlaySound(SoundID.Item73.WithPitchOffset(0.2f), Projectile.Center);

                // EL TEATRO: el destello del disparo
                for (int k = 0; k < 10; k++)
                {
                    Terraria.Dust d = Terraria.Dust.NewDustPerfect(Projectile.Center,
                        Terraria.ID.DustID.Enchanted_Pink,
                        dir * Main.rand.NextFloat(2f, 5f) +
                            Main.rand.NextVector2Circular(2f, 2f),
                        160, new Color(200, 150, 255), 0.9f);
                    d.noGravity = true;
                }
            }

            // v6.153 — LA DESPEDIDA DEL MINION: tras el disparo VIGILA
            // (sigue mirando a su presa, parpadeando) y al final CIERRA
            // el párpado antes de irse (el ojo se DUERME — no un pop).
            if (_tick > VIDA - 10)
            {
                // los últimos 10 t: el párpado cierra (lo dibuja el
                // PreDraw con el ciclo forzado) y la última luz calla
            }
            if (_tick > VIDA)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // ============================================================
            //  v6.144 — LA ANIMACIÓN DE LOS MINIONS EXACTA (la letra:
            //  «debe ser del tamaño de un minion y ademas debe estar
            //  animado, su mirada debe ser el ataque, para esto lo mejor
            //  es copiar exactamente la animacion de los minions»): EL
            //  MISMO MECANISMO del escuadrón (OjoEternoMinion.DrawElOjo)
            //  — las TRES texturas (Abierto/Medio/Cerrado) con el
            //  parpadeo escalonado, el IRIS que SE DESLIZA hacia la
            //  presa (MIRA_X/MIRA_Y del escuadrón) y se DILATA con la
            //  carga, el pop de nacimiento +16%, y LA ESCALA MINION
            //  (0.375 — ya NO es un ojo gigante).
            // ============================================================
            float f = Math.Min(1f, (float)_tick / CARGA);
            var pos = Projectile.Center - Main.screenPosition;

            // ============================================================
            //  v6.153 — EL CICLO DE PÁRPADO DEL ESCUADRÓN, DE VERDAD
            //  (la letra: «un minion totalmente animado»): el ojo
            //  parpadea CADA ~2,6 s (el ciclo vivo del OjoEternoMinion —
            //  abierto con pausas largas → medio un instante → cerrado
            //  un flash → medio), y en la MUERTE el párpado CIERRA
            //  completo (los últimos 10 t: el ojo se duerme al irse).
            // ============================================================
            bool muriendo = _tick > VIDA - 10;
            string nombreTex = "AethonMod/Content/Projectiles/OjoEterno_Abierto";
            if (muriendo)
            {
                // el cierre final: medio → cerrado, según los t que falten
                nombreTex = _tick > VIDA - 5
                    ? "AethonMod/Content/Projectiles/OjoEterno_Cerrado"
                    : "AethonMod/Content/Projectiles/OjoEterno_Medio";
            }
            else
            {
                double ciclo = (Main.GlobalTimeWrappedHourly * 1.4 + Projectile.whoAmI * 0.37) % 2.6;
                if (ciclo > 2.05 && ciclo < 2.25) nombreTex = "AethonMod/Content/Projectiles/OjoEterno_Medio";
                else if (ciclo >= 2.25 && ciclo < 2.42) nombreTex = "AethonMod/Content/Projectiles/OjoEterno_Cerrado";
                else if (ciclo >= 2.42 && ciclo < 2.6) nombreTex = "AethonMod/Content/Projectiles/OjoEterno_Medio";
            }
            Texture2D tex = ModContent.Request<Texture2D>(nombreTex).Value;

            // ============================================================
            //  LA ESCALA DE MINION DE VERDAD (0.55 — la cura del «solo
            //  sale un punto amarillo»: la v6.144 lo dejó en 0.375 y el
            //  glow lo comía) + el pop de nacimiento (+16%) + el
            //  hinchado de la carga (respira al 8% mientras carga) + LA
            //  INCLINACIÓN DEL ESCUADRÓN (el ojo SE INCLINA hacia lo
            //  que mira — la rotación viva del OjoEternoMinion).
            // ============================================================
            const float ESCALA = 0.55f;
            float pop = _tick < 26 ? 1.16f - 0.16f * (_tick / 26f) : 1f;
            float esc = ESCALA * pop * (1f + 0.08f * f);

            // ============================================================
            //  v6.154 — EL RESPLANDOR ES BRUMA DE LA LIBRERÍA (la letra:
            //  «mejor que tenga bruma o brillos… ya sabes que se debe
            //  usar la libreria de la casa»): el CÍRCULO AMARILLO era
            //  un SoftGlow PLANO (un disco de geometría plana — el
            //  pecado de la casa). AHORA: LumenLib.Bloom — el núcleo
            //  cálido con SU halo que respira (violeta→dorado al
            //  abrirse la carga) y un pulso vivo sobre él. Cero discos.
            // ============================================================
            Color cGlow = Color.Lerp(new Color(150, 84, 235), new Color(255, 200, 90), f);
            float brillo = 26f + 16f * f + 3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f);
            global::AethonMod.Content.VFX.LumenLib.Bloom(
                Main.spriteBatch, pos, brillo * esc, cGlow,
                0.42f + 0.28f * f);
            global::AethonMod.Content.VFX.LumenLib.BloomPulse(
                Main.spriteBatch, pos, brillo * 1.5f * esc, cGlow,
                0.20f + 0.18f * f, Main.GlobalTimeWrappedHourly * 2.2f + Projectile.whoAmI);

            // (el cuerpo ya vive ABAJO, con SU inclinación — la del iris)

            // ============================================================
            //  EL IRIS VIVO DEL ESCUADRÓN (el mecanismo EXACTO del
            //  OjoEterneMinion.DrawElOjo): el núcleo dorado SE DESLIZA
            //  por la almendra hacia la presa (MIRA_X/MIRA_Y de la
            //  casa), se DILATA con la carga, y el CUERPO ENTERO se
            //  INCLINA hacia su mirada (la rotación viva del escuadrón
            //  — media vuelta del rumbo, suave).
            // ============================================================
            Vector2 hacia = _presa - Projectile.Center;
            float mx = 0f, my = 0f;
            float inclinacion = 0f;
            if (hacia.LengthSquared() > 1f)
            {
                hacia.Normalize();
                mx = hacia.X * 7.5f * esc;
                my = hacia.Y * 2.5f * esc;
                inclinacion = hacia.ToRotation() * 0.10f;   // la inclinación viva
            }
            // el cuerpo con SU inclinación (el minion entero mira)
            Main.spriteBatch.Draw(tex, pos, null,
                Microsoft.Xna.Framework.Color.White * (0.85f + 0.15f * f) * (muriendo ? 0.8f : 1f),
                inclinacion, tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);

            // ============================================================
            //  v6.154 — EL IRIS TAMBIÉN ES BRUMA (la cura del «circulo
            //  amarillo»): el núcleo dorado que SE DESLIZA hacia la
            //  presa ya no es un disco plano — ES UN BLOOM de la
            //  librería (núcleo cálido + halo) que se DILATA con la
            //  carga y MIRA deslizándose por la almendra (mx, my — el
            //  mecanismo del escuadrón de siempre).
            // ============================================================
            float iris = (10f + 5f * f) * esc;   // el radio del iris (px — bruma, no escala de textura)
            global::AethonMod.Content.VFX.LumenLib.Bloom(
                Main.spriteBatch, pos + new Vector2(mx, my), iris,
                new Color(255, 200, 90), 0.80f + 0.15f * f);
            global::AethonMod.Content.VFX.LumenLib.Bloom(
                Main.spriteBatch, pos + new Vector2(mx, my), iris * 0.45f,
                new Color(255, 240, 200), 0.9f);
            return false;
        }

        private static NPC PresaMasCerca(Vector2 desde)
        {
            NPC mejor = null;
            float mejorD = 900f * 900f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.friendly || n.townNPC || n.dontTakeDamage) continue;
                float d = n.Center.DistanceSQ(desde);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }
    }

    /// <summary>
    /// LaMiradaRayo — v6.154 — EL RAYO DE LA MIRADA, ANCLADO AL OJO.
    /// La letra: «el ojo se queda quieto flotando y ataque, el ataque
    /// sale del ojo y da al enemigo, si el enemigo se mueve lo que se
    /// mueve es la punta del rayo de perlin siguiendo al enemigo».
    /// EL DISEÑO NUEVO:
    ///   · EL ORIGEN ES EL OJO (el proyectil NACE en el ojo y NO SE
    ///     MUEVE — el origen del rayo viaja con el ojo que flota);
    ///   · LA PUNTA ES LA PRESA (ai[0] = whoAmI+1 de la víctima: si se
    ///     mueve, la PUNTA la sigue — el rayo se re-apunta cada tick);
    ///   · LA LÍNEA es ojo→presa EXACTA: el rayo de Perlin recorre esa
    ///     distancia real y su COLISIÓN es esa misma línea.
    /// TODO lo que toca recibe LA MARCA SILENCIOSA (el glifo) y la
    /// QUEMADURA DEL VACÍO (el debuff de la casa — en PvP el jugador
    /// enemigo ARDE: la letra).
    /// </summary>
    public class LaMiradaRayo : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int LARGO_MIN = 60;   // la mirada mínima digna (px)
        private const int VIDA = 26;        // el ojo sostiene la mirada un instante

        /// <summary>LA PRESA (ai[0] = whoAmI + 1; 0 = al aire: recto
        /// hacia donde miraba el ojo — la práctica).</summary>
        private NPC Presa => Projectile.ai[0] <= 0f ? null
            : Main.npc[(int)Projectile.ai[0] - 1];

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;      // ATRAVIESA todo
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.timeLeft = VIDA;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        /// <summary>EL ORIGEN: el OJO que disparó (el proyectil padre
        /// — LaMiradaProyectil del mismo dueño, el más cercano). El
        /// rayo SIEMPRE nace del ojo, aunque el ojo flote.</summary>
        private Vector2 OrigenDelOjo()
        {
            int tipoOjo = ModContent.ProjectileType<LaMiradaProyectil>();
            Projectile mejor = null;
            float mejorD = 600f * 600f;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr == null || !pr.active || pr.type != tipoOjo ||
                    pr.owner != Projectile.owner) continue;
                float d = pr.Center.DistanceSQ(Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = pr; }
            }
            return mejor != null ? mejor.Center : Projectile.Center;
        }

        /// <summary>LA PUNTA DEL RAYO: la presa (que se MUEVE — la
        /// punta la sigue) o un punto al aire en la dirección de
        /// siempre.</summary>
        private Vector2 PuntaDelRayo(Vector2 origen)
        {
            NPC presa = Presa;
            if (presa != null && presa.active && presa.life > 0)
                return presa.Center;
            // sin presa (murió o era práctica): la recta de la mirada
            Vector2 dir = Projectile.velocity;
            if (dir.LengthSquared() < 0.01f) dir = -Vector2.UnitY;
            dir.Normalize();
            return origen + dir * 320f;
        }

        public override void AI()
        {
            // EL RAYO NO SE MUEVE (la letra): el origen viaja con el
            // ojo (lo pide OrigenDelOjo al dibujar/colisionar) y la
            // punta con la presa — el proyectil solo VIVE aquí.
            Projectile.velocity *= 0f;
            Projectile.Center = OrigenDelOjo();

            // LA ESTELA sobre la línea (las motas violeta-doradas de la
            // casa, repartidas por el recorrido de la mirada)
            if (Main.rand.NextBool(2))
            {
                Vector2 fin = PuntaDelRayo(Projectile.Center);
                float k = Main.rand.NextFloat();
                Terraria.Dust d = Terraria.Dust.NewDustPerfect(
                    Vector2.Lerp(Projectile.Center, fin, k),
                    Terraria.ID.DustID.Enchanted_Pink,
                    Main.rand.NextVector2Circular(1.2f, 1.2f),
                    140, new Color(200, 150, 255), 0.7f);
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // LA LÍNEA DE LA MIRADA: del ojo a la presa (la punta que
            // sigue — la MISMA línea que se dibuja: donde ves el rayo,
            // ahí golpea)
            Vector2 origen = OrigenDelOjo();
            Vector2 fin = PuntaDelRayo(origen);
            if ((fin - origen).LengthSquared() < 1f) return null;
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(),
                targetHitbox.Size(), origen, fin, 10f, ref _);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            LaMarcaSilenciosa.Marcar(target, Main.player[Projectile.owner]);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // PvP: el jugador enemigo también recibe LA MARCA (arde con
            // el debuff — la letra: «en caso de multijugador el jugador
            // enemigo recibe el daño del debuff»).
            LaMarcaSilenciosa.MarcarPlayer(target, Main.player[Projectile.owner]);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // ============================================================
            //  v6.154 — EL PERLIN DEL OJO A LA PRESA (la letra: «el
            //  ataque sale del ojo y da al enemigo, si el enemigo se
            //  mueve lo que se mueve es la punta del rayo de perlin
            //  siguiendo al enemigo»): el arco eléctrico de la casa
            //  (StormLib.PerlinBolt — el meandro fBm de 3 octavas,
            //  halo violeta + núcleo blanco-oro) nace EN EL OJO
            //  (origen FIJO que flota con él) y TERMINA EN LA PRESA
            //  (la punta que SIGUE: cada tick la línea se re-apunta).
            //  NADA del rayo vive detrás del ojo — su mitad trasera
            //  murió con la v6.153 (era el «sale de mas atras»).
            // ============================================================
            Vector2 origen = OrigenDelOjo();
            Vector2 fin = PuntaDelRayo(origen);
            var inicio = origen - Main.screenPosition;
            var finP = fin - Main.screenPosition;

            // la longitud real de la mirada (mínimo digno — una chispa
            // corta sigue siendo una mirada)
            float largo = Vector2.Distance(inicio, finP);
            if (largo < 8f) return false;
            if (largo < LARGO_MIN)
            {
                Vector2 dir = Vector2.Normalize(finP - inicio);
                finP = inicio + dir * LARGO_MIN;
            }

            float vida = Projectile.timeLeft / (float)VIDA;
            int seed = Projectile.whoAmI * 31 + (int)(Main.GameUpdateCount * 0.6f);
            global::AethonMod.Content.VFX.StormLib.PerlinBolt(
                Main.spriteBatch, inicio, finP, seed, 2,
                7f * vida,                                       // el ancho del arco
                new Color(150, 84, 235) * (0.55f * vida),        // el halo violeta
                new Color(255, 240, 210) * (0.85f * vida),       // el núcleo claro
                1f, 0.14f);                                      // amplitud del meandro

            // LA PUNTA VIVA: el punto de contacto ARDE (el instante de
            // la mirada tocando a su presa — bruma de la librería)
            if (Presa != null && Presa.active && Presa.life > 0)
            {
                global::AethonMod.Content.VFX.LumenLib.Bloom(
                    Main.spriteBatch, finP, 22f * vida,
                    new Color(200, 150, 255), 0.5f * vida);
                global::AethonMod.Content.VFX.LumenLib.Bloom(
                    Main.spriteBatch, inicio, 18f * vida,
                    new Color(255, 220, 140), 0.55f * vida);
            }
            return false;
        }
    }

    /// <summary>
    /// LaMarcaSilenciosa — EL DEBUFF NUEVO (la letra: «quien reciba un
    /// golpe de EL OJO QUE MIRA recibe LA MARCA SILENCIOSA asegurate de
    /// que la marca silenciosa active el debuff del vacio que ya
    /// tenemos»): el glifo mudo sobre la cabeza — 2 s de silencio y
    /// luego ENCIENDE la QUEMADURA DEL VACÍO de la casa (5 s: 1% de
    /// vida máxima por medio segundo). En PvP: el jugador marcado arde
    /// (QuemaduraVacioExterior.Update(Player) — v6.141).
    /// </summary>
    public class LaMarcaSilenciosa : ModBuff
    {
        public override string Texture => "AethonMod/Content/Buffs/AuraVacioBuff";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // EL GLIFO (las motas violetas cayendo del aire sobre su cabeza)
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && Main.rand.NextBool(5))
            {
                Terraria.Dust d = Terraria.Dust.NewDustPerfect(
                    npc.Top - new Vector2(0f, 14f),
                    Terraria.ID.DustID.Enchanted_Pink,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), 0.6f),
                    150, new Color(170, 120, 235), 0.6f);
                d.noGravity = false;
            }
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && Main.rand.NextBool(5))
            {
                Terraria.Dust d = Terraria.Dust.NewDustPerfect(
                    player.Top - new Vector2(0f, 14f),
                    Terraria.ID.DustID.Enchanted_Pink,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), 0.6f),
                    150, new Color(170, 120, 235), 0.6f);
                d.noGravity = false;
            }
        }

        /// <summary>MARCA a un NPC: 2 s de glifo y ENCIENDE la
        /// quemadura del vacío (la casa: 5 s de fuego).</summary>
        public static void Marcar(NPC target, Player dueno)
        {
            try
            {
                if (target == null || !target.active) return;
                target.AddBuff(ModContent.BuffType<LaMarcaSilenciosa>(), 120, false);
                // LA QUEMADURA DEL VACÍO (la intensidad de la casa: 1x)
                target.AddBuff(ModContent.BuffType
                    <global::AethonMod.Content.Buffs.QuemaduraVacioExterior>(), 300, false);
                var vacio = target.GetGlobalNPC
                    <global::AethonMod.Content.Globals.VacioGlobalNPC>();
                if (vacio != null)
                {
                    vacio.IntensidadQuemadura = Math.Max(vacio.IntensidadQuemadura, 1.5f);
                    if (dueno != null && dueno.active)
                        vacio.DuenoVacio = dueno.whoAmI + 1;
                }
            }
            catch { }
        }

        /// <summary>MARCA a un PLAYER (PvP): el glifo + el fuego — el
        /// Update(Player) de la quemadura hace el resto.</summary>
        public static void MarcarPlayer(Player target, Player dueno)
        {
            try
            {
                if (target == null || !target.active) return;
                target.AddBuff(ModContent.BuffType<LaMarcaSilenciosa>(), 120, false, true);
                target.AddBuff(ModContent.BuffType
                    <global::AethonMod.Content.Buffs.QuemaduraVacioExterior>(), 300, false, true);
            }
            catch { }
        }
    }
}
