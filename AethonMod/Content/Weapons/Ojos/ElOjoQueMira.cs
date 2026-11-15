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
        public override string Texture => "AethonMod/Content/Projectiles/OjoEterno_Abierto";

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

        private const int CARGA = 16;      // ticks mirando (el iris se abre)
        private const int VIDA = 46;       // el ojo vive poco: mira y se va

        private int _tick;
        private Vector2 _presa;           // a quién mira
        private bool _disparado;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
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

            // LA FLOTACIÓN (el bobbing del ojo de la casa)
            Projectile.velocity.Y = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.2f
                + Projectile.whoAmI) * 0.35f;

            // EL DISPARO: tras la carga, LA MIRADA
            if (_tick == CARGA && !_disparado)
            {
                _disparado = true;
                Vector2 dir = _presa - Projectile.Center;
                if (dir.LengthSquared() > 1f) dir.Normalize();

                // EL RAYO: la mirada atraviesa TODO en su línea
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, dir * 26f,
                    ModContent.ProjectileType<LaMiradaRayo>(),
                    Projectile.damage, Projectile.knockBack, Projectile.owner);

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

            // LA DESPEDIDA: tras el disparo el ojo se disipa (parpadeo)
            if (_tick > CARGA + 14)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // EL OJO DE LOS MINIONS (la letra): la misma textura, la
            // CARGA la abre (el brillo crece con _tick/CARGA).
            Texture2D tex = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
            float f = Math.Min(1f, (float)_tick / CARGA);
            float esc = 0.9f + 0.25f * f;
            var pos = Projectile.Center - Main.screenPosition;
            // el resplandor de la carga (violeta→dorado al abrirse)
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            Color cGlow = Color.Lerp(new Color(150, 84, 235), new Color(255, 200, 90), f);
            Main.spriteBatch.Draw(glow, pos, null, cGlow * (0.35f + 0.3f * f),
                0f, glow.Size() * 0.5f, 1.2f + 0.6f * f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(tex, pos, null,
                Microsoft.Xna.Framework.Color.White * (0.85f + 0.15f * f),
                0f, tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);
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
    /// LaMiradaRayo — EL RAYO DE LA MIRADA: corto, atravesante, rápido.
    /// TODO lo que toca recibe LA MARCA SILENCIOSA (el glifo) y la
    /// QUEMADURA DEL VACÍO (el debuff de la casa — en PvP el jugador
    /// enemigo ARDE: la letra).
    /// </summary>
    public class LaMiradaRayo : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int LARGO = 340;      // el alcance de la mirada (px)

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;      // ATRAVIESA todo
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.timeLeft = 18;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // LA ESTELA (las motas violeta-doradas de la casa)
            if (Main.rand.NextBool(2))
            {
                Terraria.Dust d = Terraria.Dust.NewDustPerfect(Projectile.Center,
                    Terraria.ID.DustID.Enchanted_Pink,
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    140, new Color(200, 150, 255), 0.7f);
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // EL RAYO LARGO: la línea delante del proyectil (el vuelo
            // corto + el rastro = la mirada completa)
            Vector2 dir = Projectile.velocity;
            if (dir.LengthSquared() < 0.01f) return null;
            dir.Normalize();
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(),
                targetHitbox.Size(), Projectile.Center - dir * (LARGO * 0.5f),
                Projectile.Center + dir * (LARGO * 0.5f), 10f, ref _);
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
            // LA MIRADA: la línea violeta-dorada con su núcleo claro
            Vector2 dir = Projectile.velocity;
            if (dir.LengthSquared() < 0.01f) return false;
            dir.Normalize();
            var pos = Projectile.Center - Main.screenPosition;
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            float vida = Projectile.timeLeft / 18f;

            for (int k = 0; k < 12; k++)
            {
                float f = k / 11f;
                var p = pos - dir * (LARGO * 0.5f) + dir * (LARGO * f);
                float esc = 0.5f + 0.4f * (float)Math.Sin(f * Math.PI);
                Color c = Color.Lerp(new Color(150, 84, 235), new Color(255, 210, 120), f);
                Main.spriteBatch.Draw(glow, p, null, c * 0.55f * vida,
                    0f, glow.Size() * 0.5f, esc * 1.4f, SpriteEffects.None, 0f);
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
