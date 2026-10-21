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

        /// <summary>LA CEREMONIA DEL SOL: los DOS ojos con más mejoras
        /// DISPARAN sus rayos de perlin al blanco (la animación de la
        /// letra)… y de un punto ENCIMA DEL JUGADOR nace EL SOL RÚNICO
        /// con sus anillos, LANZADO CON FUERZA al enemigo (el homing del
        /// sol hace el resto: daño de área, quemadura y la nova final).</summary>
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

                // EL PUNTO DE LA CONVERGENCIA: un punto cercano ENCIMA del
                // jugador (la letra) — los rayos de los dos celebrantes se
                // cruzan ahí y el sol CREE.
                Vector2 punto = p.Center + new Vector2(
                    Main.rand.NextFloat(-40f, 40f), -150f);

                // LA ANIMACIÓN: los celebrantes disparan sus rayos al
                // blanco (el cauce de siempre del ojo — TruenoPerlin) y
                // estallan en chispas doradas (el esfuerzo del conjuro).
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

                // EL SOL NACE: sus ANILLOS por despertar (1 · 2 en el 17 ·
                // 3 en el 35), su daño CRECIDO por la combinación, y
                // LANZADO CON FUERZA al enemigo (velocidad inicial fuerte
                // + el homing del sol de siempre).
                int anillos = DespertarGrimorio.AnillosDelSol(despertar);
                int danoSol = Math.Max(1, (int)(danoRayo * 3f *
                    (1f + DespertarGrimorio.BonusDano(despertar))));
                Vector2 rumbo = blanco.Center - punto;
                if (rumbo.LengthSquared() < 1f) rumbo = -Vector2.UnitY;
                rumbo.Normalize();
                Projectile sol = Projectile.NewProjectileDirect(
                    p.GetSource_Misc("SolCombinado"),
                    punto, rumbo * 7.5f,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Cosmic.RuneSunProjectile>(),
                    danoSol, 4f, p.whoAmI, anillos,
                    (p.whoAmI * 31 + Main.rand.Next(9973)) * 1f);
                sol.timeLeft = 480;   // 8 s de gloria (y su nova)

                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(-0.2f), punto);
            }
            catch { }
        }

        // ================================================================
        //  LOS AGUJEROS NEGROS COMBINADOS
        // ================================================================

        /// <summary>EL AGUJERO ALEATORIO DE LOS CUATRO (la letra): con 5+
        /// ojos mejorados, el escuadrón abre UNO de los CUATRO RÚNICOS al
        /// azar sobre el enemigo (nivel 25+: su versión ASCENDIDA — «hacen
        /// aún más daño»). EL DAÑO nace de los ojos combinados.</summary>
        private static void LanzarElAgujero(Player p, NPC blanco,
            List<Projectile> mejorados, int despertar)
        {
            try
            {
                bool ascendidos = DespertarGrimorio.AgujerosAscendidos(despertar);
                int tipo = (Main.rand.Next(4)) switch
                {
                    0 => ascendidos
                        ? ModContent.ProjectileType<Projectiles.Cosmic.OlvidoAscendidoBlackHoleProjectile>()
                        : ModContent.ProjectileType<Projectiles.Cosmic.OlvidoBlackHoleProjectile>(),
                    1 => ascendidos
                        ? ModContent.ProjectileType<Projectiles.Cosmic.CosmicAscendidoBlackHoleProjectile>()
                        : ModContent.ProjectileType<Projectiles.Cosmic.CosmicBlackHoleProjectile>(),
                    2 => ascendidos
                        ? ModContent.ProjectileType<Projectiles.Cosmic.UmbralAscendidoBlackHoleProjectile>()
                        : ModContent.ProjectileType<Projectiles.Cosmic.UmbralBlackHoleProjectile>(),
                    _ => ascendidos
                        ? ModContent.ProjectileType<Projectiles.Cosmic.BrumaAscendidoBlackHoleProjectile>()
                        : ModContent.ProjectileType<Projectiles.Cosmic.BrumaBlackHoleProjectile>(),
                };

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

                // LA PUERTA SE ABRE SOBRE EL ENEMIGO.
                Projectile agujero = Projectile.NewProjectileDirect(
                    p.GetSource_Misc("AgujeroCombinado"),
                    blanco.Center - new Vector2(0f, 60f), Vector2.Zero,
                    tipo, dano, 0f, p.whoAmI);
                agujero.timeLeft = 600;

                // EL TEATRO DE LA APERTURA (las chispas del escuadrón).
                foreach (Projectile ojo in mejorados)
                {
                    Dust d = Dust.NewDustPerfect(ojo.Center, DustID.Shadowflame,
                        Main.rand.NextVector2Circular(2.5f, 2.5f), 140,
                        new Color(140, 60, 210), 1f);
                    d.noGravity = true;
                }
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(0.35f), blanco.Center);
            }
            catch { }
        }

        /// <summary>EL SUPREMO (nivel 45+): «un nuevo tipo de agujero negro
        /// runico independiente… Agujero Negro Supremo y Agujero Supremo
        /// Aurora… su daño es aún mayor que el de los agujeros negros
        /// ascendidos y se necesitan el ataque combinado de 8 minions o
        /// más en su mejora número 4» — uno por minuto, al azar entre los
        /// DOS, sobre el enemigo.</summary>
        private static void LanzarElSupremo(Player p, NPC blanco,
            List<Projectile> mejorados, int despertar)
        {
            try
            {
                int tipo = Main.rand.NextBool()
                    ? ModContent.ProjectileType<Projectiles.Cosmic.SupremoBlackHoleProjectile>()
                    : ModContent.ProjectileType<Projectiles.Cosmic.SupremoAuroraBlackHoleProjectile>();

                Projectile lider = mejorados[0];
                int nivelOjo = (int)MathF.Floor(lider.ai[2]);
                float multMejoras = global::AethonMod.Content.Projectiles.OjoEternoMinion
                    .MultMejoras(lider);
                int danoRayo = Math.Max(1, (int)(lider.damage *
                    WeaponScaling.MinionContactDamageMult(nivelOjo) * multMejoras));
                int dano = Math.Max(1, (int)(danoRayo * 6.5f *
                    (1f + DespertarGrimorio.BonusDano(despertar))));

                Projectile agujero = Projectile.NewProjectileDirect(
                    p.GetSource_Misc("SupremoCombinado"),
                    blanco.Center - new Vector2(0f, 90f), Vector2.Zero,
                    tipo, dano, 0f, p.whoAmI);
                agujero.timeLeft = 600;

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
                    SoundID.Item122.WithPitchOffset(-0.5f), blanco.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, blanco.Center);
            }
            catch { }
        }
    }
}
