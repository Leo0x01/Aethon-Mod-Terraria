using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// SOMBrapaginaPROJECTILE — v6.50.62 — ARMA 3: LA SOMBRA DE LA PÁGINA.
    /// v6.50.64 — LA BRUMA NEGRA: todo el cuerpo y la boca exhalan puﬀs
    /// vivos (BrumaColumna + BrumaBoca) — la petición del usuario.
    ///
    /// v6.50.68 — LA LETRA DEL USUARIO, DOS CIRUGÍAS (el resto INTACTO):
    /// (1) «la base del tentáculo nace en el suelo bajo el jugador y no
    /// sobre el jugador como debe ser» → LA RAÍZ ES EL CUERPO DEL
    /// PORTADOR (MountedCenter): el tentáculo SALE DEL JUGADOR esté donde
    /// esté — el charco queda como su SOMBRA proyectada en el suelo
    /// (mero decorado, ya no es el origen). (2) «la boca deja mucho que
    /// desear… la boca y el tentáculo deben ser uno solo… en vez de boca
    /// una masa de bruma negra cubre la totalidad de un jefe y nace del
    /// tentáculo hacia el jefe» → LAS FAUCES PROCEDURALES FUERON
    /// RETIRADAS: la punta se disuelve en LA CABEZA DE BRUMA (caza) y al
    /// morder EL DEVORADOR — la masa de bruma negra y espesa que cubre
    /// TODO el jefe, nace de la punta REAL de la columna y traga con
    /// pulso mientras las almas vuelven al portador por el cuerpo.
    ///
    /// «La misma arma, pero SOLO POR CÓDIGO»: cero sprites — ni uno.
    /// El tentáculo COMPLETO nace, caza, muerde y disipa dibujado con
    /// los pinceles del motor (Carne + Colmillo + Ventosa + BrumaFX vía
    /// la SombrasLib): la masa negra muscular, los ojos que se abren DE
    /// GOLPE y miran, el borde de energía que fluye y la bruma que come
    /// la luz.
    ///
    /// Mismo comportamiento que el ARMA 1 (emerge del jugador → caza al
    /// jefe a cualquier distancia → muerde y drena 2.5%/6t → a 1 HP el
    /// motor del festín, estilo 3).
    /// </summary>
    public class SombraPaginaProjectile : ModProjectile
    {
        private const byte FASE_EMERGER = 0;
        private const byte FASE_CAZA = 1;
        private const byte FASE_MORDISCO = 2;
        private const byte FASE_DISIPAR = 3;
        // v6.145 — LA FASE MUÑECA (la fusión de la Sombra de Tinta):
        // disipa: REBOTA como la Muñeca Maldita (5 rebotes, cada uno
        // ×1,12 más rápido) y LUEGO se disipa con el estilo de siempre.

        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 56;
            Projectile.height = 56;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
            Projectile.extraUpdates = 2;
        }

        /// <summary>
        /// v6.50.68 — LA RAÍZ ES EL JUGADOR (la letra del usuario: «la base
        /// del tentáculo nace en el suelo bajo el jugador y no sobre el
        /// jugador como debe ser»). El tentáculo SALE DEL CUERPO del
        /// portador — vuela, salta o camine: la base SIEMPRE está en él.
        /// </summary>
        private Vector2 RaizDeSombra(Player dueño)
        {
            // v6.165 — LA RAÍZ ES EL LIBRO EN CAZA (la letra: «cuando
            // el grimorio esta en fase de caza, todos los ataques
            // deben salir del grimorio pero no lo hacen, salen del
            // jugador»): si el Eterno tiene su ESPÍRITU FLOTANTE en
            // modo caza, la sombra nace DEL LIBRO (su centro vivo),
            // no del jugador. La búsqueda es barata (un pase sobre los
            // proyectiles, solo cuando la sombra RE-ENRAÍZA) y se
            // protege con try — si no hay libro fuera, la raíz de
            // siempre (el portador).
            try
            {
                int tipoFlotante = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Nervioso.GrimorioNerviosoFlotante>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || p.type != tipoFlotante ||
                        p.owner != Projectile.owner) continue;
                    if (p.ai[0] == global::AethonMod.Content.Projectiles.Nervioso
                        .GrimorioNerviosoFlotante.MODO_CAZA)
                        return p.Center;
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SombraPaginaProjectile", exRegistro); }
            return dueño.MountedCenter;
        }

        /// <summary>v6.170 — EL LIBRO DE LA RAÍZ (el contrato de la
        /// sombra vieja, ahora también aquí): si un ESPÍRITU FLOTANTE
        /// en modo caza sostiene el tipo de SU libro en ai[2], esa es
        /// la boca que come; sin raíz, EL GRIMORIO DEL ETERNO (la
        /// sombra nueva nace de SU caza).</summary>
        private int TipoLibroDeLaRaiz()
        {
            try
            {
                int tipoFlotante = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Nervioso.GrimorioNerviosoFlotante>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || p.type != tipoFlotante ||
                        p.owner != Projectile.owner) continue;
                    if (p.ai[2] > 0f) return (int)p.ai[2];
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SombraPaginaProjectile", exRegistro); }
            return ModContent.ItemType
                <global::AethonMod.Content.Weapons.GrimoireEternal>();
        }

        /// <summary>
        /// v6.50.68 — LA SOMBRA PROYECTADA del portador en el suelo (solo
        /// decorado: el charco de siempre, pero YA NO ES el origen del
        /// tentáculo — es la sombra que el portador echa al volar).
        /// </summary>
        private Vector2 SombraProyectada(Player dueño)
        {
            Vector2 c = dueño.MountedCenter + new Vector2(0, dueño.height * 0.45f);
            int tx = (int)(c.X / 16f), ty = (int)(c.Y / 16f);
            int pasos = 0;
            while (pasos < 34 && !WorldGen.SolidTile(tx, ty)) { ty++; pasos++; }
            Vector2 suelo = new Vector2(tx * 16f + 8f, ty * 16f - 4f);
            return pasos < 34 ? suelo : c;
        }

        /// <summary>v6.153 — LAS SOMBRAS CONCENTRADAS (la letra:
        /// «o aumentar el daño que le hace a un solo enemigo si usa
        /// todas las sombras de la pagina que posea en ese momento»):
        /// el disparo del arma original con el Eterno en el inventario
        /// GASTA todas las sombras libres del grimorio sobre SU presa —
        /// viaja en localAI[0] (≥1 = concentrada) y multiplica el
        /// drenaje ×(1+n). Las sombras SE VEN: n hilillos extra nacen
        /// del libro hacia la presa mientras dure la mordida.</summary>
        private int SombrasConcentradas => (int)Projectile.localAI[0];

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC presa = Presa;
            Player dueño = Main.player[Projectile.owner];
            byte fase = (byte)Projectile.ai[1];

            bool presaValida = presa != null && presa.active && presa.life > 0;
            if (fase != FASE_DISIPAR && !presaValida)
            {
                // v6.167 — LA CAZA NO SE RINDE (la letra: «el grimorio en
                // modo caza no esta cazando»): la CAUSA — la sombra
                // nacía, EMERGÍA (26 t), PERSIGUÍA… y mientras tanto el
                // jugador (o los discos del 931) mataban a la presa: la
                // sombra se disipaba SIN MORDER NUNCA. En la v6.157 el
                // hilillo viejo mordía PLANO al contacto — cazaba
                // siempre. LA CURA MODERNA: la sombra de CAZA (la de
                // localAI[1], nacida del flotante) NO se disipa —
                // RE-BUSCA la presa viva más cercana al dueño y sigue
                // cazando (el libro hambriento no se rinde). Las
                // sombras del ARMA (las concentradas) conservan su
                // comportamiento de siempre: su presa ES la que el
                // jugador señaló.
                bool esCaza = Projectile.localAI[1] >= 1f;
                if (esCaza && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC nueva = null;
                    float mejorD = 1250f * 1250f;
                    Vector2 desde = dueño != null && dueño.active ? dueño.Center : Projectile.Center;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC n = Main.npc[i];
                        if (n == null || !n.active || n.life <= 0 || n.friendly ||
                            n.townNPC || n.dontTakeDamage) continue;
                        float d = Vector2.DistanceSquared(n.Center, desde);
                        if (d < mejorD) { mejorD = d; nueva = n; }
                    }
                    if (nueva != null)
                    {
                        Projectile.ai[0] = nueva.whoAmI + 1;
                        presa = nueva;
                        presaValida = true;
                        if (fase == FASE_EMERGER && t > 8)
                        {
                            // ya emergió lo suficiente: directo a CAZA
                            Projectile.ai[1] = FASE_CAZA;
                            fase = FASE_CAZA;
                            Projectile.ai[2] = 0;
                            t = 0;
                        }
                    }
                    else
                    {
                        Projectile.ai[1] = FASE_DISIPAR;
                        Projectile.ai[2] = 0;
                        fase = FASE_DISIPAR;
                        t = 0;
                    }
                }
                else
                {
                    Projectile.ai[1] = FASE_DISIPAR;
                    Projectile.ai[2] = 0;
                    fase = FASE_DISIPAR;
                    t = 0;
                }
            }

            switch (fase)
            {
                case FASE_EMERGER:
                {
                    Vector2 raiz = RaizDeSombra(dueño);
                    Vector2 rumbo = presa != null
                        ? (presa.Center - raiz).SafeNormalize(-Vector2.UnitY)
                        : -Vector2.UnitY;
                    float avance = SombrasLib.DeGolpe(t / 26f);
                    Projectile.Center = raiz + rumbo * (30f + 170f * avance);
                    Projectile.rotation = rumbo.ToRotation();
                    if (t == 3) Sonar(SoundID.Item122.WithPitchOffset(-0.45f).WithVolumeScale(0.75f), Projectile.Center);
                    if (t >= 26) { Projectile.ai[1] = FASE_CAZA; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_CAZA:
                {
                    if (presa != null && presaValida)
                    {
                        Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float dist = Vector2.Distance(presa.Center, Projectile.Center);
                        float vel = MathHelper.Clamp(dist * 0.05f, 20f, 52f);
                        float actual = Projectile.velocity.ToRotation();
                        float deseado = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - actual);
                        Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.26f, 0.26f)).ToRotationVector2() * vel;
                        Projectile.rotation = deseado;

                        // v6.50.69 — LA MARCA CONTINUA (la letra: «a veces La
                        // Sombra de la Página no se activa y no hace el acto
                        // de devoración con los jefes»): mientras el tentáculo
                        // TIENE UN JEFE como presa, la marca se REFRESCA — si
                        // OTRA cosa (esbirro, otro jugador, un DoT) le inflige
                        // el golpe mortal, la muerte SIGE SIENDO NUESTRA y el
                        // festín arranca igual. Antes la marca solo vivía en el
                        // golpe mortal NUESTRO: si otro lo mataba, el jefe se
                        // moría «normal» y el usuario veía que NO se activaba
                        if (t % 15f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_MORDISCO;
                            Projectile.ai[2] = 0;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.35f).WithVolumeScale(0.9f), Projectile.Center);
                        }
                    }
                    if (t > 600) { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_MORDISCO:
                {
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        // v6.50.110 — SI EL FESTÍN DE LAS FAUCES TOMÓ LA
                        // PRESA (LA PRIMERA CENA del Eterno o la devoración
                        // clásica): SOLTARLA — ese festín la termina con SU
                        // cine y SU loot (el EnFauces cubre también al
                        // dueño del pool de un worm)
                        if (FaucesGlobalNPC.EnFauces(presa))
                        {
                            Projectile.ai[1] = FASE_DISIPAR;
                            Projectile.ai[2] = 0;
                            break;
                        }
                        Projectile.Center = presa.Center;
                        Projectile.rotation = (presa.Center - RaizDeSombra(dueño)).ToRotation();

                        // v6.50.69 — LA MARCA CONTINUA mientras MUERDE (cada
                        // 5 t): el golpe mortal de CUALQUIER fuente durante la
                        // mordida también dispara la devoración
                        if (t % 5f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

                        if (Main.netMode != NetmodeID.MultiplayerClient && t > 18 && t % 6 == 0)
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 1)
                            {
                                // v6.160 — LA FÓRMULA DE LAS SOMBRAS DEL
                                // ETERNO (la letra: «por ultima vez
                                // asegurate de que todas las sombras de
                                // las paginas que use el grimorio del
                                // eterno en cualquier actividad sea la
                                // nueva»): el drenaje de la mordida es
                                // LA CUENTA NUEVA (GolpeDeLaSombra — 1 +
                                // invocación total + mágico total − 5%
                                // vidaMax + 1% manáMax) × (1 + sombras
                                // concentradas — la concentración de la
                                // v6.153 sigue apretando CON la mordida).
                                float base_ = global::AethonMod.Content.Projectiles
                                    .Nervioso.SombraPaginaEterno.GolpeDeLaSombra(
                                        dueño, dueñoPool);
                                float quitar = MathF.Max(base_, 25f)
                                    * (1f + SombrasConcentradas);
                                if (dueñoPool.life - quitar <= 1f)
                                {
                                    // v6.167 — EL JEFE CENA, LA CHUSMA MUERE:
                                    // el festín cinematográfico es de JEFES
                                    // (Iniciar hace SU guard); la chusma que
                                    // llega al final de la mordida recibe EL
                                    // GOLPE MORTAL de una vez (checkDead) y
                                    // suelta SU absorbida de siempre — nada
                                    // de zombis congelados a 1 HP.
                                    if (FaucesGlobalNPC.EsJefe(dueñoPool) ||
                                        FaucesGlobalNPC.EsSegmentoDeGusano(dueñoPool))
                                        FaucesGlobalNPC.Iniciar(dueñoPool, 3);
                                    else
                                    {
                                        // v6.170 — LA CAZA ALIMENTA (la
                                        // letra: «el grimorio deberia
                                        // perder porcentajes de hambre
                                        // mientras caza […] solo
                                        // investiga porque el grimorio
                                        // del eterno no baja su hambre
                                        // al cazar»): LA CAUSA — este
                                        // camino mataba la chusma con
                                        // checkDead() SIN pasar por la
                                        // absorción: la sombra VIEJA
                                        // (SombraPaginaCaza) llamaba
                                        // AbsorberPresa (la que baja el
                                        // hambre) y la NUEVA no — desde
                                        // la v6.161 la caza del Eterno
                                        // comía SIN mover el medidor.
                                        // LA CURA: el golpe mortal de
                                        // la chusma pasa por LA
                                        // ABSORCIÓN DEL LIBRO (come:
                                        // baja hambre — o la ENGRE si
                                        // es de estatua, +0,1% — y
                                        // cobra SU XP; el festín de los
                                        // jefes sigue en las fauces).
                                        bool deEstatua = false;
                                        try { deEstatua = dueñoPool.SpawnedFromStatue; } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SombraPaginaProjectile", exRegistro); }
                                        if (!deEstatua)
                                        {
                                            global::AethonMod.Content.Weapons
                                                .GrimorioHambrientoNervioso.AbsorberPresa(
                                                    dueñoPool, dueño, TipoLibroDeLaRaiz());
                                        }
                                        else
                                        {
                                            dueñoPool.life = 0;
                                            dueñoPool.checkDead();
                                            dueñoPool.netUpdate = true;
                                        }
                                    }
                                    Projectile.ai[1] = FASE_DISIPAR;
                                    Projectile.ai[2] = 0;
                                    break;
                                }
                                dueñoPool.life -= (int)quitar;
                                dueñoPool.netUpdate = true;
                                if (t % 36 == 0)
                                    Sonar(SoundID.NPCHit9.WithPitchOffset(-0.1f).WithVolumeScale(0.5f), Projectile.Center);
                            }
                        }
                    }
                    else { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_DISIPAR:
                {
                    if (t == 1) Sonar(SoundID.Item122.WithPitchOffset(0.2f).WithVolumeScale(0.3f), Projectile.Center);
                    Projectile.velocity *= 0.92f;
                    if (t >= 44) Projectile.Kill();
                    break;
                }
            }

            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] <= FASE_CAZA && Main.rand.NextBool(6))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(24, 24), 48, 48,
                    DustID.Shadowflame, 0f, -0.3f, 128, default, 0.55f);
                Main.dust[d].noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[1] != FASE_CAZA) return base.Colliding(projHitbox, targetHitbox);
            Player dueño = Main.player[Projectile.owner];
            Vector2 raiz = RaizDeSombra(dueño);
            Vector2[] col = SombrasLib.Columna(raiz, Projectile.Center,
                Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 17 + 5, 10, 0.24f);
            float punto = 0f;
            for (int i = 1; i < col.Length; i++)
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    col[i - 1], col[i], 8, ref punto))
                    return true;
            return false;
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

        /// <summary>v6.161 — EL CIERRE SIN FUGAS (la autopsia de las
        /// sombras que no volvían): DOS gatos, una bolsa. (1) Si ESTA
        /// sombra nació de la CAZA del Eterno (localAI[1] = 1 — el
        /// flotante ya gastó SU contador al spawnedla), su muerte
        /// devuelve ESA sombra al alma del libro. (2) Si fue el
        /// disparo del ARMA con sombras CONCENTRADAS (localAI[0] > 0 —
        /// el gasto de la v6.153), TODAS las concentradas vuelven
        /// también. La fuga masiva dejó libros con cap 10 y solo 3
        /// libres PARA SIEMPRE — nunca más.</summary>
        public override void OnKill(int timeLeft)
        {
            try
            {
                bool deCaza = Projectile.localAI[1] >= 1f;
                int n = SombrasConcentradas;
                if (!deCaza && n <= 0) return;
                Player dueño = Main.player[Projectile.owner];
                Item libro = global::AethonMod.Content.Globals
                    .AlmaEternoGlobalItem.Activo(dueño);
                var alma = global::AethonMod.Content.Globals
                    .AlmaEternoGlobalItem.De(libro);
                if (alma != null)
                {
                    int liberar = n + (deCaza ? 1 : 0);
                    alma.SombrasGastadas = System.Math.Max(0,
                        alma.SombrasGastadas - liberar);
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SombraPaginaProjectile", exRegistro); }
        }

        private void DrawTodo()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 17 + 5;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];

            Vector2 raiz = RaizDeSombra(dueño);

            // === EL CHARCO — v6.50.68: ya NO es la raíz (el tentáculo nace
            // DEL JUGADOR): es la SOMBRA que el portador proyecta al suelo —
            // sutil, viva, pero decorado ===
            SombrasLib.Charco(SombraProyectada(dueño), 44f, 0.55f, tiempo, semilla);

            // === LA COLUMNA y LA MASA ===
            // v6.50.67 — LA COLUMNA VIVA: física de verlet con inercia y
            // ONDA VIAJERA (la letra del GIF del usuario) + el GANCHO de
            // la punta — la base también se hace FLUIDA
            // v6.154 — LA MANADA: cada sombra lleva SU PROPIA FASE —
            // el tiempo propio de ESTA instancia (whoAmI como semilla de
            // nacimiento + una deriva lenta): dos sombras del mismo
            // lanzamiento NUNCA ondean a la vez (la letra: «que todas
            // se muevan en momentos diferentes como una manada de
            // tentaculos»).
            float tiempoPropio = tiempo + (Projectile.whoAmI % 97) * 0.83f
                + MathF.Sin(tiempo * 0.31f + Projectile.whoAmI) * 0.45f;
            Vector2[] col = SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempoPropio, semilla, 20, 0.24f, gancho: 0.5f);
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 44f, 0f, 1f) : 1f;
            // v6.50.71 — EL PERFIL FUSIFORME (la letra del usuario): la base
            // pegada al jugador MUY FINA (9 px) → se ENGORDA al centro (la
            // panza de 44 px) → vuelve a ser FINA en la punta que toca al
            // enemigo (13 px) — antes era un bíceps de 40 px naciendo del
            // cuerpo: un garrote, no un látigo
            // v6.152 — LA SOMBRA ORIGINAL TAMBIÉN ES LISA (la letra:
            //     «quita los ojo y colmillos, tambien vi que tiene
            //     lineas rojas, quitalas tambien dejando un tentaculo
            //     negro completo»): la Masa en modo liso (sin espinas
            //     de hueso, sin ventosas) — tinta negra que fluye.
            SombrasLib.Masa(col, 9f, 13f, 0.95f * disipa, semilla, tiempo,
                grosorCentro: 44f, lisa: true);

            // === LA BRUMA NEGRA DEL CUERPO (v6.50.64): el tentáculo
            // 100% código también EXHALA — puﬀs vivos a lo largo ===
            // v6.50.69 — MÁS BRUMA (la letra: «la cantidad de bruma en el
            // tentáculo no es suficiente, necesita más»): DOS CAPAS (16
            // puffs gruesos + 12 finos desfasados — antes una sola de 10)
            // y cada puff nace más gordo y crece más (BrumaColumna v2:
            // ×1.6 de crecimiento, 7 halos de aliento)
            SombrasLib.BrumaColumna(col, 46f, 0.62f * disipa, tiempo, semilla, 16);
            SombrasLib.BrumaColumna(col, 30f, 0.46f * disipa, tiempo, semilla + 97, 12);

            // === LOS OJOS (más ojos que el arma 1: la página es TODA ojos) ===
            // v6.146 — LA TINTA NO MIRA (la letra: «al arma quitale los
            // colmillos y ojos»): el MODO TINTA (ai[0]==1 — la Sombra de
            // Tinta) NO dibuja ojos ni boca — SOLO la masa lisa y la
            // bruma, MÁS DENSA (×1.35): la tinta es ciega, va a lo suyo.
            // (La boca procedural ya murió en v6.50.68 — solo quedaban
            // estos ojos.)
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            float presencia = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 26f) : 1f;
            // v6.152 — LOS OJOS MURIERON EN LA SOMBRA ORIGINAL (la
            //     letra: «quita los ojo y colmillos… dejando un
            //     tentaculo negro completo»): ya NO hay ojos en NINGÚN
            //     modo — la sombra es TINTA CIEGA de punta a punta.
            //     En su lugar: la bruma densa que respiraba la tinta
            //     (ahora es de TODOS los modos — la página exhala).
            SombrasLib.BrumaColumna(col, 40f, 0.50f * disipa, tiempo, semilla + 41, 10);

            // === v6.50.68 — LA CABEZA ES BRUMA (la boca procedural RETIRADA
            // por la letra del usuario): TODO lo que era boca ahora vive EN LA
            // PUNTA REAL de la columna — cabeza y tentáculo son EL MISMO
            // CUERPO por construcción, jamás vuelve a haber separación ===
            Vector2 punta = col[col.Length - 1];
            Vector2 rumbo = (col[col.Length - 1] - col[Math.Max(col.Length - 4, 0)])
                .SafeNormalize(Projectile.rotation.ToRotationVector2());

            // v6.153 — LAS SOMBRAS CONCENTRADAS SE VEN (la letra: «que
            // se vea reflejado siempre que la use»): cada sombra gastada
            // sobre ESTA presa es UN HILO NEGRO más apretándola — nacen
            // del libro, orbitan la presa y aprietan al ritmo del
            // drenaje. El festín concentrado ES visible desde el cielo.
            int sombras = (int)Projectile.localAI[0];
            if (sombras > 0 && presa != null && presa.active && presa.life > 0)
            {
                for (int s = 0; s < sombras; s++)
                {
                    // v6.154 — CADA HILO, SU MOMENTO (la manada): fase
                    // propia por hilo — el tiempo de cada uno nace
                    // desfasado (s·0.7 + la identidad de la instancia):
                    // NO es una sola onda, son TANTAS como sombras, cada
                    // una apretando en SU instante.
                    float tS = tiempo + s * 0.7f + (Projectile.whoAmI % 53) * 0.37f
                        + MathF.Sin(tiempo * 0.5f + s * 2.3f) * 0.3f;
                    float angS = tS * (0.8f + 0.12f * (s % 3)) + s * MathHelper.TwoPi / Math.Max(1, sombras);
                    float radS = 46f + 12f * MathF.Sin(tS * 3f + s * 1.7f);
                    Vector2 anclaS = presa.Center + angS.ToRotationVector2() * radS;
                    Vector2[] colS = SombrasLib.ColumnaViva(raiz, anclaS, tS,
                        semilla + s * 37, 12, 0.18f, gancho: 0.6f);
                    SombrasLib.Masa(colS, 5f, 8f, 0.55f, semilla + s * 37, tS,
                        grosorCentro: 18f, lisa: true);
                }
            }

            if (fase == FASE_MORDISCO && presa != null && presa.active)
            {
                // EL DEVORADOR — la masa de bruma negra y espesa que CUBRE
                // LA TOTALIDAD del jefe, NACE del tentáculo y TRAGA con
                // pulso (las almas vuelven por el cuerpo)
                float hambre = MathHelper.Clamp(t / 26f, 0f, 1f);
                SombrasLib.Devorador(punta, presa, hambre, disipa, tiempo, semilla, col);

                // v6.152 — LA HERIDA ROJA MURIÓ (la letra: «tambien vi
                //     que tiene lineas rojas, quitalas tambien dejando
                //     un tentaculo negro completo»): la digestión ya no
                //     ARDE en rojo — la punta bruma NEGRA sobre el
                //     devorado (la cena es discreta).
                SombrasLib.CabezaDeBruma(punta, rumbo, 0.85f * disipa, disipa, tiempo, semilla + 13);
            }
            else
            {
                // LA CABEZA DE BRUMA — la punta disuelta en humo que respira
                // hacia la presa (el capullo al emerger, la nariz al cazar)
                float vigor = fase switch
                {
                    FASE_EMERGER => 0.35f + 0.55f * SombrasLib.DeGolpe(t / 26f),
                    FASE_CAZA => 0.85f + 0.15f * MathF.Sin(tiempo * 8f),
                    _ => 0.5f * disipa,
                };
                SombrasLib.CabezaDeBruma(punta, rumbo, vigor * disipa, disipa, tiempo, semilla);

                // v6.50.71 — MÁS BRUMA JUSTO EN LA PUNTA (la letra del
                // usuario): la punta que toca al enemigo exhala SU PROPIA
                // nube — más densa que la del cuerpo (el punto de
                // contacto HUMEA: es donde la página MUERDE)
                SombrasLib.Bruma(punta, 40f, 0.5f * disipa, tiempo, semilla + 31,
                    new Vector2(MathF.Sin(tiempo * 0.9f) * 10f, -14f));
            }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SombraPaginaProjectile", exRegistro); }
        }
    }
}
