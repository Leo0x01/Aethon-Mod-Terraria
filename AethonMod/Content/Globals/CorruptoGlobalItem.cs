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
            // v6.89 — LA RAREZA VUELVE A VESTIRSE (rare no viaja en el save:
            //     SetDefaults lo devuelve a su rareza ORIGINAL al cargar —
            //     aquí se re-aplica la corrupta para que el nombre tenga SU
            //     color en TODAS partes (tooltip Y barra rápida).
            if (Corrompido) VestirLaRareza(item);
        }

        public override void NetSend(Item item, BinaryWriter writer)
        {
            writer.Write(Corrompido);
        }

        public override void NetReceive(Item item, BinaryReader reader)
        {
            try { Corrompido = reader.ReadBoolean(); }
            catch { Corrompido = false; }
            if (Corrompido) VestirLaRareza(item);
        }

        /// <summary>v6.89 — EL ARMA CORRUPTA VISTE SU RAREZA: el color de
        /// su nombre es el MISMO en el inventario y en la barra de acceso
        /// rápido (la letra: «el color del nombre es diferente en el
        /// inventario que en la barra de acceso rapido» — ya no: UNA sola
        /// fuente, la ModRarity corrupta, negro tinta (30,30,40)).</summary>
        public static void VestirLaRareza(Item item)
        {
            try
            {
                if (item == null || item.IsAir) return;
                int corrupta = ModContent.RarityType
                    <global::AethonMod.Content.Rarities.RarityCorrupta>();
                if (item.rare != corrupta) item.rare = corrupta;
            }
            catch { }
        }

        // === EL YUNQUE CERRADO («ya no puede tener etiquetas») ===

        public override bool CanReforge(Item item) => !Corrompido;

        // ================================================================
        //  v6.89 — EL PRECIO DE LA CORRUPCIÓN (la letra: «para corromper
        //  algo necesita esencia corrupta en dependencia de la calidad
        //  del arma, el tipo de arma, el daño del arma y sus
        //  estadisticas o efecto, no es lo mismo corromper una espada de
        //  cobre que una espada de adamantita, la de cobre pide menos
        //  esencia corrupta»): EL ALTAR y LA RECETA cobran LO MISMO —
        //  Esencias del Eterno según:
        //   · LA CALIDAD (la rareza: 5 lo más barato · 340 en la púrpura);
        //   · EL DAÑO (+1 esencia por cada 10 de daño);
        //   · EL TIPO (el grimorio es magia: melee ×1,0 · rango ×1,1 ·
        //     magia ×1,2 · invocación ×1,3);
        //   · SUS ESTADÍSTICAS (crítico alto, empuje alto o uso rápido
        //     suman su prima).
        // ================================================================

        /// <summary>CUÁNTAS ESENCIAS DEL ETERNO cuesta corromper ESTA
        /// arma (la fórmula única del altar y de las recetas).</summary>
        public static int CosteCorrupcion(Item item)
        {
            try
            {
                if (item == null || item.IsAir) return 1;
                // LA CALIDAD — la columna de la rareza (de vanilla −1..10).
                int[] porCalidad = { 5, 8, 15, 25, 40, 60, 80, 110, 150, 200, 260, 340 };
                int calidad = Math.Clamp(item.rare + 1, 0, 11);
                int coste = porCalidad[calidad];
                // EL DAÑO — la de adamantita pega más que la de cobre.
                coste += Math.Max(0, item.damage) / 10;
                // SUS ESTADÍSTICAS — la prima del filo fino.
                if (item.crit >= 8) coste += 10;
                if (item.knockBack >= 6f) coste += 10;
                if (item.useTime > 0 && item.useTime <= 10) coste += 15;
                // EL TIPO — el Eterno es magia: lo mágico cuesta más.
                float tipo = 1.0f;
                if (item.DamageType == DamageClass.Magic) tipo = 1.2f;
                else if (item.DamageType == DamageClass.Summon) tipo = 1.3f;
                else if (item.DamageType == DamageClass.Ranged) tipo = 1.1f;
                return Math.Max(1, (int)(coste * tipo));
            }
            catch { return 20; }
        }

        /// <summary>¿Cuántas Esencias del Eterno lleva el jugador encima?</summary>
        public static int EsenciasEnLaBolsa(Player p)
        {
            if (p == null) return 0;
            int tipo = ModContent.ItemType
                <global::AethonMod.Content.Items.Altar.ItemCorrupto>();
            int total = 0;
            for (int k = 0; k < 58; k++)
            {
                Item it = p.inventory[k];
                if (it == null || it.IsAir || it.type != tipo) continue;
                total += it.stack;
            }
            return total;
        }

        /// <summary>EL COBRO DEL ALTAR: quita las esencias del inventario
        /// del portador (de la pila que sea). Devuelve false si no llega.</summary>
        public static bool PagarCorrupcion(Player p, int coste)
        {
            if (p == null || coste <= 0) return true;
            if (EsenciasEnLaBolsa(p) < coste) return false;
            int tipo = ModContent.ItemType
                <global::AethonMod.Content.Items.Altar.ItemCorrupto>();
            int falta = coste;
            for (int k = 0; k < 58 && falta > 0; k++)
            {
                Item it = p.inventory[k];
                if (it == null || it.IsAir || it.type != tipo) continue;
                int quita = Math.Min(falta, it.stack);
                it.stack -= quita;
                falta -= quita;
                if (it.stack <= 0) it.TurnToAir();
            }
            return true;
        }

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
                // v6.89 — LA RAREZA VISTE EL NOMBRE (la letra: «el color del
                //     nombre es diferente en el inventario que en la barra
                //     de acceso rapido»): ya NO se fuerza un OverrideColor
                //     que solo vive en el tooltip — el arma corrupta lleva
                //     SU PROPIA ModRarity (negro tinta) y vanilla pinta el
                //     nombre con ELLA en el tooltip, en la barra rápida y
                //     en todas partes: UN color, UNA fuente.
                VestirLaRareza(item);
                int idxNombre = -1;
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i].Name == "ItemName") { idxNombre = i; break; }
                }
                var tenue = new Color(158, 108, 200);
                var bruma = new Color(122, 216, 182);
                if (idxNombre >= 0)
                {
                    string completo = tooltips[idxNombre].Text + " " +
                        Language.GetTextValue("Mods.AethonMod.Items.ArmaCorruptaTag");
                    // v6.97 — EL COLOR DE CONSAGRADO (la letra: «el color
                    //     de consagrado en su nombre debe ser centro negro,
                    //     medios grises y bordes blancos»): el degradado de
                    //     la casa — BORDES blancos → MEDIOS grises → CENTRO
                    //     negro — letra por letra (los tags [c/] del
                    //     tooltip pintan cada carácter con SU tinta: la
                    //     única forma de un degradado real en una línea).
                    tooltips[idxNombre].Text = NombreConsagrado(completo);
                }

                // v6.90 — LA LETRA NUEVA: «si lo que corrompes es un arma
                //     salen estadisticas, pero si se corrompe otro item
                //     como un lingote de oro, entonces SOLO sale la
                //     etiqueta al lado del nombre» — el ARMA consagrada
                //     cuenta SUS estadísticas y su lore; el MATERIAL
                //     (lingote, gema, cera…) lleva SOLO la etiqueta y la
                //     línea del porqué (la ofrenda del despertar).
                bool esArma = item.damage > 0;
                if (esArma)
                {
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
                else
                {
                    tooltips.Add(new TooltipLine(Mod, "CorruptoLinea3",
                        Language.GetTextValue("Mods.AethonMod.Items.ArmaCorrupta3"))
                    { OverrideColor = bruma });
                }
            }
            catch { }
        }

        /// <summary>v6.97 — EL NOMBRE CONSAGRADO: «el color de
        /// consagrado en su nombre debe ser centro negro, medios grises
        /// y bordes blancos» — el degradado simétrico de la casa, letra
        /// por letra: los bordes nacen BLANCOS, los medios viran a
        /// GRISES y el centro muere en NEGRO.</summary>
        private static string NombreConsagrado(string texto)
        {
            try
            {
                if (string.IsNullOrEmpty(texto)) return texto;
                var borde = new Color(235, 235, 240);
                var medio = new Color(145, 145, 155);
                var centro = new Color(20, 20, 26);
                var sb = new System.Text.StringBuilder(texto.Length * 22);
                int n = texto.Length;
                for (int i = 0; i < n; i++)
                {
                    // t: 0 en los bordes → 1 en el centro del nombre.
                    float t = n <= 1 ? 1f : 1f - MathF.Abs(2f * i / (n - 1) - 1f);
                    Color c = t < 0.5f
                        ? Color.Lerp(borde, medio, t * 2f)
                        : Color.Lerp(medio, centro, (t - 0.5f) * 2f);
                    sb.Append("[c/").Append(c.R.ToString("X2"))
                      .Append(c.G.ToString("X2")).Append(c.B.ToString("X2"))
                      .Append(':').Append(texto[i]).Append(']');
                }
                return sb.ToString();
            }
            catch { return texto; }
        }

        // === v6.88 — EL VELO DEL INVENTARIO (la letra: «se cubren con
        //     un brillo morado negro, este brillo podria ser un shader
        //     que cubra el item» — v6.97: LA LETRA NUEVA lo pide NEGRO:
        //     «todo item consagrado debe tener un aura negra con bordes
        //     difuminados en una gradiente y el aura debe ser negra
        //     clara»): el velo ahora es NEGRO CLARO (la tinta de la
        //     consagración) y el glow que respira por detrás viste el
        //     mismo gris humo. En el suelo ya vive su aura (abajo); el
        //     inventario la pedía a gritos. ===
        public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch,
            Vector2 position, Rectangle frame, Color drawColor, Color itemColor,
            Vector2 origin, float scale)
        {
            if (!Corrompido || Main.gameMenu) return;
            try
            {
                Texture2D tex = Terraria.GameContent.TextureAssets.Item[item.type].Value;
                if (tex == null) return;
                // EL VELO: el sprite encima de sí mismo, teñido NEGRO CLARO
                // (pulso 2,4 Hz — la misma respiración del aura del suelo).
                float pulso = 0.42f + 0.16f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f);
                spriteBatch.Draw(tex, position, frame,
                    new Color(24, 22, 32, (int)(255f * pulso)) * (pulso * 0.55f),
                    0f, origin, scale, SpriteEffects.None, 0f);
                // EL GLOW: la bruma gris humo que respira por detrás del slot.
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (glow != null)
                    spriteBatch.Draw(glow, position, null,
                        new Color(70, 68, 82) * (0.16f + 0.07f * pulso), 0f,
                        glow.Size() * 0.5f, new Vector2(34f, 34f) / glow.Size(),
                        SpriteEffects.None, 0f);
            }
            catch { }
        }

        // === v6.97 — EL AURA NEGRA DEL CONSAGRADO CAÍDO (la letra: «todo
        //     item consagrado debe tener un aura negra con bordes
        //     difuminados en una gradiente y el aura debe ser negra clara
        //     y con la forma del item en cuestion o similar»): EL PROPIO
        //     SPRITE del item dibujado en NEGRO CLARO, capa sobre capa
        //     detrás de él — cada capa MÁS GRANDE y MÁS TENUE: los bordes
        //     se difuminan en la gradiente y el aura lleva LA FORMA del
        //     item (nada de círculos genéricos). El viejo glow morado de
        //     la v6.88 murió con la letra nueva. ===

        public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch,
            Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (!Corrompido || Main.gameMenu || Main.netMode == NetmodeID.Server) return true;
            try
            {
                Texture2D tex = Terraria.GameContent.TextureAssets.Item[item.type].Value;
                if (tex != null)
                {
                    Vector2 pos = item.Center - Main.screenPosition;
                    // LA RESPIRACIÓN: el aura late despacio (2,4 Hz — la
                    //     misma del velo del inventario: UNA sola alma).
                    float pulso = 0.72f + 0.28f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f);
                    Rectangle frame = tex.Frame();
                    Vector2 orig = frame.Size() * 0.5f;
                    // LAS CAPAS DE LA GRADIENTE: negra clara cerca, casi
                    //     humo lejos — los BORDES DIFUMINADOS de la letra.
                    float[] escalas = { 1.14f, 1.32f, 1.54f, 1.80f };
                    float[] alfas = { 0.36f, 0.22f, 0.13f, 0.07f };
                    for (int k = 0; k < escalas.Length; k++)
                    {
                        float a = alfas[k] * pulso;
                        if (a <= 0f) continue;
                        spriteBatch.Draw(tex, pos, frame,
                            new Color(26, 24, 34) * a, rotation, orig,
                            scale * escalas[k], SpriteEffects.None, 0f);
                    }
                }
                // LA BRUMA OSCURA que se deshace hacia arriba (el alma del
                //     aura — ahora humo negro, no violeta).
                if (Main.rand.NextBool(16))
                {
                    Dust d = Dust.NewDustPerfect(
                        item.Center + new Vector2(Main.rand.NextFloat(-8f, 8f),
                            Main.rand.NextFloat(-9f, 3f)),
                        DustID.Shadowflame, new Vector2(0f, -0.5f), 140,
                        new Color(38, 32, 52), 0.5f);
                    d.noGravity = true;
                }
            }
            catch { }
            return true;
        }
    }
}
