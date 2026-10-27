using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmetic;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// ALASAURORAPLAYER — v6.52.1 — LA FÍSICA DE LAS ALAS DE CÓDIGO.
    ///
    /// LA MITAD JUGABLE de la técnica aprendida (el sistema de alas de
    /// interferencia de la investigación): las alas de código NO
    /// son un ítem de slot de alas — son un ACCESORIO que INYECTA la
    /// física de alas de vanilla SIN el sprite:
    ///
    ///   · Player.wingsLogic = 27 (las alas de Mothron — la casa ya
    ///     había verificado este slot en CosmeticPlayer v6.50.48:
    ///     «velocidad de dios, sin las penalidades de hover» de las
    ///     22/28/30/31/37/45) — el MOVIMIENTO de unas alas de verdad.
    ///   · Player.wingTimeMax = 240 (el rango de la reliquia del
    ///     original: 210 día / 270 noche — aquí plano, generoso).
    ///   · Player.wings queda 0: el DIBUJADO de alas de vanilla lee
    ///     Player.wings, NO wingsLogic (verificado en el decompile —
    ///     el comentario de CosmeticPlayer) → CERO sprite vanilla, el
    ///     visual ES el código.
    ///   · noFallDmg + el amuleto de la Emperatriz prestado (la
    ///     aceleración aérea ×1.75 de vanilla).
    ///
    /// RESPETO A LAS ALAS REALES: si el portador viste alas de verdad
    /// (wings ≠ 0), la INYECCIÓN no ocurre (sus alas mandan) pero el
    /// TIEMPO DE VUELO sí se eleva al mínimo de la aurora (Math.Max —
    /// la reliquia del original hace exactamente eso) y el VISUAL se
    /// pliega (lo maneja el halo).
    /// </summary>
    public class AlasAuroraPlayer : ModPlayer
    {
        /// <summary>¿Lleva LA AURORA DE TINTA en el accesorio? (visual + física — la enciende el ítem cada frame).</summary>
        public bool Equipada;

        /// <summary>¿La lleva en la VANIDAD? (solo visual — la física NO: la vanidad adorna).</summary>
        public bool Vanidad;

        /// <summary>¿El portador ocultó el visual? (la vanidad del accesorio manda).</summary>
        public bool OcultarVisual;

        /// <summary>v6.54.0 — QUÉ ALA lleva (el índice del catálogo:
        /// 1 = la Aurora de Tinta histórica, 2..16 = las quince nuevas
        /// de la investigación — lo marcan los ítems cada frame).</summary>
        public byte AlaEstilo;

        /// <summary>¿El visual vive? (accesorio o vanidad — lo lee el halo).</summary>
        public bool VisualViva => Equipada || Vanidad;

        /// <summary>El tiempo de vuelo de la aurora (frames — el rango de la reliquia).</summary>
        public const int TiempoVuelo = 240;

        /// <summary>El slot de física prestado (Mothron — verificado por la casa).</summary>
        private const int SlotFisica = 27;

        public override void ResetEffects()
        {
            Equipada = false;
            Vanidad = false;
            OcultarVisual = false;
            AlaEstilo = 0;      // v6.54.0 — cada ítem la vuelve a marcar
        }

        public override void PostUpdateEquips()
        {
            // EL HALO (el portador de las alas visuales): vive con el
            // accesorio O la vanidad; lo invoca SOLO el dueño local
            // (tML lo sincroniza — el patrón de la casa de los halos).
            if (VisualViva && Player.whoAmI == Main.myPlayer && !EspiarHalo())
            {
                Projectile.NewProjectile(Player.GetSource_Misc("AuroraDeTinta"),
                    Player.Center, Microsoft.Xna.Framework.Vector2.Zero,
                    ModContent.ProjectileType<AlasAuroraHalo>(),
                    0, 0f, Player.whoAmI);
            }

            if (!Equipada) return;   // la VANIDAD es solo el visual
            try
            {
                // LA TINTA NO CASTIGA LAS CAÍDAS.
                Player.noFallDmg = true;

                // LA INSIGNIA DEL ALBA PRESTADA (la aceleración aérea de
                // la Emperatriz — el mismo préstamo de la Forma Divina).
                Player.empressBrooch = true;

                // LA FÍSICA INYECTADA: SOLO si no hay alas reales ni
                // montura (el contrato de CosmeticPlayer v6.50.48 — las
                // alas de verdad mandan sobre la inyección).
                if (Player.wingsLogic <= 0 && !Player.mount.Active)
                {
                    Player.wingsLogic = SlotFisica;
                    Player.wingTimeMax = TiempoVuelo;
                }

                // EL PISO DE VUELO: con alas reales puestas, la aurora
                // ELEVA su tiempo al mínimo (Math.Max — la conducta de
                // la reliquia del original).
                if (Player.wingTimeMax > 0 && Player.wingTimeMax < TiempoVuelo)
                    Player.wingTimeMax = TiempoVuelo;
            }
            catch { }
        }

        /// <summary>¿Ya vive mi halo de alas? (el patrón Espiar de la casa).</summary>
        private bool EspiarHalo()
        {
            int tipo = ModContent.ProjectileType<AlasAuroraHalo>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Player.whoAmI && p.type == tipo)
                    return true;
            }
            return false;
        }
    }
}
