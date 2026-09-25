using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// DesgarroCuanticoProjectile — LA SUTURA CUÁNTICA (v6.33).
    ///
    /// EL DESGARRO CIRCULAR GLITCH (la referencia "desgarro de realidad
    /// cuántica"): 300 ticks de vida en el punto del cursor — 30 de
    /// APERTURA (el círculo nace irregular), 230 de CORRUPCIÓN viva y 40
    /// de IMPLOSIÓN (el cierre). El daño vive en el círculo: cada 20 ticks
    /// TODO enemigo dentro del radio es corrompido (×1.0) y cada 45 ticks
    /// el borde DISPARA RAYOS CUÁNTICOS a los 3 enemigos más cercanos
    /// (×0.6 — el código escapando de la realidad rota).
    /// </summary>
    public class DesgarroCuanticoProjectile : ModProjectile
    {
        private const int Apertura = 30;
        private const int Cierre = 40;
        private const int Vida = 300;

        /// <summary>Radio del desgarro (px).</summary>
        private const float Radio = 130f;

        /// <summary>i-frames de la corrupción del círculo.</summary>
        private const int Iframes = 20;

        /// <summary>Cadencia de los RAYOS CUÁNTICOS.</summary>
        private const int CadaRayos = 45;

        /// <summary>Daño relativo de los rayos (el círculo pega ×1.0).</summary>
        private const float DañoRayo = 0.6f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = (int)(Radio * 2);
            Projectile.height = (int)(Radio * 2);
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            // v6.35: SIN hide — el bucle DrawProjectiles de tML SALTA los
            // proyectivos ocultos ("!projectile[i].hide" en Main.cs) y PreDraw
            // jamás se llamaba: EL DESGARRO ERA INVISIBLE. Sin hide entra al
            // pase normal y PreDraw (que retorna false) pinta todo el VFX.
        }

        public override void AI()
        {
            _age++;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = _ancla;

            // La luz de la corrupción (cian-violeta inestable).
            Lighting.AddLight(Projectile.Center, 0.35f, 0.55f, 0.85f);

            // v6.50 — GolpeMotor (el cauce del motor: crítica real, varianza,
            // on-hit y sync MP del propio motor).
            // === LA CORRUPCIÓN DEL CÍRCULO (cada 20 ticks) ===
            if (_age > Apertura && _age < Vida - Cierre && (_age - Apertura) % Iframes == 0)
                GolpearCirculo(1f, 3f);

            // === LOS RAYOS CUÁNTICOS (cada 45 ticks) ===
            if (_age > Apertura + 10 && (_age - Apertura) % CadaRayos == 0)
                DispararRayos();
        }

        /// <summary>La corrupción: TODO EsObjetivo dentro del radio.
        /// v6.50 — GolpeMotor (el cauce del motor).</summary>
        private void GolpearCirculo(float factor, float knockback)
        {
            int dmg = Math.Max(1, (int)(Projectile.damage * factor));
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > Radio * Radio) continue;
                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, knockback, true);
            }
        }

        /// <summary>LOS RAYOS: los 3 EsObjetivo más cercanos en 400 px.</summary>
        private void DispararRayos()
        {
            int dmg = Math.Max(1, (int)(Projectile.damage * DañoRayo));
            int quedan = 3;
            for (int paso = 0; paso < 3 && quedan > 0; paso++)
            {
                NPC mejor = null; float mejorD = float.MaxValue;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    if (_golpeados.Contains(npc.whoAmI)) continue;
                    float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                    if (d < 400f * 400f && d < mejorD) { mejorD = d; mejor = npc; }
                }
                if (mejor == null) break;
                _golpeados.Add(mejor.whoAmI);
                // El rayo pega y se recuerda 8 ticks para el visual.
                _rayos.Add((mejor.Center, 8));
                Content.Systems.GolpeMotor.Golpear(Projectile, mejor, dmg, 2f, true);
                quedan--;
            }
            if (_golpeados.Count > 24) _golpeados.Clear();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                float progress = Progress();
                Vector2 center = Projectile.Center - Main.screenPosition;
                float time = Main.GlobalTimeWrappedHourly;

                // EL DESGARRO GLITCH (la primitiva v6.33 — círculo fragmentado).
                RiftLib.DesgarroGlitch(center, Radio, progress, time, Seed);

                // LOS RAYOS VIVOS (los que acaban de dispararse — 8 ticks).
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = _rayos.Count - 1; i >= 0; i--)
                {
                    var (target, vida) = _rayos[i];
                    if (vida <= 0) { _rayos.RemoveAt(i); continue; }
                    _rayos[i] = (target, vida - 1);
                    Vector2 dir = Vector2.Normalize(target - Projectile.Center);
                    Vector2 bord = Projectile.Center - Main.screenPosition + dir * (Radio * 0.8f);
                    StormLib.Bolt(Main.spriteBatch, bord,
                        target - Main.screenPosition, Seed + i * 31, (int)(time * 21f),
                        5f, new Color(0, 229, 255), Color.White,
                        0.55f * (vida / 8f), 5, 11f);
                }
                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>El progress 0→1→0 de la vida del desgarro.</summary>
        private float Progress()
        {
            if (_age < Apertura) return _age / (float)Apertura;
            if (_age > Vida - Cierre) return Math.Max(0f, (Vida - _age) / (float)Cierre);
            return 1f;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _ancla = Projectile.Center;
            Seed = (int)Projectile.ai[0] % 9973;
        }

        private Vector2 _ancla;
        private float _age;
        private readonly System.Collections.Generic.List<(Vector2 target, int vida)> _rayos = new();
        private readonly System.Collections.Generic.HashSet<int> _golpeados = new();

        private int Seed { get; set; }
    }

    /// <summary>
    /// PortalDimensionalProjectile — EL PORTAL DIMENSIONAL (v6.33).
    ///
    /// LA PUERTA DE ANILLOS (la referencia "se abre portal dimensional"):
    /// 360 ticks — 36 de APERTURA (los anillos nacen del centro, el
    /// exterior primero), 288 de SUCCIÓN y TRITURADO y 36 de cierre.
    /// La succión tira de los enemigos en 300 px hacia el centro (la
    /// puerta los quiere cruzar) y el NÚCLEO BLANCO los TRITURA cada 18
    /// ticks (radio 60 — la otra dimensión muerde).
    /// </summary>
    public class PortalDimensionalProjectile : ModProjectile
    {
        private const int Apertura = 36;
        private const int Cierre = 36;
        private const int Vida = 360;

        /// <summary>Radio del portal (px).</summary>
        private const float Radio = 150f;

        /// <summary>Radio del NÚCLEO TRITURADOR.</summary>
        private const float RadioNucleo = 60f;

        /// <summary>Radio de la succión.</summary>
        private const float RadioSucion = 300f;

        /// <summary>Fuerza de la succión (px/tick² — suave: es una puerta).</summary>
        private const float Traccion = 0.16f;

        /// <summary>Cadencia de la trituración del núcleo.</summary>
        private const int CadaTritura = 18;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = (int)(RadioNucleo * 2);
            Projectile.height = (int)(RadioNucleo * 2);
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            // v6.35: SIN hide — el bucle DrawProjectiles de tML SALTA los
            // proyectivos ocultos ("!projectile[i].hide" en Main.cs) y PreDraw
            // jamás se llamaba: EL DESGARRO ERA INVISIBLE. Sin hide entra al
            // pase normal y PreDraw (que retorna false) pinta todo el VFX.
        }

        public override void AI()
        {
            _age++;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = _ancla;

            Lighting.AddLight(Projectile.Center, 0.30f, 0.50f, 0.75f);

            // v6.50 — la trituración del núcleo va por GolpeMotor (el cauce
            // del motor); la succión sigue siendo lógica de servidor.
            if (_age > Apertura)
            {
                // === LA SUCCIÓN (la puerta tira de EsObjetivo en 300 px) — servidor ===
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        if (npc.boss) continue;                     // los jefes no se dejan arrastrar
                        Vector2 alCentro = Projectile.Center - npc.Center;
                        float d = alCentro.Length();
                        if (d > RadioSucion || d < 8f) continue;
                        npc.velocity += Vector2.Normalize(alCentro) * Traccion * (1f - d / RadioSucion + 0.4f);
                    }
                }

                // === LA TRITURACIÓN DEL NÚCLEO (cada 18 ticks) ===
                if ((_age - Apertura) % CadaTritura == 0)
                {
                    int dmg = Math.Max(1, Projectile.damage);
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > RadioNucleo * RadioNucleo) continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.5f, true);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                float progress = Progress();
                RiftLib.PortalAnillos(Projectile.Center - Main.screenPosition,
                    Radio, progress, Main.GlobalTimeWrappedHourly, Seed);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private float Progress()
        {
            if (_age < Apertura) return _age / (float)Apertura;
            if (_age > Vida - Cierre) return Math.Max(0f, (Vida - _age) / (float)Cierre);
            return 1f;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _ancla = Projectile.Center;
            Seed = (int)Projectile.ai[0] % 9973;
        }

        private Vector2 _ancla;
        private float _age;

        private int Seed { get; set; }
    }
}
