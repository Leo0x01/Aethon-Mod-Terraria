using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using ReLogic.Content;
using AethonMod.Content.Dusts;

namespace AethonMod.Content.Items.Investigaciones
{
    // ======================================================================
    //  v6.85 → v6.89 — LA INVESTIGACIÓN R1, REHECHA CON EL ORIGINAL EN LA
    //  MANO (la letra: «el accesorio el relicario del avispon tampoco
    //  funciona como deberia se nota que ni visitaste el codigo del
    //  original, ni sus tecnicas de como funciona, ni sus texturas ni
    //  nada»).
    //
    //  ESTA VEZ SÍ: el fuente ORIGINAL descargado y leído entero —
    //  SkippZzMods/Bombus-Apis-Bee, rama master (clon público), el
    //  accesorio LIHZAARDIAN HORNET RELIC:
    //
    //  · Core/Jungle/Items/LihzardianHornetRelic/LihzardianHornetRelic.cs
    //    — la pasiva: moveSpeed += 0,35 · wingFlightTimeBoost += 0,35.
    //  · Core/BombusApisBeePlayer.cs — LA ACTIVACIÓN (ProcessTriggers):
    //    hotkey L «JustPressed» + sin el buff de recarga → SoundID.Item74
    //    .PlayWith(center, −0,15, 0,1, 1,25) · AddBuff(Recarga, 2700) —
    //    ¡EN EL MISMO INSTANTE DE PULSAR (la v6.85 la ponía al APAGARSE:
    //    53 s de ciclo en vez de 45!) · Timer = 480 · BURST de 25 dusts
    //    (NextVector2Circular 4,25/5,25, esc 0,55, los DOS colores).
    //    EL CANAL (UpdateEquips): +12 daño/crít/velocidad-de-uso ·
    //    ModifyHitByNPC: SourceDamage ×1,1 · el anillo de dusts:
    //    NextVector2CircularEdge(Lerp(85→5, 1−t/480)) ×2/tick, colores
    //    (222,173,40)·0,45 vel 0,25 y (245,245,149)·0,35 vel 0,75.
    //  · Core/BombusApisBee_DoDetours.cs — LA TÉCNICA DEL AURA (lo que la
    //    v6.85 NO copió): un DETOUR de Main.DrawInfernoRings que dibuja
    //    CINCO pasadas alrededor del jugador con las TEXTURAS DEL
    //    ORIGINAL (BloomFlare + GlowAlpha, ahora viajan en el mod como
    //    Relicario_BloomFlare.png y Relicario_GlowAlpha.png):
    //      tex(223,170,40)·0,5   rot t·0,025    esc Lerp(1,25→0,05)
    //      tex(245,245,149)·0,5  rot −t·0,0265  esc Lerp(1,1→0,05)
    //      tex(245,245,149)·0,55 rot t·0,0165   esc Lerp(0,9→0,05)
    //      bloom(223,170,40)·0,5 rot t·0,025    esc Lerp(1,25→0,05)
    //      bloom(245,245,149)·0,5 rot t·0,0265  esc Lerp(1,1→0,05)
    //    con t = EL TIMER (que DECRECE: la rotación va HACIA ATRÁS — la
    //    v6.85 usaba la edad y giraba al revés) y el Lerp con 1−t/480
    //    (el sol SE CIERRA: de 1,25× a 0,05× al apagarse).
    //  · Buffs/CooldownBuffs.cs — la recarga: debuff, 2700 ticks (45 s),
    //    NurseCannotRemove, buffNoSave.
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
            // LA PASIVA (la del original, exacta): velocidad y vuelo.
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
    /// stats, el drenaje y el anillo de dusts — TODO como el original.</summary>
    public class RelicarioPlayer : ModPlayer
    {
        /// <summary>¿Lleva la reliquia equipada?</summary>
        public bool TieneRelicario;

        /// <summary>El timer del aura (480 → 0; 0 = apagado).</summary>
        public int TimerAura;

        /// <summary>LA TECLA (registrada en AethonMod.Load — la L del original).</summary>
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
            if (!TieneRelicario || Player.HasBuff<global::AethonMod.Content.Buffs.RelicarioCdBuff>()) return;

