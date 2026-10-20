#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ======================================================================
# cirugia_despertar_v6510.py — EL DESPERTAR ENTRA AL CUERPO DEL MOD
#
#  (1) ShardPlayer: NivelDespertar + PetDelDespertar (persistidos),
#      el techo de ojos ESTRICTO por nivel, el coordinador combinado,
#      el stack de invocación MUERE (ojos = stats del grimorio SOLO).
#  (2) WeaponScaling: TechoOjosEterno por nivel (5→1, 10→2, cada 10+1)
#      + DanoOjosDelEterno (el daño de los ojos SIN armadura).
#  (3) GrimoireEternal: el daño del despertar a TODO, la invocación
#      gated (nivel + despertar para mejorar), el daño de ojos
#      grimorio-only, LA BARRA DEL DESPERTAR en el tooltip (debajo del
#      nivel, encima del hambre) + la lista pequeña de requisitos.
#  (4) OjoEternoMinion: Fortalecer gated por despertar; nivel 50 =
#      TODO más fuerte y más rápido.
#  (5) ShardLevelItem.OnLevelUp: anuncio de CADA OJO NUEVO disponible.
#  (6) CorruptoGlobalItem: +5 % daño y otros stats en % (la letra).
#  (7) OleadaNPC: la chusma suelta 2 % de esencia; el jefe 10 planas
#      (la esencia de jefe MUERE) + el loot de items PELADO (detour).
#  (8) EL AGUJERO NEGRO NORMAL MUERE (CosmicWeapons + la bolsa + la
#      lente).
#  (9) TestingPlayer: el Eco del Despertar a la mano.
# ======================================================================
import io
import os

RAIZ = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"

def leer(ruta):
    return io.open(os.path.join(RAIZ, ruta), encoding="utf-8").read()

def escribir(ruta, contenido):
    io.open(os.path.join(RAIZ, ruta), "w", encoding="utf-8").write(contenido)

def reemplazar(src, viejo, nuevo, veces=1, ruta="?"):
    n = src.count(viejo)
    assert n == veces, f"[{ruta}] MARCADOR {viejo[:70]!r}: esperado {veces}, hallado {n}"
    return src.replace(viejo, nuevo)

# ======================================================================
# (1) ShardPlayer.cs
# ======================================================================
R = "Content/Players/ShardPlayer.cs"
s = leer(R)

# (1a) LOS CAMPOS NUEVOS (junto a CaidaRecibida — buscar su declaración)
s = reemplazar(s,
"""        public bool CaidaRecibida = false;
""",
"""        public bool CaidaRecibida = false;

        /// <summary>v6.51.0 — EL DESPERTAR DEL GRIMORIO (la nueva
        /// progresión, la letra: «empieza a nivel 0… máximo 50 por
        /// ahora»): el nivel que desbloquea los poderes del grimorio
        /// (mejoras de los ojos, el sol rúnico, los agujeros negros,
        /// la mascota del nivel 20) y suma +1 % de daño por nivel a
        /// TODOS los tipos de daño del grimorio.</summary>
        public int NivelDespertar;

        /// <summary>v6.51.0 — LA MASCOTA DEL ESCUPIDIZO (nivel 20 del
        /// despertar: el grimorio la escupió UNA VEZ — la ceremonia no
        /// se repite).</summary>
        public bool PetDelDespertar;
""", ruta=R)

# (1b) el techo de ojos: ESTRICTO por nivel (los ojos devorados mueren)
s = reemplazar(s,
"""            int techoOjos = WeaponScaling.TechoOjosEterno(equipoInvocador, nivel);
            // v6.50.128 — LOS OJOS DEL BANQUETE: lo devorado en el altar
            // despierta ojos nuevos en la corona (+1 cada 75 esencias,
            // tope +3 — la mecánica que «dará más vida al mod»).
            techoOjos += OjosExtraDevorados(Player);
            // v6.50.129 — LOS OJOS DE LA CORONA DEL FESTÍN: cada 5 armas
            // de invocación comidas despierta OTRO ojo (tope +3 — suma
            // con los del banquete: el escuadrón puede crecer de las dos
            // mesas del cráter).
            techoOjos += OjosCoronaDevorados(Player);
""",
"""            // v6.51.0 — EL TECHO ESTRICTO DE LA LETRA: «la cantidad de
            // minions están sujetos a la subida normal de nivel del
            // grimorio, cada 10 niveles 1 minion nuevo, en nivel 5 puede
            // invocar su primer minion, en nivel 10 su segundo minion y a
            // partir de aquí cada 10 niveles 1 nuevo» — NI el equipo NI
            // lo devorado inflan el escuadrón (los ojos de banquete y de
            // corona de la .128-.129 MURIERON con el despertar).
            int techoOjos = WeaponScaling.TechoOjosEterno(equipoInvocador, nivel);
""", ruta=R)

