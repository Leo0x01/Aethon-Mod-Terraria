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
    /// RealStarStaffBase — v6.26 — LAS ESTRELLAS REALES.
    ///
    /// Petición del usuario: "crea nuevas variantes de soles basado en
    /// estrellas reales, como estrellas de neutrones, pulsares, enanas
    /// blancas, estrellas muertas, entre otras variantes".
    ///
    /// ESTA FAMILIA NO ES LA DE LOS SOLES RÚNICOS: no hay tiers ni
    /// anillos que se apilan — cada bastón es una CLASE de estrella con
    /// su FÍSICA y su RENDER propios (astrofísica estilizada de la casa):
    /// la estrella de neutrones ultradensa, el púlsar-faro, la enana
    /// blanca cristalina, la enana negra muerta, la supergigante roja
    /// que colapsa antes de la nova y el magnetar tormentoso.
    ///
    /// Cada subclase declara: daño (180..320 según potencia astrológica),
    /// useTime (45..60), el proyectil que dispara y sus DOS líneas de
    /// tooltip (identidad astrológica + mecánica).
    /// </summary>
    public abstract class RealStarStaffBase : ModItem
    {
        /// <summary>Daño del bastón (la potencia de su clase de estrella).</summary>
        protected abstract int StarDamage { get; }

        /// <summary>useTime/useAnimation (45..60 según el ritmo estelar).</summary>
        protected abstract int StarUseTime { get; }

        /// <summary>El proyectil-clase-de-estrella que dispara.</summary>
        protected abstract int StarProjectile { get; }

        /// <summary>Línea 1 — la IDENTIDAD astrológica (con color propio).</summary>
        protected abstract string StarIdentity { get; }

        /// <summary>Línea 2 — la MECÁNICA única (gris de la casa).</summary>
        protected abstract string StarMechanic { get; }

        /// <summary>Color hexadecimal del tooltip de identidad.</summary>
        protected virtual string IdentityColor => "FFD080";

        /// <summary>Velocidad de disparo (deriva inicial del proyectil).</summary>
        protected virtual float StarShootSpeed => 6f;

        /// <summary>Valor de venta (cofre de quest cósmico).</summary>
        protected virtual int StarValue => 25000;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = StarDamage;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = StarUseTime;
            Item.useAnimation = StarUseTime;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = StarProjectile;
            Item.shootSpeed = StarShootSpeed;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.value = StarValue;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Los proyectiles gestionan su propia semilla en OnSpawn;
            // aquí solo les damos el impulso inicial (deriva lenta).
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // v6.18: TOOLTIP CORTO — dos líneas, el nombre vive arriba.
            tooltips.Add(new TooltipLine(Mod, "StarIdentity",
                $"[c/{IdentityColor}:{StarIdentity}]"));
            tooltips.Add(new TooltipLine(Mod, "StarMechanic",
                $"[c/78788C:{StarMechanic}]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // =====================================================================
    //  LAS SEIS CLASES DE ESTRELLAS REALES
    // =====================================================================

    /// <summary>
    /// EL BASTÓN DE LA ESTRELLA DE NEUTRONES — el cadáver comprimido de
    /// un sol masivo: ~12 px de núcleo con más masa que el sistema entero.
    /// </summary>
    public class NeutronStarStaff : RealStarStaffBase
    {
        protected override int StarDamage => 240;
        protected override int StarUseTime => 45;
        protected override int StarProjectile => ModContent.ProjectileType<NeutronStarProjectile>();
        protected override string IdentityColor => "BEE4FF";
        protected override string StarIdentity =>
            "Estrella de neutrones — el cadáver ultradenso de un sol masivo";
        protected override string StarMechanic =>
            "daño ENORME en radio minúsculo (60 px, ×3) · gravita a los enemigos · " +
            "starquakes con onda de choque · vida corta (4 s)";
    }

    /// <summary>
    /// EL BASTÓN DEL PÚLSAR — la estrella de neutrones GIRANDO con sus dos
    /// haces polares barriendo el mundo como un faro de 400 px.
    /// </summary>
    public class PulsarStaff : RealStarStaffBase
    {
        protected override int StarDamage => 280;
        protected override int StarUseTime => 50;
        protected override int StarProjectile => ModContent.ProjectileType<PulsarProjectile>();
        protected override string IdentityColor => "8CB9FF";
        protected override string StarIdentity =>
            "Púlsar — el faro cósmico: dos haces polares girando a 1 rev/s";
        protected override string StarMechanic =>
            "cada barrido del haz golpea ×1.5 (0.5 s de recarga por enemigo) · " +
            "pulso de luz sincronizado · vida 6 s";
    }

    /// <summary>
    /// EL BASTÓN DE LA ENANA BLANCA — el rescoldo cristalino: pequeña,
    /// densa, blanco-azul, robando masa a quien se le acerca.
    /// </summary>
    public class WhiteDwarfStaff : RealStarStaffBase
    {
        protected override int StarDamage => 180;
        protected override int StarUseTime => 50;
        protected override int StarProjectile => ModContent.ProjectileType<WhiteDwarfProjectile>();
        protected override string IdentityColor => "D2E6FF";
        protected override string StarIdentity =>
            "Enana blanca — el rescoldo cristalino de un sol extinto";
        protected override string StarMechanic =>
            "aura constante moderada (90 px) · anillo de acreción que roba masa cerca de enemigos · " +
            "deriva lenta · vida 8 s";
    }

    /// <summary>
    /// EL BASTÓN DE LA ESTRELLA MUERTA — la enana negra: un núcleo oscuro
    /// que apaga la luz a su alrededor y deshace el mundo por entropía.
    /// </summary>
    public class DeadStarStaff : RealStarStaffBase
    {
        protected override int StarDamage => 200;
        protected override int StarUseTime => 60;
        protected override int StarProjectile => ModContent.ProjectileType<DeadStarProjectile>();
        protected override string IdentityColor => "96787C";
        protected override string StarIdentity =>
            "Estrella muerta — la enana negra: un sol que agotó hasta su último fotón";
        protected override string StarMechanic =>
            "entropía: 8 p/s de daño en 120 px · aura que oscurece el mundo · " +
            "brasas frías y ecos rúnicos · vida 10 s";
    }

    /// <summary>
    /// EL BASTÓN DE LA SUPERGIGANTE ROJA — el coloso hinchado que late a
    /// 0.2 Hz y muere como mueren las masivas: colapso → nova ×1.8.
    /// </summary>
    public class RedSupergiantStaff : RealStarStaffBase
    {
        protected override int StarDamage => 260;
        protected override int StarUseTime => 60;
        protected override int StarProjectile => ModContent.ProjectileType<RedSupergiantProjectile>();
        protected override string IdentityColor => "FF9A50";
        protected override string StarIdentity =>
            "Supergigante roja — el coloso frío: celdas de convección y atmósfera 3×";
        protected override string StarMechanic =>
            "late a 0.2 Hz hinchándose 5% · al expirar SE COLAPSA (implosión) " +
            "y la nova sale DEL COLAPSO (×1.8) · vida 7 s";
    }

    /// <summary>
    /// EL BASTÓN DEL MAGNETAR — la estrella de neutrones EXTREMA: el campo
    /// magnético más violento del universo, en violeta.
    /// </summary>
    public class MagnetarStaff : RealStarStaffBase
    {
        protected override int StarDamage => 320;
        protected override int StarUseTime => 55;
        protected override int StarProjectile => ModContent.ProjectileType<MagnetarProjectile>();
        protected override string IdentityColor => "C9A0F5";
        protected override string StarIdentity =>
            "Magnetar — la estrella de neutrones extrema: campo magnético violento";
        protected override string StarMechanic =>
            "aura magnética (140 px) + cadenas de rayo automáticas cada 20 ticks · " +
            "líneas de campo retorcidas y arritmia · vida 5 s";
    }
}
