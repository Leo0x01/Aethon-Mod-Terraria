using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Investigaciones;

namespace AethonMod.Content.Weapons.Investigaciones
{
    /// <summary>
    /// SolEnMiniaturaStaff — v6.85 — EL SOL EN MINIATURA (la investigación
    /// R3: XiHe, la Emperatriz de la Luz del mod MEAC).
    ///
    /// LA LETRA: «investiga todo lo relacionado con la emperatris de la
    /// luz del mod MEAC, sus ataques patrones y tecnicas usadas de vfx,
    /// shader y texturas para cada ataque o habilidad». El informe
    /// (/tmp/research/r3_meac_emperatriz.md) catalogó 6 ataques y propuso
    /// 3 armas; ESTA es la PRIORIZADA (§6): «Sol en Miniatura» — la
    /// reimplementación del A2 (el sol en miniatura con anillos dorados).
    ///
    /// LA RECETA (§4 del informe):
    ///   · Núcleo térmico: centro blanco (255,255,240) → amarillo
    ///     (255,255,0) → naranja (255,140,0) → borde rojo suave
    ///     (255,99,71) — 4 glows concéntricos aditivos.
    ///   · 2 anillos dorados (255,223,0) elípticos (perspectiva 25°)
    ///     contra-rotando ±40°/s + halo de estrella de 8 puntas.
    ///   · Órbita alrededor del jugador: radio 3,5 tiles, período
    ///     240 ticks.
    ///   · El ataque: cada 45 ticks, telegrafía 10 ticks (la bola crece
    ///     0,2→1,0) y descarga el LASER0 dorado (255,220,120): núcleo
    ///     8 px + glow 24 px, alcance 40 tiles, activo 20 ticks.
    ///   · El impacto: onda en escala 0,5→2,0 (α 0,6→0) + 8 puntos de
    ///     luz — TODO con la RAMPA DE VIDA de MEAC (dorado #D4A017 →
    ///     blanco por edad — la firma «EoLProj_Color»).
    ///
    /// ARMA DE PRUEBAS: 0 maná (la regla de la sesión). El MINION ES
    /// PERSISTENTE (la letra del usuario: «los minions… siempre
    /// permanecen invocados a no ser que el jugador los desinvoque
    /// usando su buff»): sobrevive a la muerte (no se mata con el dueño
    /// caído) y se RE-INVOCA al volver al mundo (SolEnMiniaturaPlayer).
    /// </summary>
    public class SolEnMiniaturaStaff : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 28;
            Item.DamageType = DamageClass.Summon;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.mana = 0;               // ARMA DE PRUEBAS: la regla de la sesión
            Item.noMelee = true;
            Item.autoReuse = false;
            // (Este tML ya no tiene Item.buff — el buff lo aplica Shoot(),
            //  el patrón de la casa: LivingPulsarStaff.)
            Item.shoot = ModContent.ProjectileType<SolEnMiniaturaProj>();
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL BUFF DEL MINION (el patrón de la casa) + EL SELLO DE
            // PERSISTENCIA: quedará guardado con el jugador (se re-invoca
            // al volver al mundo — SolEnMiniaturaPlayer).
            player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.SolEnMiniaturaBuff>(), 2);
            player.GetModPlayer<SolEnMiniaturaPlayer>().Invocado = true;
            Projectile.NewProjectile(source, player.Center - Vector2.UnitY * 40f,
                Vector2.Zero, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>EL JUGADOR DEL SOL: la persistencia entre sesiones (la
    /// letra: «aunque salga de la partida, siempre permanecen invocados
    /// a no ser que los desinvoque usando su buff»).</summary>
    public class SolEnMiniaturaPlayer : ModPlayer
    {
        /// <summary>¿El sol fue invocado? (se guarda con el jugador).</summary>
        public bool Invocado;

        public override void SaveData(Terraria.ModLoader.IO.TagCompound tag)
        {
            tag["solInvocado"] = Invocado;
        }

        public override void LoadData(Terraria.ModLoader.IO.TagCompound tag)
        {
            Invocado = tag.GetBool("solInvocado");
        }

        public override void OnEnterWorld()
        {
            // LA RE-INVOCACIÓN (la persistencia de verdad: al volver al
            // mundo, el sol vuelve — a no ser que lo hubieran despedido).
            if (!Invocado || Main.netMode == NetmodeID.Server) return;
            if (Player.whoAmI != Main.myPlayer) return;

            int tipo = ModContent.ProjectileType<SolEnMiniaturaProj>();
            for (int i = 0; i < Main.maxProjectiles; i++)
                if (Main.projectile[i] != null && Main.projectile[i].active &&
                    Main.projectile[i].type == tipo &&
                    Main.projectile[i].owner == Player.whoAmI)
                    return;   // ya vive (carga de mundo con él presente)

            Projectile.NewProjectile(Player.GetSource_Misc("Persistencia"),
                Player.Center - Vector2.UnitY * 40f, Vector2.Zero,
                tipo, 28, 0f, Player.whoAmI);
            // (el daño de la re-invocación: el tope de prueba de la casa —
            //  una nueva invocación con el arma lo actualiza.)
        }
    }
}
