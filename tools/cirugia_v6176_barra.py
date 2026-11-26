#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6176_barra.py — v6.176 — LA BARRA DEL DEVORADOR NO REBOSA (la
letra: «algo fallo en el mundo interior al inveocar al Devoramundos, la
barra de vida se desborda»).

LA AUTOPSIA: el pilar solo SELLABA a la CABEZA del gusano — la barra
propia (BarraJefeInterior) enseñaba la vida de UNA pieza, los demás
segmentos quedaban SIN sello (sin stats del arena, sin barra propia —
la de vanilla apuntando a otra vida) y cada pieza re-encarnada por la
IA del EoW volvía a nacer desnuda. LA CURA — TRES leyes:

  1. EL GUSANO SE SELLA ENTERO: cada cuerpo/cola que la IA engendra
     hereda el sello de SU cabeza (vía SetDefaults) — como SEGMENTO:
     stats ×1.5 y la BARRA, pero SIN aura (67 auras fundirían el
     frame), SIN motor de combate (67 dashes sería el caos), SIN regen
     y SIN contar kills (el kill del gusano es de la cabeza).
  2. LA BARRA ES DEL CUERPO ENTERO: para EoW y el Destructor suma la
     vida y el lifeMax de TODAS las piezas (como hace la barra de
     vanilla del EoW) — el número total VIVO, la fracción verdadera.
  3. JAMÁS REBOSA: la vida se clampa al máximo (la fracción jamás pasa
     de 1 — el dibujo del rebose es matemáticamente imposible).

 + EL CONGELADO DE LA ASPIRADORA calla al motor de combate (la letra:
 «mientras los jefes estan siendo devorados… deben dejar de lanzar
 proyectiles»): DevorarEstadoEnFauces también consulta
 CongeladoPorAspiradora (el campo nuevo de FaucesGlobalNPC).