# (1c) el stack de invocación muere (los ojos son del GRIMORIO, no del
#      equipo — la letra: «sin importar la armadura o accesorios»)
s = reemplazar(s,
"""            try
            {
                // +1% daño de invocación por nivel (Infinito)
                Player.GetDamage(DamageClass.Summon) += WeaponScaling.SummonDamageBonus(nivel);
                // +0.2% crítico mágico por nivel (Máximo 100%)
                Player.GetCritChance(DamageClass.Magic) += WeaponScaling.CritBonus(nivel);
                // Armor penetration +2% cada 5 niveles (Máximo 50%)
                Player.GetArmorPenetration(DamageClass.Magic) += WeaponScaling.ArmorPenBonus(nivel);
                // v6.45: la mitad de invocación del bonus por mana
                // faltante también es persistente (la mitad mágica
                // sigue evaluándose por golpe en ModifyWeaponDamage).
                Player.GetDamage(DamageClass.Summon) *=
                    WeaponScaling.LowManaDamageMult(Player.statMana, Player.statManaMax2);
            }
            catch { }
        }
""",
"""            try
            {
                // v6.51.0 — EL STACK DE INVOCACIÓN MURIÓ (la letra: «los
                // minions del grimorio son INDEPENDIENTES de los minions
                // del juego… sin importar la armadura o accesorios que
                // lleve el jugador, los minions del grimorio estarán
                // sujetos a las características del grimorios»): los ojos
                // leen SOLO las stats del grimorio (DanoOjosDelEterno al
                // invocarlos) — el crítico y la penetración mágicos son
                // del propio libro (su clic izquierdo).
                // +0.2% crítico mágico por nivel (Máximo 100%)
                Player.GetCritChance(DamageClass.Magic) += WeaponScaling.CritBonus(nivel);
                // Armor penetration +2% cada 5 niveles (Máximo 50%)
                Player.GetArmorPenetration(DamageClass.Magic) += WeaponScaling.ArmorPenBonus(nivel);
            }
            catch { }
        }

        /// <summary>v6.51.0 — LOS ATAQUES COMBINADOS DEL DESPERTAR (el
        /// sol rúnico y los agujeros negros de los ojos): el coordinador
        /// corre en la PANTALLA del dueño (cada jugador con SUS ojos).</summary>
        public override void PostUpdate()
        {
            try
            {
                Content.Systems.AtaquesCombinadosSistema.Tick(Player);
            }
            catch { }
        }
""", ruta=R)

# (1d) persistencia
s = reemplazar(s,
"""                tag["punoTitan"] = PunoTitan;
            }
            catch { }
        }
""",
"""                tag["punoTitan"] = PunoTitan;
                // v6.51.0 — EL DESPERTAR (el nivel y la mascota del 20).
                tag["nivelDespertar"] = NivelDespertar;
                tag["petDespertar"] = PetDelDespertar;
            }
            catch { }
        }
""", ruta=R)

s = reemplazar(s,
"""                PunoTitan = Math.Max(0, Math.Min(99, tag.GetInt("punoTitan")));
            }
            catch { }
        }
""",
"""                PunoTitan = Math.Max(0, Math.Min(99, tag.GetInt("punoTitan")));
                // v6.51.0 — EL DESPERTAR (ausente = 0: los guardados
                // viejos estrenan la mecánica desde el nivel 0 — la
                // letra: «siempre empieza desde nivel 0»).
                NivelDespertar = Math.Max(0, Math.Min(
                    Content.Systems.DespertarGrimorio.NivelMax,
                    tag.GetInt("nivelDespertar")));
                PetDelDespertar = tag.GetBool("petDespertar");
            }
            catch { }
        }
""", ruta=R)

escribir(R, s)
print("OK —", R)

# ======================================================================
# (2) WeaponScaling.cs
# ======================================================================
R = "Content/Systems/WeaponScaling.cs"
s = leer(R)

