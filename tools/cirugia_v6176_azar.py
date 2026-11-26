#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6176_azar.py — v6.176 — SEMBRAR EL AZAR DEL DIBUJO (la letra:
«hacer esto y explica que es: Sembrar el azar del dibujo (10 llamadas
reales en JuicioDosSoles/ElCronista/PrimerTrazoHeld/AlasAuroraHalo):
cómo: semilla fija por entidad (whoAmI·edad), como ya hace
TentaculoCosmico — son 4 archivos»).

QUÉ ES «SEMBRAR EL AZAR DEL DIBUJO»: la regla R5 de la casa — el
dibujo NO tira Main.rand (la lotería global del juego: cada máquina,
cada frame, otra secuencia). El polvo decorativo que nace DENTRO del
dibujo se siembra con Hash01: la semilla nace de la ENTIDAD (whoAmI)
y su EDAD (el tick o el cubo de nacimiento) — así la MISMA migaja de
luz cae en la MISMA posición en la pantalla de cada jugador y en
cada re-dibujo (el visual es REPRODUCIBLE, no una ruleta).

LA AUTOPSIA (la medición estricta del loop v6.175): 10 menciones de
Main.rand en PreDraw/PostDraw — 3 eran COMENTARIOS (PrimerTrazoHeld
y AlasAuroraHalo ya estaban sembrados desde su era: «jamás Main.rand
en render», «ya no tira de Main.rand»); las 7 REALES vivían en:
  · ElCronista (3 — las motas de tinta de la bruma)
  · JuicioDosSoles (4 — la estela de migas de la caza)
