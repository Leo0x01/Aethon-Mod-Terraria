using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Dusts;

namespace AethonMod.Content.Items.Investigaciones
{
    // ======================================================================
    //  v6.85 — LA INVESTIGACIÓN R1: EL AURA AMARILLA DE BOMBUS APIS.
    //
    //  LA LETRA: «investiga bombus apis mod, ahi hay un arma o accesorio
    //  que posse un aura como amarilla». EL HALLAZGO (informe
    //  /tmp/research/r1_bombus_apis.md — fuente pública de SkippZzMods/
    //  Bombus-Apis-Bee, v2.3.0.1, clonada y auditada): el accesorio
    //  LIHZAARDIAN HORNET RELIC — al pulsar la tecla «canaliza el poder
    //  del sol»: durante 8 s el jugador arde en un AURA SOLAR AMARILLA
    //  (flares de 4 brazos girando + anillo de polvos dorados que
    //  CONVERGEN sobre el jugador — «el sol se cierra») a cambio de
    //  +10 % de daño recibido y un drenaje de vida. Cooldown 45 s.
    //
    //  LOS PARÁMETROS EXACTOS DEL FUENTE (tabla §d.3 del informe):
    //   · Duración 480 ticks · CD 2700 (buff debuff) · stats activos
    //     +12 % daño/use-speed/crít · costes: daño entrante ×1,1 y
    //     lifeRegen −16 (lifeRegenTime 0).
    //   · Pasivas: moveSpeed +0,35 · wingTimeMax ×1,35.
    //   · Flares: 3 capas — dorado (223,170,40)·0,5 rot t·0,025 esc
    //     1,25→0,05 · pálido (245,245,149)·0,5 rot −t·0,0265 esc
    //     1,1→0,05 · pálido ·0,55 rot t·0,0165 esc 0,9→0,05 — TODO con
    //     el factor de contracción (1 − t/480) y origen en el centro.
    //   · El anillo: 2 dusts/tick en radio Lerp(85,5, 1−t/480) —
    //     dorado (222,173,40) esc 0,45 vel 0,25 · pálido (245,245,149)
    //     esc 0,35 vel 0,75; el dust decae ×0,95, frena ×0,95, gira
    //     +0,06 rad/tick y ALUMBRA su color.
    //   · Burst de activación: 25 dusts, esc 0,55, vel 4,25-5,25.
    //   · Sonido: Item74 (pitch −0,15, var 0,1, vol 1,25).
    //  EL AURA SE DIBUJA EN DOS CAPAS PlayerDrawLayer (la recomendación
    //  del informe contra el tML 1.4.4 real): DETRÁS del jugador el
    //  bloom completo + los 3 flares; POR DELANTE los 2 flares más
    //  brillantes al 40 % (asoma como el anillo infernal del original).
    // ======================================================================

