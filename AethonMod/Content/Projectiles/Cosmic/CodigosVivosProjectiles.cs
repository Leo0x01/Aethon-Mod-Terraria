using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    // ======================================================================
    //  v6.36 — LOS CUATRO CÓDIGOS VIVOS (uno por imagen del usuario).
    //
    //  Cada proyectil de este archivo ES la traducción C# del código de
    //  su imagen: la SIMULACIÓN vive aquí (la CARNE — el render — vive
    //  en CodigosLib, la librería de los códigos vivos).
    // ======================================================================

    /// <summary>
    /// DanzaOrbesProjectile — LA DANZA DE LOS ORBES (v6.36 — IMAGEN 1).
    ///
    /// EL PUERTO EXACTO DEL follow() DE LA IMAGEN 1 (JavaScript):
    /// <code>
    ///   follow(iter) {
    ///       var x = this.parent.x;
    ///       var y = this.parent.y;
    ///       var dist = ((this.x - x) ** 2 + (this.y - y) ** 2) ** 0.5;
    ///       this.x = x + this.size * (this.x - x) / dist;
    ///       this.y = y + this.size * (this.y - y) / dist;
    ///       this.absAngle = Math.atan2(this.y - y, this.x - x);
    ///       this.relAngle = this.absAngle - this.parent.absAngle;
    ///       this.updateRelative(false, true);
    ///       if (iter) {
    ///           for (var i = 0; i &lt; this.children.length; i++) {
    ///               this.children[i].follow(true);
    ///           }
    ///       }
    ///   }
    /// </code>
    /// La traducción es <see cref="Follow"/> línea por línea (esta misma
    /// ley es el ADN compartido con la sierpe de la IMAGEN 4 — en las
    /// demos originales el sistema solar y el pez usaban la misma
    /// cadena de distancia constante).
    ///
    /// MECÁNICA: invoca UN MINISISTEMA SOLAR que orbita al jugador 12 s
    /// — la RAÍZ (el sol dorado) cabalga su órbita alrededor del dueño y
    /// de él cuelgan DOS PLANETAS, cada uno con DOS LUNAS: cada hijo
    /// atado a su padre a SU distancia exacta (this.size) y avanzando
    /// su propia deriva angular. TODO orbe quema al que toca (sol ×1.0,
    /// planetas ×0.7, lunas ×0.5 — cada 10 ticks). Un sistema a la vez.
    /// </summary>
    public class DanzaOrbesProjectile : ModProjectile
    {
        private const int Vida = 720;

        /// <summary>Distancia de la RAÍZ al jugador (su "órbita").</summary>
        private const float DistRaiz = 96f;

        /// <summary>Avance angular de la raíz alrededor del dueño (rad/tick).</summary>
        private const float OmegaRaiz = 0.045f;

        /// <summary>Cadencia de la quemadura de los orbes.</summary>
        private const int CadaQuemadura = 10;

        /// <summary>El NODO de la cadena (el "this" del follow original).</summary>
        private struct Nodo
        {
            /// <summary>this.x, this.y — la posición en el mundo.</summary>
            public Vector2 Pos;

            /// <summary>this.size — la distancia EXACTA al padre.</summary>
            public float Size;

            /// <summary>this.absAngle — el ángulo padre→nodo.</summary>
            public float AbsAngle;

            /// <summary>this.relAngle — el ángulo contra el padre.</summary>
            public float RelAngle;

            /// <summary>La deriva angular propia (rad/tick — el avance).</summary>
            public float Omega;

            /// <summary>0 = sol (raíz), 1 = planeta, 2 = luna.</summary>
            public int Tipo;

            /// <summary>Índice del padre (la raíz se apunta a sí misma).</summary>
            public int Padre;

            /// <summary>Radio visual/de golpe del orbe (px).</summary>
            public float Radio;
        }

        /// <summary>LA TRADUCCIÓN LÍNEA A LÍNEA del follow() de la IMAGEN 1.</summary>
        private static void Follow(Nodo[] nodos, int i, bool iter)
        {
            ref Nodo nodo = ref nodos[i];
            int p = nodo.Padre;

            // var x = this.parent.x;  var y = this.parent.y;
            float px = nodos[p].Pos.X, py = nodos[p].Pos.Y;

            // var dist = ((this.x - x) ** 2 + (this.y - y) ** 2) ** 0.5;
            float dx = nodo.Pos.X - px, dy = nodo.Pos.Y - py;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            // El guard de la casa: un nodo nacido ENCIMA de su padre no
            // puede dividir por cero (el original reventaría con NaN).
            if (dist < 0.0001f) { dx = 1f; dy = 0f; dist = 1f; }

            // this.x = x + this.size * (this.x - x) / dist;
            // this.y = y + this.size * (this.y - y) / dist;
            nodo.Pos.X = px + nodo.Size * dx / dist;
            nodo.Pos.Y = py + nodo.Size * dy / dist;

            // this.absAngle = Math.atan2(this.y - y, this.x - x);
            nodo.AbsAngle = MathF.Atan2(nodo.Pos.Y - py, nodo.Pos.X - px);

            // this.relAngle = this.absAngle - this.parent.absAngle;
            nodo.RelAngle = nodo.AbsAngle - nodos[p].AbsAngle;

            // this.updateRelative(false, true);  → vive en CodigosLib
            // (la estela tangencial perpendicular al radio — el sprite
            // transform del original hecho luz).

            // if (iter) { for (i...) this.children[i].follow(true); }
            if (iter)
            {
                for (int c = 1; c < nodos.Length; c++)
                {
                    if (nodos[c].Padre == i && c != i)
                        Follow(nodos, c, true);
                }
            }
        }

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            // v6.35: SIN hide (la lección de los desgarros).
        }

        public override void OnSpawn(IEntitySource source)
        {
            Seed = (int)Projectile.ai[0] % 9973;

            // === LA CONSTRUCCIÓN DEL ÁRBOL (los children del original):
            //     [0] el SOL (raíz) · [1] planeta A · [2] luna A1 ·
            //     [3] luna A2 · [4] planeta B · [5] luna B1 · [6] luna B2.
            //     Las fases iniciales salen del HASH determinista — la
            //     MISMA semilla en todas las máquinas. ===
            _nodos = new Nodo[7];

            _nodos[0] = new Nodo
            {
                Tipo = 0,
                Padre = 0,
                Size = 0f,
                Radio = 13f,
                AbsAngle = 0f,
                RelAngle = 0f,
                Pos = Projectile.Center
            };

            // Los DOS planetas (colgados del sol).
            for (int b = 0; b < 2; b++)
            {
                int ip = b == 0 ? 1 : 4;               // 1, 4 — los planetas
                float faseP = VFXCore.Hash01(Seed, 100 + ip, 31) * MathHelper.TwoPi;

                _nodos[ip] = new Nodo
                {
                    Tipo = 1,
                    Padre = 0,
                    Size = 46f,
                    Radio = 9f,
                    Omega = (b == 0 ? 1f : -1f) * 0.030f,
                    Pos = _nodos[0].Pos + Dir(faseP) * 46f
                };

                // Las DOS lunas de cada planeta.
                for (int l = 0; l < 2; l++)
                {
                    int il = ip + 1 + l;               // 2,3 y 5,6 — las lunas
                    float faseL = VFXCore.Hash01(Seed, 200 + il, 37) * MathHelper.TwoPi;

                    _nodos[il] = new Nodo
                    {
                        Tipo = 2,
                        Padre = ip,
                        Size = 24f,
                        Radio = 6f,
                        Omega = (l == 0 ? 1f : -1f) * 0.055f,
                        Pos = _nodos[ip].Pos + Dir(faseL) * 24f
                    };
                }
            }
        }

        public override void AI()
        {
            _age++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            // === LA RAÍZ: su "padre" es el JUGADOR (en la demo original
            //     el sol seguía al puntero; aquí cabalga su órbita). ===
            _angRaiz += OmegaRaiz;
            _nodos[0].Pos = duenio.Center + Dir(_angRaiz) * DistRaiz;
            _nodos[0].AbsAngle = _angRaiz;
            _nodos[0].RelAngle = 0f;

            // === EL AVANCE DE LOS HIJOS: cada nodo deriva angularmente
            //     alrededor de su padre (el paso que en la demo vivía
            //     fuera del follow — el motor de la órbita). ===
            for (int i = 1; i < _nodos.Length; i++)
            {
                Vector2 offset = _nodos[i].Pos - _nodos[_nodos[i].Padre].Pos;
                _nodos[i].Pos = _nodos[_nodos[i].Padre].Pos + offset.RotatedBy(_nodos[i].Omega);
            }

            // === Y LA CADENA follow() MANTIENE LAS DISTANCIAS EXACTAS
            //     (el puerto de la IMAGEN 1, recursivo como el original). ===
            for (int i = 1; i < _nodos.Length; i++)
            {
                if (_nodos[i].Padre == 0)
                    Follow(_nodos, i, true);
            }

            Projectile.Center = _nodos[0].Pos;
            Projectile.velocity = Vector2.Zero;

            // La luz del sol de la danza (dorado suave).
            Lighting.AddLight(Projectile.Center, 0.35f, 0.26f, 0.10f);

            // === LA QUEMADURA DE LOS ORBES (cada 10 ticks) —
            //     v6.50 — GolpeMotor (el cauce del motor). ===
            if (_age > 12 && _age % CadaQuemadura == 0)
            {
                int dmg = Math.Max(1, Projectile.damage);
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;

                    for (int i = 0; i < _nodos.Length; i++)
                    {
                        float mult = _nodos[i].Tipo == 0 ? 1f :
                                     _nodos[i].Tipo == 1 ? 0.7f : 0.5f;
                        float r = _nodos[i].Radio + 10f;
                        if (Vector2.DistanceSquared(npc.Center, _nodos[i].Pos) > r * r)
                            continue;

                        Content.Systems.GolpeMotor.Golpear(Projectile, npc,
                            (int)(dmg * mult), 2f, true);
                        break;   // un orbe por enemigo en este tick de cadencia
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // Los buffers paralelos del contrato de CodigosLib (cero GC).
            for (int i = 0; i < _nodos.Length; i++)
            {
                _pos[i] = _nodos[i].Pos;
                _radioScr[i] = _nodos[i].Radio;
                _angScr[i] = _nodos[i].AbsAngle;
            }

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                CodigosLib.DanzaOrbes(_pos, _radioScr, _angScr, _tipoScr, _padreScr,
                    _nodos.Length, Main.GlobalTimeWrappedHourly, Seed, AlphaDeVida());
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);
            return false;
        }

        /// <summary>El fundido de entrada/salida del sistema.</summary>
        private float AlphaDeVida()
        {
            const int Funda = 20;
            if (_age < Funda) return _age / (float)Funda;
            if (Projectile.timeLeft < Funda) return Math.Max(0f, Projectile.timeLeft / (float)Funda);
            return 1f;
        }

        /// <summary>Dirección unitaria del ángulo (helper local).</summary>
        private static Vector2 Dir(float ang) =>
            new Vector2(MathF.Cos(ang), MathF.Sin(ang));

        private Nodo[] _nodos = new Nodo[7];
        private float _angRaiz;
        private float _age;

        private int Seed { get; set; }

        // --- Los buffers paralelos del render (el contrato de CodigosLib;
        //     los tipos y padres del árbol son constantes de nacimiento). ---
        private readonly Vector2[] _pos = new Vector2[7];
        private readonly float[] _radioScr = new float[7];
        private readonly float[] _angScr = new float[7];
        private readonly int[] _tipoScr = { 0, 1, 2, 2, 1, 2, 2 };
        private readonly int[] _padreScr = { 0, 0, 1, 1, 0, 4, 4 };
    }

    /// <summary>
    /// LenteAbismoProjectile — LA LENTE DEL ABISMO (v6.36 — IMAGEN 2).
    ///
    /// EL PUERTO DEL animate() DEL AGUJERO NEGRO DE LA IMAGEN 2
    /// (THREE.js):
    /// <code>
    ///   function animate() {
    ///       requestAnimationFrame(animate);
    ///       const elapsedTime = clock.getElapsedTime();
    ///       diskMaterial.uniforms.uTime.value = elapsedTime;
    ///       starMaterial.uniforms.uTime.value = elapsedTime;
    ///       eventHorizonMat.uniforms.uTime.value = elapsedTime;
    ///       eventHorizonMat.uniforms.uCameraPosition.value.copy(camera.position);
    ///       blackHoleScreenPosVec3.copy(blackHoleMesh.position).project(camera);
    ///       lensingPass.uniforms.blackHoleScreenPos.value.set(
    ///           (blackHoleScreenPosVec3.x + 1) / 2,
    ///           (blackHoleScreenPosVec3.y + 1) / 2
    ///       );
    ///   }
    /// </code>
    /// El uTime que alimentaba los tres materiales ES el reloj que hace
    /// vivir TODO el render (CodigosLib.OjoAbismo); el
    /// blackHoleScreenPos proyectado a pantalla que alimentaba la
    /// lensingPass ES GravLensRegistrada cada tick con la posición
    /// mundial→pantalla del vórtice (la lente de pantalla de la casa);
    /// y el uCameraPosition del horizonte es la dependencia de cámara
    /// que aquí vive como la respiración del núcleo contra el reloj.
    ///
    /// MECÁNICA: planta el ojo en el cursor 6 s — LA LENTE curva el
    /// fondo a su alrededor (la más fuerte del arsenal), LA SUCCIÓN
    /// arrastra en 300 px (los jefes no se dejan), el que cruza el
    /// HORIZONTE DE SUCESOS es devorado (×1.2 cada 12 ticks) y cada
    /// 90 ticks LA LENTE ENFOCA: la luz doblada Muerde al enemigo más
    /// cercano (×0.6).
    /// </summary>
    public class LenteAbismoProjectile : ModProjectile
    {
        private const int Apertura = 24;
        private const int Cierre = 36;
        private const int Vida = 360;

        /// <summary>Radio del vórtice (px — el anillo llega a ~1.9×).</summary>
        private const float Radio = 64f;

        /// <summary>Radio del horizonte de sucesos (la boca).</summary>
        private const float RadioNucleo = 38f;

        /// <summary>Radio de la succión.</summary>
        private const float RadioSucion = 300f;

        /// <summary>Fuerza de la succión (px/tick²).</summary>
        private const float Traccion = 0.22f;

        /// <summary>Fuerza de LA LENTE (la más fuerte del arsenal).</summary>
        private const float FuerzaLente = 0.62f;

        /// <summary>Cadencia del devorar del horizonte.</summary>
        private const int CadaDevora = 12;

        /// <summary>Daño relativo del devorar.</summary>
        private const float DañoDevora = 1.2f;

        /// <summary>Cadencia del mordisco de la lente.</summary>
        private const int CadaMordida = 90;

        /// <summary>Daño relativo del mordisco de la lente.</summary>
        private const float DañoMordida = 0.6f;

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
            // v6.35: SIN hide (la lección de los desgarros).
        }

        public override void AI()
        {
            _age++;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = _ancla;

            // La luz del abismo (magenta con alma violeta).
            Lighting.AddLight(Projectile.Center, 0.42f, 0.06f, 0.30f);

            // === LA LENSINGPASS DEL ORIGINAL: el fondo se curva alrededor
            //     del agujero proyectado a coordenadas de pantalla — la
            //     lente de la casa, re-registrada cada tick (el patrón del
            //     portal estable). Fuerza 0.62: LA MÁS FUERTE del arsenal. ===
            GravLens.Registrar(Projectile.Center, Radio * 2.4f, FuerzaLente, 0.10f);

            // v6.50 — el devorar y la mordida de la lente van por GolpeMotor
            // (el cauce del motor: crítica real, varianza, on-hit y sync MP
            // del propio motor); la succión sigue siendo lógica de servidor.
            if (_age > Apertura)
            {
                // === LA SUCCIÓN (la materia cae al pozo) — servidor ===
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        if (npc.boss) continue;
                        Vector2 alCentro = Projectile.Center - npc.Center;
                        float d = alCentro.Length();
                        if (d > RadioSucion || d < 8f) continue;
                        npc.velocity += Vector2.Normalize(alCentro) * Traccion * (1f - d / RadioSucion + 0.5f);
                    }
                }

                // === EL DEVORAR DEL HORIZONTE DE SUCESOS (cada 12 ticks) ===
                if ((_age - Apertura) % CadaDevora == 0)
                {
                    int dmg = Math.Max(1, (int)(Projectile.damage * DañoDevora));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > RadioNucleo * RadioNucleo) continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 0f, true);   // kb 0: devorado
                    }
                }

                // === LA LENTE ENFOCA (cada 90 ticks): la luz doblada
                //     MUERDE al enemigo más cercano en 420 px. ===
                if ((_age - Apertura) % CadaMordida == 0)
                {
                    NPC mejor = null; float mejorD = float.MaxValue;
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        if (_golpeados.Contains(npc.whoAmI)) continue;
                        float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                        if (d < 420f * 420f && d < mejorD) { mejorD = d; mejor = npc; }
                    }
                    if (mejor != null)
                    {
                        _golpeados.Add(mejor.whoAmI);
                        if (_golpeados.Count > 8) _golpeados.Clear();
                        _mordidas.Add((mejor.Center, 10));
                        Content.Systems.GolpeMotor.Golpear(Projectile, mejor,
                            Math.Max(1, (int)(Projectile.damage * DañoMordida)), 0f, true);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                // EL OJO (los tres materiales del original esclavos del uTime).
                CodigosLib.OjoAbismo(Projectile.Center, Radio, Progress(),
                    Main.GlobalTimeWrappedHourly, Seed);

                // LAS MORDIDAS VIVAS (la lente enfocando — 10 ticks de destello).
                if (_mordidas.Count > 0)
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    for (int i = _mordidas.Count - 1; i >= 0; i--)
                    {
                        var (target, vida) = _mordidas[i];
                        if (vida <= 0) { _mordidas.RemoveAt(i); continue; }
                        _mordidas[i] = (target, vida - 1);
                        float t = vida / 10f;
                        StormLib.SparkBurst(Main.spriteBatch,
                            target - Main.screenPosition, new Color(255, 150, 240),
                            26f, 4, Seed + 61 + i, 1f - t, 30f);
                    }
                    Main.spriteBatch.End();
                }
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);
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
        private readonly List<(Vector2 target, int vida)> _mordidas = new();
        private readonly HashSet<int> _golpeados = new();

        private int Seed { get; set; }
    }

    /// <summary>
    /// SolVivoProjectile — EL SOL VIVO (v6.36 — IMAGEN 3).
    ///
    /// EL PUERTO DEL animate() DEL SOL DE LA IMAGEN 3 (THREE.js):
    /// <code>
    ///   const clock = new THREE.Clock();
    ///   function animate() {
    ///       requestAnimationFrame(animate);
    ///       const delta = clock.getDelta();
    ///       const time = clock.getElapsedTime();
    ///       starMaterial.uniforms.time.value = time;
    ///       shellMaterial.uniforms.time.value = time;
    ///       diskMat.uniforms.time.value = time;
    ///       emberMat.uniforms.time.value = time;
    ///       ringMat.uniforms.time.value = time;
    ///       prominenceMat.uniforms.time.value = time;
    ///       const pulse = 0.5 + 0.5 * Math.sin(time * 2.15);
    ///       bloomPass.strength = 0.8 + 0.4 * pulse;
    ///       coreGroup.rotation.y += delta * 0.05;
    ///       controls.update();
    ///       composer.render();
    ///   }
    /// </code>
    /// EL PULSO EXACTO (0.5 + 0.5·sin(time·2.15)) y EL BLOOM EXACTO
    /// (0.8 + 0.4·pulse) laten AQUÍ: en el render (CodigosLib.SolVivo)
    /// Y en la LUZ del mundo — la MISMA fórmula, el mismo reloj. Los
    /// seis materiales (star/shell/disk/ember/ring/prominence) son las
    /// seis capas del compositor de la casa; el coreGroup girando a
    /// 0.05 rad/s es la rotación del núcleo.
    ///
    /// MECÁNICA: enciende el sol donde apuntas 5 s — TODO enemigo
    /// dentro del aura (130 px) se quema (×0.55 cada 15 ticks), en cada
    /// PICO del pulso el sol LATE: una onda quema a 170 px (×0.7) y
    /// cada 45 ticks las PROMINENCIAS azotan a los 2 enemigos más
    /// cercanos (×0.85).
    /// </summary>
    public class SolVivoProjectile : ModProjectile
    {
        private const int Apertura = 20;
        private const int Cierre = 30;
        private const int Vida = 300;

        /// <summary>Radio del núcleo solar (px).</summary>
        private const float RadioNucleo = 58f;

        /// <summary>Radio del aura de quemadura.</summary>
        private const float RadioAura = 130f;

        /// <summary>Radio del latido del pico.</summary>
        private const float RadioLatido = 170f;

        /// <summary>Cadencia de la quemadura del aura.</summary>
        private const int CadaQuemadura = 15;

        /// <summary>Daño relativo de la quemadura.</summary>
        private const float DañoQuemadura = 0.55f;

        /// <summary>Daño relativo del latido del pico.</summary>
        private const float DañoLatido = 0.7f;

        /// <summary>Cadencia de las prominencias.</summary>
        private const int CadaLengua = 45;

        /// <summary>Daño relativo de las prominencias.</summary>
        private const float DañoLengua = 0.85f;

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
            // v6.35: SIN hide (la lección de los desgarros).
        }

        public override void AI()
        {
            _age++;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = _ancla;

            float time = Main.GlobalTimeWrappedHourly;

            // === LA LUZ CON EL BLOOM EXACTO DEL ORIGINAL:
            //     bloomPass.strength = 0.8 + 0.4 * pulse → 0.8..1.2. ===
            float pulse = 0.5f + 0.5f * MathF.Sin(time * CodigosLib.FrecuenciaPulso);
            float bloom = 0.8f + 0.4f * pulse;
            Lighting.AddLight(Projectile.Center,
                0.55f * bloom, 0.33f * bloom, 0.11f * bloom);

            // v6.50 — la quemadura, el latido y las lenguas van por GolpeMotor
            // (el cauce del motor: crítica real, varianza, on-hit y sync MP
            // del propio motor).
            if (_age > Apertura && _age < Vida - Cierre)
            {
                // === LA QUEMADURA DEL AURA (cada 15 ticks) ===
                if ((_age - Apertura) % CadaQuemadura == 0)
                {
                    int dmg = Math.Max(1, (int)(Projectile.damage * DañoQuemadura));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > RadioAura * RadioAura) continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 2f, true);
                    }
                }

                // === EL LATIDO DEL PICO: cuando el pulso del original
                //     corona (cos cruza cero bajando), el sol LATE — una
                //     ola quema a 170 px. ===
                float c = MathF.Cos(time * CodigosLib.FrecuenciaPulso);
                if (_prevCos > 0f && c <= 0f)
                {
                    int dmg = Math.Max(1, (int)(Projectile.damage * DañoLatido));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > RadioLatido * RadioLatido) continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 3f, true);
                    }
                }
                _prevCos = c;

                // === LAS PROMINENCIAS AZOTAN (cada 45 ticks — los 2 más
                //     cercanos, como las llamaradas del corazón v6.35). ===
                if ((_age - Apertura) % CadaLengua == 0)
                    AzotarLenguas();
            }
        }

        /// <summary>Las lenguas: los 2 EsObjetivo más cercanos en 500 px.</summary>
        private void AzotarLenguas()
        {
            int dmg = Math.Max(1, (int)(Projectile.damage * DañoLengua));
            int quedan = 2;
            for (int paso = 0; paso < 2 && quedan > 0; paso++)
            {
                NPC mejor = null; float mejorD = float.MaxValue;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    if (_golpeados.Contains(npc.whoAmI)) continue;
                    float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                    if (d < 500f * 500f && d < mejorD) { mejorD = d; mejor = npc; }
                }
                if (mejor == null) break;
                _golpeados.Add(mejor.whoAmI);
                _lenguas.Add((mejor.Center, 12));
                Content.Systems.GolpeMotor.Golpear(Projectile, mejor, dmg, 2f, true);
                quedan--;
            }
            if (_golpeados.Count > 16) _golpeados.Clear();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                float alpha = Progress();

                // EL SOL (los seis materiales del original, el pulso exacto).
                CodigosLib.SolVivo(Projectile.Center, RadioNucleo,
                    Main.GlobalTimeWrappedHourly, Seed, alpha);

                // LAS LENGUAS VIVAS (las prominencias que acaban de azotar —
                // 12 ticks): fuego de verdad del borde al enemigo.
                if (_lenguas.Count > 0)
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    for (int i = _lenguas.Count - 1; i >= 0; i--)
                    {
                        var (target, vida) = _lenguas[i];
                        if (vida <= 0) { _lenguas.RemoveAt(i); continue; }
                        _lenguas[i] = (target, vida - 1);
                        float t = vida / 12f;

                        Vector2 dir = target - Projectile.Center;
                        float dist = dir.Length();
                        if (dist < 1f) continue;
                        dir /= dist;
                        if (float.IsNaN(dir.X) || float.IsNaN(dir.Y)) continue;

                        Vector2 basePos = Projectile.Center - Main.screenPosition +
                            dir * (RadioNucleo * 0.85f);
                        PyraLib.Tongue(Main.spriteBatch, basePos, dist * 0.85f,
                            RadioNucleo * 0.22f, PyraPalettes.SolarFire, 0.80f,
                            Seed + 90 + i, Main.GlobalTimeWrappedHourly,
                            0.90f * t, rot: dir.ToRotation() + MathHelper.PiOver2);
                    }
                    Main.spriteBatch.End();
                }
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);
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
        private float _prevCos = 1f;
        private readonly List<(Vector2 target, int vida)> _lenguas = new();
        private readonly HashSet<int> _golpeados = new();

        private int Seed { get; set; }
    }

    /// <summary>
    /// SierpeEstelarProjectile — LA SIERPE ESTELAR (v6.36 — IMAGEN 4).
    ///
    /// EL PUERTO DE LOS elems DE LA IMAGEN 4 (JavaScript):
    /// <code>
    ///   const elems = [];
    ///   for (let i = 0; i &lt; N; i++) elems[i] = { use: null, x: width / 2, y: 0 };
    ///   const pointer = { x: width / 2, y: height / 2 };
    ///   const radm = Math.min(pointer.x, pointer.y) - 20;
    ///   let frm = Math.random();
    ///   let rad = 0;
    ///
    ///   for (let i = 1; i &lt; N; i++) {
    ///     if (i === 1) prepend("Cabeza", i);
    ///     else if (i === 8 || i === 14) prepend("Aletas", i);
    ///     else prepend("Espina", i);
    ///   }
    /// </code>
    /// La JERARQUÍA EXACTA del prepend (traducida a 0-based: la i==1 →
    /// CABEZA en el índice 0, las i==8 e i==14 → ALETAS en los índices
    /// 7 y 13, el resto → ESPINA) y el COMPORTAMIENTO: el puntero
    /// alrededor del cual nada (pointer) con su radio de vuelta (radm)
    /// y el vaivén del nado (frm/rad). La CADENA que ata cada segmento
    /// al anterior es la MISMA LEY de la IMAGEN 1 (el follow de
    /// distancia constante — el ADN compartido de ambas demos).
    ///
    /// MECÁNICA: libera la sierpe 8 s — la CABEZA nada en círculos
    /// alrededor del cursor (radm = 110 px, vaivén de velocidad) y la
    /// ESPINA la sigue eslabón a eslabón. La cabeza ATRAVIESA todo lo
    /// que toca y la espina quema a su paso (×0.4 cada 8 ticks). Una
    /// sierpe a la vez.
    /// </summary>
    public class SierpeEstelarProjectile : ModProjectile
    {
        /// <summary>El N del original: 16 segmentos.</summary>
        private const int N = 16;

        private const int Vida = 480;

        /// <summary>El radm del original: radio de la vuelta alrededor del puntero.</summary>
        private const float Radm = 110f;

        /// <summary>Velocidad de nado base (px/tick).</summary>
        private const float Velocidad = 11f;

        /// <summary>La amplitud del vaivén (el frm/rad del original).</summary>
        private const float Vaiven = 0.35f;

        /// <summary>La distancia EXACTA entre eslabones (el this.size de la cadena).</summary>
        private const float TamanoSeg = 18f;

        /// <summary>Cadencia de la quemadura de la espina.</summary>
        private const int CadaEspina = 8;

        /// <summary>Daño relativo de la espina.</summary>
        private const float DañoEspina = 0.4f;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            // v6.35: SIN hide (la lección de los desgarros).
        }

        public override void OnSpawn(IEntitySource source)
        {
            // ai[] SOLO TIENE 3 SLOTS (la lección v6.27 del péndulo):
            // ai[0..1] = EL PUNTERO (x,y) · ai[2] = la semilla.
            // El rumbo inicial llega por la VELOCIDAD de nacimiento (que
            // viaja sola en el paquete del proyectil).
            Seed = (int)Projectile.ai[2] % 9973;
            _rumbo = Projectile.velocity.LengthSquared() > 0.01f
                ? Projectile.velocity.ToRotation()
                : 0f;

            // === EL INIT DEL ORIGINAL (elems[i] = { x: width/2, y: 0 }):
            //     los segmentos nacen APILADOS tras la cabeza y la cadena
            //     los despliega en el primer latido. ===
            Vector2 atras = new Vector2(MathF.Cos(_rumbo + MathHelper.Pi),
                MathF.Sin(_rumbo + MathHelper.Pi));
            for (int i = 0; i < N; i++)
            {
                _segmentos[i] = Projectile.Center + atras * (i * 2f);
                _angulos[i] = _rumbo;
            }

            // El puntero inicial (viaja en ai[0..1] desde el disparo).
            if (Projectile.ai[0] == 0f && Projectile.ai[1] == 0f)
            {
                Projectile.ai[0] = Projectile.Center.X;
                Projectile.ai[1] = Projectile.Center.Y;
            }
        }

        public override void AI()
        {
            _age++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            float time = Main.GlobalTimeWrappedHourly;

            // === EL PUNTERO (el pointer del original): el dueño local
            //     manda — la velocidad se sincroniza sola (patrón de la
            //     congregación v6.29); el objetivo viaja en ai[0..1]. ===
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 puntero = Main.MouseWorld;

                // La correa de la casa: la sierpe no abandona a su dueño
                // (760 px, el alcance de la congregación).
                Vector2 alDueño = puntero - duenio.Center;
                if (alDueño.Length() > 760f)
                    puntero = duenio.Center + Vector2.Normalize(alDueño) * 760f;

                if (Vector2.DistanceSquared(puntero,
                    new Vector2(Projectile.ai[0], Projectile.ai[1])) > 24f * 24f ||
                    _age % 30 == 0)
                {
                    Projectile.ai[0] = puntero.X;
                    Projectile.ai[1] = puntero.Y;
                    Projectile.netUpdate = true;
                }
            }
            Vector2 objetivo = new Vector2(Projectile.ai[0], Projectile.ai[1]);

            // === EL NADO (el radm del original): la cabeza persigue el
            //     punto TANGENTE del círculo alrededor del puntero — el
            //     pez nada en VUELTAS, no embiste; lejos del círculo
            //     apunta directo para acercarse. ===
            Vector2 alPuntero = _segmentos[0] - objetivo;
            float distP = alPuntero.Length();
            float deseado;
            if (distP > Radm * 2.4f && distP > 1f)
                deseado = (objetivo - _segmentos[0]).ToRotation();
            else if (distP > 1f)
                deseado = alPuntero.ToRotation() + MathHelper.PiOver2;
            else
                deseado = _rumbo;

            float delta = MathHelper.WrapAngle(deseado - _rumbo);
            _rumbo += MathHelper.Clamp(delta, -0.07f, 0.07f);

            // === EL VAIVÉN (el frm/rad del original): la velocidad
            //     respira — el pez empuja con la cola. ===
            float surge = 1f + Vaiven * MathF.Sin(time * MathHelper.TwoPi * 0.9f + Seed);
            Vector2 vel = new Vector2(MathF.Cos(_rumbo), MathF.Sin(_rumbo)) *
                (Velocidad * surge);
            _segmentos[0] += vel;
            _angulos[0] = _rumbo;

            // === LA CADENA (la MISMA LEY de la IMAGEN 1 — el follow de
            //     distancia constante, eslabón a eslabón):
            //     seg[i] = seg[i-1] + normalize(seg[i] - seg[i-1]) * size. ===
            for (int i = 1; i < N; i++)
            {
                Vector2 d = _segmentos[i] - _segmentos[i - 1];
                float dist = d.Length();
                if (dist < 0.0001f) { d = new Vector2(1f, 0f); dist = 1f; }   // guard NaN
                _segmentos[i] = _segmentos[i - 1] + d / dist * TamanoSeg;
                _angulos[i] = MathF.Atan2(d.Y, d.X);
            }

            // El hitbox y el rumbo viajan con la cabeza.
            Projectile.Center = _segmentos[0];
            Projectile.velocity = vel;
            Projectile.rotation = _rumbo;

            // La luz fría-estelar de la sierpe.
            Lighting.AddLight(Projectile.Center, 0.18f, 0.18f, 0.12f);

            // === LA ESPINA QUEMA (cada 8 ticks — la cabeza golpea sola
            //     por colisión del motor, como el leviatán v6.35).
            //     v6.50 — GolpeMotor (el cauce del motor). ===
            if (_age > 10 && _age % CadaEspina == 0)
            {
                int dmg = Math.Max(1, (int)(Projectile.damage * DañoEspina));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;

                    for (int i = 1; i < N; i++)
                    {
                        float r = MathHelper.Lerp(15f, 5f, i / (float)(N - 1)) + 8f;
                        if (Vector2.DistanceSquared(npc.Center, _segmentos[i]) > r * r)
                            continue;

                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.5f, true);
                        break;   // un eslabón por enemigo en este tick
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                // EL FADE de nacimiento (la sierpe se despliega) y de cierre.
                const int Funda = 24;
                float alpha = 1f;
                if (_age < Funda) alpha = _age / (float)Funda;
                else if (Projectile.timeLeft < Funda) alpha = Math.Max(0f, Projectile.timeLeft / (float)Funda);

                CodigosLib.SierpeEstelar(_segmentos, _angulos, N,
                    Main.GlobalTimeWrappedHourly, Seed, alpha);
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);
            return false;
        }

        private readonly Vector2[] _segmentos = new Vector2[N];
        private readonly float[] _angulos = new float[N];
        private float _rumbo;
        private float _age;

        private int Seed { get; set; }
    }
}
