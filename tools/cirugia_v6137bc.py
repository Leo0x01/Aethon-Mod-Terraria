#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6137bc.py — MÓDULOS B + C: LOS COLMILLOS DE LAS CUCHILLAS + EL
MUÑECO DE LA CASA (receta · bruma · los 999).

MÓDULO B — LA LETRA: «sobre las cuchillas, y si son colmillos, el
proyecto ya tiene textura de colmillo en algun lugar». CIERTO:
ColmilloAbismo.png (48×96, la aguja de marfil con la encía violeta en la
base — la usan las Sombras de la página 5). LA CIRUGÍA: el dibujado de
DagaEternaMinion cambia la hoja DagaEterna_Hoja (34×34×5) por EL
COLMILLO — cada tipo tiñe el marfil con SU alma (el tinte de la
estela), el ojo minion vive EN LA BASE (la encía) y la máquina queda
INTACTA (la IA, las fases, el descanso, los golpes — solo cambia la
PIEL).

MÓDULO C — LA LETRA: «hacer receta de muñeco de prueba negro, que sean
solo 5 de madera y 5 esencia del eterno» + «algo que vi en el muñeco es
que tiene algo asi como un circulo plano, acaso ese circulo es bruma,
de ser asi usar la libreria correspondiente, recuerda la regla de la
casa, nada de geometria plana para representar algo» + «dale al jugador
999 muñecos de prueba negros».
  · EL CÍRCULO ERA EL SoftGlow estirado del resplandor nocturno —
    GEOMETRÍA PLANA (la regla rota). MUERE. Su lugar: BrumaFX.Puff (LA
    biblioteca de brumas de la casa) latiendo bajo el muñeco — la
    bruma violeta del grimorio, de verdad.
  · LA RECETA: 5 madera + 5 Esencia del Eterno (ItemCorrupto), en la
    mano (la casa no exige estación).
  · LOS 999: en el regalo de pruebas (EntregarSiNoTiene no sirve —
    necesita stack: se entrega el stack entero si NO lleva ya 999).
