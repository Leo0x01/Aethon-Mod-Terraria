#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# docs_v650116.py — CHANGES entrada .116 + STABLE-SNAPSHOT (header .116/.115/.114
# — la .113 SALE — + fila HISTORIAL + item 1 de PRÓXIMOS con renumeración
# .115→2/.114→3/.113→4 y la .112 PODADA).
# NOTA: README INTACTO (legal-only desde el Task 32 — la versión vive AQUÍ).

CHANGES = """# AethonMod — Historial de Cambios

## Commit v6.50.116 — EL ESCUADRÓN DE ONCE, LA CORONA DOBLE, EL FESTÍN AISLADO, LA GRACIA DEL GUARDIÁN Y EL RECORTES DEL IRIS

**La letra**: «en vez de 10 minion que sean 11, así se ve mejor en la
animación de cuando está el libro fuera» + «debes mejorar la animación
sobre el jugador ya que se quedan muy juntos uno de otros cuando están
encima del jugador, dame ideas para animación en esa fase» + «algo
importante es que los jefes de las oleadas no deben contar para los
cambios del mundo, un ejemplo es el esqueletron, si muere en la oleada
cuenta como matarlo en la mazmorra eso no debe ser asi, todos los jefes
de oleadas son de oleadas y estan separados de los logros de sus
versiones originales asi que no afectan la progresion del juego, solo
afectan al grimorio como tal y su propia progresion asi como al hambre
entre otras cosas del grimorio» + «si se realizo la animacion sobre el
guardian pero salieron mas guardianes justo al instante, debes bloquear
la aparicion de guardianes a solo 1 antes de ser devorado y despues de
ser deborado por al menos 2 minutos, o sea el jugador llega, el guardian
se acerca y antes de tocar al jugador es devorado por el grimorio, luego
hay 2 minutos de gracias antes de que vuelvan a aparecer» + «en el
momento en el que el grimorio esta devorando este entrecierra los ojos,
en ese momento el iris esta por encima del ojo, cuando el grimorio es
pequeño no se nota, pero cuando se hace mas grande para devorar a un
jefe se nota, deberias crear un recorde del iris para evitar eso, por
ejemplo recortar el iris a la mitad, y que la mitad superior se haga
invisible cuando el ojo este medio cerrado».

**EL CAMBIO**: cinco entregas — el once, la corona doble, el festín
aislado, la gracia del guardián y el recortes del iris:

- **EL ESCUADRÓN DE ONCE**: `WeaponScaling.EscuadronEterno = 11` — con la
  banda COMPLETA, las órbitas del libro (la fase de caza, «cuando está
  el libro fuera») cierran EXACTO: el anillo interior lleva 5 ojos y el
  exterior 6 — DOS RUEDAS LLENAS, sin huecos ni remiendos. La 12.ª
  invocación FORTALECE (la compuerta lee `TechoOjosDe`): +50% de daño
  por mejora al ojo con menos mejoras, 4 mejoras máximas. Tooltips y
  `Armas.OjosAlMaximo` («Los once ojos ya llevan sus cuatro mejoras»)
  en los 3 idiomas.
- **LA CORONA DOBLE (la animación sobre el jugador)**: un solo arco de
  ~70 px para 11 ojos de 21 px era UNA FILA APIÑADA (17 px por ojo — se
  pisaban las pestañas). AHORA la banda se abre en **DOS ARCOS
  ENTRELAZADOS a lo LADRILLO**: la primera mitad cuelga del arco
  EXTERIOR (radio 46+2,6·n, tope 80) y la segunda del INTERIOR (23 px
  más cerca, DESFASADA MEDIO PASO — nunca un ojo debajo de otro); el
  arco SE ABRE con la banda (±9°) y se balancea entero, el radio
  RESPIRA ±4% (cada capa a SU reloj — se leen dos capas vivas) y cada
  ojo CABECEA a su propio tempo (±3 px vertical, ±2 px horizontal):
  el DOBLE de cielo por ojo.
- **EL FESTÍN AISLADO (los jefes de oleada NO cuentan para el mundo)**:
  `OleadaNPC.AislarJefeDeOleada` — llamada por `FaucesGlobalNPC.
  CheckDead` en TODA salida donde la muerte de vanilla PROSIGUE (la
  cena y el festín clásico que la secuestran NO la llaman) — guarda la
  identidad del jefe en el sello (`TipoReservado`/`JefeReservado`) y
  entrega el NPC a vanilla con `type=0`/`netID=0`/`boss=false`.
  VERIFICADO AL IL DE ESTE tML (2026.08.3.0): dentro de `NPCLoot` el
  orden es `CountKillForAchievements` → bestiario → `PreKill` → loot →
  `OnKill` (aquí cobra el mod) → `DoDeathEvents` (los flags — con
  type 0 el switch NO casa NINGÚN caso). Resultado: NI `downedBoss3`
  (el Esqueletron de oleada YA NO abre la mazmorra — el ejemplo exacto
  de la letra), NI `downedBoss2`/meteorito del Devorador de oleada, NI
  logros (`NotifyNPCKilled` con netID 0 no dispara NINGUNO), NI
  celebración, NI loot vanilla (el solver cae por netID — ni demonita
  ni bolsas del jefe: «solo afectan al grimorio»). El bestiario con
  netID 0 NO truena (`ContentSamples.NpcBestiaryCreditIdsByNpcNetIds`
  se construye desde −65: la entrada 0 existe y es invisible). Y TODO
  el mod cobra por el SELLO VERDADERO (`TipoVerdadero`/
  `JefeVerdadero`): la esencia (`EsenciaDeJefeItem.DeNPC(
  TipoReservado)`), la XP de jefe (`ShardLevelSystem.XPForNPC`), el
  hambre del 10% (`HambrePorPresa`), la crónica del Testigo
  (`CronicaMarcar(TipoVerdadero)`), la voz del grimorio
  (`ClaveDeVoz`/`ColorDeVoz`) y la victoria del Juicio (`FaseJefe`
  acepta `TipoReservado == _tipoJefeOleada` — el `DerrotaOleada10`
  intacto). El Fragmento Génesis NO cae de jefes de oleada (lee el
  type vivo: es del jefe DE VERDAD, para quien aún no tiene libro).
- **LA GRACIA DEL GUARDIÁN (no se amontona)**: `FaucesGlobalNPC.
  EditSpawnPool` — (1) LA GRACIA: `ShardPlayer.GuardianGracia = 7200`
  ticks (2 minutos) arranca EN EL MORDISCO del easteregg (cubre TODA
  la cena: el instante de «salieron más guardianes justo al instante»
  era AHÍ) y mientras viva, el DungeonGuardian SALE del pool — la
  mazmorra no pare guardianes; (2) UNO SOLO: con el pacto PENDIENTE
  (portador con el Eterno a mano y sin `GuardianMazmorraDevorado`),
  no nace un SEGUNDO guardián si ya hay uno vivo. Para quien no carga
  el libro, la mazmorra sigue siendo la de siempre.
- **EL RECORTES DEL IRIS**: en la cena, el libro gigante entrecierra
  el ojo (párpado Medio) y el iris se dibujaba ENTERO POR ENCIMA de la
  página — de pequeño no se notaba, pero a escala de jefe (×2,6) el
  iris asomaba por encima del párpado. AHORA el iris se dibuja con
  RECTÁNGULO FUENTE RECORTADO DESDE ABAJO (la mitad inferior es la que
  vive) y el ORIGEN COMPENSADO (el borde inferior queda clavado donde
  siempre — la pupila no salta); la banda visible RESPIRA con el
  masticar (`apertura = 0,45 + 0,50·muerde`): al MORDER queda a medias
  (la mitad superior INVISIBLE — la letra) y al ABRIR el bocado afloja
  hasta el 95% del iris. El iris JAMÁS vuelve a asomar por encima de
  la página.

**VERIFICACIÓN**: oráculo 0/0 (79 refs, 311 .cs) · build real 0/0 ·
.tmod 2.626.076 B md5 2a0adae0a46ecef64d42d400fc8f2143 (390 entradas —
los mismos nombres de la .115: solo cambia código —, EOF exacto) ·
headless 0 excepciones (serverconfig116, mundo squad116, puerto 7785) ·
`audit_v650116.py` TODO OK (340 ✓): heap
(`AislarJefeDeOleada`/`TipoReservado`/`JefeReservado`/`TipoVerdadero`/
`JefeVerdadero`/`GuardianGracia`/`GuardianGraciaTicks`/`EditSpawnPool`
VIVOS), ldstr (el escuadrón de 11 VIVO, el de 10 MUERTO), constantes
.104–.116 (0,45/46/23/80/0,92 nuevas en el IL), el árbol con las 5
piezas + la regresión .110–.115 ÍNTEGRA, hjson ×3 semántico + es-MX
byte a byte (OjosAlMaximo once/eleven — el diez/ten MUERTO), los
iconos del usuario SIN cambios.

"""


