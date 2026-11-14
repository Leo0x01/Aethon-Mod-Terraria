#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6136.py — LA PURGA DE LAS DOS ARMAS DE INVESTIGACIÓN + EL MUÑECO
DE LA CASA.

LA LETRA (sesión 17): «Arbitrador II, El Principio y el Fin, puedes borrar
esas armas, ya no son útiles, el resto de contenido de la carpeta se queda
tal cual, ya que no tiene menciones externas ni nada de eso» + «Genera las
texturas con SForja, y luego verifica que todo esté sin mencionar ni
referencias externas».

LO QUE HACE:
  (1) BORRA los 13 archivos de las dos armas y SUS dependencias muertas
      (solo ellas las usaban — verificado por grep):
        · Weapons/Investigaciones/ArbitradorII.cs + .png
        · Weapons/Investigaciones/ElPrincipioYElFin.cs + .png
        · Projectiles/Investigaciones/ArbitradorIIProyectil.cs
        · Projectiles/Investigaciones/DagaEterna.cs (las dagas del canje
          + sus 4 clases internas: AlmaEco, AlmaLuzInferior, OndaSonora…)
        · Players/Investigaciones/SigiloRatero.cs (el sigilo — solo ellas)
        · Buffs/Investigaciones/BuffsEternos.cs (las 4 marcas) + 4 .png
        · Globals/Investigaciones/InvestigacionesGlobalNPC.cs (las pilas)
  (2) LAVA las menciones de los archivos que se quedan:
        · BolsasCategorias.cs (el bloque v6.124/v6.133 + los 2 l.Add)
        · CetroDagasEterno.cs (RestOffsetAngle DEL STABBER (Calamity) + la
          secuencia del stabber)
        · DagaEternaMinion.cs (la cita v6.133 del staff ajeno)
        · LA-SUPERFORJA.md (la palabra prestada del estilo del cristal)
  (3) hjson ×3: borra los bloques de los condenados (armas, proyectiles,
      buffs) + el comentario huérfano de es-MX + los tooltips del muñeco
      (ya no es "el mismo muñeco de Terraria").
  (4) EL MUÑECO DE LA CASA: DummyNegroNPC pierde el override
      "Terraria/Images/NPC_113" (sprite propio de la forja — se genera en
      el paso siguiente) y DummyNegroItem lo toma PRESTADO del NPC
      (override de la casa). PreDraw simplificado a un solo marco.
  (5) build.txt → 6.136.

