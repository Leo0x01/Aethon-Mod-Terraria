using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  EL CERROJO DE LOS ESBIRROS (v6.51.0) — LA LETRA DEL USUARIO:
    //  «los minions del grimorio son independientes de los minions del
    //  juego así que sin importar la armadura o accesorios que lleve el
    //  jugador, los minions del grimorio estarán sujetos a las
    //  características del grimorios y mientras los minions del grimorio
    //  estén activos no se pueden invocar otros minions de ningún tipo».
    //
    //  MIENTRAS LOS OJOS DEL ETERNO VUELAN: ningún otro cetro de
    //  esbirros puede usarse (el grimorio no comparte su cielo). Los
    //  LÁTIGOS siguen libres (no invocan esbirros — solo los marcan).
    // ======================================================================
    public class CerrojoEsbirrosGlobalItem : GlobalItem
    {
        /// <summary>El throttling del aviso (una vez cada 3 s, no un muro).</summary>
        private static int _ultimoAviso;

        public override bool CanUseItem(Item item, Player player)
        {
            try
            {
                if (item == null || item.IsAir) return true;
                if (item.type == ModContent.ItemType
                        <global::AethonMod.Content.Weapons.GrimoireEternal>())
                    return true;

                // ¿Es un arma que INVOCA esbirros? (los látigos no: no
                // crean nada — solo azuzan).
                bool invocaEsbirros =
                    item.DamageType == DamageClass.Summon &&
                    item.shoot > 0 &&
                    !ProjectileID.Sets.IsAWhip[item.shoot];
                if (!invocaEsbirros) return true;

                // ¿Hay OJOS DEL ETERNO vivos de este jugador?
                int tipoOjo = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.OjoEternoMinion>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile pr = Main.projectile[i];
                    if (pr == null || !pr.active || pr.type != tipoOjo) continue;
                    if (pr.owner != player.whoAmI) continue;

                    // EL CERROJO (la letra): con los ojos del grimorio en
                    // el cielo, ningún otro esbirro responde.
                    if (Main.GameUpdateCount - (ulong)_ultimoAviso > 180ul)
                    {
                        _ultimoAviso = (int)Main.GameUpdateCount;
                        if (player.whoAmI == Main.myPlayer)
                            Main.NewText(
                                Terraria.Localization.Language.GetTextValue(
                                    "Mods.AethonMod.Despertar.CerrojoEsbirros"),
                                new Microsoft.Xna.Framework.Color(255, 130, 130));
                    }
                    return false;
                }
                return true;
            }
            catch { return true; }
        }
    }
}