Y TAMBIÉN: el loop medía los COMENTARIOS — ahora mide SOLO código
(limpiar_comentarios antes de contar: la medición dice la verdad).
"""
import pathlib
import sys

RAIZ = pathlib.Path(__file__).resolve().parent.parent
F_CRONISTA = RAIZ / "AethonMod" / "Content" / "NPCs" / "ElCronista.cs"
F_JUICIO = RAIZ / "AethonMod" / "Content" / "Weapons" / "Definitivas" / "JuicioDosSoles.cs"
F_LOOP = RAIZ / "tools" / "loop_retro_v6175.py"

# ---------- (1) EL CRONISTA: las motas de tinta ----------
V1 = """                    // LAS MOTAS DE TINTA (las 2 de siempre, ahora nacen
                    // DENTRO de la bruma — la firma del escriba)
                    if ((Main.GameUpdateCount + NPC.whoAmI) % 14f == 0f)
                    {
                        Vector2 cuna = NPC.BottomLeft + new Vector2(
                            Main.rand.NextFloat(NPC.width), 0f);
                        Dust d = Dust.NewDustPerfect(cuna,
                            ModContent.DustType<global::AethonMod.Content.Dusts.PolvoVacioInvertido>(),
                            new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), -0.5f),
                            0, new Color(140, 90, 200), Main.rand.NextFloat(0.22f, 0.34f));
                        d.noGravity = true;
                    }"""

N1 = """                    // LAS MOTAS DE TINTA (las 2 de siempre, ahora nacen
                    // DENTRO de la bruma — la firma del escriba)
                    // v6.176 — EL AZAR SEMBRADO (R5 — la letra de la
                    // sesión: «semilla fija por entidad (whoAmI·edad),
                    // como ya hace TentaculoCosmico»): la mota nace con SU
                    // valor FIJO — semilla = entidad · cubo de nacimiento
                    // (GameUpdateCount/14): la misma mota en todas las
                    // máquinas, sin re-tirar nada por frame.
                    if ((Main.GameUpdateCount + NPC.whoAmI) % 14f == 0f)
                    {
                        int semMota = NPC.whoAmI * 31 + (int)(Main.GameUpdateCount / 14f) * 17 + 5;
                        Vector2 cuna = NPC.BottomLeft + new Vector2(
                            VFXCore.Hash01(semMota, 1, 977) * NPC.width, 0f);
                        Dust d = Dust.NewDustPerfect(cuna,
                            ModContent.DustType<global::AethonMod.Content.Dusts.PolvoVacioInvertido>(),
                            new Vector2((VFXCore.Hash01(semMota, 2, 979) - 0.5f) * 0.4f, -0.5f),
                            0, new Color(140, 90, 200), 0.22f + 0.12f * VFXCore.Hash01(semMota, 3, 983));
                        d.noGravity = true;
                    }"""

# ---------- (2) EL JUICIO: la estela de migas de la caza ----------
V2 = """                    // LA ESTELA DE LA CAZA: migas de luz dorada→roja que el
                    // sol deja mientras persigue (la historia del vuelo)
                    if (!Main.dedServ && Main.rand.NextBool(2))
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        Dust d = Dust.NewDustPerfect(_centroCaza + ang.ToRotationVector2() *
                            (r * (0.7f + 0.5f * Main.rand.NextFloat())),
                            DustID.GoldFlame,
                            -ang.ToRotationVector2() * Main.rand.NextFloat(0.8f, 2.2f), 0,
                            Color.Lerp(new Color(255, 226, 150), new Color(255, 90, 40), rg), 1.1f);
                        d.noGravity = true;
                    }"""

N2 = """                    // LA ESTELA DE LA CAZA: migas de luz dorada→roja que el
                    // sol deja mientras persigue (la historia del vuelo)
                    // v6.176 — EL AZAR SEMBRADO (R5): la miga nace (o no)
                    // con Hash01, y SU ángulo/radio/empuje nacen de la
                    // MISMA semilla — semilla = entidad (whoAmI) · edad
                    // (_t): cada máquina ve la MISMA estela.
                    if (!Main.dedServ)
                    {
                        int semMiga = Projectile.whoAmI * 29 + (int)_t;
                        if (VFXCore.Hash01(semMiga, 4, 701) < 0.5f)
                        {
                            float ang = VFXCore.Hash01(semMiga, 1, 703) * MathHelper.TwoPi;
                            Dust d = Dust.NewDustPerfect(_centroCaza + ang.ToRotationVector2() *
                                (r * (0.7f + 0.5f * VFXCore.Hash01(semMiga, 2, 707))),
                                DustID.GoldFlame,
                                -ang.ToRotationVector2() * (0.8f + 1.4f * VFXCore.Hash01(semMiga, 3, 709)), 0,
                                Color.Lerp(new Color(255, 226, 150), new Color(255, 90, 40), rg), 1.1f);
                            d.noGravity = true;
                        }
                    }"""

# ---------- (3) EL LOOP MIDE CÓDIGO, NO COMENTARIOS ----------
V3 = """            cuerpo = t[k:j]
            nr = cuerpo.count('Main.rand')
            na = len(re.findall(r'new\\s+[A-Za-z_.]+\\[', cuerpo))"""

N3 = """            # v6.176 — LA MEDIDA HONESTA: el cuerpo se LIMPIA DE
            # COMENTARIOS antes de contar (3 de las 10 «llamadas» de la
            # auditoría eran documentación: «jamás Main.rand en render»).
            cuerpo = limpiar_comentarios(t[k:j])
            nr = cuerpo.count('Main.rand')
            na = len(re.findall(r'new\\s+[A-Za-z_.]+\\[', cuerpo))"""


def main() -> int:
    t = F_CRONISTA.read_text(encoding="utf-8")
    if N1 not in t:
        assert V1 in t, "PATRÓN NO ENCONTRADO: ElCronista (motas de tinta)"
        t = t.replace(V1, N1, 1)
        F_CRONISTA.write_text(t, encoding="utf-8")
        print("✔ ElCronista: motas de tinta sembradas (whoAmI · cubo de nacimiento)")
    else:
        print("YA: ElCronista sembrado")

    t = F_JUICIO.read_text(encoding="utf-8")
    if N2 not in t:
        assert V2 in t, "PATRÓN NO ENCONTRADO: JuicioDosSoles (estela de la caza)"
        t = t.replace(V2, N2, 1)
        F_JUICIO.write_text(t, encoding="utf-8")
        print("✔ JuicioDosSoles: la estela de migas sembrada (whoAmI · _t)")
    else:
        print("YA: JuicioDosSoles sembrado")

    t = F_LOOP.read_text(encoding="utf-8")
    if N3 not in t:
        assert V3 in t, "PATRÓN NO ENCONTRADO: loop_retro (medición de comentarios)"
        t = t.replace(V3, N3, 1)
        F_LOOP.write_text(t, encoding="utf-8")
        print("✔ loop_retro: la medición cuenta CÓDIGO, no comentarios")
    else:
        print("YA: loop_retro mide código")
    return 0


if __name__ == "__main__":
    sys.exit(main())