TODO reemplazo lleva assert: si el patrón no está, el script MUERE.
"""
import os
import re

RAIZ = '/home/z/my-project/mod/AethonMod'
REPO = '/home/z/my-project/mod'


def lee(ruta):
    return open(ruta, encoding='utf-8').read()


def escribe(ruta, s):
    open(ruta, 'w', encoding='utf-8').write(s)


def reemplaza(ruta, viejo, nuevo, veces=1):
    s = lee(ruta)
    assert s.count(viejo) == veces, f'PATRÓN ({veces}x esperado): {viejo[:70]!r} en {ruta}'
    escribe(ruta, s.replace(viejo, nuevo))
    print(f'  ✔ {os.path.basename(ruta)}: {viejo[:48]!r}…')


def borra_bloque_hjson(ruta, clave):
    """Borra el bloque de nivel-1 '\tCLAVE: { … }' (primer cierre '\n\t}')."""
    s = lee(ruta)
    pat = re.compile(r'\n\t' + re.escape(clave) + r': \{.*?\n\t\}', re.S)
    s2, n = pat.subn('', s)
    assert n == 1, f'bloque {clave} × {n} en {ruta}'
    escribe(ruta, s2)
    print(f'  ✔ {os.path.basename(ruta)}: bloque {clave} borrado')


def borra_linea_hjson(ruta, clave):
    """Borra la línea plana '\tCLAVE.DisplayName: …' (sin tocar CLAVExxx)."""
    s = lee(ruta)
    pat = re.compile(r'\n\t' + re.escape(clave) + r'\.DisplayName: [^\n]*')
    s2, n = pat.subn('', s)
    assert n == 1, f'línea {clave} × {n} en {ruta}'
    escribe(ruta, s2)
    print(f'  ✔ {os.path.basename(ruta)}: línea {clave}.DisplayName borrada')


print('=== (1) LOS ARCHIVOS MUERTOS ===')
MUERTOS = [
    'Content/Weapons/Investigaciones/ArbitradorII.cs',
    'Content/Weapons/Investigaciones/ArbitradorII.png',
    'Content/Weapons/Investigaciones/ElPrincipioYElFin.cs',
    'Content/Weapons/Investigaciones/ElPrincipioYElFin.png',
    'Content/Projectiles/Investigaciones/ArbitradorIIProyectil.cs',
    'Content/Projectiles/Investigaciones/DagaEterna.cs',
    'Content/Players/Investigaciones/SigiloRatero.cs',
    'Content/Buffs/Investigaciones/BuffsEternos.cs',
    'Content/Buffs/Investigaciones/MarcaDelPrincipio.png',
    'Content/Buffs/Investigaciones/MiradaDelFin.png',
    'Content/Buffs/Investigaciones/SupresionVital.png',
    'Content/Buffs/Investigaciones/Servidumbre.png',
    'Content/Globals/Investigaciones/InvestigacionesGlobalNPC.cs',
]
for m in MUERTOS:
    p = os.path.join(RAIZ, m)
    assert os.path.exists(p), f'no existe: {m}'
    os.remove(p)
    print(f'  ✝ {m}')
# las carpetas que quedaron vacías
for d in ['Content/Buffs/Investigaciones', 'Content/Players/Investigaciones',
          'Content/Globals/Investigaciones', 'Content/Projectiles/Investigaciones']:
    p = os.path.join(RAIZ, d)
    assert not os.listdir(p), f'{d} NO está vacía: {os.listdir(p)}'
    os.rmdir(p)
    print(f'  ✝ {d}/ (carpeta vacía)')

print('=== (2) LAS MENCIONES DE LOS QUE SE QUEDAN ===')
# --- BolsasCategorias: el bloque v6.124/v6.133 + las 2 entregas ---
BOLSAS = os.path.join(RAIZ, 'Content/Items/Bolsas/BolsasCategorias.cs')
reemplaza(BOLSAS, '''            // v6.124/v6.133 — LAS ARMAS DE INVESTIGACIÓN de la letra de
            // la sesión (el Arbitrador II y el Principio y el Fin,
            // fichas del wiki de Calamity Entropy: la jabalina que
            // atraviesa muros y estalla con su sigilosa que SE CLAVA y
            // detona la X, y las dagas de oro y azul con sus marcas de
            // 8 capas y el canje de almas). EL DAZZLING STABBER STAFF
            // cayó con su arma en la v6.133 (la letra: «puedes borrar
            // Dazzling Stabber Staff de Calamity Mod del codigo y
            // cualquier referencia» — su máquina ya vive entera en las
            // CUCHILLAS DEL ETERNO).
            l.Add((ModContent.ItemType<Weapons.Investigaciones.ArbitradorII>(), 1));
            l.Add((ModContent.ItemType<Weapons.Investigaciones.ElPrincipioYElFin>(), 1));
''', '''            // v6.136 — LA PURGA DE LAS DOS ARMAS DE INVESTIGACIÓN (la
            // letra: «Arbitrador II, El Principio y el Fin, puedes
            // borrar esas armas, ya no son útiles, el resto de
            // contenido de la carpeta se queda tal cual»): murieron
            // con su jabalina, sus dagas del canje, sus marcas y su
            // sigilo — el resto de la carpeta VIVE tal cual (la Mirada
            // del Eterno, el Cetro de las Dagas y sus cinco cuchillas,
            // el Códice y el motor de píxeles).
''')

# --- CetroDagasEterno: el RestOffsetAngle y la secuencia ---
CETRO = os.path.join(RAIZ, 'Content/Weapons/Investigaciones/CetroDagasEterno.cs')
reemplaza(CETRO,
          '''            // LAS DAGAS que faltan (la PRIMERA que falte de cada tipo — la
            // secuencia del stabber: el set se mantiene COMPLETO)''',
          '''            // LAS DAGAS que faltan (la PRIMERA que falte de cada tipo — la
            // secuencia de las cuchillas: el set se mantiene COMPLETO)''')
reemplaza(CETRO,
          '''                // v6.132 — ai[1] ES EL RestOffsetAngle DEL STABBER
                // (Calamity): el ángulo del puesto de descanso en el
                // abanico — las cinco repartidas (k/5 · 2π − π/2: la''',
          '''                // v6.132 — ai[1] ES EL RestOffsetAngle: el ángulo del
                // puesto de descanso en el abanico — las cinco
                // repartidas (k/5 · 2π − π/2: la''')

# --- DagaEternaMinion: la cita v6.133 del arma ajena ---
MINION = os.path.join(RAIZ, 'Content/Projectiles/DagaEternaMinion.cs')
reemplaza(MINION,
          '''        // v6.133 — LA MÁQUINA DEL STABBER ES DE LA CASA (la letra:
        // «con el comportamiento ya creado de las cuchillas, puedes
        // borrar Dazzling Stabber Staff de Calamity Mod del codigo y
        // cualquier referencia»): EL ARMA DE INVESTIGACIÓN murió con sus
        // sprites y su buff — la MÁQUINA VIVE porque ya es nuestra: la
        // casa la forjó en la v6.132 para curar el ataque muerto (LAS''',
          '''        // v6.133/v6.136 — LA MÁQUINA DE LAS CUCHILLAS ES 100% DE LA
        // CASA (la v6.132 la forjó de cero; la v6.136 cerró la
        // historia: las armas ajenas murieron y esta máquina VIVE
        // porque no debe nada a nadie): la casa la forjó para curar
        // el ataque muerto (LAS''')

# --- LA-SUPERFORJA.md: la palabra prestada del cristal ---
reemplaza(os.path.join(REPO, 'LA-SUPERFORJA.md'),
          'la «Dazzling» estilete', 'la estilete deslumbrante')

print('=== (3) hjson ×3 ===')
for lang in ('es-ES', 'es-MX', 'en-US'):
    H = os.path.join(RAIZ, f'Localization/{lang}_Mods.AethonMod.hjson')
    # los bloques de las 2 armas
    borra_bloque_hjson(H, 'ArbitradorII')
    borra_bloque_hjson(H, 'ElPrincipioYElFin')
    # los buffs de las marcas
    borra_bloque_hjson(H, 'MarcaDelPrincipio')
    borra_bloque_hjson(H, 'MiradaDelFin')
    borra_bloque_hjson(H, 'SupresionVital')
    borra_bloque_hjson(H, 'Servidumbre')
    # los proyectiles muertos (¡DagaEterna PUNTO, no DagaEternaMinion!)
    borra_linea_hjson(H, 'ArbitradorIIProyectil')
    borra_linea_hjson(H, 'DagaEterna')
    borra_linea_hjson(H, 'OndaSonoraEterna')
    borra_linea_hjson(H, 'AlmaLuzInferior')
    borra_linea_hjson(H, 'AlmaEco')

# el comentario huérfano de es-MX (la copia local de los efectos…)
reemplaza(os.path.join(RAIZ, 'Localization/es-MX_Mods.AethonMod.hjson'),
          '''	// v6.124 — LOS BUFFS DE LAS INVESTIGACIONES (la copia local de los
	// efectos de Calamity y Calamity Entropy).
''', '')

# los tooltips del muñeco (ya no es el de Terraria — es de la casa)
for lang, viejo, nuevo in (
    ('es-ES', 'Tooltip: El mismo muñeco de Terraria… pero TEÑIDO DE NEGRO',
     'Tooltip: El muñeco de entrenamiento de la casa — NEGRO-VIOLETA de la forja'),
    ('es-MX', 'Tooltip: El mismo muñeco de Terraria… pero TEÑIDO DE NEGRO',
     'Tooltip: El muñeco de entrenamiento de la casa — NEGRO-VIOLETA de la forja'),
    ('en-US', 'Tooltip: The same dummy as always… DYED BLACK',
     'Tooltip: The house’s own training dummy — forged in black-violet'),
):
    reemplaza(os.path.join(RAIZ, f'Localization/{lang}_Mods.AethonMod.hjson'), viejo, nuevo)

print('=== (4) EL MUÑECO DE LA CASA ===')
NPC = os.path.join(RAIZ, 'Content/NPCs/DummyNegroNPC.cs')
ITEM = os.path.join(RAIZ, 'Content/Items/Llamados/DummyNegroItem.cs')

# --- el NPC: el override muere, el sprite es de la forja ---
reemplaza(NPC, '''    //   · EL MISMO SPRITE que el dummy de Terraria normal
    //     (Terraria/Images/NPC_113) pero TEÑIDO DE NEGRO (el
    //     negro-violeta de la casa, con su resplandor morado para
    //     leerlo de noche) — dibujado a mano en PreDraw con el tinte.''',
          '''    //   · EL SPRITE DE LA CASA (v6.136 — la letra: «Genera las
    //     texturas con SForja»): el muñeco de paja NEGRO-VIOLETA nació
    //     en LA FORJA — cero texturas del juego base (la regla de la
    //     casa: todo el arte es 100% nuestro).''')
reemplaza(NPC, '''        /// <summary>EL SPRITE DE TERRARIA NORMAL — la textura vanilla
        /// del Target Dummy (NPC_113), teñida de negro en el dibujo.</summary>
        public override string Texture => "Terraria/Images/NPC_113";''',
          '''        /// <summary>EL SPRITE DE LA CASA — el muñeco negro de la
        /// forja (DummyNegroNPC.png junto a esta clase).</summary>''')
reemplaza(NPC, '            Main.npcFrameCount[Type] = 1;   // quieto: el frame 0 del vanilla',
          '            Main.npcFrameCount[Type] = 1;   // quieto: UN solo marco (el de la forja)')
reemplaza(NPC, '''                // EL FRAME 0 DEL SPRITE VANILLA — la tira puede tener
                // varios marcos (el wobble del dummy base): el de pie.
                int framesVanilla = Math.Max(1, Main.npcFrameCount[NPCID.TargetDummy]);
                int altoFrame = tex.Height / framesVanilla;
                var marco = new Rectangle(0, 0, tex.Width, altoFrame);''',
          '''                // EL SPRITE DE LA CASA — un solo marco: el muñeco
                // negro de la forja (ya nació negro-violeta).
                var marco = new Rectangle(0, 0, tex.Width, tex.Height);''')
reemplaza(NPC, '''                // (2) EL MUÑECO TEÑIDO DE NEGRO — el sprite vanilla
                //     multiplicado por el negro-violeta de la casa: la
                //     silueta del dummy de siempre, en negro (con el
                //     matiz morado que lo delata como del grimorio).
                Color tinte = new Color(52, 44, 74);
                spriteBatch.Draw(tex, pos, marco, tinte, balanceo, origen,
                    1f, SpriteEffects.None, 0f);''',
          '''                // (2) EL MUÑECO — el sprite de la casa tal cual (ya
                //     nació negro-violeta en la forja), bañado por la
                //     luz del mundo (drawColor).
                spriteBatch.Draw(tex, pos, marco, drawColor, balanceo, origen,
                    1f, SpriteEffects.None, 0f);''')
reemplaza(NPC, '''                // el dibujo JAMÁS tumba el frame — si algo falla, el
                // sprite crudo del vanilla sale por el return true.''',
          '''                // el dibujo JAMÁS tumba el frame — si algo falla, el
                // sprite crudo sale por el return true.''')

# --- el ítem: prestado del NPC por el override de la casa ---
reemplaza(ITEM, '''    //  EL ÍTEM: clic y el muñeco nace en el tile del cursor (la misma
    //  mecánica del ítem vanilla 3202). El icono ES el sprite del
    //  dummy de Terraria normal TEÑIDO DE NEGRO (dibujado a mano en
    //  PreDrawInInventory — cero forja: el arte es de vanilla, el
    //  tinte de la casa).''',
          '''    //  EL ÍTEM: clic y el muñeco nace en el tile del cursor. El icono
    //  ES el sprite del muñeco de la casa (v6.136 — la letra:
    //  «Genera las texturas con SForja»: nació en la forja; el ítem lo
    //  toma prestado del NPC con el override de la casa).''')
reemplaza(ITEM, '''        /// <summary>EL SPRITE DE TERRARIA NORMAL (el mismo del NPC).</summary>
        public override string Texture => "Terraria/Images/NPC_113";''',
          '''        /// <summary>EL SPRITE DE LA CASA — prestado del NPC por el
        /// override de la casa (el muñeco negro de la forja).</summary>
        public override string Texture => "AethonMod/Content/NPCs/DummyNegroNPC";''')
reemplaza(ITEM, '''            // EL ICONO — el sprite del dummy de TERRARIA NORMAL teñido de
            // negro-violeta (la casa), el frame de pie, escalado al slot.
            Texture2D tex = TextureAssets.Item[Type].Value;
            if (tex == null || tex.IsDisposed)
                return true;
            int framesVanilla = System.Math.Max(1, Main.npcFrameCount[NPCID.TargetDummy]);
            int altoFrame = tex.Height / framesVanilla;
            var marco = new Rectangle(0, 0, tex.Width, altoFrame);

            float esc = scale * 28f / System.MathF.Max(marco.Width, altoFrame);
            Color tinte = new Color(64, 54, 92);
            spriteBatch.Draw(tex, position, marco, tinte, 0f,
                marco.Size() * 0.5f, esc, SpriteEffects.None, 0f);''',
          '''            // EL ICONO — el sprite de la casa (el muñeco negro de la
            // forja), escalado al slot.
            Texture2D tex = TextureAssets.Item[Type].Value;
            if (tex == null || tex.IsDisposed)
                return true;
            var marco = new Rectangle(0, 0, tex.Width, tex.Height);

            float esc = scale * 28f / System.MathF.Max(marco.Width, marco.Height);
            spriteBatch.Draw(tex, position, marco, drawColor, 0f,
                marco.Size() * 0.5f, esc, SpriteEffects.None, 0f);''')
reemplaza(ITEM, '''            Texture2D tex = TextureAssets.Item[Type].Value;
            if (tex == null || tex.IsDisposed)
                return true;
            int framesVanilla = System.Math.Max(1, Main.npcFrameCount[NPCID.TargetDummy]);
            int altoFrame = tex.Height / framesVanilla;
            var marco = new Rectangle(0, 0, tex.Width, altoFrame);

            // EL ÍTEM EN EL SUELO — el muñeco negro tirado (teñido).
            spriteBatch.Draw(tex, Item.position - Main.screenPosition, marco,
                new Color(52, 44, 74), rotation, marco.Size() * 0.5f, scale,
                SpriteEffects.None, 0f);''',
          '''            Texture2D tex = TextureAssets.Item[Type].Value;
            if (tex == null || tex.IsDisposed)
                return true;
            var marco = new Rectangle(0, 0, tex.Width, tex.Height);

            // EL ÍTEM EN EL SUELO — el muñeco negro tirado.
            spriteBatch.Draw(tex, Item.position - Main.screenPosition, marco,
                lightColor, rotation, marco.Size() * 0.5f, scale,
                SpriteEffects.None, 0f);''')

print('=== (5) LA VERSIÓN ===')
reemplaza(os.path.join(RAIZ, 'build.txt'), 'version = 6.135', 'version = 6.136')

print()
print('=== LA VERIFICACIÓN INMEDIATA (cero menciones en código+loc) ===')
MALOS = re.compile(r'calamity|entropy|miraheze|arbitrat|stabber|dazzling'
                   r'|arbitradorii|elprincipioyelfin|sigiloratero'
                   r'|marcadelprincipio|miradadelfin|supresionvital|servidumbre'
                   r'|almaec|almaluzinferior|ondasonoraeterna'
                   r'|stacksprincipio|stacksfin|investigacionesglobalnpc', re.I)
sucios = []
for base, _, archivos in os.walk(RAIZ):
    for a in archivos:
        if a.endswith(('.cs', '.hjson')):
            p = os.path.join(base, a)
            for i, linea in enumerate(lee(p).split('\n'), 1):
                if MALOS.search(linea):
                    sucios.append(f'{p}:{i}: {linea.strip()[:90]}')
assert not sucios, 'MENCIONES VIVAS:\n' + '\n'.join(sucios[:20])
print('  ✔ CERO menciones externas ni condenadas en los .cs/.hjson')

# el barrido del PNG vanilla
restantes = []
for base, _, archivos in os.walk(RAIZ):
    for a in archivos:
        if a.endswith('.cs'):
            p = os.path.join(base, a)
            if '"Terraria/' in lee(p):
                restantes.append(p)
assert not restantes, f'Texturas vanilla vivas: {restantes}'
print('  ✔ CERO texturas vanilla en los .cs')

print()
print('CIRUGÍA v6.136 COMPLETA ✔ — falta el sprite del muñeco (SForja)')
