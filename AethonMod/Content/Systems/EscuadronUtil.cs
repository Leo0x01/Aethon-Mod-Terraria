using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  ESCUADRONUTIL — v6.51.12 — LA CURA DE LA DESINVOCACIÓN SILENCIOSA.
    //
    //  LA LETRA DEL USUARIO: «a veces los minions se desinvocan, o sea
    //  que estan invocado y de pronto dejan de estarlo».
    //
    //  LA AUTOPSIA (la causa raíz): TODOS los minions de la casa sostenían
    //  su vida en el ICONO DEL BUFF — «si el buff no está, Kill()». Y el
    //  buff MUERE SOLO en dos situaciones de vanilla que el jugador no
    //  ve venir: (1) LA BARRA LLENA — cuando un buff NUEVO llega y no hay
    //  slot, vanilla REEMPLAZA el de MENOR tiempo restante (nuestro buff
    //  se refresca a 18000 — con 21 buffs más largos encima, ÉL era el
    //  más corto: la siguiente poción se lo comía), y (2) LA MUERTE DEL
    //  BUFF AL INVOCAR con la barra ya llena (el buff jamás aterrizaba:
    //  los ojos nacían y morían AL TICK SIGUIENTE). «De pronto dejaban
    //  de estarlo» — exactamente eso.
    //
    //  LA LEY NUEVA (la de la .51.3, por fin de verdad): los minions
    //  JAMÁS mueren por falta de buff — mueren por DAÑO, por la muerte
    //  del dueño, o porque el jugador LOS DESPIDE con el clic derecho
    //  en su icono (ModBuff.RightClick — «solo se deben desactivar si
    //  el jugador los quita usando su buff de minion»). El icono se
    //  RE-ANCLA solo si hay un slot LIBRE (jamás expulsa el buff de
    //  nadie — la ciudadanía vanilla de la casa).
    // ======================================================================
    public static class EscuadronUtil
    {
        /// <summary>EL SOSTÉN DEL MINION: el reloj nunca expira, el icono
        /// se re-ancla si falta (y hay sitio), y NADIE muere aquí. La
        /// única despedida legítima es Despedir (el clic derecho del
        /// buff) o la muerte de verdad.</summary>
        public static void Sostener(Projectile p, Player dueño, int tipoBuff)
        {
            if (p == null || !p.active) return;

            // EL RELOJ DE LA MASCOTA (el patrón vanilla de toda la casa):
            // el timeLeft se refresca A CADA TICK — la muerte la deciden
            // el daño, el dueño o la despedida, jamás el reloj.
            p.timeLeft = 2;

            if (dueño == null || !dueño.active) return;

            // ¿EL ICONO VIVE? (con tiempo de verdad, no un fantasma)
            bool tiene = false;
            for (int i = 0; i < Player.MaxBuffs; i++)
                if (dueño.buffType[i] == tipoBuff && dueño.buffTime[i] > 0)
                { tiene = true; break; }

            if (tiene)
            {
                // EL REFRESCO SUAVE: el buff ya está — solo se le asegura
                // el reloj (el ModBuff.Update de cada casa también lo
                // hace; esto cubre el tick de carrera).
                for (int i = 0; i < Player.MaxBuffs; i++)
                    if (dueño.buffType[i] == tipoBuff && dueño.buffTime[i] < 18000)
                        dueño.buffTime[i] = 18000;
            }
            else
            {
                // v6.51.12 — RE-ANCLAJE CIUDADANO: el icono vuelve SOLO si
                // hay un slot LIBRE (nunca reemplaza el buff de nadie —
                // el bug de la barra llena muere aquí, en las dos
                // direcciones). Sin sitio: el minion VIVE igual — la
                // despedida espera a que se haga hueco.
                for (int i = 0; i < Player.MaxBuffs; i++)
                {
                    if (dueño.buffType[i] == 0)
                    {
                        dueño.AddBuff(tipoBuff, 18000);
                        break;
                    }
                }
            }
        }

        /// <summary>LA DESPEDIDA LEGÍTIMA: la llama el RightClick del buff
        /// (el jugador quitó el icono — la letra: «solo se deben
        /// desactivar si el jugador los quita usando su buff de minion»).
        /// Mata al escuadrón ENTERO de ese tipo del jugador local.</summary>
        public static void Despedir(int tipoProyectil)
        {
            Player yo = Main.LocalPlayer;
            if (yo == null) return;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != tipoProyectil) continue;
                if (p.owner != yo.whoAmI) continue;

                // LA PARTÍCULA DE LA DESPEDIDA (se disuelven en tinta)
                if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                {
                    int n = 10;
                    for (int k = 0; k < n; k++)
                    {
                        Dust d = Dust.NewDustPerfect(p.Center, DustID.Shadowflame,
                            Microsoft.Xna.Framework.Vector2.Normalize(
                                new Microsoft.Xna.Framework.Vector2(
                                    Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 0.2f))) *
                                Main.rand.NextFloat(1.5f, 3.5f), 140,
                            new Microsoft.Xna.Framework.Color(120, 40, 180), 0.9f);
                        d.noGravity = true;
                    }
                }
                p.Kill();
            }
        }
    }
}
