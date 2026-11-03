using System.Collections.Generic;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Players
{
    // ======================================================================
    //  v6.67 — EL REGISTRO DEL CAZADOR (la letra del usuario: «en caso de
    //  no tener el nivel necesario que lo pida y mates el jefe, este debe
    //  contar»): las puertas de la caza del DESPERTAR exigen que su jefe
    //  haya caído — y el crédito es RETROACTIVO. Este registro guarda,
    //  POR PERSONAJE, las puertas cuyos jefes tiró ESTE cazador con el
    //  mod vivo (viaja con el .plr a cualquier mundo).
    //
    //  LA SEGUNDA FUENTE (la principal): las BANDERAS DEL MUNDO de
    //  vanilla (DespertarGrimorio.BanderaDelMundo — NPC.downed*), que
    //  Terraria escribe SIEMPRE que un jefe muere en el mundo, por
    //  quien sea, incluso ANTES de que el mod existiera. Basta
    //  CUALQUIERA de las dos: mátalo hoy a nivel 1, o lo mataste hace
    //  un año en este mundo — cuando llegues al nivel que lo pide, YA
    //  ESTÁ MARCADO.
    //
    //  LA CLAVE del registro es el NÚMERO DE NIVEL de la puerta (cada
    //  peldaño de la caza pide UN jefe — la llave canónica de la casa).
    // ======================================================================
    public class CazaJefesPlayer : ModPlayer
    {
        /// <summary>LAS PUERTAS PAGAS: los niveles de la caza cuyos jefes
        /// ya tiró ESTE cazador (persiste en el .plr).</summary>
        public readonly HashSet<int> PuertasPagas = new HashSet<int>();

        /// <summary>¿YA CAÍDO el jefe de esta puerta (por ESTE cazador)?</summary>
        public bool Caido(int nivelPuerta) => PuertasPagas.Contains(nivelPuerta);

        /// <summary>LA PUERTA SE PAGA (la llama CazaJefesGlobalNPC al morir
        /// el jefe — en todas las pantallas vivas: una cacería es del que
        /// la ve... y de su escuadrón).</summary>
        public void Pagar(int nivelPuerta)
        {
            if (nivelPuerta > 0) PuertasPagas.Add(nivelPuerta);
        }

        // ================================================================
        //  v6.86 — LOS CONTADORES DE LA IRA (la letra de la cima: «la
        //  Ira o sea el Haz, sera obtenible en el nivel 100 y venciendo
        //  a moon lord al menos 5 veces, y venciendo al fragmento de un
        //  recuerdo lejano Aethon 1 vez»): el despertar 100 YA NO
        //  BASTA — estos dos relojes viajan con el cazador y los paga
        //  CazaJefesGlobalNPC.OnKill (todas las pantallas vivas).
        // ================================================================

        /// <summary>CUÁNTAS VECES tiró ESTE cazador al Señor de la Luna
        /// (la Ira exige 5 — el legacy: el que ya lo tenía muerto al
        /// cargar la v6.86 cobra 1 de las 5, una sola vez).</summary>
        public int VecesMoonLord = 0;

        /// <summary>CUÁNTAS VECES tiró a Aethon (cualquiera de sus tres
        /// formas — el fragmento, el gemelo oscuro o el recompuesto:
        /// TODOS son «el recuerdo lejano»). La puerta 59 Y la Ira (1 vez)
        /// lo leen.</summary>
        public int VecesAethon = 0;

        /// <summary>El LEGADO del Señor de la Luna: cobra la 1.ª de las 5
        /// la PRIMERA vez que este cazador pisa un mundo con la lápida
        /// escrita (una sola vez por cazador — jamás dos veces).</summary>
        public bool LegadoMoonLord = false;

        /// <summary>v6.88 — El LEGADO de la Emperatriz: cobra la 1.ª de
        /// las 5 (una sola vez por cazador).</summary>
        public bool LegadoEmperatriz = false;

        /// <summary>v6.88 — CUÁNTAS VECES tiró ESTE cazador a la Emperatriz
        /// de la Luz (la letra de la cima: «5 emperatris de la luz»).
        /// El legacy: el que ya la tenía muerta al cargar la v6.88 cobra
        /// 1 de las 5, una sola vez.</summary>
        public int VecesEmperatriz = 0;

        // ================================================================
        //  v6.86 — EL LIBRO CORRUPTO (la letra del peldaño 20): «este
        //  solo se consigue... y solo se puede conseguir 1 por jugador,
        //  asi que una vez que un jugador consigue un libro ya no puede
        //  conseguir mas» — el sello viaja con el cazador: CanPickup lo
        //  respeta (nadie levanta su segundo libro) y las fuentes solo
        //  sueltan uno si queda alguien sin el suyo.
        // ================================================================

        /// <summary>¿ESTE cazador ya consiguió (y levantó) SU Libro
        /// Corrupto? (true = el que caiga ya no es suyo: CanPickup lo
        /// bloquea — uno por cazador, para siempre).
        /// v6.91 — LA COMPUTA VIEJA SE RETIRA de los drops (la letra:
        /// «si el jugador no lo ha usado todavia le pueden aparecer
        /// mas, solo cuando el jugador lo usa y el grimorio tiene un
        /// nivel de despertar de 20 o superior es que dejan de salir»)
        /// — queda por compatibilidad de saves, pero los drops ya la
        /// ignoran.</summary>
        public bool YaTuvoLibro = false;

        /// <summary>v6.91 — ¿ESTE jugador ya USÓ (devoró con su
        /// grimorio) un Libro Consagrado? EL CONTADOR INDIVIDUAL de la
        /// letra: «esto es algo individual por jugador, cada jugador
        /// debe tener su propio contador en multijugador» — se marca en
        /// la MESA (al devorar) y viaja en el .plr. Con él + el nivel 20
        /// del grimorio, los libros DEJAN de salir.</summary>
        public bool LibroUsado = false;

        // ================================================================
        //  LA PERSISTENCIA (el .plr del cazador — la lista viaja con el
        //  personaje a todos los mundos).
        // ================================================================
        public override void SaveData(TagCompound tag)
        {
            try
            {
                if (PuertasPagas.Count > 0)
                    tag["cazaPuertas"] = new List<int>(PuertasPagas);
                tag["mlVeces"] = VecesMoonLord;
                tag["eolVeces"] = VecesEmperatriz;
                tag["aethonVeces"] = VecesAethon;
                tag["mlLegado"] = LegadoMoonLord;
                tag["libroUno"] = YaTuvoLibro;
                tag["libroUsado"] = LibroUsado;
            }
            catch { }
        }

        public override void LoadData(TagCompound tag)
        {
            try
            {
                PuertasPagas.Clear();
                var lista = tag.GetList<int>("cazaPuertas");
                foreach (int nivel in lista)
                    if (nivel > 0 && nivel <= Content.Systems.DespertarGrimorio.NivelMax)
                        PuertasPagas.Add(nivel);
                VecesMoonLord = Math.Max(0, Math.Min(999, tag.GetInt("mlVeces")));
                VecesAethon = Math.Max(0, Math.Min(999, tag.GetInt("aethonVeces")));
                VecesEmperatriz = Math.Max(0, Math.Min(999, tag.GetInt("eolVeces")));
                LegadoMoonLord = tag.GetBool("mlLegado");
                LegadoEmperatriz = tag.GetBool("eolLegado");
                YaTuvoLibro = tag.GetBool("libroUno");
                LibroUsado = tag.GetBool("libroUsado");
            }
            catch { PuertasPagas.Clear(); }
        }

        /// <summary>v6.86 — EL LEGADO DEL SEÑOR DE LA LUNA (se cobra
        /// UNA vez, al pisar un mundo con la lápida escrita: el que ya
        /// lo mató ANTES de esta versión no arranca de cero — cobra 1
        /// de las 5, las otras cuatro hay que ganárselas).</summary>
        public override void OnEnterWorld()
        {
            try
            {
                if (LegadoMoonLord || VecesMoonLord > 0) { LegadoMoonLord = true; }
                else if (NPC.downedMoonlord)
                {
                    VecesMoonLord = 1;
                    LegadoMoonLord = true;
                }
                // v6.88 — EL LEGADO DE LA EMPERATRIZ (la cima compuesta
                //     exige 5: el que ya la mató ANTES cobra 1 de las 5).
                if (!LegadoEmperatriz && VecesEmperatriz == 0 && NPC.downedEmpressOfLight)
                {
                    VecesEmperatriz = 1;
                    LegadoEmperatriz = true;
                }
            }
            catch { }
        }
    }
}
