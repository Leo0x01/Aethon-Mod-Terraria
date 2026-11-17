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
    /// FormaEternaPlayer — v6.142 — LA FORMA ETERNA DEL DESPERTAR (la
    /// letra completa: «La forma eterna debia estar en el nivel de
    /// despertar, cuando el despertar llegue a nivel 100, entonces
    /// comienza a desbloquear la forma del eterno, como dices 12
    /// partes, seria hasta nivel 112 pagando los requisitos con 10 de
    /// platino + 1000 esencias del Eterno, subiendo 10% por nivel,
    /// dando cada nivel un +4% de ataque y su pieza visual puesta en
    /// el jugador SOLO mientras usa el libro — no es un ítem, es un
    /// miembro del portador. Luego con las 12: el ítem cosmético La
    /// Forma Eterna nace y LA MANIFESTACIÓN estalla… en el momento de
    /// la manifestacion la forma eterna que es parte del grimorio
    /// desaparece ya que es convertida en el item cosmetico»).
    ///
    /// EL DISEÑO:
    ///  · LA CASA: EL DESPERTAR del Grimorio del Eterno (101-112 — el
    ///    constructor es DespertarGrimorioSistema.SubirNivel).
    ///  · LAS 12 PARTES (una por peldaño): cada una es UNA PARTE
    ///    VISUAL DEL CUERPO que se PONE en el portador MIENTRAS USA EL
    ///    LIBRO (nada de ítems: son MIEMBROS del portador).
    ///  · EL ATAQUE: lo paga BonusDano del despertar (+4%/nivel
    ///    101-112, +10%/nivel 113-120) — A TODO el grimorio (mágico e
    ///    invocación), como toda la escalera.
    ///  · LA 12ª PARTE (nivel 112): LA CONVERSIÓN — el ítem cosmético
    ///    LA FORMA ETERNA nace, LA MANIFESTACIÓN estalla y LOS
    ///    VISUALES DEL PORTADOR DESAPARECEN (la forma se convirtió en
    ///    el ítem: el portador ya no la lleva puesta — se la pone
    ///    quien la vista, como el cosmético de siempre).
    /// </summary>
    public class FormaEternaPlayer : ModPlayer
    {
        /// <summary>¿Cuántas partes tiene LA FORMA ETERNA COMPLETA?</summary>
        public const int PartesTotales = 12;

        // ============ EL ESTADO (persiste con el jugador) ============

        /// <summary>Las partes ya construidas (0..12 — lo construyó EL
        /// DESPERTAR del libro: peldaños 101-112).</summary>
        public int PartesConstruidas;

        /// <summary>LA CONVERSIÓN ya vivió (la 12ª parte se convirtió
        /// en el ítem cosmético — los visuales ya NO visten al
        /// portador).</summary>
        public bool Convertida;

        /// <summary>¿Ya vivió LA MANIFESTACIÓN? (una vez por jugador —
        /// el ítem cosmético puede perderse, la gloria no repite).</summary>
        public bool ManifestacionVivida;

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
        //  12  EL AURA DEL VACÍO — la obra maestra (el campo activo)
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

        // ============ EL CONSTRUCTOR (lo llama SubirNivel 101-112) ============

        /// <summary>Construye la parte N (1-12) del peldaño del despertar
        /// (el pago ya lo cobró la MESA — esencias + monedas). Al llegar
        /// a la 12: LA CONVERSIÓN (el ítem nace + LA MANIFESTACIÓN + los
        /// visuales del portador DESAPARECEN).</summary>
        public void ConstruirParte(int numero)
        {
            try
            {
                if (numero < 1 || numero > PartesTotales) return;
                if (PartesConstruidas >= numero) return;   // ya construida
                PartesConstruidas = numero;

                // EL AVISO (la voz de la casa)
                Main.NewText(Language.GetTextValue("Mods.AethonMod.FormaEterna.Construida",
                        PartesConstruidas, PartesTotales,
                        Language.GetTextValue(ClaveParte(numero - 1))),
                    new Color(196, 138, 255));
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);

                // ¿LA CONVERSIÓN? (la 12ª parte — la cima de la escalera)
                if (PartesConstruidas >= PartesTotales && !Convertida)
                    ConvertirLaForma();
                // v6.147 — LA PROTECCIÓN DEL DOBLE ESCUPE: si el jugador
                // ya convirtió una vez (save viejo con feConvertida) y
                // vuelve a construir 12 partes con OTRO grimorio, el
                // ESCUPE no se repite — el ítem ya es suyo (la escalera
                // viva del despertar manda el vestido, no este flag).
            }
            catch { }
        }

        /// <summary>v6.142 — LA CONVERSIÓN (la letra: «en el momento de
        /// la manifestacion la forma eterna que es parte del grimorio
        /// desaparece ya que es convertida en el item cosmetico»): la
        /// forma que vestía al portador SE CONVIERTE — el ítem cosmético
        /// LA FORMA ETERNA nace, LA MANIFESTACIÓN estalla (una sola vez).
        /// v6.147 — la letra nueva la llama EL ESCUPE: el grimorio al
        /// llegar al 120 del despertar ESCUPE La Forma Eterna (el ítem
        /// con las alas rúnicas y el aura dentro); el vestido del dios
        /// se recoge y desde entonces la forma vive en el ítem.</summary>
        private void ConvertirLaForma()
        {
            try
            {
                if (Player == null || !Player.active) return;
                Convertida = true;

                // v6.143 — EL ACCESORIO NACE (la letra: «sea el
                // accesorio, ya que debe tener el efecto de aura de
                // vacío»): La Forma Eterna ES un accesorio — puesto,
                // enciende EL AURA DEL VACÍO (el ancla de 19 tiles),
                // su QUEMADURA en los impíos (×1→×3 por cercanía, slow
                // ×0,55, el fuego de salida) y SUS BUFFS al portador
                // (Vacío Exterior mientras arden + el buff del aura +
                // los stats del panteón).
                Player.QuickSpawnItem(Player.GetSource_GiftOrReward(),
                    ModContent.ItemType<Items.Cosmetics.FormaEternaItem>(), 1);

                // v6.145 — LAS ALAS VIVEN DENTRO (la letra: «ahora las
                // alas runicas deben estar tambien fusionadas dentro de
                // la forma eterna»): la Forma Eterna ES el accesorio
                // con las alas rúnicas integradas — inmunidad a las
                // caídas y vuelo infinito (los PostUpdateEquips del
                // ítem). NADA de regalo aparte: la fusión ES el hito.

                // LA MANIFESTACIÓN (una sola vez): la explosión de luz
                if (!ManifestacionVivida)
                {
                    ManifestacionVivida = true;
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
                // v6.147 — LA LETRA NUEVA MANDA (el usuario: «el vestido
                // debe verse siempre que un grimorio no llegue a nivel 120
                // de despertar, y una vez llega a nivel 120 de despertar
                // debe escupir La Forma Eterna»): el vestido ya NO muere
                // con la conversión vieja — vive MIENTRAS el grimorio
                // ACTIVO (el de la barra) NO ha llegado al 120. El estado
                // viejo 'Convertida' queda como historial (el ítem ya
                // nació una vez — no se re-escupe), pero el VESTIDO manda
                // la escalera VIVA del libro, no el recuerdo.
                //
                // LA LETRA ORIGINAL (v6.141): «una parte que sera puesta
                // en el jugador MIENTRAS USA EL LIBRO» + la v6.146: el
                // libro en la HOTBAR (los primeros 10 slots), no la bolsa.
                bool libroEnBarra = false;
                for (int i = 0; i < 10; i++)
                {
                    Item inv = Player.inventory[i];
                    if (inv != null && !inv.IsAir &&
                        inv.type == ModContent.ItemType<Weapons.GrimoireEternal>())
                    { libroEnBarra = true; break; }
                }
                if (!libroEnBarra || PartesConstruidas <= 0) return;

                // LA ESCALERA VIVA: el despertar DEL LIBRO ACTIVO decide.
                // < 120 → el dios viste al portador (las partes que haya
                // construido). ≥ 120 → la forma YA FUE ESCUPIDA (nació el
                // ítem con LA MANIFESTACIÓN en ConvertirLaForma) y el
                // portador la lleva puesta como accesorio — el vestido
                // del dios se recoge.
                Item libroActivo = global::AethonMod.Content.Globals.AlmaEternoGlobalItem.Activo(Player);
                if (libroActivo == null) return;
                int despertar = Systems.DespertarGrimorio.NivelDe(libroActivo);
                bool formaEscupida = despertar >= Systems.DespertarGrimorio.NivelManifestacion ||
                                     Convertida;
                if (formaEscupida) return;

                var visuales = Player.GetModPlayer<CosmeticPlayer>();
                if (visuales == null) return;

                // LAS PARTES SE PONEN (de adentro hacia afuera):
                if (PartesConstruidas >= 1) visuales.AnilloDorsal = true;         // 1: la escritura
                if (PartesConstruidas >= 5) visuales.FormaAscendidaVisual = true;  // 5: la primera luz
                if (PartesConstruidas >= 6) visuales.BrasaDelEclipse = true;       // 6: la brasa
                if (PartesConstruidas >= 7) visuales.BrilloSolar = true;           // 7-9: los soles
                if (PartesConstruidas >= 10) visuales.FormaAscendidaOchoVisual = true; // 10: las ofanim

                // (EL ATAQUE no se paga aquí: lo paga BonusDano del
                // despertar — +4%/nivel 101-112 y +10%/nivel 113-120, a
                // TODO el ataque del grimorio, mágico e invocación.)

                // LA PARTE 12 — EL AURA DEL VACÍO: la obra maestra es EL
                // EFECTO ACTIVO (el campo arde mientras la forma viste al
                // portador — hasta que el libro llega al 120 y LA ESCUPE).
                if (PartesConstruidas >= 12)
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

        // ============ PERSISTENCIA ============
        public override void SaveData(Terraria.ModLoader.IO.TagCompound tag)
        {
            tag["fePartes"] = PartesConstruidas;
            tag["feConvertida"] = Convertida;
            tag["feManifestacion"] = ManifestacionVivida;
        }

        public override void LoadData(Terraria.ModLoader.IO.TagCompound tag)
        {
            PartesConstruidas = Math.Max(0, Math.Min(PartesTotales, tag.GetInt("fePartes")));
            Convertida = tag.GetBool("feConvertida");
            ManifestacionVivida = tag.GetBool("feManifestacion");
            // LA MIGRACIÓN HONESTA: las partes v6.141 se pagaron con el
            // nivel del LIBRO — en la v6.142 la casa es el DESPERTAR. El
            // estado sobrevive tal cual (las construidas, construidas
            // están): quien quiera las 12 de verdad, que suba el
            // despertar — las partes ya puestas se respetan.
        }
    }
}
