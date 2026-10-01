using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Oleadas
{
    /// <summary>
    /// AtaqueOleadaProjectile — v6.48 — LOS PROYECTILES DE LOS GUARDIANES.
    ///
    /// v6.50.48 — LA QUINTA RONDA (la letra del usuario): «todos esas
    /// formas de ataques extras de los jefes, o sea los proyectiles
    /// brillantes y los tajos que tienen no combinan nada con el jefe».
    /// SEIS de los siete dientes de librería MURIERON (las cuentas del
    /// trono, los tajos del vigía, el abanico de aguijones, las fauces
    /// corruptas, el estallido carmesí y las calaveras en órbita): los
    /// guardianes ahora CONVOCAN A LOS SUYOS (monstruos y proyectiles
    /// ORIGINALES de Terraria — véase OleadaNPC.Coreografia). Quedan
    /// en este proyectil solo los dos que SÍ son su tema:
    ///
    ///   Estilo 2 — DEERCLOPS, "Látigos de Escarcha": espinas de hielo
    ///     que caen del cielo en zigzag (StormLib.ChainBolt) y estallan
    ///     al clavarse (polvo de hielo + sonido). La escarcha ES él.
    ///   Estilo 7 — REY GELATINA, "La Bola de Gel": EL ÍTEM GEL de
    ///     Terraria dibujado como proyectil (la letra: «bolas de slime
    ///     que es un item de terraria, creo que se llama Gel») con
    ///     gravedad de verdad, REBOTE (dos picos) y SALPICÓN de polvo
    ///     azul al morir — al despegar el Rey y AL ATERRIZAR, doce
    ///     bolas hacia todas las direcciones al azar.
    ///
    /// REGLAS DE LA CASA: daño por COLISIÓN del motor, cero Main.rand
    /// en el render, SIN hide, el contrato de lote cerrado->cerrado.
    /// </summary>
    public class AtaqueOleadaProjectile : ModProjectile
    {
        // === LOS ESTILOS (ai[0]) ===
        public const int EstiloDeerclops = 2;   // el invierno caminante (le queda)
        public const int EstiloBolaGel = 7;     // el Rey Gelatina: el ítem Gel como proyectil

        /// <summary>El estilo del diente (ai[0]).</summary>
        private int Estilo => (int)Projectile.ai[0];
        /// <summary>Semilla determinista (ai[2]).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[2] % 9973);

        // === EL ESTADO (por estilo) ===
        private float _edad;

        // LA BOLA DE GEL: su física propia.
        private Vector2 _velAnterior;   // para leer el rebote del motor
        private int _botes;             // dos picos y salpica
        private float _giro;            // el rodar del gel

        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;   // la BOLA la enciende su lanzador
            Projectile.ignoreWater = true;
            Projectile.light = 0.6f;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _velAnterior = Projectile.velocity;
        }

        /// <summary>La presa más cercana (el portador del libro).</summary>
        private Player Presa()
        {
            Player mejor = null;
            float mejorD = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead) continue;
                float d = Vector2.DistanceSquared(p.Center, Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = p; }
            }
            return mejor;
        }

        public override void AI()
        {
            _edad++;

            switch (Estilo)
            {
                // =============================================================
                //  LOS LATIGOS DE ESCARCHA — caída en zigzag + estallido
                //  (DEERCLOPS: la escarcha ES su tema — sobrevivió a la
                //  quinta ronda porque le queda)
                // =============================================================
                case EstiloDeerclops:
                {
                    // Cae con vaivén (el látigo del invierno).
                    Projectile.velocity.X = MathF.Sin(_edad * 0.35f + Seed) * 2.6f;
                    Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.28f, 11f);

                    // ¿CLAVADO? (suelo debajo o edad límite)
                    Point tile = Projectile.Center.ToTileCoordinates();
                    bool clavado = false;
                    if (tile.X > 5 && tile.X < Main.maxTilesX - 5 &&
                        tile.Y > 5 && tile.Y < Main.maxTilesY - 5)
                    {
                        Tile tl = Main.tile[tile.X, tile.Y + 1];
                        clavado = tl != null && tl.HasTile && Main.tileSolid[tl.TileType];
                    }
                    if (clavado || _edad > 200)
                    {
                        // EL ESTALLIDO: el polvo del impacto + fin del látigo.
                        for (int d = 0; d < 6; d++)
                        {
                            int idx = Dust.NewDust(Projectile.Center, 10, 10,
                                DustID.IceTorch,
                                -Main.rand.NextFloat(3f, 6f) * MathF.Cos(d * MathHelper.TwoPi / 6f),
                                -Main.rand.NextFloat(2f, 5f));
                            Main.dust[idx].noGravity = true;
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item30, Projectile.Center);
                        Projectile.Kill();
                    }
                    break;
                }

                // =============================================================
                //  LA BOLA DE GEL (el Rey Gelatina) — el ítem Gel de
                //  Terraria como proyectil: gravedad, REBOTE y salpicón
                // =============================================================
                case EstiloBolaGel:
                {
                    // LA GRAVEDAD de verdad (la bola es física).
                    Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.34f, 16f);
                    _giro += Projectile.velocity.X * 0.06f;

                    // EL REBOTE: el motor de colisiones ZERO la velocidad
                    // al chocar — la lectura de la velocidad ANTERIOR dice
                    // qué eje murió y la bola SALTA.
                    bool piso = _velAnterior.Y > 3f && Projectile.velocity.Y < _velAnterior.Y * 0.3f;
                    bool techo = _velAnterior.Y < -3f && Projectile.velocity.Y > _velAnterior.Y * 0.3f;
                    bool pared = Math.Abs(_velAnterior.X) > 3f &&
                                 Math.Abs(Projectile.velocity.X) < Math.Abs(_velAnterior.X) * 0.3f;
                    if (piso || techo || pared)
                    {
                        _botes++;
                        if (_botes >= 2)
                        {
                            SalpicarGel();
                            break;
                        }
                        // EL PICO: la bola rebota muriendo un poco.
                        Projectile.velocity = new Vector2(
                            _velAnterior.X * (pared ? -0.62f : 0.72f),
                            _velAnterior.Y * (piso ? -0.55f : (techo ? -0.55f : 0.72f)));
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
                    }
                    _velAnterior = Projectile.velocity;

                    if (_edad > 300) SalpicarGel();
                    break;
                }
            }

            // La luz del proyectil (el color propio de su tema).
            if (Estilo == EstiloDeerclops)
                Lighting.AddLight(Projectile.Center, new Vector3(0.68f, 0.86f, 0.95f));
            else if (Estilo == EstiloBolaGel)
                Lighting.AddLight(Projectile.Center, new Vector3(0.22f, 0.36f, 0.92f)); // el azul del gel
        }

        /// <summary>
        /// EL SALPICÓN (la muerte de la bola): «al caer muchas de esas
        /// bolas salpican del jefe hacia todas las direcciones» — la
        /// bola revienta en polvo azul de gel hacia todos lados.
        /// </summary>
        private void SalpicarGel()
        {
            if (!Main.dedServ)
            {
                for (int d = 0; d < 9; d++)
                {
                    float ang = d * MathHelper.TwoPi / 9f;
                    int idx = Dust.NewDust(Projectile.Center, 8, 8, DustID.BlueMoss,
                        MathF.Cos(ang) * Main.rand.NextFloat(2.5f, 5f),
                        MathF.Sin(ang) * Main.rand.NextFloat(2.5f, 5f) - 1.5f);
                    Main.dust[idx].noGravity = true;
                    Main.dust[idx].scale = 1.1f;
                }
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
            Projectile.Kill();
        }

        // ==================================================================
        //  EL RENDER — cada proyectil con SU tema. EL FLUJO DE LA CASA:
        //  (1) los quads al BÚFER (coords de MUNDO) y su volcado propio;
        //  (2) el lote aditivo NUESTRO para las librerías de coords de
        //  PANTALLA; (3) el lote del pase reabierto TAL CUAL.
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            VFXCore.CerrarLoteSiAbierto();

            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 pos = Projectile.Center - Main.screenPosition;

                // === FASE 1 — EL BÚFER DE QUADS (coords de MUNDO) ===
                switch (Estilo)
                {
                    case EstiloDeerclops:
                        VFXCore.Quad(Projectile.Center, new Color(230, 245, 255) * 0.8f,
                            new Vector2(12f, 18f), Projectile.velocity.ToRotation() + MathHelper.PiOver2);
                        break;

                    case EstiloBolaGel:
                        // LA BOLA: el SPRITE DEL ÍTEM GEL dibujado tal cual
                        // (la letra del usuario: es un item de terraria).
                        if (!Main.dedServ)
                        {
                            Texture2D gel = Terraria.GameContent.TextureAssets.Item[ItemID.Gel].Value;
                            Main.spriteBatch.Draw(gel, Projectile.Center - Main.screenPosition, null,
                                Microsoft.Xna.Framework.Color.White * 0.96f, _giro,
                                gel.Size() * 0.5f, 1.15f, SpriteEffects.None, 0f);
                        }
                        break;
                }
                VFXCore.FlushAdditive(null, false);

                // === FASE 2 — EL LOTE ADITIVO DE LAS LIBRERÍAS (PANTALLA) ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                switch (Estilo)
                {
                    // === LOS LATIGOS DE ESCARCHA: el zigzag helado ===
                    case EstiloDeerclops:
                    {
                        Vector2 cola = pos - Projectile.velocity * 2.4f;
                        StormLib.ChainBolt(Main.spriteBatch, cola, pos, Seed,
                            StormLib.FlickTick(t, 12f), 2.6f,
                            new Color(120, 190, 235), new Color(230, 245, 255), 0.85f);
                        LumenLib.Bloom(Main.spriteBatch, pos, 22f,
                            new Color(168, 220, 242), 0.8f);
                        break;
                    }

                    // === LA BOLA DE GEL: el brillo azul del ítem ===
                    case EstiloBolaGel:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 26f,
                            new Color(80, 140, 255), 0.45f);
                        break;
                    }
                }
                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            finally
            {
                VFXCore.ReabrirLoteVanilla();
            }
            return false;
        }
    }
}
