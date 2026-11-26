using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Investigaciones;

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    //  ProyeccionDelCiclope — v6.176 — EL ARMA DEL CÍCLOPE PROYECTADO (la
    //  letra de la sesión: «si creaste algo de esto Cíclope Proyectado,
    //  debes darselo al jugador directamente, ademas lo que se tenia que
    //  crear era un arma» — la v6.175 dejó al Cíclope como JEFE (NPC +
    //  EcoProyectado); lo que la letra pedía era su ARMA).
    //
    //  EL ARMA del vídeo («Me Proyecté» de Chopilas): el ojo del cíclope
    //  SE PROYECTA sobre el portador — el telegraph del vídeo (el ojo
    //  GRITA creciendo mientras carga) y luego el RAYO MAGENTA doble de
    //  su cara. Todo con los pinceles de la casa (SombrasLib.Ojo — el
    //  ojo vivo que MIRA y parpadea · StormLib.Bolt — el doble filamento
    //  magenta del rayo del NPC).
    //
    //  El disparo: 12 ticks de ojo (invulnerable, mira al cursor) y el
    //  rayo sale a 26 px/t con penetración infinita. Daño mágico.
    // ======================================================================
    public class ProyeccionDelCiclope : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 150;
            Item.DamageType = DamageClass.Magic;
            Item.width = 34;
            Item.height = 34;
            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.mana = 9;
            Item.shoot = ModContent.ProjectileType<ProyeccionCiclopeRayo>();
            Item.shootSpeed = 26f;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.Item122;
        }
    }
}
