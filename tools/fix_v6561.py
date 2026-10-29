#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.56.1 — LA RONDA DE CURACIÓN tras la auditoría de los 4 agentes (4-a/4-b/4-c/4-d).

Cirugía QUIRÚRGICA por reemplazo exacto con aserciones: si un patrón no
aparece EXACTAMENTE las veces esperadas, el archivo NO se escribe y el
script aborta. Nada de regex suelta sobre el código de la casa.

Uso:  python3 tools/fix_v6561.py   (desde la raíz del repo)
"""
import sys, os

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MOD = os.path.join(RAIZ, "AethonMod")

reporte = []

def cirugia(ruta_relativa, fixes, obligatorios=None):
    """Aplica lista de (nombre, viejo, nuevo, veces_esperadas)."""
    ruta = os.path.join(MOD, ruta_relativa)
    with open(ruta, encoding="utf-8") as f:
        src = f.read()
    original = src
    for nombre, viejo, nuevo, veces in fixes:
        # v6.56.2 — LECCIÓN: las curas de PURA ELIMINACIÓN (nuevo ⊂ viejo)
        # NO pueden usar este chequeo — el texto "nuevo" existe como subcadena
        # del viejo y el fix se salta en silencio (así sobrevivió la 6ª
        # recursión del Partidor). Para eliminaciones: aserción de conteo
        # directa, sin atajo de idempotencia.
        if src.count(nuevo) >= veces and src.count(viejo) == 0:
            reporte.append((ruta_relativa, nombre + " [ya estaba]", veces))
            continue
        encontrados = src.count(viejo)
        if encontrados != veces:
            print(f"FALLO [{ruta_relativa}] {nombre}: esperado {veces}, hallado {encontrados}")
            print("--- patron ---")
            print(viejo[:400])
            sys.exit(1)
        src = src.replace(viejo, nuevo)
        reporte.append((ruta_relativa, nombre, veces))
    if src != original:
        with open(ruta, "w", encoding="utf-8") as f:
            f.write(src)
    print(f"OK  {ruta_relativa}: {len(fixes)} fixes")

# ============================================================================
# 1. CRÍTICO — LA RECURSIÓN SendExtraAI/ReceiveExtraAI (StackOverflow en MP)
#    (el decompilador convirtió el base-call en auto-llamada virtual)
# ============================================================================
CURA_RECURSION_TX = (
    "            writer.Write(((Entity)((ModProjectile)this).Projectile).direction == 1);\n"
    "            ((ModProjectile)this).SendExtraAI(writer);",
    "            writer.Write(((Entity)((ModProjectile)this).Projectile).direction == 1);\n"
    "            // v6.56.1 — LA CURA DE LA RECURSIÓN: el decompilador convirtió\n"
    "            // el base-call en auto-llamada virtual (callvirt) — StackOverflow\n"
    "            // en el primer sync de MP. La base es vacía: NO hay nada que llamar.\n"
    "            writer.Write(targetRotation);   // v6.56.1: la mira del dueño viaja al server",
)
CURA_RECURSION_RX = (
    "            ((Entity)((ModProjectile)this).Projectile).direction = (reader.ReadBoolean() ? 1 : (-1));\n"
    "            ((ModProjectile)this).ReceiveExtraAI(reader);",
    "            ((Entity)((ModProjectile)this).Projectile).direction = (reader.ReadBoolean() ? 1 : (-1));\n"
    "            targetRotation = reader.ReadSingle();   // v6.56.1: la mira sincronizada",
)

cirugia("Content/Projectiles/Definitivas/RayoConcentradoNivelProjectile.cs", [
    ("recursion-send", CURA_RECURSION_TX[0], CURA_RECURSION_TX[1], 1),
    ("recursion-receive", CURA_RECURSION_RX[0], CURA_RECURSION_RX[1], 1),
    # LA MIRA EN MP: solo el dueño apunta con su ratón
    ("mira-mp",
     "            targetRotation = Utils.AngleLerp(targetRotation, ((Entity)((ModProjectile)this).Projectile).AngleTo(Main.MouseWorld), Utils.GetLerpValue((float)Carga + (float)Laser * 0.1f, (float)Carga, Time, true) * 0.04f + Utils.GetLerpValue((float)Carga + (float)Laser * 0.3f, (float)Carga + (float)Laser * 0.6f, Time, true) * 0.01f);",
     "            // v6.56.1 — LA MIRA EN MP: solo el DUEÑO apunta con SU ratón; el\n"
     "            // servidor y el resto usan la rotación que viaja por ExtraAI.\n"
     "            if (Main.netMode != 2 && Main.myPlayer == ((Entity)((ModProjectile)this).Projectile).owner)\n"
     "            {\n"
     "                targetRotation = Utils.AngleLerp(targetRotation, ((Entity)((ModProjectile)this).Projectile).AngleTo(Main.MouseWorld), Utils.GetLerpValue((float)Carga + (float)Laser * 0.1f, (float)Carga, Time, true) * 0.04f + Utils.GetLerpValue((float)Carga + (float)Laser * 0.3f, (float)Carga + (float)Laser * 0.6f, Time, true) * 0.01f);\n"
     "            }",
     1),
    # LA SALIDA DEL PREDRAW: restaurar el lote de entidades (cura del aditivo)
    ("salida-lote",
     "            VFXCore.ReabrirLoteVanilla();\n            return false;",
     "            VFXCore.RestaurarLoteVanilla();\n            return false;",
     1),
])

cirugia("Content/Projectiles/Definitivas/RayoConcentradoDosProjectile.cs", [
    ("recursion-send", CURA_RECURSION_TX[0], CURA_RECURSION_TX[1], 1),
    ("recursion-receive", CURA_RECURSION_RX[0], CURA_RECURSION_RX[1], 1),
    ("mira-mp",
     "            targetRotation = Utils.AngleLerp(targetRotation, ((Entity)((ModProjectile)this).Projectile).AngleTo(Main.MouseWorld), Utils.GetLerpValue((float)ChargeTime + (float)LaserDuration * 0.1f, (float)ChargeTime, Time, true) * 0.04f + Utils.GetLerpValue((float)ChargeTime + (float)LaserDuration * 0.3f, (float)ChargeTime + (float)LaserDuration * 0.6f, Time, true) * 0.01f);",
     "            // v6.56.1 — LA MIRA EN MP: solo el DUEÑO apunta con SU ratón.\n"
     "            if (Main.netMode != 2 && Main.myPlayer == ((Entity)((ModProjectile)this).Projectile).owner)\n"
     "            {\n"
     "                targetRotation = Utils.AngleLerp(targetRotation, ((Entity)((ModProjectile)this).Projectile).AngleTo(Main.MouseWorld), Utils.GetLerpValue((float)ChargeTime + (float)LaserDuration * 0.1f, (float)ChargeTime, Time, true) * 0.04f + Utils.GetLerpValue((float)ChargeTime + (float)LaserDuration * 0.3f, (float)ChargeTime + (float)LaserDuration * 0.6f, Time, true) * 0.01f);\n"
     "            }",
     1),
    # LA AGUJA DE PUNTERÍA (localAI[1] jamás asignada → invisible para siempre)
    ("aguja",
     "            float num2 = Utils.GetLerpValue(0f, (float)ChargeTime, ((ModProjectile)this).Projectile.localAI[1], true) * Utils.GetLerpValue((float)(ChargeTime + 10), (float)(ChargeTime - 30), Time, true);",
     "            // v6.56.1 — LA AGUJA RECUPERADA: localAI[1] jamás se asignaba\n"
     "            // (la aguja de puntería llevaba alfa 0 desde la v6.56.0). El\n"
     "            // gemelo RayoConcentradoNivel usa Time aquí — esta era la idea.\n"
     "            float num2 = Utils.GetLerpValue(0f, (float)ChargeTime, Time, true) * Utils.GetLerpValue((float)(ChargeTime + 10), (float)(ChargeTime - 30), Time, true);",
     1),
    # EL VUELO ES DEL DUEÑO (antes: TODO EL MUNDO mientras CUALQUIER RC2 vivía)
    ("vuelo-dueno",
     "            else if (Main.netMode == 2)\n"
     "            {\n"
     "                Player[] player = Main.player;\n"
     "                foreach (Player val4 in player)\n"
     "                {\n"
     "                    if (val4 != null && ((Entity)val4).active && !val4.dead)\n"
     "                    {\n"
     "                        val4.wingTime = val4.wingTimeMax;\n"
     "                    }\n"
     "                }\n"
     "            }\n"
     "            else if (Main.LocalPlayer != null)\n"
     "            {\n"
     "                Main.LocalPlayer.wingTime = Main.LocalPlayer.wingTimeMax;\n"
     "            }",
     "            // v6.56.1 — EL VUELO ES DEL DUEÑO: antes cualquier RC2 vivo en\n"
     "            // el mundo le rellenaba el wingTime a TODOS los jugadores.\n"
     "            else if (Main.netMode == 2)\n"
     "            {\n"
     "                Player val4 = Main.player[(int)Owner];\n"
     "                if (val4 != null && ((Entity)val4).active && !val4.dead)\n"
     "                {\n"
     "                    val4.wingTime = val4.wingTimeMax;\n"
     "                }\n"
     "            }\n"
     "            else if (Main.LocalPlayer != null && ((Entity)Main.LocalPlayer).whoAmI == (int)Owner)\n"
     "            {\n"
     "                Main.LocalPlayer.wingTime = Main.LocalPlayer.wingTimeMax;\n"
     "            }",
     1),
    # LOS POZOS DEL RC2 (cero-GC): campos estáticos
    ("pozos-campos",
     "        private static Effect ObtenerShader()",
     "        // v6.56.1 — LOS POZOS DEL RC2 (cero-GC): antes cada frame del láser\n"
     "        // alocaba 2 arrays de 1500 + un VertexStrip nuevo (~36 KB/frame de\n"
     "        // basura Gen0 mientras vivía el arma cima). Tamaños constantes.\n"
     "        private static readonly Vector2[] _cachePuntos = (Vector2[])(object)new Vector2[1500];\n"
     "        private static readonly float[] _cacheRotas = new float[1500];\n"
     "        private static readonly Vector2[] _cachePuntos64 = (Vector2[])(object)new Vector2[64];\n"
     "        private static readonly float[] _cacheSemis64 = new float[64];\n"
     "        private static readonly float[] _cacheSemisA = new float[64];\n"
     "        private static readonly float[] _cacheSemisB = new float[64];\n"
     "        private static readonly VertexStrip _strip = new VertexStrip();\n\n"
     "        private static Effect ObtenerShader()",
     1),
    ("pozos-1500",
     "                Vector2[] array = (Vector2[])(object)new Vector2[1500];\n"
     "                float[] array2 = new float[1500];",
     "                Vector2[] array = _cachePuntos;\n"
     "                float[] array2 = _cacheRotas;",
     1),
    ("pozos-strip",
     "                VertexStrip val4 = new VertexStrip();",
     "                VertexStrip val4 = _strip;",
     1),
    ("pozos-64",
     "                    Vector2[] array3 = (Vector2[])(object)new Vector2[64];\n"
     "                    float[] array4 = new float[64];",
     "                    Vector2[] array3 = _cachePuntos64;\n"
     "                    float[] array4 = _cacheSemis64;",
     1),
    ("pozos-semis-07",
     "RayoStrip.FajaTexturadaEnLote(array3, ArraySemis(array4, 0.7f),",
     "RayoStrip.FajaTexturadaEnLote(array3, ArraySemis(array4, 0.7f, _cacheSemisA),",
     1),
    ("pozos-semis-04",
     "RayoStrip.FajaTexturadaEnLote(array3, ArraySemis(array4, 0.4f),",
     "RayoStrip.FajaTexturadaEnLote(array3, ArraySemis(array4, 0.4f, _cacheSemisB),",
     1),
    ("arraysemis",
     "        private static float[] ArraySemis(float[] perfil, float k)\n"
     "        {\n"
     "            float[] array = new float[perfil.Length];\n"
     "            for (int i = 0; i < perfil.Length; i++)\n"
     "            {\n"
     "                array[i] = perfil[i] * k;\n"
     "            }\n"
     "            return array;\n"
     "        }",
     "        private static float[] ArraySemis(float[] perfil, float k, float[] destino)\n"
     "        {\n"
     "            // v6.56.1 — escribe en el pozo del llamador (cero alocaciones)\n"
     "            for (int i = 0; i < perfil.Length; i++)\n"
     "            {\n"
     "                destino[i] = perfil[i] * k;\n"
     "            }\n"
     "            return destino;\n"
     "        }",
     1),
    ("salida-lote",
     "            VFXCore.ReabrirLoteVanilla();\n            return false;",
     "            VFXCore.RestaurarLoteVanilla();\n            return false;",
     1),
])

cirugia("Content/Projectiles/Definitivas/PartidorDeMundosProjectile.cs", [
    ("recursion-send",
     "            writer.Write(_ancho);\n"
     "            writer.Write(_techo);\n"
     "            writer.Write(_piso);\n"
     "            writer.Write(_anclaLista);\n"
     "            ((ModProjectile)this).SendExtraAI(writer);",
     "            writer.Write(_ancho);\n"
     "            writer.Write(_techo);\n"
     "            writer.Write(_piso);\n"
     "            writer.Write(_anclaLista);\n"
     "            // v6.56.1 — LA CURA DE LA RECURSIÓN (StackOverflow en MP):\n"
     "            // el base-call del decompilador era una auto-llamada virtual.\n"
     "            // La base es vacía: no hay nada que llamar.",
     1),
    ("recursion-receive",
     "            _ancho = reader.ReadSingle();\n"
     "            _techo = reader.ReadSingle();\n"
     "            _piso = reader.ReadSingle();\n"
     "            _anclaLista = reader.ReadBoolean();\n"
     "            ((ModProjectile)this).ReceiveExtraAI(reader);",
     "            _ancho = reader.ReadSingle();\n"
     "            _techo = reader.ReadSingle();\n"
     "            _piso = reader.ReadSingle();\n"
     "            _anclaLista = reader.ReadBoolean();",
     1),
    ("salida-lote",
     "            VFXCore.ReabrirLoteVanilla();\n            return false;",
     "            VFXCore.RestaurarLoteVanilla();\n            return false;",
     1),
])

# ============================================================================
# 2. CRÍTICO — ai[2] = whoAmI envenenando el TEMPORIZADOR de las 3 armas nuevas
# ============================================================================
cirugia("Content/Weapons/Definitivas/DefinitivasNuevasV6560.cs", [
    ("ai2-temporizador",
     "Projectile.NewProjectile((IEntitySource)(object)source, player.MountedCenter, Vector2.Zero, type, damage, knockback, ((Entity)player).whoAmI, 0f, 0f, (float)((Entity)player).whoAmI);",
     "Projectile.NewProjectile((IEntitySource)(object)source, player.MountedCenter, Vector2.Zero, type, damage, knockback, ((Entity)player).whoAmI, 0f, 0f, 0f);",
     3),
])
# Nota del fix (añadida como comentario de cabecera del archivo)
with open(os.path.join(MOD, "Content/Weapons/Definitivas/DefinitivasNuevasV6560.cs"), encoding="utf-8") as f:
    s = f.read()
s = s.replace(
    "// Los comentarios de la fuente original no viajan dentro del .dll.",
    "// Los comentarios de la fuente original no viajan dentro del .dll.\n"
    "//\n"
    "// v6.56.1 — LA CURA DEL TEMPORIZADOR: los 3 Shoot pasaban whoAmI en ai[2]\n"
    "// (la convención del RC2, donde ai[2] es el DUEÑO), pero estos 3 proyectiles\n"
    "// usan ai[2] como RELOJ (umbrales num==2/30/60/104/150/178…). En SP quienI==0\n"
    "// lo disimulaba; en MP cualquier jugador con slot != 0 arrancaba el reloj\n"
    "// desplazado (el Ojo jamás se colocaba, el Índice saltaba su apertura…).\n"
    "// El dueño ya viaja en Projectile.owner — ai[2] arranca en 0.",
    1)
with open(os.path.join(MOD, "Content/Weapons/Definitivas/DefinitivasNuevasV6560.cs"), "w", encoding="utf-8") as f:
    f.write(s)

# ============================================================================
# 3. ALTO — Testamento: gate de red del switch de sentencias + mira del server
# ============================================================================
cirugia("Content/Projectiles/Definitivas/TestamentoAethonProjectile.cs", [
    ("rumbo-server",
     "                Vector2 rumbo = (Main.MouseWorld - dueño.MountedCenter).SafeNormalize(Vector2.UnitX);",
     "                // v6.56.1 — el servidor no tiene ratón: usa el rumbo del\n"
     "                // dueño que llega sincronizado (dirección + elevación).\n"
     "                Vector2 rumbo = (Main.netMode == NetmodeID.Server)\n"
     "                    ? new Vector2(dueño.direction, -0.35f)\n"
     "                    : (Main.MouseWorld - dueño.MountedCenter).SafeNormalize(Vector2.UnitX);",
     1),
    ("switch-gate",
     "                switch (sentencia)\n"
     "                {\n"
     "                    case 0:     // EL PRISMA PRIMORDIAL (sentencia: vive su ciclo acotado)\n"
     "                        _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,\n"
     "                            ModContent.ProjectileType<PrismaPrimordialProjectile>(), daño, Projectile.knockBack,\n"
     "                            Projectile.owner, 1f, 0f, 0f);\n"
     "                        break;\n"
     "\n"
     "                    case 1:     // EL PARTIDOR DE MUNDOS\n"
     "                        _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, rumbo * 10f,\n"
     "                            ModContent.ProjectileType<PartidorDeMundosProjectile>(), daño, Projectile.knockBack,\n"
     "                            Projectile.owner, 0f, 0f, 0f);\n"
     "                        break;\n"
     "\n"
     "                    case 2:     // EL RÉQUIEM ESTELAR\n"
     "                        _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,\n"
     "                            ModContent.ProjectileType<RequiemEstelarProjectile>(), daño, Projectile.knockBack,\n"
     "                            Projectile.owner, 0f, 0f, 0f);\n"
     "                        break;\n"
     "\n"
     "                    default:    // LA IRA DEL CIELO\n"
     "                        _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,\n"
     "                            ModContent.ProjectileType<IraDelCieloProjectile>(), daño, Projectile.knockBack,\n"
     "                            Projectile.owner, 0f, 0f, 0f);\n"
     "                        break;\n"
     "                }",
     "                // v6.56.1 — EL GATE DE RED: la sentencia la spawnea la\n"
     "                // AUTORIDAD (server/SP). Antes el switch corría también en\n"
     "                // el cliente dueño y la sentencia nacía DOS veces en MP.\n"
     "                if (Main.netMode != NetmodeID.MultiplayerClient)\n"
     "                {\n"
     "                    switch (sentencia)\n"
     "                    {\n"
     "                        case 0:     // EL PRISMA PRIMORDIAL (sentencia: vive su ciclo acotado)\n"
     "                            _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,\n"
     "                                ModContent.ProjectileType<PrismaPrimordialProjectile>(), daño, Projectile.knockBack,\n"
     "                                Projectile.owner, 1f, 0f, 0f);\n"
     "                            break;\n"
     "\n"
     "                        case 1:     // EL PARTIDOR DE MUNDOS\n"
     "                            _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, rumbo * 10f,\n"
     "                                ModContent.ProjectileType<PartidorDeMundosProjectile>(), daño, Projectile.knockBack,\n"
     "                                Projectile.owner, 0f, 0f, 0f);\n"
     "                            break;\n"
     "\n"
     "                        case 2:     // EL RÉQUIEM ESTELAR\n"
     "                            _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,\n"
     "                                ModContent.ProjectileType<RequiemEstelarProjectile>(), daño, Projectile.knockBack,\n"
     "                                Projectile.owner, 0f, 0f, 0f);\n"
     "                            break;\n"
     "\n"
     "                        default:    // LA IRA DEL CIELO\n"
     "                            _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,\n"
     "                                ModContent.ProjectileType<IraDelCieloProjectile>(), daño, Projectile.knockBack,\n"
     "                                Projectile.owner, 0f, 0f, 0f);\n"
     "                            break;\n"
     "                    }\n"
     "                }\n"
     "                else if (Main.myPlayer == Projectile.owner)\n"
     "                {\n"
     "                    // el cliente dueño NO spawnea (el servidor ya lo hizo):\n"
     "                    // BUSCA al hijo para el vínculo visual del libro.\n"
     "                    _hijo = BuscarHijoDeSentencia();\n"
     "                }",
     1),
    ("vinculo-reintento",
     "            if (_hijo >= 0 && _hijo < Main.maxProjectiles)",
     "            // v6.56.1 — en MP el hijo puede llegar 1-2 ticks tarde:\n"
     "            // ventana de reintento de la búsqueda para el vínculo.\n"
     "            if (_hijo < 0 && Projectile.ai[2] > TApertura && Projectile.ai[2] < TApertura + 90\n"
     "                && Main.myPlayer == Projectile.owner)\n"
     "            {\n"
     "                _hijo = BuscarHijoDeSentencia();\n"
     "            }\n"
     "            if (_hijo >= 0 && _hijo < Main.maxProjectiles)",
     1),
    ("salida-lote",
     "            VFXCore.ReabrirLoteVanilla();\n            return false;",
     "            VFXCore.RestaurarLoteVanilla();\n            return false;",
     1),
    # El helper de búsqueda del hijo (insertado antes del PreDraw)
    ("helper-buscar-hijo",
     "        public override bool PreDraw(ref Color lightColor)",
     "        /// <summary>v6.56.1 — En MP el hijo lo spawnea el servidor; el\n"
     "        /// cliente dueño lo BUSCA para el vínculo visual (el filamento\n"
     "        /// libro→arma de la CAPA 7).</summary>\n"
     "        private int BuscarHijoDeSentencia()\n"
     "        {\n"
     "            for (int i = 0; i < Main.maxProjectiles; i++)\n"
     "            {\n"
     "                Projectile p = Main.projectile[i];\n"
     "                if (p == null || !p.active || p.owner != Projectile.owner) continue;\n"
     "                if (p.type == ModContent.ProjectileType<PrismaPrimordialProjectile>() ||\n"
     "                    p.type == ModContent.ProjectileType<PartidorDeMundosProjectile>() ||\n"
     "                    p.type == ModContent.ProjectileType<RequiemEstelarProjectile>() ||\n"
     "                    p.type == ModContent.ProjectileType<IraDelCieloProjectile>())\n"
     "                    return i;\n"
     "            }\n"
     "            return -1;\n"
     "        }\n\n"
     "        public override bool PreDraw(ref Color lightColor)",
     1),
])

# ============================================================================
# 4. MEDIO — Las miras de MouseWorld en el servidor (Ojo/Pluma/Indice/Prisma)
# ============================================================================
cirugia("Content/Projectiles/Definitivas/OjoDelJuicioProjectile.cs", [
    ("mira-mp",
     "            if (num < 2f)\n"
     "            {\n"
     "                ((Entity)((ModProjectile)this).Projectile).Center = val.MountedCenter + new Vector2(0f, -430f);",
     "            // v6.56.1 — LA COLOCACIÓN LA DECIDE EL DUEÑO (su ratón); el\n"
     "            // servidor y el resto la reciben por sync (antes el servidor\n"
     "            // peleaba la rotación con su MouseWorld basura).\n"
     "            if (num < 2f && Main.netMode != 2 && Main.myPlayer == ((ModProjectile)this).Projectile.owner)\n"
     "            {\n"
     "                ((Entity)((ModProjectile)this).Projectile).Center = val.MountedCenter + new Vector2(0f, -430f);",
     1),
])

cirugia("Content/Projectiles/Definitivas/PlumaDelVeredictoProjectile.cs", [
    ("mira-mp",
     "            if (num < 2f)\n"
     "            {\n"
     "                Vector2 val2 = Main.MouseWorld - val.MountedCenter;",
     "            // v6.56.1 — el sello lo ancla el DUEÑO con su ratón (sync al resto).\n"
     "            if (num < 2f && Main.netMode != 2 && Main.myPlayer == ((ModProjectile)this).Projectile.owner)\n"
     "            {\n"
     "                Vector2 val2 = Main.MouseWorld - val.MountedCenter;",
     1),
])

cirugia("Content/Projectiles/Definitivas/IndiceDelJuicioProjectile.cs", [
    ("mira-mp",
     "                    Vector2 val5 = Main.MouseWorld - val2;",
     "                    // v6.56.1 — el servidor no tiene ratón: las páginas van\n"
     "                    // por la rotación sincronizada del dueño.\n"
     "                    Vector2 val5 = (Main.netMode == 2)\n"
     "                        ? Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation) * 300f\n"
     "                        : (Main.MouseWorld - val2);",
     1),
])

cirugia("Content/Projectiles/Definitivas/PrismaPrimordialProjectile.cs", [
    ("mira-mp",
     "            Vector2 val2 = Utils.SafeNormalize(Main.MouseWorld - val.MountedCenter, Vector2.UnitX);\n"
     "            ((Entity)((ModProjectile)this).Projectile).Center = val.MountedCenter + val2 * 26f + new Vector2(0f, -8f);\n"
     "            ((ModProjectile)this).Projectile.rotation = Utils.ToRotation(val2);",
     "            // v6.56.1 — LA MIRA EN MP: solo el dueño apunta con su ratón;\n"
     "            // el servidor y los demás usan el Center/rotation sincronizados.\n"
     "            if (Main.netMode != 2 && Main.myPlayer == ((Entity)((ModProjectile)this).Projectile).owner)\n"
     "            {\n"
     "                Vector2 val2 = Utils.SafeNormalize(Main.MouseWorld - val.MountedCenter, Vector2.UnitX);\n"
     "                ((Entity)((ModProjectile)this).Projectile).Center = val.MountedCenter + val2 * 26f + new Vector2(0f, -8f);\n"
     "                ((ModProjectile)this).Projectile.rotation = Utils.ToRotation(val2);\n"
     "            }",
     1),
    ("salida-lote",
     "            VFXCore.ReabrirLoteVanilla();\n            return false;",
     "            VFXCore.RestaurarLoteVanilla();\n            return false;",
     1),
])

cirugia("Content/Projectiles/Definitivas/IraDelCieloProjectile.cs", [
    # solo la salida de la zona de caída (la salida final 618 ya restaura vanilla)
    ("salida-lote-zona",
     "                    finally\n"
     "                    {\n"
     "                        RayoStrip.CerrarLote();\n"
     "                    }\n"
     "                }\n"
     "            }\n"
     "            VFXCore.ReabrirLoteVanilla();\n"
     "            return false;",
     "                    finally\n"
     "                    {\n"
     "                        RayoStrip.CerrarLote();\n"
     "                    }\n"
     "                }\n"
     "            }\n"
     "            // v6.56.1 — CerrarLote deja el lote en ADITIVO: restaurar el\n"
     "            // pase de entidades o el resto de proyectiles sale LAVADO.\n"
     "            VFXCore.RestaurarLoteVanilla();\n"
     "            return false;",
     1),
])

cirugia("Content/Projectiles/PrimerTrazoFajo.cs", [
    ("salida-lote",
     "            VFXCore.ReabrirLoteVanilla();\n            return false;",
     "            VFXCore.RestaurarLoteVanilla();\n            return false;",
     1),
])

# ============================================================================
# 5. EL NÚCLEO — VFXCore: RestaurarLoteVanilla + fugas de VRAM + quads server
# ============================================================================
cirugia("Content/VFX/VFXCore.cs", [
    ("restaurar-lote",
     "            catch { }\n"
     "        }\n"
     "\n"
     "        // ------------------------------------------------------------------\n"
     "        //  HELPERS DE MOVIMIENTO (los \"latidos\" de la librería)\n"
     "        // ------------------------------------------------------------------",
     "            catch { }\n"
     "        }\n"
     "\n"
     "        /// <summary>\n"
     "        /// v6.56.1 — LA CURA DEL LOTE ADITIVO. RayoStrip.CerrarLote()\n"
     "        /// reabre el SpriteBatch en ADITIVO (para que el llamador dibuje\n"
     "        /// sus gorros con el lote vivo); pero si un PreDraw SALÍA así, el\n"
     "        /// resto de proyectiles del pase se dibujaba con blend aditivo\n"
     "        /// (LAVADOS, sin alfa). Este helper termina el lote abierto (sea\n"
     "        /// cual sea su blend — la sonda decide) y reabre con los\n"
     "        /// parámetros EXACTOS del pase de entidades. Los sprites ya en\n"
     "        /// cola se vuelcan con el blend con el que fueron encolados y el\n"
     "        /// pase continúa limpio. Es LA SALIDA correcta de todo PreDraw\n"
     "        /// que haya usado RayoStrip (gorros de StormLib incluidos).\n"
     "        /// </summary>\n"
     "        public static void RestaurarLoteVanilla()\n"
     "        {\n"
     "            CerrarLoteSiAbierto();\n"
     "            ReabrirLoteVanilla();\n"
     "        }\n"
     "\n"
     "        // ------------------------------------------------------------------\n"
     "        //  HELPERS DE MOVIMIENTO (los \"latidos\" de la librería)\n"
     "        // ------------------------------------------------------------------",
     1),
    ("quads-server",
     "            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;",
     "            // v6.56.1 — limpiar el búfer también en el server (antes el\n"
     "            // return lo dejaba crecer y el presupuesto mentía).\n"
     "            if (Main.netMode == Terraria.ID.NetmodeID.Server) { _quads.Clear(); return; }",
     2),
])

# Reiniciar: funeral de las 4 horneadas (fuga de VRAM en recargas)
with open(os.path.join(MOD, "Content/VFX/VFXCore.cs"), encoding="utf-8") as f:
    vc = f.read()
VIEJO_REINICIAR = """        public static void Reiniciar()
        {
            try { _quads.Clear(); } catch { }
            // v6.50.51 — el anillo horneado también se suelta (la textura
            // perezosa se rehornea sola al próximo uso — 1 ms).
            Texture2D arcoirisViejo = _arcoiris;
            _arcoiris = null;
            if (arcoirisViejo != null)
            {
                try
                {
                    if (Main.dedServ ||
                        System.Threading.Thread.CurrentThread.ManagedThreadId == _hiloPrincipal)
                    {
                        arcoirisViejo.Dispose();
                    }
                    else
                    {
                        Main.QueueMainThreadAction(() =>
                        {
                            try { arcoirisViejo.Dispose(); } catch { }
                        });
                    }
                }
                catch { }
            }
            _quadsDelFrame = 0;
            _frameDelPresupuesto = 0;
            _factorCalidad = 1f;
            _fpsSuave = 60f;
        }"""
NUEVO_REINICIAR = """        public static void Reiniciar()
        {
            try { _quads.Clear(); } catch { }
            // v6.50.51 — el anillo horneado se suelta (se rehornea solo al
            // próximo uso — 1 ms). v6.56.1 — LAS 4 HORNEADAS TAMBIÉN
            // (Carne/Colmillo/Ventosa/OjoRasgado): el comentario viejo decía
            // "se repiden solas" pero NADIE las disponía — fuga de VRAM en
            // cada recarga del mod. El funeral encolado, ahora en helper.
            SoltarTexturaHorneada(ref _arcoiris);
            SoltarTexturaHorneada(ref _carne);
            SoltarTexturaHorneada(ref _colmillo);
            SoltarTexturaHorneada(ref _ventosa);
            SoltarTexturaHorneada(ref _ojoRasgado);
            _quadsDelFrame = 0;
            _frameDelPresupuesto = 0;
            _factorCalidad = 1f;
            _fpsSuave = 60f;
        }

        /// <summary>v6.56.1 — EL FUNERAL ENCOLADO de una textura horneada
        /// (el patrón del arcoiris de la v6.50.51, extraído a helper: capturar,
        /// despegar y dejar que el hilo principal la entierre).</summary>
        private static void SoltarTexturaHorneada(ref Texture2D campo)
        {
            Texture2D vieja = campo;
            campo = null;
            if (vieja == null) return;
            try
            {
                if (Main.dedServ ||
                    System.Threading.Thread.CurrentThread.ManagedThreadId == _hiloPrincipal)
                {
                    vieja.Dispose();
                }
                else
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        try { vieja.Dispose(); } catch { }
                    });
                }
            }
            catch { }
        }"""
if vc.count(VIEJO_REINICIAR) != 1:
    print("FALLO [VFXCore] Reiniciar: patrón no único"); sys.exit(1)
vc = vc.replace(VIEJO_REINICIAR, NUEVO_REINICIAR)
with open(os.path.join(MOD, "Content/VFX/VFXCore.cs"), "w", encoding="utf-8") as f:
    f.write(vc)
reporte.append(("Content/VFX/VFXCore.cs", "reiniciar-horneadas", 1))
print("OK  Content/VFX/VFXCore.cs: Reiniciar + helper (fuera de cirugía)")

# ============================================================================
# 6. RayoStrip — EmitirQuadTex con vaciado-y-continúa (como EmitirTri)
# ============================================================================
cirugia("Content/VFX/RayoStrip.cs", [
    ("emitirquad-flush",
     "        private static void EmitirQuadTex(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1,\n"
     "            Color c0, Color c1, float u0, float u1, float vMin, float vMax)\n"
     "        {\n"
     "            if (_nVertsT + 6 > _vertsT.Length) return;",
     "        private static void EmitirQuadTex(Texture2D tex, Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1,\n"
     "            Color c0, Color c1, float u0, float u1, float vMin, float vMax)\n"
     "        {\n"
     "            // v6.56.1 — VACIAR-Y-CONTINUAR (el contrato de EmitirTri):\n"
     "            // antes el pozo lleno DESCARTABA el quad en silencio — la\n"
     "            // PUNTA del haz desaparecería sin síntoma con espinas largas.\n"
     "            if (_nVertsT + 6 > _vertsT.Length) VolcarTextura(tex);\n"
     "            if (_nVertsT + 6 > _vertsT.Length) return;",
     1),
    ("emitirquad-caller",
     "                EmitirQuadTex(a0, a1, b0, b1, c0, c1, u0, u1, vMin, vMax);",
     "                EmitirQuadTex(tex, a0, a1, b0, b1, c0, c1, u0, u1, vMin, vMax);",
     1),
])

# ============================================================================
# 7. AlasAuroraLib — el latch eterno + BrumaSystem — el barrendero ampliado
# ============================================================================
cirugia("Content/VFX/AlasAuroraLib.cs", [
    ("latch-reset",
     "            _ruido = null;\n"
     "            _ruidos.Clear();",
     "            _ruido = null;\n"
     "            _ruidos.Clear();\n"
     "            // v6.56.1 — EL LATCH ETERNO: si UNA carga de textura fallaba\n"
     "            // una vez, las alas de TODOS los estilos dejaban de intentar\n"
     "            // cargar hasta reiniciar el juego. La descarga lo perdona.\n"
     "            _texturaProboFallar = false;",
     1),
])

cirugia("Content/Effects/Bruma/BrumaSystem.cs", [
    ("podar-marcas",
     "        public override void PostUpdateWorld()\n"
     "        {\n"
     "            Cinta.TickPurga();\n"
     "        }",
     "        /// <summary>v6.56.1 — el podador de las marcas del festín: la\n"
     "        /// caducidad de 5 s de FaucesGlobalNPC vivía SIN llamador desde\n"
     "        /// una purga antigua — una marca sobreviviente interceptaba la\n"
     "        /// muerte del jefe PARA SIEMPRE (18 s de cine por cualquier otra\n"
     "        /// causa de muerte minutos después).</summary>\n"
     "        private int _tickPodaMarcas;\n\n"
     "        public override void PostUpdateWorld()\n"
     "        {\n"
     "            Cinta.TickPurga();\n"
     "            if (++_tickPodaMarcas >= 60)\n"
     "            {\n"
     "                _tickPodaMarcas = 0;\n"
     "                try { AethonMod.Content.Globals.FaucesGlobalNPC.PodarMarcas(); } catch { }\n"
     "            }\n"
     "        }",
     1),
    ("barrendero-alas",
     "            try { Cinta.Purgar(); } catch { }\n"
     "            try { VFXCore.Reiniciar(); } catch { }",
     "            try { Cinta.Purgar(); } catch { }\n"
     "            try { VFXCore.Reiniciar(); } catch { }\n\n"
     "            // v6.56.1 — las ALAS también: sus 2 BasicEffect solo se\n"
     "            // limpiaban al salir del MUNDO (OnWorldUnload) — en la\n"
     "            // descarga del mod fugaban en cada recarga.\n"
     "            try { AethonMod.Content.VFX.AlasAuroraLib.Reiniciar(); } catch { }",
     1),
])

# ============================================================================
# 8. Globals — el guard de servidor de CosmicProjectileFX
# ============================================================================
cirugia("Content/Globals/CosmicProjectileFX.cs", [
    ("dedServ-guard",
     "        public override void AI(Projectile projectile)\n"
     "        {\n"
     "            // === ESTELA CÓSMICA (v5.2: duración CORTA) ===",
     "        public override void AI(Projectile projectile)\n"
     "        {\n"
     "            // v6.56.1 — el servidor no dibuja polvos ni luces (trabajo\n"
     "            // tirado a la basura cada tick; el patrón de referencia de la\n"
     "            // casa — CalaveraPotenciadaFX — SÍ tenía el guard).\n"
     "            if (Main.dedServ) return;\n"
     "\n"
     "            // === ESTELA CÓSMICA (v5.2: duración CORTA) ===",
     1),
])

# ============================================================================
# 9. Bolsas — el slot 58 (el cursor) invisible para la garantía
# ============================================================================
cirugia("Content/Items/Bolsas/BolsaCategoria.cs", [
    ("slot-58",
     "        /// <summary>¿El jugador tiene este ítem en el inventario (58 slots)?</summary>\n"
     "        protected static bool Tiene(Player player, int itemType)\n"
     "        {\n"
     "            for (int i = 0; i < 58; i++)",
     "        /// <summary>¿El jugador tiene este ítem? (incluido el slot 58 del\n"
     "        /// cursor: si su ÚNICA copia va en la mano, la garantía no debe\n"
     "        /// duplicarla — v6.56.1).</summary>\n"
     "        protected static bool Tiene(Player player, int itemType)\n"
     "        {\n"
     "            for (int i = 0; i < player.inventory.Length; i++)",
     1),
])

cirugia("Content/Players/TestingPlayer.cs", [
    ("slot-58",
     "        /// <summary>¿El jugador tiene este ítem en el inventario (58 slots)?</summary>\n"
     "        private bool HasItem(int itemType)\n"
     "        {\n"
     "            for (int i = 0; i < 58; i++)",
     "        /// <summary>¿El jugador tiene este ítem? (incluido el slot del\n"
     "        /// cursor — v6.56.1, la misma cura de BolsaCategoria.Tiene).</summary>\n"
     "        private bool HasItem(int itemType)\n"
     "        {\n"
     "            for (int i = 0; i < Player.inventory.Length; i++)",
     1),
])

# ============================================================================
# 10. ShardSyncSystem — el protocolo muerto (const + case + TODO)
# ============================================================================
cirugia("Content/Systems/ShardSyncSystem.cs", [
    ("const-muerta",
     "        public const byte SyncResonance = 2;\n\n",
     "        // v6.56.1 — SyncResonance (const 2) ERA UN PROTOCOLO MUERTO: cero\n"
     "        // emisores y un case vacío con TODO. Retirado (era el único\n"
     "        // TODO-tarea real de todo el mod).\n\n",
     1),
    ("case-muerto",
     "                    case SyncResonance:\n"
     "                        // TODO: implementar sync de resonancia.\n"
     "                        break;\n\n",
     "",
     1),
])

# ============================================================================
# 11. LA ESCALERA — la exclusión mutua CRUZADA (un solo haz entre TODAS las clases)
# ============================================================================
cirugia("Content/Weapons/Definitivas/RayosConcentrados.cs", [
    ("hazlibre-cruzado",
     "        public static bool HazLibre(Player player)\n"
     "        {\n"
     "            int num = ModContent.ProjectileType<RayoConcentradoNivelProjectile>();\n"
     "            for (int i = 0; i < Main.maxProjectiles; i++)\n"
     "            {\n"
     "                Projectile val = Main.projectile[i];\n"
     "                if (val != null && ((Entity)val).active && val.owner == ((Entity)player).whoAmI && val.type == num)\n"
     "                {\n"
     "                    return false;\n"
     "                }\n"
     "            }\n"
     "            return true;\n"
     "        }",
     "        public static bool HazLibre(Player player)\n"
     "        {\n"
     "            // v6.56.1 — LA EXCLUSIÓN CRUZADA DE LA ESCALERA: UN solo haz\n"
     "            // definitivo a la vez, CRUZANDO clases (niveles 1-3 + RC2 +\n"
     "            // Prisma). Antes cada familia solo se excluía a sí misma y\n"
     "            // cabían hasta 3 haces simultáneos apilando daño.\n"
     "            int nivel = ModContent.ProjectileType<RayoConcentradoNivelProjectile>();\n"
     "            int rc2 = ModContent.ProjectileType<RayoConcentradoDosProjectile>();\n"
     "            int prisma = ModContent.ProjectileType<PrismaPrimordialProjectile>();\n"
     "            for (int i = 0; i < Main.maxProjectiles; i++)\n"
     "            {\n"
     "                Projectile val = Main.projectile[i];\n"
     "                if (val != null && ((Entity)val).active && val.owner == ((Entity)player).whoAmI\n"
     "                    && (val.type == nivel || val.type == rc2 || val.type == prisma))\n"
     "                {\n"
     "                    return false;\n"
     "                }\n"
     "            }\n"
     "            return true;\n"
     "        }",
     1),
])

cirugia("Content/Weapons/Definitivas/RayoConcentradoDos.cs", [
    ("canuseitem-cruzado",
     "        public override bool CanUseItem(Player player)\n"
     "        {\n"
     "            // UN solo haz a la vez: su ciclo completo dura 1200 ticks (20 s).\n"
     "            for (int i = 0; i < Main.maxProjectiles; i++)\n"
     "            {\n"
     "                Projectile p = Main.projectile[i];\n"
     "                if (p != null && p.active && p.owner == player.whoAmI &&\n"
     "                    p.type == ModContent.ProjectileType<RayoConcentradoDosProjectile>())\n"
     "                    return false;\n"
     "            }\n"
     "            return true;\n"
     "        }",
     "        public override bool CanUseItem(Player player)\n"
     "        {\n"
     "            // v6.56.1 — UN solo haz a la vez, cruzando TODA la escalera\n"
     "            // (niveles 1-3 + RC2 + Prisma): EscaleraDelRayo.HazLibre.\n"
     "            return EscaleraDelRayo.HazLibre(player);\n"
     "        }",
     1),
])

cirugia("Content/Weapons/Definitivas/DefinitivasItemsA.cs", [
    ("canuseitem-cruzado",
     "        public override bool CanUseItem(Player player)\n"
     "        {\n"
     "            for (int i = 0; i < Main.maxProjectiles; i++)\n"
     "            {\n"
     "                Projectile p = Main.projectile[i];\n"
     "                if (p != null && p.active && p.owner == player.whoAmI &&\n"
     "                    p.type == ModContent.ProjectileType<PrismaPrimordialProjectile>())\n"
     "                    return false;\n"
     "            }\n"
     "            return true;\n"
     "        }",
     "        public override bool CanUseItem(Player player)\n"
     "        {\n"
     "            // v6.56.1 — UN solo haz a la vez, cruzando TODA la escalera\n"
     "            // (niveles 1-3 + RC2 + Prisma): EscaleraDelRayo.HazLibre.\n"
     "            return EscaleraDelRayo.HazLibre(player);\n"
     "        }",
     1),
])

# ============================================================================
# 12. El halo del aura — determinismo (Main.rand fuera del render)
# ============================================================================
cirugia("Content/Projectiles/Cosmetic/AlasAuroraHalo.cs", [
    ("halo-determinista",
     "            if (Utils.NextBool(Main.rand, 22))\n"
     "            {\n"
     "                Dust.NewDustPerfect(val.MountedCenter + new Vector2((float)(-((Entity)val).direction) * Utils.NextFloat(Main.rand, 20f, 60f), (0f - Utils.NextFloat(Main.rand, 30f, 80f)) * val.gravDir), 173, (Vector2?)new Vector2(0f, 0.4f), 140, ColorPrisma(Utils.NextFloat(Main.rand)), 0.8f).noGravity = false;\n"
     "            }",
     "            // v6.56.1 — DETERMINISMO DEL AURA (ley §0.4 del códice): el\n"
     "            // brillo ya no tira de Main.rand en render — Hash01 con\n"
     "            // semilla viva (misma probabilidad 1/22 y mismos rangos).\n"
     "            if (VFXCore.Hash01(((Entity)val).whoAmI * 17 + 5, (int)Main.GameUpdateCount, 311) < 0.0455f)\n"
     "            {\n"
     "                int semillaAura = ((Entity)val).whoAmI * 31 + 7;\n"
     "                int tick = (int)Main.GameUpdateCount;\n"
     "                float dx = 20f + 40f * VFXCore.Hash01(semillaAura, tick, 313);\n"
     "                float dy = 30f + 50f * VFXCore.Hash01(semillaAura + 1, tick, 317);\n"
     "                Dust.NewDustPerfect(val.MountedCenter + new Vector2((float)(-((Entity)val).direction) * dx, (0f - dy) * val.gravDir), 173, (Vector2?)new Vector2(0f, 0.4f), 140, ColorPrisma(VFXCore.Hash01(semillaAura + 2, tick, 319)), 0.8f).noGravity = false;\n"
     "            }",
     1),
])

# ============================================================================
# 13. RayoLib — el parpadeo VenaTrueno determinista (Main.rand fuera del render)
# ============================================================================
cirugia("Content/VFX/RayoLib.cs", [
    ("parpadeo-determinista",
     "                    if (r.Parpadeo && !Main.rand.NextBool()) continue;",
     "                    // v6.56.1 — DETERMINISMO (ley §0.4): el parpadeo ya no\n"
     "                    // tira de Main.rand en render — cada máquina ve el\n"
     "                    // MISMO canal vivo/muerto (Hash01 con el tick).\n"
     "                    if (r.Parpadeo && VFXCore.Hash01(i * 17 + 3, (int)Main.GameUpdateCount, 911) < 0.5f) continue;",
     1),
])

# ============================================================================
# 14. AudioLib.SilenciarZona — la promesa del códice §7.1 (nadie lo llamaba)
# ============================================================================
cirugia("Content/Systems/GrimorioFuriaSistema.cs", [
    ("silenciar-zona",
     "        public override void PostUpdateWorld()",
     "        /// <summary>v6.56.1 — LA PROMESA DEL CÓDICE §7.1: al cambiar de\n"
     "        /// arma la voz nueva suena YA (AudioLib.SilenciarZona estaba\n"
     "        /// documentado pero NADIE lo llamaba). Corre en todas las\n"
     "        /// máquinas, gated a la máquina local.</summary>\n"
     "        private static int _ultimoItemEnMano = -1;\n\n"
     "        public override void PostUpdateEverything()\n"
     "        {\n"
     "            if (!Main.dedServ && Main.netMode != 2 && Main.LocalPlayer != null && Main.LocalPlayer.HeldItem != null)\n"
     "            {\n"
     "                int itemActual = Main.LocalPlayer.HeldItem.type;\n"
     "                if (_ultimoItemEnMano >= 0 && _ultimoItemEnMano != itemActual)\n"
     "                {\n"
     "                    try { AudioLib.SilenciarZona(); } catch { }\n"
     "                }\n"
     "                _ultimoItemEnMano = itemActual;\n"
     "            }\n"
     "        }\n\n"
     "        public override void PostUpdateWorld()",
     1),
])

# ============================================================================
print()
print("=" * 62)
print(f"CURACIONES APLICADAS: {len(reporte)}")
print("=" * 62)
for ruta, nombre, veces in reporte:
    print(f"  {ruta} :: {nombre} (×{veces})")
