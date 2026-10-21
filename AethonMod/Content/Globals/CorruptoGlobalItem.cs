using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  CORRUPTO GLOBAL ITEM (v6.50.129) — LA ESTADÍSTICA CORRUPTA de la
    //  letra: «entonces cualquier arma que caiga ahi o directamente
    //  desde el menu de creacion, se pueden corromper con la estadistica
    //  corrupto, al ser corrompida ya no puede tener etiquetas como
    //  superior, mitica, rota y cualquier otra etiqueta y el duende debe
    //  decir algo como esta arma esta corrompida no puedo hacer nada con
    //  ella».
    //
    //  EL SELLLO: un bool POR INSTANCIA (InstancePerEntity — viaja con el
    //  item: TagCompound al .plr, NetSend/NetReceive por el cable). El
    //  altar de la bruma lo pone (AltarDevoradorSistema.CorromperArma:
    //  además PELA el prefijo con Prefix(-3) — «ya no puede tener
    //  etiquetas»)… y NADIE lo quita: CanReforge le niega el yunque para
    //  SIEMPRE (el duende lo confiesa al hablarle — PuebloChismesNPC).
    //
    //  EL ARMA SIGUE SIENDO UN ARMA (sus stats intactos): la corrupción
    //  no la rompe — la MARCA. Y marcada es COMIDA: el Eterno la devora
    //  en la bruma (EXP por rareza + un stat por clase + el alma única
    //  si es especial).
    // ======================================================================
    public class CorruptoGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        /// <summary>¿Esta instancia lleva LA ESTADÍSTICA CORRUPTA?</summary>
        public bool Corrompido;

        // === LA PERSISTENCIA (viaja con el item a donde él vaya) ===

        public override void SaveData(Item item, TagCompound tag)
        {
            if (Corrompido) tag["corrompido"] = true;
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            Corrompido = tag.GetBool("corrompido");
        }

        public override void NetSend(Item item, BinaryWriter writer)
        {
            writer.Write(Corrompido);
        }

        public override void NetReceive(Item item, BinaryReader reader)
        {
            try { Corrompido = reader.ReadBoolean(); }
            catch { Corrompido = false; }
        }

        // === EL YUNQUE CERRADO («ya no puede tener etiquetas») ===

        public override bool CanReforge(Item item) => !Corrompido;

        // ================================================================
        //  v6.51.0 — LOS STATS EN % DE LAS ARMAS CORRUPTAS (la letra:
        //  «las armas corruptas obtienen un 5% de daño extra, además de
        //  otras estadísticas en porcentaje pero ya no pueden ser
        //  modificables ni mejorables»): +5 % de daño, +5 % de empuje,
        //  +2 % de crítico y 5 % MÁS RÁPIDAS — el precio de la eternidad
        //  (sin reforge, sin prefijos, sin mejora alguna: para siempre).
        // ================================================================

        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (!Corrompido) return;
            damage *= 1.05f;                 // +5 % de daño (la letra)
        }

        public override void ModifyWeaponKnockback(Item item, Player player, ref StatModifier knockback)
        {
            if (!Corrompido) return;
            knockback *= 1.05f;              // +5 % de empuje
        }

        public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
        {
            if (!Corrompido) return;
            crit *= 1.05f;                   // +5 % de crítico (TODAS las estadísticas en 5 %)
        }

        public override float UseTimeMultiplier(Item item, Player player)
        {
            if (!Corrompido) return 1f;
            return 0.95f;                    // 5 % más rápida
        }

        // === v6.50.130 — EL SELLO DEL APARTADO CREACIÓN: lo pone la
        //     RECETA misma (RecetasCorrupcionSistema.MarcarCorrupta, vía
        //     Recipe.AddOnCraftCallback — ESTE tML 2026.08 no trae el
        //     hook OnCraft en GlobalItem: el callback vive en la receta,
        //     firma (Recipe, Item, List<Item>, Item) verificada al IL). ===

        // === LA ETIQUETA Y EL LORE (donde vivía el prefijo) ===

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!Corrompido) return;
            try
            {
                // LA ETIQUETA: pegada al nombre (el puesto del «Superior»
                // que ya jamás volverá a ocupar).
                int idxNombre = -1;
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i].Name == "ItemName") { idxNombre = i; break; }
                }
                var violeta = new Color(186, 96, 236);
                var tenue = new Color(158, 108, 200);
                var bruma = new Color(122, 216, 182);
                var etiqueta = new TooltipLine(Mod, "CorruptoEtiqueta",
                    Language.GetTextValue("Mods.AethonMod.Items.ArmaCorruptaTag"))
                { OverrideColor = violeta };
                if (idxNombre >= 0) tooltips.Insert(idxNombre + 1, etiqueta);
                else tooltips.Add(etiqueta);

                tooltips.Add(new TooltipLine(Mod, "CorruptoLinea1",
                    Language.GetTextValue("Mods.AethonMod.Items.ArmaCorrupta1"))
                { OverrideColor = tenue });
                tooltips.Add(new TooltipLine(Mod, "CorruptoLinea2",
                    Language.GetTextValue("Mods.AethonMod.Items.ArmaCorrupta2"))
                { OverrideColor = tenue });
                tooltips.Add(new TooltipLine(Mod, "CorruptoLinea3",
                    Language.GetTextValue("Mods.AethonMod.Items.ArmaCorrupta3"))
                { OverrideColor = bruma });
            }
            catch { }
        }

        // === LA AURA DEL ARMA CAÍDA (se VE maldita en el suelo) ===

        public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch,
            Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (!Corrompido || Main.gameMenu || Main.netMode == NetmodeID.Server) return true;
            try
            {
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (glow != null)
                {
                    Vector2 pos = item.Center - Main.screenPosition;
                    float pulso = 0.26f + 0.10f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f);
                    spriteBatch.Draw(glow, pos, null, new Color(96, 40, 150) * pulso, 0f,
                        glow.Size() * 0.5f, new Vector2(40f, 40f) / glow.Size(),
                        SpriteEffects.None, 0f);
                    spriteBatch.Draw(glow, pos, null, new Color(150, 70, 220) * (pulso * 0.5f), 0f,
                        glow.Size() * 0.5f, new Vector2(18f, 18f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }
                if (Main.rand.NextBool(16))
                {
                    Dust d = Dust.NewDustPerfect(
                        item.Center + new Vector2(Main.rand.NextFloat(-8f, 8f),
                            Main.rand.NextFloat(-9f, 3f)),
                        DustID.Shadowflame, new Vector2(0f, -0.5f), 140,
                        new Color(120, 40, 180), 0.5f);
                    d.noGravity = true;
                }
            }
            catch { }
            return true;
        }
    }
}
