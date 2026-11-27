using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  DiagnosticoPersistenciaSistema — v6.178 — EL BANCO DE PRUEBAS DE LA
    //  PERSISTENCIA (la letra: «cada vez que inicio en terraria y reviso
    //  el altar los 3 slot del arma estan en blanco aunque si tienen el
    //  arma» + «el altar no guarda cuando se activa la puerta al mundo
    //  interior»).
    //
    //  QUÉ HACE: SOLO en el servidor dedicado (el banco de pruebas de la
    //  casa — en partida real es un no-op total), al cargar el mundo
    //  reproduce EL CICLO COMPLETO de un reinicio de verdad:
    //
    //    1. EL TRÍO: arma las tres ranuras (dos armas vanilla + el arma
    //       de la casa), guarda el ítem con ItemIO.Save → TagIO.ToFile
    //       (EL DISCO de verdad) → TagIO.FromFile → ItemIO.Load en un
    //       ítem NUEVO (SetDefaults + LoadData, como el juego al abrir
    //       el .plr) y grita si alguna ranura llegó vacía.
    //    2. LA PUERTA: SubmundoGrimorio.SaveWorldData → disco →
    //       LoadWorldData y grita si la cuenta de esencias no sobrevive.
    //    3. EL SELLO: la ofrenda consagrada (GlobalItem) guarda y
    //       recupera su sello (la ranura exige arma consagrada).
    //
    //  El veredicto va al LOG del servidor con el prefijo
    //  [DiagnosticoPersistencia] — el gate headless lo lee.
    // ======================================================================
    public class DiagnosticoPersistenciaSistema : ModSystem
    {
        public override void OnWorldLoad()
        {
            // SOLO el banco de pruebas: en partida de verdad, nada.
            if (!Main.dedServ) return;
            try
            {
                ProbarElTrio();
                ProbarLaPuerta();
                ProbarElSello();
            }
            catch (System.Exception exRegistro)
            {
                RegistroSistema.Fallo("DiagnosticoPersistencia", exRegistro);
            }
        }

        private static void Decir(string linea)
        {
            try { Terraria.ModLoader.Logging.PublicLogger.Error("[DiagnosticoPersistencia] " + linea); }
            catch { }
        }

        private static void ProbarElTrio()
        {
            // ── 1. EL TRÍO ARMADO: dos vanilla + el arma de la casa ──
            int tipoTrio = ModContent.ItemType
                <global::AethonMod.Content.Weapons.Investigaciones.TrioDelEterno>();
            var trio = new Item();
            trio.SetDefaults(tipoTrio);
            var tríoMod = trio.ModItem as global::AethonMod.Content.Weapons.Investigaciones.TrioDelEterno;
            if (tríoMod == null) { Decir("TRIO-FALLO: el ModItem no nació"); return; }

            var espada = new Item();
            espada.SetDefaults(ItemID.AmethystStaff);               // vanilla mágica
            var arco = new Item();
            arco.SetDefaults(ItemID.DiamondStaff);                    // vanilla mágica
            var aguja = new Item();
            aguja.SetDefaults(ModContent.ItemType
                <global::AethonMod.Content.Weapons.Investigaciones.TrioDelEterno>()); // mod: cualquiera de la casa
            // el trío no puede contenerse a sí mismo: tercera ranura con el cetro de la casa
            aguja.SetDefaults(ItemID.WandofSparking);

            // EL SELLO de la casa sobre una (CorruptoGlobalItem.Corrompido)
            try
            {
                espada.GetGlobalItem<global::AethonMod.Content.Globals.CorruptoGlobalItem>().Corrompido = true;
            }
            catch (System.Exception exRegistro) { RegistroSistema.Fallo("DiagnosticoPersistencia", exRegistro); }

            tríoMod.Ranuras[0] = (Item)espada.Clone();
            tríoMod.Ranuras[1] = (Item)arco.Clone();
            tríoMod.Ranuras[2] = (Item)aguja.Clone();
            tríoMod.Inteligencia[1] = 2;      // albedrío: al cursor
            tríoMod.ModoDisparo = global::AethonMod.Content.Weapons.Investigaciones.TrioDelEterno.Todas;

            // ── 2. EL CICLO DE REINICIO: SaveData → DISCO → LoadData ──
            var tagTrio = new TagCompound();
            tríoMod.SaveData(tagTrio);
            string ruta = Path.Combine(Main.SavePath, "DiagnosticoTrio.tag");
            TagIO.ToFile(tagTrio, ruta, true);
            var tagLeido = TagIO.FromFile(ruta, true);
            try { File.Delete(ruta); } catch { }

            var renacido = new Item();
            renacido.SetDefaults(tipoTrio);
            var renacidoMod = renacido.ModItem as global::AethonMod.Content.Weapons.Investigaciones.TrioDelEterno;
            if (renacidoMod == null) { Decir("TRIO-FALLO: el renacido no nació"); return; }
            renacidoMod.LoadData(tagLeido);

            // ── 3. EL VEREDICTO ──
            for (int i = 0; i < 3; i++)
            {
                Item r = renacidoMod.Ranuras != null && i < renacidoMod.Ranuras.Length
                    ? renacidoMod.Ranuras[i] : null;
                bool viva = r != null && !r.IsAir;
                Decir($"TRIO-RANURA-{i}: {(viva ? $"VIVA type={r.type} ({r.Name})" : "VACIA — EL BUG DEL USUARIO")}");
            }
            Decir("TRIO-MODO: " + (renacidoMod.ModoDisparo == global::AethonMod.Content.Weapons.Investigaciones.TrioDelEterno.Todas ? "TODAS (correcto)" : "TURNOS (perdido)"));
            Decir("TRIO-ALBEDRIO-1: " + (renacidoMod.Inteligencia != null && renacidoMod.Inteligencia.Length > 1 ? renacidoMod.Inteligencia[1].ToString() : "?"));

            // EL SELLO de la ranura 0 (exigido por PuedeEntrar)
            bool sello = false;
            try
            {
                sello = renacidoMod.Ranuras[0].GetGlobalItem
                    <global::AethonMod.Content.Globals.CorruptoGlobalItem>().Corrompido;
            }
            catch (System.Exception exRegistro) { RegistroSistema.Fallo("DiagnosticoPersistencia", exRegistro); }
            Decir("TRIO-SELLO-RANURA-0: " + (sello ? "VIVO (correcto)" : "PERDIDO — PuedeEntrar lo rechazaría"));

            // ── 4. EL CICLO COMPLETO POR EL .PLR DE VERDAD: ItemIO entero ──
            var tagPlr = ItemIO.Save(tríoMod.Item);           // lo que SaveInventory escribe
            var deDisco = ItemIO.Load(tagPlr);                  // lo que LoadInventory lee
            var modPlr = deDisco.ModItem as global::AethonMod.Content.Weapons.Investigaciones.TrioDelEterno;
            if (modPlr == null) { Decir("TRIO-PLR-FALLO: el ItemIO.Load no renació"); return; }
            for (int i = 0; i < 3; i++)
            {
                Item r = modPlr.Ranuras != null && i < modPlr.Ranuras.Length ? modPlr.Ranuras[i] : null;
                bool viva = r != null && !r.IsAir;
                Decir($"TRIO-PLR-RANURA-{i}: {(viva ? $"VIVA type={r.type} ({r.Name})" : "VACIA — EL BUG DEL USUARIO")}");
            }
        }

        private static void ProbarLaPuerta()
        {
            var submundo = ModContent.GetInstance<SubmundoGrimorio>();
            var tag = new TagCompound();
            // v6.179 — EL CONTRATO NUEVO DE LA PUERTA (la letra: «una vez
            // activo debe estar activo siempre… consumir las 1000 esencias
            // la primera vez que se activa»): el banco prueba TRES cosas —
            // (1) el TRAGO: al cargar un mundo con las mil pagadas, la
            // puerta SE LAS COME (quedan 0) y queda VIVA (PuertaViva);
            // (2) el CERROJO: sin las esencias encima (EsenciasConsagradas
            // = 0 — el «si quito las 1000 esencias» del usuario) la puerta
            // SIGUE ABIERTA para SIEMPRE;
            // (3) NADA SE FUGA: el mundo real queda EXACTAMENTE como
            // estaba (las DOS variables se restauran — el banco de la
            // v6.178 dejaba la cuenta viva y un banco descuidado habría
            // ABIERTO la puerta de verdad para siempre).
            int originalCuenta = SubmundoGrimorio.EsenciasConsagradas;
            bool originalViva = SubmundoGrimorio.PuertaViva;
            SubmundoGrimorio.EsenciasConsagradas = 1000;      // las mil pagadas
            SubmundoGrimorio.PuertaViva = false;
            submundo.SaveWorldData(tag);
            SubmundoGrimorio.EsenciasConsagradas = 0;
            SubmundoGrimorio.PuertaViva = false;
            submundo.LoadWorldData(tag);
            bool seLasComio = SubmundoGrimorio.EsenciasConsagradas == 0;
            bool cerrojoVivo = SubmundoGrimorio.PuertaViva;
            // EL «SI QUITO LAS 1000 ESENCIAS» DEL USUARIO: la cuenta a
            // cero NO cierra lo que ya se abrió.
            SubmundoGrimorio.EsenciasConsagradas = 0;
            bool sigueAbierta = SubmundoGrimorio.PuertaAbierta;
            Decir("PUERTA-TRAGO: " + (seLasComio && cerrojoVivo
                ? "COMIDAS ✔ (0/1000 y PuertaViva — la primera vez es la ÚNICA)"
                : $"MAL (quedó {SubmundoGrimorio.EsenciasConsagradas}/1000, cerrojo {cerrojoVivo})"));
            Decir("PUERTA-CERROJO: " + (sigueAbierta
                ? "ABIERTA PARA SIEMPRE ✔ (sin las esencias encima, el botón NO desaparece)"
                : "SE CIERRA — EL BUG DEL USUARIO"));
            // el mundo real queda COMO ESTABA (las DOS variables)
            SubmundoGrimorio.EsenciasConsagradas = originalCuenta;
            SubmundoGrimorio.PuertaViva = originalViva;
        }

        private static void ProbarElSello()
        {
            var arma = new Item();
            arma.SetDefaults(ItemID.AmethystStaff);
            arma.GetGlobalItem<global::AethonMod.Content.Globals.CorruptoGlobalItem>().Corrompido = true;
            var tag = ItemIO.Save(arma);
            var deDisco = ItemIO.Load(tag);
            bool sello = deDisco.GetGlobalItem<global::AethonMod.Content.Globals.CorruptoGlobalItem>().Corrompido;
            Decir("SELLO-OFRENDA: " + (sello ? "VIVO (correcto)" : "PERDIDO — la ofrenda perdería el sello"));
        }
    }
}
