using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    /// <summary>
    /// RuneSunStaffBase — v6.19 — LA FAMILIA DE LOS SOLES RÚNICOS.
    ///
    /// DIEZ COPIAS DEL SOL (petición del usuario: "creen varias copias del
    /// sol y a estas copias ponle anillos con runas — la primera un solo
    /// anillo, la segunda 2 anillos en direcciones distintas, la tercera 3
    /// y así hasta 10; cada copia mejorada un poquito más, hasta la copia
    /// 10 con muchas mejoras y animaciones. El sol original no se toca").
    ///
    /// Cada bastón dispara RuneSunProjectile con su copia en ai[0]:
    /// el NÚMERO de anillos rúnicos = el número de la copia. Daño, vida,
    /// aura y nova escalan con ella.
    /// </summary>
    public abstract class RuneSunStaffBase : ModItem
    {
        /// <summary>La copia (1..10): N anillos rúnicos, N-ésima mejora.</summary>
        protected abstract int Tier { get; }

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            int tier = Tier;
            Item.damage = 80 + 24 * (tier - 1);          // 80 → 536
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RuneSunProjectile>();
            Item.shootSpeed = 6f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.value = 1000 * tier;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // ai[0] = la copia (los anillos rúnicos que vestirá).
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback,
                player.whoAmI, Tier, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // v6.18: TOOLTIP CORTO — dos líneas, el nombre vive arriba.
            int tier = Tier;
            string anillos = $"{tier} anillos rúnicos";
            string mejora = tier switch
            {
                >= 20 => "cometa + lluvia rúnica + aurora polar + compañera azul + cinturón de asteroides + tormenta total + corona y lanzas prismáticas + corazón de nova + EL GRAN SELLADO",
                >= 17 => "cometa + lluvia rúnica + aurora polar + compañera azul + cinturón de asteroides + tormenta total + corona prismática",
                >= 14 => "cometa + lluvia rúnica + aurora polar + estrella compañera",
                >= 11 => "cometa + lluvia rúnica + aurora polar",
                _ => "aura ardiente · persigue enemigos · gigante final · una sola nova",
            };
            tooltips.Add(new TooltipLine(Mod, "D",
                $"[c/FFD080:Copia {tier} del Sol — {anillos} en planos orbitales con giros alternos]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                $"[c/78788C:{mejora}]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // =====================================================================
    //  LAS DIEZ COPIAS
    // =====================================================================

    /// <summary>Copia 1 — UN anillo rúnico: el sello simple.</summary>
    public class SolRunico1Staff : RuneSunStaffBase { protected override int Tier => 1; }

    /// <summary>Copia 2 — DOS anillos girando en direcciones OPUESTAS.</summary>
    public class SolRunico2Staff : RuneSunStaffBase { protected override int Tier => 2; }

    /// <summary>Copia 3 — TRES anillos + destellos de 4 puntas.</summary>
    public class SolRunico3Staff : RuneSunStaffBase { protected override int Tier => 3; }

    /// <summary>Copia 4 — CUATRO anillos + prominencias de plasma.</summary>
    public class SolRunico4Staff : RuneSunStaffBase { protected override int Tier => 4; }

    /// <summary>Copia 5 — CINCO anillos + viento solar.</summary>
    public class SolRunico5Staff : RuneSunStaffBase { protected override int Tier => 5; }

    /// <summary>Copia 6 — SEIS anillos + rayos fugitivos entre órbitas.</summary>
    public class SolRunico6Staff : RuneSunStaffBase { protected override int Tier => 6; }

    /// <summary>Copia 7 — SIETE anillos + precesión + acentos azul-estelar.</summary>
    public class SolRunico7Staff : RuneSunStaffBase { protected override int Tier => 7; }

    /// <summary>Copia 8 — OCHO anillos + núcleo pulsante y ondas de eco.</summary>
    public class SolRunico8Staff : RuneSunStaffBase { protected override int Tier => 8; }

    /// <summary>Copia 9 — NUEVE anillos + corona de pétalos de plasma.</summary>
    public class SolRunico9Staff : RuneSunStaffBase { protected override int Tier => 9; }

    /// <summary>Copia 10 — DIEZ anillos: el SISTEMA COMPLETO (erupción
    /// rúnica + jets polares + todas las mejoras a máxima potencia).</summary>
    public class SolRunico10Staff : RuneSunStaffBase { protected override int Tier => 10; }

    // =====================================================================
    //  v6.22 — LA SEGUNDA DÉCADA (11..20)
    // =====================================================================

    /// <summary>Copia 11 — ONCE anillos + COMETA ORBITAL con cola.</summary>
    public class SolRunico11Staff : RuneSunStaffBase { protected override int Tier => 11; }

    /// <summary>Copia 12 — DOCE anillos + LLUVIA DE RUNAS cayendo al sol.</summary>
    public class SolRunico12Staff : RuneSunStaffBase { protected override int Tier => 12; }

    /// <summary>Copia 13 — TRECE anillos + AURORA POLAR prismática.</summary>
    public class SolRunico13Staff : RuneSunStaffBase { protected override int Tier => 13; }

    /// <summary>Copia 14 — CATORCE anillos + ESTRELLA COMPAÑERA azul con puente de luz.</summary>
    public class SolRunico14Staff : RuneSunStaffBase { protected override int Tier => 14; }

    /// <summary>Copia 15 — QUINCE anillos + CINTURÓN DE ASTEROIDES con brecha.</summary>
    public class SolRunico15Staff : RuneSunStaffBase { protected override int Tier => 15; }

    /// <summary>Copia 16 — DIECISÉIS anillos + TORMENTA TOTAL: multi-boltos + arco corona.</summary>
    public class SolRunico16Staff : RuneSunStaffBase { protected override int Tier => 16; }

    /// <summary>Copia 17 — DIECISIETE anillos + CORONA PRISMÁTICA de rayos de luz.</summary>
    public class SolRunico17Staff : RuneSunStaffBase { protected override int Tier => 17; }

    /// <summary>Copia 18 — DIECIOCHO anillos + LANZAS PRISMÁTICAS orbitando.</summary>
    public class SolRunico18Staff : RuneSunStaffBase { protected override int Tier => 18; }

    /// <summary>Copia 19 — DIECINUEVE anillos + NÚCLEO DE NUEVA latiendo a estallido.</summary>
    public class SolRunico19Staff : RuneSunStaffBase { protected override int Tier => 19; }

    /// <summary>Copia 20 — VEINTE anillos: EL SISTEMA SUPREMO con el GRAN
    /// SELLADO (los 8 glifos maestros + contrasello retrógrado).</summary>
    public class SolRunico20Staff : RuneSunStaffBase { protected override int Tier => 20; }
}
