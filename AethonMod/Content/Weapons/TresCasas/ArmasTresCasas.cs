using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.TresCasas;

namespace AethonMod.Content.Weapons.TresCasas
{
    // ======================================================================
    //  LOS FAVORITOS DE LAS TRES CASAS (v6.100) — LA PURGA DE LA FORJA.
    //
    //  LA LETRA DEL USUARIO (v6.100): las armas creadas con la
    //  herramienta — las de la bolsa del Taller de Efectos y las de la
    //  bolsa de las Tres Casas — SE BORRAN. Las veinte armas de la forja
    //  v6.97 y las tres del taller v6.92 murieron; SOLO quedan los dos
    //  favoritos del usuario (los del canon de SPRITES-FAVORITOS):
    //
    //   · LA PLUMA DEL ESCRIBA SANTO — el trazo blanco-dorado que vuela
    //     a la marca y VUELVE cortando dos veces.
    //   · EL CORO DE LOS PROFUNDOS — las semillas de tentáculo violeta
    //     que caen y CANTAN persiguiendo a los vivos.
    //
    //  LA MAQUINARIA (intacta): cada arma es UNA línea de datos (daño ·
    //  uso · estilo del motor · variante · SUS dos tintas) — TODO el VFX
    //  y el comportamiento vive en EL FUEGO (TresCasasProjectile, ahora
    //  vestido por capas + el motor de píxeles de la v6.99). Regla de la
    //  casa: SIN COSTE DE MANÁ (v6.35).
    // ======================================================================
    public abstract class ArmaTresCasas : ModItem
    {
        /// <summary>EL PLATO del motor (1..8).</summary>
        protected abstract int Estilo { get; }
        /// <summary>LA VARIANTE de la casa (mueve los números del plato).</summary>
        protected virtual int Variante => 0;
        /// <summary>LA TINTA primaria (el cuerpo).</summary>
        protected abstract Color Tinta { get; }
        /// <summary>LA TINTA secundaria (el halo).</summary>
        protected abstract Color Tinta2 { get; }
        /// <summary>EL DAÑO.</summary>
        protected abstract int Dano { get; }
        /// <summary>EL USO (ticks).</summary>
        protected abstract int Uso { get; }
        /// <summary>¿Nace en el JUGADOR (haz/corona/estela) o en el CURSOR?</summary>
        protected virtual bool NaceEnElPortador => false;
        /// <summary>LA RESEÑA (la línea de color del tooltip).</summary>
        protected abstract string Resena { get; }

        public override void SetDefaults()
        {
            Item.damage = Dano;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30; Item.height = 30;
            Item.useTime = Uso; Item.useAnimation = Uso;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TresCasasProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 0; Item.noMelee = true;   // SIN MANÁ (la regla v6.35)
            Item.rare = ItemRarityID.Quest;
            Item.value = 30000;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.30f);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA POSICIÓN NACE: el ancla del plato (el portador o el cursor).
            Vector2 ancla = NaceEnElPortador ? player.MountedCenter : Main.MouseWorld;
            int c1 = (Tinta.R << 16) | (Tinta.G << 8) | Tinta.B;
            int c2 = (Tinta2.R << 16) | (Tinta2.G << 8) | Tinta2.B;
            // EL TOPE DE CASA (una copia del plato a la vez: la corona y la
            // zona no se apilan — el festín es de a una).
            int estiloPuro = Estilo;
            if (estiloPuro == TresCasasProjectile.ESTILO_CORONA ||
                estiloPuro == TresCasasProjectile.ESTILO_ZONA ||
                estiloPuro == TresCasasProjectile.ESTILO_VORTICE)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || p.type != type || p.owner != player.whoAmI)
                        continue;
                    if (p.ai.Length > 0 && (int)(p.ai[0] / 100f) == estiloPuro)
                        p.Kill();
                }
            }
            int idx = Projectile.NewProjectile(source, ancla, Vector2.Zero,
                type, damage, knockback, player.whoAmI,
                Estilo * 100 + Variante, c1, c2);
            if (idx >= 0) Main.projectile[idx].netUpdate = true;
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string hex = $"{Tinta.R:X2}{Tinta.G:X2}{Tinta.B:X2}";
            string hex2 = $"{Tinta2.R:X2}{Tinta2.G:X2}{Tinta2.B:X2}";
            tooltips.Add(new TooltipLine(Mod, "TresCasasResena",
                $"[c/{hex}:{Resena}]"));
            tooltips.Add(new TooltipLine(Mod, "TresCasasCasa",
                $"[c/{hex2}:(Los favoritos de la forja · SIN COSTE DE MANÁ)]"));
        }
    }

    // ====================================================================
    //  LA CASA SAGRADA — el favorito del canon
    // ====================================================================

    /// <summary>LA PLUMA DEL ESCRIBA SANTO: el trazo de luz que va y vuelve.</summary>
    public class PlumaDelEscribaSanto : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_ESTELA;
        protected override Color Tinta => new(250, 248, 235);
        protected override Color Tinta2 => new(255, 216, 120);
        protected override int Dano => 105;
        protected override int Uso => 28;
        protected override bool NaceEnElPortador => true;
        protected override string Resena => "LA PLUMA: un trazo de luz blanca que vuela a la marca y VUELVE al portador, cortando dos veces";
    }

    // ====================================================================
    //  LA CASA DEL HORROR CÓSMICO — el favorito del canon
    // ====================================================================

    /// <summary>EL CORO DE LOS PROFUNDOS: las semillas que cantan cazando.</summary>
    public class CoroDeLosProfundos : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_ENJAMBRE;
        protected override int Variante => 1;
        protected override Color Tinta => new(160, 80, 235);
        protected override Color Tinta2 => new(70, 40, 110);
        protected override int Dano => 68;
        protected override int Uso => 26;
        protected override string Resena => "EL CORO: semillas de tentáculo violeta caen y CANTAN persiguiendo a los vivos — ninguna escapa dos veces";
    }
}
