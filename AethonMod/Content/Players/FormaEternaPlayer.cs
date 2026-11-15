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

                // ¿LA CONVERSIÓN? (la 12ª parte — nivel 112 del despertar)
                if (PartesConstruidas >= PartesTotales && !Convertida)
                    ConvertirLaForma();
            }
            catch { }
        }

        /// <summary>v6.142 — LA CONVERSIÓN (la letra: «en el momento de
        /// la manifestacion la forma eterna que es parte del grimorio
        /// desaparece ya que es convertida en el item cosmetico»): la
        /// forma que vestía al portador SE CONVIERTE — el ítem cosmético
        /// LA FORMA ETERNA nace, LA MANIFESTACIÓN estalla (una sola vez)
        /// y desde este instante los visuales YA NO VISTEN al portador
        /// (¿quieren la forma? que se pongan el ítem — es suyo).</summary>
        private void ConvertirLaForma()
        {
            try
            {
                if (Player == null || !Player.active) return;
                Convertida = true;

                // EL ÍTEM COSMÉTICO NACE
                Player.QuickSpawnItem(Player.GetSource_GiftOrReward(),
                    ModContent.ItemType<Items.Cosmetics.FormaEternaItem>(), 1);

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
                // LA LETRA: «una parte que sera puesta en el jugador
                // MIENTRAS USA EL LIBRO» — el dios solo viste a quien
                // lo SOSTIENE. Y tras LA CONVERSIÓN ya no viste a nadie
                // (la forma vive en el ÍTEM — «desaparece ya que es
                // convertida en el item cosmetico»).
                if (Convertida || PartesConstruidas <= 0) return;
                Item held = Player.HeldItem;
                bool usandoLibro = held != null && !held.IsAir &&
                    held.type == ModContent.ItemType<Weapons.GrimoireEternal>();
                if (!usandoLibro) return;

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
                // EFECTO ACTIVO (el campo arde solo mientras la forma
                // viste al portador — hasta LA CONVERSIÓN).
                if (PartesConstruidas >= PartesTotales)
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
