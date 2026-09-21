using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.Projectiles.Cosmic;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Cosmic
{
    /// <summary>
    /// MetronomoPulsar — v6.42 — APUESTA 1: EL METRÓNOMO DE PÚLSAR.
    ///
    /// El arma que dispara AL COMPÁS: un púlsar en miniatura flota sobre
    /// tu cabeza mientras la sostienes, latiendo cada 0,5 s con un anillo
    /// que se contrae hacia el tic.
    ///
    ///   · Disparar a ±3 ticks del tic (PERFECT): crítico garantizado +
    ///     ABANICO DOBLE (dos pulsos hermanos a ±0,18 rad, 60% de daño).
    ///   · Disparar a ±6 ticks (GOOD): crítico garantizado.
    ///   · Fuera de compás: el disparo sale tal cual y el combo vuelve a
    ///     0 (el ritmo premia, nunca castiga).
    ///   · 8 aciertos seguidos: EL FARO — 3 segundos de haz giratorio de
    ///     900 px que pica a todo lo que barre.
    ///
    /// ARMA DE PRUEBAS — sin coste de maná.
    /// </summary>
    public class MetronomoPulsar : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 65;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30; Item.height = 30;
            Item.useTime = 12; Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;                       // SIN MANÁ (la regla de la casa)
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item9;
            Item.shoot = ModContent.ProjectileType<PulsoPulsarProjectile>();
            Item.shootSpeed = 14f;
        }

        public override void HoldItem(Player player)
        {
            // EL COMPÁS VIVO: el púlsar flotando sobre el portador
            // (compañero local — solo el jugador propio lo invoca).
            if (Main.myPlayer == player.whoAmI && Main.GameUpdateCount % 10 == 0)
            {
                bool vivo = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p != null && p.active &&
                        p.type == ModContent.ProjectileType<MetronomoPulsarHalo>() &&
                        p.owner == player.whoAmI)
                    { vivo = true; break; }
                }
                if (!vivo)
                {
                    int halo = Projectile.NewProjectile(player.GetSource_FromThis(),
                        player.Center - Vector2.UnitY * 66f, Vector2.Zero,
                        ModContent.ProjectileType<MetronomoPulsarHalo>(),
                        Item.damage, 0f, player.whoAmI);
                    if (halo >= 0)
                        Main.projectile[halo].timeLeft = 60;
                }
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // === LA LECTURA DEL COMPÁS (el reloj del juego, jamás el del render). ===
            int dist = MetronomoPulsarHalo.DistAlTic;
            bool perfecta = dist <= MetronomoPulsarHalo.VentanaPerfecta;
            bool buena = dist <= MetronomoPulsarHalo.VentanaBuena;

            Vector2 dir = velocity.SafeNormalize(Vector2.UnitX);

            int idx = Projectile.NewProjectile(source, position, velocity, type, damage, knockback,
                player.whoAmI);
            if (idx >= 0 && (perfecta || buena))
                Main.projectile[idx].CritChance = 100;   // el crítico garantizado

            // EL ABANICO DOBLE del PERFECT: dos pulsos hermanos al 60%.
            if (perfecta)
            {
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    Projectile.NewProjectile(source, position,
                        dir.RotatedBy(lado * 0.18f) * velocity.Length(), type,
                        (int)(damage * 0.6f), knockback, player.whoAmI);
                }
            }

            // El combo se entera (el faro espera a sus 8 aciertos).
            MetronomoPulsarHalo.NotificarAcierto(player, perfecta || buena, perfecta);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// VelaSolar — v6.42 — APUESTA 2: LA VELA SOLAR.
    ///
    /// El arma de CADENCIA CRECIENTE: canaliza (mantén pulsado) y la vela
    /// de fotones se despliega sobre tu espalda capturando el viento de
    /// la luz — el calor sube con la curva exponencial de un capacitor
    /// (τ = 2,2 s) y la cadencia con él: de 12 a 30 disparos/s, daño
    /// ×1 → ×1,6.
    ///
    ///   · Al calor pleno: LA FUSIÓN — 1,5 s de plasma azul-blanco (daño
    ///     ×2, velocidad ×1,4) y luego la vela se FUNDE: 3 s de cañón
    ///     muerto humeante antes de poder recargar.
    ///   · Soltar a tiempo conserva ~70% del calor (la ley de Newton).
    ///
    /// ARMA DE PRUEBAS — sin coste de maná.
    /// </summary>
    public class VelaSolar : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 40;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30; Item.height = 30;
            Item.useTime = 6; Item.useAnimation = 6;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;                 // el arma de canalización
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;                       // SIN MANÁ (la regla de la casa)
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item13;
            Item.shoot = ModContent.ProjectileType<VelaSolarProjectile>();
            Item.shootSpeed = 11f;
        }

        public override void HoldItem(Player player)
        {
            // LA VELA VIVA: se despliega al empuñar el arma (compañera del
            // portador — la invoca su propio jugador).
            if (Main.myPlayer == player.whoAmI && Main.GameUpdateCount % 10 == 0)
            {
                bool viva = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p != null && p.active &&
                        p.type == ModContent.ProjectileType<VelaSolarProjectile>() &&
                        p.owner == player.whoAmI)
                    { viva = true; break; }
                }
                if (!viva)
                    Projectile.NewProjectile(player.GetSource_FromThis(),
                        player.Center - Vector2.UnitY * 58f, Vector2.Zero,
                        ModContent.ProjectileType<VelaSolarProjectile>(),
                        Item.damage, 0f, player.whoAmI);
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // La vela se encarga de TODO (el disparo lo hace ella por su
            // cadencia propia — la del calor, no la del arma).
            HoldItem(player);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// GuadanaDesgarro — v6.42 — APUESTA 3: LA GUADAÑA DEL DESGARRO.
    ///
    /// EL MELEE DEL ARSENAL CÓSMICO: el tajo de la guadaña vuela 220 px
    /// como un creciente de luz y DONDE muere abre una FISURA VERTICAL
    /// de realidad (2 s): todo lo que la cruza sangra 0,3× por tick…
    /// y los PROYECTILES ENEMIGOS que la tocan DESAPARECEN — la fisura
    /// se los traga y su daño se suma AL PRÓXIMO TAJO (EL HAMBRE, con
    /// tope de 2× el daño del arma).
    ///
    ///   · Clic izq: tajo + fisura donde caiga.
    ///   · Clic der: LA FISURA A DISTANCIA — se abre directamente donde
    ///     apunte tu mira (hasta 480 px), lista para tragarse una andanada.
    ///
    /// ARMA DE PRUEBAS — sin coste de maná.
    /// </summary>
    public class GuadanaDesgarro : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 85;
            Item.DamageType = DamageClass.Melee;
            Item.width = 30; Item.height = 30;
            Item.useTime = 28; Item.useAnimation = 28;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.noMelee = false;                // la hoja también muerde de cerca
            Item.mana = 0;                       // SIN MANÁ (la regla de la casa)
            Item.knockBack = 5f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item71;
            Item.shoot = ModContent.ProjectileType<TajoGuadanaProjectile>();
            Item.shootSpeed = 12f;
        }

        public override bool AltFunctionUse(Player player) => true;

        // v6.50.3 — FIX (doble aplicación del Hambre): el hook de
        // ModifyWeaponDamage YA alimenta el parámetro `damage` de Shoot a
        // través del pipeline de tML (GetWeaponDamage → ItemLoader.Shoot —
        // el mismo flujo en el que confía GrimoireEternal). Con el bono AQUÍ
        // y la suma manual en Shoot, el tajo y la fisura salían con
        // base+2×hambre (y el tope de la fisura, Projectile.damage×2,
        // quedaba inflado). El Hambre pertenece SOLO AL TAJO (el diseño:
        // "se suma al tajo y se gasta") — la hoja melee NO lo lleva. La
        // suma única vive en Shoot; este hook se retira.

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            ApuestasPlayer ap = player.GetModPlayer<ApuestasPlayer>();
            int hambre = ap.HambreGuadana;
            ap.HambreGuadana = 0;   // el tajo SE COME el hambre acumulado

            if (player.altFunctionUse == 2)
            {
                // === LA FISURA A DISTANCIA: donde apunta la mira (≤480 px). ===
                Vector2 mira = new Vector2(Main.mouseX + Main.screenPosition.X,
                    Main.mouseY + Main.screenPosition.Y);
                Vector2 delta = mira - player.Center;
                if (delta.Length() > 480f)
                    mira = player.Center + Vector2.Normalize(delta) * 480f;

                int dmg = damage + hambre;
                Projectile.NewProjectile(source, mira, Vector2.Zero,
                    ModContent.ProjectileType<FisuraDesgarroProjectile>(),
                    dmg, 0f, player.whoAmI);
            }
            else
            {
                // === EL Tajo normal: el creciente vuela 220 px. ===
                int dmg = damage + hambre;
                Projectile.NewProjectile(source, position, velocity, type, dmg,
                    knockback, player.whoAmI);
            }
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// EgidaNova — v6.42 — APUESTA 4: LA ÉGIDA DE NOVA.
    ///
    /// EL ESCUDO-OFENSIVO: el parry de riesgo del arsenal.
    ///
    ///   · Clic izq: EL EMBESTÓN — un golpe de escudo corto y duro (160
    ///     px) con el empujón 9 del bulldozer.
    ///   · Clic der: ALZAR LA GUARDIA (enfriamiento 8 s, visible como el
    ///     anillo que se cierra a tu alrededor): 0,3 s de círculo rúnico.
    ///     Un golpe esquivable en los primeros 8 ticks NO EXISTE: en su
    ///     lugar detona LA NOVA DEL PARRY (3× en 240 px + empujón +
    ///     quemadura + 1 s de invulnerabilidad). La guardia que expira
    ///     sin parar nada te ATURDE 0,5 s — el precio del parry fallido.
    ///
    /// ARMA DE PRUEBAS — sin coste de maná.
    /// </summary>
    public class EgidaNova : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 50;
            Item.DamageType = DamageClass.Melee;
            Item.width = 30; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;                       // SIN MANÁ (la regla de la casa)
            Item.knockBack = 9f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<EmbestidaNovaProjectile>();
            Item.shootSpeed = 8f;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            ApuestasPlayer ap = player.GetModPlayer<ApuestasPlayer>();
            // v6.50.2 — FIX (égida 2c, coherencia con el desync del stun): este
            // gate queda COHERENTE con la nueva vida del testigo: tras un
            // parry EXITOSO FreeDodge marca (ai[0]=1 + netUpdate) y tumba al
            // GuardiaNovaProjectile → el testigo nunca aturde → Aturdido
            // queda a 0 en TODAS las copias y este gate NO bloquea en falso.
            // El Aturdido>0 (bloqueo de ambos clics) y el EnfriamientoGuardia
            // (que el testigo ahora también pone al fallar) solo cortan tras
            // un fallo REAL de la guardia.
            if (player.altFunctionUse == 2)
                return ap.EnfriamientoGuardia <= 0 && ap.Aturdido <= 0;
            return ap.Aturdido <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                // === ALZAR LA GUARDIA: el círculo rúnico + el anillo de
                //     enfriamiento que lo anuncia. ===
                ApuestasPlayer ap = player.GetModPlayer<ApuestasPlayer>();
                ap.GuardiaTicks = GuardiaNovaProjectile.GuardiaTotal;
                ap.ParryHecho = false;

                Projectile.NewProjectile(source, player.Center, Vector2.Zero,
                    ModContent.ProjectileType<GuardiaNovaProjectile>(),
                    damage, 0f, player.whoAmI);
                Projectile.NewProjectile(source, player.Center, Vector2.Zero,
                    ModContent.ProjectileType<AnilloEnfriamientoProjectile>(),
                    damage, 0f, player.whoAmI);

                // v6.50.2 — FIX (host mudo al alzar la guardia): Shoot corre
                // solo en el cliente dueño — el viejo `!= Server` callaba al
                // HOST (netMode 1 CON pantalla). "Con pantalla" = !dedServ.
                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f, Pitch = 0.3f },
                        player.Center);
            }
            else
            {
                // === EL EMBESTÓN: la onda corta hacia donde miras. ===
                Vector2 origen = player.Center + Vector2.UnitX * 44f * player.direction;
                Projectile.NewProjectile(source, origen, Vector2.Zero, type,
                    damage, knockback, player.whoAmI, player.direction);
            }
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// EcoCuantico — v6.42 — APUESTA 5: EL ECO CUÁNTICO.
    ///
    /// El arma que SE RECUERDA: cada pulso vuela 180 px y decohiere en
    /// una semilla fantasmal que se re-emite como ECO contra el enemigo
    /// más cercano (60% del daño — la física de la decoherencia); el eco
    /// deja su propio eco (36%)… y el de este otro (21,6%): 2,176× de
    /// daño total si toda la cadena encuentra a quién morder.
    ///
    ///   · Clic der: LA RESONANCIA — todas las semillas vivas disparan
    ///     AL INSTANTE y a la vez (el coro cuántico). 5 s de recarga.
    ///
    /// ARMA DE PRUEBAS — sin coste de maná.
    /// </summary>
    public class EcoCuantico : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 70;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30; Item.height = 30;
            Item.useTime = 24; Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;                       // SIN MANÁ (la regla de la casa)
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item9;
            Item.shoot = ModContent.ProjectileType<PulsoEcoProjectile>();
            Item.shootSpeed = 12f;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
                return player.GetModPlayer<ApuestasPlayer>().ResonanciaCooldown <= 0;
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                // === LA RESONANCIA: el coro dispara a la vez. ===
                ApuestasPlayer ap = player.GetModPlayer<ApuestasPlayer>();
                ap.ResonanciaCooldown = 300;

                int coro = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p != null && p.active &&
                        p.type == ModContent.ProjectileType<PulsoEcoProjectile>() &&
                        p.owner == player.whoAmI && p.ai[1] > 0f)
                    {
                        p.ai[1] = -1f;
                        p.netUpdate = true;
                        coro++;
                    }
                }

                // v6.50.2 — FIX (host sordo a su resonancia): mismo error raíz
                // que el sonido de la guardia — !Main.dedServ.
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.5f, Pitch = 0.5f },
                        player.Center);
                    PulsoLib.EmpujarPantalla(new Color(150, 210, 255), 0.25f, 10);
                    if (coro > 0)
                        // v6.50.3 — strings→hjson (hallazgo V-3)
                        Main.NewText(Language.GetTextValue("Mods.AethonMod.Armas.ResonanciaEcos", coro),
                            new Color(170, 220, 255));
                }
            }
            else
            {
                Projectile.NewProjectile(source, position, velocity, type, damage,
                    knockback, player.whoAmI);
            }
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// VerboPrimordialStaff — v6.42 — EL VERBO PRIMORDIAL.
    ///
    /// LA PALABRA QUE PRONUNCIÓ LA PRIMERA LUZ — el arma que habla con
    /// TODAS las bibliotecas de la casa, una por movimiento, como una
    /// sinfonía en siete tiempos (7 segundos exactos):
    ///
    ///   I · EL PULSO      (onda + empujón de pantalla)
    ///   II · EL SELLO     (círculos rúnicos contrarrotando)
    ///   III · LA CORONA   (anillo de energía + fotones)
    ///   IV · LA TORMENTA  (arcos voltaicos + cadenas que pican)
    ///   V · EL FUEGO      (llama y lengua solares)
    ///   VI · EL DESGARRO  (la herida EN el mundo + lente + tajos)
    ///   VII · LA SINFONÍA (todo a la vez + el telegraph del final)
    ///
    /// Y al último tick: LA DETONACIÓN — 2,5× en 240 px con quemadura,
    /// onda cromática doble, flor de fuego, cadenas fractales a los
    /// seis más cercanos y el estruendo de la casa. El daño crece con
    /// cada movimiento (×1,0 → ×1,4): la palabra gana volumen.
    ///
    /// ARMA DE PRUEBAS — sin coste de maná.
    /// </summary>
    public class VerboPrimordialStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 120;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30; Item.height = 30;
            Item.useTime = 45; Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;                       // SIN MANÁ (la regla de la casa)
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<VerboPrimordialProjectile>();
            Item.shootSpeed = 6f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // La palabra nace frente al portador, rumbo a la mira.
            Vector2 spawn = position + velocity.SafeNormalize(Vector2.Zero) * 32f;
            Projectile.NewProjectile(source, spawn, velocity, type, damage, knockback,
                player.whoAmI);
            AudioLib.Sonar(AudioLib.Familia.Cosmica, "apertura", spawn, 1f, 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