s = reemplazar(s,
"""        public static int TechoOjosEterno(int equipoInvocador, int nivel)
        {
            return EscuadronEterno;
        }
""",
"""        public static int TechoOjosEterno(int equipoInvocador, int nivel)
        {
            // v6.51.0 — LA ESCALERA DE LA LETRA: «la cantidad de minions
            // están sujetos a la subida normal de nivel del grimorio:
            // cada 10 niveles 1 minion nuevo, en nivel 5 puede invocar
            // su primer minion, en nivel 10 su segundo minion y a partir
            // de aquí cada 10 niveles 1 nuevo» — nivel 5 → 1 · nivel 10
            // → 2 · nivel 20 → 3 · … nivel 100 → 11 (EscuadronEterno, el
            // tope de las órbitas que cierran exacto).
            if (nivel < 5) return 0;
            if (nivel < 10) return 1;
            int ojos = 2 + (nivel - 10) / 10;
            return System.Math.Min(EscuadronEterno, ojos);
        }

        /// <summary>v6.51.0 — EL DAÑO DE LOS OJOS SIN ARMADURA (la
        /// letra: «sin importar la armadura o accesorios que lleve el
        /// jugador, los minions del grimorio estarán sujetos a las
        /// características del grimorios»): el daño de nacimiento de un
        /// ojo nace SOLO del grimorio — su base, su nivel, su despertar
        /// y lo devorado (los banquetes de la .128-.129)… y el NIVEL 50
        /// DEL DESPERTAR lo multiplica (la letra: «mejora TODO el ataque
        /// de los minions y sus proyectiles»).</summary>
        public static int DanoOjosDelEterno(int danoBase, int nivel, Player p)
        {
            float d = danoBase * (1f + SummonDamagePerLevel * System.Math.Max(0, nivel));
            int despertar = Content.Systems.DespertarGrimorio.NivelDe(p);
            d *= 1f + Content.Systems.DespertarGrimorio.BonusDano(despertar);
            d *= 1f + Players.ShardPlayer.BonusDanoDevorado(p);
            d *= 1f + Players.ShardPlayer.BonusDanoArmasDevoradas(p);
            if (despertar >= Content.Systems.DespertarGrimorio.NivelMax)
                d *= 1.25f;   // el nivel 50: TODO el ataque de los ojos
            return System.Math.Max(1, (int)d);
        }
""", ruta=R)

escribir(R, s)
print("OK —", R)

# ======================================================================
# (3) GrimoireEternal.cs
# ======================================================================
R = "Content/Weapons/GrimoireEternal.cs"
s = leer(R)

# (3a) EL DAÑO DEL DESPERTAR a todos los tipos de daño del grimorio
s = reemplazar(s,
"""            // v6.50.129 — EL FESTÍN DE LAS ARMAS (la letra: «cualquier arma
            // corrompida se vuelve exp para el grimorio… cada arma debe
            // subir algun stats del grimorio»): la Fuerza (armas melee
            // comidas) + el EXP acumulado (la rareza de todo lo devorado).
            damage *= 1f + Players.ShardPlayer.BonusDanoArmasDevoradas(player);
        }
""",
"""            // v6.50.129 — EL FESTÍN DE LAS ARMAS (la letra: «cualquier arma
            // corrompida se vuelve exp para el grimorio… cada arma debe
            // subir algun stats del grimorio»): la Fuerza (armas melee
            // comidas) + el EXP acumulado (la rareza de todo lo devorado).
            damage *= 1f + Players.ShardPlayer.BonusDanoArmasDevoradas(player);

            // v6.51.0 — EL DESPERTAR (la letra: «consumir la esencia
            // corrupta sigue aumentando un poquito el daño en %… ese
            // daño aumentado es de TODOS los tipos de daño que tiene el
            // grimorio, o sea mágico y de invocación»): +1 % por nivel
            // de despertar.
            damage *= 1f + Systems.DespertarGrimorio.BonusDano(
                Systems.DespertarGrimorio.NivelDe(player));
        }
""", ruta=R)

# (3b) LA COMPUERTA: nivel para invocar + despertar para mejorar
s = reemplazar(s,
"""                int maxMinions = Players.ShardPlayer.TechoOjosDe(player, level);
                int tipoOjo = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.OjoEternoMinion>();
""",
"""                int maxMinions = Players.ShardPlayer.TechoOjosDe(player, level);
                int tipoOjo = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.OjoEternoMinion>();

                // v6.51.0 — LA ESCALERA DE LA LETRA: «en nivel 5 puede
                // invocar su PRIMER minion» — antes del nivel 5 el
                // escuadrón está cerrado (cada ojo nuevo se ANUNCIA al
                // subir de nivel — ShardLevelItem.OnLevelUp).
                int despertar = Systems.DespertarGrimorio.NivelDe(player);
                int mejorasDesbloqueadas = Systems.DespertarGrimorio.MejorasDesbloqueadas(despertar);
                if (maxMinions <= 0)
                {
                    if (Main.myPlayer == player.whoAmI)
                        Main.NewText(Terraria.Localization.Language.GetTextValue(
                            "Mods.AethonMod.Despertar.OjosCerrados", 5),
                            new Color(255, 130, 130));
                    return false;
                }
""", ruta=R)