    /// <summary>EL ACCESORIO (la reliquia del avispón — test item de la
    /// investigación R1; su bolsa: la Bolsa de las Investigaciones).</summary>
    public class RelicarioDelAvispon : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // LA PASIVA (la del original: velocidad y vuelo).
            player.moveSpeed += 0.35f;
            player.GetModPlayer<RelicarioPlayer>().TieneRelicario = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/FFD77A:Pulsa la tecla del Relicario Solar (L) para canalizar el poder del sol]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:8 s de aura solar: +12 % daño, velocidad de uso y crítico · a cambio +10 % de daño recibido y drenaje de vida · 45 s de recarga]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>EL JUGADOR DEL RELICARIO: el timer del aura, la tecla, los
    /// stats, el drenaje y el anillo de dusts.</summary>
    public class RelicarioPlayer : ModPlayer
    {
        /// <summary>¿Lleva la reliquia equipada?</summary>
        public bool TieneRelicario;

        /// <summary>El timer del aura (480 → 0; 0 = apagado).</summary>
        public int TimerAura;

        /// <summary>LA TECLA (registrada en AethonMod.Load).</summary>
        public static ModKeybind TeclaRelicario;

        public override void ResetEffects()
        {
            TieneRelicario = false;
            // (el timer NO se resetea: vive a través de los ticks)
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (TeclaRelicario == null || Player.dead || !Player.active) return;
            if (!TeclaRelicario.JustPressed) return;
            if (!TieneRelicario || TimerAura > 0 || Player.HasBuff<global::AethonMod.Content.Buffs.RelicarioCdBuff>()) return;

            // === LA CANALIZACIÓN DEL SOL (la activación) ===
            TimerAura = 480;
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item74.WithPitchOffset(-0.15f)
                    .WithVolumeScale(1.25f), Player.Center);

            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                // EL BURST: 25 dusts alternando los dos colores, esc 0,55,
                // vel circular 4,25-5,25 (la tabla del informe).
                for (int i = 0; i < 25; i++)
                {
                    bool dorado = i % 2 == 0;
                    Dust d = Dust.NewDustPerfect(Player.Center,
                        ModContent.DustType<SolarGlowDust>(),
                        Main.rand.NextVector2Circular(4.5f, 4.5f),
                        0, dorado ? new Color(222, 173, 40) : new Color(245, 245, 149),
                        0.55f);
                    d.noGravity = true;
                }
            }
        }

        public override void PreUpdate()
        {
            if (TimerAura <= 0) return;

            TimerAura--;
            if (TimerAura == 0)
            {
                // EL SOL SE CIERRA: la recarga comienza (el debuff de 45 s
                // del original — buffNoSave, la enfermera no lo quita).
                Player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.RelicarioCdBuff>(), 2700);
                return;
            }

            // === EL ANILLO DE POLVOS (2/tick convergiendo) ===
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                float contraccion = 1f - TimerAura / 480f;
                float radio = MathHelper.Lerp(85f, 5f, contraccion);
                for (int i = 0; i < 2; i++)
                {
                    bool dorado = i == 0;
                    Vector2 pos = Player.Center + Main.rand.NextVector2CircularEdge(radio, radio);
                    Dust d = Dust.NewDustPerfect(pos, ModContent.DustType<SolarGlowDust>(),
                        Main.rand.NextVector2Circular(dorado ? 0.25f : 0.75f, dorado ? 0.25f : 0.75f),
                        0, dorado ? new Color(222, 173, 40) : new Color(245, 245, 149),
                        dorado ? 0.45f : 0.35f);
                    d.noGravity = true;
                }
            }
        }

        public override void UpdateEquips()
        {
            if (TimerAura <= 0) return;
            // LOS STATS ACTIVOS (+12/12/12 — genéricos: todas las clases).
            Player.GetDamage<GenericDamageClass>() *= 1.12f;
            Player.GetCritChance<GenericDamageClass>() += 12;
            Player.GetAttackSpeed<GenericDamageClass>() *= 1.12f;
        }

        public override void PostUpdateMiscEffects()
        {
            // LA PASIVA DEL VUELO (la del original — en Post para pisar
            // los cálculos de vanilla).
            if (TieneRelicario)
                Player.wingTimeMax = (int)(Player.wingTimeMax * 1.35f);
        }

        public override void UpdateBadLifeRegen()
        {
            if (TimerAura <= 0) return;
            // EL DRENAJE (el coste del sol): −16/s con lifeRegenTime 0 y
            // la regen positiva anulada (la tabla del informe).
            Player.lifeRegen -= 16;
            Player.lifeRegenTime = 0;
            if (Player.lifeRegen > 0) Player.lifeRegen = 0;
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            if (TimerAura > 0) modifiers.SourceDamage *= 1.1f;   // +10 % recibido
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref Player.HurtModifiers modifiers)
        {
            if (TimerAura > 0) modifiers.SourceDamage *= 1.1f;
        }
    }

    /// <summary>EL AURA DETRÁS DEL JUGADOR: el bloom + los 3 flares
    /// girando (la capa principal — BeforeFirstVanillaLayer).</summary>
    public class RelicarioAuraDetras : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
            => PlayerDrawLayers.BeforeFirstVanillaLayer;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            var mp = drawInfo.drawPlayer.GetModPlayer<RelicarioPlayer>();
            return mp.TimerAura > 0 && !Main.gameMenu;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            var mp = p.GetModPlayer<RelicarioPlayer>();
            int t = 480 - mp.TimerAura;                    // la edad del aura
            float contraccion = 1f - mp.TimerAura / 480f;  // 0 → 1

            Vector2 pos = new Vector2(p.Center.X, p.Center.Y + p.gfxOffY)
                - Main.screenPosition;

            var flare = ModContent.Request<Microsoft.Xna.Framework.Graphics.Texture2D>(
                "AethonMod/Content/Effects/FlareEstrella4Puntas",
                ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            var bloom = global::AethonMod.Content.VFX.VFXCore.SoftGlow;

            var orig = flare.Size() * 0.5f;
            var origB = bloom.Size() * 0.5f;

            Color dorado = new Color(223, 170, 40) * 0.5f;
            Color palido = new Color(245, 245, 149) * 0.5f;

            // EL BLOOM (la base del sol): dos glows girando.
            drawInfo.DrawDataCache.Add(new DrawData(
                bloom, pos, null, dorado, t * 0.025f, origB,
                MathHelper.Lerp(1.25f, 0.05f, contraccion) * 2.2f,
                SpriteEffects.None, 0f));
            drawInfo.DrawDataCache.Add(new DrawData(
                bloom, pos, null, palido, t * 0.0265f, origB,
                MathHelper.Lerp(1.1f, 0.05f, contraccion) * 2.2f,
                SpriteEffects.None, 0f));

            // LOS 3 FLARES (la tabla exacta del informe).
            drawInfo.DrawDataCache.Add(new DrawData(
                flare, pos, null, dorado, t * 0.025f, orig,
                MathHelper.Lerp(1.25f, 0.05f, contraccion),
                SpriteEffects.None, 0f));
            drawInfo.DrawDataCache.Add(new DrawData(
                flare, pos, null, palido, -t * 0.0265f, orig,
                MathHelper.Lerp(1.1f, 0.05f, contraccion),
                SpriteEffects.None, 0f));
            drawInfo.DrawDataCache.Add(new DrawData(
                flare, pos, null, palido * 1.1f, t * 0.0165f, orig,
                MathHelper.Lerp(0.9f, 0.05f, contraccion),
                SpriteEffects.None, 0f));
        }
    }

    /// <summary>EL AURA POR DELANTE: los 2 flares más brillantes al 40 %
    /// (la segunda capa del informe — asoma sobre el sprite).</summary>
    public class RelicarioAuraDelante : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
            => PlayerDrawLayers.AfterLastVanillaLayer;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            var mp = drawInfo.drawPlayer.GetModPlayer<RelicarioPlayer>();
            return mp.TimerAura > 0 && !Main.gameMenu;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            var mp = p.GetModPlayer<RelicarioPlayer>();
            int t = 480 - mp.TimerAura;
            float contraccion = 1f - mp.TimerAura / 480f;

            Vector2 pos = new Vector2(p.Center.X, p.Center.Y + p.gfxOffY)
                - Main.screenPosition;

            var flare = ModContent.Request<Microsoft.Xna.Framework.Graphics.Texture2D>(
                "AethonMod/Content/Effects/FlareEstrella4Puntas",
                ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            var orig = flare.Size() * 0.5f;

            Color dorado = new Color(223, 170, 40) * 0.5f * 0.4f;
            Color palido = new Color(245, 245, 149) * 0.5f * 0.4f;

            drawInfo.DrawDataCache.Add(new DrawData(
                flare, pos, null, dorado, t * 0.025f, orig,
                MathHelper.Lerp(1.25f, 0.05f, contraccion),
                SpriteEffects.None, 0f));
            drawInfo.DrawDataCache.Add(new DrawData(
                flare, pos, null, palido, -t * 0.0265f, orig,
                MathHelper.Lerp(1.1f, 0.05f, contraccion),
                SpriteEffects.None, 0f));
        }
    }
}
