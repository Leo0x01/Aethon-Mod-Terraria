using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Investigaciones;

namespace AethonMod.Content.Items.Investigaciones
{
    // ======================================================================
    //  v6.85 — LA INVESTIGACIÓN R9: LA BARRERA / CAMPO DE FUERZA DE LOS
    //  «GRILLETES DE ANDRÓMEDA».
    //
    //  LA LETRA: «investiga la barrera o campo de fuerza que crea el item
    //  de Grilletes de andromeda del mod Calamity Mage Addon, ya sabes
    //  investiga sus tecincas vfx, shader y textura y crea un item en
    //  concecuencia».
    //
    //  EL HALLAZGO (informe /tmp/research/r9_grilletes.md): el item exacto
    //  NO existe — la fuente del recuerdo es DOBLE:
    //   (i) «Pap's Calamity Mage Optimization» (pap的灾厄法师优化) — su
    //       accesorio 天界秘法之盾 «Escudo Arcano Celestial»: por cada 100
    //       de maná gastados acumula una carga de escudo; al recibir daño
    //       el escudo se rompe y restaura 50 de maná — visual: aura de luz
    //       azul pulsante. La receta original lleva «Esposas Celestiales»
    //       (grilletes) — de ahí el nombre recordado.
    //  (ii) «Andrómeda» en Calamity real = el mech pilotable del
    //       Flamsteed Ring (paleta cian (89,229,255)).
    //
    //  LA SÍNTESIS AQUÍ (la del informe §d): mecánica pap + paleta/tema
    //  Andromeda + afterimages del DirectionalSolynForcefield:
    //   · Cada 100 de maná consumido → +20 de carga (tope 100 = 5).
    //   · Con cargas: +1 def y daño entrante ×0,85; al recibir golpe
    //     pierde 20; al romperse: shatter (flash ×1,35 8 ticks + 5
    //     afterimages + burst de 10 brazos) + restaura 50 de maná.
    //   · Pasivas: +40 maná máx, +2 def, mejor regen de maná.
    //   · EL CAMPO (proyectil persistente): domo hexagonal cian
    //     (89,229,255) pulsando 0,9+0,06·sen(t/7) + anillo rúnico
    //     violeta (170,90,255) DOBLE contra-rotante (+0,03/−0,018
    //     rad/tick — OrbitaLib.CirculoRunico de la casa) + 6-8 polvos
    //     eléctricos orbitales + luz cian 1,5f.
    // ======================================================================

    /// <summary>EL ACCESORIO (los grilletes de cristal astral).</summary>
    public class GrilletesDeAndromeda : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 26;
            Item.accessory = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var mp = player.GetModPlayer<GrilletesPlayer>();
            mp.TieneGrilletes = true;

            // LAS PASIVAS (la tabla del informe).
            player.statManaMax2 += 40;
            player.statDefense += 2;
            player.manaRegenDelayBonus += 1;
            player.manaCost -= 0.05f;   // 5 % más barato (la mejora de regen de la casa)