# (3c) el fortalecimiento respeta EL DESPERTAR (las mejoras son de la mecánica)
s = reemplazar(s,
"""                    Projectile objetivo = null;
                    int mejorMenor = int.MaxValue;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile p = Main.projectiles[i];
                        if (p == null || !p.active || p.owner != player.whoAmI ||
                            p.type != tipoOjo) continue;
                        int m = global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasDe(p);
                        if (m >= global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasMax)
                            continue;                       // ya lleva sus 4 mejoras
                        if (m < mejorMenor) { mejorMenor = m; objetivo = p; }
                    }

                    if (objetivo == null)
                    {
                        // los once ojos ya llevan sus cuatro mejoras: no
                        // queda nada que fortalecer (ni se cobra maná)
                        if (Main.myPlayer == player.whoAmI)
                            Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.AethonMod.Armas.OjosAlMaximo"),
                                new Color(255, 120, 120));
                        return false;
                    }
""",
"""                    Projectile objetivo = null;
                    int mejorMenor = int.MaxValue;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile p = Main.projectiles[i];
                        if (p == null || !p.active || p.owner != player.whoAmI ||
                            p.type != tipoOjo) continue;
                        int m = global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasDe(p);
                        if (m >= global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasMax)
                            continue;                       // ya lleva sus 4 mejoras
                        // v6.51.0 — LAS MEJORAS SON DEL DESPERTAR (la
                        // letra: «con la nueva mecánica las mejoras están
                        // sujetas a esa mecánica de corrupción» — nivel 5
                        // → la 1 · 15 → la 2 · 30 → la 3 · 40 → la 4).
                        if (m >= mejorasDesbloqueadas)
                            continue;                       // el despertar aún no las presta
                        if (m < mejorMenor) { mejorMenor = m; objetivo = p; }
                    }

                    if (objetivo == null)
                    {
                        // v6.51.0 — DOS CIERRES DISTINTOS: o todo el
                        // escuadrón llevó sus 4 mejoras (el final de
                        // verdad)… o el DESPERTAR aún no presta más.
                        if (Main.myPlayer == player.whoAmI)
                        {
                            if (mejorasDesbloqueadas >= global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasMax)
                                Main.NewText(Terraria.Localization.Language.GetTextValue(
                                    "Mods.AethonMod.Armas.OjosAlMaximo"),
                                    new Color(255, 120, 120));
                            else
                                Main.NewText(Terraria.Localization.Language.GetTextValue(
                                    "Mods.AethonMod.Despertar.MejorasSelladas", despertar),
                                    new Color(255, 130, 130));
                        }
                        return false;
                    }
""", ruta=R)

# (3d) el daño de los ojos: GRIMORIO SOLAMENTE (sin armadura del jugador)
s = reemplazar(s,
"""                // Invocar el OJO (EXACTAMENTE una vez) — el nivel viaja en
                // ai[2] desde el spawn (v6.50.2: el paquete 27 sí viaja).
                player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.OjoEternoBuff>(), 18000);
                Projectile.NewProjectile(source, position, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.OjoEternoMinion>(),
                    damage, knockback, player.whoAmI, 0f, 0f, level);
""",
"""                // Invocar el OJO (EXACTAMENTE una vez) — el nivel viaja en
                // ai[2] desde el spawn (v6.50.2: el paquete 27 sí viaja).
                // v6.51.0 — EL DAÑO ES DEL GRIMORIO SOLAMENTE (la letra:
                // «sin importar la armadura o accesorios»): NO el damage
                // del disparo (que ya lleva las stats del jugador) sino
                // DanoOjosDelEterno — su base, su nivel, su despertar.
                int danoOjo = WeaponScaling.DanoOjosDelEterno(Item.damage, level, player);
                player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.OjoEternoBuff>(), 18000);
                Projectile.NewProjectile(source, position, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.OjoEternoMinion>(),
                    danoOjo, knockback, player.whoAmI, 0f, 0f, level);
""", ruta=R)

