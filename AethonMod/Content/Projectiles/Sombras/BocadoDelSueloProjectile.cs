using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// BOCADODELSUELOPROJECTILE — v6.51.12 — LA ANIMACIÓN DE DEVORAR LOS
    /// PLATOS DE LA MESA DEL SUELO (la letra del usuario: «el grimorio
    /// devora los items correctamaente, pero necesita hacer la
    /// animacion de devorar, es la misma animacion que usa con los
    /// jefes»).
    ///
    /// EL MISMO FESTÍN DE LOS JEFES, EN MINIATURA — la misma receta del
    /// FauceDevoradorProjectile (el tentáculo que sale DEL PORTADOR,
    /// envuelve, traga y disipa), a escala de plato:
    ///
    ///   0-14   MANIFESTACIÓN — el tentáculo fino sale del portador.
    ///   14-26  ENVOLVER — la bruma abraza al plato (la herida enciende).
    ///   26-70  EL TRAGO — el plato VIAJA por el cuerpo hacia la boca
    ///          del portador, encogiéndose y girando (el fantasma).
    ///   70-88  EL POSO — la última voluta + el destello del trago.
    ///
    /// ES PURA PANTALLA (cero lógica de juego): el consumo de verdad lo
    /// hace ServirLaMesa en el MISMO instante del spawn — esto es el
    /// TEATRO que faltaba (los polvos de la .51.11 solos no eran «la
    /// animación de devorar»).
    ///
    /// ai[0] = índice del item caído · ai[1] = estilo (0 esencias ·
    /// 1 monedas · 2 mineral · 3 el arma del peldaño · 4 el bocado de
    /// siempre) · ai[2] = tick. Projectile.owner = el portador. El
    /// ANCLA del plato y el TIPO del fantasma viajan por SendExtraAI
    /// (el patrón de la casa — las localAI no cruzan el cable).
    /// </summary>
    public class BocadoDelSueloProjectile : ModProjectile
    {
        private const float T_ENVOLVER = 14f;
        private const float T_TRAGO = 26f;
        private const float T_FIN = 70f;
        private const float T_MUERTE = 88f;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 96;
            Projectile.netImportant = true;   // el festín se ve en todas las pantallas
        }

        private Player Portador()
        {
            int i = Projectile.owner;
            if (i < 0 || i >= Main.maxPlayers) return null;
            Player p = Main.player[i];
            return p != null && p.active ? p : null;
        }

        /// <summary>v6.51.12 — EL CABLE EXTRA: el ancla y el tipo del
        /// fantasma viajan con el proyectil (las localAI NO cruzan —
        /// SendExtraAI es el cauce de la casa).</summary>
        public override void SendExtraAI(System.IO.BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[1]);   // ancla X
            writer.Write(Projectile.localAI[2]);   // ancla Y
            writer.Write(Projectile.localAI[3]);   // tipo del fantasma
        }

        public override void ReceiveExtraAI(System.IO.BinaryReader reader)
        {
            Projectile.localAI[1] = reader.ReadSingle();
            Projectile.localAI[2] = reader.ReadSingle();
            Projectile.localAI[3] = reader.ReadSingle();
        }

        /// <summary>El ANCLA del plato: mientras el item caído viva, su
        /// posición (sigue en el suelo si la pila quedó); muerto, la
        /// última anclada (la que viajó por el cable).</summary>
        private Vector2 Ancla()
        {
            int idx = (int)Projectile.ai[0];
            if (idx >= 0 && idx < Main.maxItems && Main.item[idx] != null &&
                Main.item[idx].active && !Main.item[idx].IsAir)
            {
                Projectile.localAI[1] = Main.item[idx].Center.X;
                Projectile.localAI[2] = Main.item[idx].Center.Y;
                if (Main.item[idx].type > 0)
                    Projectile.localAI[3] = Main.item[idx].type;
            }
            return new Vector2(Projectile.localAI[1], Projectile.localAI[2]);
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            Projectile.Center = Ancla();

            if (Main.netMode == NetmodeID.Server) return;   // el server no dibuja ni suena

            // EL SONIDO DEL FESTÍN CHICO (el patrón del grande, en miniatura)
            if (t == 1f)
                Sonar(SoundID.Item113.WithPitchOffset(-0.55f).WithVolumeScale(0.55f), Projectile.Center);
            if (t == T_TRAGO || t == T_TRAGO + 24f)
                Sonar(SoundID.Item122.WithPitchOffset(-0.25f).WithVolumeScale(0.4f), Projectile.Center);

            // el polvillo del plato mientras lo envuelven
            if (t > T_ENVOLVER && t < T_TRAGO && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Ancla() +
                    new Vector2(Main.rand.NextFloat(-12f, 12f), Main.rand.NextFloat(-10f, 6f)),
                    DustID.Shadowflame, new Vector2(0f, -0.9f), 128, default, 0.7f);
                d.noGravity = true;
            }
            if (t >= T_MUERTE) Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                DrawElFestinChico();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();

            // EL PLATO FANTASMA va en el LOTE VANILLA (ya reabierto — el
            // contrato de la casa: dibujar sprites NUNCA dentro del lote VFX)
            try { DibujarElFantasma(); } catch { }
            return false;
        }

        // ==================================================================
        //  EL FESTÍN CHICO — la misma receta del tentáculo de los jefes,
        //  a escala de plato (ColumnaViva + Masa + Bruma + ojos + cabeza
        //  de bruma + el plato viajando por dentro).
        // ==================================================================

        private static readonly Color[] Tinte = new Color[]
        {
            new Color(196, 138, 255),   // 0 esencias — violeta
            new Color(255, 215, 120),   // 1 monedas — oro
            new Color(150, 190, 235),   // 2 mineral — acero
            new Color(255, 130, 110),   // 3 el arma del peldaño — brasa
            new Color(178, 110, 236),   // 4 el bocado de siempre — tinta
        };

        /// <summary>EL CAMINO del cuerpo (lo comparten el festín y el
        /// fantasma — una sola corriente).</summary>
        private Vector2[] Camino(Player portador, Vector2 ancla, float tiempo, int semilla)
            => SombrasLib.ColumnaViva(portador.MountedCenter, ancla, tiempo, semilla,
                12, 0.22f, gancho: 0.42f);

        /// <summary>LA POSICIÓN DEL BOCAO EN VUELO: del plato a la boca,
        /// POR DENTRO del cuerpo (la corriente del trago).</summary>
        private Vector2 PosicionDelBocado(Vector2 raiz, Vector2[] col, Vector2 ancla,
            float progTrago)
        {
            int idxCamino = (int)MathHelper.Lerp(col.Length - 1, 0f, progTrago);
            Vector2 pos = Vector2.Lerp(ancla, col[idxCamino], progTrago);
            return Vector2.Lerp(pos, raiz + new Vector2(0f, -18f), progTrago * progTrago);
        }

        /// <summary>EL PROGRESO DEL TRAGO (0→1 entre T_TRAGO y T_FIN —
        /// con el DeGolpe del festín grande).</summary>
        private float ProgresoTrago()
            => SombrasLib.DeGolpe(MathHelper.Clamp(
                (Projectile.ai[2] - T_TRAGO) / (T_FIN - T_TRAGO), 0f, 1f));

        private void DrawElFestinChico()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 29 + 11;
            float t = Projectile.ai[2];
            Player portador = Portador();
            if (portador == null) return;

            Vector2 ancla = Ancla();
            Vector2 raiz = portador.MountedCenter;
            Color tinte = Tinte[(int)MathHelper.Clamp(Projectile.ai[1], 0f, 4f)];

            // fases
            float manifiesta = SombrasLib.DeGolpe(MathHelper.Clamp(t / T_ENVOLVER, 0f, 1f));
            float envuelve = MathHelper.Clamp((t - T_ENVOLVER) / (T_TRAGO - T_ENVOLVER), 0f, 1f);
            float progTrago = ProgresoTrago();
            float poso = MathHelper.Clamp((t - T_FIN) / (T_MUERTE - T_FIN), 0f, 1f);
            float vivo = 1f - poso * 0.85f;

            // === EL CUERPO: raíz → plato (el alcance de la manifestación
            //     y el envolver — el mismo gesto del grande) ===
            float alcance = manifiesta * 0.4f + envuelve * 0.6f;
            Vector2 destino = Vector2.Lerp(raiz + new Vector2(0f, -50f), ancla, alcance);
            destino = Vector2.Lerp(destino, raiz + new Vector2(0f, -26f), progTrago * 0.9f);
            Vector2[] col = SombrasLib.ColumnaViva(raiz, destino, tiempo, semilla, 12, 0.22f, gancho: 0.42f);

            // EL PERFIL FUSIFORME del grande, fino (es un plato, no un jefe)
            SombrasLib.Masa(col, 5f, 8f, 0.92f * vivo, semilla, tiempo, grosorCentro: 20f);

            // LA BRUMA DEL CUERPO (una capa — el grande lleva dos)
            SombrasLib.BrumaColumna(col, 26f, 0.50f * vivo, tiempo, semilla, 9);

            // LOS OJOS DEL CUERPO (pocos, chicos, TODOS al plato)
            SombrasLib.OjosDeMasa(col, ancla, (0.4f + 0.6f * envuelve) * vivo, semilla, tiempo, 4);

            // === LA POSICIÓN DEL BOCAO EN VUELO (el plato por dentro) ===
            Vector2[] colPleno = Camino(portador, ancla, tiempo, semilla);
            Vector2 posBocado = PosicionDelBocado(raiz, colPleno, ancla, progTrago);

            // === LA CABEZA DE BRUMA persigue al bocado (la boca que mastica) ===
            Vector2 rumbo = (posBocado - col[Math.Max(col.Length - 3, 0)])
                .SafeNormalize(-Vector2.UnitY);
            float muerde = CicloMasticar(t);
            SombrasLib.CabezaDeBruma(posBocado + rumbo * 8f, rumbo,
                (0.55f + 0.45f * muerde) * vivo, vivo, tiempo, semilla);

            // === LA HERIDA (el punto rojo donde mordió — la digestión) ===
            VFXCore.Begin();
            VFXCore.Quad(ancla, SombrasLib.Alfa(SombrasLib.Rojo,
                (0.30f + 0.25f * envuelve) * (1f - progTrago) * vivo),
                new Vector2(48f, 48f), rumbo.ToRotation());
            // y el TINTE del plato brillando dentro de la bruma
            VFXCore.Quad(ancla, SombrasLib.Alfa(tinte, 0.22f * (1f - progTrago) * vivo),
                new Vector2(40f, 40f), 0f);
            VFXCore.FlushAdditive();

            // === LA CORRIENTE DEL TRAGO — bruma que vuelve por el cuerpo ===
            if (progTrago > 0.05f)
            {
                for (int k = 0; k < 3; k++)
                {
                    float prog = SombrasLib.Frac(tiempo * 0.9f + k * 0.33f);
                    float idxF = (1f - prog) * (col.Length - 1);
                    int idx = Math.Min((int)idxF, col.Length - 2);
                    if (idx < 0) idx = 0;
                    Vector2 pos = Vector2.Lerp(col[idx], col[idx + 1], SombrasLib.Frac(idxF))
                        + new Vector2(MathF.Sin(k * 2.4f + tiempo * 3f) * 10f, -7f);
                    SombrasLib.Bruma(pos, 18f + 10f * SombrasLib.Frac(k * 0.71f),
                        0.45f * progTrago * vivo, tiempo, semilla + k * 7,
                        new Vector2(0f, -22f * progTrago));
                }
            }

            // === EL POSO — la última voluta + el destello final ===
            if (t >= T_FIN)
            {
                float fade = 1f - poso;
                SombrasLib.Bruma(ancla, 46f * (0.5f + 0.5f * fade), 0.5f * fade,
                    tiempo, semilla, new Vector2(MathF.Sin(tiempo * 0.8f) * 12f, -10f));
                if (t < T_FIN + 8f)
                    SombrasLib.OndaChoque(ancla, 30f + 90f * (t - T_FIN) / 8f, 0.35f * fade);
            }
        }

        /// <summary>EL PLATO FANTASMA: el sprite del item VIAJANDO por el
        /// cuerpo hacia la boca (encogiéndose + girando — el bocado que
        /// se lo traga el libro). Dibujado en el LOTE VANILLA (después
        /// de ReabrirLoteVanilla — el contrato de la casa).</summary>
        private void DibujarElFantasma()
        {
            Player portador = Portador();
            if (portador == null) return;
            float progTrago = ProgresoTrago();
            if (progTrago <= 0.02f || progTrago >= 0.99f) return;

            int tipo = (int)Projectile.localAI[3];
            if (tipo <= 0) return;
            Texture2D tex = Terraria.GameContent.TextureAssets.Item[tipo].Value;
            if (tex == null) return;

            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 29 + 11;
            Vector2 ancla = Ancla();
            Vector2 raiz = portador.MountedCenter;
            Vector2[] col = Camino(portador, ancla, tiempo, semilla);
            Vector2 posBocado = PosicionDelBocado(raiz, col, ancla, progTrago);

            float esc = MathHelper.Lerp(0.95f, 0.30f, progTrago);
            float giro = Projectile.ai[2] * 0.22f * (Projectile.whoAmI % 2 == 0 ? 1f : -1f);
            float vivo = 1f - MathHelper.Clamp(
                (Projectile.ai[2] - T_FIN) / (T_MUERTE - T_FIN), 0f, 1f) * 0.85f;
            Color luz = Color.Lerp(Lighting.GetColor(posBocado.ToTileCoordinates()),
                Color.White, 0.25f) * vivo;
            Main.spriteBatch.Draw(tex, posBocado - Main.screenPosition, null,
                luz, giro, tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }

        /// <summary>El masticar del festín chico (el ciclo del grande, rápido).</summary>
        private static float CicloMasticar(float t)
        {
            float m = (t - T_TRAGO) % 18f;
            if (m < 6f) return MathHelper.Lerp(0.2f, 1f, m / 6f);
            if (m < 10f) return MathHelper.Lerp(1f, 0.05f, SombrasLib.DeGolpe((m - 6f) / 4f));
            return 0.15f + 0.1f * MathF.Sin(m * 1.6f);
        }
    }
}