            // EL CAMPO PERSISTE mientras el accesorio está equipado (el
            // proyectil se refresca solo — patrón Nanomachina).
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                !player.GetModPlayer<GrilletesPlayer>().CampoVivo())
            {
                Projectile.NewProjectile(player.GetSource_Accessory(Item),
                    player.MountedCenter, Vector2.Zero,
                    ModContent.ProjectileType<CampoAndromedaProj>(),
                    0, 0f, player.whoAmI);
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/59E5FF:Convierte el maná gastado en un campo de fuerza visible]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Cada 100 de maná: +20 de escudo (máx 100) · con escudo: daño recibido ×0,85 · al romperse una carga: restaura 50 de maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>EL JUGADOR DE LOS GRILLETES: el acumulador de maná, las
    /// cargas, la reducción de daño y la rotura.</summary>
    public class GrilletesPlayer : ModPlayer
    {
        public bool TieneGrilletes;

        /// <summary>Las cargas del escudo (0-100, de 20 en 20).</summary>
        public int Cargas;

        /// <summary>El acumulador de maná (0-100).</summary>
        private float _manaAcumulado;

        /// <summary>El shock de la rotura (ticks restantes; el flash).</summary>
        public int ShockTicks;

        /// <summary>Las rotaciones viejas del anillo (los afterimages).</summary>
        public readonly float[] RotVieja = new float[5];

        public override void ResetEffects()
        {
            TieneGrilletes = false;
            if (ShockTicks > 0) ShockTicks--;
        }

        public override void OnConsumeMana(Item item, int manaConsumed)
        {
            if (!TieneGrilletes || manaConsumed <= 0) return;

            // EL ACUMULADOR (la mecánica pap: 100 de maná → +20 de carga).
            _manaAcumulado += manaConsumed;
            while (_manaAcumulado >= 100f)
            {
                _manaAcumulado -= 100f;
                if (Cargas < 100)
                {
                    Cargas += 20;
                    if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                    {
                        // EL TIC de la carga ganada (un destello corto).
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item4.WithPitchOffset(0.35f)
                                .WithVolumeScale(0.5f), Player.Center);
                    }
                }
            }
        }

        public override void PostUpdateEquips()
        {
            // CON CARGAS: +1 def (la capa defensiva visible).
            if (Cargas > 0 && TieneGrilletes)
                Player.statDefense += 1;
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            if (Cargas > 0) modifiers.SourceDamage *= 0.85f;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref Player.HurtModifiers modifiers)
        {
            if (Cargas > 0) modifiers.SourceDamage *= 0.85f;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (Cargas <= 0 || !TieneGrilletes) return;

            // === LA ROTURA DE UNA CARGA (el «wow» del informe) ===
            bool ultima = Cargas <= 20;
            Cargas -= 20;
            ShockTicks = 8;

            // El afterimage: la rotación actual del anillo queda grabada.
            for (int i = RotVieja.Length - 1; i > 0; i--)
                RotVieja[i] = RotVieja[i - 1];

            // LA COMPENSACIÓN pap: cada rotura restaura 50 de maná.
            Player.statMana += 50;
            Player.ManaEffect(50);

            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                // EL SONIDO metálico-cristalino (la idea del informe).
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item27.WithPitchOffset(0.45f)
                        .WithVolumeScale(0.85f), Player.Center);
                // LA SACUDIDA suave (la última carga, más fuerte).
                global::AethonMod.Content.VFX.Pantalla.PresetImpacto(Player.Center,
                    ultima ? 1.1f : 0.7f, new Color(89, 229, 255));

                // EL BURST DE 10 BRAZOS (paso 6: radio 20→90, velocidad
                // radial ±6, noGravity — el pétalo del informe reducido a
                // escala jugador).
                for (int i = 0; i < 10; i++)
                {
                    float ang = i / 10f * MathHelper.TwoPi;
                    Dust d = Dust.NewDustPerfect(
                        Player.Center + new Vector2((float)System.Math.Cos(ang),
                            (float)System.Math.Sin(ang)) * 20f,
                        DustID.Electric,
                        new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) *
                            Main.rand.NextFloat(4f, 6f),
                        0, ultima ? new Color(240, 250, 255) : new Color(89, 229, 255), 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // LA ÚLTIMA CARGA: la estrella de 8 puntas blanco-cian
                // (dos flares cruzados — la estrella de 6 puntas del
                // informe, la versión de la casa).
                if (ultima)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        Dust d = Dust.NewDustPerfect(Player.Center, DustID.Electric,
                            Main.rand.NextVector2CircularEdge(7f, 7f) * Main.rand.NextFloat(2.5f, 5f),
                            0, new Color(240, 250, 255), 1.5f);
                        d.noGravity = true;
                    }
                }
            }
        }

        /// <summary>¿El campo persistente está vivo?</summary>
        public bool CampoVivo()
        {
            int tipo = ModContent.ProjectileType<CampoAndromedaProj>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == tipo &&
                    p.owner == Player.whoAmI)
                    return true;
            }
            return false;
        }
    }
}
