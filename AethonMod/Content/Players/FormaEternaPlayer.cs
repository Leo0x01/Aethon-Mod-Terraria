using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// FormaEternaPlayer — v6.150 — LA FORMA ETERNA ES DEL GRIMORIO.
    ///
    /// LA LETRA NUEVA (v6.150): «la forma eterna no se viste si el
    /// jugador ya la completo en un grimorio, esto no debe estar ligado
    /// a un jugador para saber si se completo, debe estar ligado a un
    /// grimorio para completarlo» + «la forma eterna no viste los
    /// anillos de soles runicos mientras se construye hasta nivel 112,
    /// por lo tanto que se construya hasta nivel 120 y que se muestren
    /// los anillos de soles runicos mientras lo hace».
    ///
    /// EL DISEÑO (v6.150 — el estado CAMBIÓ DE DUEÑO):
    ///  · LAS PARTES Y LA CONVERSIÓN VIVEN EN EL GRIMORIO
    ///    (AlmaEternoGlobalItem.FormasPartes / FormaEscupida): cada
    ///    libro construye SU forma y la escupe AL LLEGAR al 120 de SU
    ///    despertar. Un jugador que ya completó un grimorio puede
    ///    empezar OTRO y lo verá construirse desde cero.
    ///  · LA CONSTRUCCIÓN LLEGA HASTA EL 120: las partes 1-11 nacen en
    ///    los peldaños 101-111 (una por nivel, como siempre) y de 113 a
    ///    119 LOS ANILLOS DE SOLES RÚNICOS se encienden UNO A UNO
    ///    alrededor del portador (el vestido del sol creciendo — la
    ///    corona que la letra pedía ver). El 120: la 12ª parte (LAS
    ///    ALAS + EL AURA) + LA CONVERSIÓN (el ítem nace de la
    ///    explosión de luz MÁS GRANDE).
    ///  · MIGRACIÓN (una sola vez): los saves v6.142-149 guardaban
    ///    fePartes/feConvertida en el JUGADOR — al cargar, copian al
    ///    grimorio ACTIVO y el jugador queda limpio.
    /// </summary>
    public class FormaEternaPlayer : ModPlayer
    {
        /// <summary>¿Cuántas partes tiene LA FORMA ETERNA COMPLETA?</summary>
        public const int PartesTotales = 12;

        // ============ EL ESTADO (v6.150 — YA NO VIVE AQUÍ) ============
        // PartesConstruidas/Convertida/ManifestacionVivida viajan con el
        // GRIMORIO (AlmaEternoGlobalItem). Este ModPlayer solo conserva
        // los campos por la MIGRACIÓN (leer los saves viejos una vez).

        /// <summary>MIGRACIÓN — las partes del save viejo (v6.142-149,
        /// vivían en el jugador). Se copian al grimorio y se limpian.</summary>
        public int LegadoPartes;

        /// <summary>MIGRACIÓN — la conversión del save viejo.</summary>
        public bool LegadoConvertida;

        /// <summary>MIGRACIÓN — la manifestación del save viejo.</summary>
        public bool LegadoManifestacion;

        // ============ LAS 12 PARTES (el orden de adentro hacia afuera) ============
        //   1  LA ESCRITURA — el tatuaje rúnico (la triple corona de conjuro)
        //   2  EL PRIMER ANILLO — el anillo rúnico interior
        //   3  EL SEGUNDO ANILLO — el anillo del agujero negro
        //   4  EL TERCER ANILLO — el anillo exterior dorado
        //   5  LA PRIMERA LUZ — el aura de la Forma Ascendida
        //   6  LA BRASA — el humo negro y oro del Eclipse
        //   7  EL PRIMER SOL — el primer sol rúnico
        //   8  EL SEGUNDO SOL — la pareja solar
        //   9  EL TERCER SOL — el coro completo
        //  10  LAS OFANIM — las ruedas de Ezequiel
        //  11  EL MANTO — el velo que une el panteón
        //  12  EL AURA DEL VACÍO + LAS ALAS — la obra maestra (nace al 120)
        public static string ClaveParte(int i) => i switch
        {
            0 => "Mods.AethonMod.FormaEterna.Parte1",
            1 => "Mods.AethonMod.FormaEterna.Parte2",
            2 => "Mods.AethonMod.FormaEterna.Parte3",
            3 => "Mods.AethonMod.FormaEterna.Parte4",
            4 => "Mods.AethonMod.FormaEterna.Parte5",
            5 => "Mods.AethonMod.FormaEterna.Parte6",
            6 => "Mods.AethonMod.FormaEterna.Parte7",
            7 => "Mods.AethonMod.FormaEterna.Parte8",
            8 => "Mods.AethonMod.FormaEterna.Parte9",
            9 => "Mods.AethonMod.FormaEterna.Parte10",
            10 => "Mods.AethonMod.FormaEterna.Parte11",
            11 => "Mods.AethonMod.FormaEterna.Parte12",
            _ => "Mods.AethonMod.FormaEterna.Parte1",
        };

        // ============ EL ACCESO AL ESTADO DEL LIBRO ============

        /// <summary>v6.150 — El grimorio ACTIVO del portador (el de la
        /// barra; null si no lleva ninguno).</summary>
        public static Item GrimorioActivo(Player p)
        {
            // v6.153 — EL LIBRO VIVE DONDE LO LLEVES (la cura de «nunca
            // aparecieron los anillos»: la v6.150 exigía el Eterno EN LA
            // BARRA DE ACCESO RÁPIDO — con el libro en el inventario
            // normal (slots 10-49, p. ej. tras reorganizar) el vestido
            // entero moría sin decir nada). La casa YA tiene el lector
            // correcto: Activo(p) (mano primero, luego los 58 slots).
            // El despertar sube IGUAL desde la bolsa; la construcción
            // también tenía que verse desde ahí.
            if (p == null || !p.active) return null;
            return global::AethonMod.Content.Globals.AlmaEternoGlobalItem.Activo(p);
        }

        /// <summary>v6.150 — Las partes construidas por EL GRIMORIO que
        /// el portador lleva puesto (0 si no lleva ninguno).</summary>
        public static int PartesDe(Item grimorio)
        {
            var alma = global::AethonMod.Content.Globals.AlmaEternoGlobalItem.De(grimorio);
            return alma == null ? 0 : alma.FormasPartes;
        }

        /// <summary>v6.150 — ¿EL grimorio ya escupió su Forma Eterna?</summary>
        public static bool EscupidaDe(Item grimorio)
        {
            var alma = global::AethonMod.Content.Globals.AlmaEternoGlobalItem.De(grimorio);
            return alma != null && alma.FormaEscupida;
        }

        // ============ EL CONSTRUCTOR (lo llama SubirNivel 101-112) ============

        /// <summary>v6.150 — Construye la parte N (1-12) EN EL GRIMORIO
        /// ACTIVO del portador (el pago ya lo cobró la MESA). Al llegar
        /// a la 12 (el 120 del despertar): LA CONVERSIÓN — el ítem nace
        /// + LA MANIFESTACIÓN. El estado pertenece AL LIBRO: cada
        /// grimorio construye el suyo.</summary>
        public void ConstruirParte(int numero)
        {
            try
            {
                if (numero < 1 || numero > PartesTotales) return;
                Item libro = GrimorioActivo(Player);
                var alma = global::AethonMod.Content.Globals.AlmaEternoGlobalItem.De(libro);
                if (alma == null) return;                       // sin libro: nada que construir
                if (alma.FormasPartes >= numero) return;        // este libro ya la tiene
                alma.FormasPartes = numero;

                // EL AVISO (la voz de la casa)
                Main.NewText(Language.GetTextValue("Mods.AethonMod.FormaEterna.Construida",
                        alma.FormasPartes, PartesTotales,
                        Language.GetTextValue(ClaveParte(numero - 1))),
                    new Color(196, 138, 255));
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);

                // ¿LA CONVERSIÓN? (la 12ª parte — la cima de la escalera)
                if (alma.FormasPartes >= PartesTotales && !alma.FormaEscupida)
                    ConvertirLaForma(libro, alma);
            }
            catch { }
        }

        /// <summary>v6.150 — LA CONVERSIÓN, EN EL LIBRO (la letra: «en el
        /// momento de la manifestacion la forma eterna que es parte del
        /// grimorio desaparece ya que es convertida en el item
        /// cosmetico»): la forma que vestía al portador SE CONVIERTE —
        /// el ítem cosmético LA FORMA ETERNA nace, LA MANIFESTACIÓN
        /// estalla (una sola vez POR LIBRO).</summary>
        private void ConvertirLaForma(Item libro,
            global::AethonMod.Content.Globals.AlmaEternoGlobalItem alma)
        {
            try
            {
                if (Player == null || !Player.active) return;
                alma.FormaEscupida = true;

                // EL ACCESORIO NACE (la letra v6.143: «sea el accesorio,
                // ya que debe tener el efecto de aura de vacío» + v6.145:
                // las alas rúnicas viven DENTRO).
                Player.QuickSpawnItem(Player.GetSource_GiftOrReward(),
                    ModContent.ItemType<Items.Cosmetics.FormaEternaItem>(), 1);

                // LA MANIFESTACIÓN (una por LIBRO): la explosión de luz.
                if (!alma.ManifestacionVivida)
                {
                    alma.ManifestacionVivida = true;
                    Content.Systems.DespertarGrimorioSistema.ManifestacionDeLaForma(Player);
                }
            }
            catch { }
        }

        // ============ EL VESTIDO (cada frame mientras USA el libro) ============

        public override void PostUpdateEquips()
        {
            try
            {
                // v6.150 — LA MIGRACIÓN PRIMERO (una sola vez): los saves
                // v6.142-149 traían el estado en el JUGADOR — se copia al
                // grimorio activo en cuanto exista y se limpia.
                if (LegadoPartes > 0 || LegadoConvertida || LegadoManifestacion)
                    MigrarAlGrimorioSiHaceFalta();

                // v6.150 — LA LETRA NUEVA MANDA: «esto no debe estar
                // ligado a un jugador para saber si se completo, debe
                // estar ligado a un grimorio para completarlo». El
                // vestido lo decide EL GRIMORIO ACTIVO (sus partes, su
                // escupida) — el jugador ya NO guarda nada.
                Item libroActivo = GrimorioActivo(Player);
                if (libroActivo == null) return;
                var alma = global::AethonMod.Content.Globals.AlmaEternoGlobalItem.De(libroActivo);
                if (alma == null || alma.FormasPartes <= 0) return;

                // ============================================================
                //  v6.151 — EL VESTIDO NO SE APAGA POR UNA ESCUPIDA AJENA
                //  (la letra: «La Forma Eterna solo se construye en
                //  personaje que no la hayan desbloqueado antes, una vez
                //  desbloqueada ya no se puede volver a construir y por eso
                //  en mi personaje de prueba sin importar cuantos libros
                //  lleve a despertar 101 a 120 no puedo ver como se
                //  construye»): la condición v6.150 leía ESTADO GLOBAL —
                //  si CUALQUIER libro del personaje ya había escupido su
                //  forma, TODO libro nuevo nacía «ya vestido y recogido»:
                //  jamás se veía la construcción. LA CURA: el vestido lo
                //  decide EL DESPERTAR DE ESTE LIBRO — mientras ESTE libro
                //  no llegue a SU 120 (ni escupa SU forma), el dios VISTE:
                //  cada grimorio construye SU forma (la letra v6.150, por
                //  fin de verdad).
                // ============================================================
                int despertar = Systems.DespertarGrimorio.NivelDe(libroActivo);

                // ============================================================
                //  v6.153 — LOS ANILLOS DE SOLES RÚNICOS, PRIMERO QUE
                //  NADA (la letra: «nunca aparecioron los anillos de
                //  soles runicos» + la pregunta respondida en el código:
                //  SÍ SON 12 PARTES — las 11 de los peldaños 101-111 y
                //  LA 12ª del 120 (alas + aura del vacío + LA
                //  CONVERSIÓN al ítem); los peldaños 113-119 son EL
                //  VESTIDO DEL SOL: un anillo de la corona por nivel).
                //  Este bloque vive ANTES del early-return de la
                //  escupida: un libro EN CONSTRUCCIÓN (que todavía no
                //  llega a su 120) jamás pierde sus anillos — y un
                //  libro VIEJO que ya escupió no los revive (su forma
                //  YA vive en el ítem).
                // ============================================================
                var visuales = Player.GetModPlayer<CosmeticPlayer>();
                if (visuales == null) return;
                bool escupida = alma.FormaEscupida && alma.FormasPartes >= PartesTotales;
                // v6.154 — LOS ANILLOS NUNCA SE APAGAN ANTES DE LA
                // CONVERSIÓN (la letra: «los anillos de soles runicos
                // nunca aparecen en la construccion»): la v6.153 los
                // encendía SOLO en la ventana 113-119 — en el 120 SIN
                // PAGAR AÚN (la puerta del jefe espera) la condición
                // `< NivelManifestacion` los apagaba TODOS: el portador
                // pasaba de 7 anillos a CERO en el peldaño de la cima,
                // justo antes de la conversión. LA CURA: los anillos
                // viven desde el 113 hasta LA ESCUPIDA (el 120 incluido
                // mientras se paga) — el vestido del sol se queda puesto
                // hasta que la Forma lo reclame.
                if (!escupida &&
                    despertar > Systems.DespertarGrimorio.NivelManifestacion - 8)
                {
                    visuales.AnillosSolRunico = Math.Min(7, despertar - 112); // 113→1 … 119-120→7
                    if (visuales.AnillosSolRunico > 0 &&
                        Player.whoAmI == Main.myPlayer && !alma.AnillosAvisoVivido)
                    {
                        alma.AnillosAvisoVivido = true;
                        Main.NewText(Language.GetTextValue(
                                "Mods.AethonMod.FormaEterna.Anillos"),
                            new Color(255, 215, 120));
                    }
                }

                if (despertar >= Systems.DespertarGrimorio.NivelManifestacion ||
                    (alma.FormaEscupida && alma.FormasPartes >= PartesTotales))
                    return;

                // EL VESTIDO SE CONSTRUYE PARTE POR PARTE — el contador
                // viaja al anillo dorsal (crece con cada parte).
                visuales.PartesFormaEterna = alma.FormasPartes;

                // LAS PARTES SE PONEN (de adentro hacia afuera):
                if (alma.FormasPartes >= 1) visuales.AnilloDorsal = true;         // 1: la escritura
                if (alma.FormasPartes >= 5) visuales.FormaAscendidaVisual = true;  // 5: la primera luz
                if (alma.FormasPartes >= 6) visuales.BrasaDelEclipse = true;       // 6: la brasa
                if (alma.FormasPartes >= 7) visuales.BrilloSolar = true;           // 7-9: los soles
                if (alma.FormasPartes >= 10) visuales.FormaAscendidaOchoVisual = true; // 10: las ofanim
                // LA PARTE 11, EL MANTO: el velo que une el panteón.
                if (alma.FormasPartes >= 11) visuales.FormaAscendidaVisual = true;

                // (v6.153 — los anillos viven ARRIBA, antes del return)

                // LA PARTE 12 — EL AURA DEL VACÍO: la obra maestra es EL
                // EFECTO ACTIVO (el campo arde mientras la forma viste al
                // portador — hasta que el libro llega al 120 y LA ESCUPE).
                if (alma.FormasPartes >= 12)
                {
                    if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
                    {
                        int tipoAncla = ModContent.ProjectileType
                            <global::AethonMod.Content.Projectiles.AuraJuicio2Ancla>();
                        bool viva = false;
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            Projectile pr = Main.projectile[i];
                            if (pr != null && pr.active && pr.type == tipoAncla &&
                                pr.owner == Player.whoAmI) { viva = true; break; }
                        }
                        if (!viva)
                            Projectile.NewProjectile(Player.GetSource_Misc("FormaEternaVacio"),
                                Player.Center, Vector2.Zero, tipoAncla,
                                0, 0f, Player.whoAmI);
                    }
                }
            }
            catch { }
        }

        // ============ LA MIGRACIÓN (una sola vez) ============

        /// <summary>v6.150 — Copia el estado VIEJO del jugador (saves
        /// v6.142-149) al grimorio ACTIVO y se limpia: la forma eterna
        /// pasa a ser del LIBRO. La llama PostUpdateEquips (cuando el
        /// grimorio ya existe — sin libro no hay a quién dárselo).</summary>
        private void MigrarAlGrimorioSiHaceFalta()
        {
            try
            {
                if (LegadoPartes <= 0 && !LegadoConvertida && !LegadoManifestacion)
                    return;                                    // save nuevo: nada que migrar
                Item libro = GrimorioActivo(Player);
                var alma = global::AethonMod.Content.Globals.AlmaEternoGlobalItem.De(libro);
                if (alma == null) return;                      // sin grimorio: se reintenta el próximo tick

                if (LegadoPartes > alma.FormasPartes) alma.FormasPartes = LegadoPartes;
                if (LegadoConvertida) alma.FormaEscupida = true;
                if (LegadoManifestacion) alma.ManifestacionVivida = true;
                LegadoPartes = 0;
                LegadoConvertida = false;
                LegadoManifestacion = false;
            }
            catch { }
        }

        // ============ PERSISTENCIA (solo la migración pendiente) ============
        public override void SaveData(Terraria.ModLoader.IO.TagCompound tag)
        {
            // v6.150 — el estado YA VIVE EN EL LIBRO. Aquí solo queda la
            // migración PENDIENTE (jugador sin grimorio en el momento de
            // migrar): se conserva hasta que tenga el libro puesto.
            if (LegadoPartes > 0) tag["fePartes"] = LegadoPartes;
            if (LegadoConvertida) tag["feConvertida"] = true;
            if (LegadoManifestacion) tag["feManifestacion"] = true;
        }

        public override void LoadData(Terraria.ModLoader.IO.TagCompound tag)
        {
            // v6.150 — LECTURA COMPATIBLE: el save viejo traía el estado
            // del jugador; se recibe como MIGRACIÓN pendiente (se copia al
            // grimorio en cuanto el jugador entre a un mundo con él).
            LegadoPartes = Math.Max(0, Math.Min(PartesTotales, tag.GetInt("fePartes")));
            LegadoConvertida = tag.GetBool("feConvertida");
            LegadoManifestacion = tag.GetBool("feManifestacion");
        }
    }
}