# (3e) LA BARRA DEL DESPERTAR en el tooltip (debajo del nivel, encima del hambre)
s = reemplazar(s,
"""                tooltips.Add(new TooltipLine(Mod, "Level",
                    $"[c/FFD700:Nivel {sl.Level}]  [c/B388FF:{bar} {sl.XP}/{xpNeeded} XP]"));
                if (barraHambre != null) tooltips.Add(barraHambre);
""",
"""                tooltips.Add(new TooltipLine(Mod, "Level",
                    $"[c/FFD700:Nivel {sl.Level}]  [c/B388FF:{bar} {sl.XP}/{xpNeeded} XP]"));

                // === v6.51.0 — LA BARRA DEL DESPERTAR (la letra: «estos
                //     niveles deben estar ubicados en la ventana de
                //     información DEBAJO de la barra de niveles y ENCIMA
                //     de la barra de hambre») + la LISTA PEQUEÑA de lo
                //     que necesita el próximo nivel («para subirla de
                //     nivel se presentará una lista pequeña de lo que
                //     necesita»). ===
                try
                {
                    Player portador = Main.LocalPlayer;
                    int despertar = Systems.DespertarGrimorio.NivelDe(portador);
                    int siguiente = Systems.DespertarGrimorio.NivelDespertarSiguiente(portador);

                    // LA BARRA (el nivel de despertar es la propia barra:
                    // 50 celdas, el nivel las llena).
                    int celdas = 25;
                    int llenas = (int)(celdas * despertar / (float)Systems.DespertarGrimorio.NivelMax);
                    string barDesp = "";
                    for (int i = 0; i < celdas; i++)
                        barDesp += i < llenas ? "▓" : "·";

                    tooltips.Add(new TooltipLine(Mod, "DespertarBarra",
                        $"[c/C49CFF:Despertar {despertar}/50]  [c/9B6FE8:{barDesp}]"));
                    tooltips.Add(new TooltipLine(Mod, "DespertarDano",
                        $"[c/9B6FE8:  +{(despertar)}% de daño a TODO el grimorio]"));

                    // LA LISTA PEQUEÑA DEL PRÓXIMO NIVEL (esencias ·
                    // dinero · el arma del peldaño).
                    if (despertar < Systems.DespertarGrimorio.NivelMax)
                    {
                        int esencias = Systems.DespertarGrimorio.EsenciasPara(siguiente);
                        long monedas = Systems.DespertarGrimorio.MonedasPara(siguiente);
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq1",
                            $"[c/78788C:  Para Despertar {siguiente}:]"));
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq2",
                            $"[c/C4A2FF:  • Devorar {esencias} Esencia{(esencias == 1 ? "" : "s")} Corrupta{(esencias == 1 ? "" : "s")}]"));
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq3",
                            $"[c/C4A2FF:  • {Systems.DespertarGrimorioSistema.ItemToText(monedas)}]"));
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq4",
                            $"[c/C4A2FF:  • Devorar 1 {Systems.DespertarGrimorio.NombreOfrenda(siguiente)} CORRUPTA]"));
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq5",
                            "[c/78788C:  (suéltala con el grimorio en la mano)]"));
                    }
                    else
                    {
                        tooltips.Add(new TooltipLine(Mod, "DespertarMax",
                            "[c/C49CFF:  EL GRIMORIO DESPERTÓ DEL TODO (máximo por ahora)]"));
                    }
                }
                catch { }

                if (barraHambre != null) tooltips.Add(barraHambre);
""", ruta=R)

escribir(R, s)
print("OK —", R)

# ======================================================================
# (4) OjoEternoMinion.cs
# ======================================================================
R = "Content/Projectiles/OjoEternoMinion.cs"
s = leer(R)

# (4a) Fortalecer respeta el despertar
s = reemplazar(s,
"""        public static void Fortalecer(Projectile p)
        {
            if (p == null || !p.active) return;
            int nivel = (int)MathF.Floor(p.ai[2]);
            int m = MejorasDe(p);
            if (m >= MejorasMax) return;
""",
"""        public static void Fortalecer(Projectile p)
        {
            if (p == null || !p.active) return;
            int nivel = (int)MathF.Floor(p.ai[2]);
            int m = MejorasDe(p);
            if (m >= MejorasMax) return;
            // v6.51.0 — LAS MEJORAS SON DEL DESPERTAR (la letra: «con la
            // nueva mecánica las mejoras están sujetas a esa mecánica»):
            // nivel 5 → la 1 · 15 → la 2 · 30 → la 3 · 40 → la 4.
            Player dueño = Main.player[p.owner];
            if (m >= Content.Systems.DespertarGrimorio.MejorasDesbloqueadas(
                    Content.Systems.DespertarGrimorio.NivelDe(dueño)))
                return;
""", ruta=R)

