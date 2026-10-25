using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Escriba;
using AethonMod.Content.Skills;

namespace AethonMod.Content.Weapons.Escriba
{
    /// <summary>
    /// PLUMACONSCIENTE — v6.52.0 — LA PLUMA CONSCIENTE: el arma viva.
    ///
    /// LA LETRA DEL USUARIO (la petición tras ver el mod de Calamity que
    /// guarda «la conciencia de un NPC en diferentes armas y además un
    /// árbol de habilidades completo y funcional para cada una»): «recuerda
    /// que intentamos crear algo así y no funcionó». ESTA ES LA RESPUESTA
    /// ARMADA — el diseño de Calamity Overhaul (Onikiri/OniMei) recreado
    /// con las reglas de la casa:
    ///
    ///   · LA CONCIENCIA: el ESCRIBA mismo — la mano que escribió las
    ///     páginas que Aethon devoró — vive atado a su última pluma.
    ///     HABLA al primer encuentro, REACCIONA a cada jefe (regalos =
    ///     inscripciones nuevas) y SUSURRA de vez en cuando.
    ///   · EL ÁRBOL: 18 INSCRIPCIONES en 3 ranuras (Filigrana/Verso/
    ///     Colofón) — cada una con PODER y CARGA. Ábrelo con [P] sosteniendo
    ///     la pluma (ArbolInscripcionesUI).
    ///   · EL CRECIMIENTO: el NIVEL viaja con el ÍTEM (cada jefe vencido
    ///     con la pluma: +12% de daño permanente); la biblioteca viaja
    ///     con el JUGADOR (hechos y regalos).
    ///
    /// Arma de PRUEBA de la casa: maná SIMBÓLICO (las inscripciones lo
    /// mueven — la Pluma Ajena lo rebaja un 40%).
    /// </summary>
    public class PlumaConsciente : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName y Tooltip: Items.PlumaConsciente (la localización).
        }

        public override void SetDefaults()
        {
            Item.damage = 45;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.mana = 6;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TintaBolt>();
            Item.shootSpeed = 13f;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item9;
        }

        public override void AddRecipes()
        {
            // LA VÍA DE REPUESTO de la casa (la tradición de pruebas).
            CreateRecipe().AddIngredient(ItemID.Wood, 10)
                .Register();
        }

        /// <summary>El perfil de las ranuras del ítem EN LA MANO (o neutro).</summary>
        private static PerfilInscripcion Perfil(Player p)
        {
            if (p == null || p.HeldItem == null || p.HeldItem.IsAir) return PerfilInscripcion.Neutral();
            if (p.HeldItem.type != ModContent.ItemType<PlumaConsciente>()) return PerfilInscripcion.Neutral();
            return InscripcionDefinicion.Resolver(p.HeldItem);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            PerfilInscripcion perfil = Perfil(player);
            int extra = perfil.ProyectilesExtra;

            // LA PALABRA: uno o más bolts de tinta; con extra, el ABANICO
            // (±12° por trazo — los trazos adicionales salen ESCONDIDOS en
            // el disparo, no en fila india).
            int total = 1 + Math.Max(0, extra);
            float giro = velocity.ToRotation();
            for (int i = 0; i < total; i++)
            {
                float offset = total == 1 ? 0f
                    : MathHelper.Lerp(-0.21f, 0.21f, i / (float)(total - 1)) * (total - 1) * 0.5f;
                Vector2 rumbo = velocity.RotatedBy(offset);
                Projectile.NewProjectile(source, position, rumbo, type, damage, knockback,
                    player.whoAmI, perfil.BuscaPresa ? 1f : 0f, perfil.EstelaTinta ? 1f : 0f);
            }
            return false;
        }

        // === LAS INSCRIPCIONES EN EL COMBATE (composición multiplicativa) ===

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            PerfilInscripcion perfil = Perfil(player);
            InscripcionItem datos = InscripcionItem.De(player.HeldItem);
            float porNivel = datos != null ? datos.DañoPorNivel : 1f;
            damage *= perfil.DañoMul * porNivel;
        }

        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            crit += Perfil(player).CriticoSuma;
        }

        public override float UseSpeedMultiplier(Player player)
        {
            float mult = Perfil(player).VelocidadMul;
            return mult <= 0.05f ? 0.05f : mult;
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            mult *= Perfil(player).ManaMul;
        }

        // (Los efectos de GOLPE — Firma/robo de vida, Punto Final/ejecución
        // y el hecho ManoFirme — viven en TintaBolt, el proyectil que
        // golpea: el ModItem.OnHitNPC solo cubre el cuerpo a cuerpo.)

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player p = Main.LocalPlayer;
            InscripcionItem datos = InscripcionItem.De(p?.HeldItem);
            PerfilInscripcion perfil = p != null ? Perfil(p) : PerfilInscripcion.Neutral();

            // EL NIVEL DEL ARMA (viaja con el ítem).
            if (datos != null)
            {
                tooltips.Add(new TooltipLine(Mod, "PlumaNivel",
                    Terraria.Localization.Language.GetTextValue(
                        "Mods.AethonMod.Items.PlumaConsciente.Nivel", datos.Nivel))
                { OverrideColor = new Color(255, 226, 160) });
            }

            // LAS RANURAS EQUIPADAS (nombre por ranura).
            for (int r = 0; r < 3 && datos != null; r++)
            {
                string clave = r < datos.Ranuras.Length ? datos.Ranuras[r] : null;
                string nombre = InscripcionDefinicion.PorClave(clave)?.Nombre;
                if (nombre == null) continue;
                tooltips.Add(new TooltipLine(Mod, "PlumaRanura" + r,
                    Terraria.Localization.Language.GetTextValue(
                        "Mods.AethonMod.Items.PlumaConsciente.Ranura" + r, nombre))
                { OverrideColor = new Color(212, 190, 255) });
            }

            // LA LÍNEA DEL ÁRBOL (cómo abrirlo).
            tooltips.Add(new TooltipLine(Mod, "PlumaArbol",
                Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Items.PlumaConsciente.Arbol"))
            { OverrideColor = new Color(170, 220, 255) });
        }
    }
}
