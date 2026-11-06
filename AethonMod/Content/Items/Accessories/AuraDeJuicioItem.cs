using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Globals;
using AethonMod.Content.Projectiles;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Items.Accessories
{
    // ======================================================================
    //  AuraDeJuicioItem — v6.104 — EL AURA DE JUICIO (LA COPIA FIEL DEL
    //  «WARBANNER OF THE RIGHTEOUS» — mecánica y técnica VFX, arte de la
    //  casa).
    //
    //  LA LETRA DEL USUARIO: «veo que no entiendes el concepto del
    //  estandarte, investiga bien como funciona y como reacciona
    //  visualmente los enemigos, copia exactamente el codigo de
    //  Warbanner of the Righteous y sus textura, efectos vfx tecnica
    //  todo, en un nuevo item llamado Aura de Juicio y daselo al
    //  jugador, ademas todas estas cosas nuevas ponlas en una nueva
    //  bolsa, esto sera material de investigacion de auras».
    //
    //  LA INVESTIGACIÓN (el espejo público del código fuente, estudiado
    //  entero): el original es un ACCESORIO DE ESTANDARTE — el guerrero
    //  carga la tela y EL CAMPO DE BATALLA REACCIONA a su alrededor:
    //
    //   · LA CORRIENTE DE PODER (CalculateBonus): TODO el daño del
    //     portador se multiplica ×0.85..×1.15 según qué TAN CERCA esté
    //     el enemigo válido más cercano (720 px → −15%; 80 px → +30%
    //     de bono crudo, es decir +15% neto). LA TÉCNICA EXACTA: el
    //     punto más cercano del HITBOX del enemigo (Vector2.Clamp al
    //     rectángulo), remapeado 720→0.30 / 80→pleno.
    //   · LA MARCA DE QUEMADURA: cada enemigo dentro de 720 px (+ la
    //     mitad de su hitbox + 100 px de margen generoso) queda MARCADO
    //     180 ticks (refrescados dentro): arde con el golpe periódico
    //     (15 × el MEJOR daño de clase × nerf multitarjet × intensidad,
    //     50 de penetración de armadura, crítica posible) y con la
    //     INTENSIDAD ×1 (borde) → ×3 (pegado) — TODO vive en
    //     AuraJuicioGlobalNPC.
    //   · EL NERF MULTITARJET: más de 7 enemigos marcados a la vez y el
    //     daño de la quemadura cae hasta ×0.10 (Remap(targetCount, 7→1,
    //     0.10→1)) — el original castiga el fanatismo de las hordas.
    //   · EL VESTIDO (AuraJuicioLuz): CINCO CAPAS de bloom concéntrico
    //     Goldenrod↔Orange pulsando + DOS destellos rotando + las
    //     luciérnagas doradas cada 2 ticks — la técnica exacta del
    //     WarbannerLight original.
    //   · EL MEDIDOR: la barra «Fuerza del Estandarte» (−15%..+15%,
    //     SlateGray→Gold) — AuraJuicioMetroSistema.
    //
    //  LA LEY DE LA CASA (la limpieza v6.93): las MECÁNICAS y la
    //  TÉCNICA VFX se portan FIEL; el ARTE se RECREA de cero (la tira
    //  animada 32×390 forjada por tools/aura_juicio_v6104.py y la
    //  forja IA — cero píxel ajeno).
    // ======================================================================
    [AutoloadEquip(EquipType.Balloon)]
    public class AuraDeJuicioItem : ModItem
    {
        // ------------------------------------------------------------
        //  LOS NÚMEROS DEL ORIGINAL (la tabla exacta, sin un punto de
        //  más ni de menos).
        // ------------------------------------------------------------
        /// <summary>EL BONO MÁXIMO crudo por cercanía (+0.30 → +15%
        /// neto sobre el daño total: el rango del original va de
        /// −0.15 a +0.15 sobre TODO el daño).</summary>
        internal const float BonoMaximo = 0.3f;

        /// <summary>EL RADIO DEL AURA: 720 px (45 tiles — el campo de
        /// batalla entero alrededor del estandarte).</summary>
        internal const float DistanciaMax = 720f;

        /// <summary>LA DISTANCIA ÍNTIMA: a 80 px del enemigo el bono
        /// crudo es pleno (la técnica Remap del original).</summary>
        internal const float DistanciaIntima = 80f;

        /// <summary>EL ALCANCE DE LA BÚSQUEDA del enemigo más cercano
        /// (720 × 7 = 5040 px: el original IGNORA si el más cercano
        /// está fuera del aura — el bono lo da la distancia, no el
        /// radio).</summary>
        internal const float AlcanceBusqueda = DistanciaMax * 7f;

        /// <summary>EL MÁXIMO DE OBJETIVOS antes del nerf multitarjet.</summary>
        internal const int ObjetivosMaximos = 7;

        /// <summary>EL PISO del nerf multitarjet (×0.10 con 7+ marcas).</summary>
        internal const float MultiplicadorMultitarjetMinimo = 0.10f;

        /// <summary>EL DAÑO BASE por golpe de quemadura (antes de clase,
        /// multitarjet e intensidad — el «15» del original).</summary>
        internal const float DanoBaseQuemadura = 15f;

        // ------------------------------------------------------------
        //  EL MEDIDOR (los statics que lee AuraJuicioMetroSistema).
        //  Solo los escribe la UpdateAccessory del jugador LOCAL.
        // ------------------------------------------------------------
        /// <summary>LA FUERZA VIVA del estandarte (−0.15..+0.15 — el
        /// multiplicador neto sobre TODO el daño). El medidor la
        /// dibuja; se congela al desequipar (el sistema re-verifica
        /// el equipamiento antes de dibujar).</summary>
        public static float FuerzaViva;

        /// <summary>TRUE mientras el jugador local lleva el aura puesta
        /// (el medidor lo re-verifica contra los slots de armadura).</summary>
        public static bool Activo;

        public override string Texture => "AethonMod/Content/Items/Accessories/AuraDeJuicioItem";

        public override void SetStaticDefaults()
        {
            // LA TIRA ANIMADA — como el original: 5 marcos a 6 ticks
            // (la tela ondea, el sol del juicio late, las ascuas suben).
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 5, false));
            ItemID.Sets.AnimatesAsSoul[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 78;
            Item.accessory = true;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.buyPrice(0, 40, 0, 0);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // ==========================================================
            //  1) EL AURA VISIBLE — el ancla de luz que sigue al
            //     portador (una sola por dueño; si el portador pidió
            //     silencio visual o cayó, no nace — como el original).
            // ==========================================================
            int tipoLuz = ModContent.ProjectileType<AuraJuicioLuz>();
            if (player.ownedProjectileCounts[tipoLuz] < 1 && !hideVisual &&
                !player.dead && !Main.gameMenu)
            {
                Projectile.NewProjectile(player.GetSource_FromThis(),
                    player.MountedCenter, Vector2.Zero, tipoLuz, 0, 0f,
                    player.whoAmI);
            }

            // ==========================================================
            //  2) LA CORRIENTE DE PODER — el multiplicador de TODO el
            //     daño: CalculateBonus da 0..+0.30 crudos; el original
            //     los centra en −0.15..+0.15 (lejos de todo enemigo el
            //     portador pega al 85% — el estandarte EXIGE cercanía).
            //     v6.104 — LA VÍA DE LA CASA: este tML no trae
            //     StatModifier.MultiplicativeBonus — el multiplicador
            //     vive en AuraJuicioGlobalItem.ModifyWeaponDamage (EL
            //     patrón de la casa: damage *= 1f + bono) leyendo
            //     FuerzaViva (que aquí se escribe para el jugador local).
            float bono = CalcularBonus(player) - 0.15f;

            // EL MEDIDOR (solo el jugador local escribe los statics).
            if (player.whoAmI == Main.myPlayer)
            {
                FuerzaViva = bono;
                Activo = true;
            }

            // ==========================================================
            //  3) LA MARCA DE QUEMADURA — cada impío dentro del campo
            //     del estandarte queda marcado (el corazón vive en
            //     AuraJuicioGlobalNPC; aquí solo se SIEMBRA la marca,
            //     como el original).
            // ==========================================================
            MarcarElCampo(player, hideVisual);
        }

        // ============================================================
        //  EL CALCULO DEL BONO — el puerto EXACTO del CalculateBonus
        //  original (el enemigo válido más cercano en 5040 px — IGNORA
        //  si está fuera del aura: el bono lo mide la DISTANCIA AL
        //  HITBOX, de 720 px → 0.00 a 80 px → +0.30).
        // ============================================================
        private static float CalcularBonus(Player player)
        {
            float bono = 0f;
            NPC cercano = EnemigoMasCercano(player.Center, AlcanceBusqueda);
            if (cercano != null)
            {
                // EL PUNTO MÁS CERCANO DEL HITBOX (la técnica del
                // original: el Clamp al rectángulo — los jefes grandes
                // cuentan desde su piel, no desde su centro).
                Vector2 punto = Vector2.Clamp(player.Center, cercano.TopLeft, cercano.BottomRight);
                bono = Remap(Vector2.Distance(player.Center, punto),
                    DistanciaMax, DistanciaIntima, 0f, BonoMaximo);
            }
            return bono;
        }

        /// <summary>EL ENEMIGO VÁLIDO MÁS CERCANO (el equivalente de la
        /// extensión ClosestNPCAt del original, que este tML no trae:
        /// el bucle manual con el filtro de la casa VFXCore.EsObjetivo
        /// — activo, ni amigo, ni inmune). Null si no hay nadie en
        /// `alcance`.</summary>
        public static NPC EnemigoMasCercano(Vector2 desde, float alcance)
        {
            NPC mejor = null;
            float min = alcance * alcance;   // distancia al CUADRADO (sin raíz)
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !VFXCore.EsObjetivo(n))
                    continue;
                float d = n.Center.DistanceSQ(desde);
                if (d < min)
                {
                    min = d;
                    mejor = n;
                }
            }
            return mejor;
        }

        // ============================================================
        //  LA SIEMBRA DE LA MARCA — el puerto del bucle por NPC del
        //  UpdateAccessory original.
        // ============================================================
        private static void MarcarElCampo(Player player, bool ocultarEfectos)
        {
            // LA CUENTA PREVIA de los ya marcados POR ESTE PORTADOR (el
            // original cuenta las marcas antes del bucle: el nerf
            // multitarjet usa el número de OCUPANTES del campo).
            int cuentaObjetivos = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active)
                    continue;
                AuraJuicioGlobalNPC g = n.GetGlobalNPC<AuraJuicioGlobalNPC>();
                if (g.Marcada && g.Dueño == player.whoAmI + 1)
                    cuentaObjetivos++;
            }

            // LA CLASE DOMINANTE Y SU MODIFICADOR — una sola vez por
            // tick (la ley cero-GC: nada de recalcular por NPC).
            DamageClass claseDom = ClaseDominante(player);
            StatModifier modMejor = player.GetDamage(claseDom);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC nPC = Main.npc[i];
                if (nPC == null || !nPC.active)
                    continue;

                AuraJuicioGlobalNPC gNpc = nPC.GetGlobalNPC<AuraJuicioGlobalNPC>();

                // EL MARGEN GENEROSO — la mitad mayor del hitbox + 100 px
                // (los jefes grandes entran al campo antes: su PIEL cruza
                // el borde, no su centro — la técnica del original).
                float margen = Math.Max(nPC.Hitbox.Width / 2f, nPC.Hitbox.Height / 2f) + 100f;
                float dist = Vector2.Distance(player.Center, nPC.Center);

                // LA INTENSIDAD: ×1 en el borde del campo → ×3 PEGADO (la
                // técnica Remap del original — el corazón visual: el fuego
                // del impío ARDE MÁS CUANTO MÁS SE ACERCA).
                float intensidad = Remap(dist, DistanciaMax + margen, margen, 1f, 3f);

                if (dist < DistanciaMax + margen && VFXCore.EsObjetivo(nPC))
                {
                    // EL NERF MULTITARJET — 1 marca = daño pleno; 7+ =
                    // ×0.10 (el original castiga las hordas).
                    float multitarjet = Remap(cuentaObjetivos,
                        ObjetivosMaximos, 1, MultiplicadorMultitarjetMinimo, 1f);

                    // LA DIRECCIÓN hacia el impío (unitaria: portador →
                    // impío — las chispas se EXPULSAN al otro lado).
                    Vector2 hacia = nPC.Center - player.Center;
                    if (hacia != Vector2.Zero)
                        hacia /= hacia.Length();
                    else
                        hacia = Vector2.UnitX;

                    // LA MARCA — recién puesta o refrescada: 180 ticks de
                    // fuego que SIGUEN ardiendo ya fuera del campo.
                    if (!gNpc.Marcada)
                    {
                        gNpc.DireccionQuemadura = hacia;
                        gNpc.Marcada = true;
                        gNpc.TimerQuemadura = 180;
                        gNpc.TiempoGolpe = 0;
                    }
                    gNpc.IntensidadQuemadura = intensidad;
                    gNpc.DireccionQuemadura = hacia;

                    // EL DAÑO DE LA QUEMADURA — 15 × el MEJOR daño de
                    // clase del portador × multitarjet (el golpe final
                    // multiplica por intensidad en el corazón).
                    int danoQuemadura = (int)modMejor
                        .ApplyTo(DanoBaseQuemadura * multitarjet);
                    gNpc.DanoQuemadura = danoQuemadura;

                    // EL APILAMIENTO — cada tick dentro del campo suma un
                    // stack (300 = el BURST del corazón); el timer se
                    // refresca mientras el impío permanezca.
                    gNpc.StacksQuemadura++;
                    gNpc.TimerQuemadura = 180;
                    gNpc.OcultarEfectos = ocultarEfectos;
                    gNpc.Dueño = player.whoAmI + 1;
                }
            }
        }

        // ============================================================
        //  LA CLASE DOMINANTE — el «MejorDañoDeClase» del original:
        //  entre Melé, Distancia, Magia y Invocación, la que más daño
        //  multiplicaría el golpe base de la quemadura. La lee el
        //  AuraJuicioDano para calzar SU DamageType (así el golpe
        //  hereda crítica y penetración de la MEJOR clase).
        // ============================================================
        public static DamageClass ClaseDominante(Player player)
        {
            DamageClass mejor = DamageClass.Melee;
            float mejorValor = player.GetDamage<MeleeDamageClass>().ApplyTo(1f);
            float v = player.GetDamage<RangedDamageClass>().ApplyTo(1f);
            if (v > mejorValor) { mejorValor = v; mejor = DamageClass.Ranged; }
            v = player.GetDamage<MagicDamageClass>().ApplyTo(1f);
            if (v > mejorValor) { mejorValor = v; mejor = DamageClass.Magic; }
            v = player.GetDamage<SummonDamageClass>().ApplyTo(1f);
            if (v > mejorValor) { mejorValor = v; mejor = DamageClass.Summon; }
            return mejor;
        }

        /// <summary>EL MAYOR CRÍTICO DE CLASE del portador (el original:
        /// el golpe de quemadura «puede crítico» con el mejor de
        /// melé/distancia/magia/invocación).</summary>
        public static float MejorCritica(Player player)
        {
            float mejor = player.GetCritChance<MeleeDamageClass>();
            float v = player.GetCritChance<RangedDamageClass>();
            if (v > mejor) mejor = v;
            v = player.GetCritChance<MagicDamageClass>();
            if (v > mejor) mejor = v;
            v = player.GetCritChance<SummonDamageClass>();
            if (v > mejor) mejor = v;
            return mejor;
        }

        // ============================================================
        //  EL DIBUJO — la tira es alta (5 marcos de 32×78): el marco
        //  vivo LLEGA en el parámetro frame (la lección v6.100.1:
        //  JAMÁS Main.itemFrame — ese array es vanilla y revienta).
        //  Escala 0.6 y offset (0, −2): el ajuste exacto del original.
        // ============================================================
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D tex = TextureAssets.Item[Type].Value;
            float esc = scale * 0.6f;
            spriteBatch.Draw(tex, position - new Vector2(0f, 2f * scale),
                frame, drawColor, 0f, Utils.Size(frame) * 0.5f, esc,
                SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            // EL ÍTEM EN EL SUELO: el helper oficial (el mismo del
            // Estandarte del Juicio — seguro para mods, SIEMPRE).
            Main.GetItemDrawFrame(Type, out Texture2D tex, out Rectangle marco);
            spriteBatch.Draw(tex, Item.position - Main.screenPosition,
                marco, lightColor, rotation, Utils.Size(marco) * 0.5f, scale,
                SpriteEffects.None, 0f);
            return false;
        }

        /// <summary>¿El jugador lleva EL AURA DE JUICIO puesta? (los
        /// slots 3..10 de la armadura — la revisa el ancla de luz y el
        /// medidor; la copia del inventario NO cuenta: hay que
        /// PORTARLA).</summary>
        public static bool LlevaElAura(Player player)
        {
            if (player == null || !player.active)
                return false;
            int tipo = ModContent.ItemType<AuraDeJuicioItem>();
            for (int i = 3; i <= 10 && i < player.armor.Length; i++)
            {
                Item a = player.armor[i];
                if (a != null && !a.IsAir && a.type == tipo)
                    return true;
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // EL PERGAMINO DEL JUICIO — las claves viven en el hjson
            // (×3 idiomas; sin clave, la línea no se dibuja — nada de
            // texto duro salvo el título de respaldo).
            void Linea(string clave, string respaldo)
            {
                string texto = Language.GetTextValue("Mods.AethonMod.Items.AuraDeJuicioItem." + clave);
                if (string.IsNullOrEmpty(texto) || texto.StartsWith("Mods."))
                    texto = respaldo;
                if (!string.IsNullOrEmpty(texto))
                    tooltips.Add(new TooltipLine(Mod, clave, texto));
            }
            Linea("Titulo", "[c/FFC850:═══ EL AURA DE JUICIO ═══]");
            Linea("Linea1", "");
            Linea("Linea2", "");
            Linea("Linea3", "");
            Linea("Linea4", "");
        }

        /// <summary>El Remap acotado del original del estandarte (la
        /// técnica de las tres rampas: intensidad, bono y multitarjet
        /// — todo vive aquí).</summary>
        internal static float Remap(float v, float in1, float in2, float out1, float out2)
        {
            float k = (v - in1) / (in2 - in1);
            if (k < 0f) k = 0f;
            else if (k > 1f) k = 1f;
            return out1 + (out2 - out1) * k;
        }
    }

    // ======================================================================
    //  v6.104 — AURA JUICIO GLOBAL ITEM — LA CORRIENTE DE PODER (la
    //  técnica del original: TODO el daño del portador ×(0.85..1.15)
    //  según la cercanía del enemigo — el warbannerDamageMult de
    //  Calamity hecho casa): este tML no trae
    //  StatModifier.MultiplicativeBonus, así que el multiplicador vive
    //  aquí — EL PATRÓN DE LA CASA (damage *= X en
    //  ModifyWeaponDamage, como el +5% del consagrado) — leyendo
    //  FuerzaViva, que la UpdateAccessory del estandarte escribe cada
    //  tick para el jugador local (en MP cada cliente computa SU
    //  propio bono: el estandarte es del que lo porta).
    // ======================================================================
    public class AuraJuicioGlobalItem : GlobalItem
    {
        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            try
            {
                if (player == null || !player.active) return;
                if (player.whoAmI != Main.myPlayer) return;       // solo el local computa
                if (!AuraDeJuicioItem.LlevaElAura(player)) return;
                float bono = AuraDeJuicioItem.FuerzaViva;         // −0.15..+0.15
                if (bono == 0f) return;
                damage *= 1f + bono;                              // TODO el daño ×(0.85..1.15)
            }
            catch { }
        }
    }
}
