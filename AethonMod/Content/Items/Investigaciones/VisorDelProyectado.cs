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
    //  VisorDelProyectado — v6.179 — EL ARMA DEL CÍCLOPE, SOLO EL ARMA.
    //
    //  LA LETRA (sesión 32): «solo necesito el arma visor del proyectado
    //  cualquier otra cosa del mod puedes borrarlo, ademas que el arma
    //  ya este en su maxima capacidad y pueda cambiar entre las
    //  diferentes fases».
    //
    //  LA CIRUGÍA: las diez mejoras que dolían, el Cíclope, su Eco y los
    //  drops de jefe MURIERON — queda SOLO EL ARMA, y NACE A SU MÁXIMA
    //  CAPACIDAD (energía 300/300 INFINITA — la luna ya está bajada de
    //  fábrica; nada que recoger, nada que esperar).
    //
    //  LAS DOS FASES (cambian con la TECLA X o el CLIC DERECHO):
    //   · FASE 1 — EL RAYO CONTINUO: mantén el clic; el haz fino sale de
    //     tu ojo y sigue al cursor (la de siempre).
    //   · FASE 2 — LA PROYECCIÓN: cada clic suelta el rayo ENORME que
    //     atraviesa todo (el cine completo: el visor se levanta, el ojo
    //     se enciende, 100 px de ancho — SIN enfriamiento, una
    //     proyección a la vez).
    //
    //  EL DAÑO sigue escalando SOLO (las 11 etapas por los jefes de
    //  Terraria caídos + los opcionales + la desesperación acumulada).
    // ======================================================================
    public class VisorDelProyectado : ModItem
    {
        /// <summary>El rayo grande de la fase 2 golpea ×3 (el plano de la v6.177).</summary>
        public const float MultiplicadorProyeccion = 3f;

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
            // FASE 2 — LA PROYECCIÓN: una proyección a la vez (el cine dura
            // sus seis segundos — ese ES el ritmo; nada de enfriamientos
            // falsos en un arma YA MÁXIMA).
            if (player.GetModPlayer<ProyectadoPlayer>().Fase == 2)
                return player.ownedProjectileCounts[ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Investigaciones.RayoProyectado>()] == 0;
            // FASE 1 — EL RAYO CONTINUO: un canal a la vez (y nada de
            // disparar mientras la proyección grande vive).
            if (player.ownedProjectileCounts[ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Investigaciones.RayoOpticoProyectado>()] > 0)
                return false;
            if (player.ownedProjectileCounts[ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Investigaciones.RayoProyectado>()] > 0)
                return false;
            return true;
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // FASE 2 — LA PROYECCIÓN: el clic suelta EL RAYO ENORME (el
            // cine completo, sin enfriamiento) y el rayo fino NO nace.
            if (player.GetModPlayer<ProyectadoPlayer>().Fase == 2)
            {
                int dano = (int)(damage * MultiplicadorProyeccion);
                float kb = knockback * 2f;
                Vector2 rumbo = velocity.SafeNormalize(Vector2.UnitX * player.direction);
                Projectile.NewProjectile(source, player.Center, rumbo,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Investigaciones.RayoProyectado>(),
                    dano, kb, player.whoAmI);
                return false;   // el rayo óptico fino de la fase 1 no nace
            }
            return true;        // FASE 1: el canal de siempre (el óptico fino)
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            int etapa = ProyectadoPlayer.EtapaActual();
            damage *= (float)ProyectadoPlayer.Etapas[etapa].dano / ProyectadoPlayer.Etapas[0].dano;
            damage *= 1f + ProyectadoPlayer.BonoOpcionales();
            damage *= 1f + ProyectadoPlayer.BonoMuertes(player.GetModPlayer<ProyectadoPlayer>().Muertes, etapa);
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
                // v6.179 — LA CAPACIDAD MÁXIMA, A LA VISTA: la energía es
                // INFINITA (nada se gasta, nada se recarga — ya está toda).
                tooltips.Add(new TooltipLine(Mod, "ProyectadoEnergia",
                    Language.GetTextValue("Mods.AethonMod.Proyectado.EnergiaInfinita"))
                { OverrideColor = vivo });
                // LAS FASES (la letra: «pueda cambiar entre las diferentes
                // fases»): la viva primero, la otra detrás.
                string tecla = Language.GetTextValue("Mods.AethonMod.Proyectado.SinTecla");
                var kb = global::AethonMod.Content.Systems.ProyectadoSistema.TeclaProyectarse;
                var asignadas = kb?.GetAssignedKeys(Terraria.GameInput.InputMode.Keyboard);
                if (asignadas != null && asignadas.Count > 0) tecla = asignadas[0];
                tooltips.Add(new TooltipLine(Mod, "ProyectadoFase",
                    Language.GetTextValue("Mods.AethonMod.Proyectado.FaseViva",
                        pr.Fase, Language.GetTextValue("Mods.AethonMod.Proyectado.Fase" + pr.Fase)))
                { OverrideColor = vivo });
                tooltips.Add(new TooltipLine(Mod, "ProyectadoFaseCambio",
                    Language.GetTextValue("Mods.AethonMod.Proyectado.FaseCambioTip", tecla))
                { OverrideColor = apagado });
                tooltips.Add(new TooltipLine(Mod, "ProyectadoOpcionales",
                    Language.GetTextValue("Mods.AethonMod.Proyectado.Opcionales",
                        ProyectadoPlayer.JefesOpcionales(), ProyectadoPlayer.TotalOpcionales,
                        ProyectadoPlayer.BonoOpcionales() * 100f)));
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("VisorDelProyectado", exRegistro); }
        }
    }
}
