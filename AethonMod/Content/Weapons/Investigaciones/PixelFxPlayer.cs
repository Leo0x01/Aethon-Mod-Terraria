using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    //  v6.100 — EL MOTOR DE PÍXELES, LIBERADO DEL TALLER.
    //
    //  El Taller de Efectos murió (la letra del usuario: las armas de la
    //  bolsa del taller y las de las tres casas SE BORRAN) pero su MOTOR
    //  se quedó: este reproductor es la puerta de entrada del motor de
    //  píxeles de la casa (PixelFxCore + PixelFxCasas) a CUALQUIER
    //  proyectil del mod. Lo usan:
    //
    //   · EL SELLO DEL ESCRIBA   (clicker #3 — NovaActo: la tinta estalla)
    //   · EL PARPADEO DEL ETERNO (clicker #4 — GargantaVortice: el ojo)
    //   · LA CORONA DE PLUMAS    (aura #1 — el aro de plumas vivas)
    //   · EL DERRAME DE TINTA    (aura #2 — el charco que hierve)
    //   · LA ÚLTIMA PALABRA      (la definitiva — el acto entero)
    //   · LA SOMBRA DE LA PÁGINA IV (la copia forjada del original)
    //   · EL ESTANDARTE DEL JUICIO (CorazonCorona: el aura del juicio)
    //
    //  CÓMO SE DIBUJA (la lección del taller, intacta): cada proyectil
    //  vivo tiene un PixelFxPlayer — el efecto corre a 60 fps sobre su
    //  buffer, el compositor del motor junta color+luz en RGBA y un
    //  Texture2D.SetData por frame lo pone en el SpriteBatch AMBIENTE de
    //  PreDraw (AlphaBlend). Buffers y textura se liberan al morir el
    //  proyectil (OnKill → Dispose). CERO polvos encima: el efecto ES
    //  el efecto.
    // ======================================================================

    /// <summary>EL REPRODUCTOR por proyectil: instancia el efecto del motor de píxel
    /// de la casa sobre su frame, lo corre a 60 fps y lo vuelca a una Texture2D por
    /// frame. ES el «árbol de render» del motor metido en Terraria: los dos buffers
    /// del frame → Composite (el compositor del motor, con la premultiplicación de
    /// la luz sobre transparencia) → SetData → SpriteBatch ambiente. El coste por
    /// tick es clear + simulación + dibujo + composite + SetData de ~23k px.</summary>
    internal sealed class PixelFxPlayer : IDisposable
    {
        public readonly PxFxInstance Fx;
        public readonly PxMeta Meta;
        public float Escala;
        readonly PxFrame _frame;
        readonly byte[] _buf;
        readonly Color[] _colors;
        Texture2D _tex;

        public PixelFxPlayer(PxFxInstance fx, float escala)
        {
            Fx = fx;
            Meta = fx.meta;
            Escala = escala;
            _frame = new PxFrame(Meta.W, Meta.H, 0, 0);
            _buf = new byte[Meta.W * Meta.H * 4];
            _colors = new Color[Meta.W * Meta.H];
        }

        /// <summary>Un tick de juego (1/60 s); el runner del motor
        /// subpasa la simulación a 120 Hz por dentro.</summary>
        public void Tick() => Fx.Update(1.0 / 60.0);

        /// <summary>Dibuja el efecto con su punto (0,0) del frame en
        /// `posPantalla` (el origen del efecto — place(OX, OY) — lo pone
        /// el runner dentro del frame).</summary>
        public void Draw(SpriteBatch sb, Vector2 posPantalla)
        {
            if (_tex == null || _tex.IsDisposed)
                _tex = new Texture2D(Main.graphics.GraphicsDevice, Meta.W, Meta.H);
            _frame.Clear();
            Fx.Draw(_frame, null);                    // AMBAS capas (back+front), como el arnés
            PixelFxCore.Composite(_frame, _buf);      // el compositor del motor (RGBA de alfa recta)
            for (int i = 0; i < _colors.Length; i++)
                _colors[i] = new Color(_buf[i * 4], _buf[i * 4 + 1], _buf[i * 4 + 2], _buf[i * 4 + 3]);
            _tex.SetData(_colors);
            sb.Draw(_tex, posPantalla, null, Color.White, 0f, Vector2.Zero,
                new Vector2(Escala), SpriteEffects.None, 0f);
        }

        public void Dispose()
        {
            if (_tex != null && !_tex.IsDisposed) _tex.Dispose();
            _tex = null;
        }
    }
}
