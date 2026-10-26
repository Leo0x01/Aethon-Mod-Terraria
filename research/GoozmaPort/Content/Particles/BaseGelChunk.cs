// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using AethonMod.Content.GoozmaPort.Common.Systems.Particles;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.GoozmaPort.Content.Particles;

public abstract class BaseGelChunk<T> : Particle<T> where T : BaseGelChunk<T>
{
    public int style;

    public int time;

    public bool sticking;

    public override void FetchFromPool()
    {
        base.FetchFromPool();

        style = 0;
        time = 0;
        sticking = false;
    }

    public override void OnSpawn()
    {
        Scale *= Main.rand.NextFloat(1f, 1.2f);
        style = Main.rand.Next(2);
    }

    protected override void Update()
    {
        base.Update();
        
        time++;

        if (!sticking) {
            if (Velocity.Y < 30) {
                Velocity += new Vector2(0, 0.5f);
            }

            Rotation = Velocity.ToRotation() - MathHelper.PiOver2;

            if (Collision.IsWorldPointSolid(Position + Velocity) && time > 2) {
                time = 0;
                sticking = true;
                Position = new Vector2(Position.X, (int)(Position.Y / 16f) * 16 + 16);
                for (int i = 0; i < 8; i++) {
                    if (Collision.IsWorldPointSolid(Position + Velocity - new Vector2(0, 8 * i))) {
                        Position -= new Vector2(0, 8);
                    }
                }
                Position -= new Vector2(0, 3);
            }
        }
        else {
            Rotation = 0;
            Velocity = Vector2.Zero;
            if (time > 10) {
                Scale *= 0.95f;
            }

            if (Scale.X < 0.1f) {
                ShouldBeRemovedFromRenderer = true;
            }
        }
    }
}
