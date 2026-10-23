using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  v6.51.10 — LA SOMBRA DE LA PÁGINA (la letra del usuario: «seria
    //  interesante una animacion donde el grimorio del inventario
    //  extiende su sombra de la pagina hasta los items correctos y estos
    //  son automaticamente devorados en caso de que el inventario este
    //  abierto, si el inventario no esta abierto pues no pasa nada y
    //  solo sube su nivel de despertar, seria interesante saber que los
    //  jugadores se sorprende cuando el grimorio del inventario se lleva
    //  los items requeridos jajajaja»).
    //
    //  LA TÉCNICA (tres piezas, sin tocar el lote de vanilla):
    //   · LA MESA DE POSICIONES: un GlobalItem (DevoracionSlotsGlobalItem)
    //     anota en PreDrawInInventory el CENTRO EXACTO de cada item
    //     dibujado en el inventario (el hook recibe screenPositionForItemCenter
    //     — verificado en el ItemSlot decompilado) + su escala. La mesa
    //     se llena CADA FRAME mientras el inventario está abierto: sirve
    //     para CUALQUIER resolución/escala, sin fórmulas de layout.
    //   · EL DISPARO: DespertarGrimorioSistema.AutoDevorarOfrenda, al
    //     comer la mesa del despertar, llama Disparar() con los SLOTS
    //     víctima — sus posiciones se resuelven contra la mesa del frame
    //     anterior (los platos aún estaban dibujados) y quedan FOTOGRAFIADAS
    //     en la víctima (a partir de ahí ya no importan los items: los
    //     fantasmas se dibujan solos).
    //   · EL CINE (DevoracionInventarioSistema.PostDrawInterface): para
    //     cada víctima, escalonada — LA SOMBRA se EXTIENDE del libro al
    //     plato (una cadena de círculos suaves sobre una curva Bézier),
    //     el FANTASMA tiembla en su slot, viaja por la sombra hasta la
    //     boca del libro encogiéndose… y al llegar, el LIBRO TRAGA (su
    //     pulso dorado) con su eructito de chispas. Todo determinista
    //     (Main.GameUpdateCount), todo en espacio de pantalla, UNA sola
    //     pasada alfa: sin End/Begin del lote (la regla de la casa tras
    //     el crash de la v6.50.49).
    //
    //  Si el inventario NO está abierto, AutoDevorarOfrenda ni llama al
    //  FX («no pasa nada y solo sube su nivel de despertar»). En
    //  dedicado no se dibuja (nadie mira): la lógica sigue en el
    //  servidor y el despertar llega por EcoRed.
    // ======================================================================
    public static class DevoracionInventarioFX
    {
        // === LA MESA DE POSICIONES (la llena el GlobalItem cada frame) ===
        private static readonly Dictionary<Item, PosicionDibujada> _pos =
            new Dictionary<Item, PosicionDibujada>();

        private readonly struct PosicionDibujada
        {
            public readonly Vector2 Centro;
            public readonly float Escala;
            public PosicionDibujada(Vector2 centro, float escala)
            { Centro = centro; Escala = escala; }
        }

        /// <summary>La anotación del GlobalItem: dónde y a qué escala se
        /// dibujó ESTE item en el inventario (el centro exacto).</summary>
        public static void Anotar(Item item, Vector2 centro, float escala)
        {
            if (item == null || item.IsAir) return;
            if (_pos.Count > 512) _pos.Clear();   // el portero: nada de crecer sin fin
            _pos[item] = new PosicionDibujada(centro, escala);
        }

        // === LA ANIMACIÓN VIVA ===
        /// <summary>UNA VÍCTIMA de la devoración: el slot de la mochila,
        /// el tipo de item, cuántos se llevó… y la posición FOTOGRAFIADA
        /// de su sprite (el centro exacto donde estaba dibujado).</summary>
        public readonly struct Victima
        {
            public readonly int Slot;
            public readonly int Tipo;
            public readonly int Cantidad;
            public readonly Vector2 Pos;      // el centro del slot (fotografiado)
            public readonly float Escala;     // la escala a la que se dibujaba
            public readonly bool Visible;     // ¿tenía posición conocida?
            public Victima(int slot, int tipo, int cantidad)
            { Slot = slot; Tipo = tipo; Cantidad = cantidad; Pos = Vector2.Zero; Escala = 1f; Visible = false; }
            private Victima(int slot, int tipo, int cantidad, Vector2 pos, float escala)
            { Slot = slot; Tipo = tipo; Cantidad = cantidad; Pos = pos; Escala = escala; Visible = true; }
            public Victima ConPosicion(Vector2 pos, float escala)
                => new Victima(Slot, Tipo, Cantidad, pos, escala);
        }

        private static readonly List<Victima> _victimas = new List<Victima>();
        private static Vector2 _boca;              // el centro del libro (su slot)
        private static float _escalaLibro = 1f;
        private static bool _activa = false;
        private static ulong _t0 = 0;

        private const int F_EXTIENNE = 22;   // la sombra crece del libro al plato
        private const int F_ACECHA = 10;     // el fantasma tiembla en su slot
        private const int F_TRAGA = 30;      // el fantasma viaja a la boca
        private const int ESCALON = 9;       // el retardo entre víctima y víctima
        private const int COLA = 26;         // el cierre (el libro saborea)

        private static int DuracionTotal(int n)
            => F_EXTIENNE + F_ACECHA + F_TRAGA + COLA + ESCALON * Math.Max(0, n - 1);

        /// <summary>¿La sombra está en pleno cine?</summary>
        public static bool Activa => _activa;

        /// <summary>EL DISPARO: la mesa ya está comida — la sombra de la
        /// página sale del libro (su slot del inventario) hasta cada plato.
        /// Las posiciones se leen de la MESA DEL FRAME ANTERIOR (los items
        /// aún estaban dibujados); las que no se conocen se saltan sin
        /// drama (el plato ya está comido: su fantasma es un lujo).</summary>
        public static void Disparar(Player dueño, List<Victima> victimas, Item libro)
        {
            try
            {
                if (dueño == null || victimas == null || victimas.Count == 0) return;
                if (libro == null || libro.IsAir) return;
                if (!_pos.TryGetValue(libro, out var boca)) return;   // sin boca no hay cine
                if (Main.dedServ) return;

                _boca = boca.Centro;
                _escalaLibro = boca.Escala;
                _victimas.Clear();
                foreach (var v in victimas)
                {
                    Item it = dueño.inventory[v.Slot];
                    if (it != null && _pos.TryGetValue(it, out var cap))
                        _victimas.Add(v.ConPosicion(cap.Centro, cap.Escala));
                }
                if (_victimas.Count == 0) return;

                _t0 = Main.GameUpdateCount;
                _activa = true;

                // EL RUIDO DEL TENEDOR (el trago de la casa).
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item113.WithPitchOffset(-0.45f), dueño.Center);
            }
            catch { _activa = false; }
        }

        /// <summary>LA CURVA de la sombra (una Bézier con su barriga:
        /// cada víctima curva hacia su lado — la sombra de una página
        /// nunca va derecha).</summary>
        private static Vector2 Curva(Vector2 a, Vector2 b, float semilla, float t)
        {
            Vector2 medio = (a + b) * 0.5f;
            Vector2 dir = b - a;
            if (dir.LengthSquared() < 1f) dir = new Vector2(0f, -1f);
            Vector2 perp = new Vector2(-dir.Y, dir.X);
            perp.Normalize();
            float barriga = 14f + 26f * (0.5f + 0.5f * MathF.Sin(semilla * 2.399f));
            Vector2 control = medio + perp * barriga *
                (MathF.Sin(semilla * 1.7f) >= 0f ? 1f : -1f);
            float u = 1f - t;
            return u * u * a + 2f * u * t * control + t * t * b;
        }

        private static float Suave(float x)
            => x <= 0f ? 0f : (x >= 1f ? 1f : x * x * (3f - 2f * x));

        /// <summary>EL CINE: lo llama DevoracionInventarioSistema en
        /// PostDrawInterface (espacio de pantalla, lote vanilla intacto).</summary>
        public static void Draw(SpriteBatch sb)
        {
            if (!_activa) return;
            try
            {
                // v6.51.10 — LA REGLA DE LA LETRA: «si el inventario no
                //     esta abierto pues no pasa nada» — si el dueño cierra
                //     la mochila a mitad del trago, el cine SE APAGA (los
                //     fantasmas no flotan sobre el mundo).
                if (!Main.playerInventory) { _activa = false; return; }

                long transcurrido = (long)(Main.GameUpdateCount - _t0);
                if (transcurrido > DuracionTotal(_victimas.Count))
                { _activa = false; return; }

                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (glow == null) { _activa = false; return; }

                float tGlobal = Main.GlobalTimeWrappedHourly;
                var tinta = new Color(26, 8, 44);        // la negrura morada de la página
                var tintaClara = new Color(66, 26, 104); // el filo de la sombra

                // LA BOCA DEL LIBRO — su pulso mientras come.
                bool comiendo = false;

                for (int i = 0; i < _victimas.Count; i++)
                {
                    var v = _victimas[i];
                    float t0v = i * ESCALON;
                    float tv = transcurrido - t0v;
                    if (tv < 0f) continue;    // aún no le toca
                    if (tv > F_EXTIENNE + F_ACECHA + F_TRAGA + 6f) continue;  // ya fue comida

                    // === FASE 1 — LA SOMBRA SE EXTIENDE (0..F_EXTIENNE) ===
                    float pExt = Suave(tv / F_EXTIENNE);
                    // === FASE 3 — EL VIAJE DEL FANTASMA ===
                    float pTraga = Suave((tv - F_EXTIENNE - F_ACECHA) / F_TRAGA);
                    bool enViaje = tv > F_EXTIENNE + F_ACECHA;
                    if (enViaje) comiendo = true;

                    // EL LÍMITE de la sombra: crece hasta el plato en la
                    // fase 1… y EN COBRAJE se RETRASA tras el fantasma que
                    // viaja (visible solo del libro hasta el bocado).
                    float fantasmaF = 1f - 0.92f * pTraga;   // dónde va el fantasma
                    float limite = enViaje ? fantasmaF : pExt;

                    // LA SOMBRA: cadena de círculos suaves sobre la curva.
                    int eslabones = 16;
                    for (int e = 0; e <= eslabones; e++)
                    {
                        float f = e / (float)eslabones;
                        if (f > limite) break;   // aún está creciendo / ya se retrajo
                        Vector2 p = Curva(_boca, v.Pos, i + 1.3f, f);
                        float radio = (5.5f + 3.5f * MathF.Sin(f * MathHelper.Pi)) *
                            (0.85f + 0.15f * MathF.Sin(tGlobal * 5f + e * 1.3f));
                        float alfa = 0.42f * (enViaje ? (1f - 0.65f * pTraga) : 1f) *
                            (f < 0.06f || f > limite - 0.06f ? 0.6f : 1f);
                        sb.Draw(glow, p, null, tinta * alfa, 0f,
                            glow.Size() * 0.5f,
                            new Vector2(radio, radio) / glow.Size() * 2.2f,
                            SpriteEffects.None, 0f);
                    }
                    // EL FILO de la sombra (la puntita que avanza — o que
                    // persigue al fantasma en el cobre).
                    if (limite > 0.05f && limite < 0.98f)
                    {
                        Vector2 punta = Curva(_boca, v.Pos, i + 1.3f, limite);
                        sb.Draw(glow, punta, null, tintaClara * 0.55f, 0f,
                            glow.Size() * 0.5f,
                            new Vector2(9f, 9f) / glow.Size() * 2.2f,
                            SpriteEffects.None, 0f);
                    }

                    // === FASE 2 — EL FANTASMA TIEMBLA en su slot (el agarre) ===
                    Vector2 posFantasma = v.Pos;
                    float escala = v.Escala;
                    Color colorFantasma = Color.White;
                    if (!enViaje && tv > F_EXTIENNE * 0.55f)
                    {
                        float agarre = Suave((tv - F_EXTIENNE * 0.55f) / (F_ACECHA + F_EXTIENNE * 0.45f));
                        posFantasma += new Vector2(
                            MathF.Sin(tGlobal * 31f + i * 7f) * 2.2f * agarre,
                            MathF.Cos(tGlobal * 27f + i * 5f) * 1.6f * agarre);
                        colorFantasma = Color.Lerp(Color.White,
                            new Color(150, 90, 210), agarre * 0.55f);
                    }
                    else if (enViaje)
                    {
                        posFantasma = Curva(_boca, v.Pos, i + 1.3f, 1f - pTraga * 0.92f);
                        escala = v.Escala * (1f - 0.78f * pTraga);
                        colorFantasma = Color.Lerp(
                            new Color(150, 90, 210), tinta, pTraga * 0.85f);
                        colorFantasma *= 1f - 0.35f * pTraga;
                    }

                    // EL SPRITE FANTASMA (la textura del item, como el juego).
                    try
                    {
                        Texture2D tex = Terraria.GameContent.TextureAssets.Item[v.Tipo].Value;
                        if (tex != null)
                        {
                            Rectangle frame = tex.Frame();
                            var anim = Main.itemAnimations[v.Tipo];
                            if (anim != null) frame = anim.GetFrame(tex);
                            sb.Draw(tex, posFantasma, frame, colorFantasma, 0f,
                                frame.Size() * 0.5f, escala, SpriteEffects.None, 0f);
                        }
                    }
                    catch { }

                    // LAS MIGAS que escapan (partículas deterministas hacia la boca).
                    if (enViaje && pTraga < 0.85f)
                    {
                        for (int m = 0; m < 3; m++)
                        {
                            float fm = (tGlobal * 0.9f + m * 0.33f + i * 0.21f) % 1f;
                            Vector2 pMiga = Curva(_boca, v.Pos, i + 1.3f, 1f - fm);
                            sb.Draw(glow, pMiga, null, tintaClara * (0.5f * (1f - fm)), 0f,
                                glow.Size() * 0.5f,
                                new Vector2(4.5f, 4.5f) / glow.Size() * 2.2f,
                                SpriteEffects.None, 0f);
                        }
                    }

                    // EL ERUCTO dorado al tragar (el libro saborea).
                    float tEructo = (tv - F_EXTIENNE - F_ACECHA - F_TRAGA) / 10f;
                    if (tEructo > 0f && tEructo < 1f)
                    {
                        float arde = MathF.Sin(tEructo * MathHelper.Pi);
                        sb.Draw(glow, _boca, null,
                            new Color(255, 216, 122) * (0.55f * arde), 0f,
                            glow.Size() * 0.5f,
                            new Vector2(20f, 20f) / glow.Size() * 2.2f * (0.6f + 0.4f * arde),
                            SpriteEffects.None, 0f);
                    }
                }

                // EL LIBRO — su pulso de apetito (la boca que mastica).
                if (comiendo || transcurrido < F_EXTIENNE)
                {
                    float pulso = 1f + 0.10f * MathF.Sin(tGlobal * 16f);
                    try
                    {
                        Texture2D texLibro = Terraria.GameContent.TextureAssets
                            .Item[ModContent.ItemType<
                                global::AethonMod.Content.Weapons.GrimoireEternal>()].Value;
                        if (texLibro != null)
                        {
                            Rectangle fr = texLibro.Frame();
                            sb.Draw(texLibro, _boca, fr,
                                Color.White * 0.85f, 0f, fr.Size() * 0.5f,
                                _escalaLibro * pulso, SpriteEffects.None, 0f);
                        }
                    }
                    catch { }
                    sb.Draw(glow, _boca, null,
                        new Color(196, 138, 255) * 0.30f, 0f,
                        glow.Size() * 0.5f,
                        new Vector2(26f, 26f) / glow.Size() * 2.2f * pulso,
                        SpriteEffects.None, 0f);
                }
            }
            catch { _activa = false; }
        }
    }

    // ======================================================================
    //  LA MESA DE POSICIONES — anota el CENTRO de cada item dibujado en
    //  CUALQUIER inventario (el hook de tML recibe screenPositionForItemCenter:
    //  el punto exacto donde el juego dibuja el sprite — verificado en el
    //  ItemSlot decompilado del tML 2026.08). Sin fórmulas de layout:
    //  sirve para toda resolución y escala de interfaz.
    // ======================================================================
    public class DevoracionSlotsGlobalItem : GlobalItem
    {
        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch,
            Vector2 position, Rectangle frame, Color drawColor, Color environmentColor,
            Vector2 origin, float scale)
        {
            try { DevoracionInventarioFX.Anotar(item, position, scale); } catch { }
            return true;   // el dibujado de vanilla sigue INTACTO
        }
    }

    // ======================================================================
    //  LA SALA DE CINE — dibuja la sombra de la página ENCIMA de la
    //  interfaz (cuando el inventario está abierto y hay devoración viva).
    // ======================================================================
    public class DevoracionInventarioSistema : ModSystem
    {
        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            try
            {
                if (DevoracionInventarioFX.Activa)
                    DevoracionInventarioFX.Draw(spriteBatch);
            }
            catch { }
        }
    }
}
