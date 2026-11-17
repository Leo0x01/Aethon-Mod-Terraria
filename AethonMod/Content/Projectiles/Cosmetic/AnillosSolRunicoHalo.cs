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
    /// AnillosSolRunicoHalo — v6.150 — LOS ANILLOS DE SOLES RÚNICOS DE
    /// LA CONSTRUCCIÓN DE LA FORMA ETERNA.
    ///
    /// LA LETRA: «la forma eterna no viste los anillos de soles runicos
    /// mientras se construye hasta nivel 112, por lo tanto que se
    /// construya hasta nivel 120 y que se muestren los anillos de soles
    /// runicos mientras lo hace».
    ///
    /// Durante los peldaños 113-119 del despertar (DESPUÉS de las 11
    /// partes, ANTES de la escupida del 120), EL SISTEMA DE ANILLOS DEL
    /// SOL RÚNICO — el mismo de SigiloLib.SistemaAnillos que viste a
    /// los soles de verdad — se enciende UNO A UNO alrededor del
    /// portador: cada peldaño un anillo más (1 al 113 … 7 al 119), el
    /// vestido del sol creciendo hacia el 120. No es una copia
    /// "parecida": ES el mismo trazo, el mismo latido y los mismos
    /// glifos de siempre (la ley de la casa — nada de geometría plana).
    ///
    /// Patrón probado de AnilloRunicoDorsalHalo/BrilloSolarHalo: pegado
    /// al dueño, el dueño local lo invoca y tML lo sincroniza, PreDraw
    /// por el camino aditivo de la casa (cerrar el lote del pase →
    /// vuelco propio → reabrir TAL CUAL).
    /// </summary>
    public class AnillosSolRunicoHalo : ModProjectile
    {
        // La textura FANTASMA de los proyectiles 100%-código (el patrón
        // de la casa: sin este override, MissingResourceException y TODO
        // el mod se desactiva al cargar).
        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;      // cosmético: no toca nada
            Projectile.penetrate = -1;
            Projectile.timeLeft = 6;          // CosmeticPlayer lo refresca
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            // SIN hide (la lección v6.35: tML no dibuja los ocultos).
        }

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];

            // Los anillos viven mientras su dueño los construya.
            if (duenio == null || !duenio.active || duenio.dead ||
                duenio.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>()
                    .AnillosSolRunico <= 0)
            {
                Projectile.Kill();
                return;
            }

            // Pegado al dueño, siempre.
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 6;

            // La luz del sol en construcción (dorada cálida).
            Lighting.AddLight(Projectile.Center, 0.30f, 0.22f, 0.10f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // LA PRESA TRAGADA: el portador está DENTRO del libro — el
            // cosmético se apaga CON él (la ley de la casa).
            if (global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna.Tragado &&
                Projectile.owner == Main.myPlayer)
                return false;
            if (Main.dedServ) return false;

            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead) return false;

            int anillos = duenio.GetModPlayer
                <global::AethonMod.Content.Players.CosmeticPlayer>().AnillosSolRunico;
            if (anillos <= 0) return false;

            float t = Main.GlobalTimeWrappedHourly;
            // El pegue del festín (v6.131): el delta del CUERPO COMPLETO
            // se hereda mientras el Eterno arrastra al portador.
            Vector2 centro = Projectile.Center +
                (global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna
                    .ArrastrandoA(Projectile.owner)
                    ? global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna
                        .DeltaDelFestin
                    : Vector2.Zero);

            // EL SISTEMA DE ANILLOS DEL SOL RÚNICO — LA MISMA LEY que
            // viste a los soles (SigiloLib.SistemaAnillos): los anillos
            // elípticos con precesión, las runas de pie, el oro y el
            // azul-estelar cada 3º. Cada peldaño 113-119 enciende UNO:
            // el vestido crece hacia el 120. Semilla del dueño (una
            // identidad por portador).
            int semilla = Projectile.identity % 89 + 11;

            VFXCore.CerrarLoteSiAbierto();
            VFXCore.Begin();
            try
            {
                // R de los anillos del sol: proporcional al cuerpo del
                // portador (el sol de la casa con BodyPx=34; aquí el
                // portador ES la estrella — su altura es la regla).
                float R = 34f * (duenio.height / 42f);
                SigiloLib.SistemaAnillos(centro, R, t, semilla,
                    anillos, 0f, 1f, 1f);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.FlushAdditive();
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
