using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.Projectiles.Grimorio;

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    //  EL TRÍO DEL ETERNO — v6.159 — EL ARMA DE LAS TRES ARMAS.
    //
    //  LA LETRA: «debes crear una nueva arma, esta arma nueva no
    //  dispara nada, pero tendra una nueva mecanica, en la que se le
    //  pondran hasta 3 armas de terraria y entonces el arma nueva
    //  usara sus proyectiles para atacar, entendiste».
    //
    //  LA MECÁNICA: EL ATRIL DEL FUSIONADO — un atrio de tinta que
    //  SOSTIENE hasta TRES armas MÁGICAS consagradas (v6.162: solo
    //  mágicas; v6.161: solo se fusionan en el altar del libro). EL
    //  TRÍO NO DISPARA NADA PROPIO: al atacar, lo que sale es EL
    //  DISPARO DE CADA ARMA INSERTADA, con SU daño y SU proyectil.
    //
    //  ──────────────────────────────────────────────────────────────
    //  v6.174 — EL CAÑÓN DE VANILLA (la letra: «las armas del trío
    //  siguen sin disparar: todas deberían disparar sin importar
    //  compatibilidad»).
    //
    //  LA AUTOPSIA (el client.log del dueño): «tenían seguimiento pero
    //  no disparaban» + una excepción silenciosa en
    //  `Terraria.Projectile.AI_001()`. El trío de la v6.173 llamaba
    //  a `CombinedHooks.Shoot` (el hook de mods) y, si el arma no
    //  tenía ModItem, paría el proyectil con `Projectile.NewProjectile`
    //  PELADO + dos inventos suyos en `ai[0]` (ángulo × 1000) y
    //  `ai[1]` (velocidad). Dos pecados:
    //   · la IA 1 de vanilla (los pernos mágicos teledirigidos) lee
    //     `ai[0]` como el ÍNDICE de su presa → Main.npc[6283] → crash;
    //   · vanilla dispara CADA arma con `Player.ItemCheck_Shoot`: 2 300
    //     líneas de casos especiales (el Prisma Último, el Cañón
    //     Cargado, la Lluvia de Nimbos, el Arcoíris Lunar… nacen con
    //     SU posición, SU `ai` y SU lógica) — un NewProjectile pelado
    //     nunca los reproduce.
    //
    //  LA CURA — EL CAÑÓN: el trío llama a LA MISMA FUNCIÓN de vanilla
    //  (`Player.ItemCheck_Shoot`) con el arma de la ranura como
    //  parámetro. Se verificó en el decompile real: esa función trabaja
    //  SOLO con el `Item` que recibe (`sItem.shoot`, `sItem.useAmmo`,
    //  `CombinedHooks.CanShoot/ModifyShootStats/Shoot` con ese mismo
    //  ítem) y apunta al cursor del jugador local — exactamente lo que
    //  el trío necesita. Lo único que toca del trío es su tiempo de
    //  uso (`ApplyItemTime`): se protege y se restaura alrededor de la
    //  llamada. Si el cañón no existe (otra versión de tML), el trío
    //  cae a la cadena de siempre — SIN inventar `ai`.
    //  Y NADA DE `ai`: la inteligencia opcional (EL ALBEDRÍO del altar:
    //  Original · Presa · Cursor) vive en TrioGuiado, que solo dobla la
    //  velocidad del proyectil después de SU IA.
    //
    //  LOS MODOS (la casilla «3» del altar): LAS TRES A LA VEZ · o POR
    //  TURNOS (una ranura armada en cada uso).
    //  LA BRASA: si un arma no logra parir ningún proyectil, el trío
    //  dispara por ella una brasa de tinta con SU daño — ningún clic
    //  en vacío.
    //  ──────────────────────────────────────────────────────────────
    public class TrioDelEterno : ModItem
    {
        /// <summary>Dispara POR TURNOS: una ranura armada en cada uso.</summary>
        public const int Turnos = 0;
        /// <summary>Dispara LAS TRES ranuras armadas en cada uso.</summary>
        public const int Todas = 3;

        /// <summary>LAS TRES RANURAS — las armas insertadas (air = ranura
        /// libre). Viajan con el ítem.</summary>
        public Item[] Ranuras = new Item[3];

        /// <summary>EL ALBEDRÍO de cada ranura: 0 = Original (el proyectil
        /// tal cual lo hizo su arma) · 1 = Presa (persigue al enemigo) ·
        /// 2 = Cursor (vuela al puntero del dueño y orbita). Se elige en
        /// el altar del libro.</summary>
        public int[] Inteligencia = new int[3];

        /// <summary>El modo de fuego: `Todas` o `Turnos` (la casilla «3»
        /// del altar).</summary>
        public int ModoDisparo = Todas;

        /// <summary>La última ranura que disparó en el modo por turnos
        /// (−1 al nacer: el primer uso arranca en la primera armada).</summary>
        public int Turno = -1;

        /// <summary>El conteo del aviso «el atril está vacío» (no spamea).</summary>
        private int _usosSinArmas;

        private static readonly Color ChatVioleta = new(196, 110, 228);
        private static readonly Color ChatOro = new(214, 152, 70);

        // ── EL CAÑÓN: `Player.ItemCheck_Shoot(int i, Item sItem, int
        //    weaponDamage)` — el disparo de vanilla, palabra por
        //    palabra. Es privado: se busca UNA vez por reflexión. ──
        private static readonly MethodInfo Canon = typeof(Player).GetMethod("ItemCheck_Shoot",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static readonly PropertyInfo UsosDeLaAnimacion =
            typeof(Player).GetProperty("ItemUsesThisAnimation");

        public override string Texture => "AethonMod/Content/Weapons/Investigaciones/CetroDagasEterno";

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.damage = 0;                 // el daño es EL DE CADA ARMA
            Item.DamageType = DamageClass.Generic;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;             // el Trío no golpea por sí mismo
            Item.noUseGraphic = true;        // la vaina no se dibuja girando
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.LightPurple;
            // EL GATILLO (v6.162): con shoot = None el hook Shoot JAMÁS
            // corre (el contrato de Terraria: Shoot solo vive con shoot >
            // 0). Este proyectil invisible de 1 tick es SOLO la puerta.
            Item.shoot = ModContent.ProjectileType
                <global::AethonMod.Content.Projectiles.Grimorio.PortalTrioProjectile>();
            Item.shootSpeed = 12f;
            Item.autoReuse = true;
            Ranuras = new Item[3];
            Inteligencia = new int[3];
            for (int i = 0; i < 3; i++)
                Ranuras[i] = new Item();
            ModoDisparo = Todas;
            Turno = -1;
        }

        // ================================================================
        //  EL CLON DE VERDAD (v6.174): las dos tablas del trío son
        //  REFERENCIAS. El Clone por defecto de tML copia el ModItem
        //  miembro a miembro — dos ítems del trío terminaban apuntando a
        //  LA MISMA tabla Ranuras (mover, apilar, guardar: el arma de la
        //  ranura aparecía en dos sitios). AHORA cada copia nace con SUS
        //  tablas y SUS armas clonadas.
        // ================================================================
        public override ModItem Clone(Item newEntity)
        {
            var copia = (TrioDelEterno)base.Clone(newEntity);
            copia.Ranuras = new Item[3];
            copia.Inteligencia = new int[3];
            for (int i = 0; i < 3; i++)
            {
                copia.Ranuras[i] = (Ranuras != null && i < Ranuras.Length && Ranuras[i] != null &&
                    !Ranuras[i].IsAir) ? (Item)Ranuras[i].Clone() : new Item();
                copia.Inteligencia[i] = (Inteligencia != null && i < Inteligencia.Length)
                    ? Math.Clamp(Inteligencia[i], 0, 2) : 0;
            }
            copia.ModoDisparo = ModoDisparo == Todas ? Todas : Turnos;
            copia.Turno = Turno;
            copia._usosSinArmas = 0;
            return copia;
        }

        // ================================================================
        //  LA LECTURA DEL ALTAR: el trío del inventario del portador
        //  (o null si no lleva ninguno).
        //
        //  v6.179 — EL TRÍO MÁS ARMADO (la letra: «los slot de armas del
        //  trio del eterno en el altar todavia se muestran vacios cuando
        //  entro, el item solo vuelve a aparecer si lo desequipo y lo
        //  vuelvo a equipar»): LA AUTOPSIA. La persistencia estaba VIVA
        //  (el banco headless v6.178 probó el ciclo disco→reinicio y las
        //  tres ranuras sobrevivían) — el fantasma era OTRO: si el
        //  portador lleva DOS tríos (la receta es de 5 maderas) y el
        //  ARMADO cambia de sitio en el inventario, la lectura de ANTES
        //  devolvía el PRIMERO que encontraba… que podía ser el VACÍO:
        //  el altar mostraba sus ranuras VACÍAS aunque el arma estaba
        //  (viva) en el otro. «Desequipar y volver a equipar» armaba el
        //  trío que la lectura encontraba — y por eso el arma «volvía».
        //  LA CURA: el altar lee SIEMPRE el trío MÁS ARMADO del
        //  inventario (a igualdad, el primero) — el arma ya no depende
        //  del sitio que ocupe.
        // ================================================================
        public static TrioDelEterno Del(Player p)
        {
            try
            {
                int tipo = ModContent.ItemType<TrioDelEterno>();
                if (p == null || !p.active) return null;
                TrioDelEterno mejor = null;
                int mejorArmadas = -1;
                for (int i = 0; i < 58; i++)
                {
                    Item it = p.inventory[i];
                    if (it == null || it.IsAir || it.type != tipo) continue;
                    if (it.ModItem is not TrioDelEterno trio) continue;
                    int armadas = 0;
                    if (trio.Ranuras != null)
                        for (int r = 0; r < trio.Ranuras.Length && r < 3; r++)
                            if (trio.Ranuras[r] != null && !trio.Ranuras[r].IsAir) armadas++;
                    if (armadas > mejorArmadas) { mejorArmadas = armadas; mejor = trio; }
                }
                return mejor;
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TrioDelEterno", exRegistro); }
            return null;
        }

        /// <summary>¿ES UN ARMA para el atril? v6.162 — SOLO ARMAS MÁGICAS:
        /// daño > 0, clase Mágica, y no el propio Trío.</summary>
        public static bool EsArmaValida(Item it)
        {
            int tipo = ModContent.ItemType<TrioDelEterno>();
            return it != null && !it.IsAir && it.damage > 0 && it.type != tipo &&
                it.DamageType == DamageClass.Magic;
        }

        /// <summary>¿PUEDE ENTRAR en una ranura? Arma mágica Y consagrada
        /// (el sello del altar). `razon` lleva la voz del rechazo.</summary>
        public static bool PuedeEntrar(Item it, out string razon)
        {
            razon = "";
            if (it == null || it.IsAir)
            {
                razon = Language.GetTextValue("Mods.AethonMod.Items.TrioNoArma");
                return false;
            }
            if (!EsArmaValida(it))
            {
                razon = Language.GetTextValue("Mods.AethonMod.Items.TrioNoArma");
                return false;
            }
            bool sello = false;
            try
            {
                sello = it.GetGlobalItem<global::AethonMod.Content.Globals.CorruptoGlobalItem>().Corrompido;
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TrioDelEterno", exRegistro); }
            if (!sello)
            {
                razon = Language.GetTextValue("Mods.AethonMod.Items.TrioRequiereConsagrada");
                return false;
            }
            return true;
        }

        /// <summary>¿La ranura `i` lleva un arma? (con o sin proyectil: la
        /// brasa responde por las mudas).</summary>
        private bool RanuraArmada(int i)
            => i >= 0 && i < 3 && Ranuras != null && Ranuras[i] != null && !Ranuras[i].IsAir &&
               Ranuras[i].damage > 0;

        /// <summary>La siguiente ranura armada después de `Turno` (el modo
        /// por turnos es un carrusel) o −1 si no hay ninguna.</summary>
        private int SiguienteConArma()
        {
            for (int paso = 0; paso < 3; paso++)
            {
                Turno = (Turno + 1) % 3;
                if (RanuraArmada(Turno)) return Turno;
            }
            return -1;
        }

        // ================================================================
        //  EL ATAQUE
        // ================================================================
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // El portal que abrió la puerta muere aquí: solo era la puerta.
            int portal = ModContent.ProjectileType
                <global::AethonMod.Content.Projectiles.Grimorio.PortalTrioProjectile>();
            if (type == portal)
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p != null && p.active && p.owner == player.whoAmI && p.type == portal) p.Kill();
                }

            bool alguna = RanuraArmada(0) || RanuraArmada(1) || RanuraArmada(2);
            if (!alguna)
            {
                // SIN ARMAS: la voz del libro lo dice (una de cada cuatro veces).
                if (Main.myPlayer == player.whoAmI && ++_usosSinArmas % 4 == 1)
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Items.TrioVacio"), ChatOro);
                return false;
            }
            _usosSinArmas = 0;

            // EL MODO: las tres a la vez, o la del turno
            int laDelTurno = ModoDisparo == Todas ? -1 : SiguienteConArma();
            for (int idx = 0; idx < 3; idx++)
            {
                if (laDelTurno >= 0 && idx != laDelTurno) continue;
                if (!RanuraArmada(idx)) continue;
                try { DispararLaRanura(player, idx); }
                catch
                {
                    // un arma rota NO tumba a las otras (la casa no traga
                    // errores en silencio: lo dice una vez por uso)
                    if (Main.myPlayer == player.whoAmI)
                        Main.NewText(Language.GetTextValue("Mods.AethonMod.Items.TrioNoDispara",
                            Ranuras[idx].Name), ChatOro);
                }
            }
            return false;   // nada de vanilla: el disparo fue nuestro
        }

        /// <summary>LA RANURA DISPARA: el maná de SU arma y EL CAÑÓN DE
        /// VANILLA con esa arma en la mano. Solo en la máquina del dueño
        /// (la ley de vanilla: los proyectiles nacen en el cliente del
        /// jugador que dispara).</summary>
        private void DispararLaRanura(Player player, int idx)
        {
            if (Main.myPlayer != player.whoAmI) return;
            Item arma = Ranuras[idx];
            if (arma == null || arma.IsAir) return;

            // EL MANÁ DEL ARMA: sin él, ESA arma no dispara (como en la mano)
            if (arma.mana > 0)
            {
                if (!player.CheckMana(arma, pay: false)) return;
                player.CheckMana(arma, pay: true);
                player.manaRegenDelay = (int)player.maxRegenDelay;   // ItemCheck_ApplyManaRegenDelay
            }

            int modo = (Inteligencia != null && idx < Inteligencia.Length)
                ? Math.Clamp(Inteligencia[idx], 0, 2) : 0;
            int dano = Math.Max(1, player.GetWeaponDamage(arma));
            float kb = player.GetWeaponKnockback(arma, arma.knockBack);
            int nacidos = 0;

            if (arma.shoot > 0)
            {
                int antes = ContarProyectiles(player.whoAmI);
                int anim = player.itemAnimation, animMax = player.itemAnimationMax;
                int tiempo = player.itemTime, tiempoMax = player.itemTimeMax;
                int usos = player.ItemUsesThisAnimation;
                TrioGuiado.Capturando = true;
                TrioGuiado.ModoCaptura = modo;
                TrioGuiado.Nacidos = 0;
                try
                {
                    bool porCanon = false;
                    try { porCanon = DispararPorElCanon(player, arma, dano, kb); }
                    catch { porCanon = false; }
                    if (!porCanon) DispararPorLaCadena(player, arma, dano, kb);
                }
                finally
                {
                    // EL TIEMPO DEL TRÍO SE RESTAURA: el cañón aplica el
                    // tiempo de USO DEL ARMA de la ranura (ApplyItemTime) —
                    // el trío conserva SU cadencia.
                    player.itemAnimation = anim;
                    player.itemAnimationMax = animMax;
                    player.itemTime = tiempo;
                    player.itemTimeMax = tiempoMax;
                    try { UsosDeLaAnimacion?.SetValue(player, usos); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TrioDelEterno", exRegistro); }
                    TrioGuiado.Capturando = false;
                }
                nacidos = Math.Max(TrioGuiado.Nacidos, ContarProyectiles(player.whoAmI) - antes);
            }

            if (nacidos == 0)
            {
                // LA BRASA: el arma no creó nada — el trío responde por ella
                Vector2 mira = Mira(player);
                var src = new Terraria.DataStructures.EntitySource_ItemUse_WithAmmo(player, arma, 0);
                Projectile.NewProjectile(src, player.MountedCenter + mira * 24f, mira * 14f,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Grimorio.TrioRescoldoProjectile>(),
                    dano, kb, player.whoAmI);
            }
        }

        /// <summary>EL CAÑÓN DE VANILLA: `Player.ItemCheck_Shoot` con el
        /// arma de la ranura. Devuelve false si el cañón no existe en
        /// esta versión de tML (el llamador cae a la cadena).</summary>
        private static bool DispararPorElCanon(Player player, Item arma, int dano, float kb)
        {
            MethodInfo m = Canon;
            if (m == null) return false;
            ParameterInfo[] p = m.GetParameters();
            object[] args;
            if (p.Length == 4) args = new object[] { player.whoAmI, arma, dano, kb };
            else if (p.Length == 3) args = new object[] { player.whoAmI, arma, dano };
            else return false;
            m.Invoke(player, args);
            return true;
        }

        /// <summary>LA CADENA (el respaldo sin cañón): el hook de mods con
        /// los números del arma y — si pide el disparo por defecto — el
        /// proyectil del arma nacido LIMPIO: sin ningún `ai` inventado
        /// (la lección de AI_001).</summary>
        private static void DispararPorLaCadena(Player player, Item arma, int dano, float kb)
        {
            Vector2 mira = Mira(player);
            float vel = MathHelper.Clamp(arma.shootSpeed, 1f, 40f);
            Vector2 pos = player.MountedCenter + mira * 24f;
            var src = new Terraria.DataStructures.EntitySource_ItemUse_WithAmmo(player, arma, 0);
            bool quiereDefault = true;
            try
            {
                quiereDefault = CombinedHooks.Shoot(player, arma, src, pos, mira * vel,
                    arma.shoot, dano, kb);
            }
            catch { quiereDefault = true; }
            if (quiereDefault)
                Projectile.NewProjectile(src, pos, mira * vel, arma.shoot, dano, kb, player.whoAmI);
        }

        private static Vector2 Mira(Player player)
        {
            Vector2 mira = Main.MouseWorld - player.MountedCenter;
            if (mira.HasNaNs() || mira.LengthSquared() < 1f) return new Vector2(player.direction, 0f);
            return Vector2.Normalize(mira);
        }

        private static int ContarProyectiles(int dueno)
        {
            int n = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == dueno) n++;
            }
            return n;
        }

        public override bool AltFunctionUse(Player player) => false;

        public override bool CanRightClick() => false;

        public override bool CanUseItem(Player player) => true;

        // === EL TOOLTIP: las tres ranuras, su albedrío y el modo ===

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "TrioComo",
                Language.GetTextValue("Mods.AethonMod.Items.TrioComo")));
            for (int i = 0; i < 3; i++)
            {
                Item arma = (Ranuras != null && i < Ranuras.Length) ? Ranuras[i] : null;
                bool viva = arma != null && !arma.IsAir;
                int modo = (viva && Inteligencia != null && i < Inteligencia.Length)
                    ? Math.Clamp(Inteligencia[i], 0, 2) : 0;
                string texto = viva
                    ? Language.GetTextValue("Mods.AethonMod.Items.TrioRanuraConAlbedrio", i + 1, arma.Name,
                        Language.GetTextValue("Mods.AethonMod.Items.TrioAlbedrio" + modo))
                    : Language.GetTextValue("Mods.AethonMod.Items.TrioRanura", i + 1,
                        Language.GetTextValue("Mods.AethonMod.Items.TrioLibre"));
                tooltips.Add(new TooltipLine(Mod, "TrioR" + i, texto)
                { OverrideColor = viva ? new Color(196, 110, 228) : new Color(168, 140, 104) });
            }
            tooltips.Add(new TooltipLine(Mod, "TrioModo",
                Language.GetTextValue(ModoDisparo == Todas
                    ? "Mods.AethonMod.Items.TrioModoTresTip"
                    : "Mods.AethonMod.Items.TrioModoTurnosTip"))
            { OverrideColor = new Color(200, 160, 255) });
        }

        // === LA PERSISTENCIA: las tres armas, el modo y el albedrío viajan con el ítem ===

        public override void SaveData(TagCompound tag)
        {
            for (int i = 0; i < 3; i++)
                if (Ranuras[i] != null && !Ranuras[i].IsAir)
                    tag["trio" + i] = ItemIO.Save(Ranuras[i]);
            tag["trioModo"] = ModoDisparo == Todas ? Todas : Turnos;
            for (int i = 0; i < 3; i++)
                if (Inteligencia != null && i < Inteligencia.Length && Inteligencia[i] != 0)
                    tag["trioAlb" + i] = Inteligencia[i];
        }

        public override void LoadData(TagCompound tag)
        {
            for (int i = 0; i < 3; i++)
            {
                Ranuras[i] = tag.ContainsKey("trio" + i)
                    ? ItemIO.Load(tag.Get<TagCompound>("trio" + i))
                    : new Item();
                if (Ranuras[i] == null) Ranuras[i] = new Item();
                Inteligencia[i] = tag.ContainsKey("trioAlb" + i)
                    ? Math.Clamp(tag.GetInt("trioAlb" + i), 0, 2) : 0;
            }
            // los ítems de la v6.162 guardaban 0..2 como «una sola ranura»:
            // ahora todo lo que no sea LAS TRES significa POR TURNOS
            ModoDisparo = !tag.ContainsKey("trioModo") || tag.GetInt("trioModo") == Todas
                ? Todas : Turnos;
            Turno = -1;
        }

        public override void NetSend(System.IO.BinaryWriter writer)
        {
            for (int i = 0; i < 3; i++)
            {
                writer.Write(Ranuras[i] != null && !Ranuras[i].IsAir);
                if (Ranuras[i] != null && !Ranuras[i].IsAir)
                    ItemIO.Send(Ranuras[i], writer, true);
            }
            writer.Write(ModoDisparo == Todas);          // el modo viaja por la red
            for (int i = 0; i < 3; i++)
                writer.Write((byte)((Inteligencia != null && i < Inteligencia.Length)
                    ? Math.Clamp(Inteligencia[i], 0, 2) : 0));
        }

        public override void NetReceive(System.IO.BinaryReader reader)
        {
            for (int i = 0; i < 3; i++)
            {
                bool viva = reader.ReadBoolean();
                Ranuras[i] = viva ? ItemIO.Receive(reader, true, false) : new Item();
                if (Ranuras[i] == null) Ranuras[i] = new Item();
            }
            ModoDisparo = reader.ReadBoolean() ? Todas : Turnos;
            for (int i = 0; i < 3; i++)
                Inteligencia[i] = Math.Clamp((int)reader.ReadByte(), 0, 2);
            Turno = -1;
        }

        public override void AddRecipes()
        {
            // LA VÍA DE LA CASA: receta de prueba (5 madera).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