def changes():
    p = "CHANGES.md"
    s = open(p, encoding="utf-8").read()
    s = s.replace("# AethonMod — Historial de Cambios\n\n", "", 1)
    s = CHANGES + s
    open(p, "w", encoding="utf-8").write(s)
    assert "v6.50.116 — EL ESCUADRÓN DE ONCE" in s
    print("CHANGES.md OK")


HEADER_116 = """> Última actualización: v6.50.116 (EL ESCUADRÓN DE ONCE,
> LA CORONA DOBLE, EL FESTÍN AISLADO, LA GRACIA DEL GUARDIÁN Y EL
> RECORTES DEL IRIS. La letra: «en vez de 10 minion que sean 11, así
> se ve mejor en la animación de cuando está el libro fuera» + «debes
> mejorar la animación sobre el jugador ya que se quedan muy juntos
> uno de otros cuando están encima del jugador» + «los jefes de las
> oleadas no deben contar para los cambios del mundo… solo afectan al
> grimorio como tal y su propia progresión así como al hambre» +
> «bloquear la aparición de guardianes a solo 1 antes de ser devorado
> y después de ser devorado por al menos 2 minutos» + «el iris está
> por encima del ojo… recortar el iris a la mitad, y que la mitad
> superior se haga invisible cuando el ojo esté medio cerrado»).
> Cinco entregas: (1) EL ESCUADRÓN DE ONCE — EscuadronEterno=11: con
> la banda completa las órbitas del libro cierran EXACTO (anillo
> interior 5 + exterior 6, dos ruedas llenas); la 12.ª invocación
> FORTALECE (+50% daño por mejora, 4 mejoras). (2) LA CORONA DOBLE —
> dos arcos entrelazados a lo ladrillo (mitad fuera radio 46+2,6n
> tope 80, mitad dentro a 23 px DESFASADA medio paso), el arco se abre
> con la banda (±9°), el radio respira ±4% y cada ojo cabecea a su
> tempo (±3 px) — el doble de cielo por ojo, FIN del apiñamiento.
> (3) EL FESTÍN AISLADO — AislarJefeDeOleada (FaucesGlobalNPC.
> CheckDead, en toda salida donde la muerte de vanilla prosigue):
> identidad en el sello (TipoReservado/JefeReservado) y NPC con
> type/netID 0 y boss=false — ni downedBoss3 (el Esqueletron de
> oleada YA NO abre la mazmorra), ni meteorito, ni logros, ni
> celebración, ni loot vanilla; el mod cobra TODO por el sello
> verdadero (esencia, XP, hambre 10%, crónica, voz, DerrotaOleada10);
> el Génesis NO cae de oleadas. (4) LA GRACIA DEL GUARDIÁN —
> EditSpawnPool: GuardianGracia=7200 t (2 min) desde el mordisco
> (cubre toda la cena) vacía al guardián del pool; con el pacto
> pendiente, UNO y solo UNO. (5) EL RECORTES DEL IRIS — el iris del
> libro gigante se dibuja con rectángulo fuente recortado desde abajo
> (origen compensado: la pupila no salta) y la banda visible respira
> con el masticar (0,45+0,50·muerde: al morder la mitad superior es
> INVISIBLE, al abrir afloja hasta 0,95) — jamás asoma sobre el
> párpado Medio. .tmod 2.626.076 B md5
> 2a0adae0a46ecef64d42d400fc8f2143, 390 entradas, oráculo 0/0, build
> real 0/0, headless 0 excepciones, audit_v650116 TODO OK (340 ✓).
> PENDIENTE EN JUEGO: (a) invocar 11 ojos: con el libro FUERA, DOS
> RUEDAS exactas (5+6); con el libro guardado, la CORONA DOBLE (seis
> fuera, cinco dentro enladrillados), (b) la 12.ª invocación
> fortalece (igual que siempre, ahora con 11 puestos), (c) matar al
> Esqueletron de una oleada: la mazmorra SIGUE CERRADA, sin logro, sin
> meteorito, sin loot vanilla — y el libro cobra esencia + XP + hambre
> + crónica + voz, (d) el easteregg del guardián: durante y DESPUÉS de
> la cena (2 minutos) no aparece NI UNO más, (e) la cena: el ojo
> entrecerrado del libro gigante SIN iris asomando por encima de la
> página (y el recorte respirando con cada mordida).
"""