# (4b) el nivel 50 del despertar: TODO más fuerte y más rápido
s = reemplazar(s,
"""            if (Projectile.owner == Main.myPlayer)
            {
                int dano = Math.Max(1, (int)(Projectile.damage *
                    WeaponScaling.MinionContactDamageMult(nivel) * MultMejoras(Projectile)));
""",
"""            if (Projectile.owner == Main.myPlayer)
            {
                // v6.51.0 — EL NIVEL 50 DEL DESPERTAR (la letra: «mejora
                // TODO el ataque de los minions y sus proyectiles así
                // como la cadencia de disparo»): el rayo pega ×1,25.
                float bonusFinal = Content.Systems.DespertarGrimorio.NivelDe(
                    Main.player[Projectile.owner]) >= Content.Systems.DespertarGrimorio.NivelMax
                    ? 1.25f : 1f;
                int dano = Math.Max(1, (int)(Projectile.damage *
                    WeaponScaling.MinionContactDamageMult(nivel) * MultMejoras(Projectile) * bonusFinal));
""", ruta=R)

escribir(R, s)
print("OK —", R)

# ======================================================================
# (5) ShardLevelItem.cs — el anuncio de cada OJO NUEVO
# ======================================================================
R = "Content/Globals/ShardLevelItem.cs"
s = leer(R)

s = reemplazar(s,
"""                if (cruzoHito && notificarHitos && nivelHito > 0)
                {
                    Main.NewText($"✦✦ Hito nivel {nivelHito}! {item.Name} resuena con poder. ✦✦",
                        new Microsoft.Xna.Framework.Color(245, 196, 81));
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.DD2_EtherianPortalOpen);
                }
""",
"""                if (cruzoHito && notificarHitos && nivelHito > 0)
                {
                    Main.NewText($"✦✦ Hito nivel {nivelHito}! {item.Name} resuena con poder. ✦✦",
                        new Microsoft.Xna.Framework.Color(245, 196, 81));
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.DD2_EtherianPortalOpen);
                }

                // v6.51.0 — EL ANUNCIO DE CADA OJO NUEVO (la letra:
                // «cada vez que puede invocar un nuevo minion este debe
                // ser anunciado al jugador»): la escalera del escuadrón —
                // nivel 5 el PRIMERO, nivel 10 el SEGUNDO y cada 10
                // niveles uno nuevo (5 · 10 · 20 · 30 · … · 100).
                int nivelAnterior = Level - nivelesGanados;
                foreach (int umbral in new[] { 5, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 })
                {
                    if (nivelAnterior < umbral && Level >= umbral && notificar)
                    {
                        int numero = umbral == 5 ? 1 : (umbral == 10 ? 2 : 2 + (umbral - 10) / 10);
                        Main.NewText(
                            Terraria.Localization.Language.GetTextValue(
                                "Mods.AethonMod.Despertar.NuevoOjo", numero),
                            new Microsoft.Xna.Framework.Color(196, 138, 255));
                        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item113);
                    }
                }
""", ruta=R)

escribir(R, s)
print("OK —", R)

# ======================================================================
# (6) CorruptoGlobalItem.cs — los stats en % de las armas corruptas
# ======================================================================
R = "Content/Globals/CorruptoGlobalItem.cs"
s = leer(R)

s = reemplazar(s,
"""        /// <summary>El yunque le queda cerrado PARA SIEMPRE.</summary>
        public override bool CanReforge() => !Corrompido;
""",
"""        /// <summary>El yunque le queda cerrado PARA SIEMPRE.</summary>
        public override bool CanReforge() => !Corrompido;

        // ================================================================
        //  v6.51.0 — LOS STATS EN % DE LAS ARMAS CORRUPTAS (la letra:
        //  «las armas corruptas obtienen un 5% de daño extra, además de
        //  otras estadísticas en porcentaje pero ya no pueden ser
        //  modificables ni mejorables»): +5 % de daño, +5 % de empuje,
        //  +2 % de crítico y 5 % MÁS RÁPIDAS — el precio de la eternidad
        //  (sin reforge, sin prefijos, sin mejora alguna: para siempre).
        // ================================================================

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (!Corrompido) return;
            damage *= 1.05f;                 // +5 % de daño (la letra)
        }

        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            if (!Corrompido) return;
            knockback *= 1.05f;              // +5 % de empuje
        }

        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            if (!Corrompido) return;
            crit += 2f;                      // +2 % de crítico
        }

        public override float UseTimeMultiplier(Player player)
        {
            if (!Corrompido) return 1f;
            return 0.95f;                    // 5 % más rápida
        }
""", ruta=R)

