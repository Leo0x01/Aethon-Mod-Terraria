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
    //  LAS VEINTE ARMAS DE LAS TRES CASAS (v6.97) — LA ENTREGA DE LA
    //  FORJA.
    //
    //  LA LETRA DEL USUARIO: «usa tu nueva habilidad de la forja para
    //  crear un nuevo grupo de armas con diferentes efectos y vfx,
    //  intenta que sean vistosos y que sean tipo sagrado, horror
    //  cosmico y oscuridad, crea al menos 20 armas nuevas».
    //
    //  LAS TRES CASAS (el flavor de la forja):
    //   · LA CASA SAGRADA — el oro y la luz blanca: las armas del alba,
    //     los serafines y el juicio. (7 armas)
    //   · LA CASA DEL HORROR CÓSMICO — el teal y el violeta: las armas
    //     del abismo, la geometría imposible y las estrellas muertas.
    //     (7 armas)
    //   · LA CASA DE LA OSCURIDAD — el violeta y el negro tinta: las
    //     armas del verdugo, la ceniza y el eclipse. (6 armas)
    //
    //  LA MAQUINARIA: cada arma es UNA línea de datos (daño · uso ·
    //  estilo del motor · variante · SUS dos tintas) — TODO el VFX y el
    //  comportamiento vive en EL FUEGO (TresCasasProjectile: el haz ·
    //  la corona · la lluvia · la nova · el vórtice · el enjambre · la
    //  estela · la zona). Regla de la casa: SIN COSTE DE MANÁ (v6.35).
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
                $"[c/{hex2}:(Las Tres Casas — la entrega de LA FORJA · SIN COSTE DE MANÁ)]"));
        }
    }

    // ====================================================================
    //  LA CASA SAGRADA — el oro y la luz blanca (7 armas)
    // ====================================================================

    /// <summary>1 — EL CETRO DEL ALBA: el haz dorado que barre el frente.</summary>
    public class CetroDelAlba : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_HAZ;
        protected override Color Tinta => new(255, 208, 96);
        protected override Color Tinta2 => new(255, 248, 224);
        protected override int Dano => 120;
        protected override int Uso => 34;
        protected override bool NaceEnElPortador => true;
        protected override string Resena => "EL ALBA: un haz de luz dorada que barre el frente — tres capas de sol con el corazón blanco";
    }

    /// <summary>2 — LA CORONA DE LOS SERAFINES: los aros dorados orbitando.</summary>
    public class CoronaDeSerafines : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_CORONA;
        protected override int Variante => 0;
        protected override Color Tinta => new(255, 216, 120);
        protected override Color Tinta2 => new(255, 250, 230);
        protected override int Dano => 96;
        protected override int Uso => 30;
        protected override bool NaceEnElPortador => true;
        protected override string Resena => "LA CORONA: tres aros de luz orbitan al portador 8 s — todo el que se acerca, quemado";
    }

    /// <summary>3 — EL JUICIO DEL CIELO: la lluvia de espadas doradas.</summary>
    public class JuicioDelCielo : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_LLUVIA;
        protected override Color Tinta => new(255, 222, 128);
        protected override Color Tinta2 => new(255, 240, 180);
        protected override int Dano => 88;
        protected override int Uso => 26;
        protected override string Resena => "EL JUICIO: espadas de luz caen del cielo sobre la marca — clavan y estallan";
    }

    /// <summary>4 — LA CAMPANA DEL SANTUARIO: la nova de la onda santa.</summary>
    public class CampanaDelSantuario : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_NOVA;
        protected override Color Tinta => new(255, 214, 110);
        protected override Color Tinta2 => new(255, 255, 240);
        protected override int Dano => 130;
        protected override int Uso => 38;
        protected override string Resena => "LA CAMPANA: una onda de luz dorada carga y ESTALLA en anillo — empuja a los impuros";
    }

    /// <summary>5 — LA LÁMPARA DEL PEREGRINO: el enjambre de luciérnagas blancas.</summary>
    public class LamparaDelPeregrino : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_ENJAMBRE;
        protected override Color Tinta => new(255, 244, 200);
        protected override Color Tinta2 => new(220, 190, 120);
        protected override int Dano => 64;
        protected override int Uso => 24;
        protected override string Resena => "LA LÁMPARA: suelta luciérnagas de luz blanca que persiguen a los que acechan en la sombra";
    }

    /// <summary>6 — LA PLUMA DEL ESCRIBA SANTO: la estela que va y vuelve.</summary>
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

    /// <summary>7 — LA RELIQUIA DEL AMANECER: la zona del charco de luz.</summary>
    public class ReliquiaDelAmanecer : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_ZONA;
        protected override Color Tinta => new(255, 220, 130);
        protected override Color Tinta2 => new(255, 250, 220);
        protected override int Dano => 130;
        protected override int Uso => 60;
        protected override string Resena => "LA RELIQUIA: un charco de amanecer en la marca — 5 s de luz que muele a los que la pisan";
    }

    // ====================================================================
    //  LA CASA DEL HORROR CÓSMICO — el teal y el violeta (7 armas)
    // ====================================================================

    /// <summary>8 — LA PÚPILA DEL ABISMO: el haz del ojo que todo lo mira.</summary>
    public class PupilaDelAbismo : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_HAZ;
        protected override int Variante => 1;
        protected override Color Tinta => new(90, 220, 180);
        protected override Color Tinta2 => new(150, 67, 235);
        protected override int Dano => 140;
        protected override int Uso => 36;
        protected override bool NaceEnElPortador => true;
        protected override string Resena => "LA PÚPILA: el ojo del abismo abre su haz teal sobre la marca — la mirada que no parpadea";
    }

    /// <summary>9 — LA GEOMETRÍA IMPOSIBLE: los triángulos que no deberían existir.</summary>
    public class GeometriaImposible : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_CORONA;
        protected override int Variante => 1;
        protected override Color Tinta => new(96, 236, 200);
        protected override Color Tinta2 => new(170, 90, 240);
        protected override int Dano => 104;
        protected override int Uso => 30;
        protected override bool NaceEnElPortador => true;
        protected override string Resena => "LA GEOMETRÍA: cuatro ángulos imposibles orbitan al portador — cada esquina, un filo teal";
    }

    /// <summary>10 — LA MORDIDA DEL ESPACIO: los colmillos que caen del vacío.</summary>
    public class MordidaDelEspacio : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_LLUVIA;
        protected override int Variante => 1;
        protected override Color Tinta => new(80, 230, 190);
        protected override Color Tinta2 => new(120, 60, 200);
        protected override int Dano => 92;
        protected override int Uso => 26;
        protected override string Resena => "LA MORDIDA: colmillos de vacío desgarran el cielo sobre la marca — la carne del espacio muerde";
    }

    /// <summary>11 — LA LETANÍA NEBULAR: la zona de la nebulosa que arrastra.</summary>
    public class LetaniaNebular : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_ZONA;
        protected override int Variante => 1;
        protected override Color Tinta => new(140, 90, 235);
        protected override Color Tinta2 => new(80, 220, 190);
        protected override int Dano => 150;
        protected override int Uso => 64;
        protected override string Resena => "LA LETANÍA: una nebulosa violeta se posa en la marca — los que la pisan, disueltos en polvo de estrella";
    }

    /// <summary>12 — EL SUSURRO DEL VACÍO: el vórtice teal que arrastra.</summary>
    public class SusurroDelVacio : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_VORTICE;
        protected override Color Tinta => new(90, 225, 185);
        protected override Color Tinta2 => new(40, 30, 60);
        protected override int Dano => 170;
        protected override int Uso => 44;
        protected override string Resena => "EL SUSURRO: un vacío teal ATRAE a los vivos hacia su corazón y los muele en silencio";
    }

    /// <summary>13 — LA SEMILLA DE ESTRELLAS: la nova que florece.</summary>
    public class SemillaDeEstrellas : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_NOVA;
        protected override int Variante => 1;
        protected override Color Tinta => new(170, 100, 240);
        protected override Color Tinta2 => new(90, 230, 190);
        protected override int Dano => 125;
        protected override int Uso => 40;
        protected override string Resena => "LA SEMILLA: florece en la marca y su corona de estrella estalla en anillos gemelos violeta y teal";
    }

    /// <summary>14 — EL CORO DE LOS PROFUNDOS: el enjambre de semillas tentáculo.</summary>
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

    // ====================================================================
    //  LA CASA DE LA OSCURIDAD — el violeta y el negro tinta (6 armas)
    // ====================================================================

    /// <summary>15 — EL HAMBRE PRIMIGENIA: el vórtice de la fauce.</summary>
    public class HambrePrimigenia : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_VORTICE;
        protected override int Variante => 1;
        protected override Color Tinta => new(178, 110, 236);
        protected override Color Tinta2 => new(16, 10, 26);
        protected override int Dano => 190;
        protected override int Uso => 48;
        protected override string Resena => "EL HAMBRE: la fauce del Eterno en miniatura — arrastra a los vivos al corazón NEGRO y devora";
    }

    /// <summary>16 — LA GUADAÑA DEL VERDUGO: la estela del filo oscuro.</summary>
    public class GuadanaDelVerdugo : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_ESTELA;
        protected override int Variante => 1;
        protected override Color Tinta => new(186, 110, 236);
        protected override Color Tinta2 => new(30, 20, 44);
        protected override int Dano => 160;
        protected override int Uso => 32;
        protected override bool NaceEnElPortador => true;
        protected override string Resena => "LA GUADAÑA: el filo violeta del verdugo vuela a la marca y regresa — su estela corta dos veces";
    }

    /// <summary>17 — LAS CADENAS DEL OLVIDO: la lluvia de eslabones violeta.</summary>
    public class CadenasDelOlvido : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_LLUVIA;
        protected override int Variante => 2;
        protected override Color Tinta => new(178, 110, 236);
        protected override Color Tinta2 => new(40, 28, 66);
        protected override int Dano => 98;
        protected override int Uso => 30;
        protected override string Resena => "LAS CADENAS: eslabones de olvido caen sobre la marca — atan lo que tocan al fondo del cielo";
    }

    /// <summary>18 — EL VELO DE CENIZA: la zona de la ceniza gris.</summary>
    public class VeloDeCeniza : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_ZONA;
        protected override int Variante => 2;
        protected override Color Tinta => new(150, 145, 160);
        protected override Color Tinta2 => new(50, 44, 60);
        protected override int Dano => 160;
        protected override int Uso => 56;
        protected override string Resena => "EL VELO: una nube de ceniza gris cubre la marca — lo que respira dentro, se apaga despacio";
    }

    /// <summary>19 — LAS ESPINAS NOCTURNAS: la corona de espinas negras.</summary>
    public class EspinasNocturnas : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_CORONA;
        protected override int Variante => 2;
        protected override Color Tinta => new(150, 90, 220);
        protected override Color Tinta2 => new(24, 16, 38);
        protected override int Dano => 108;
        protected override int Uso => 30;
        protected override bool NaceEnElPortador => true;
        protected override string Resena => "LAS ESPINAS: cinco agujas de noche orbitan al portador — la corona que nadie lleva dos veces";
    }

    /// <summary>20 — EL ECLIPSE INTERIOR: la nova del disco negro.</summary>
    public class EclipseInterior : ArmaTresCasas
    {
        protected override int Estilo => TresCasasProjectile.ESTILO_NOVA;
        protected override int Variante => 2;
        protected override Color Tinta => new(178, 110, 236);
        protected override Color Tinta2 => new(10, 6, 18);
        protected override int Dano => 190;
        protected override int Uso => 50;
        protected override string Resena => "EL ECLIPSE: un sol negro se enciende en la marca — su corona violeta estalla y APAGA a los vivos";
    }
}