            // === LA CANALIZACIÓN DEL SOL (la activación — COMO EL ORIGINAL:
            //     el buff de recarga nace EN EL MISMO INSTANTE de pulsar (no
            //     al apagarse): 45 s de ciclo, de verdad. El sonido con sus
            //     tres números: pitch −0,15 · variación 0,1 · volumen 1,25. ===
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item74.WithPitchOffset(-0.15f).WithVolumeScale(1.25f), Player.Center);
            Player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.RelicarioCdBuff>(), 2700);
            TimerAura = 480;

            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                // EL BURST: 25 dusts alternando los dos colores — vel circular
                // 4,25 y 5,25 (la tabla EXACTA del original), esc 0,55.
                for (int i = 0; i < 25; i++)
                {
                    bool dorado = i % 2 == 0;
                    Dust d = Dust.NewDustPerfect(Player.Center,
                        ModContent.DustType<SolarGlowDust>(),
                        Main.rand.NextVector2Circular(dorado ? 4.25f : 5.25f, dorado ? 4.25f : 5.25f),
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

            // === EL ANILLO DE POLVOS (2/tick convergiendo — el UpdateEquips
            //     del original: radio Lerp(85,5, 1−t/480), dos dusts/tick) ===
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && TimerAura > 0)
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
            // LOS STATS ACTIVOS (+12/12/12 — genéricos: todas las clases;
            //     el original los da a su clase apícola, la nuestra es TODAS).
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
            // la regen positiva anulada (la promesa del tooltip original:
            // «take damage-over-time while channeling»).
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

    // ======================================================================
    //  v6.89 — EL AURA DEL SOL: LA TÉCNICA DEL ORIGINAL (Bombus Apis Bee
    //  la dibuja en un DETOUR de Main.DrawInfernoRings — el mismo sitio
    //  donde vanilla dibuja LOS ANILLOS DEL INFIERNO): cinco pasadas
    //  alrededor del jugador con LAS TEXTURAS DEL ORIGINAL (BloomFlare y
    //  GlowAlpha — viajan en el mod como Relicario_BloomFlare.png y
    //  Relicario_GlowAlpha.png), la rotación sobre EL TIMER (que decrece:
    //  gira hacia atrás, como el original) y el cierre del sol (el Lerp
    //  con 1 − t/480: de 1,25× a 0,05×). La v6.85 la dibujaba en DOS
    //  PlayerDrawLayer con texturas propias y la edad al revés — ya no.
    // ======================================================================
    public class RelicarioAuraSistema : ModSystem
    {
        private static bool _cargada;
        private static Asset<Texture2D> _texFlare;
        private static Asset<Texture2D> _texBloom;

        public override void Load()
        {
            _cargada = false;
            On_Main.DrawInfernoRings += DibujarElAura;
        }

        public override void Unload()
        {
            On_Main.DrawInfernoRings -= DibujarElAura;
            _texFlare = null;
            _texBloom = null;
            _cargada = false;
        }

        private static void CargarTexturas()
        {
            if (_cargada) return;
            _cargada = true;
            try
            {
                _texFlare = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Items/Investigaciones/Relicario_BloomFlare",
                    ReLogic.Content.AssetRequestMode.ImmediateLoad);
                _texBloom = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Items/Investigaciones/Relicario_GlowAlpha",
                    ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            catch { }
        }

        private static void DibujarElAura(Terraria.On_Main.orig_DrawInfernoRings orig, Main self)
        {
            try
            {
                if (!Main.dedServ && !Main.gameMenu)
                {
                    CargarTexturas();
                    Texture2D tex = _texFlare?.Value;
                    Texture2D texBloom = _texBloom?.Value;
                    if (tex != null && texBloom != null)
                    {
                        for (int i = 0; i < Main.maxPlayers; i++)
                        {
                            Player player = Main.player[i];
                            if (player == null || !player.active || player.outOfRange || player.dead)
                                continue;
                            var mp = player.GetModPlayer<RelicarioPlayer>();
                            if (mp == null || mp.TimerAura <= 0) continue;

                            // LA POSICIÓN (la del original: centro + gfxOffY).
                            Vector2 pos = new Vector2(player.Center.X, player.Center.Y + player.gfxOffY)
                                - Main.screenPosition;

                            // LAS CINCO PASADAS (Core/BombusApisBee_DoDetours.cs
                            // — la tabla EXACTA, byte a byte: rotación sobre EL
                            // TIMER (decrece → gira hacia atrás), escala con el
                            // cierre del sol Lerp(a, 0,05, 1 − t/480)).
                            float t = mp.TimerAura;
                            Main.spriteBatch.Draw(tex, pos, null,
                                new Color(223, 170, 40, 0) * 0.5f,
                                t * 0.025f, tex.Size() * 0.5f,
                                MathHelper.Lerp(1.25f, 0.05f, 1f - mp.TimerAura / 480f),
                                SpriteEffects.None, 0f);
                            Main.spriteBatch.Draw(tex, pos, null,
                                new Color(245, 245, 149, 0) * 0.5f,
                                t * -0.0265f, tex.Size() * 0.5f,
                                MathHelper.Lerp(1.1f, 0.05f, 1f - mp.TimerAura / 480f),
                                SpriteEffects.None, 0f);
                            Main.spriteBatch.Draw(tex, pos, null,
                                new Color(245, 245, 149, 0) * 0.55f,
                                t * 0.0165f, tex.Size() * 0.5f,
                                MathHelper.Lerp(0.9f, 0.05f, 1f - mp.TimerAura / 480f),
                                SpriteEffects.None, 0f);
                            Main.spriteBatch.Draw(texBloom, pos, null,
                                new Color(223, 170, 40, 0) * 0.5f,
                                t * 0.025f, tex.Size() * 0.5f,
                                MathHelper.Lerp(1.25f, 0.05f, 1f - mp.TimerAura / 480f),
                                SpriteEffects.None, 0f);
                            Main.spriteBatch.Draw(texBloom, pos, null,
                                new Color(245, 245, 149, 0) * 0.5f,
                                t * 0.0265f, tex.Size() * 0.5f,
                                MathHelper.Lerp(1.1f, 0.05f, 1f - mp.TimerAura / 480f),
                                SpriteEffects.None, 0f);
                        }
                    }
                }
            }
            catch { }
            orig(self);
        }
    }
}
