using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    //  CETRO DE LAS DAGAS DEL ETERNO — v6.127 — EL ARMA NUEVA DE LAS CINCO
    //  DAGAS (la letra del usuario: «las dagas no van en el grimorio por
    //  ahora, es contenido de prueba» + «crea un arma nueva que invoque
    //  las 5 dagas en la posición correcta, que tenga su propio contador
    //  de minions, y ese contador invoque los 11 ojos Y a la vez las 5
    //  dagas simultáneamente»).
    //
    //  EL CUADRO DEL ARMA:
    //   · SU PROPIO CONTADOR — PLANO, independiente del grimorio (nada
    //     de TechoOjosDe ni niveles): el escuadrón COMPLETO de la letra,
    //     11 OJOS + 5 DAGAS = 16 puestos. El arma de prueba invoca la
    //     formación ENTERA, no la escalera del libro.
    //   · UNA SOLA VARA — SIMULTÁNEA: cada uso invoca TODO lo que falte
    //     — los ojos que faltan hasta 11 Y las dagas que faltan hasta 5,
    //     en el MISMO golpe (a la vez, la letra exacta). Con el escuadrón
    //     completo, el uso refresca los iconos y lo anuncia.
    //   · LA POSICIÓN CORRECTA — la formación se ordena SOLA (la casa):
    //     los ojos cierran su círculo alrededor del espíritu del libro
    //     (SlotDeFormacion del OjoEternoMinion) y las CINCO DAGAS viven
    //     en su ANILLO ÍNTIMO de radio 32 (SlotDelAnillo del
    //     DagaEternaMinion — la guardia del portador, dentro de la
    //     corona de ojos).
    //   · EL DAÑO DEL GRIMORIO — la ley v6.51 viva: DanoOjosDelEterno
    //     (su base, el nivel del libro, el despertar — sin armadura ni
    //     accesorios del jugador).
    //   · ARMA DE PRUEBA — mana 0 (la regla de la Bolsa de las
    //     Investigaciones), receta de 5 maderas, vive en la bolsa 26.
    // ======================================================================
    public class CetroDagasEterno : ModItem
    {
        /// <summary>EL TECHO PROPIO — los ojos de la formación completa
        /// (el anillo que cierra exacto: interior 5 + exterior 6).</summary>
        public const int TechoOjos = 11;

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 46;
            Item.damage = 7;                        // la base del grimorio — la ley v6.51
            Item.useAnimation = Item.useTime = 24;
            Item.mana = 0;                          // arma de prueba — la regla de la bolsa
            Item.knockBack = 2f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item113;
            // v6.129 — LA CAUSA RAÍZ DE «NO INVOCABA NADA» (la letra: «el
            // arma cetro de las dagas del eterno no invoca nada, mejora eso
            // y revisa porque no invoca nada»): el cetro NACIÓ sin
            // Item.shoot — y el hook Shoot de tModLoader SOLO corre cuando
            // Item.shoot > 0 (el gatillo del uso: Player.ItemCheck solo
            // llama a Shoot si el ítem tiene un proyectil que disparar).
            // El grimorio de la casa lleva Item.shoot = 931 (Nightglow
            // vanilla) POR ESO: es SOLO el gatillo — aquí el gatillo es el
            // PROPIO escuadrón (el primer ojo), y Shoot devuelve false:
            // NADA de vanilla vuela, todo nace del Shoot nuestro.
            Item.shoot = ModContent.ProjectileType<
                global::AethonMod.Content.Projectiles.OjoEternoMinion>();
            Item.shootSpeed = 10f;
            // v6.129 — NO ES ARMA DE INVOCADOR (la letra: «haz que esta
            // arma no sea de invocador asi no se ven afectado los
            // minions»): el cetro era DamageClass.Summon y su escuadrón
            // cobraba minionSlots — la maquinaria vanilla de puestos
            // MATABA a los minions que no caben (los del jugador Y los
            // propios). El cetro pasa a LA CLASE DEL GRIMORIO (Magic — el
            // daño del escuadrón sigue la ley v6.51 con DanoOjosDelEterno,
            // independiente de la clase) y el escuadrón YA NO COBRA
            // puestos (minionSlots 0 en el ojo y la daga): ni desplaza a
            // los minions del jugador, ni muere por exceso de puestos.
            Item.DamageType = DamageClass.Magic;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Purple;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // === SU PROPIO CONTADOR (PLANO — 11 ojos + 5 dagas) ===
            int tipoOjo = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.OjoEternoMinion>();
            int tipoDaga = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.DagaEternaMinion>();

            int ojosVivos = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI && p.type == tipoOjo)
                    ojosVivos++;
            }
            int dagasVivas = global::AethonMod.Content.Projectiles.DagaEternaMinion.DagasVivas(player);

            // EL NIVEL DEL LIBRO (el daño del escuadrón sigue al grimorio
            // — la ley v6.51: su base, su nivel, su despertar; el nivel
            // público lee el libro en la mano o la barra rápida).
            int level = player.GetModPlayer<global::AethonMod.Content.Players.ShardPlayer>().NivelGrimorioPublico;
            if (level < 1) level = 1;
            int danoEscuadron = WeaponScaling.DanoOjosDelEterno(Item.damage, level, player);

            // === LO QUE FALTA — todo a la vez (la letra: «invoca los 11
            //     ojos Y a la vez las 5 dagas simultáneamente») ===
            int ojosNuevos = 0;
            var tiposNacidos = new List<int>();

            // LOS OJOS que faltan hasta el 11
            for (int k = ojosVivos; k < TechoOjos; k++)
            {
                Projectile.NewProjectile(source, player.Center, Vector2.Zero,
                    tipoOjo, danoEscuadron, knockback, player.whoAmI, 0f, 0f, level);
                ojosNuevos++;
            }

            // LAS DAGAS que faltan (la PRIMERA que falte de cada tipo — la
            // secuencia del stabber: el set se mantiene COMPLETO)
            for (int k = 0; k < global::AethonMod.Content.Projectiles.DagaEternaMinion.TiposMax; k++)
            {
                if (global::AethonMod.Content.Projectiles.DagaEternaMinion.ViveTipo(player, k))
                    continue;
                // LA POSICIÓN CORRECTA: nace sobre el portador — su
                // corona (la tiara sobre los ojos) se ordena sola.
                // v6.132 — ai[1] ES EL RestOffsetAngle DEL STABBER
                // (Calamity): el ángulo del puesto de descanso en el
                // abanico — las cinco repartidas (k/5 · 2π − π/2: la
                // corona ENTERA cubierta, nadie apilado).
                Projectile.NewProjectile(source, player.Center - Vector2.UnitY * 30f,
                    Vector2.Zero, tipoDaga, danoEscuadron, knockback, player.whoAmI,
                    k, k / (float)global::AethonMod.Content.Projectiles.DagaEternaMinion.TiposMax * MathHelper.TwoPi - MathHelper.PiOver2, level);
                tiposNacidos.Add(k);
            }
            int dagasNuevas = tiposNacidos.Count;

            // LOS ICONOS DEL ESCUADRÓN (el sostén de la casa: el reloj
            // nunca expira mientras el icono viva)
            player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.OjoEternoBuff>(), 18000);
            player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.DagaEternaBuff>(), 18000);

            if (Main.myPlayer == player.whoAmI)
            {
                if (ojosNuevos > 0 || tiposNacidos.Count > 0)
                {
                    // EL ANUNCIO DEL ESCUADRÓN — una sola vara, la formación entera
                    Main.NewText(Language.GetTextValue(
                        "Mods.AethonMod.Armas.CetroEscuadron", ojosNuevos, tiposNacidos.Count),
                        new Color(196, 138, 255));
                    // CADA DAGA NACIDA CANTA SU TÉCNICA (las claves de la
                    // v6.123 — un mensaje por daga que revele SU técnica)
                    foreach (int t in tiposNacidos)
                    {
                        Main.NewText(Language.GetTextValue(
                            "Mods.AethonMod.Armas.DagaInvocada" + (t + 1)),
                            new Color(196, 138, 255));
                    }
                }
                else
                {
                    // EL ESCUADRÓN YA ESTÁ COMPLETO — la vara refresca y lo anuncia
                    Main.NewText(Language.GetTextValue(
                        "Mods.AethonMod.Armas.CetroCompleto"),
                        new Color(150, 200, 255));
                }
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113);
            return false;   // todos los spawns son nuestros
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // EL CONTADOR EN LA MANO — el escuadrón se lee COMPLETO (ojos
            // + dagas, como el grimorio cantaba la línea de la v6.124 —
            // ahora la línea es del CETRO)
            Player portador = Main.LocalPlayer;
            int ojosVivos = 0;
            int tipoOjo = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.OjoEternoMinion>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == tipoOjo &&
                    p.owner == portador.whoAmI) ojosVivos++;
            }
            int dagasVivas = global::AethonMod.Content.Projectiles.DagaEternaMinion.DagasVivas(portador);

            tooltips.Add(new TooltipLine(Mod, "SectionEscuadron", "[c/BE78FD:═══ EL ESCUADRÓN DEL CETRO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "EscuadronCuenta",
                $"[c/E8D8FF:Ojos: {ojosVivos}/{TechoOjos}]  " +
                $"[c/D8B8FF:Dagas: {dagasVivas}/5]"));
            tooltips.Add(new TooltipLine(Mod, "EscuadronUso",
                "[c/BE78FD:Una sola vara: invoca TODO el escuadrón a la vez]"));

            // las líneas del pergamino (hjson ×3)
            void Linea(string clave)
            {
                string texto = Language.GetTextValue("Mods.AethonMod.Items.CetroDagasEterno." + clave);
                if (string.IsNullOrEmpty(texto) || texto.StartsWith("Mods."))
                    return;
                tooltips.Add(new TooltipLine(Mod, clave, texto));
            }
            Linea("Linea1");
            Linea("Linea2");
            Linea("Linea3");
        }

        public override void AddRecipes()
        {
            // EL PROTOCOLO DE LA CASA para las armas de investigación:
            // la madera de siempre (la vía de prueba).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
