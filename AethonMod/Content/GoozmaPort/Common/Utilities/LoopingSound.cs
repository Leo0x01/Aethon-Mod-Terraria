// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using System;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria.Audio;

namespace AethonMod.Content.GoozmaPort.Common.Utilities;

public class LoopingSound
{
    private SoundStyle style;
    private SlotId slot;
    private Func<bool> condition;

    public LoopingSound(SoundStyle soundStyle, Func<bool> activeCondition)
    {
        style = soundStyle with { IsLooped = true };
        condition = activeCondition;
        slot = SlotId.Invalid;
    }

    public void PlaySound(Func<Vector2> position, Func<float> volume, Func<float> pitch)
    {
        bool active = SoundEngine.TryGetActiveSound(slot, out ActiveSound activeSound);

        if ((!active || !slot.IsValid) && condition.Invoke()) {
            slot = SoundEngine.PlaySound(style, position.Invoke(), sound => {
                if (sound != null) {
                    sound.Position = position.Invoke();
                    sound.Volume = volume.Invoke();
                    sound.Pitch = pitch.Invoke();
                }

                return condition.Invoke();
            });
        }

        if (active) {
            activeSound.Position = position.Invoke();
            activeSound.Volume = volume.Invoke();
            activeSound.Pitch = pitch.Invoke();
        }
    }

    public void StopSound()
    {
        SoundEngine.TryGetActiveSound(slot, out ActiveSound activeSound);

        if (activeSound != null) {
            activeSound.Stop();
        }
        slot = SlotId.Invalid;
    }

}
