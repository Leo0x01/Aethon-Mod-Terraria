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
    // ======================================================================

    // ======================================================================
    //  8 · LA ESTRELLA DOMÉSTICA — la caída que alimenta
    //      E1: nace arriba y cae al punto · E2: cae más rápido · E3: el
    //      estallido llega más lejos · E4: la estrella brilla TODO el camino.
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
            Lighting.AddLight(Projectile.Center, new Vector3(0.55f, 0.5f, 0.3f) * (0.4f + prog));

            // EL ESTALLIDO EN EL PUNTO EXACTO (la letra: «estalla en el
            // punto exacto» — precisión pura).
            if (dist < 15f) Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            int dano = (int)(Projectile.damage * 1.5f);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                dano, Projectile.knockBack * 1.6f, Projectile.owner);
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
            float prog = MathHelper.Clamp(1f - Vector2.Distance(Projectile.Center, Punto) / MathF.Max(_dist0, 1f), 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(255, 252, 240), 0.4f + 0.4f * prog);
            VFXCore.AbrirAdditive();
            VFXCore.Begin();
            float rGlow = 14f + 36f * prog * (Etapa >= 4 ? 1.3f : 1f);
            VFXCore.Quad(Projectile.Center, new Color(255, 240, 205) * (0.25f + 0.4f * prog),
                new Vector2(rGlow, rGlow));
            VFXCore.FlushAdditive();
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  9 · LA PÁGINA CORTANTE — la daga que crece rebotando
    //      E1: rebota y crece (hasta ×3) · E2: el daño por rebote sube a
    //      ×1,5 · E3: 5 rebotes · E4: la hoja deja tinta en cada muro.
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

        public override void AI()
        {
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

            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(4))
            {
                int di = Dust.NewDust(Projectile.Center - new Vector2(3f, 3f), 6, 6,
                    DustID.Enchanted_Gold, 0f, 0f, 170, new Color(235, 230, 255), 0.7f);
                Main.dust[di].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, new Color(255, 252, 245), 0.3f + 0.12f * Math.Min(Rebotes, 3));
            return true;
        }
    }

    // ======================================================================
    //  10 · EL PRISMA FRAGMENTADO — la fusión que golpea
    //      El NUEVO fragmento divide al ANTERIOR en dos menores que se
    //      RE-FUSIONAN en el nuevo: uno MAYOR que golpea.
    //      E2: los mini vuelan más vivos · E3: el AoE de la fusión completa
    //      · E4: al fundirse, destello.
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

        public override void AI()
        {
            // EL DAÑO BASE (para que la fusión crezca desde la raíz).
            if (_danoBase == 0) _danoBase = Math.Max(1, Projectile.damage);

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
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                    (int)(Projectile.damage * 1.3f), Projectile.knockBack * 1.8f, Projectile.owner);
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fundido = MathHelper.Clamp(Fusion / 2.4f, 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(235, 255, 250), 0.35f + 0.45f * fundido);
            VFXCore.AbrirAdditive();
            VFXCore.Begin();
            float rGlow = 15f + 30f * fundido;
            VFXCore.Quad(Projectile.Center,
                (Modo == 1f ? new Color(170, 230, 225) : new Color(120, 220, 210)) * (0.25f + 0.35f * fundido),
                new Vector2(rGlow, rGlow));
            VFXCore.FlushAdditive();
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  11 · LA RUNA ORBITAL — los glifos que esperan
    //      3-5 glifos orbitan el libro (inertes, visibles en idle); al
    //      clicar, el MÁS VIEJO sale disparado hacia el cursor (activo).
    //      Los glifos crecen con el tiempo de órbita.
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
            float madurez = MathHelper.Clamp(Projectile.ai[1] / 600f, 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(255, 240, 210), 0.35f + 0.4f * madurez);
            VFXCore.AbrirAdditive();
            VFXCore.Begin();
            float rGlow = Activo ? 26f : 12f + 16f * madurez;
            VFXCore.Quad(Projectile.Center,
                new Color(255, 225, 175) * (Activo ? 0.5f : 0.22f + 0.2f * madurez),
                new Vector2(rGlow, rGlow));
            VFXCore.FlushAdditive();
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  12 · LA ESPORA DEL ESCRIBA — las trampas vivas
    //      La madre lenta planta esporas en su camino; las esporas crecen
    //      en silencio y DETONAN al acercarse un enemigo.
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
            if (EsMadre)
                lightColor = Color.Lerp(lightColor, new Color(215, 255, 215), 0.45f);
            else
            {
                float vida = 1f - Projectile.timeLeft / 480f;
                lightColor = Color.Lerp(lightColor, new Color(225, 255, 228), 0.3f + 0.45f * vida);
            }
            return true;
        }
    }

    // ======================================================================
    //  13 · EL ECO DEL TRAZO — el látigo que regresa
    //      La letra: «el proyectil deja un trazo de tinta que crece hacia
    //      atrás (la estela se vuelve cuerpo): mientras más lejos viaja,
    //      más grueso es el látigo de tinta que barre a su regreso».
    //      El trazo se dibuja con la textura RuidoTinta (la tinta de la
    //      casa) sobre el lote del pase de entidades.
    // ======================================================================
    public class EcoDelTrazoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/Trail";

        /// <summary>LA TINTA de la cinta (cacheada — la textura de la casa).</summary>
        private static Texture2D _tinta;
        private static Texture2D Tinta =>
            _tinta ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/RuidoTinta").Value;

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
        /// <summary>La RUTA viva (los puntos de la cinta) — la estela que se vuelve cuerpo.</summary>
        private readonly List<Vector2> _ruta = new List<Vector2>(64);
        private bool _regresando;

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

            // LA RUTA: cada 3 t un punto (la cinta que se vuelve cuerpo).
            if (Projectile.timeLeft % 3f < 1f)
            {
                _ruta.Add(Projectile.Center);
                if (_ruta.Count > 22) _ruta.RemoveAt(0);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // === LA CINTA DE TINTA (la estela se vuelve cuerpo): los
            //     puntos de la ruta con RuidoTinta — el MÁS VIEJO el más
            //     GRUESO (crece hacia atrás, la letra) y el cuerpo delante
            //     (el látigo que barre). Coords de pantalla sobre el pase
            //     de entidades (EntitySpriteDraw — el patrón de la casa).
            //     ===
            float grosor = MathHelper.Clamp(DistTotal / 800f, 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(245, 240, 255), 0.3f + 0.4f * grosor);
            for (int i = 0; i < _ruta.Count; i++)
            {
                // i=0 (el más viejo) → grueso; el último (yo) → fino.
                float f = 1f - (float)i / _ruta.Count;                  // 1 → 0
                float esc = (0.35f + 1.05f * f) * (0.7f + 0.6f * grosor);
                float alfa = 0.25f + 0.45f * f * (0.6f + 0.4f * grosor);
                Main.EntitySpriteDraw(Tinta,
                    _ruta[i] - Main.screenPosition, null,
                    new Color(120, 95, 200) * alfa,
                    (i > 0 ? (_ruta[i] - _ruta[i - 1]).ToRotation() : Projectile.rotation),
                    Tinta.Size() * 0.5f, new Vector2(esc, esc * 0.7f),
                    SpriteEffects.None, 0f);
            }
            // EL CUERPO brilla (la cabeza del látigo de regreso).
            VFXCore.AbrirAdditive();
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, new Color(190, 175, 255) * (0.25f + 0.3f * grosor),
                new Vector2(18f + 26f * grosor, 18f + 26f * grosor));
            VFXCore.FlushAdditive();
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  14 · LA TINTA VIVA — el snowball con tope
    //      La letra: «una gota de tinta semitransparente que se aviva con
    //      cada enemigo que toca (cada golpe la hace más grande y más
    //      opaca) — un snowball controlado con tope».
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

        public override void AI()
        {
            // EL CUERPO CON TOPE (la letra: «snowball controlado con
            // tope»): ×2 de tamaño y casi opaca a los 12 toques.
            float t = MathHelper.Clamp(Toques / 12f, 0f, 1f);
            Projectile.scale = 0.5f + t * 1.5f;
            Projectile.width = (int)(26 * Projectile.scale);
            Projectile.height = (int)(26 * Projectile.scale);
            Projectile.rotation += 0.03f + t * 0.04f;
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
            Projectile.damage = (int)(Projectile.damage * 1.08f);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
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
            // MÁS OPACA con cada toque (0,35 → 0,95 — la letra).
            float opacidad = 0.35f + MathHelper.Clamp(Toques / 12f, 0f, 1f) * 0.60f;
            lightColor = new Color(150, 130, 230) * opacidad;
            lightColor.A = (byte)(255 * MathHelper.Clamp(opacidad, 0.1f, 1f));
            return true;
        }
    }
}
