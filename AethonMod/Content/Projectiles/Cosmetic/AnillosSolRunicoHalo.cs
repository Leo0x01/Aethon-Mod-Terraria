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
    /// partes, ANTES de la escupida del 120), LA CORONA DORSAL DEL
    /// ACCESORIO (AnilloDorsalRenderer — la versión que manda, la letra
    /// v6.156) se enciende PARTE POR PARTE alrededor del portador: cada
    /// peldaño más alpha y más runas (113 → la corona joven; 119-120 →
    /// LA PLENA del accesorio), el vestido del sol creciendo hacia el
    /// 120. No es una copia "parecida": ES EL MISMO RENDERER del
    /// accesorio AnilloRunicoDorsal (el anillo de fotones + la triple
    /// corona de conjuro con sus runas de pie).
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
            // El ancla del festín (v6.131) queda como red de seguridad
            // del centro; la corona de la construcción (v6.156) se
            // ancla a LA ESPALDA del portador, como el accesorio.
            Vector2 centro = Projectile.Center +
                (global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna
                    .ArrastrandoA(Projectile.owner)
                    ? global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna
                        .DeltaDelFestin
                    : Vector2.Zero);
            _ = centro;

            // ============================================================
            //  v6.157 — LA CORONA ES LA DEL ACCESORIO, TAL CUAL (la
            // letra: «solo toma los anillos del accesorio tal cual
            // estan y copialos para la construccion»): LA MISMA
            // LLAMADA EXACTA de AnilloRunicoDorsalHalo (el halo del
            // ACCESORIO): alpha 1.0 · runas 1.0 · el mismo anclaje a
            // la espalda · el mismo tamaño (Radio = 0.348 × altura —
            // la corona externa ABRAZA al jugador). LA RAMPA de la
            // v6.156 (alpha 0.60+0.10×n, runas 0.70+0.10×n — «no
            // tienen el tamaño correcto») MURIÓ: no hay corona joven
            // ni corona vieja — ES LA CORONA, idéntica al accesorio
            // desde el peldaño 113 hasta la escupida del 120.
            // ============================================================
            Vector2 espalda = duenio.Center -
                new Vector2(0f, duenio.height * 0.04f * duenio.gravDir);
            if (global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna
                    .ArrastrandoA(Projectile.owner))
            {
                Vector2 eje = duenio.fullRotationOrigin;
                Vector2 rel = espalda - (duenio.position + eje);
                espalda = duenio.position + eje + rel.RotatedBy(duenio.fullRotation);
            }
            try
            {
                global::AethonMod.Content.VFX.AnilloDorsalRenderer.Draw(
                    espalda, duenio.height, t, 1f, 1f);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            return false;
        }
    }
}
