using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  LOS ATAQUES COMBINADOS DE LOS OJOS (v6.51.0) — LOS PODERES DEL
    //  DESPERTAR, LA LETRA COMPLETA:
    //
    //  · EL SOL RÚNICO (nivel 7+): «2 minions con la primera mejora
    //    lanzan su ataque en un punto cercano encima del jugador, los 2
    //    rayos de perlin que lanzan convergen en un punto y de ahí crece
    //    un sol runico de 1 anillo, los minions que están creando el sol
    //    hacen una animación y lanzan con fuerza el sol runico al
    //    enemigo haciendo daño de área, quemando… este ataque se puede
    //    hacer 2 veces por minuto y de forma aleatoria» — nivel 17: DOS
    //    ANILLOS y 3/min; nivel 35: TRES ANILLOS y 4/min.
    //
    //  · LOS AGUJEROS NEGROS (nivel 10+): «al tener 5 o más minion con
    //    la primera mejora, estos pueden combinar su ataque y lanzar un
    //    agujero negro aleatorio de 4 listados: Agujero del Olvido,
    //    Agujero Cósmico, Agujero del Umbral y el Agujero de la Bruma…
    //    solo lanzarán uno de esos 4 aleatoriamente cada minuto» — nivel
    //    25: la versión ASCENDIDA; nivel 45: 2/min Y EL SUPREMO
    //    independiente («Agujero Negro Supremo y Agujero Supremo Aurora…
    //    el ataque combinado de 8 minions o más en su mejora número 4,
    //    siempre se puede lanzar uno de esos cada minuto»).
    //
    //  LA MÁQUINA: cada jugador corre SU coordinador en SU pantalla (el
    //  dueño de los ojos es la autoridad de sus propios proyectiles — el
    //  patrón del Disparar de OjoEternoMinion: owner == Main.myPlayer);
    //  los relojes viven aquí y se limpian con el mundo.
    // ======================================================================
    public class AtaquesCombinadosSistema : ModSystem
    {
        /// <summary>EL RELOJ del sol de cada jugador (GameUpdateCount en
        /// que podrá volver a nacer).</summary>
        private static readonly Dictionary<int, ulong> _solHasta = new();

        /// <summary>EL RELOJ de los agujeros de cada jugador.</summary>
        private static readonly Dictionary<int, ulong> _agujeroHasta = new();

        /// <summary>EL RELOJ del supremo de cada jugador (independiente).</summary>
        private static readonly Dictionary<int, ulong> _supremoHasta = new();

        /// <summary>El ruido de la moneda aleatoria (por tick, cuando el
        /// reloj ya está listo: 1 de cada ~75 t dispara).</summary>
        private const int DADO = 75;

        /// <summary>El radio de caza del enemigo blanco.</summary>
        private const float RADIO_CAZA = 1400f;

        public override void OnWorldUnload()
        {
            _solHasta.Clear();
            _agujeroHasta.Clear();
            _supremoHasta.Clear();
        }

        /// <summary>UN TICK DEL COORDINADOR (lo llama ShardPlayer.PostUpdate
        /// — SOLO en la pantalla del dueño de los ojos).</summary>
        public static void Tick(Player p)
        {
            try
            {
                if (p == null || !p.active || p.dead) return;
                if (Main.gameMenu) return;
                // LA PANTALLA DEL DUEÑO (SP: myPlayer; MP: cada cliente
                // con SUS ojos; el host con los suyos).
                if (p.whoAmI != Main.myPlayer || Main.dedServ) return;

                int despertar = DespertarGrimorio.NivelDe(p);
                if (despertar < 7) return;   // el primer poder nace en el 7

                // === EL ESCUADRÓN: cuántos ojos, cuántos con mejora, ===
                // === cuántos en su mejora FINAL                    ===
                int tipoOjo = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.OjoEternoMinion>();
                var mejorados = new List<Projectile>();
                int mejoras4 = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile pr = Main.projectile[i];
                    if (pr == null || !pr.active || pr.type != tipoOjo) continue;
                    if (pr.owner != p.whoAmI) continue;
                    int m = global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasDe(pr);
                    if (m >= 1) mejorados.Add(pr);
                    if (m >= 4) mejoras4++;
                }
                if (mejorados.Count == 0) return;

                // === EL ENEMIGO BLANCO (el más cercano y de verdad) ===
                NPC blanco = null;
                float mejorD = RADIO_CAZA * RADIO_CAZA;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    // v6.51.1 — LA CURA (la letra: «no son lanzados por los
                    // minions cuando deben ser lanzados»): el filtro viejo
                    // tenía el AMISTOSO invertido — SALTABA A LOS ENEMIGOS y
                    // solo dejaba pasar a los vecinos del pueblo (que
                    // EsObjetivo rechaza): el blanco era SIEMPRE null y los
                    // ataques combinados JAMÁS disparaban. Ahora: solo vivos
                    // — EsObjetivo hace el resto (ni amistosos ni intocables).
                    if (n == null || !n.active) continue;
                    if (!VFXCore.EsObjetivo(n)) continue;
                    float d = n.Center.DistanceSQ(p.Center);
                    if (d < mejorD) { mejorD = d; blanco = n; }
                }
                if (blanco == null) return;

                ulong ahora = Main.GameUpdateCount;

                // === EL SOL RÚNICO (2+ ojos mejorados, nivel 7+) ===
                int solesMin = DespertarGrimorio.SolesPorMinuto(despertar);
                if (solesMin > 0 && mejorados.Count >= 2)
                {
                    ulong hasta = _solHasta.TryGetValue(p.whoAmI, out ulong v) ? v : 0ul;
                    if (ahora >= hasta && Main.rand.Next(DADO) == 0)
                    {
                        _solHasta[p.whoAmI] = ahora + 3600ul / (ulong)solesMin;
                        DispararElSol(p, blanco, mejorados, despertar);
                    }
                }

                // === LOS AGUJEROS NEGROS (5+ ojos mejorados, nivel 10+) ===
                int agujerosMin = DespertarGrimorio.AgujerosPorMinuto(despertar);
                if (agujerosMin > 0 && mejorados.Count >= 5)
                {
                    ulong hasta = _agujeroHasta.TryGetValue(p.whoAmI, out ulong v) ? v : 0ul;
                    if (ahora >= hasta && Main.rand.Next(DADO) == 0)
                    {
                        _agujeroHasta[p.whoAmI] = ahora + 3600ul / (ulong)agujerosMin;
                        LanzarElAgujero(p, blanco, mejorados, despertar);
                    }
                }

                // === EL SUPREMO (8+ ojos en su MEJORA 4, nivel 45+) ===
                if (DespertarGrimorio.SupremoDisponible(despertar) && mejoras4 >= 8)
                {
                    ulong hasta = _supremoHasta.TryGetValue(p.whoAmI, out ulong v) ? v : 0ul;
                    if (ahora >= hasta && Main.rand.Next(DADO) == 0)
                    {
                        _supremoHasta[p.whoAmI] = ahora + 3600ul;   // 1/min SIEMPRE
                        LanzarElSupremo(p, blanco, mejorados, despertar);
                    }
                }
            }
            catch { }
        }

        // ================================================================
        //  EL SOL RÚNICO COMBINADO
        // ================================================================

        /// <summary>LA CEREMONIA DEL SOL (v6.51.2 — LA LETRA: «para
        /// los soles rúnicos el rayo convergente tome 2 SEGUNDOS en
        /// crearlo y lanzarlo al enemigo… deben aparecer encima del
        /// jugador»): los DOS ojos con más mejoras DISPARAN sus rayos de
        /// perlin (la reverencia de los celebrantes) y ENCIMA DEL
        /// JUGADOR nace EL RAYO CONVERGENTE — 2 segundos de rayos que
        /// convergen y CREAN el sol — y al cumplir, el sol sale
        /// DISPARADO al enemigo (su homing hace el resto: daño de área,
        /// quemadura y la nova final).</summary>
        private static void DispararElSol(Player p, NPC blanco,
            List<Projectile> mejorados, int despertar)
        {
            try
            {
                // LOS DOS CELEBRANTES (los más despiertos).
                var celebrantes = new List<Projectile>(mejorados);
                celebrantes.Sort((a, b) =>
                    global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasDe(b)
                    - global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasDe(a));
                Projectile ojo1 = celebrantes[0];
                Projectile ojo2 = celebrantes.Count > 1 ? celebrantes[1] : celebrantes[0];

                int nivelOjo = (int)MathF.Floor(ojo1.ai[2]);
                float multMejoras = global::AethonMod.Content.Projectiles.OjoEternoMinion
                    .MultMejoras(ojo1);
                int danoRayo = Math.Max(1, (int)(ojo1.damage *
                    WeaponScaling.MinionContactDamageMult(nivelOjo) * multMejoras));

                // EL DAÑO DEL SOL QUE NACERÁ: crecido por la combinación
                // y el despertar (viaja en ai[2] del ritual — ya lleva el
                // grimorio Y las armaduras del portador).
                int anillos = DespertarGrimorio.AnillosDelSol(despertar);
                int danoSol = Math.Max(1, (int)(danoRayo * 3f *
                    (1f + DespertarGrimorio.BonusDano(despertar))));

                // LA ANIMACIÓN DE LOS CELEBRANTES: disparan sus rayos al
                // blanco (la reverencia que ABRRE el ritual) y estallan en
                // chispas doradas.
                foreach (Projectile ojo in new[] { ojo1, ojo2 })
                {
                    if (ojo == null || !ojo.active) continue;
                    Projectile.NewProjectile(ojo.GetSource_FromAI(),
                        blanco.Center, Vector2.Zero,
                        ModContent.ProjectileType
                            <global::AethonMod.Content.Projectiles.Cosmic.TruenoPerlinProjectile>(),
                        danoRayo, ojo.knockBack * 0.6f, p.whoAmI,
                        1f, ojo.whoAmI, blanco.whoAmI);
                    for (int k = 0; k < 10; k++)
                    {
                        Dust d = Dust.NewDustPerfect(ojo.Center, DustID.GoldFlame,
                            Main.rand.NextVector2Circular(3.5f, 3.5f), 128, default, 1.2f);
                        d.noGravity = true;
                    }
                }

                // EL RAYO CONVERGENTE: 2 SEGUNDOS de ceremonia ENCIMA DEL
                // JUGADOR — al cumplir, EL SOL nace y sale al enemigo.
                Projectile.NewProjectile(
                    p.GetSource_Misc("SolCombinado"),
                    p.Center + new Vector2(0f, -150f), Vector2.Zero,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Cosmic.RayoConvergenteProjectile>(),
                    0, 0f, p.whoAmI,
                    1f,                          // EL MODO: SOL
                    blanco.whoAmI * 1f,          // el blanco elegido
                    danoSol * 1f);               // el daño combinado

                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(-0.2f), p.Center);
            }
            catch { }
        }

        // ================================================================
        //  LOS AGUJEROS NEGROS COMBINADOS
        // ================================================================

        /// <summary>EL RITUAL DEL AGUJERO (v6.51.2 — LA LETRA: «para
        /// los agujeros negros rúnicos tome 3 SEGUNDOS»): con 5+ ojos
        /// mejorados, el escuadrón abre ENCIMA DEL JUGADOR el RAYO
        /// CONVERGENTE de 3 segundos — y al cumplir, uno de los CUATRO
        /// RÚNICOS al azar (nivel 25+: su versión ASCENDIDA) se ABRE
        /// sobre la presa. EL DAÑO nace de los ojos combinados.</summary>
        private static void LanzarElAgujero(Player p, NPC blanco,
            List<Projectile> mejorados, int despertar)
        {
            try
            {
                bool ascendidos = DespertarGrimorio.AgujerosAscendidos(despertar);

                // EL DAÑO COMBINADO: el rayo del ojo más despierto × los
                // celebrantes que abren la puerta (×1,45 en ascendidos).
                Projectile lider = mejorados[0];
                int nivelOjo = (int)MathF.Floor(lider.ai[2]);
                float multMejoras = global::AethonMod.Content.Projectiles.OjoEternoMinion
                    .MultMejoras(lider);
                int danoRayo = Math.Max(1, (int)(lider.damage *
                    WeaponScaling.MinionContactDamageMult(nivelOjo) * multMejoras));
                int dano = Math.Max(1, (int)(danoRayo * (ascendidos ? 4.5f : 3.2f) *
                    (1f + DespertarGrimorio.BonusDano(despertar))));

                // EL RAYO CONVERGENTE: 3 SEGUNDOS de ceremonia — al
                // cumplir, la puerta (el agujero rúnico al azar — los
                // ascendidos si el despertar los trajo) se abre SOBRE la
                // presa (la elección del tipo la hace el ritual al
                // cumplir: es UN AZAR de cada lanzamiento, como manda la
                // letra).
                Projectile.NewProjectile(
                    p.GetSource_Misc("AgujeroCombinado"),
                    p.Center + new Vector2(0f, -150f), Vector2.Zero,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Cosmic.RayoConvergenteProjectile>(),
                    0, 0f, p.whoAmI,
                    2f,                          // EL MODO: AGUJERO
                    blanco.whoAmI * 1f,
                    dano * 1f);

                // EL TEATRO DE LA APERTURA (las chispas del escuadrón).
                foreach (Projectile ojo in mejorados)
                {
                    Dust d = Dust.NewDustPerfect(ojo.Center, DustID.Shadowflame,
                        Main.rand.NextVector2Circular(2.5f, 2.5f), 140,
                        new Color(140, 60, 210), 1f);
                    d.noGravity = true;
                }
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(0.35f), p.Center);
            }
            catch { }
        }

        /// <summary>EL SUPREMO (nivel 45+): «un nuevo tipo de agujero negro
        /// runico independiente… Agujero Negro Supremo y Agujero Supremo
        /// Aurora… su daño es aún mayor que el de los agujeros negros
        /// ascendidos y se necesitan el ataque combinado de 8 minions o
        /// más en su mejora número 4» — uno por minuto, al azar entre los
        /// DOS. v6.51.2: SU RITUAL TAMBIÉN (el rayo convergente de 3
        /// segundos — la carta fuerte se anuncia).</summary>
        private static void LanzarElSupremo(Player p, NPC blanco,
            List<Projectile> mejorados, int despertar)
        {
            try
            {
                Projectile lider = mejorados[0];
                int nivelOjo = (int)MathF.Floor(lider.ai[2]);
                float multMejoras = global::AethonMod.Content.Projectiles.OjoEternoMinion
                    .MultMejoras(lider);
                int danoRayo = Math.Max(1, (int)(lider.damage *
                    WeaponScaling.MinionContactDamageMult(nivelOjo) * multMejoras));
                int dano = Math.Max(1, (int)(danoRayo * 6.5f *
                    (1f + DespertarGrimorio.BonusDano(despertar))));

                // EL RAYO CONVERGENTE DEL SUPREMO: 3 segundos — al cumplir,
                // el SUPREMO o el AURORA (al azar) se abre sobre la presa.
                Projectile.NewProjectile(
                    p.GetSource_Misc("SupremoCombinado"),
                    p.Center + new Vector2(0f, -150f), Vector2.Zero,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Cosmic.RayoConvergenteProjectile>(),
                    0, 0f, p.whoAmI,
                    3f,                          // EL MODO: SUPREMO
                    blanco.whoAmI * 1f,
                    dano * 1f);

                // LA SOLEMNIDAD: TODO el escuadrón estalla (la carta
                // fuerte del grimorio despierto).
                foreach (Projectile ojo in mejorados)
                {
                    for (int k = 0; k < 6; k++)
                    {
                        Dust d = Dust.NewDustPerfect(ojo.Center, DustID.PurpleTorch,
                            Main.rand.NextVector2Circular(3.5f, 3.5f), 150,
                            new Color(190, 130, 255), 1.2f);
                        d.noGravity = true;
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(-0.5f), p.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, p.Center);
            }
            catch { }
        }
    }
}
