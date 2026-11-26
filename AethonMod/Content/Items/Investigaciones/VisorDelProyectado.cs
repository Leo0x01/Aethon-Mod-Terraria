using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Players;

namespace AethonMod.Content.Items.Investigaciones
{
    // ======================================================================
    //  VisorDelProyectado — v6.177 — EL ARMA DEL CÍCLOPE, LA DE VERDAD.
    //
    //  LA LETRA (sesión 30): «el arma del ciclope no tiene nada que ver
    //  te voy a dar el archivo .tmod del ciclope proyectado» — el .tmod
    //  fue EL PLANO (mecánica y espíritu del meme de Chopilas); TODO lo
    //  demás es de la casa: código, sprites, textos y sonidos vanilla.
    //
    //  QUÉ ES: un VISOR de una sola lente magenta. Mantén el CLIC y el
    //  rayo óptico continuo sale de tu ojo y sigue al cursor (no gasta
    //  maná: gasta ENERGÍA PROYECTADA, una barra propia de 100). La
    //  tecla X es LA PROYECCIÓN de verdad: te quitas el visor, se te
    //  enciende el ojo y sueltas un rayo ENORME que atraviesa todo (20 s
    //  de enfriamiento, no gasta energía).
    //
    //  EL DAÑO escala SOLO: es daño MÁGICO (las armaduras y accesorios
    //  de mago lo visten) y sube con cada jefe de Terraria caído (las
    //  11 etapas), +5% por cada jefe opcional y un bono por enemigos
    //  caídos con tope por etapa. Cada jefe suelta su MEJORA (una de un
    //  solo uso que sube la energía máxima y acelera la recarga — la del
    //  Señor de la Luna da la PROYECCIÓN INFINITA).
    // ======================================================================
    public class VisorDelProyectado : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 18;
            Item.damage = ProyectadoPlayer.Etapas[0].dano;
            Item.DamageType = DamageClass.Magic;
            Item.knockBack = 2f;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.channel = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType
                <global::AethonMod.Content.Projectiles.Investigaciones.RayoOpticoProyectado>();
            Item.shootSpeed = 10f;
            Item.rare = ItemRarityID.Expert;
            Item.value = Item.sellPrice(0, 1, 0, 0);
        }

        public override bool CanUseItem(Player player)
        {
            var pr = player.GetModPlayer<ProyectadoPlayer>();
            if (!pr.PuedeDisparar) return false;
            if (player.ownedProjectileCounts[ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Investigaciones.RayoOpticoProyectado>()] > 0)
                return false;
            // durante la proyección grande el visor no dispara
            if (player.ownedProjectileCounts[ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Investigaciones.RayoProyectado>()] > 0)
                return false;
            return true;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var pr = player.GetModPlayer<ProyectadoPlayer>();
            int etapa = ProyectadoPlayer.EtapaActual();
            damage *= (float)ProyectadoPlayer.Etapas[etapa].dano / ProyectadoPlayer.Etapas[0].dano;
            damage *= 1f + ProyectadoPlayer.BonoOpcionales();
            damage *= 1f + ProyectadoPlayer.BonoMuertes(pr.Muertes, etapa);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            try
            {
                var pr = Main.LocalPlayer.GetModPlayer<ProyectadoPlayer>();
                int etapa = ProyectadoPlayer.EtapaActual();
                Color vivo = new(255, 90, 70);
                Color apagado = new(180, 180, 180);
                tooltips.Add(new TooltipLine(Mod, "ProyectadoEtapa",
                    Language.GetTextValue("Mods.AethonMod.Proyectado.Etapa",
                        ProyectadoPlayer.Etapas[etapa].nombre, ProyectadoPlayer.Etapas[etapa].dano))
                { OverrideColor = vivo });
                if (etapa < ProyectadoPlayer.Etapas.Length - 1)
                    tooltips.Add(new TooltipLine(Mod, "ProyectadoSiguiente",
                        Language.GetTextValue("Mods.AethonMod.Proyectado.Siguiente",
                            ProyectadoPlayer.Etapas[etapa + 1].nombre, ProyectadoPlayer.Etapas[etapa + 1].dano))
                    { OverrideColor = apagado });
                string energia = pr.Infinito
                    ? Language.GetTextValue("Mods.AethonMod.Proyectado.EnergiaInfinita")
                    : Language.GetTextValue("Mods.AethonMod.Proyectado.Energia",
                        (int)pr.Energia, (int)pr.EnergiaMax, (int)((pr.MultiplicadorRecarga - 1f) * 100f));
                tooltips.Add(new TooltipLine(Mod, "ProyectadoEnergia", energia) { OverrideColor = vivo });
                tooltips.Add(new TooltipLine(Mod, "ProyectadoMejoras",
                    Language.GetTextValue("Mods.AethonMod.Proyectado.Mejoras",
                        pr.CantidadUsados, ProyectadoPlayer.PuntosMejora.Length))
                { OverrideColor = apagado });
                tooltips.Add(new TooltipLine(Mod, "ProyectadoMuertes",
                    Language.GetTextValue("Mods.AethonMod.Proyectado.Muertes", pr.Muertes,
                        ProyectadoPlayer.BonoMuertes(pr.Muertes, etapa) * 100f,
                        ProyectadoPlayer.TopeMuertes(etapa) * 100f)));
                tooltips.Add(new TooltipLine(Mod, "ProyectadoOpcionales",
                    Language.GetTextValue("Mods.AethonMod.Proyectado.Opcionales",
                        ProyectadoPlayer.JefesOpcionales(), ProyectadoPlayer.BonoOpcionales() * 100f)));
                // LA TECLA viva: el nombre de la tecla de habilidad
                string tecla = Language.GetTextValue("Mods.AethonMod.Proyectado.SinTecla");
                var kb = global::AethonMod.Content.Systems.ProyectadoSistema.TeclaProyectarse;
                var asignadas = kb?.GetAssignedKeys(Terraria.GameInput.InputMode.Keyboard);
                if (asignadas != null && asignadas.Count > 0) tecla = asignadas[0];
                string listo = pr.Enfriamiento > 0
                    ? Language.GetTextValue("Mods.AethonMod.Proyectado.Recargando", pr.Enfriamiento / 60 + 1)
                    : Language.GetTextValue("Mods.AethonMod.Proyectado.ListoCorto");
                tooltips.Add(new TooltipLine(Mod, "ProyectadoEspecial",
                    Language.GetTextValue("Mods.AethonMod.Proyectado.Especial", tecla, listo))
                { OverrideColor = pr.Enfriamiento > 0 ? apagado : vivo });
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("VisorDelProyectado", exRegistro); }
        }
    }
}
