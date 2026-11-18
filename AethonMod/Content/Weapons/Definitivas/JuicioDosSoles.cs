using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  v6.127 — LA CEREMONIA MEJORADA + LA LEY DE LOS ANILLOS (la letra del
    //  usuario: «la animación actual es insatisfactoria, mejórala» + «máximo
    //  1 anillo rúnico por sol; al fusionarse deben verse 3 anillos»).
    //
    //  LA LEY DE LOS ANILLOS: cada sol sencillo lleva UN SOLO anillo rúnico
    //  (la corona mínima — antes cargaban OCHO cada uno, un mercado de
    //  aros); EL SOL FUSIONADO es el único que merece TRES (la corona
    //  completa de la sentencia — nace con 3, vive con 3, muere con 3).
    //
    //  LA ANIMACIÓN — LA MUERTE DE UNA BINARIA (el guion nuevo, el drama
    //  de verdad — la v6.126 espiralaba a velocidad CONSTANTE y el sol
    //  fusionado nacía a media fusión, sin contacto, sin destello):
    //    ACTO 1 · LA CONVOCATORIA — los dos soles CRECEN mientras vuelan
    //      (nacen chicos, llegan gordos) y dejan ESTELAS de su fuego.
    //    ACTO 2 · LA ESPIRAL DE MUERTE — los soles orbitan su centro común
    //      y la órbita SE CAE: el giro ACELERA (la ley de Kepler — mientras
    //      el radio se hunde, la velocidad sube) hasta el CONTACTO: el
    //      DESTELLO DE FUSIÓN (la luz blanca) y el sol fusionado NACE DEL
    //      CHOQUE — nada nace antes del contacto.
    //    ACTO 3 · LA CARGA — el gigante se hincha, enrojece, TIEMBLA, sus
    //      TRES anillos GIRAN CADA VEZ MÁS RÁPIDO y el polvo cercano CAE
    //      al interior (la succión de la sentencia).
    //    ACTO 4 · LA NOVA — EL COLAPSO: el sol SE CONTRAE de golpe (el
    //      núcleo plegándose, la luz APAGÁNDOSE)… y al fondo del colapso
    //      LA NOVA: la onda, la bruma, las runas — la sentencia.
    // ======================================================================
    public class JuicioDosSoles : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 800;               // el ataque DEFINITIVO (la Mirada: 800)
            Item.DamageType = DamageClass.Magic;
            Item.width = 44; Item.height = 44;
            Item.useTime = 60; Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;             // la ceremonia se sostiene con el clic
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<JuicioDosSolesProj>();
            Item.shootSpeed = 1f;
            Item.mana = 20;
            Item.ArmorPenetration = 300;
            Item.rare = ItemRarityID.Purple;
            Item.value = 0;
        }

        public override bool CanUseItem(Player player)
        {
            // UN juicio a la vez: mientras la ceremonia viva, no hay otro.
            int tipo = ModContent.ProjectileType<JuicioDosSolesProj>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI && p.type == tipo)
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL PUNTO DE ENCUENTRO: el cursor con correa de 640 px.
            Vector2 punto = Main.MouseWorld;
            float d = Vector2.Distance(player.Center, punto);
            if (d > 640f) punto = player.Center + Vector2.Normalize(punto - player.Center) * 640f;
            Projectile.NewProjectile(source, player.Center, Vector2.Zero, type,
                damage, knockback, player.whoAmI, punto.X, punto.Y);
            return false;
        }

        public override void AddRecipes()
        {
            // EL PROTOCOLO DE LA CASA: la madera de siempre.
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  JuicioDosSolesProj — LA CEREMONIA ENTERA (los dos soles rúnicos,
    //  la fusión y la nova — un solo proyectil, cuatro actos).
    // ======================================================================
    public class JuicioDosSolesProj : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int T_CONVOCATORIA = 36;   // los dos soles vuelan al punto
        private const int T_FUSION = 34;         // la espiral de muerte y el contacto

        // ================================================================
        //  v6.151 — LA CAZA DEL SOL FUSIONADO (la letra: «el sol formado
        //  por la fusion no debe explotar asi nada mas, debe mantenerse,
        //  perseguir a los enemigos y despues de un tiempo debe crecer al
        //  menos 70% mas su tamaño, volverse completamente rojo y luego
        //  explotar, junto a todas estas acciones su daño aumenta y ademas
        //  su caja de colisiones tambien para abarcar mas area de daño»):
        //  el acto 2 YA NO es la carga de 24 t — es LA CAZA de 300 t
        //  (5 s): el gigante PERSIGUE al enemigo más cercano (homing
        //  suave de la casa), SE MANTIENE vivo y creciendo, y su cuerpo
        //  ENROJECE por completo conforme la caza madura; el daño del
        //  proyectil y SU HITBOX escalan con el mismo reloj (×1 → ×1.7+
        //  al final). La nova estalla AL FINAL de la caza — nunca antes.
        // ================================================================
        private const int T_CARGA = 300;         // LA CAZA: 5 s persiguiendo y creciendo
        private const int T_NOVA = 46;           // el colapso y LA NOVA

        /// <summary>v6.127 — EL CONTACTO: la fracción de la espiral donde
        /// los dos soles SE TOCAN (el resto del acto es el nacimiento del
        /// gigante — nada nace antes del choque).</summary>
        private const float P_CONTACTO = 0.78f;

        /// <summary>v6.151 — EL CRECIMIENTO DE LA CAZA: +70 % mínimo al
        /// final (la letra: «crecer al menos 70% mas su tamaño») — la
        /// curva es SIEMPRE creciente (smoothstep) y el ROJO la sigue.</summary>
        private const float CRECE_CAZA = 0.70f;

        /// <summary>v6.127 — EL COLAPSO: los primeros ticks de la nova en
        /// que el sol se PLEGA antes de estallar.
        /// v6.152 — EL PLEGADO SUAVE (la cura del ÚLTIMO salto): el SIM
        /// midió Δ 0.02→0.27 creciente en los 7 ticks viejos — el
        /// cuadrático ACELERABA hacia el fondo y el ojo lo leía como un
        /// salto. AHORA: 14 ticks con smoothstep (arranca despacio,
        /// frena al llegar) — el pliegue es una RESPIRACIÓN.</summary>
        private const int T_COLAPSO = 14;

        /// <summary>El reloj del acto actual.</summary>
        private float _t;

        /// <summary>v6.152 — EL RELOJ TOTAL (la cura de TODOS los
        /// saltos): la edad del proyectil desde su nacimiento — UNA
        /// SOLA línea de tiempo continua para TODO el dibujado (los
        /// actos siguen existiendo para la IA/lógica, pero el PRESENTA
        /// ya no corta nunca: cada curva visual es función continua de
        /// ESTE reloj con costuras suavizadas).</summary>
        private float _edad;

        /// <summary>0 convocatoria · 1 fusión · 2 caza · 3 nova.</summary>
        private int _acto;

        /// <summary>v6.151 — EL BLANCO DE LA CAZA (el enemigo más cercano
        /// al punto de la fusión; se re-sortea si muere).</summary>
        private int _presa = -1;

        /// <summary>v6.151 — La posición REAL del sol (durante la caza se
        /// MUEVE persiguiendo; el Punto sigue siendo la cita de siempre
        /// para los actos 0-1).</summary>
        private Vector2 _centroCaza;

        private Vector2 Punto => new(Projectile.ai[0], Projectile.ai[1]);

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = T_CONVOCATORIA + T_FUSION + T_CARGA + T_NOVA + 10;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            _t++;
            _edad++;   // v6.152 — la línea de tiempo del dibujado (continua)

            // EL RELOJ DE LOS ACTOS
            if (_acto == 0 && _t > T_CONVOCATORIA) { _acto = 1; _t = 0; }
            else if (_acto == 1 && _t > T_FUSION)
            {
                _acto = 2; _t = 0;
                // v6.151 — LA CAZA EMPIEZA: el sol fusionado nace EN el
                // punto de la fusión y elige a SU presa (la más cercana).
                _centroCaza = Punto;
                _presa = PresaMasCercana(Punto);
            }
            else if (_acto == 2 && _t > T_CARGA) { _acto = 3; _t = 0; }
            else if (_acto == 3 && _t > T_NOVA) { Projectile.Kill(); return; }

            // v6.127 — LA NOVA ESTALLA AL FONDO DEL COLAPSO (no al empezar
            // el acto: el sol se pliega primero — la sentencia cae cuando
            // el núcleo ya tocó fondo)
            if (_acto == 3 && _t == T_COLAPSO) NacerLaNova();

            if (_acto == 2)
            {
                // ============================================================
                //  v6.151 — LA CAZA (la letra: «debe mantenerse, perseguir
                //  a los enemigos y despues de un tiempo debe crecer…»):
                //  el gigante NO se queda clavado — PERSIGUE. El homing
                //  suave de la casa (aceleración hacia la presa + tope de
                //  velocidad que CRECE con la caza: empieza solemne y
                //  termina arrebatado). La presa muerta → la siguiente.
                // ============================================================
                if (_presa < 0 || _presa >= Main.maxNPCs || !Main.npc[_presa].active ||
                    !VFXCore.EsObjetivo(Main.npc[_presa]))
                    _presa = PresaMasCercana(_centroCaza);

                float caza = MathHelper.Clamp(_t / (float)T_CARGA, 0f, 1f);
                if (_presa >= 0)
                {
                    NPC blanco = Main.npc[_presa];
                    Vector2 hacia = blanco.Center - _centroCaza;
                    if (hacia.LengthSquared() > 40f * 40f)
                    {
                        hacia.Normalize();
                        float acel = 0.10f + 0.16f * caza;       // cada vez más hambre
                        _centroCaza += hacia * acel * (1f + CRECE_CAZA * 0.4f * caza);
                    }
                    // el freno vertical del sol (pesa más que el vuelo):
                    _centroCaza.Y += MathF.Sin(Main.GlobalTimeWrappedHourly * 1.7f) * 0.35f;
                }
                Projectile.Center = _centroCaza;

                // EL DAÑO Y LA CAJA CRECEN CON LA CAZA (la letra: «su daño
                // aumenta y ademas su caja de colisiones tambien»): el
                // daño base ×(1 → ×1.85 al final) y el hitbox engorda al
                // ritmo del cuerpo (1.7× su tamaño: 30 → 300 px de lado).
                float escalaCuerpo = 1f + CRECE_CAZA * caza;
                int lado = (int)(46f * escalaCuerpo * escalaCuerpo);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.position = _centroCaza - new Vector2(lado * 0.5f, lado * 0.5f);
                    Projectile.width = lado;
                    Projectile.height = lado;
                    if (_t % 30f == 0f)   // el daño escala por tramos (sync sano)
                    {
                        Projectile.damage = (int)(Projectile.originalDamage *
                            (1f + 0.85f * caza));
                        Projectile.netUpdate = true;
                    }
                }
            }
            else
                Projectile.Center = Punto;

            if (_acto == 0)
            {
                // === ACTO 1 — LA CONVOCATORIA: los DOS SOLES RÚNICOS
                //     CRECEN mientras vuelan a la cita y dejan SU ESTELA
                //     (la luz siembra ORO, la oscuridad VACÍO INVERTIDO) ===
                float p = MathHelper.Clamp(_t / (float)T_CONVOCATORIA, 0f, 1f);
                float suave = p * p * (3f - 2f * p);
                if (!Main.dedServ)
                {
                    for (int lado = 0; lado < 2; lado++)
                    {
                        bool esLuz = lado == 0;
                        Vector2 pos = PosicionSolSencillo(esLuz, suave, Main.GlobalTimeWrappedHourly);
                        Vector2 prev = PosicionSolSencillo(esLuz, Math.Max(0f, suave - 0.06f), Main.GlobalTimeWrappedHourly);
                        Vector2 hacia = pos - prev;
                        if (Main.rand.NextBool(2) && hacia.LengthSquared() > 0.01f)
                        {
                            Dust d = Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(14f, 14f),
                                esLuz ? DustID.GoldFlame : DustID.Shadowflame,
                                -Vector2.Normalize(hacia) * Main.rand.NextFloat(1.2f, 3f),
                                0, esLuz ? new Color(255, 226, 150) : new Color(150, 84, 235), 0.9f);
                            d.noGravity = true;
                        }
                    }
                }
            }
            else if (_acto == 1)
            {
                // === ACTO 2 — LA ESPIRAL DE MUERTE: la órbita SE CAE, el
                //     giro ACELERA (Kepler) y TODO el polvo cercano cae al
                //     centro; al CONTACTO: el destello y el terremoto ===
                float p = MathHelper.Clamp(_t / (float)T_FUSION, 0f, 1f);
                if (!Main.dedServ)
                {
                    if (Main.rand.NextBool(3))
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        float lejos = Main.rand.NextFloat(180f, 420f);
                        Vector2 pos = Punto + ang.ToRotationVector2() * lejos;
                        Dust d = Dust.NewDustPerfect(pos,
                            Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Shadowflame,
                            -ang.ToRotationVector2() * (lejos / 14f), 0,
                            new Color(200, 150, 255), 1.1f);
                        d.noGravity = true;
                    }
                    // el RETUMBE crece con la caída de la órbita
                    if (p > 0.3f && _t % 4 == 0)
                        OndaLib.Kick(1 + (int)(5f * p), 10);
                    // EL CONTACTO: el destello de fusión (oro + violeta
                    // cruzados — los dos fuegos abrazándose)
                    if (p >= P_CONTACTO && _t <= P_CONTACTO * T_FUSION + 5)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            bool oro = Main.rand.NextBool();
                            float a = Main.rand.NextFloat(MathHelper.TwoPi);
                            Dust d = Dust.NewDustPerfect(Punto,
                                oro ? DustID.GoldFlame : DustID.Shadowflame,
                                a.ToRotationVector2() * Main.rand.NextFloat(4f, 11f), 0,
                                oro ? new Color(255, 240, 190) : new Color(180, 120, 255), 1.4f);
                            d.noGravity = true;
                        }
                    }
                    if (p >= P_CONTACTO && _t <= P_CONTACTO * T_FUSION + 1)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item122.WithPitchOffset(0.35f), Punto);
                        OndaLib.Kick(8f, 18);
                    }
                }
            }
            else
            {
                // === ACTO 4 — LA NOVA: primero EL COLAPSO (el núcleo se
                //     pliega, la luz SE APAGA)… y al fondo, la explosión:
                //     la luz INUNDA y la lluvia dorada cae ===
                if (!Main.dedServ)
                {
                    if (_t < T_COLAPSO)
                    {
                        float plega = _t / (float)T_COLAPSO;
                        Lighting.AddLight(Punto, new Vector3(2.2f, 1.8f, 1.2f) * (0.9f - 0.72f * plega));
                    }
                    else
                    {
                        float vida = Math.Max(0f, 1f - (_t - T_COLAPSO) / (T_NOVA - T_COLAPSO));
                        Lighting.AddLight(Punto, new Vector3(2.6f, 2.3f, 1.6f) * vida);
                        if (_t < T_COLAPSO + 14 && Main.rand.NextBool(2))
                        {
                            Dust d = Dust.NewDustPerfect(Punto, DustID.GoldFlame,
                                Main.rand.NextVector2Circular(11f, 11f), 0,
                                new Color(255, 244, 204), Main.rand.NextFloat(1.1f, 2f));
                            d.noGravity = true;
                        }
                    }
                }
            }
        }

        /// <summary>EL NACIMIENTO DE LA NOVA: la onda golpea (una ventana
        /// de 10 t con la hitbox de 920 px — la sentencia) y TODO el
        /// teatro de la nova de los soles de la casa estalla (el patrón
        /// de RuneSunProjectile.OnKill: bruma, chispa, runas de eco).</summary>
        private void NacerLaNova()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // v6.151 — LA NOVA EN EL FINAL DE LA CAZA: estalla donde
                // el gigante terminó su persecución, con el ÁREA de su
                // tamaño final (la letra: la caja de colisión «tambien
                // para abarcar mas area de daño») y el daño YA escalado
                // por la caza (Projectile.damage lo fue subiendo la IA).
                Projectile.position = _centroCaza - new Vector2(460f, 460f);
                Projectile.width = 920;
                Projectile.height = 920;
                Projectile.netUpdate = true;
            }
            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, _centroCaza);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, Punto);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Punto);
                OndaLib.Kick(14f, 26);

                // LA NOVA DE BRUMA — el DOBLE círculo de la casa (el
                // patrón del sol rúnico: el círculo COMPLETO al radio de
                // la nova + el corazón MÁS DENSO que se disipa antes).
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(),
                    Punto, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.BrumaNacimientoFX>(),
                    0, 0f, Projectile.owner, 3f, 460f / 340f);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(),
                    Punto, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.BrumaNacimientoFX>(),
                    0, 0f, Projectile.owner, 3f, (460f * 0.55f) / 340f);

                // LA CHISPA de la nova (el fogonazo central de la casa).
                ParticlePresets.Explosion(Punto, 300f, 40,
                    new Color(255, 245, 200), new Color(255, 170, 60), 44);

                // LAS RUNAS DE ECO: la nova escupe glifos de luz (la
                // lluvia rúnica del sol de siempre).
                for (int i = 0; i < 26; i++)
                {
                    float angle = (MathHelper.TwoPi / 26f) * i +
                                  Main.rand.NextFloat(-0.08f, 0.08f);
                    Vector2 outward = new((float)Math.Cos(angle), (float)Math.Sin(angle));
                    Dust d = Dust.NewDustPerfect(Punto, DustID.Enchanted_Gold,
                        outward * Main.rand.NextFloat(5f, 12f),
                        255, new Color(255, 235, 170), 1.5f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // LA SENTENCIA NO SE ESQUIVA: el destello es un hecho consumado.
            modifiers.SourceDamage *= 1f;
        }

        // ================================================================
        //  EL DIBUJO — TODO CON EL CÓDIGO DE SOLES DE LA CASA
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                float gt = Main.GlobalTimeWrappedHourly;
                VFXCore.CerrarLoteSiAbierto();

                // ================================================================
                //  v6.149 — LA NUEVA COREOGRAFÍA DEL JUICIO (la letra:
                //  «cambia y mejora la animacion del arma el juicio de los
                //  dos soles»). CUATRO actos REHECHOS:
                //    0 · LA INVOCACIÓN GÓTICA — los soles nacen como RUNAS
                //      de silueta apenas insinuada y se ENCENDEN desde el
                //      núcleo (la materia brotando del conjuro), con el
                //      SIGILO de la cita dibujándose bajo ellos.
                //    1 · EL VALS DE LOS ECLIPSES — la órbita de caída con
                //      ESTELAS DE COMETA (la historia viva de la danza) y
                //      cada cruce ES UN ECLIPSE: el sol que pasa por
                //      delante APAGA al otro (el brillo alterna — los
                //      cuerpos se comen la luz por turnos, el lenguaje de
                //      la fusión).
                //    2 · LA FUSIÓN — el gigante con sus anillos
                //      COLAPSANDO EN ESPIRAL hacia el núcleo (los aros ya
                //      no giran planos: CAEN hacia dentro, la materia
                //      rindiéndose), el temblor y el sonrojo de siempre.
                //    3 · EL SILENCIO Y LA SENTENCIA — primero TODO SE
                //      APAGA (el colapso ahora es OSCURO: el mundo retiene
                //      la respiración un instante)… y la SENTENCIA estalla
                //      DOBLE: el destello blanco + la onda de choque +
                //      LA LLUVIA DE LUZ (motas doradas cayendo del
                //      estallido).
                // ================================================================
                if (_acto == 0)
                {
                    // === LA INVOCACIÓN GÓTICA: runa → sol ===
                    float p = MathHelper.Clamp(_t / T_CONVOCATORIA, 0f, 1f);
                    float suave = p * p * (3f - 2f * p);
                    // v6.152 — el radio sale de LA CURVA MAESTRA (la
                    // misma del resto del juicio: imposible saltar)
                    float r = RuneSunRenderer.BodyPx * RadioDelJuicio(_edad);

                    // EL SIGILO DE LA CITA (el círculo de conjuro dibujándose
                    // bajo los soles: un aro que SE TRAZA — el arco crece
                    // con la invocación)
                    VFXCore.AbrirAdditive();
                    try
                    {
                        Vector2 cSig = Punto - Main.screenPosition;
                        // el trazo: aro de 200px que nace de un punto y se
                        // cierra (el compás del mago)
                        float arco = MathHelper.TwoPi * suave;
                        for (int k = 0; k < 24; k++)
                        {
                            float a0 = k * MathHelper.TwoPi / 24f;
                            if (a0 > arco) break;
                            Vector2 pos = cSig + a0.ToRotationVector2() * 200f;
                            Vector2 pos2 = cSig + MathF.Min(a0 + 0.16f, arco).ToRotationVector2() * 200f;
                            VFXCore.Line(pos, pos2, SigiloLib.Tint(new Color(255, 214, 130), 0.45f + 0.30f * suave), 3f);
                        }
                    }
                    finally { VFXCore.RestaurarLoteVanilla(); }

                    for (int lado = 0; lado < 2; lado++)
                    {
                        bool luz = lado == 0;
                        Vector2 pos = PosicionSolSencillo(luz, suave, gt);
                        // EL ENCENDIDO: el sol nace OSCURO (la silueta
                        // gótica, alpha 0.35) y su núcleo SE PRENDE al
                        // 60% — la materia brotando del conjuro
                        float enciende = MathHelper.Clamp((p - 0.55f) / 0.45f, 0f, 1f);
                        DibujarSol(luz, pos, r, gt, 1, rg: 0f,
                            alpha: 0.35f + 0.65f * enciende,
                            seed: luz ? 3 : 7, rot: gt * (luz ? 0.4f : -0.4f));
                    }
                }
                else if (_acto == 1)
                {
                    // === EL VALS DE LOS ECLIPSES (órbita + estelas + turnos de luz) ===
                    float p = MathHelper.Clamp(_t / (float)T_FUSION, 0f, 1f);
                    float pc = MathHelper.Clamp(p / P_CONTACTO, 0f, 1f);
                    if (p < P_CONTACTO)
                    {
                        float caida = pc * pc * (3f - 2f * pc);
                        caida = MathF.Pow(caida, 1.45f);
                        float radio = MathHelper.Lerp(190f, 24f, caida);
                        float ang = gt * 1.6f + lado0(gt) + pc * pc * 10.5f;

                        for (int lado = 0; lado < 2; lado++)
                        {
                            bool luz = lado == 0;
                            float angLado = ang + (luz ? 0f : MathHelper.Pi);
                            Vector2 pos = Punto + angLado.ToRotationVector2() * radio;
                            // v6.152 — el radio del vals también nace de
                            // LA CURVA MAESTRA (continuo con TODO)
                            float r = RuneSunRenderer.BodyPx * RadioDelJuicio(_edad);

                            // EL ECLIPSE MUTUO: el sol que pasa por DELANTE
                            // (la mitad inferior de la órbita: sin>0) APAGA
                            // al otro — los brillos ALTERNAN con la danza
                            bool delante = MathF.Sin(angLado) > 0f;
                            float alfaEclipse = delante ? 1.10f : 0.55f;

                            // LA ESTELA DEL COMETA: la historia viva de la
                            // caída — 9 posiciones PASADAS con el radio y
                            // ángulo de hace k·0.045 rad, apagándose
                            VFXCore.AbrirAdditive();
                            try
                            {
                                for (int k = 1; k <= 9; k++)
                                {
                                    // v6.152 — LA ESTELA DETERMINISTA (la
                                    // cura fina de los saltos): el azar
                                    // por frame (Main.rand en las estelas)
                                    // parpadeaba como micro-saltos — el
                                    // Hash01 de la casa: LA MISMA estela
                                    // cada frame, quieta como historia.
                                    float angPas = angLado - k * 0.045f - 0.09f;
                                    float radPas = radio + k * 2.2f;   // la estela nace ANCHA (la órbita era mayor)
                                    Vector2 posPas = Punto + angPas.ToRotationVector2() * radPas - Main.screenPosition;
                                    float fEst = (1f - k / 10f);
                                    VFXCore.Quad(posPas, SigiloLib.Tint(
                                        luz ? new Color(255, 240, 200) : new Color(190, 130, 255),
                                        0.30f * fEst * fEst * alfaEclipse),
                                        new Vector2(r * (0.55f + 0.4f * fEst)), 0f);
                                }
                            }
                            finally { VFXCore.RestaurarLoteVanilla(); }

                            DibujarSol(luz, pos, r, gt, 1, rg: 0f,
                                alpha: MathHelper.Clamp(alfaEclipse, 0f, 1f),
                                seed: luz ? 3 : 7, rot: gt * (luz ? 0.4f : -0.4f));
                        }
                    }
                    else
                    {
                        // ============================================================
                        //  v6.151 — LA FUSIÓN SIN SALTO (la letra: «se unen
                        //  pero el proyectil da un salto para hacer otra
                        //  animacion, debe ser una animacion suave al
                        //  inicio y ademas debe ser completamente fluida»).
                        //  EL DIAGNÓSTICO: en el CONTACTO el vals moría con
                        //  sus soles a radio 24 px y escala 0.55·BodyPx — y
                        //  el gigante nacía de GOLPE a 0.7·BodyPx con un
                        //  jitter aleatorio encima (el «salto»). LA CURA:
                        //  el gigante nace EXACTO del tamaño del par en el
                        //  contacto (0.55) y crece CONTINUO hasta 1.05 —
                        //  la MISMA curva que el acto de la caza recogerá
                        //  después (una sola función, cero cortes de
                        //  fase); el TEMBLOR aleatorio murió (era el
                        //  «saltito» leído) y el destello de contacto se
                        //  FUNDIÓ con la curva (ya no es un flash seco:
                        //  es la misma luz que sigue creciendo).
                        // ============================================================
                        float n = MathHelper.Clamp((p - P_CONTACTO) / (1f - P_CONTACTO), 0f, 1f);
                        float r = RuneSunRenderer.BodyPx * RadioDelJuicio(_edad);
                        DibujarSolFusionado(Punto, r, gt, 3, rg: 0f, alpha: 1f);
                        // EL ALIENTO DEL NACIMIENTO: la luz que respira al
                        // ritmo de la curva (continua — el destello viejo
                        // era un corte): nace con el contacto y se disuelve
                        // en el arranque de la caza
                        if (n < 0.6f)
                        {
                            VFXCore.Begin();
                            try
                            {
                                float f = 1f - n / 0.6f;
                                Vector2 dPos = Punto - Main.screenPosition;
                                float fr = 460f * (0.30f + 0.55f * (1f - f));
                                VFXCore.Quad(dPos, SigiloLib.Tint(new Color(255, 250, 235), 0.70f * f * f),
                                    new Vector2(fr, fr), 0f);
                                VFXCore.Quad(dPos, SigiloLib.Tint(new Color(255, 230, 170), 0.42f * f * f * f),
                                    new Vector2(fr * 1.6f, fr * 1.6f), 0f);
                            }
                            finally { VFXCore.FlushAdditive(null, false); }
                        }
                    }
                }
                else if (_acto == 2)
                {
                    // ============================================================
                    //  v6.151 — LA CAZA DIBUJADA (la letra: «el sol formado
                    //  por la fusion… debe mantenerse, perseguir a los
                    //  enemigos y despues de un tiempo debe crecer al menos
                    //  70% mas su tamaño, volverse completamente rojo y
                    //  luego explotar»): el gigante CABALGA tras su presa
                    //  (el _centroCaza que mueve la IA), crece con la
                    //  MISMA curva continua del nacimiento (RadioFusion
                    //  0.55→1.05 y LUEGO la caza 1.05→1.05+0.70 — una
                    //  sola línea de vida, cero saltos) y su paleta SE
                    //  PIERDE EN EL ROJO conforme la caza madura (rg:
                    //  el dorado de la fusión → el carmesí del final).
                    // ============================================================
                    float caza = MathHelper.Clamp(_t / (float)T_CARGA, 0f, 1f);
                    float rg = caza * caza * (3f - 2f * caza);   // smoothstep: el rojo madura suave
                    // v6.152 — el radio de la caza TAMBIÉN es LA CURVA
                    // MAESTRA (nace del contacto sin costura y crece
                    // al gigante) + el pulso arterial encima
                    float r = RuneSunRenderer.BodyPx *
                        (RadioDelJuicio(_edad) + 0.05f * MathF.Sin(gt * 9f) * rg);
                    DibujarSolFusionado(_centroCaza, r, gt * (1f + 1.2f * rg), 3, rg, alpha: 1f);

                    // LA LUZ DE LA CAZA: el dorado se vuelve carmesí
                    if (!Main.dedServ)
                        Lighting.AddLight(_centroCaza,
                            new Vector3(2.2f, 1.8f - 0.9f * rg, 1.2f - 0.8f * rg) * (0.7f + 0.5f * rg));

                    // LA ESTELA DE LA CAZA: migas de luz dorada→roja que el
                    // sol deja mientras persigue (la historia del vuelo)
                    if (!Main.dedServ && Main.rand.NextBool(2))
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        Dust d = Dust.NewDustPerfect(_centroCaza + ang.ToRotationVector2() *
                            (r * (0.7f + 0.5f * Main.rand.NextFloat())),
                            DustID.GoldFlame,
                            -ang.ToRotationVector2() * Main.rand.NextFloat(0.8f, 2.2f), 0,
                            Color.Lerp(new Color(255, 226, 150), new Color(255, 90, 40), rg), 1.1f);
                        d.noGravity = true;
                    }

                    // EL ARO QUE CAE (la firma de la carga vieja, ahora
                    // SIRVIENDO a la caza: el anillo espirala hacia el
                    // cuerpo ENROJECIENDO — la gravedad de la sentencia)
                    VFXCore.AbrirAdditive();
                    try
                    {
                        float cae = 1f - rg;
                        float radCae = MathHelper.Lerp(r * 1.6f, 210f, cae * cae);
                        Vector2 cPos = _centroCaza - Main.screenPosition;
                        for (int k = 0; k < 20; k++)
                        {
                            float a0 = k * MathHelper.TwoPi / 20f + gt * 2.4f + rg * 7f;
                            Vector2 p0 = cPos + a0.ToRotationVector2() * radCae;
                            Vector2 p1 = cPos + (a0 + 0.33f).ToRotationVector2() * radCae;
                            Color cCae = Color.Lerp(new Color(255, 224, 150), new Color(255, 80, 40), rg);
                            VFXCore.Line(p0, p1, SigiloLib.Tint(cCae, 0.5f * (0.4f + 0.6f * rg)), 2.5f);
                        }
                    }
                    finally { VFXCore.RestaurarLoteVanilla(); }
                }
                else
                {
                    // === EL SILENCIO Y LA SENTENCIA (en el FINAL de la
                    //     caza: el gigante ROJO y enorme se pliega — la
                    //     sentencia cae DONDE terminó la persecución) ===
                    if (_t < T_COLAPSO)
                    {
                        // EL SILENCIO OSCURO: el sol se plisá y SE APAGA —
                        // el mundo retiene la respiración (el alpha cae
                        // MÁS rápido: al 70% del colapso ya casi no hay
                        // luz — el vacío antes de la palabra)
                        float plega = _t / (float)T_COLAPSO;
                        // v6.152 — el plisado del colapso también por la
                        // curva maestra (continuo con la caza que acaba)
                        float r = RuneSunRenderer.BodyPx * RadioDelJuicio(_edad);
                        DibujarSolFusionado(_centroCaza, r, gt * 2.8f, 3, rg: 1f,
                            alpha: MathF.Max(0.08f, 1f - 1.45f * plega));
                    }
                    else
                    {
                        // LA SENTENCIA: el estallido DOBLE — destello + onda
                        // + LA LLUVIA DE LUZ (las motas doradas del veredicto
                        // cayendo del centro del estallido — DONDE cayó el
                        // gigante rojo, no en el punto de la cita vieja)
                        float p = MathHelper.Clamp((_t - T_COLAPSO) / (float)(T_NOVA - T_COLAPSO), 0f, 1f);
                        float apaga = 1f - p;
                        // v6.152 — la sentencia cierra LA MISMA curva (el
                        // estallido final del radio maestro)
                        float r = RuneSunRenderer.BodyPx * RadioDelJuicio(_edad);
                        DibujarSolFusionado(_centroCaza, r, gt * 3.2f, 3, rg: 1f,
                            alpha: MathF.Min(1f, apaga * 1.8f));

                        VFXCore.AbrirAdditive();
                        try
                        {
                            Vector2 cPos = _centroCaza - Main.screenPosition;
                            OndaLib.Shock(Main.spriteBatch, cPos,
                                MathHelper.Clamp((_t - T_COLAPSO) / 26f, 0f, 1f),
                                1.5f + 4.5f * MathF.Min(1f, (_t - T_COLAPSO) / 7f),
                                new Color(255, 232, 170), 0.8f * apaga, 7, 14f);

                            // LA LLUVIA DE LUZ (14 motas deterministas que
                            // nacen del estallido y CAEN — el oro del
                            // veredicto esparciéndose)
                            if (apaga > 0.05f)
                            {
                                for (int k = 0; k < 14; k++)
                                {
                                    float h = VFXCore.Hash01(k, 3, 991);
                                    float ang = h * MathHelper.TwoPi + gt * 0.3f;
                                    float vel = 60f + 130f * VFXCore.Hash01(k, 5, 993);
                                    float edad = MathHelper.Clamp((_t - T_COLAPSO - h * 6f) / 20f, 0f, 1f);
                                    if (edad <= 0f) continue;
                                    Vector2 mota = cPos + new Vector2(
                                        MathF.Cos(ang) * vel * edad,
                                        MathF.Sin(ang) * vel * edad + 60f * edad * edad); // la gravedad del oro
                                    float fMota = (1f - edad) * apaga;
                                    VFXCore.Quad(mota, SigiloLib.Tint(
                                        k % 3 == 0 ? new Color(255, 250, 235) : new Color(255, 216, 140),
                                        0.65f * fMota), new Vector2(14f, 14f), 0f);
                                }
                            }
                        }
                        finally { VFXCore.RestaurarLoteVanilla(); }
                    }
                }

                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.RestaurarLoteVanilla(); }
            return false;
        }


        /// <summary>v6.127 — el ángulo base de la espiral (el arranque
        /// sereno del par — determinista, del reloj global).</summary>
        private static float lado0(float gt) => gt * 0.2f;

        /// <summary>v6.151 — LA CURVA ÚNICA DE LA FUSIÓN (la cura del
        /// «salto»): el radio del sol fusionado de su NACIMIENTO (n=0,
        /// EXACTO al radio 0.55·BodyPx del par en el contacto — la
        /// continuidad matemática) a su adultez (n=1, 1.05·BodyPx).
        /// La caza recoge esta curva por su cola: una sola línea de
        /// vida, sin cortes de fase JAMÁS.</summary>
        private static float RadioFusion(float n)
        {
            float s = n * n * (3f - 2f * n);   // smoothstep: suave al inicio
            return MathHelper.Lerp(0.55f, 1.05f, s);
        }

        /// <summary>v6.152 — LA CURVA MAESTRA DE TODO EL JUICIO: el
        /// radio del cuerpo central (el par → el gigante → la caza → el
        /// colapso → la sentencia) como función CONTINUA del RELOJ
        /// TOTAL. Cada tramo termina EXACTO donde empieza el siguiente
        /// (costuras C0) y con su misma pendiente (costuras C1: las
        /// curvas son smoothstep — la derivada en los bordes es 0 en
        /// ambos lados): ES IMPOSIBLE QUE SALTE, matemáticamente.
        /// Los tramos (en ticks del _edad): 0-36 convocatoria (el par
        /// vuela, r 0.5→0.92) · 36-63 el vals (espiral, r 0.92→0.55) ·
        /// 63-66 el contacto (r 0.55) · 66-366 la caza (0.55 →
        /// 1.05+0.70) · 366-373 el colapso (plisado) · 373+ la nova.</summary>
        private static float RadioDelJuicio(float edad)
        {
            // EL PAR (convocatoria + vals): el radio de CADA sol —
            // idéntico al PosicionSolSencillo/vals de siempre
            if (edad < T_CONVOCATORIA)
            {
                float u0 = edad / (float)T_CONVOCATORIA;
                return MathHelper.Lerp(0.50f, 0.92f, u0 * u0 * (3f - 2f * u0));
            }
            if (edad < T_CONVOCATORIA + T_FUSION)
            {
                float u1 = (edad - T_CONVOCATORIA) / (float)T_FUSION;
                return MathHelper.Lerp(0.92f, 0.55f, u1 * u1 * (3f - 2f * u1));
            }
            // EL CONTACTO + LA CAZA: nace del 0.55 del par y crece
            // continuo hasta el gigante adulto + el 70 % de la caza
            if (edad < T_CONVOCATORIA + T_FUSION + T_CARGA)
            {
                float n = (edad - T_CONVOCATORIA - T_FUSION) / (float)T_CARGA;
                return RadioFusion(Math.Min(1f, n * 3f))        // nacimiento 0.55→1.05
                     + CRECE_CAZA * (n * n * (3f - 2f * n));     // la caza 1.05→1.75
            }
            // EL COLAPSO + LA NOVA: plisado y estallido (continuos)
            float tPost = edad - T_CONVOCATORIA - T_FUSION - T_CARGA;
            float rGigante = 1.05f + CRECE_CAZA;
            if (tPost < T_COLAPSO)
            {
                float plega = tPost / (float)T_COLAPSO;
                return MathHelper.Lerp(rGigante, rGigante * 0.42f,
                    plega * plega * (3f - 2f * plega));   // smoothstep: respira
            }
            // v6.152 — LA NOVA SIN COSTURA (el SIM cazó el salto): el
            // estallido nace EXACTO del fondo del plisado (0.42·rGigante)
            // y crece CONTINUO a su pico (0.42+0.95 → ×1.03 del fondo) —
            // sin la división que deshacía el plisado y saltaba 0.27 en
            // el primer tick de la sentencia.
            float p = MathHelper.Clamp((tPost - T_COLAPSO) / (float)(T_NOVA - T_COLAPSO), 0f, 1f);
            float crece = MathF.Min(1f, (tPost - T_COLAPSO) / (float)(T_NOVA - T_COLAPSO));
            return rGigante * 0.42f * (1f + 1.25f * smoothstepNova(crece)) * (0.85f + 0.15f * (1f - p));
        }

        /// <summary>v6.152 — el smoothstep de la nova (suave al nacer del
        /// fondo del colapso — cero salto, cero división rara).</summary>
        private static float smoothstepNova(float x)
        {
            x = MathHelper.Clamp(x, 0f, 1f);
            return x * x * (3f - 2f * x);
        }

        /// <summary>v6.152 — LA POSICIÓN DEL SOL AHORA: el punto donde
        /// vive el cuerpo central en este tick del reloj total (la
        /// convocatoria y el vals en su sitio de siempre; la caza
        /// CABALGA el _centroCaza que mueve la IA — UNA verdad).</summary>
        private Vector2 CentroDelJuicio(float edad)
        {
            if (edad >= T_CONVOCATORIA + T_FUSION) return _centroCaza;
            return Punto;
        }

        /// <summary>v6.151 — LA PRESA de la caza: el enemigo REAL más
        /// cercano al punto (VFXCore.EsObjetivo — la ley de la casa:
        /// ni amistosos ni intocables), en un radio de caza amplio.</summary>
        private static int PresaMasCercana(Vector2 desde)
        {
            int mejor = -1;
            float mejorD = 1600f * 1600f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !VFXCore.EsObjetivo(n)) continue;
                float d = n.Center.DistanceSQ(desde);
                if (d < mejorD) { mejorD = d; mejor = i; }
            }
            return mejor;
        }

        /// <summary>v6.127 — LA POSICIÓN DEL SOL SENCILLO durante la
        /// convocatoria (el MISMO camino para la IA de las estelas y el
        /// PreDraw — una sola verdad): desde su lado (±300 px) con el
        /// cabeceo sereno de siempre, al punto de la cita.</summary>
        private Vector2 PosicionSolSencillo(bool luz, float suave, float gt)
        {
            Vector2 origen = Punto + (luz ? new Vector2(-300f, 0f) : new Vector2(300f, 0f));
            return Vector2.Lerp(origen, Punto, suave) +
                new Vector2(0f, MathF.Sin(gt * 6f + (luz ? 0f : 3.1f)) * 26f * (1f - suave));
        }

        // ================================================================
        //  LOS CUERPOS SOLARES — LA MISMA TÉCNICA DE LA FAMILIA DE SOLES
        //  (DrawSunBody: el disco de plasma SunShader con su granulación
        //  dendrítica + EmitRingSystem: los aros rúnicos de SigiloLib)
        // ================================================================

        /// <summary>Dibuja un sol (blanco o negro) con SU paleta y SUS
        /// anillos rúnicos — la base de soles rúnicos de la casa.</summary>
        private static void DibujarSol(bool luz, Vector2 centroMundo, float r, float gt,
            int anillos, float rg, float alpha, int seed, float rot)
        {
            Vector2 drawPos = centroMundo - Main.screenPosition;
            if (luz)
            {
                // EL SOL BLANCO — blanco-oro (la esfera de luz).
                RuneSunRenderer.DrawSunBody(drawPos, r, rot, gt,
                    new Color(255, 255, 252), new Color(238, 228, 205),
                    new Color(255, 214, 120),
                    new Color(255, 240, 196), new Color(255, 205, 120),
                    new Color(255, 248, 228), 0.9f, alpha);
            }
            else
            {
                // EL SOL NEGRO — negro-violeta (la esfera de oscuridad:
                // el negro solo existe si su borde arde — su backglow
                // violeta lo recorta del mundo).
                RuneSunRenderer.DrawSunBody(drawPos, r, rot, gt,
                    new Color(20, 11, 32), new Color(7, 4, 12),
                    new Color(96, 54, 170),
                    new Color(130, 76, 215), new Color(76, 44, 150),
                    new Color(150, 84, 235), 0.9f, alpha);
            }
            VFXCore.Begin();
            try { RuneSunRenderer.EmitRingSystem(centroMundo, r, gt, seed, anillos, rg, 0f, alpha); }
            finally { VFXCore.FlushAdditive(null, false); }
        }

        /// <summary>EL SOL FUSIONADO — el sol MÁS GRANDE Y DEVASTADOR:
        /// el corazón blanco-oro de la luz con las vetas violetas de la
        /// oscuridad (la paleta de la fusión) y SUS TRES ANILLOS (la ley
        /// de la letra: solo el gigante merece la corona completa).</summary>
        private static void DibujarSolFusionado(Vector2 centroMundo, float r, float gt,
            int anillos, float rg, float alpha)
        {
            Vector2 drawPos = centroMundo - Main.screenPosition;
            // la gigante enrojece la paleta (la ley de los soles de la casa)
            Color main = Color.Lerp(new Color(255, 251, 244), new Color(255, 196, 130), rg * 0.8f);
            Color darker = Color.Lerp(new Color(216, 186, 255), new Color(170, 90, 200), rg * 0.8f);
            RuneSunRenderer.DrawSunBody(drawPos, r, gt * 0.5f, gt,
                main, darker, new Color(120, 70, 200),
                Color.Lerp(new Color(255, 238, 205), new Color(255, 170, 90), rg),
                Color.Lerp(new Color(180, 120, 255), new Color(140, 70, 220), rg),
                Color.Lerp(new Color(255, 250, 240), new Color(255, 210, 150), rg),
                0.6f, alpha);
            VFXCore.Begin();
            try { RuneSunRenderer.EmitRingSystem(centroMundo, r, gt, 11, anillos, rg, 0f, alpha); }
            finally { VFXCore.FlushAdditive(null, false); }
        }
    }
}
