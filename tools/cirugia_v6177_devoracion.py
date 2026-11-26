#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""cirugia_v6177_devoracion.py — la devoración que nace del libro + el silencio total.

v6.177 (la letra):
  · «la forma en la que aparecen los tentaculos en la animacion de
    devorar jefes del grimorio del eterno esta un poco rara, los
    tentaculos en esa animacion deben aparecer desde el mismo grimorio
    y deben extenderse hacia afuera en direcciones al jefe que sera
    devorado, luego estos lo cubren e inicia el resto de la
    animacion» — EL CORO EN DOS TIEMPOS: (1) EXTENDER — la punta nace
    EN el libro y viaja hacia afuera hasta su puesto en la jaula;
    (2) CUBRIR — ya extendidos, la jaula se ciñe y la cobertura
    Devorador crece DESPUÉS. Antes «alcance = sale·0.35 + festín·0.65»
    mezclaba los dos tiempos: los brazos nunca se leían extendiéndose.
  · «asegurate de que en el momento en el que el grimorio del eterno
    devora a cualquier jefe, toda actividad del jefe cese» + «todos los
    jefes devorados mientras son devorados deben callar en todos los
    sentidos» — LA PURGA DE LA MESA: al arrancar CUALQUIER devoración
    (cena · festín · aspiradora), los proyectiles hostiles que el jefe
    ya había escupido SE APAGAN con una mota.
"""
R1 = 'AethonMod/Content/Globals/FaucesGlobalNPC.cs'
R2 = 'AethonMod/Content/Projectiles/Sombras/PrimeraCenaProjectile.cs'
R3 = 'AethonMod/Content/Projectiles/Sombras/AspiradoraDevoradorProjectile.cs'

# ══ 1. FaucesGlobalNPC: LA PURGA DE LA MESA ════════════════════════
s = open(R1, encoding='utf-8').read()

viejo = """        /// <summary>v6.176 — EL DESCONGELADO: la aspiradora terminó (o"""
nuevo = """        /// <summary>v6.177 — LA PURGA DE LA MESA (la letra: «asegurate
        /// de que en el momento en el que el grimorio del eterno devora
        /// a cualquier jefe, toda actividad del jefe cese» + «todos los
        /// jefes devorados mientras son devorados deben callar en todos
        /// los sentidos»): al ARRANCAR cualquier devoración (cena ·
        /// festín · aspiradora), los proyectiles hostiles que vuelan a
        /// menos de `radio` del preso SE APAGAN con una mota — nada del
        /// jefe sigue volando mientras el libro come.</summary>
        public static void PurgarProyectilesDelJefe(NPC jefe, float radio = 1000f)
        {
            if (jefe == null || Main.netMode == NetmodeID.MultiplayerClient) return;
            try
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || p.friendly) continue;
                    if (p.Center.DistanceSQ(jefe.Center) > radio * radio) continue;
                    if (!Main.dedServ)
                    {
                        int d = Dust.NewDustPerfect(p.Center, 60,
                            new Vector2(0f, -0.6f), 120, new Color(60, 30, 90), 1.1f);
                        Main.dust[d].noGravity = true;
                    }
                    p.Kill();
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("FaucesGlobalNPC", exRegistro); }
        }

        /// <summary>v6.176 — EL DESCONGELADO: la aspiradora terminó (o"""
assert viejo in s, 'ancla de la purga no encontrada'
s = s.replace(viejo, nuevo, 1)

# 1b — llamada en la PRIMERA CENA (cuando el cine arranca de verdad)
viejo = """        public static void ArrancarPrimeraCena(NPC jefe, Player portador)
        {
            if (jefe == null || !jefe.active || portador == null) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (!jefe.TryGetGlobalNPC<FaucesGlobalNPC>(out var g) || g.DevorarEstado != 0) return;"""
nuevo = """        public static void ArrancarPrimeraCena(NPC jefe, Player portador)
        {
            if (jefe == null || !jefe.active || portador == null) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (!jefe.TryGetGlobalNPC<FaucesGlobalNPC>(out var g) || g.DevorarEstado != 0) return;
            // v6.177 — LA PURGA: el preso CALLA EN TODO SENTIDO (nada
            // suyo sigue volando mientras el libro come)
            PurgarProyectilesDelJefe(jefe);"""
assert viejo in s, 'ArrancarPrimeraCena no encontrada'
s = s.replace(viejo, nuevo, 1)

# 1c — llamada en el FESTÍN CLÁSICO (la marca de devorado)
viejo = """            g.DevorarEstado = 1;
            g.DevorarEstilo = estilo;
            g.DevorarTick = 0;
            g.FaseFestin = 0;

            dueño.life = 1;"""
nuevo = """            g.DevorarEstado = 1;
            g.DevorarEstilo = estilo;
            g.DevorarTick = 0;
            g.FaseFestin = 0;
            // v6.177 — LA PURGA: el preso calla en TODO sentido
            PurgarProyectilesDelJefe(dueño);

            dueño.life = 1;"""
