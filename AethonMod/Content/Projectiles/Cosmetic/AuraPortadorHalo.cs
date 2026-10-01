using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    /// <summary>
    /// AuraPortadorHalo — v6.48 — EL PORTADOR DEL AURA DEL JUGADOR.
    ///
    /// LA MEJORA DE AURALIB que cierra el huecho del camino del jugador:
    /// los NPCs dibujan su aura ADITIVA (neón) desde PreDraw; el jugador
    /// iba por DrawData (AlphaBlend) y las auras de color vivo salían
    /// PLANAS (la lección v6.40). Este proyectil cosmético pegado a su
    /// dueño dibuja la CAPA TRASERA del aura por el CAMINO ADITIVO:
    /// vanilla dibuja los proyectiles ANTES que los jugadores → la capa
    /// queda DETRÁS del cuerpo (la profundidad del look, igual que los
    /// NPCs) y con el brillo de neón de verdad.
    ///
    /// LOS TRES PORTADORES (ai[0] = modo):
    ///   0 — LA CENIZA DEL HAMBRE: el aura gris del grimorio hambriento
    ///       (la lee de ShardPlayer, el jugador local la viste).
    ///   2 — LA FORMA ASCENDIDA: el aura dorada-violeta de la Luz
    ///       Primordial (el drop cumplido de Aethon; v6.50.44 — LA
    ///       CORONA RÚNICA murió fusionada en ella: «los item La forma
    ///       Ascendida y la corona runica de aura son lo mismo, dejar
    ///       solo La Forma Ascendida»).
    ///   3 — v6.50.23 — LA BRASA DEL ECLIPSE: el cuarto tipo de aura —
    ///       el patrón BRUMA (humo negro en los bordes por ALFA-blend,
    ///       oro en el medio y núcleo rojo aditivos, con luz de mundo
    ///       cálida que LATE con el corazón de la brasa).
    ///
    /// El VELO FRONTAL (el 6% que pisa el cuerpo) sigue por DrawData
    /// (AuraJugadorFrontal): a esa transparencia no necesita neón y la
    /// capa frontal debe pisar el sprite — los proyectiles dibujan antes.
    ///
    /// Patrón probado de AnilloRunicoDorsalHalo (v6.37): PreDraw cierra
    /// el lote del pase, AuraLib vuelca el SUYO aditivo y el lote del
    /// pase se reabre TAL CUAL. SIN hide (la lección v6.35).
    ///
    /// v6.49 (auditoría AUD-C) — EL CONTRATO DE VERDAD: el End manual del
    /// patrón v6.10 peleaba con el bool de DibujarJugadorAditivo (doble
    /// End → excepción tragada cada frame + estado a ciegas). Ahora es el
    /// contrato de OleadaNPC: el BOOL decide, ReabrirLoteVanilla reabre.
    /// Y los perfiles se creaban NUEVOS en cada llamada (2 por tick:
    /// AI + PreDraw) — ahora son cache estático (son inmutables en la
    /// práctica; el modo hambre ya usaba el cache de ShardPlayer).
    /// </summary>
    public class AuraPortadorHalo : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        // v6.49 — EL CACHE DE LOS PERFILES INMUTABLES (cero GC por frame).
        private static AuraPerfil _perfilAscendida;
        private static AuraPerfil _perfilAscendidaDos;
        private static AuraPerfil _perfilBrasa;

        /// <summary>El perfil de la forma ascendida, creado UNA vez.</summary>
        private static AuraPerfil PerfilAscendida =>
            _perfilAscendida ??= AuraPerfil.FormaAscendida();

        /// <summary>v6.50.48 — El perfil de LA FORMA ASCENDIDA 2 (la
        /// apoteosis absoluta: las once capas de la .47 elevadas + la
        /// mandorla, la corona de doce estrellas, los siete candeleros,
        /// el río de luz y el DOBLE de plumas en las alas), creado UNA vez.</summary>
        private static AuraPerfil PerfilAscendidaDos =>
            _perfilAscendidaDos ??= AuraPerfil.FormaAscendidaDos();

        /// <summary>v6.50.23 — El perfil de la brasa del eclipse, creado UNA vez.</summary>
        private static AuraPerfil PerfilBrasa =>
            _perfilBrasa ??= AuraPerfil.BrasaDelEclipse();

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
            Projectile.timeLeft = 6;          // el dueño lo refresca
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        /// <summary>El modo del portador (0 hambre · 2 ascendida · 3 brasa ·
        /// 4 ascendida 2 — el 1 era la corona rúnica, muerta en la v6.50.44).</summary>
        private int Modo => (int)Projectile.ai[0];

        /// <summary>¿El dueño sigue VISTIENDO el aura de este modo?</summary>
        private bool SigueViva(Player duenio)
        {
            switch (Modo)
            {
                case 0:
                    return duenio.whoAmI == Main.myPlayer &&
                           duenio.GetModPlayer<ShardPlayer>().MomentosHambre > 0;
                case 2:
                    return duenio.GetModPlayer<CosmeticPlayer>().FormaAscendida;
                case 3:
                    return duenio.GetModPlayer<CosmeticPlayer>().BrasaDelEclipse;
                case 4:
                    return duenio.GetModPlayer<CosmeticPlayer>().FormaAscendidaDos;
            }
            return false;
        }

        /// <summary>El PERFIL del aura de este modo.</summary>
        private AuraPerfil Perfil(Player duenio)
        {
            switch (Modo)
            {
                case 0: return duenio.GetModPlayer<ShardPlayer>().AuraHambrePublica();
                case 2: return PerfilAscendida;
                case 3: return PerfilBrasa;
                case 4: return PerfilAscendidaDos;
            }
            return null;
        }

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];

            if (duenio == null || !duenio.active || duenio.dead || !SigueViva(duenio))
            {
                Projectile.Kill();
                return;
            }

            // Pegado al dueño, siempre.
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 6;

            // LAS PARTÍCULAS corren en la AI (el contrato de la casa —
            // nunca en el render).
            AuraPerfil p = Perfil(duenio);
            if (p != null)
                AuraLib.ActualizarJugador(duenio, p);

            // La luz suave del modo (la corona ilumina, el hambre oscurece).
            // v6.50.44 — LA LUZ DIVINA del ascendido: más cálida, más
            // ancha y RESPIRANDO (el pulso lento de un dios).
            else if (Modo == 2)
            {
                float alba = 0.85f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 1.6f);
                Lighting.AddLight(Projectile.Center, 0.30f * alba, 0.24f * alba, 0.11f * alba);
            }
            // v6.50.23 — LA BRASA: la LUZ de la petición — cálida (oro
            // con rojo) y LATE con el corazón de la brasa (el doble
            // golpe de 84 bpm del perfil, la misma curva de AuraLib).
            else if (Modo == 3)
            {
                float b = FracLatido(Main.GlobalTimeWrappedHourly * 1.4f);
                float golpe = MathF.Pow(MathF.Sin(MathHelper.Pi * b), 14f);
                float eco = MathF.Pow(MathF.Sin(MathHelper.Pi * FracLatido(b + 0.18f)), 14f);
                float lat = 1f + 0.45f * (golpe + 0.55f * eco);
                Lighting.AddLight(Projectile.Center, 0.30f * lat, 0.13f * lat, 0.02f * lat);
            }
        }

        /// <summary>La parte fraccionaria (siempre positiva).</summary>
        private static float FracLatido(float x) => x - MathF.Floor(x);

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead) return false;

            AuraPerfil p = Perfil(duenio);
            if (p == null) return false;

            // v6.49 — EL CONTRATO DEL BOOL (el de OleadaNPC):
            // DibujarJugadorAditivo cierra el lote del pase, vuelva SU
            // aditivo y DEVUELVE true; aquí se reabre TAL CUAL. El End
            // manual del patrón v6.10 moría con el End interno del
            // FlushAdditive (excepción tragada + reapertura a ciegas).
            try
            {
                if (AuraLib.DibujarJugadorAditivo(duenio, p))
                    AuraLib.ReabrirLoteVanilla();
            }
            catch { try { AuraLib.ReabrirLoteVanilla(); } catch { } }

            return false; // el halo SE dibuja solo (el vuelco fue el dibujo)
        }
    }
}
