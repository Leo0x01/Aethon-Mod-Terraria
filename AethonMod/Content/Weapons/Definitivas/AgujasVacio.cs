using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  v6.122 — LAS AGUJAS DEL VACÍO (la letra del usuario: «crea otra
    //  arma con agujas delgadas que atraviezan y se clavan en los
    //  enemigos, estas aparecen siempre cerca del enemigo»).
    //
    //  EL COSTURERO DEL VACÍO — HASTA TRES AGUJAS por uso, siempre
    //  CERCA de los enemigos (los más cercanos al cursor): cada aguja
    //  nace a 90-140 px de su presa y VUELA HACIA ELLA — delgada,
    //  veloz, con la PUNTA BLANCA que abre la tela del mundo.
    //
    //  EL CLAVADO (la letra): la aguja ATRAVIESA a su presa (perfora) y
    //  SE QUEDA CLAVADA — viva dentro del cuerpo, siguiéndolo donde
    //  camine y sangrando el vacío (un goteo de daño cada 15 t, ~3
    //  tandas), hasta disolverse en motas violetas. Si la presa muere,
    //  la aguja sigue su vuelo (busca la siguiente tela).
    // ======================================================================
    public class AgujasVacio : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 200;
            Item.DamageType = DamageClass.Magic;
            Item.width = 44; Item.height = 44;
            Item.useTime = 11; Item.useAnimation = 11;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<AgujaVacioProj>();
            Item.shootSpeed = 1f;
            Item.mana = 8;
            Item.ArmorPenetration = 120;
            Item.rare = ItemRarityID.Purple;
            Item.value = 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // HASTA TRES PRESAS: los enemigos más cercanos al cursor
            // (radio 700) — las agujas nacen CERCA de ellos (la letra).
            int tres = 0;
            for (int pasada = 0; pasada < 3 && tres < 3; pasada++)
            {
                NPC masCerca = null;
                float mejor = float.MaxValue;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy() || n.friendly ||
                        yaMarcada(n)) continue;
                    float d = n.Center.Distance(Main.MouseWorld);
                    if (d < 700f && d < mejor) { mejor = d; masCerca = n; }
                }
                if (masCerca == null) break;
                _marcadas[tres++] = masCerca.whoAmI;

                // LA AGUJA NACE CERCA (90-140 px, ángulo con aire) y VUELA
                // A LA PRESA — delgada y certera.
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                float lejos = Main.rand.NextFloat(90f, 140f);
                Vector2 nace = masCerca.Center + ang.ToRotationVector2() * lejos;
                Vector2 vuelo = masCerca.Center - nace;
                if (vuelo.LengthSquared() < 1f) vuelo = -Vector2.UnitY;
                vuelo = Vector2.Normalize(vuelo) * 34f;
                // (el 20 % de las agujas va con aire: el costurero distrae)
                vuelo = vuelo.RotatedByRandom(0.09f);
                Projectile.NewProjectile(source, nace, vuelo, type,
                    damage, knockback, player.whoAmI);
            }
            return false;
        }

        // LA MEMORIA DEL TIRO (para no marcar tres veces al mismo NPC)
        private static readonly int[] _marcadas = new int[3];

        private static bool yaMarcada(NPC n)
        {
            for (int k = 0; k < 3; k++)
                if (_marcadas[k] == n.whoAmI) return true;
            return false;
        }

        public override bool CanShoot(Player player)
        {
            _marcadas[0] = _marcadas[1] = _marcadas[2] = -1;
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  AgujaVacioProj — LA AGUJA DEL VACÍO: delgada, atraviesa, se clava
    //  y sangra el vacío (punta blanca · cuerpo violeta · ojo dorado).
    // ======================================================================
    public class AgujaVacioProj : ModProjectile
    {
        // EL SPRITE DE LA AGUJA — la receta `aguja` de la SForja
        // (44×12: punta blanca · cuerpo violeta · ojo dorado)
        public override string Texture => "AethonMod/Content/Effects/Procedural/AgujaVacioProj";

        private const int T_CLAVADA = 46;

        /// <summary>¿Está clavada en una presa?</summary>
        private bool _clavada;

        /// <summary>La presa (mientras viva y activa).</summary>
        private int _presa = -1;

        /// <summary>El agujero de la aguja dentro del cuerpo (offset).</summary>
        private Vector2 _offset;

        /// <summary>El goteo del clavado (cada 15 t, ~3 tandas).</summary>
        private float _goteo;

        /// <summary>v6.124 — LA DIRECCIÓN DEL VUELO en el momento del
        /// clavado (la aguja queda apuntando HACIA DENTRO del cuerpo
        /// — la vieja usaba el offset y la aguja miraba PARA AFUERA).</summary>
        private float _rotVuelo;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;         // ATRAVIESA (la letra)
            Projectile.timeLeft = 160;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // === CLAVADA: la aguja VIVE dentro de la presa ===
            if (_clavada)
            {
                NPC presa = _presa >= 0 && _presa < Main.maxNPCs ? Main.npc[_presa] : null;
                if (presa == null || !presa.active || presa.life <= 0)
                {
                    // LA PRESA MURIÓ: la aguja sigue su vuelo (busca la
                    // siguiente tela del costurero).
                    _clavada = false;
                    _presa = -1;
                    if (Projectile.velocity.LengthSquared() < 1f)
                        Projectile.velocity = -Vector2.UnitY.RotatedByRandom(0.6f) * 20f;
                }
                else
                {
                    // LA AGUJA CABALGA al cuerpo donde vive.
                    Projectile.Center = presa.Center + _offset;
                    Projectile.velocity = Vector2.Zero;
                    // EL GOTEO DEL VACÍO: la herida sangra morado.
                    if (++_goteo % 15 == 0 && Main.netMode != NetmodeID.MultiplayerClient &&
                        !presa.immortal)
                    {
                        int dano = (int)(Projectile.damage * 0.25f);
                        if (dano < 1) dano = 1;
                        presa.SimpleStrikeNPC(dano, _offset.X < 0f ? 1 : -1,
                            false, Projectile.knockBack * 0.2f);
                    }
                    if (_goteo >= T_CLAVADA)
                    {
                        // LA DISOLUCIÓN: la aguja se evapora en motas.
                        if (!Main.dedServ)
                            for (int k = 0; k < 8; k++)
                            {
                                Dust d = Dust.NewDustPerfect(Projectile.Center,
                                    DustID.Shadowflame,
                                    Main.rand.NextVector2Circular(2.4f, 2.4f), 0,
                                    new Color(150, 84, 235), 0.9f);
                                d.noGravity = true;
                            }
                        Projectile.Kill();
                        return;
                    }
                    if (!Main.dedServ && Main.rand.NextBool(4))
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                            Main.rand.NextVector2Circular(0.7f, 0.7f), 0,
                            new Color(200, 150, 255), 0.7f);
                        d.noGravity = true;
                    }
                }
            }
            // === VUELO: delgada y veloz ===
            else
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
                if (!Main.dedServ && Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                        -Projectile.velocity * 0.08f, 0, new Color(96, 54, 170), 0.6f);
                    d.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // EL CLAVADO (la letra): la aguja se QUEDA DENTRO.
            _clavada = true;
            _presa = target.whoAmI;
            _offset = Projectile.Center - target.Center;
            _rotVuelo = Projectile.velocity.ToRotation();   // v6.124 — apuntando hacia dentro
            _goteo = 0;
            // EL CHASQUIDO del clavado.
            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig.WithPitchOffset(0.6f),
                    Projectile.Center);
                for (int k = 0; k < 6; k++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                        Main.rand.NextVector2Circular(2.6f, 2.6f), 0,
                        new Color(220, 190, 255), 0.9f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                // v6.124 — LA AGUJA ES SU SPRITE DE LA SFORJA (la letra:
                // «todo proyectil debe ser creado con la superforja»):
                // punta blanca · cuerpo violeta · ojo dorado — dibujada
                // en el lote ALFA (el cuerpo es CUERPO, no luz) con el
                // aura aditiva de la casa detrás.
                Vector2 pos = Projectile.Center - Main.screenPosition;
                float rot = _clavada && _presa >= 0 ? _rotVuelo : Projectile.velocity.ToRotation();

                Main.instance.LoadProjectile(Projectile.type);
                Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
                if (tex != null)
                {
                    Vector2 origen = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);
                    // LA AGUJA (el sprite — la escala entera, legible)
                    Main.spriteBatch.Draw(tex, pos, null, Color.White, rot,
                        origen, 1f, SpriteEffects.None, 0f);
                }

                // LA AURA tenue del vuelo (aditiva — la aguja destella)
                VFXCore.AbrirAdditive();
                try
                {
                    if (tex != null)
                    {
                        Vector2 origen = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);
                        Main.spriteBatch.Draw(tex, pos, null,
                            new Color(150, 84, 235) * 0.30f, rot, origen,
                            new Vector2(1.7f, 2.3f), SpriteEffects.None, 0f);
                    }
                }
                finally { VFXCore.RestaurarLoteVanilla(); }
            }
            catch { }
            return false;
        }
    }
}