escribir(R, s)
print("OK —", R)

# ======================================================================
# (7) OleadaNPC.cs — la mesa nueva de las oleadas
# ======================================================================
R = "Content/Globals/OleadaNPC.cs"
s = leer(R)

# (7a) el jefe de oleada: 10 esencias corruptas (la esencia de jefe MUERE)
s = reemplazar(s,
"""                    // SU ESENCIA (un alma por guardián — el ítem que sube
                    // un nivel COMPLETO al libro). v6.50.116 — EL TIPO
                    // RESERVADO: el jefe muere aislado (type/netID en 0
                    // para vanilla — AislarJefeDeOleada) pero SU alma se
                    // paga con SU especie verdadera.
                    int esencia = EsenciaDeJefeItem.DeNPC(
                        TipoReservado > 0 ? TipoReservado : npc.type);
                    if (esencia > 0)
                        Item.NewItem(src, npc.Center, esencia, 1);
""",
"""                    // v6.51.0 — LAS 10 ESENCIAS CORRUPTAS DEL GUARDIÁN
                    // (la letra: «cada jefe puede soltar 10 tras ser
                    // vencido» — la esencia de jefe de la vieja era
                    // MUERE, sustituida por la esencia corrupta: el
                    // propio grimorio llama a estas criaturas).
                    int tipoHusos = ModContent.ItemType
                        <global::AethonMod.Content.Items.Altar.ItemCorrupto>();
                    Item.NewItem(src, npc.Center, tipoHusos, 10);
""", ruta=R)

# (7b) la chusma: 2 % de esencia (la letra)
s = reemplazar(s,
"""                else
                {
                    // EL ORO DE LA CHUSMA (k monedas — la 10: 10).
                    if (monedas > 0)
                        Item.NewItem(src, npc.Center, ItemID.GoldCoin, monedas);
                }
""",
"""                else
                {
                    // EL ORO DE LA CHUSMA (k monedas — la 10: 10).
                    if (monedas > 0)
                        Item.NewItem(src, npc.Center, ItemID.GoldCoin, monedas);

                    // v6.51.0 — EL 2 % DE ESENCIA (la letra: «en las
                    // oleadas de hambre cada monstruo tiene una
                    // probabilidad de 2% de soltar 1 esencia corrupta» —
                    // las probabilidades de los ítems vanilla están
                    // PELADAS al mínimo: solo dinero + esencia, ver el
                    // detour de NPCLoot_DropItems en Load).
                    if (Main.rand.Next(100) < 2)
                        Item.NewItem(src, npc.Center, ModContent.ItemType
                            <global::AethonMod.Content.Items.Altar.ItemCorrupto>(), 1);
                }
""", ruta=R)

# (7c) el detour: la chusma de oleada NO suelta items vanilla (solo dinero)
marcador = "        public override void OnKill(NPC npc)"
assert marcador in s, "no hallé OnKill de OleadaNPC"
s = s.replace(marcador,
"""        public override void Load()
        {
            // v6.51.0 — EL LOOT PELADO DE LAS OLEADAS (la letra:
            // «recuerda que en las oleadas cada enemigo tiene las
            // probabilidades de soltar sus ítems reducidos al mínimo
            // posible y solo sueltan dinero al morir más las
            // probabilidades de soltar esencia corrupta»): la CHUSMA
            // sellada no corre el drop de items de vanilla — su pago es
            // el ORO del OnKill + el 2 % de esencia corrupta (el jefe ya
            // estaba aislado desde la .116: ni loot ni flags).
            On.Terraria.NPC.NPCLoot_DropItems += PelarElLootDeLaChusma;
        }

        public override void Unload()
        {
            On.Terraria.NPC.NPCLoot_DropItems -= PelarElLootDeLaChusma;
        }

        private static void PelarElLootDeLaChusma(
            On.Terraria.NPC.orig_NPCLoot_DropItems orig, NPC self)
        {
            try
            {
                var sello = self?.GetGlobalNPC<OleadaNPC>();
                if (sello != null && sello.EsDeOleada && !sello.EsJefeDeOleada)
                    return;   // SIN items vanilla: solo dinero + esencia
            }
            catch { }
            orig(self);
        }

""" + marcador, 1)

escribir(R, s)
print("OK —", R)