"""
import pathlib
import sys

RAIZ = pathlib.Path(__file__).resolve().parent.parent
F = RAIZ / "AethonMod" / "Content" / "Globals" / "JefeInteriorGlobalNPC.cs"

# ---------- (1) EL CAMPO EsSegmento ----------
V1 = """        /// <summary>EL AURA VIVA (la dibujan PreDraw/PostDraw — el
        /// contrato de lotes de la casa).</summary>
        public AuraPerfil Aura;"""

N1 = """        /// <summary>EL AURA VIVA (la dibujan PreDraw/PostDraw — el
        /// contrato de lotes de la casa).</summary>
        public AuraPerfil Aura;

        /// <summary>v6.176 — EL SEGMENTO DE GUSANO: pieza sellada por
        /// adopción (la letra: «la barra de vida se desborda» al invocar
        /// al Devorador): el gusano se sella ENTERO — cada cuerpo/cola
        /// que la IA engendra hereda el sello de SU cabeza. El segmento
        /// lleva los stats del arena y SU barra, pero NO aura, NO motor
        /// de combate, NO regen y NO cuenta kills: el jefe es UNO, no
        /// sesenta y siete.</summary>
        public bool EsSegmento;"""

# ---------- (2) LA BARRA: SUMA DEL GUSANO + CLAMP ----------
V2 = """                if (!n.TryGetGlobalNPC<JefeInteriorGlobalNPC>(out var g) || !g.EsInterior)
                    return false;   // sin sello: la barra de vanilla
                // LA VIDA VIVA (el cacheo de vanilla muere aquí)
                life = n.life;
                lifeMax = n.lifeMax;"""

N2 = """                if (!n.TryGetGlobalNPC<JefeInteriorGlobalNPC>(out var g) || !g.EsInterior)
                    return false;   // sin sello: la barra de vanilla
                // LA VIDA VIVA (el cacheo de vanilla muere aquí)
                // v6.176 — EL GUSANO ENTERO (la letra: «la barra de vida
                // se desborda»): para los jefes SECCIONADOS (EoW y el
                // Destructor) la barra es del CUERPO ENTERO — la suma de
                // la vida de TODAS sus piezas vivas (como la barra de
                // vanilla del EoW), con el número total VIVO de verdad.
                if (EsPiezaDeGusano(n.type))
                {
                    int tipoC = n.type == NPCID.TheDestroyer || n.type == NPCID.TheDestroyerBody
                        || n.type == NPCID.TheDestroyerTail
                        ? NPCID.TheDestroyerBody : NPCID.EaterofWorldsBody;
                    int tipoT = tipoC == NPCID.TheDestroyerBody
                        ? NPCID.TheDestroyerTail : NPCID.EaterofWorldsTail;
                    int tipoH = tipoC == NPCID.TheDestroyerBody
                        ? NPCID.TheDestroyer : NPCID.EaterofWorldsHead;
                    double suma = 0.0, sumaMax = 0.0;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC s = Main.npc[i];
                        if (s == null || !s.active) continue;
                        if (s.type != tipoH && s.type != tipoC && s.type != tipoT) continue;
                        suma += s.life;
                        sumaMax += s.lifeMax;
                    }
                    life = (float)suma;
                    lifeMax = (float)Math.Max(1.0, sumaMax);
                }
                else
                {
                    life = n.life;
                    lifeMax = n.lifeMax;
                }
                // v6.176 — JAMÁS REBOSA: la fracción jamás pasa de 1 (el
                // dibujo del rebose es matemáticamente imposible)
                if (lifeMax <= 0f) lifeMax = 1f;
                if (life > lifeMax) life = lifeMax;
                if (life < 0f) life = 0f;"""

# ---------- (3) LA ADOPCIÓN DE SEGMENTOS EN SetDefaults ----------
V3 = """        public override void SetDefaults(NPC npc)
        {
            // LA RE-ADOPCIÓN (la lección del Devorador: si vanilla MUTA
            // una pieza, SetDefaults destruye los globals frescos — el
            // sello se re-aplica en el mismo tick).
            try
            {"""

N3 = """        /// <summary>v6.176 — ¿Es una pieza (cuerpo/cola) del EoW o del
        /// Destructor? (la cabeza NO: ella nace sellada por el pilar).</summary>
        private static bool EsPiezaDeGusano(int tipo) => tipo == NPCID.EaterofWorldsBody
            || tipo == NPCID.EaterofWorldsTail
            || tipo == NPCID.TheDestroyerBody || tipo == NPCID.TheDestroyerTail;

        /// <summary>v6.176 — ¿El gusano seccionado? (cualquiera de sus
        /// seis piezas — para la barra del CUERPO entero).</summary>
        private static bool EsGusanoSeccionadoONo(int tipo) => EsPiezaDeGusano(tipo)
            || tipo == NPCID.EaterofWorldsHead || tipo == NPCID.TheDestroyer;

        public override void SetDefaults(NPC npc)
        {
            // v6.176 — LA ADOPCIÓN DE LOS SEGMENTOS (la letra: «algo
            // falló en el mundo interior al invocar al Devoramundos»):
            // el gusano se sella ENTERO — cada cuerpo/cola que la IA de
            // vanilla engendra sobre la marcha hereda el sello de SU
            // cabeza (stats del arena + la barra propia). Como
            // SEGMENTO: sin aura, sin motor, sin regen, sin kills.
            try
            {
                if (!EsInterior && EsPiezaDeGusano(npc.type))
                {
                    int tipoH = npc.type == NPCID.TheDestroyerBody || npc.type == NPCID.TheDestroyerTail
                        ? NPCID.TheDestroyer : NPCID.EaterofWorldsHead;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC h = Main.npc[i];
                        if (h == null || !h.active || h.type != tipoH) continue;
                        if (!h.TryGetGlobalNPC<JefeInteriorGlobalNPC>(out var gh) || !gh.EsInterior)
                            continue;
                        EsInterior = true;
                        EsSegmento = true;
                        TipoOriginal = npc.type;
                        NivelIA = gh.NivelIA;
                        AplicarStats(npc);
                        npc.BossBar = ModContent.GetInstance<BarraJefeInterior>();
                        break;
                    }
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
            // LA RE-ADOPCIÓN (la lección del Devorador: si vanilla MUTA
            // una pieza, SetDefaults destruye los globals frescos — el
            // sello se re-aplica en el mismo tick).
            try
            {"""

# ---------- (4) PostAI: LOS GUARDAS DEL SEGMENTO ----------
V4 = """            if (!EsInterior) return;
            try
            {"""

N4 = """            if (!EsInterior) return;
            // v6.176 — EL SEGMENTO SOLO LLEVA EL CUERPO: los refrescos de
            // stats corren para todos (la vida del arena es del gusano
            // ENTERO) — pero la REGEN, la coreografía de minions y el
            // MOTOR de combate viven SOLO en la cabeza: 67 piezas
            // dashando y disparando voleas sería el caos (y la mitad de
            // la vida del jefe regenerándose por segundo, el apagón).
            if (EsSegmento)
            {
                try
                {
                    if (++_tickRefrescoStats >= 30)
                    {
                        _tickRefrescoStats = 0;
                        if (_vidaBase > 0) AplicarStats(npc);
                    }
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
                return;
            }
            try
            {"""

# ---------- (5) OnKill: EL KILL ES DE LA CABEZA ----------
V5 = """            if (!EsInterior) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            try
            {
                int tipo = TipoOriginal > 0 ? TipoOriginal : npc.type;"""

N5 = """            if (!EsInterior) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            // v6.176 — EL KILL DEL GUSANO ES DE LA CABEZA: las piezas no
            // cuentan (un EoW no son 67 kills — la arena no se infla)
            if (EsSegmento) return;
            try
            {
                int tipo = TipoOriginal > 0 ? TipoOriginal : npc.type;"""

# ---------- (6) EL MOTOR calla con la ASPIRADORA ----------
V6 = """                if (npc.TryGetGlobalNPC<FaucesGlobalNPC>(out var g) && g.DevorarEstado != 0)
                    return true;
                return false;"""

N6 = """                if (npc.TryGetGlobalNPC<FaucesGlobalNPC>(out var g) &&
                    (g.DevorarEstado != 0 || g.CongeladoPorAspiradora))
                    return true;
                return false;"""

# ---------- (7) PreAI: el segmento congela con la ASPIRADORA ----------
# (la IA de vanilla de las piezas sigue — pero FaucesGlobalNPC la corta
# cuando el cine las congela; nada que hacer aquí.)


def main() -> int:
    t = F.read_text(encoding="utf-8")
    ya = sum(1 for v in (N1, N2, N3, N4, N5, N6) if v in t)
    if ya:
        print(f"YA APLICADO parcialmente ({ya}/6) — se aborta para no duplicar")
        return 1 if ya < 6 else 0
    for etiqueta, viejo, nuevo in [
        ("campo EsSegmento", V1, N1),
        ("barra del gusano", V2, N2),
        ("adopción de segmentos", V3, N3),
        ("guardas del PostAI", V4, N4),
        ("kill de la cabeza", V5, N5),
        ("motor vs aspiradora", V6, N6),
    ]:
        assert viejo in t, f"PATRÓN NO ENCONTRADO: {etiqueta} — el archivo cambió de forma"
        t = t.replace(viejo, nuevo, 1)
        print(f"✔ {etiqueta}")
    # el ModifyInfo usa EsPiezaDeGusano (cabeza O pieza): ajustar la
    # condición de la barra para incluir las cabezas
    t = t.replace("                if (EsPiezaDeGusano(n.type))\n                {\n                    int tipoC",
                  "                if (EsGusanoSeccionadoONo(n.type))\n                {\n                    int tipoC", 1)
    F.write_text(t, encoding="utf-8")
    print("✔ JefeInteriorGlobalNPc sellado entero + barra del cuerpo + clamp")
    return 0


if __name__ == "__main__":
    sys.exit(main())
