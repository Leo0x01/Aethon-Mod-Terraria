using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Nervioso
{
    /// <summary>
    /// SOMBRAPAGINAETERNO — v6.153 — LA SOMBRA DE LA PÁGINA DEL GRIMORIO
    /// DEL ETERNO (la letra: «dale al grimorio la sombra de la pagina
    /// original esa que ahora no tiene ni ojos ni colmillos, y ademas en
    /// el grimorio hacer que por cada 20 despertares este pueda usar una
    /// sombra de la pagina extra y que se vea reflejado siempre que la
    /// use, cada sombra de la pagina que use sera individual»).
    ///
    /// LA SOMBRA VISIBLE: el tentáculo NEGRO LISO de la página original
    /// (la v6.152: sin ojos, sin colmillos, sin líneas rojas — SombrasLib
    /// con Masa lisa y bruma) nacido DEL LIBRO FLOTANTE del Eterno.
    ///
    /// LA LETRA DEL DAÑO: «en el modo caza del grimorio del eterno, su
    /// sombra de la pagina hace 1% de daño por segundo, y ese 1% es de
    /// la vida maxima del enemigo» — UN golpe de 1%·lifeMax cada 60
    /// ticks POR SOMBRA: si el grimorio CONCENTRA varias sombras sobre
    /// la misma presa, CADA UNA aprieta su 1% (la presa arde ×N).
    ///
    /// EL PAGO: cuando la presa cae, la sombra la ABSORBE (sin loot, el
    /// festín del libro) y la sombra VUELVE al grimorio (SombrasGastadas
    /// −1 en el alma del libro que la llamó).
    /// </summary>
    public class SombraPaginaEterno : ModProjectile
    {
        /// <summary>LA CADENCIA DE LA LETRA: un golpe por segundo.</summary>
        private const int CADENCIA = 60;

        /// <summary>La presa (ai[0] = whoAmI + 1).</summary>
        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 44;
            Projectile.tileCollide = false;
            // el festín del libro NO es del jugador (como SombraPaginaCaza)
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3600;
            Projectile.netImportant = true;
        }

        private Vector2 RaizDeSombra()
        {
            int tipo = ModContent.ProjectileType<GrimorioNerviosoFlotante>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == tipo && p.owner == Projectile.owner)
                    return p.Center;
            }
            Player dueño = Main.player[Projectile.owner];
            return dueño != null && dueño.active ? dueño.MountedCenter : Projectile.Center;
        }

        /// <summary>¿El libro que me llamó sigue vivo y con SU alma? (el
        /// grimorio del Eterno que pagó esta sombra).</summary>
        private global::AethonMod.Content.Globals.AlmaEternoGlobalItem AlmaDelLibro()
        {
            Player dueño = Main.player[Projectile.owner];
            if (dueño == null || !dueño.active) return null;
            Item libro = global::AethonMod.Content.Globals.AlmaEternoGlobalItem.Activo(dueño);
            return global::AethonMod.Content.Globals.AlmaEternoGlobalItem.De(libro);
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC presa = Presa;
            bool autoridad = Main.netMode != NetmodeID.MultiplayerClient;
            bool presaValida = presa != null && presa.active && presa.life > 0;

            // LA PRESA MURIÓ (o desapareció): la sombra vuelve al libro
            // y se disipa (su sombra NO se pierde: SombrasGastadas−1)
            if (!presaValida)
            {
                if (t > 6f)
                {
                    DevolverLaSombra();
                    Projectile.Kill();
                }
                Projectile.velocity *= 0.92f;
                return;
            }

            // LA CAZA: persigue su presa individual (el hilillo de la
            // casa — SombraPaginaCaza: giro suave + tope por distancia)
            Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
            float dist = Vector2.Distance(presa.Center, Projectile.Center);
            if (dist > 8f)
            {
                float vel = MathHelper.Clamp(dist * 0.05f, 18f, 42f);
                float actual = Projectile.velocity.ToRotation();
                float deseado = hacia.ToRotation();
                float giro = MathHelper.WrapAngle(deseado - actual);
                Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.26f, 0.26f))
                    .ToRotationVector2() * vel;
                Projectile.rotation = deseado;
            }
            else
            {
                Projectile.Center = presa.Center;
                Projectile.velocity = Vector2.Zero;
            }

            // EL DRENAJE DE LA LETRA NUEVA (v6.160, solo AUTORIDAD): «1
            // más la suma del daño de invocación total y el daño magico
            // total menos el 5% de la vida maxima del jugador mas el 1%
            // del mana total» — LA FÓRMULA DE LA CASA PARA TODAS LAS
            // SOMBRAS DE LA PÁGINA DEL ETERNO (y «deben hacer un poco
            // mas de daño», decía la letra: la vieja era un 1% plano de
            // lifeMax — un hilo; la nueva lee EL PODER REAL del
            // portador). El golpe NUNCA mata: a 1 de vida, LA ABSORCIÓN
            // (la sombra se la come entera y VUELVE al libro).
            if (autoridad && t > 10f && t % CADENCIA == 0f)
            {
                int golpe = GolpeDeLaSombra(Main.player[Projectile.owner], presa);
                if (presa.life - golpe <= 1)
                {
                    // LA ABSORCIÓN — el festín del Eterno: sin loot, sin
                    // crédito; XP del libro; la sombra VUELVE.
                    Weapons.GrimorioHambrientoNervioso.AbsorberPresa(presa,
                        Main.player[Projectile.owner],
                        ModContent.ItemType<Weapons.GrimoireEternal>());
                    DevolverLaSombra();
                    Projectile.Kill();
                    return;
                }
                presa.life -= golpe;
                presa.netUpdate = true;
                if (t % 120 == 0)
                    Sonar(SoundID.NPCHit9.WithPitchOffset(-0.15f).WithVolumeScale(0.4f), Projectile.Center);
            }

            // polvillo violeta del vuelo (la firma de la casa)
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(7))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(20, 20), 40, 40,
                    DustID.Shadowflame, 0f, -0.3f, 128, default, 0.5f);
                Main.dust[d].noGravity = true;
            }
        }

        // ================================================================
        //  v6.160 — LA FÓRMULA DE LAS SOMBRAS DEL ETERNO (la letra):
        //  «el nuevo calculo de daño sera el siguiente, 1 mas la suma
        //  del daño de invocacion total y el daño magico total menos el
        //  5% de la vida maxima del jugador mas el 1% del mana total,
        //  ese sera el nuevo calculo de daño» — PÚBLICA y ESTÁTICA:
        //  TODAS las sombras de la página que sirvan al Grimorio del
        //  Eterno (la caza del libro flotante, la concentración del
        //  arma SombraDeLaPagina, cualquier actividad) golpean con ESTA
        //  cuenta — el poder REAL del portador, no un porcentaje plano.
        // ================================================================
        public static int GolpeDeLaSombra(Player portador, NPC presa)
        {
            try
            {
                if (portador == null || !portador.active) return 1;
                // LA SUMA DE INVOCACIÓN: el daño de los minions VIVOS (los
                // SourceDamage de cada minion activo — la lectura real de
                // la casa) + el bono de invocación del portador.
                float invocacion = portador.maxMinions > 0
                    ? portador.slotsMinions * 0f : 0f;   // (los minions no dan daño plano: la escala real vive abajo)
                int minionsVivos = 0;
                float danoMinions = 0f;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile pr = Main.projectile[i];
                    if (pr == null || !pr.active || !pr.minion || pr.owner != portador.whoAmI)
                        continue;
                    minionsVivos++;
                    danoMinions += pr.damage;
                }
                invocacion = danoMinions;   // la suma TOTAL del daño de invocación
                // EL DAÑO MÁGICO TOTAL: GetTotalDamage respeta TODAS las
                // modifiers del portador (armadura, accesorios, buffs).
                float magico = portador.GetTotalDamage(DamageClass.Magic).ApplyTo(1f) - 1f;
                // (ApplyTo(1) devuelve el multiplicador aplicado: 1 → 0
                // base... la lectura correcta es el valor escalado con un
                // daño base de referencia del propio portador)
                float magicoTotal = 0f;
                try
                {
                    // el daño mágico del portador: su arma mágica más
                    // alta no existe — se usa EL ESCALADO REAL: el
                    // multiplicador mágico aplicado sobre el daño base
                    // de las armas mágicas del inventario.
                    foreach (Item it in portador.inventory)
                    {
                        if (it == null || it.IsAir || it.damage <= 0) continue;
                        if (it.DamageType != DamageClass.Magic) continue;
                        float d = portador.GetWeaponDamage(it);
                        if (d > magicoTotal) magicoTotal = d;
                    }
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SombraPaginaEterno", exRegistro); }
                if (magicoTotal <= 0f && magico > 0f)
                    magicoTotal = magico * 10f;   // sin armas mágicas: el multiplicador como base
                // LA FÓRMULA DE LA LETRA:
                // 1 + (invocación total + mágico total) − 5%·vidaMax + 1%·manáMax
                float golpe = 1f
                    + invocacion
                    + magicoTotal
                    - portador.statLifeMax2 * 0.05f
                    + portador.statManaMax2 * 0.01f;
                if (golpe < 1f) golpe = 1f;   // la sombra pica, aunque el portador sea gigante
                return (int)Math.Ceiling(golpe);
            }
            catch { return 1; }
        }

        /// <summary>LA SOMBRA VUELVE AL GRIMORIO (la sombra gastada se
        /// libera — el contador del alma respira).</summary>
        private void DevolverLaSombra()
        {            var alma = AlmaDelLibro();
            if (alma != null && alma.SombrasGastadas > 0)
            {
                alma.SombrasGastadas--;
                if (Main.netMode == NetmodeID.Server)
                {
                    // el alma del libro viaja con el item — mejor esfuerzo
                    // (el alma viaja con el item en su próximo sync —
                    // mejor esfuerzo de la casa, SP-first)
                }
            }
        }

        /// <summary>v6.161 — EL CIERRE SIN FUGAS (la letra: «el grimorio
        /// acaba de devorar a un jefe y sigue teniendo la sombra de la
        /// pagina vieja» — la autopsia: SOMBRAS QUE NO VOLVÍAN): TODO
        /// camino de muerte devuelve la sombra. El expiry de timeLeft
        /// (60 s mordiendo a un jefe testarudo) y cualquier Kill de
        /// vanilla pasaban por aquí SIN liberar el contador — cada uno
        /// era una sombra PERDIDA para siempre (con 7 fugadas: cap 10
        /// y solo 3 libres — el número exacto que reportaste). El Kill
        /// de vanilla llama AQUÍ primero: la sombra SIEMPRE vuelve.</summary>
        public override void OnKill(int timeLeft)
        {
            try { DevolverLaSombra(); }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SombraPaginaEterno", exRegistro); }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try { DrawTodo(); }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private void DrawTodo()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 23 + 9;
            NPC presa = Presa;

            Vector2 raiz = RaizDeSombra();

            // === LA COLUMNA VIVA (el hilillo del festín — como la del
            //     Nervioso, delgada: esta página es un hilo de hambre) ===
            Vector2[] col = SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempo, semilla, 16, 0.22f, gancho: 0.5f);

            // === LA MASA LISA Y NEGRA (la original v6.152: SIN ojos, SIN
            //     colmillos, SIN líneas rojas — tinta negra que fluye) ===
            SombrasLib.Masa(col, 6f, 10f, 0.90f, semilla, tiempo, grosorCentro: 30f, lisa: true);

            // === LA BRUMA NEGRA (más densa que el arma: la página EXHALA
            //     al apretar — el uso SE VE siempre) ===
            SombrasLib.BrumaColumna(col, 36f, 0.55f, tiempo, semilla, 12);
            SombrasLib.BrumaColumna(col, 24f, 0.42f, tiempo, semilla + 71, 8);

            // === LA PUNTA: la cabeza de bruma hacia la presa ===
            Vector2 punta = col[col.Length - 1];
            Vector2 rumbo = (col[col.Length - 1] - col[Math.Max(col.Length - 4, 0)])
                .SafeNormalize(Projectile.rotation.ToRotationVector2());
            float vigor = 0.85f + 0.15f * MathF.Sin(tiempo * 8f);
            SombrasLib.CabezaDeBruma(punta, rumbo, vigor, 1f, tiempo, semilla + 13);
            SombrasLib.Bruma(punta, 32f, 0.5f, tiempo, semilla + 31,
                new Vector2(MathF.Sin(tiempo * 0.9f) * 10f, -14f));

            // === LA MARCA DE LA COMIDA (el 1% apretando: la herida
            //     VIOLETA tenue en la presa — el uso se refleja SIEMPRE) ===
            if (presa != null && presa.active && presa.life > 0)
            {
                VFXCore.Begin();
                try
                {
                    Vector2 herida = presa.Center - Main.screenPosition;
                    float apriete = 0.30f + 0.18f * MathF.Sin(tiempo * 5f);
                    VFXCore.Quad(herida, SombrasLib.Alfa(new Color(118, 74, 190), apriete),
                        new Vector2(60f, 60f), 0f);
                }
                finally { VFXCore.FlushAdditive(); }
            }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SombraPaginaEterno", exRegistro); }
        }
    }
}
