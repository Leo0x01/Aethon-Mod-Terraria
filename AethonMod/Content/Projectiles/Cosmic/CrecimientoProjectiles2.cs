using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Weapons.Cosmic;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    // ======================================================================
    //  v6.77 — LAS ARMAS DEL CRECIMIENTO (8 · 14) — los proyectiles.
    //      La segunda mitad del arsenal de las 14 ideas (la letra
    //      completa en CrecimientoArsenal.cs).
    //
    //  v6.79 — EL REDISEÑO VFX TOTAL (la letra del playtest: «todas se ven
    //      horribles... solo tomaste una textura y la lanzaste»): el
    //      gameplay queda BYTE-INTACTO; cada arma usa ahora 4-6
    //      librerías de la casa — el sol vivo y el ojo del abismo
    //      (CodigosLib), el fuego por rampas (PyraLib), la luz que emana
    //      (LumenLib), los tajos (TajoLib), la tinta viva (TelaLib), el
    //      prisma (ColorPrisma), las nebulosas (NebulaLib), la escritura
    //      rúnica (SigiloLib) y las ondas (OndaLib). Determinismo por
    //      whoAmI/GameUpdateCount (cero Main.rand en render).
    // ======================================================================

    // ======================================================================
    //  8 · LA ESTRELLA DOMÉSTICA — la caída que alimenta
    //      E1: nace arriba y cae al punto · E2: cae más rápido · E3: el
    //      estallido llega más lejos · E4: la estrella brilla TODO el camino.
    //      v6.79 EL VESTIDO: la estrella es EL SOL VIVO (CodigosLib: la
    //      cáscara + el disco ecuatorial + el núcleo girando + las
    //      prominencias de fuego), cae con su cinta dorada (Ribbon) y
    //      TELEGRÁFICA su destino (OndaLib: la raya que apunta al punto
    //      y el anillo que se contrae) — la estrella avisa adónde va a
    //      caer; el impacto es fuego solar (PyraLib.SolarFire) con cine.
    // ======================================================================
    public class EstrellaDomesticaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/DestelloFinal";

        public override void SetDefaults()
        {
            Projectile.width = 22; Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 75;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Etapa => IdeasGrimorio.Etapa(Nivel);
        private Vector2 Punto => new(Projectile.localAI[0], Projectile.localAI[1]);
        private float _dist0 = -1f;

        public override void AI()
        {
            // EL PUNTO DEL CLIC (SP-first: el arma de pruebas de la casa).
            if (Projectile.localAI[0] == 0f && Projectile.localAI[1] == 0f &&
                Projectile.owner == Main.myPlayer)
            {
                Projectile.localAI[0] = Main.MouseWorld.X;
                Projectile.localAI[1] = Main.MouseWorld.Y;
                _dist0 = Vector2.Distance(Projectile.Center, Punto);
            }

            // LA CAÍDA (la letra: «cae hacia el cursor creciendo durante la
            // caída (la gravedad la alimenta)»): la estrella PERSIGUE su
            // punto — y el CUERPO crece con lo andado.
            if (_dist0 < 0f) _dist0 = 200f;
            float dist = Vector2.Distance(Projectile.Center, Punto);
            float prog = MathHelper.Clamp(1f - dist / _dist0, 0f, 1f);
            Projectile.scale = 0.5f + prog * 1.1f;
            Projectile.width = (int)(22 * Projectile.scale);
            Projectile.height = (int)(22 * Projectile.scale);

            float caida = Etapa >= 2 ? 14f : 11f;
            Vector2 hacia = (Punto - Projectile.Center).SafeNormalize(Vector2.UnitY);
            Projectile.velocity = hacia * caida;
            Projectile.rotation += 0.22f;
            // v6.79 — LA ESTELA DE LA CAÍDA + la luz del camino.
            EstelaLib.Track(Projectile.whoAmI, 14).Push(Projectile.Center);
            LumenLib.LightAlong(Projectile.Center, Punto,
                new Color(255, 240, 205), 0.5f + 0.6f * prog, 34f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.55f, 0.5f, 0.3f) * (0.4f + prog));

            // EL ESTALLIDO EN EL PUNTO EXACTO (la letra: «estalla en el
            // punto exacto» — precisión pura).
            if (dist < 15f) Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            // v6.79 — EL IMPACTO ES FUEGO SOLAR: el AoE con la rampa
            //     SolarFire + el cine suave + la lente de la caída.
            CrecimientoEstallidoProjectile.TinteProximo = new Color(255, 240, 205);
            CrecimientoEstallidoProjectile.RampaProxima = PyraPalettes.SolarFire;
            ImpactoLib.GolpeDeCine(Projectile.Center, 1.2f, 5,
                new Color(255, 240, 205), suave: true, destello: true);
            GravLens.Registrar(Projectile.Center, 140f, 0.26f, 0.35f);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                (int)(Projectile.damage * 1.5f), Projectile.knockBack * 1.6f, Projectile.owner);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item25, Projectile.Center);
            for (int i = 0; i < 8; i++)
            {
                int di = Dust.NewDust(Projectile.Center, 14, 14, DustID.Enchanted_Gold,
                    Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 1f), 180,
                    new Color(255, 246, 215), 1f);
                Main.dust[di].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            float prog = MathHelper.Clamp(1f - Vector2.Distance(Projectile.Center, Punto) / MathF.Max(_dist0, 1f), 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(255, 252, 240), 0.4f + 0.4f * prog);

            Vector2 pos = Projectile.Center - Main.screenPosition;
            Vector2 destino = Punto - Main.screenPosition;

            // (1) EL SOL VIVO (CodigosLib — la cáscara, el disco ecuatorial,
            //     el núcleo girando, las prominencias: la estrella doméstica
            //     es un sol de bolsillo que crece con la caída).
            if (!Main.gameMenu && VFXCore.Presupuesto(150))
                CodigosLib.SolVivo(Projectile.Center, 14f + 30f * prog, time,
                    Projectile.whoAmI * 11 + 7, 0.5f + 0.5f * prog);

            VFXCore.AbrirAdditive();
            // (2) LA CINTA DORADA DE LA CAÍDA.
            Vector2[] camino = EstelaLib.Track(Projectile.whoAmI, 14).Points();
            if (camino.Length >= 2)
            {
                for (int i = 0; i < camino.Length; i++)
                    camino[i] -= Main.screenPosition;
                EstelaLib.Ribbon(Main.spriteBatch, camino, 10f + 14f * prog,
                    EstelaProfile.Comet, new Color(255, 240, 205), 0.5f + 0.4f * prog,
                    Projectile.whoAmI * 31 + 23, time, head: true, taper: 110f);
            }
            // (3) EL TELEGRAPH DEL DESTINO (la raya que apunta al punto y
            //     el anillo que se contrae — la estrella AVISA adónde cae).
            OndaLib.Telegrafo(Main.spriteBatch, pos, destino, prog,
                new Color(255, 244, 214), 2.4f);
            // (4) EL BLOOM que gana con la caída (E4: brilla TODO el camino).
            LumenLib.BloomPulse(Main.spriteBatch, pos, 30f + 40f * prog,
                new Color(255, 240, 205), 0.45f + 0.5f * prog, time, 2.6f);
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  9 · LA PÁGINA CORTANTE — la daga que crece rebotando
    //      E1: rebota y crece (hasta ×3) · E2: el daño por rebote sube a
    //      ×1,5 · E3: 5 rebotes · E4: la hoja deja tinta en cada muro.
    //      v6.79 EL VESTIDO: la página es una DAGA DE LUZ (LumenLib.Lance
    //      apuntando a la marcha), cada rebote estampa su TAJO en el muro
    //      (TajoLib — la media luna del corte, con su frente de revelado
    //      ardiendo), la estela es la cinta del veredicto (GradienteVeredicto)
    //      y la tinta de la E4 es una nebulosa morada en el muro.
    // ======================================================================
    public class PaginaCortanteProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmic/FolioHoja";

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;   // el rebote lo lleva la AI (la cuenta de los rebotes)
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Etapa => IdeasGrimorio.Etapa(Nivel);
        private int Rebotes => (int)Projectile.ai[1];
        private float _edad;

        // v6.79 — el tajo del rebote (12 ticks de vida).
        private Vector2 _tajoPos;
        private float _tajoAng;
        private int _tajoT;

        public override void AI()
        {
            _edad++;
            // === EL REBOTE (la letra: «una hoja voladora doblada como daga
            //     que crece con cada rebote en paredes (hasta ×3)»): el
            //     choque se detecta por tile y la velocidad se refleja —
            //     cada rebote la hace MÁS GRANDE y MÁS FILA. ===
            Vector2 futura = Projectile.Center + Projectile.velocity;
            int tx = (int)(futura.X / 16f); int ty = (int)(futura.Y / 16f);
            bool choca = false;
            if (WorldGen.InWorld(tx, ty) && WorldGen.SolidTile(tx, ty)) choca = true;
            if (choca)
            {
                // ¿EJE? probar X e Y por separado (el reflejo del que sea).
                int ax = (int)((Projectile.Center.X + Projectile.velocity.X) / 16f);
                int ay = (int)((Projectile.Center.Y + Projectile.velocity.Y) / 16f);
                bool chocaX = WorldGen.InWorld(ax, (int)(Projectile.Center.Y / 16f)) &&
                    WorldGen.SolidTile(ax, (int)(Projectile.Center.Y / 16f));
                bool chocaY = WorldGen.InWorld((int)(Projectile.Center.X / 16f), ay) &&
                    WorldGen.SolidTile((int)(Projectile.Center.X / 16f), ay);
                if (chocaX) Projectile.velocity.X *= -1f;
                if (chocaY) Projectile.velocity.Y *= -1f;
                if (!chocaX && !chocaY) Projectile.velocity *= -1f;

                // v6.79 — EL TAJO DEL REBOTE: la media luna en el muro —
                //     abierta hacia DENTRO (la normal del muro: si rebotó
                //     en X el muro es vertical, la normal es horizontal).
                _tajoPos = Projectile.Center;
                _tajoAng = (chocaX && !chocaY) ? (Projectile.velocity.X > 0 ? 0f : MathHelper.Pi)
                    : (chocaY && !chocaX) ? (Projectile.velocity.Y > 0 ? MathHelper.PiOver2 : -MathHelper.PiOver2)
                    : Projectile.velocity.ToRotation();
                _tajoT = 12;

                // EL CUERPO CRECE (el tope de la letra: ×3).
                int tope = Etapa >= 3 ? 4 : 2;   // E3: 5 rebotes (4 + el nacimiento)
                if (Rebotes < tope)
                {
                    Projectile.ai[1]++;
                    Projectile.scale = 1f + Rebotes * 0.5f;   // ×1 → ×2 → ×3
                    if (Projectile.scale > 3f) Projectile.scale = 3f;
                    Projectile.width = (int)(24 * Projectile.scale);
                    Projectile.height = (int)(30 * Projectile.scale);
                    // EL FILO CRECE: cada rebote muerde más (E2: ×1,5).
                    Projectile.damage = (int)(Projectile.damage * (Etapa >= 2 ? 1.5f : 1.35f));
                    OndaLib.Kick(2f, 5);   // v6.79: el rebote se SIENTE
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item15, Projectile.Center);
                    // E4: la tinta del muro.
                    if (Etapa >= 4)
                        for (int i = 0; i < 4; i++)
                        {
                            int di = Dust.NewDust(Projectile.Center, 12, 12, DustID.Shadowflame,
                                Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 1f), 160,
                                new Color(120, 80, 190), 0.9f);
                            Main.dust[di].noGravity = true;
                        }
                }
                else { Projectile.Kill(); return; }
            }
            if (_tajoT > 0) _tajoT--;

            Projectile.rotation = Projectile.velocity.ToRotation();
            // v6.79 — la estela de la daga.
            EstelaLib.Track(Projectile.whoAmI, 10).Push(Projectile.Center);
            if (Main.rand.NextBool(4))
            {
                int di = Dust.NewDust(Projectile.Center - new Vector2(3f, 3f), 6, 6,
                    DustID.Enchanted_Gold, 0f, 0f, 170, new Color(235, 230, 255), 0.7f);
                Main.dust[di].noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // v6.79 — la muerte de la daga: el AoE con el violeta del veredicto.
            CrecimientoEstallidoProjectile.TinteProximo = new Color(225, 205, 255);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                (int)(Projectile.damage * 1.1f), Projectile.knockBack, Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            lightColor = Color.Lerp(lightColor, new Color(255, 252, 245), 0.3f + 0.12f * Math.Min(Rebotes, 3));

            Vector2 pos = Projectile.Center - Main.screenPosition;
            Color jugo = DefinitivasLib.GradienteVeredicto(_edad * 0.004f);

            // (1) LA TINTA DE LA E4 (la nebulosa morada que queda en el
            //     muro — la vida del tajo).
            VFXCore.Begin();
            if (Etapa >= 4 && _tajoT > 4)
                NebulaLib.Nube(_tajoPos, 26f, time, Projectile.whoAmI * 17 + 12,
                    new Color(120, 80, 190), new Color(80, 50, 150), 0.10f, 8);
            VFXCore.FlushAdditive();

            VFXCore.AbrirAdditive();
            // (2) LA DAGA DE LUZ (Lance apuntando a la marcha — el filo
            //     delante del cuerpo, creciendo con los rebotes).
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            LumenLib.Lance(Main.spriteBatch, pos, dir,
                34f + 26f * Math.Min(Rebotes, 3), 3.2f, jugo, 0.8f);
            // (3) LA ESTELA (la cinta fina del veredicto).
            Vector2[] camino = EstelaLib.Track(Projectile.whoAmI, 10).Points();
            if (camino.Length >= 2)
            {
                for (int i = 0; i < camino.Length; i++)
                    camino[i] -= Main.screenPosition;
                EstelaLib.Ribbon(Main.spriteBatch, camino, 6f,
                    EstelaProfile.Head, jugo * 0.8f, 0.4f,
                    Projectile.whoAmI * 31 + 27, time, head: true, taper: 70f);
            }
            // (4) EL TAJO DEL REBOTE (la media luna en el muro — el frente
            //     de revelado ardiendo y las puntas que prenden).
            if (_tajoT > 0)
            {
                float pT = 1f - _tajoT / 12f;
                TajoLib.Tajo(_tajoPos - Main.screenPosition,
                    34f + 10f * Math.Min(Rebotes, 3),
                    _tajoAng - 0.62f, _tajoAng + 0.62f,
                    DefinitivasLib.DeGolpe(pT), TajoLib.Campana(pT),
                    4.5f, jugo * 0.85f, Color.White,
                    Projectile.whoAmI * 7 + 6, time);
            }
            // (5) EL CUERPO: el bloom del filo.
            LumenLib.Bloom(Main.spriteBatch, pos, 24f + 14f * Math.Min(Rebotes, 3),
                jugo, 0.5f);
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  10 · EL PRISMA FRAGMENTADO — la fusión que golpea
    //      El NUEVO fragmento divide al ANTERIOR en dos menores que se
    //      re-fusionan en uno MAYOR que golpea.
    //      v6.79 EL VESTIDO: el prisma es PRISMA VIVO (ColorPrisma: el
    //      color cicla el arcoíris por fragmento, desfasado por whoAmI),
    //      con su ANILLO ARCOÍRIS horneado (VFXCore.Arcoiris) girando,
    //      los mini vuelan a fundirse con la lanza de fantasmas
    //      (LanceTrail — la soga prisma) y cada fusión estrena su onda.
    // ======================================================================
    public class PrismaFragmentadoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/Crescent";

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Etapa => IdeasGrimorio.Etapa(Nivel);
        /// <summary>La FUSIÓN acumulada (0 = recién nacido, 2,4 = tope).</summary>
        private float Fusion => Projectile.ai[1];
        /// <summary>0=fragmento · 1=mini · 2=principal (el anterior, dividido).</summary>
        private float Modo => Projectile.ai[2];
        private bool _primera = true;
        private int _danoBase;
        private float _relojPrisma;
        private int _fusionT;   // v6.79: la onda de la fusión (10 ticks)
        private Vector2 _posPrincipal;
        private bool _hayPrincipal;

        public override void AI()
        {
            // EL DAÑO BASE (para que la fusión crezca desde la raíz).
            if (_danoBase == 0) _danoBase = Math.Max(1, Projectile.damage);
            _relojPrisma += 0.5f + 0.2f * Fusion * 0.4f;
            if (_fusionT > 0) _fusionT--;
            _hayPrincipal = false;

            // === LA DIVISIÓN (la letra: «el primer clic lanza un
            //     fragmento; el siguiente lo divide en dos menores que se
            //     re-fusionan en uno MAYOR»): al NACER yo, el ANTERIOR
            //     vivo se PARTE en dos mini — y los mini me buscan para
            //     fundirse en mí (yo crezco con cada uno que llega). ===
            if (_primera && Modo == 0f)
            {
                _primera = false;
                Projectile anterior = null; float mejorD = 900f;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || p.type != Type ||
                        p.owner != Projectile.owner || p.whoAmI == Projectile.whoAmI) continue;
                    if (p.ModProjectile is not PrismaFragmentadoProjectile prisma) continue;
                    if (prisma.Modo == 1f) continue;   // los mini no cuentan
                    float d = Vector2.Distance(p.Center, Projectile.Center);
                    if (d < mejorD) { mejorD = d; anterior = p; }
                }
                if (anterior != null && anterior.ModProjectile is PrismaFragmentadoProjectile viejo)
                    viejo.Dividirse(Projectile);
            }

            // EL MINI: me fundo en el PRINCIPAL más cercano (el nuevo).
            if (Modo == 1f)
            {
                Projectile principal = null; float mejorD = 700f;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || p.type != Type ||
                        p.owner != Projectile.owner || p.whoAmI == Projectile.whoAmI) continue;
                    if (p.ModProjectile is not PrismaFragmentadoProjectile prisma) continue;
                    if (prisma.Modo == 1f) continue;
                    float d = Vector2.Distance(p.Center, Projectile.Center);
                    if (d < mejorD) { mejorD = d; principal = p; }
                }
                if (principal != null)
                {
                    _hayPrincipal = true;
                    _posPrincipal = principal.Center;
                    Vector2 hacia = (principal.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    float vivo = Etapa >= 2 ? 0.16f : 0.10f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 13f, vivo);
                    if (mejorD < 14f && principal.ModProjectile is PrismaFragmentadoProjectile nuevoP)
                    {
                        nuevoP.Fundirse();
                        Projectile.active = false;
                        return;
                    }
                }
            }

            // EL CUERPO con la FUSIÓN acumulada.
            Projectile.scale = 0.9f + Fusion * 0.55f;
            Projectile.width = (int)(24 * Projectile.scale);
            Projectile.height = (int)(24 * Projectile.scale);
            Projectile.rotation += 0.12f * (Modo == 1f ? 2.2f : 1f);
            // EL DAÑO viaja con la FUSIÓN (de la BASE — nunca cadena).
            Projectile.damage = (int)(_danoBase * (1f + Fusion * 0.5f));
        }

        /// <summary>ME DIVIDEN (el nuevo nació): suelto mis dos mini.</summary>
        public void Dividirse(Projectile nuevo)
        {
            if (Modo == 1f) return;   // los mini no se dividen
            // los DOS menores (la letra: «lo divide en dos menores»).
            for (int i = 0; i < 2; i++)
            {
                float lado = i == 0 ? -0.55f : 0.55f;
                Vector2 vel = Projectile.velocity.RotatedBy(lado) * 0.8f;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, vel, Type,
                    (int)(Projectile.damage * 0.5f), Projectile.knockBack * 0.6f, Projectile.owner,
                    Nivel, 0f, 1f);   // mini
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item42, Projectile.Center);
        }

        /// <summary>UN MINI SE FUNDE EN MÍ: crezco.</summary>
        public void Fundirse()
        {
            if (Fusion < 2.4f) Projectile.ai[1] += 0.5f;
            _fusionT = 10;   // v6.79: la onda que estrena la fusión nueva
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item42, Projectile.Center);
            // E4: el destello de la fusión.
            if (Etapa >= 4)
                for (int i = 0; i < 5; i++)
                {
                    int di = Dust.NewDust(Projectile.Center, 10, 10, DustID.RainbowRod,
                        Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 1f), 180,
                        new Color(210, 255, 245), 0.9f);
                    Main.dust[di].noGravity = true;
                }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // EL MAYOR GOLPEA (la letra: «se re-fusionan en uno MAYOR que
            // golpea»): la fusión completa detona en ÁREA (E3).
            if (Fusion > 1.4f && Etapa >= 3)
            {
                CrecimientoEstallidoProjectile.TinteProximo = ColorPrisma.ValueAt(_relojPrisma);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                    (int)(Projectile.damage * 1.3f), Projectile.knockBack * 1.8f, Projectile.owner);
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            float fundido = MathHelper.Clamp(Fusion / 2.4f, 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(235, 255, 250), 0.35f + 0.45f * fundido);

            Vector2 pos = Projectile.Center - Main.screenPosition;
            Color cPrisma = ColorPrisma.ValueAt(_relojPrisma);

            // (1) EL ANILLO ARCOÍRIS (la banda horneada de la casa girando
            //     — el prisma lleva su espectro alrededor).
            VFXCore.Begin();
            float rArco = 26f + 40f * fundido;
            VFXCore.Quad(Projectile.Center, new Color(255, 255, 255) * (0.35f + 0.4f * fundido),
                new Vector2(rArco, rArco), time * 0.55f, VFXCore.Arcoiris);
            VFXCore.FlushAdditive();

            VFXCore.AbrirAdditive();
            // (2) LA SOGA PRISMA (el mini que vuela a fundirse, con su
            //     lanza de fantasmas hacia el principal).
            if (_hayPrincipal && Modo == 1f)
                LumenLib.LanceTrail(Main.spriteBatch, pos,
                    (_posPrincipal - Projectile.Center).SafeNormalize(Vector2.UnitX),
                    Vector2.Distance(_posPrincipal, Projectile.Center) * 0.6f, 4f,
                    cPrisma, 0.9f, 6, 18f);
            // (3) LA ONDA DE LA FUSIÓN (los 10 ticks que estrena el cuerpo).
            if (_fusionT > 0)
            {
                float pF = 1f - _fusionT / 10f;
                OndaLib.Pulse(Main.spriteBatch, pos, DefinitivasLib.DeGolpe(pF),
                    70f + 60f * fundido, cPrisma, 0.8f, Projectile.whoAmI * 7 + 8);
                OrbitaLib.EcosAnillo(pos, (36f + 50f * pF) * OrbitaLib.Distorsion(time),
                    time, OrbitaLib.Distorsion(time), cPrisma, cPrisma * 0.5f);
            }
            // (4) EL CUERPO: el bloom prisma + el flare de la media luna.
            LumenLib.Bloom(Main.spriteBatch, pos, 30f + 40f * fundido,
                cPrisma, 0.45f + 0.4f * fundido);
            if (fundido > 0.4f)
                LumenLib.Flare(Main.spriteBatch, pos, 34f + 30f * fundido,
                    cPrisma, 0.7f * fundido, time * 1.2f);
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  11 · LA RUNA ORBITAL — los glifos que esperan
    //      3-5 glifos orbitan el libro (inertes, visibles en idle); al
    //      clicar, el MÁS VIEJO sale disparado hacia el cursor (activo).
    //      Los glifos crecen con el tiempo de órbita.
    //      v6.79 EL VESTIDO: cada glifo ES su runa (SigiloLib.Runa — el
    //      glifo solar dibujado a mano, de pie sobre la tangente), el
    //      riel de la órbita es un aro elíptico tenue (AroEliptico), la
    //      madurez dora el glifo (azul → oro) y el disparado vuela con
    //      su cinta dorada y su flare (la chispa del glifo lanzado).
    // ======================================================================
    public class RunaOrbitalProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/BitsChispa";

        public override void SetDefaults()
        {
            Projectile.width = 22; Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Etapa => IdeasGrimorio.Etapa(Nivel);
        /// <summary>0=orbital inerte · 1=activo disparado.</summary>
        private bool Activo => Projectile.ai[2] == 1f;
        /// <summary>El glifo de ESTE proyecto (estable por whoAmI).</summary>
        private int Glifo => (Projectile.whoAmI * 3 + 1) % 8;

        /// <summary>EL CLIC del arma (lo llama el item): lanza el glifo
        /// más viejo y rellena la órbita si hay hueco.</summary>
        internal static void AlClic(Terraria.DataStructures.IEntitySource source,
            Player player, int type, int damage, float knockback, int nivel)
        {
            int etapa = IdeasGrimorio.Etapa(nivel);
            int tope = etapa >= 4 ? 5 : etapa >= 2 ? 4 : 3;   // 3-5 (la letra)
            if (player == null || !player.active) return;

            // (1) LANZAR el más viejo (más tiempo orbitando).
            Projectile viejo = null; int mejorEdad = -1;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != type ||
                    p.owner != player.whoAmI) continue;
                if (p.ModProjectile is not RunaOrbitalProjectile r || r.Activo) continue;
                int edad = (int)p.ai[1];
                if (edad > mejorEdad) { mejorEdad = edad; viejo = p; }
            }
            if (viejo != null)
            {
                Vector2 vel = (Main.MouseWorld - viejo.Center).SafeNormalize(Vector2.UnitX) * 15f;
                Projectile.NewProjectile(source, viejo.Center, vel, type,
                    damage, knockback, player.whoAmI, nivel, 0f, 1f);
                viejo.active = false;
            }

            // (2) RELLENAR la órbita si hay hueco.
            int vivos = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != type ||
                    p.owner != player.whoAmI) continue;
                if (p.ModProjectile is RunaOrbitalProjectile r && !r.Activo) vivos++;
            }
            if (vivos < tope)
                Projectile.NewProjectile(source,
                    player.MountedCenter + new Vector2(0f, -44f), Vector2.Zero, type,
                    damage, knockback, player.whoAmI, nivel, 0f, 0f);
        }

        public override void AI()
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active || dueno.dead) { Projectile.Kill(); return; }

            if (Activo)
            {
                // EL DISPARO (la letra: «al clicar, uno sale disparado
                // GIRANDO hacia el cursor»).
                Projectile.rotation += 0.34f;
                Projectile.timeLeft = 160;
                // v6.79 — la cinta dorada del disparo.
                EstelaLib.Track(Projectile.whoAmI, 10).Push(Projectile.Center);
                if (Main.rand.NextBool(3))
                {
                    int di = Dust.NewDust(Projectile.Center - new Vector2(3f, 3f), 6, 6,
                        DustID.RainbowRod, 0f, 0f, 180, new Color(255, 230, 180), 0.8f);
                    Main.dust[di].noGravity = true;
                }
            }
            else
            {
                // EL ORBITAL INERTE (la letra: «3-5 glifos orbitan el libro
                // mientras lo llevas (visibles incluso en idle)»).
                Projectile.friendly = false;
                Projectile.ai[1]++;   // la edad (crece con la órbita)
                float ang = Projectile.whoAmI * 2.399f +   // el ángulo dorado: reparto sin índice
                    (float)Main.GlobalTimeWrappedHourly * 1.1f;
                Vector2 centro = dueno.MountedCenter + new Vector2(0f, -44f);
                Projectile.Center = centro + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 46f;
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = ang + MathHelper.PiOver2;
                // EL CRECIMIENTO con la órbita (la letra: «los glifos
                // crecen con la órbita del tiempo»): 0,6 → 1,15 en 10 s.
                Projectile.scale = 0.6f + MathHelper.Clamp(Projectile.ai[1] / 600f, 0f, 1f) * 0.55f;
                Projectile.width = (int)(22 * Projectile.scale);
                Projectile.height = (int)(22 * Projectile.scale);
                Projectile.timeLeft = 60;   // el inerte se renueva (vive mientras el dueño)
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (!Activo) return;
            // v6.79 — el impacto del glifo: el AoE dorado pequeño.
            CrecimientoEstallidoProjectile.TinteProximo = new Color(255, 232, 170);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                (int)(Projectile.damage * 1.1f), Projectile.knockBack, Projectile.owner);
            for (int i = 0; i < 6; i++)
            {
                int di = Dust.NewDust(Projectile.Center, 10, 10, DustID.RainbowRod,
                    Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(-2.5f, 1f), 180,
                    new Color(255, 235, 190), 0.9f);
                Main.dust[di].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            float madurez = MathHelper.Clamp(Projectile.ai[1] / 600f, 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(255, 240, 210), 0.35f + 0.4f * madurez);

            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active) return true;
            Vector2 centroLibro = dueno.MountedCenter + new Vector2(0f, -44f);

            // (1) EL GLIFO (el dibujo a mano de SigiloLib — el glifo solar
            //     de ESTE proyecto, de pie sobre la tangente; la madurez
            //     lo DORA: el azul del aprendiz → el oro del maestro).
            Color cuerpo = Color.Lerp(SigiloLib.CuerpoAzul, SigiloLib.CuerpoOro, madurez);
            Color punta = Color.Lerp(SigiloLib.PuntaAzul, SigiloLib.PuntaOro, madurez);
            float brillo = Activo ? 1.3f : 0.45f + 0.55f * madurez;

            VFXCore.Begin();
            if (VFXCore.Presupuesto(120))
            {
                SigiloLib.Runa(Projectile.Center, Projectile.rotation,
                    SigiloLib.RunasSolares, Glifo,
                    (Activo ? 1.15f : 0.55f + 0.6f * madurez) * 1.1f,
                    time, Projectile.whoAmI * 0.71f, cuerpo, punta, brillo);
                // EL RIEL DE LA ÓRBITA (el aro elíptico tenue alrededor del
                // libro — cada glifo aporta su octava).
                if (!Activo)
                    SigiloLib.AroEliptico(centroLibro, 46f, 46f, -0.10f, 0.26f, time, 0,
                        cuerpo, 0.06f + 0.05f * madurez);
            }
            VFXCore.FlushAdditive();

            VFXCore.AbrirAdditive();
            // (2) LA CHISPA (el bloom del glifo — el activo arde).
            LumenLib.BloomPulse(Main.spriteBatch, Projectile.Center - Main.screenPosition,
                Activo ? 40f : 16f + 20f * madurez,
                Activo ? new Color(255, 232, 170) : new Color(255, 225, 175),
                Activo ? 1.0f : 0.25f + 0.25f * madurez, time, Activo ? 4f : 1.1f);
            // (3) LA CINTA DEL DISPARO (el rastro dorado del glifo lanzado).
            if (Activo)
            {
                Vector2[] camino = EstelaLib.Track(Projectile.whoAmI, 10).Points();
                if (camino.Length >= 2)
                {
                    for (int i = 0; i < camino.Length; i++)
                        camino[i] -= Main.screenPosition;
                    EstelaLib.Ribbon(Main.spriteBatch, camino, 8f,
                        EstelaProfile.Comet, new Color(255, 232, 170), 0.7f,
                        Projectile.whoAmI * 31 + 31, time, head: true, taper: 80f);
                }
                LumenLib.Flare(Main.spriteBatch, Projectile.Center - Main.screenPosition,
                    30f, new Color(255, 240, 200), 0.9f, time * 2.3f);
            }
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  12 · LA ESPORA DEL ESCRIBA — las trampas vivas
    //      La madre lenta planta esporas en su camino; las esporas crecen
    //      en silencio y DETONAN al acercarse un enemigo.
    //      v6.79 EL VESTIDO: la madre camina en su nebulosa (NebulaLib)
    //      con su cinta verde; la espora es una nebulosa que RESPIRA
    //      LENTO (BloomPulse hz 0,6 — la trampa dormida) con su perímetro
    //      de detono dibujado (AnilloFino — el radio que acecha); la
    //      detonación es fuego VERDE (PyraPalettes.VoidFire) + flash.
    // ======================================================================
    public class EsporaDelEscribaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/OblivionBlob";

        public override void SetDefaults()
        {
            Projectile.width = 20; Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 480;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Etapa => IdeasGrimorio.Etapa(Nivel);
        /// <summary>0=madre · 1=espora (la trampa).</summary>
        private bool EsMadre => Projectile.ai[2] == 0f;

        public override void AI()
        {
            if (EsMadre)
            {
                // LA MADRE LENTA: va dejando esporas en su camino (la
                // letra: «proyectil lento que va plantando esporas»).
                if (Projectile.timeLeft % 22f < 1f && Projectile.timeLeft > 60f)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                        Projectile.Center, Vector2.Zero, Type,
                        (int)(Projectile.damage * 0.9f), Projectile.knockBack, Projectile.owner,
                        Nivel, 0f, 1f);
                }
                // v6.79 — la cinta de la madre.
                EstelaLib.Track(Projectile.whoAmI, 8).Push(Projectile.Center);
                if (Main.rand.NextBool(4))
                {
                    int di = Dust.NewDust(Projectile.Center - new Vector2(3f, 3f), 6, 6,
                        DustID.Enchanted_Gold, 0f, 0f, 160, new Color(190, 255, 190), 0.7f);
                    Main.dust[di].noGravity = true;
                }
            }
            else
            {
                // LA ESPORA: inerte, creciendo en silencio (la letra:
                // «las esporas crecen en silencio y detonan cuando un
                // enemigo se acerca»).
                Projectile.friendly = false;      // la trampa no pisa: DETONA
                Projectile.velocity = Vector2.Zero;
                float vida = 1f - Projectile.timeLeft / 480f;
                Projectile.scale = 0.35f + vida * 0.7f;
                Projectile.width = (int)(20 * Projectile.scale);
                Projectile.height = (int)(20 * Projectile.scale);
                Projectile.rotation += 0.02f;

                // EL DETONADOR: enemigo a <90 px → revienta.
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                    if (Vector2.Distance(n.Center, Projectile.Center) < 90f)
                    {
                        // v6.79 — LA DETONACIÓN: fuego VERDE (VoidFire) + el
                        //     flash + el kick — la trampa cobra.
                        CrecimientoEstallidoProjectile.TinteProximo = new Color(185, 255, 190);
                        CrecimientoEstallidoProjectile.RampaProxima = PyraPalettes.VoidFire;
                        OndaLib.Flash(new Color(185, 255, 190), 0.16f, 8, Projectile.Center);
                        OndaLib.Kick(4f, 8);
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                            Projectile.Center, Vector2.Zero,
                            ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                            (int)(Projectile.damage * 1.6f), Projectile.knockBack * 1.8f, Projectile.owner);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
                        for (int k = 0; k < 8; k++)
                        {
                            int di = Dust.NewDust(Projectile.Center, 12, 12, DustID.Enchanted_Gold,
                                Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 1f), 170,
                                new Color(200, 255, 200), 1f);
                            Main.dust[di].noGravity = true;
                        }
                        Projectile.active = false;
                        return;
                    }
                }
                // EL HÁLITO de la trampa (una bruma mínima — crece en silencio).
                if (Projectile.timeLeft % 14f < 1f)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(0f, -4f),
                        DustID.ViciousPowder, new Vector2(0f, -0.35f), 140,
                        new Color(185, 245, 190), 0.55f);
                    d.noGravity = true;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            // LA MADRE MUERTA: dos esporas más (la letra viva hasta el final).
            if (EsMadre)
            {
                for (int i = 0; i < 2; i++)
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                        Projectile.Center + new Vector2(i == 0 ? -10f : 10f, 0f), Vector2.Zero,
                        Type, (int)(Projectile.damage * 0.9f), Projectile.knockBack, Projectile.owner,
                        Nivel, 0f, 1f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 pos = Projectile.Center - Main.screenPosition;

            // === LA MADRE: la nebulosa que camina + su cinta verde. ===
            if (EsMadre)
            {
                lightColor = Color.Lerp(lightColor, new Color(215, 255, 215), 0.45f);
                VFXCore.Begin();
                NebulaLib.Nube(Projectile.Center, 34f, time, Projectile.whoAmI * 17 + 15,
                    new Color(185, 255, 190), new Color(120, 200, 140), 0.10f, 10);
                VFXCore.FlushAdditive();
                VFXCore.AbrirAdditive();
                Vector2[] camino = EstelaLib.Track(Projectile.whoAmI, 8).Points();
                if (camino.Length >= 2)
                {
                    for (int i = 0; i < camino.Length; i++)
                        camino[i] -= Main.screenPosition;
                    EstelaLib.Ribbon(Main.spriteBatch, camino, 7f,
                        EstelaProfile.Alive, new Color(185, 255, 190), 0.4f,
                        Projectile.whoAmI * 31 + 35, time, head: true, taper: 60f);
                }
                LumenLib.Bloom(Main.spriteBatch, pos, 26f, new Color(185, 255, 190), 0.5f);
                VFXCore.RestaurarLoteVanilla();
                return true;
            }

            // === LA ESPORA: la nebulosa que RESPIRA LENTO (la trampa
            //     dormida — hz 0,6) + su PERÍMETRO DE DETONO (el anillo
            //     fino de los 90 px que acecha, tenue como el aro del
            //     sello: la trampa avisa su territorio). ===
            float vida = 1f - Projectile.timeLeft / 480f;
            lightColor = Color.Lerp(lightColor, new Color(225, 255, 228), 0.3f + 0.45f * vida);

            VFXCore.Begin();
            NebulaLib.Nube(Projectile.Center, 18f + 26f * vida, time,
                Projectile.whoAmI * 17 + 41, new Color(185, 255, 190),
                new Color(120, 200, 140), 0.08f + 0.05f * vida, 6);
            VFXCore.FlushAdditive();

            VFXCore.AbrirAdditive();
            OrbitaLib.AnilloFino(pos, 90f, time * 0.4f,
                new Color(185, 255, 190) * (0.10f + 0.10f * vida));
            LumenLib.BloomPulse(Main.spriteBatch, pos, 18f + 26f * vida,
                new Color(185, 255, 190), 0.4f + 0.4f * vida, time, 0.6f);
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  13 · EL ECO DEL TRAZO — el látigo que regresa
    //      La letra: «el proyectil deja un trazo de tinta que crece hacia
    //      atrás (la estela se vuelve cuerpo): mientras más lejos viaja,
    //      más grueso es el látigo de tinta que barre a su regreso».
    //      v6.79 EL VESTIDO: el trazo es LA CINTA DE LA CASA (TelaLib —
    //      la cinta con núcleo y perfil Cometa: la estela SE VUELVE
    //      cuerpo, el ancho crece con lo andado), la cabeza del regreso
    //      lleva su flare (viene brillando) y cada golpe del barrido
    //      empuja su onda (Pulse + Kick).
    // ======================================================================
    public class EcoDelTrazoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/Trail";

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 6;
            Projectile.timeLeft = 200;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Etapa => IdeasGrimorio.Etapa(Nivel);
        private float DistTotal => Projectile.ai[1];   // px andados (ida y vuelta)
        private bool _regresando;
        private int _rachaT;    // v6.79: la onda del golpe del barrido
        private Vector2 _rachaPos;

        public override void AI()
        {
            // LA CUENTA de lo andado (el grosor del látigo).
            Projectile.ai[1] += Projectile.velocity.Length();

            // LA IDA (45 t) y EL REGRESO (la letra: «barre a su regreso»).
            if (!_regresando && Projectile.timeLeft <= 200f - 45f)
                _regresando = true;
            if (_regresando)
            {
                Player dueno = Main.player[Projectile.owner];
                if (dueno != null && dueno.active)
                {
                    Vector2 hacia = (dueno.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    float vivo = Etapa >= 2 ? 0.15f : 0.10f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 13f, vivo);
                    if (Vector2.Distance(dueno.Center, Projectile.Center) < 24f)
                    { Projectile.Kill(); return; }
                }
                else Projectile.Kill();
            }

            // EL CUERPO CRECE con la distancia (la letra: «mientras más
            // lejos viaja, más grueso es el látigo»).
            float grosor = MathHelper.Clamp(DistTotal / 800f, 0f, 1f);
            Projectile.scale = 0.8f + grosor * 0.9f;
            Projectile.width = (int)(24 * Projectile.scale);
            Projectile.height = (int)(24 * Projectile.scale);
            Projectile.rotation = Projectile.velocity.ToRotation();

            // v6.79 — LA CINTA DE TINTA (TelaLib: la estela que se vuelve
            //     cuerpo — un empuje por tick, sub-step aware).
            Cinta.Adquirir(Projectile.whoAmI, 30).Empujar(Projectile.Center);
            if (_rachaT > 0) _rachaT--;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // v6.79 — EL GOLPE DEL BARRIDO: la onda en el punto + el kick
            //     suave (el látigo barre y la pantalla lo siente).
            _rachaT = 7;
            _rachaPos = Projectile.Center;
            OndaLib.Kick(2f, 5);
        }

        public override void OnKill(int timeLeft)
        {
            // v6.79 — la cinta se suelta al morir (la tinta vuelve al pool).
            Cinta.Soltar(Projectile.whoAmI);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            float grosor = MathHelper.Clamp(DistTotal / 800f, 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(245, 240, 255), 0.3f + 0.4f * grosor);

            Vector2 pos = Projectile.Center - Main.screenPosition;

            VFXCore.AbrirAdditive();
            // (1) LA CINTA DE TINTA (la estela que se vuelve cuerpo: el
            //     ancho MÁXIMO vive en la CABEZA y el perfil Cometa lo
            //     despliega — más lejos viajó, más grueso el látigo).
            Cinta cinta = Cinta.De(Projectile.whoAmI);
            cinta?.Dibujar(Main.spriteBatch, new Cinta.CintaSpec
            {
                Ancho = 22f + 56f * grosor,
                Perfil = Cinta.PerfilAncho.Cometa,
                ColorCuerpo = new Color(120, 95, 200),
                ColorNúcleo = new Color(225, 218, 255),
                Intensidad = 0.45f + 0.5f * grosor,
                EscalaFase = 1.0f,
                ConNúcleo = true,
                LargoMáximo = 380f,
            });
            // (2) LA CABEZA DEL REGRESO (el flare del que viene barriendo).
            LumenLib.Bloom(Main.spriteBatch, pos, 26f + 34f * grosor,
                new Color(190, 175, 255), 0.4f + 0.4f * grosor);
            if (_regresando)
                LumenLib.Flare(Main.spriteBatch, pos, 30f + 20f * grosor,
                    new Color(190, 175, 255), 0.8f, time * 1.8f);
            // (3) LA ONDA DEL BARRIDO (los 7 ticks del golpe).
            if (_rachaT > 0)
            {
                float pR = 1f - _rachaT / 7f;
                OndaLib.Pulse(Main.spriteBatch, _rachaPos - Main.screenPosition,
                    DefinitivasLib.DeGolpe(pR), 60f + 40f * grosor,
                    new Color(190, 175, 255), 0.7f, Projectile.whoAmI * 7 + 9);
            }
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  14 · LA TINTA VIVA — el snowball con tope
    //      La letra: «una gota de tinta semitransparente que se aviva con
    //      cada enemigo que toca (cada golpe la hace más grande y más
    //      opaca) — un snowball controlado con tope».
    //      v6.79 EL VESTIDO: la gota es una NEBULOSA que se aviva (la
    //      nube morada gana alpha y radio con cada toque), el toque
    //      enciende su pulso (BloomPulse hz 4 — la gota que CHISPEA al
    //      morder) y la muerte estalla con el violeta de la tinta.
    // ======================================================================
    public class TintaVivaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/RuidoTinta";

        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 8;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Etapa => IdeasGrimorio.Etapa(Nivel);
        private int Toques => (int)Projectile.ai[1];
        private int _aviveT;   // v6.79: el pulso del toque

        public override void AI()
        {
            // EL CUERPO CON TOPE (la letra: «snowball controlado con
            // tope»): ×2 de tamaño y casi opaca a los 12 toques.
            float t = MathHelper.Clamp(Toques / 12f, 0f, 1f);
            Projectile.scale = 0.5f + t * 1.5f;
            Projectile.width = (int)(26 * Projectile.scale);
            Projectile.height = (int)(26 * Projectile.scale);
            Projectile.rotation += 0.03f + t * 0.04f;
            if (_aviveT > 0) _aviveT--;
            // LA GOTA GOTEA (la viva deja rastro — la E4 lo engorda).
            if (Etapa >= 4 || Main.rand.NextBool(4))
            {
                int di = Dust.NewDust(Projectile.Center - new Vector2(4f, 4f), 8, 8,
                    DustID.Shadowflame, 0f, 0f, 150,
                    new Color(130, 105, 215), 0.5f + t * 0.5f);
                Main.dust[di].noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // SE AVIVA (la letra: «cada golpe la hace más grande y más
            // opaca»): +12 % de cuerpo y +8 % de daño por toque.
            if (Toques < 12) Projectile.ai[1]++;
            _aviveT = 8;   // v6.79: el pulso del mordisco
            Projectile.damage = (int)(Projectile.damage * 1.08f);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            // v6.79 — LA MANCHA FINAL: el AoE morado pequeño (la gota
            //     muerta revienta su tinta).
            CrecimientoEstallidoProjectile.TinteProximo = new Color(150, 130, 230);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                (int)(Projectile.damage * 0.8f), Projectile.knockBack, Projectile.owner);
            // LA MANCHA: la gota muerta deja su tinta en el suelo.
            for (int i = 0; i < 6; i++)
            {
                int di = Dust.NewDust(Projectile.Center, 14, 14, DustID.Shadowflame,
                    Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1f, 0.5f), 150,
                    new Color(120, 95, 205), 0.8f);
                Main.dust[di].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            // MÁS OPACA con cada toque (0,35 → 0,95 — la letra).
            float t = MathHelper.Clamp(Toques / 12f, 0f, 1f);
            float opacidad = 0.35f + t * 0.60f;
            lightColor = new Color(150, 130, 230) * opacidad;
            lightColor.A = (byte)(255 * MathHelper.Clamp(opacidad, 0.1f, 1f));

            Vector2 pos = Projectile.Center - Main.screenPosition;

            // (1) LA NEBULOSA QUE SE AVIVA (la gota nebulosa: el alpha y
            //     el radio ganan con cada toque — la tinta se CONDENSA).
            VFXCore.Begin();
            NebulaLib.Nube(Projectile.Center, 30f + 40f * t, time,
                Projectile.whoAmI * 17 + 47, new Color(130, 105, 215),
                new Color(90, 70, 170), 0.05f + 0.10f * t, 8 + (int)(8f * t));
            VFXCore.FlushAdditive();

            // (2) EL PULSO DEL TOQUE (los 8 ticks — la gota que CHISPEA
            //     al morder) + el bloom que gana con la condensación.
            VFXCore.AbrirAdditive();
            if (_aviveT > 0)
                LumenLib.BloomPulse(Main.spriteBatch, pos, 30f + 50f * t,
                    new Color(190, 175, 255), 1.1f, time, 4f);
            else
                LumenLib.Bloom(Main.spriteBatch, pos, 24f + 40f * t,
                    new Color(150, 130, 230), 0.3f + 0.5f * t);
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }
}
