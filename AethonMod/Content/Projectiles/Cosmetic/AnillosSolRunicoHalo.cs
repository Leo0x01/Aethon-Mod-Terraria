using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    /// <summary>
    /// AnillosSolRunicoHalo — v6.158 — REHECHO DESDE CERO (la letra:
    /// «los anillos de soles runicos siguen si aparecer, lo mejor que
    /// puedes hacer es rehacer el codigo de la construccion desde
    /// cero para darte cuenta de que esta mal, recuerda, el accesorio
    /// La Forma Eterna es como debe de estar»).
    ///
    /// LA RECONSTRUCCIÓN: ESTE ARCHIVO ES LA COPIA LITERAL DE SU
    /// REFERENCIA — AnillosSolaresHalo (SellosCosmeticosHalos.cs), EL
    /// HALO DEL ACCESORIO DE LOS ANILLOS DEL SOL RÚNICO, el que
    /// SellosPlayer enciende cuando el accesorio está equipado. Misma
    /// AI, mismo PreDraw, MISMA LLAMADA EXACTA:
    ///
    ///     SigiloLib.SistemaAnillos(duenio.Center, R, time,
    ///         duenio.whoAmI * 7 + 5, tier: 7, rg: 0f, lifeT: 0f,
    ///         alpha: 0.80f);
    ///
    /// con R = duenio.height * 0.46f (la base del sistema de la capa
    /// de siempre: el primer aro despeja el cuerpo y el séptimo llega
    /// a ~4.3×R) — DENTRO DE SU PROPIO LOTE (VFXCore.Begin →
    /// FlushAdditive), el contrato de la casa.
    ///
    /// LA ÚNICA DIFERENCIA con el halo del accesorio: QUIÉN lo
    /// enciende (CosmeticPlayer.AnillosSolRunico — la construcción de
    /// la Forma Eterna, peldaños 113-120 — en vez de
    /// SellosPlayer.AnillosSol del accesorio equipado). NADA más
    /// cambia: el sistema de anillos es EL MISMO del accesorio, tal
    /// cual, como pide la letra.
    ///
    /// (La v6.157 dibujaba AnilloDorsalRenderer — LA CORONA DEL
    /// AGUJERO NEGRO, el accesorio EQUIVOCADO; la .155 usaba una
    /// escala inventada. Esta versión es la copia textual.)
    /// </summary>
    public class AnillosSolRunicoHalo : ModProjectile
    {
        // La textura FANTASMA de los proyectiles 100%-código (el
        // patrón de la casa: tML exige el asset aunque el PreDraw
        // jamás lo dibuje).
        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            // LITERAL de AnillosSolaresHalo (la referencia del accesorio).
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;      // cosmético: no toca nada
            Projectile.penetrate = -1;
            Projectile.timeLeft = 6;          // CosmeticPlayer lo refresca
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];

            // Los anillos viven mientras su dueño los construya (el
            // campo de la construcción — CosmeticPlayer.AnillosSolRunico,
            // encendido por FormaEternaPlayer en los peldaños 113-120).
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }
            int anillos = duenio.GetModPlayer
                <global::AethonMod.Content.Players.CosmeticPlayer>().AnillosSolRunico;
            if (anillos <= 0)
            {
                Projectile.Kill();
                return;
            }

            // Pegado al cuerpo (el jugador ES el corazón de la
            // constelación) — LITERAL de la referencia.
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 6;

            // Luz cálida de estrella viva — LITERAL de la referencia.
            Lighting.AddLight(Projectile.Center, 0.30f, 0.22f, 0.05f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // LA PRESA TRAGADA: el portador está DENTRO del libro — el
            // cosmético se apaga CON él (la ley de la casa) — LITERAL.
            if (global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna.Tragado &&
                Projectile.owner == Main.myPlayer)
                return false;
            if (Main.dedServ) return false;

            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un
            // Begin vivo — LITERAL de la referencia.
            VFXCore.CerrarLoteSiAbierto();

            float time = Main.GlobalTimeWrappedHourly;

            // La base del sistema de la capa de siempre (el primer aro
            // despeja el cuerpo y el séptimo llega a ~4.3×R) — LITERAL.
            float R = duenio.height * 0.46f;

            try
            {
                VFXCore.Begin();

                // EL SISTEMA COMPLETO — tier 7 de 20 (precesión viva +
                // azul-estelar cada tercer aro), vida plena, intensidad
                // de accesorio — LA MISMA LLAMADA EXACTA del halo del
                // ACCESORIO (AnillosSolaresHalo.PreDraw).
                SigiloLib.SistemaAnillos(duenio.Center, R, time,
                    duenio.whoAmI * 7 + 5, tier: 7, rg: 0f, lifeT: 0f, alpha: 0.80f);

                VFXCore.FlushAdditive(null, false);   // el lote del pase YA está cerrado
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla
            // — LITERAL de la referencia.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
