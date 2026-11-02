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
    /// RequiemDelPulsarStaff — v6.82 — EL REQUIEM DEL PÚLSAR (arma cósmica).
    ///
    /// LA LETRA: «investiga tecnicas vfx de otros mods, investiga armas
    /// de otros mod, y con esa investigacion y las librerias que conoces,
    /// dame una nueva arma de tipo cosmico».
    ///
    /// LA INVESTIGACIÓN (el informe contra el código real de Calamity
    /// Public, Stars Above 1.4 y Starlight River): las armas cósmicas
    /// premium comparten CINCO técnicas — y ESTA arma las usa TODAS con
    /// las librerías de la casa:
    ///   (1) EL FARO ROTANTE: la estrella GIRA a 4 rev/s con sus DOS
    ///       haces opuestos barriendo el espacio (el lighthouse de un
    ///       púlsar real — la antena que todo lo toca).
    ///   (2) LA ESTELA Catmull-Rom: EstelaLib.Ribbon Comet (la cinta
    ///       que muere hacia atrás — la técnica de trails de Calamity).
    ///   (3) EL OVERBRIGHT ×1.4: el cian ARDE sobre el aditivo (el truco
    ///       HDR de Scarlet Devil).
    ///   (4) LOS PULSOS PERIÓDICOS: anillos expandiéndose cada 12 ticks
    ///       (la señal de radio del púlsar — el ritmo que la hace
    ///       reconocible entre cualquier arma).
    ///   (5) EL COLAPSO CON ABERRACIÓN: al morir, la implosión + el
    ///       anillo de choque + CalorLib.Estallido (el aire se parte en
    ///       R/G/B — la técnica del DistortionPulse de Starlight River,
    ///       la firma del estallido de la casa).
    ///
    /// LA MECÁNICA: dispara LA ESTRELLA (homing suave del sol de la
    /// casa, 3 penetraciones, 240 ticks de vida) — cada muerte es un
    /// colapso completo. El grimorio NO se toca: esta arma es SU hermana
    /// cósmica del lado de la investigación.
    /// </summary>
    public class RequiemDelPulsarStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 260;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 22; Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RequiemPulsarProjectile>();
            Item.shootSpeed = 13f;
            Item.mana = 12; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item92.WithPitchOffset(-0.25f);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA ESTRELLA NACE CON SU GIRO (desfase por disparo: cada
            // púlsar del coro gira distinto).
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback,
                player.whoAmI, Main.rand.NextFloat(MathHelper.TwoPi));
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D", "[c/8CE1FF:Lanza una estrella de púlsar: el faro rotante que todo lo barre, persiguiendo a tus presas]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Cada muerte es un colapso: implosión · anillo de choque · el aire partido en R/G/B]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