def snapshot():
    p = "STABLE-SNAPSHOT.md"
    lineas = open(p, encoding="utf-8").read().splitlines(keepends=True)

    # ---------- 1) EL HEADER: título .116 + bloques .116/.115/.114 ----------
    i_b115 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.115 ("))
    i_b114 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.114 ("))
    i_b113 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.113 ("))
    bloque_115 = "".join(lineas[i_b115:i_b114])
    bloque_114 = "".join(lineas[i_b114:i_b113])
    # el fin del bloque .113: la primera línea que NO empieza con '>' tras i_b113
    i_fin113 = next(i for i in range(i_b113 + 1, len(lineas))
                    if not lineas[i].startswith(">"))
    header_nuevo = [
        "# AethonMod — ESTADO ACTUAL (v6.50.116)\n",
        "\n",
        "> **Este documento = \"¿por dónde nos quedamos?\"** — se actualiza en cada entrega.\n",
        HEADER_116,
        bloque_115,
        bloque_114,
    ]
    lineas = header_nuevo + lineas[i_fin113:]
    s = "".join(lineas)
    assert "ESTADO ACTUAL (v6.50.116)" in s
    assert s.count("Última actualización: v6.50.116") == 1
    assert "Última actualización: v6.50.113" not in s.split("## ✅ ESTADO VERIFICADO")[0]
    print("  header OK (bloques .116/.115/.114 — la .113 salió)")

    # ---------- 2) LA FILA DEL HISTORIAL (antes de la fila de la .115) ----------
    ancla = "| **v6.50.115** |"
    i = s.find(ancla)
    assert i > 0, "no encontré la fila .115 del HISTORIAL"
    s = s[:i] + FILA_116 + s[i:]
    assert s.count("| **v6.50.116** |") == 1
    print("  fila HISTORIAL OK")

    # ---------- 3) PRÓXIMOS: doc-rot POR LÍNEAS con anclas por versión ----------
    # 3a. PODAR la .112 (de su línea de inicio a la línea previa a la .100)
    lin = s.splitlines(keepends=True)
    i_112 = next(i for i, l in enumerate(lin)
                 if l.startswith("4. **✅ v6.50.112 PUBLICADA"))
    i_100 = next(i for i, l in enumerate(lin)
                 if l.startswith("1. **✅ v6.50.100 PUBLICADA"))
    assert i_112 < i_100, "la .112 debería estar antes que la .100"
    del lin[i_112:i_100]
    s = "".join(lin)
    assert "v6.50.112 PUBLICADA" not in s
    print("  .112 PODADA de PRÓXIMOS")

    # 3b. RENUMERAR (del 3 hacia arriba — anclas únicas por versión)
    for viejo, nuevo, version in [
        ("3. **✅ v6.50.113 PUBLICADA", "4. **✅ v6.50.113 PUBLICADA", ".113"),
        ("2. **✅ v6.50.114 PUBLICADA", "3. **✅ v6.50.114 PUBLICADA", ".114"),
        ("1. **✅ v6.50.115 PUBLICADA", "2. **✅ v6.50.115 PUBLICADA", ".115"),
    ]:
        assert s.count(viejo) == 1, f"ancla no única para {version}: {viejo!r}"
        s = s.replace(viejo, nuevo, 1)
    print("  renumeración .115→2 · .114→3 · .113→4")

    # 3c. INSERTAR la .116 como item 1 (tras el header de PRÓXIMOS)
    ancla = "## 🚧 PRÓXIMOS PASOS SUGERIDOS (en orden)\n\n"
    i = s.find(ancla)
    assert i > 0
    i += len(ancla)
    s = s[:i] + ITEM_116 + s[i:]
    assert s.count("v6.50.116 PUBLICADA — EL ESCUADRÓN DE ONCE") == 1
    print("  item 1 (.116) insertado")

    open(p, "w", encoding="utf-8").write(s)
    print("STABLE-SNAPSHOT.md OK")


FILA_116 = """| **v6.50.116** | ✅ Build-verificada (oráculo 0/0 sobre 311 .cs con 79 refs, build real 0/0, .tmod 2.626.076 B md5 2a0adae0a46ecef64d42d400fc8f2143, 390 entradas (los mismos nombres de la .115: solo cambia código), EOF exacto, heap/ldstr/constantes .104–.116 (AislarJefeDeOleada/TipoReservado/JefeReservado/TipoVerdadero/JefeVerdadero/GuardianGracia/GuardianGraciaTicks/EditSpawnPool VIVOS; el ldstr «Escuadrón de 11 ojos…» VIVO en #US UTF-16 y el de 10 MUERTO; las constantes 0,45/46/23/80/0,92 nuevas VIVAS en el IL), el árbol con las 5 piezas + la regresión .110–.115 ÍNTEGRA (la corona de conjuro, la sombra enraizada en el libro, el easteregg del guardián y el fortalecimiento .115 vivos), hjson ×3 semántico + es-MX byte a byte (OjosAlMaximo once/eleven — diez/ten MUERTOS), iconos del usuario SIN cambios, headless «Sandboxing: Aethon, the Eternal Grimoire v6.50.116 → Adding Content → Server started» 0 excepciones (serverconfig116, mundo squad116, puerto 7785)), ✔ PUBLICADA (release 411973795 + asset AethonMod.tmod 637862185 2.626.076 B, CDN verificado byte a byte md5 2a0adae0…, /releases/latest = v6.50.116, respaldo /home/sync/AethonMod-v6.50.116.tmod), ⏳ en juego | LA LETRA (2026-10-13): «en vez de 10 minion que sean 11, así se ve mejor en la animación de cuando está el libro fuera» + «debes mejorar la animación sobre el jugador ya que se quedan muy juntos uno de otros cuando están encima del jugador, dame ideas para animación en esa fase» + «los jefes de las oleadas no deben contar para los cambios del mundo… todos los jefes de oleadas son de oleadas y están separados de los logros de sus versiones originales… solo afectan al grimorio como tal y su propia progresión así como al hambre» + «bloquear la aparición de guardianes a solo 1 antes de ser devorado y después de ser devorado por al menos 2 minutos» + «el iris está por encima del ojo… recortar el iris a la mitad, y que la mitad superior se haga invisible cuando el ojo esté medio cerrado». CINCO ENTREGAS: (1) EL ESCUADRÓN DE ONCE — EscuadronEterno=11 (órbitas del libro 5+6 EXACTO); la 12.ª fortalece. (2) LA CORONA DOBLE — dos arcos entrelazados a lo ladrillo (mitad fuera 46+2,6n tope 80 / mitad dentro −23 px desfasada medio paso), arco que se abre ±9°, radio que respira ±4% y cabeceo propio ±3 px — el doble de cielo por ojo. (3) EL FESTÍN AISLADO — AislarJefeDeOleada (CheckDead, salidas de muerte que prosigue): sello TipoReservado/JefeReservado + type/netID 0 + boss=false — sin downedBoss3/meteorito/logros/celebración/loot vanilla; el mod cobra por TipoVerdadero/JefeVerdadero (esencia, XP, hambre, crónica, voz, DerrotaOleada10); Génesis no cae de oleadas. (4) LA GRACIA DEL GUARDIÁN — EditSpawnPool: GuardianGracia 7200 t desde el mordisco (cubre la cena) + UNO solo con el pacto pendiente. (5) EL RECORTES DEL IRIS — rectángulo fuente recortado desde abajo + origen compensado; apertura 0,45+0,50·muerde (mitad superior invisible al morder). LECCIONES: (a) el ORDEN REAL de NPCLoot (verificado al IL: OnKill corre ANTES de DoDeathEvents) permite entregar el jefe con type 0 y cobrar TODO el mod en OnKill desde el sello — el aislamiento sin tocar UNA línea de vanilla; (b) ContentSamples.NpcBestiaryCreditIdsByNpcNetIds arranca en −65: netID 0 tiene entrada — RegisterKill(0) es seguro (la muerte con netID 0 no truena el bestiario); (c) 11 = 5+6: cuando la letra pide un número «para que se vea mejor», la geometría de la formación ya sabía la respuesta (dos anillos llenos); (d) un easteregg que SPAWNEA (el guardián) necesita su puerta en el POOL (EditSpawnPool), no en el PreAI — el amontonamiento nacía del motor de spawns, no del guardián mismo |
"""

ITEM_116 = """1. **✅ v6.50.116 PUBLICADA — EL ESCUADRÓN DE ONCE, LA CORONA DOBLE,
   EL FESTÍN AISLADO, LA GRACIA DEL GUARDIÁN Y EL RECORTES DEL IRIS**
   (la letra: «en vez de 10 minion que sean 11, así se ve mejor en la
   animación de cuando está el libro fuera» + «mejorar la animación
   sobre el jugador ya que se quedan muy juntos» + «los jefes de las
   oleadas no deben contar para los cambios del mundo… solo afectan al
   grimorio» + «bloquear la aparición de guardianes a solo 1 antes de
   ser devorado y después por al menos 2 minutos» + «recortar el iris
   a la mitad, y que la mitad superior se haga invisible cuando el ojo
   esté medio cerrado»). (1) EL ESCUADRÓN DE ONCE — con la banda
   completa las órbitas del libro son DOS RUEDAS exactas (5+6); la
   12.ª invocación FORTALECE. (2) LA CORONA DOBLE — dos arcos
   entrelazados a lo ladrillo sobre el jugador: el doble de cielo por
   ojo, el arco se abre con la banda, el radio respira y cada ojo
   cabecea a su tempo. (3) EL FESTÍN AISLADO — el jefe de oleada muere
   con su identidad en el sello y el NPC entregado con type/netID 0:
   la mazmorra NO se abre por el Esqueletron de oleada, sin meteorito,
   sin logros, sin loot vanilla — y el grimorio cobra esencia, XP,
   hambre, crónica y voz IGUAL. (4) LA GRACIA DEL GUARDIÁN — desde el
   mordisco del easteregg, 2 minutos sin guardianes (cubre toda la
   cena); antes del pacto, UNO y solo UNO. (5) EL RECORTES DEL IRIS —
   el iris del libro gigante entrecerrado se recorta desde abajo (la
   mitad superior invisible al morder, el borde inferior clavado) y
   respira con el masticar. **PENDIENTE EN JUEGO**: (a) 11 ojos con el
   libro fuera (dos ruedas) y con el libro guardado (la corona doble,
   sin apiñar), (b) matar al Esqueletron de una oleada y comprobar que
   la mazmorra SIGUE cerrada (y el libro cobra igual), (c) el easteregg
   del guardián: durante la cena y 2 minutos después, NI UN guardián
   más, (d) la cena: el iris del libro grande SIN asomar sobre el
   párpado, recortándose con cada mordida.

"""


def main():
    changes()
    snapshot()
    print("DOCS .116 OK (README intocado — legal-only)")


if __name__ == "__main__":
    main()