assert viejo in s, 'la marca del festín no se encontró'
s = s.replace(viejo, nuevo, 1)
open(R1, 'w', encoding='utf-8', newline='\n').write(s)
print('OK FaucesGlobalNPC: purga + 2 llamadas')

# ══ 2. La aspiradora también purga al capturar ════════════════════
s = open(R3, encoding='utf-8').read()
viejo = """            Systems.GrimorioFuriaSistema.CancelarExtensionDevorador();
            CosecharSecciones(cabeza);"""
nuevo = """            Systems.GrimorioFuriaSistema.CancelarExtensionDevorador();
            // v6.177 — LA PURGA: el gusano calla en TODO sentido (ni un
            // proyectil suyo sigue volando mientras el libro aspira)
            FaucesGlobalNPC.PurgarProyectilesDelJefe(cabeza, 1400f);
            CosecharSecciones(cabeza);"""
assert viejo in s, 'la captura de la aspiradora no se encontró'
s = s.replace(viejo, nuevo, 1)
open(R3, 'w', encoding='utf-8', newline='\n').write(s)
print('OK Aspiradora: purga al capturar')

# ══ 3. EL CORO EN DOS TIEMPOS (nace del libro → se extiende → cubre) ═══
s = open(R2, encoding='utf-8').read()

# 3a — la cobertura empieza DESPUÉS de la extensión
viejo = """                float sale = SombrasLib.DeGolpe(MathHelper.Clamp((t - (T_FESTIN - 20f)) / 30f, 0f, 1f));
                float festin = MathHelper.Clamp((t - T_FESTIN) / 85f, 0f, 1f);     // la cobertura crece"""
nuevo = """                float sale = SombrasLib.DeGolpe(MathHelper.Clamp((t - (T_FESTIN - 20f)) / 30f, 0f, 1f));
                // v6.177 — LA COBERTURA ESPERA A LA EXTENSIÓN (la letra:
                // «los tentaculos… deben aparecer desde el mismo grimorio y
                // deben extenderse hacia afuera… LUEGO estos lo cubren e
                // inicia el resto de la animacion»): la cobertura del preso
                // crece DESPUÉS de que los brazos terminaron de extenderse
                float festin = MathHelper.Clamp((t - T_FESTIN - 12f) / 73f, 0f, 1f);   // la cobertura crece"""
assert viejo in s, 'el festín de cobertura no se encontró'
s = s.replace(viejo, nuevo, 1)

# 3b — el destino: EXTENDER desde el libro → CUBRIR con la jaula
viejo = """                    // EL DESTINO: cada sombra ORBITA al preso y SE CIÑE —
                    // la jaula de N brazos que se aprieta (radio 0.9→0.35)
                    float angOrbita = k * (MathF.PI * 2f / nSombras) + tiempo * (0.9f + 0.12f * k);
                    float radioOrbita = radioJefe * MathHelper.Lerp(0.90f, 0.35f, festin);
                    Vector2 destino = centroJefe + new Vector2(MathF.Cos(angOrbita), MathF.Sin(angOrbita) * 0.72f) * radioOrbita;
                    float alcance = saleK * 0.35f + festin * 0.65f;
                    destino = Vector2.Lerp(raiz + rumboRaiz * 42f, destino, alcance);"""
nuevo = """                    // ============================================================
                    //  v6.177 — EL TENTÁCULO NACE DEL LIBRO (la letra: «los
                    //  tentaculos en esa animacion deben aparecer desde el
                    //  mismo grimorio y deben extenderse hacia afuera en
                    //  direcciones al jefe que sera devorado, luego estos lo
                    //  cubren e inicia el resto de la animacion»). DOS
                    //  TIEMPOS LIMPIOS — antes «alcance = sale·0.35 +
                    //  festín·0.65» mezclaba ambos y los brazos nunca se
                    //  leían creciendo del libro:
                    //   (1) EXTENDER: la punta viaja DESDE el libro hacia
                    //       SU puesto alrededor del preso (saleK 0→1).
                    //   (2) CUBRIR: ya extendidos, la jaula SE CIÑE (el
                    //       radio cae 0.95→0.35) y lo cubre.
                    // ============================================================
                    float angOrbita = k * (MathF.PI * 2f / nSombras) + tiempo * (0.9f + 0.12f * k);
                    float cina = MathHelper.Clamp((t - (T_FESTIN + 10f + retardo)) / 70f, 0f, 1f);
                    float radioOrbita = radioJefe * MathHelper.Lerp(0.95f, 0.35f, cina);
                    Vector2 destino = centroJefe + new Vector2(MathF.Cos(angOrbita), MathF.Sin(angOrbita) * 0.72f) * radioOrbita;
                    // (1) EXTENDER: nace EN el libro y CRECE hacia afuera
                    destino = Vector2.Lerp(raiz + rumboRaiz * 24f, destino, saleK);"""
assert viejo in s, 'el destino del coro no se encontró'
s = s.replace(viejo, nuevo, 1)
open(R2, 'w', encoding='utf-8', newline='\n').write(s)
print('OK PrimeraCena: el coro nace del libro y se extiende antes de cubrir')
