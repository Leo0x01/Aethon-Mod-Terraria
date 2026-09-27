using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// VenaTruenoProjectile — v6.50.24 — LA CAÍDA DEL DRAGÓN DE VENA TRUENO.
    ///
    /// LA PETICIÓN CON NOMBRE Y APELLIDOS: "para los rayos crea una nueva
    /// arma basada en thundervein dragon Coralite mod terraria". La
    /// investigación (Task 2-a, repo oficial de Coralite descargado): el
    /// Thundervein Dragon (荒雷龙) es el wyvern eléctrico del mod chino
    /// Coralite — cuerpo negro con venas de trueno amarillas — y SU firma
    /// visual es el ThunderFalling (落雷): TRES zigzags superpuestos (1
    /// NARANJA + 2 AMARILLO — ThunderveinOrange 219,114,22 y
    /// ThunderveinYellow 255,202,101, extraídas de su código) que caen del
    /// cielo creciendo de 50 a 120 px de ancho, PARPADEANDO (CanDraw =
    /// NextBool, re-random cada 4 ticks) y desintegrándose en la agonía
    /// (el zigzag se ABRE mientras el alfa muere con X2Ease).
    ///
    /// ESTE proyectil ES ESO, montado sobre el RayoLib de la casa (el
    /// puerto 1:1 del LightningGenerator de vanilla 1.4.5): el generador
    /// hace el zigzag (raymarch de 4 capas con timón y horquillas — los
    /// tres canales germinan con semillas distintas: el trío ligeramente
    /// desalineado del dragón), y la FIRMA VENA TRUENO del RayoSistema
    /// (v6.50.24: parpadeo + recada + ancho creciente + apertura del
    /// colapso) hace el flicker, la caída y la desintegración. CERO
    /// sprites: el pincel es el pixel 1×1 del motor con la pila de
    /// pasadas sólidas.
    ///
    /// LA CADENCIA DEL DRAGÓN: telegrafiada como su FallingThunder — el
    /// canal cae durante 26 ticks (el frente desciende energizando) y el
    /// GOLPE aterriza al final: radial 130 px + CADENA de 3 (el
    /// LightningRaid: "3 cortos + 1 largo") + Electrified. Los que se
    /// mueven antes de que caiga, esquivan — la furia del dragón tiene
    /// fecha. Sin maná (regla de la casa para las armas de prueba).
    /// </summary>
    public class VenaTruenoProjectile : ModProjectile
    {
        /// <summary>Los ticks de la caída (el LightingTime del ThunderFalling).</summary>
        private const int CaidaTicks = 26;

        private bool _nacio;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            // El daño se aplica a mano al ATERRIZAR la caída: sin contacto.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = CaidaTicks + 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        // LA PALETA DEL DRAGÓN (extraída del código de Coralite: las
        // constantes de ThunderveinDragon.cs, líneas 72-74).
        private static readonly Color AmarilloVena = new(255, 202, 101);
        private static readonly Color NaranjaVena = new(219, 114, 22);
        private static readonly Color NucleoVena = new(255, 246, 215);

        public override void AI()
        {
            Vector2 impacto = Projectile.Center;
            Player dueno = Main.player[Projectile.owner];
            bool vivoDueno = dueno != null && dueno.active;

            // === EL NACIMIENTO: EL TRÍO DEL DRAGÓN (solo quien tiene ojos) ===
            if (!_nacio)
            {
                _nacio = true;
                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                {
                    LanzarTrio(impacto);
                    // EL RUGIDO DE LA TORMENTA que convoca al dragón (el
                    // trueno grave que anuncia la caída).
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item12.WithPitchOffset(-0.45f).WithVolumeScale(0.9f), impacto);
                }
            }

            // === LA CAÍDA (el telegraph vivo): chispas amarillas corriendo
            //     por el frente descendente (el SpawnDusts del original —
            //     ElectricParticle/LightningShineBall en la casa: Electric
            //     amarillo) ===
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                float p = 1f - Projectile.timeLeft / (float)(CaidaTicks + 2);
                Vector2 cielo = impacto - Vector2.UnitY * 1000f;
                Vector2 frente = Vector2.Lerp(cielo, impacto, MathHelper.Clamp(p * 1.25f, 0f, 1f));
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = frente + Main.rand.NextVector2Circular(30f, 30f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.Electric,
                        Main.rand.NextVector2Circular(0.6f, 0.6f),
                        180, AmarilloVena, Main.rand.NextFloat(0.4f, 0.8f));
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // === EL ATERRIZAJE (la máquina dueña manda; GolpeMotor en el
            //     cauce de la casa) ===
            if (Projectile.timeLeft <= 2 && Projectile.owner == Main.myPlayer)
            {
                Aterrizar(impacto);
            }
        }

        /// <summary>
        /// EL TRÍO VENA TRUENO: tres canales del RayoSistema sobre el mismo
        /// blanco — 1 NARANJA + 2 AMARILLO (la receta Coralite), cada uno
        /// con su propia semilla (zigzags desalineados), su PARPADEO del
        /// 50%, su RECADA de 4 ticks, el ANCHO creciendo 46→92 en la caída
        /// y la APERTURA del colapso (la desintegración del zigzag al
        /// morir). Cae de 1000 px como el rayo del clima (choca con tiles).
        /// </summary>
        private static void LanzarTrio(Vector2 impacto)
        {
            // El generador del clima de vanilla: cae de 1000 px con ±20° de
            // deriva, choca con tiles y líquidos, horquillas reflejadas.
            RayoParams plantilla = RayoParams.Tormenta;

            var colores = new[] { NaranjaVena, AmarilloVena, AmarilloVena };
            var anchos = new[] { 46f, 44f, 42f };

            for (int i = 0; i < 3; i++)
            {
                RayoSistema.Lanzar(plantilla, (uint)Main.rand.Next(1, int.MaxValue),
                    impacto, null,
                    colores[i], NucleoVena,
                    ancho: anchos[i], vidaTicks: 52, AnimRayo.VenaTrueno,
                    luzEstable: i == 0, jugadorAncla: -1,
                    recada: 4, parpadeo: true, anchoFin: anchos[i] * 2.0f,
                    aperturaColapso: true);
            }
        }

        /// <summary>EL GOLPE DEL ATERRIZAJE: radial + cadena de 3 + el raid de arcos.</summary>
        private void Aterrizar(Vector2 impacto)
        {
            // === EL CINE DEL IMPACTO (solo quien tiene ojos) ===
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                for (int i = 0; i < 26; i++)
                {
                    float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float spd = Main.rand.NextFloat(2f, 9f);
                    Dust d = Dust.NewDustPerfect(impacto, DustID.Electric,
                        new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang) * 0.8f) * spd,
                        220, i % 3 == 0 ? NaranjaVena : AmarilloVena, Main.rand.NextFloat(0.8f, 1.5f));
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                // EL GOLPE DE PANTALLA de la casa (la caída de un dragón).
                Pantalla.PresetImpacto(impacto, 1.35f, AmarilloVena);

                // EL ZAP del aterrizaje + el retumbo.
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item15.WithPitchOffset(-0.2f).WithVolumeScale(0.7f), impacto);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(-0.3f).WithVolumeScale(0.9f), impacto);
            }

            // === LA CADENA del LightningRaid (3 cortos + 1 largo) ===
            var golpeados = new List<NPC>();
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if ((npc.Center - impacto).Length() < 130f)
                {
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, Projectile.damage, 4f, true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        try { npc.AddBuff(BuffID.Electrified, 300); } catch { }
                    golpeados.Add(npc);
                }
            }

            int cadenas = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (cadenas >= 3) break;
                if (!VFXCore.EsObjetivo(npc) || golpeados.Contains(npc)) continue;
                if ((npc.Center - impacto).Length() > 460f) continue;

                // El golpe de la cadena: el primero entero, los demás
                // decaen (el "1 largo + 3 cortos" del LightningRaid).
                float escala = cadenas == 0 ? 0.75f : 0.55f - 0.12f * cadenas;
                Content.Systems.GolpeMotor.Golpear(Projectile, npc,
                    (int)(Projectile.damage * escala), 2.5f, true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(BuffID.Electrified, 240); } catch { }

                // EL ARCO DE LA CADENA (el Arco de ArcSurge de la casa,
                // en AMARILLO VENA — el color del dragón).
                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                {
                    RayoSistema.Lanzar(RayoParams.Arco(impacto, npc.Center),
                        (uint)Main.rand.Next(1, int.MaxValue),
                        npc.Center, impacto - npc.Center,
                        AmarilloVena, NucleoVena,
                        ancho: 10f, vidaTicks: 18, AnimRayo.Arco,
                        luzEstable: false);
                }
                cadenas++;
            }
        }

        // INVISIBLE: el canal lo dibuja el RayoSistema (PostDrawTiles).
        public override bool PreDraw(ref Color lightColor) => false;
    }
}