"""
import os

RAIZ = '/home/z/my-project/mod/AethonMod'


def lee(ruta):
    return open(ruta, encoding='utf-8').read()


def escribe(ruta, s):
    open(ruta, 'w', encoding='utf-8').write(s)


def reemplaza(ruta, viejo, nuevo, veces=1):
    s = lee(ruta)
    n = s.count(viejo)
    assert n == veces, f'× {n} (esperaba {veces}): {viejo[:70]!r} en {ruta}'
    escribe(ruta, s.replace(viejo, nuevo))
    print(f'  ✔ {os.path.basename(ruta)}: {viejo[:52]!r}…')


# ============================================================
# MÓDULO B — LOS COLMILLOS DE LAS CUCHILLAS
# ============================================================
P = os.path.join(RAIZ, 'Content/Projectiles/DagaEternaMinion.cs')
s = lee(P)

VIEJO_DIBUJO = '''                Texture2D tex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/DagaEterna_Hoja", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                int frameH = tex.Height / TiposMax;
                Rectangle rect = new Rectangle(0, Tipo * frameH, tex.Width, frameH);
                Vector2 pos = Projectile.Center - Main.screenPosition;
                Vector2 orig = new Vector2(tex.Width * 0.5f, frameH * 0.5f);
                float rot = Projectile.rotation;
                // v6.131 — LA CORONA GRANDE: la hoja ×1,4 (ESCALA_TIARA
                // vive en Projectile.scale — el golpe y el dibujado
                // crecen JUNTOS con la hoja).
                float esc = Projectile.scale;

                // LA ESTELA DEL GOLPE (el rastro de la casa): tres
                // fantasmas ATRÁS mientras VUELA al ataque — v6.132: la
                // máquina de las cuchillas no tiene fases; la estela vive
                // cuando el cuchillo NO descansa (la ronda abierta).
                if (attackCooldown == 0 && Projectile.velocity.LengthSquared() > 25f)
                {
                    for (int j = 1; j <= 3; j++)
                    {
                        float a = (0.30f / j);
                        Main.spriteBatch.Draw(tex, pos - new Vector2(0f, j * 7f).RotatedBy(rot - MathHelper.PiOver2),
                            rect, LuzDe(Tipo) * a, rot, orig, esc - j * 0.17f, SpriteEffects.None, 0f);
                    }
                }

                // LA CUCHILLA (el marco de SU tipo del spritesheet — la
                // hoja SIN mango con el agujero taladrado al centro)
                Main.spriteBatch.Draw(tex, pos, rect, Color.White, rot, orig, esc, SpriteEffects.None, 0f);'''

NUEVO_DIBUJO = '''                // ==============================================================
                //  v6.137 — EL COLMILLO (la letra: «sobre las cuchillas, y
                //  si son colmillos, el proyecto ya tiene textura de
                //  colmillo en algun lugar»): la hoja MURIÓ — cada
                //  cuchilla viste EL COLMILLO DE LA FORJA
                //  (ColmilloAbismo.png 48×96: la aguja de marfil con la
                //  encía violeta en la base), apuntando con la PUNTA al
                //  rumbo del golpe. El tinte de la estela pinta EL ALMA
                //  de cada tipo sobre el marfil; la máquina queda
                //  INTACTA (solo cambia la piel).
                // ==============================================================
                Texture2D tex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/ColmilloAbismo", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                Rectangle rect = new Rectangle(0, 0, tex.Width, tex.Height);
                Vector2 pos = Projectile.Center - Main.screenPosition;
                // EL COLMILLO APUNTA CON LA PUNTA: la textura mira ARRIBA
                // (la convención de SombrasLib.ColmilloTexturizado) — el
                // origen en la BASE (la encía) y la rotación del golpe.
                Vector2 orig = new Vector2(tex.Width * 0.5f, tex.Height * 0.88f);
                float rot = Projectile.rotation + MathHelper.PiOver2;
                // v6.131 — LA CORONA GRANDE: la hoja ×1,4 (ESCALA_TIARA
                // vive en Projectile.scale — el golpe y el dibujado
                // crecen JUNTOS con la hoja).
                float esc = Projectile.scale * 0.62f;   // el colmillo es LARGO (96 px): ×0,62 para la talla de la cuchilla

                // LA ESTELA DEL GOLPE (el rastro de la casa): tres
                // fantasmas ATRÁS mientras VUELA al ataque — v6.132: la
                // máquina de las cuchillas no tiene fases; la estela vive
                // cuando el cuchillo NO descansa (la ronda abierta).
                if (attackCooldown == 0 && Projectile.velocity.LengthSquared() > 25f)
                {
                    for (int j = 1; j <= 3; j++)
                    {
                        float a = (0.30f / j);
                        Main.spriteBatch.Draw(tex, pos - new Vector2(0f, j * 7f).RotatedBy(rot - MathHelper.PiOver2),
                            rect, LuzDe(Tipo) * a, rot, orig, esc - j * 0.17f, SpriteEffects.None, 0f);
                    }
                }

                // EL COLMILLO (el marfil de la forja con el ALMA de su
                // tipo — el tinte SUAVE: el marfil se lee, el alma asoma)
                Color tintaColmillo = Color.White;
                try { tintaColmillo = Color.Lerp(Color.White, LuzDe(Tipo), 0.38f); }
                catch { }
                Main.spriteBatch.Draw(tex, pos, rect, tintaColmillo, rot, orig, esc, SpriteEffects.None, 0f);'''

if 'ColmilloAbismo", ReLogic' in s and 'EL COLMILLO (la letra' in s:
    print('  · B: el colmillo ya vive (idempotente)')
else:
    assert VIEJO_DIBUJO in s, 'el dibujado viejo de la hoja no está'
    s = s.replace(VIEJO_DIBUJO, NUEVO_DIBUJO)

    # EL TRONO DEL OJO: el ojo vive en la BASE del colmillo (la encía)
    VIEJO_TRONO = '''        private static readonly Vector2[] TronoDelOjo =
        {
            new(-4.5f, 0.5f),    // cristal (la curva luna lo baja a la izquierda)
            new(0.5f, -0.5f),    // llama (el serpenteo lo deja casi al centro)
            new(-5.5f, -1.5f),   // roca (el cincel ancho, arriba-izquierda)
            new(-2.5f, -1.5f),   // vacío (la puerta lo pone a la izquierda)
            new(-0.5f, 1.5f),    // veredicto (la balanza lo baja al pie)
        };'''
    NUEVO_TRONO = '''        // v6.137 — EL TRONO DEL OJO EN LA ENCÍA: el colmillo apunta con
        // la punta y su BASE (la encía violeta) es donde el OJO del
        // grimorio vive — el mismo espíritu del trasplante v6.130, en
        // la nueva piel.
        private static readonly Vector2[] TronoDelOjo =
        {
            new(-4.5f, -12.5f),  // cristal (en la encía, un pelo a la izquierda)
            new(0.5f, -13.5f),   // llama (casi al centro de la encía)
            new(-5.5f, -14.5f),  // roca (arriba-izquierda de la encía)
            new(-2.5f, -14.5f),  // vacío (la puerta lo pone a la izquierda)
            new(-0.5f, -11.5f),  // veredicto (la balanza lo baja al pie)
        };'''
    assert VIEJO_TRONO in s, 'el trono viejo no está'
    s = s.replace(VIEJO_TRONO, NUEVO_TRONO)
    escribe(P, s)
    print('  ✔ DagaEternaMinion.cs: el colmillo de la forja (dibujado + trono)')

# ============================================================
# MÓDULO C — EL MUÑECO (bruma · receta · los 999)
# ============================================================
NPC = os.path.join(RAIZ, 'Content/NPCs/DummyNegroNPC.cs')
s = lee(NPC)

# (C1) EL CÍRCULO PLANO MUERE → BrumaFX.Puff (la librería de la casa)
VIEJO_GLOW = '''                // (1) EL RESPLANDOR MORADO — la lectura nocturna del
                //     negro de la casa (un halo suave bajo el lote alfa
                //     del propio NPC).
                Texture2D glow = VFXCore.SoftGlow;
                if (glow != null && !glow.IsDisposed)
                {
                    spriteBatch.Draw(glow, pos - new Vector2(0f, marco.Height * 0.5f),
                        null, new Color(66, 32, 108) * 0.42f, 0f,
                        glow.Size() * 0.5f, new Vector2(marco.Width * 2.6f / glow.Width,
                            marco.Height * 2.2f / glow.Height), SpriteEffects.None, 0f);
                }'''
NUEVO_GLOW = '''                // (1) LA BRUMA DEL GRIMORIO (v6.137 — la letra: «tiene
                //     algo asi como un circulo plano, acaso ese circulo
                //     es bruma, de ser asi usar la libreria
                //     correspondiente, recuerda la regla de la casa,
                //     nada de geometria plana»): el SoftGlow estirado
                //     MURIÓ — era geometría plana. La bruma de verdad es
                //     BrumaFX.Puff (LA biblioteca de la casa) latiendo
                //     alrededor del cuerpo: el banco de pruebas respira
                //     envuelto en la niebla violeta del vacío.
                //     (En el lote del NPC: BrumaFX cierra y reabre sus
                //     propios lotes — el contrato de la casa.)
                try
                {
                    VFXCore.CerrarLoteSiAbierto();
                    BrumaFX.Puff(
                        NPC.Center - new Vector2(0f, marco.Height * 0.42f),
                        34f + (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 0.9f) * 4f,
                        new Color(96, 58, 148), NPC.whoAmI * 97 + 13,
                        Main.GlobalTimeWrappedHourly, 0.42f, 0.85f);
                    VFXCore.ReabrirLoteVanilla();
                }
                catch { try { VFXCore.ReabrirLoteVanilla(); } catch { } }'''
if 'BrumaFX.Puff' in s:
    print('  · C1: la bruma ya vive (idempotente)')
else:
    assert VIEJO_GLOW in s, 'el glow plano no está'
    # el using de la bruma
    if 'using AethonMod.Content.Effects.Bruma;' not in s:
        s = s.replace('using AethonMod.Content.VFX;',
                      'using AethonMod.Content.VFX;\nusing AethonMod.Content.Effects.Bruma;')
    s = s.replace(VIEJO_GLOW, NUEVO_GLOW)
    escribe(NPC, s)
    print('  ✔ DummyNegroNPC.cs: el círculo plano murió → BrumaFX.Puff')

# (C2) LA RECETA: 5 madera + 5 Esencia del Eterno
ITEM = os.path.join(RAIZ, 'Content/Items/Llamados/DummyNegroItem.cs')
s = lee(ITEM)
if 'AddRecipes' in s:
    print('  · C2: la receta ya vive (idempotente)')
else:
    VIEJO_FIN = '''        public override bool CanUseItem(Player player)
        {'''
    NUEVO_FIN = '''        // ==============================================================
        //  v6.137 — LA RECETA DEL BANCO (la letra: «hacer receta de
        //  muñeco de prueba negro, que sean solo 5 de madera y 5
        //  esencia del eterno»): la madera del espantapájaros y la
        //  esencia del Eterno — en la mano de cualquiera (la casa no
        //  exige estación para las herramientas).
        // ==============================================================
        public override void AddRecipes()
        {
            CreateRecipe(5)
                .AddIngredient(ItemID.Wood, 5)
                .AddIngredient(ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.ItemCorrupto>(), 5)
                .Register();
        }

        public override bool CanUseItem(Player player)
        {'''
    assert VIEJO_FIN in s
    # el using de ItemID ya vive (Terraria.ID). ModContent ya vive.
    s = s.replace(VIEJO_FIN, NUEVO_FIN, 1)
    escribe(ITEM, s)
    print('  ✔ DummyNegroItem.cs: la receta 5 madera + 5 esencias (lote de 5)')

# (C3) LOS 999 — en el regalo de pruebas de ShardPlayer
SP = os.path.join(RAIZ, 'Content/Players/ShardPlayer.cs')
s = lee(SP)
if 'DummyNegroItem' in s:
    print('  · C3: los 999 ya viven (idempotente)')
else:
    # el ancla: la entrega de la v6.105 del muñeco si existe; si no, la
    # primera entrega del regalo (FormaAscendidaDos)
    ancla = '                entregados += EntregarSiNoTiene(ModContent.ItemType\n                    <global::AethonMod.Content.Items.Cosmetics.FormaAscendidaDosItem>());'
    assert ancla in s, 'el ancla del regalo no está'
    NUEVO = '''                // ==============================================================
                //  v6.137 — LOS 999 MUÑECOS DE PRUEBAS (la letra: «dale
                //  al jugador 999 muñecos de prueba negros»): el banco
                //  de pruebas del grimorio A GRANEL — un stack de 999
                //  (se rellena SOLO si el jugador no lleva ya uno
                //  completo: la entrega es una vez por guardado).
                // ==============================================================
                try
                {
                    int tipoDummy = ModContent.ItemType
                        <global::AethonMod.Content.Items.Llamados.DummyNegroItem>();
                    bool yaLlena = false;
                    for (int k = 0; k < 50; k++)
                    {
                        Item inv = Player.inventory[k];
                        if (inv != null && !inv.IsAir && inv.type == tipoDummy &&
                            inv.stack >= 999) { yaLlena = true; break; }
                    }
                    if (!yaLlena)
                    {
                        int idx99 = Item.NewItem(Player.GetSource_GiftOrReward(),
                            Player.Hitbox, tipoDummy, 999);
                        if (idx99 >= 0 && idx99 < Main.maxItems)
                        {
                            Main.item[idx99].noGrabDelay = 0;
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                                NetMessage.SendData(MessageID.ItemDrop, -1, -1, null, idx99);
                        }
                        entregados++;
                    }
                }
                catch { }
''' + ancla
    s = s.replace(ancla, NUEVO, 1)
    escribe(SP, s)
    print('  ✔ ShardPlayer.cs: los 999 muñecos en el regalo')

print()
print('MÓDULOS B + C COMPLETOS ✔')