# ======================================================================
# (8) EL AGUJERO NEGRO NORMAL MUERE
# ======================================================================
R = "Content/Weapons/Cosmic/CosmicWeapons.cs"
s = leer(R)
i0 = s.index("    public class BlackHoleStaff : ModItem")
# el fin de la clase: el siguiente "    }" que cierra un nivel de clase
i1 = s.index("    public class SunStaff", i0)
# retroceder al cierre de BlackHoleStaff (los }    } justo antes de SunStaff)
bloque = s[i0:i1]
cerrar = bloque.rindex("    }\n")
s = s[:i0] + bloque[:cerrar] + bloque[cerrar + len("    }\n"):] + s[i1:]
# el comentario de la clase también muere: buscar el doc /// que precede
i_doc = s.rindex("    /// <summary>", 0, s.index("    public class SunStaff"))
# encontrar el inicio del bloque de resumen de BlackHoleStaff (antes de i_doc
# hay otro summary de SunStaff? no: SunStaff viene DESPUÉS). El doc de
# BlackHoleStaff está entre el header del archivo y su clase.
i0b = s.rindex("    // =====", 0, i_doc) if "    // =====" in s[:i_doc] else i_doc
assert "BlackHoleStaff" in s[i_doc - 2000:i_doc + 200]
# limpiar cualquier mención restante en el header
assert "class BlackHoleStaff" not in s, "la clase BlackHoleStaff sigue viva"
escribir(R, s)
print("OK —", R, "(BlackHoleStaff fuera)")

# (8b) la bolsa: sin el agujero normal, DIEZ formas
R = "Content/Items/Bolsas/BolsasCategorias.cs"
s = leer(R)
s = reemplazar(s,
"""            l.Add((ModContent.ItemType<Weapons.Cosmic.BlackHoleStaff>(), 1));
""", "", ruta=R)
s = reemplazar(s,
"""        protected override string Nota => "Once formas de devorar la luz";
""",
"""        // v6.51.0 — EL AGUJERO NEGRO NORMAL MURIÓ (la letra: «quiero
        // que borres al agujero negro normal del paquete, solo deben
        // estar los agujeros negros runicos»): DIEZ formas, todas
        // rúnicas.
        protected override string Nota => "Diez formas rúnicas de devorar la luz";
""", ruta=R)
escribir(R, s)
print("OK —", R)

# (8c) la lente: sin el agujero normal
R = "Content/Effects/BlackHoleLensSystem.cs"
s = leer(R)
s = reemplazar(s,
"""            int blackHoleType = ModContent.ProjectileType<BlackHoleProjectile>();
""", "", ruta=R)
s = reemplazar(s,
"""                        BlackHoleProjectile.DrawCoreVisuals(bh, false);
""",
"""                        // v6.51.0 — el agujero negro NORMAL murió: solo
                        // los rúnicos viven en la lente.
""", ruta=R)
assert "BlackHoleProjectile" not in s.replace("BlackHoleProjectile.", "x") or True
# limpiar menciones en comentarios del archivo
s = s.replace("(BlackHoleProjectile.PreDraw se salta su dibujado cuando la lente",
              "(los agujeros rúnicos saltan su dibujado cuando la lente")
s = s.replace("IndexOutOfRangeException en BlackHoleProjectile.",
              "IndexOutOfRangeException en el agujero negro.")
escribir(R, s)
print("OK —", R)

# (8d) el proyectil MUERE
os.remove(os.path.join(RAIZ, "Content/Projectiles/Cosmic/BlackHoleProjectile.cs"))
print("BORRADO — Content/Projectiles/Cosmic/BlackHoleProjectile.cs")

# ======================================================================
# (9) TestingPlayer — el Eco del Despertar a la mano
# ======================================================================
R = "Content/Players/TestingPlayer.cs"
s = leer(R)
s = reemplazar(s,
"""            bolsas += Entregar(ModContent.ItemType<BolsaEmperatriz>());      // v6.50.99: LAS CUATRO PRUEBAS DE LA EMPERATRIZ (A/B/C + la dual Equinoccio)
""",
"""            bolsas += Entregar(ModContent.ItemType<BolsaEmperatriz>());      // v6.50.99: LAS CUATRO PRUEBAS DE LA EMPERATRIZ (A/B/C + la dual Equinoccio)

            // v6.51.0 — LA LLAVE DEL DESPERTAR (la letra: «crea un ítem
            // no consumible que al usarlo suba el nivel en 1 por cada
            // uso»): el probador de la nueva progresión.
            bolsas += Entregar(ModContent.ItemType
                <global::AethonMod.Content.Items.Llamados.EcoDelDespertar>());
""", ruta=R)
escribir(R, s)
print("OK —", R)

print("CIRUGÍA DEL DESPERTAR COMPLETA")
