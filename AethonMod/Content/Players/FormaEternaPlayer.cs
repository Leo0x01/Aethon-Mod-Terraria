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
    /// FormaEternaPlayer — v6.141 — LA FORMA ETERNA DE VERDAD (la letra
    /// completa: «hagamos que La Forma Eterna hable de la Forma Eterna
    /// de verdad, no los items por separado, me refiero al item que los
    /// tiene todos, estos deben aparecer a partir del nivel 100, para
    /// desbloquearlo se necesita 10 de platino por cada nivel extra mas
    /// 1000 de esencia del eterno, eso por cada nivel y va aumentando
    /// la cantidad hasta un 10% por cada nivel, hasta el nivel donde se
    /// desbloquee toda La Forma Eterna completa, una parte por cada
    /// nivel… cada anillo de sol runico o anillo runico de agujero
    /// negro que tiene la forma eterna es una parte por separado… y lo
    /// mismo con cada una de las partes que conforma a La Forma Eterna
    /// completa… esto no sera un item cosmetico, sera una parte que
    /// sera puesta en el jugador mientras usa el libro y solo cuando
    /// esta este completa se convierte en un item cosmetico el cual
    /// seria La Forma Eterna como tal»).
    ///
    /// EL DISEÑO:
    ///  · LA CASA: el NIVEL NORMAL del Grimorio del Eterno
    ///    (ShardLevelItem.Level — el de la XP de las muertes, el que no
    ///    tiene tope). Desde el 100, el libro abre LA CONSTRUCCIÓN.
    ///  · LAS 12 PARTES (una por nivel, 100→111): cada una es UNA
    ///    PARTE VISUAL DEL CUERPO que se PONE en el jugador MIENTRAS
    ///    USA EL LIBRO (nada de ítems: son MIEMBROS del portador).
    ///  · EL COSTE por parte: 10 de platino + 1000 esencias del eterno,
    ///    +10% CADA NIVEL (11P+1100 en la 2ª, 12,1P+1210 en la 3ª…).
    ///  · EL ATAQUE: cada parte paga +4% de daño (el libro se vuelve
    ///    más letal conforme el dios se viste).
    ///  · LA COMPLETA (nivel 112 = las 12 partes): LA FORMA ETERNA
    ///    COMO TAL — el ítem cosmético entra al inventario Y LA
    ///    MANIFESTACIÓN estalla (la explosión de luz v6.140: todo lo
    ///    vivo en pantalla muere con loot — una vez por libro).
    ///  · EL PAGO: el menú de la mesa del Eterno (el tooltip del libro
    ///    canta la parte siguiente y su coste; el pago lo hace el
    ///    FESTÍN DE ARMAS... NO — la casa manda: se paga con el clic
    ///    derecho del libro EN LA MESA DEL ALTAR, como toda ofrenda —
    ///    pero SIN tocar el grimorio (la letra): el PAGO es del
    ///    JUGADOR, no del arma. La puerta es ESTA clase.
    /// </summary>
    public class FormaEternaPlayer : ModPlayer
    {
        /// <summary>¿Cuántas partes tiene LA FORMA ETERNA COMPLETA?</summary>
        public const int PartesTotales = 12;

        /// <summary>El nivel del libro DONDE nace la construcción.</summary>
        public const int NivelBase = 100;

        // ============ EL ESTADO (persiste con el jugador) ============

        /// <summary>Las partes ya construidas (0..12).</summary>
        public int PartesConstruidas;

        /// <summary>¿Ya vivió LA MANIFESTACIÓN? (una vez por jugador —
        /// el ítem cosmético puede perderse, la gloria no repite).</summary>
        public bool ManifestacionVivida;

        // ============ LAS 12 PARTES (nombre + qué enciende) ============
        //  Cada parte es UNA PIEZA del cuerpo del dios que se PONE en
        //  el portador mientras SOSTIENE el libro (la letra: «una parte
        //  que sera puesta en el jugador mientras usa el libro»).
        //  El ORDEN es de adentro hacia afuera (el dios se viste):
        //   1  LA ESCRITURA — el tatuaje rúnico del pecho (AnilloDorsal
        //      base: la triple corona de conjuro nace en la espalda)
        //   2  EL PRIMER ANILLO — el anillo rúnico interior (gira)
        //   3  EL SEGUNDO ANILLO — el anillo del agujero negro (oscuro)
        //   4  EL TERCER ANILLO — el anillo exterior dorado
        //   5  LA PRIMERA LUZ — el aura de la Forma Ascendida
        //   6  LA BRASA — el humo negro y oro del Eclipse
        //   7  EL PRIMER SOL — el primer sol rúnico (brillo de 4 puntas)
        //   8  EL SEGUNDO SOL — el segundo sol (la pareja)
        //   9  EL TERCER SOL — el tercer sol (el coro completo)
        //  10  LAS OFANIM — las ruedas de Ezequiel (la octava luz)
        //  11  EL MANTO — el velo final que une todo el panteón
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

        // ============ LOS COSTES (10P + 1000 esencias, +10%/nivel) ============

        /// <summary>El coste de la parte N (0-based): 10.000.000 cobre
        /// × 1,1^N — 10P+1000 la primera, 11P+1100 la segunda…</summary>
        public static long MonedasParte(int parte)
            => (long)(10_000_000d * Math.Pow(1.1d, Math.Max(0, parte)));

        public static int EsenciasParte(int parte)
            => (int)(1000d * Math.Pow(1.1d, Math.Max(0, parte)));

        // ============ LA PUERTA: ¿el libro ya abrió la construcción? ============

        /// <summary>El nivel NORMAL del libro del portador (0 si no lo
        /// lleva).</summary>
        public int NivelLibro => Player != null
            ? Player.GetModPlayer<ShardPlayer>().NivelGrimorioPublico : 0;

        /// <summary>¿La puerta está abierta? (libro nivel 100+).</summary>
        public bool PuertaAbierta => NivelLibro >= NivelBase;

        // ============ EL VESTIDO (cada frame mientras USA el libro) ============

        public override void PostUpdateEquips()
        {
            try
            {
                // LA LETRA: «una parte que sera puesta en el jugador
                // MIENTRAS USA EL LIBRO» — el dios solo viste a quien
                // lo SOSTIENE (guardado: el cuerpo se pliega).
                Item held = Player.HeldItem;
                bool usandoLibro = held != null && !held.IsAir &&
                    held.type == ModContent.ItemType<Weapons.GrimoireEternal>();
                if (!usandoLibro || PartesConstruidas <= 0) return;

                var visuales = Player.GetModPlayer<CosmeticPlayer>();
                if (visuales == null) return;

                // LAS PARTES SE PONEN (de adentro hacia afuera):
                if (PartesConstruidas >= 1) visuales.AnilloDorsal = true;         // 1: la escritura
                if (PartesConstruidas >= 5) visuales.FormaAscendidaVisual = true;  // 5: la primera luz
                if (PartesConstruidas >= 6) visuales.BrasaDelEclipse = true;       // 6: la brasa
                if (PartesConstruidas >= 7) visuales.BrilloSolar = true;           // 7-9: los soles
                if (PartesConstruidas >= 10) visuales.FormaAscendidaOchoVisual = true; // 10: las ofanim

                // EL ATAQUE DEL DIOS (la letra: «por cada nivel debes
                // incluir que aumenta el ataque»): +4% TODOS los tipos
                // por parte construida — el dios se viste y aprieta.
                Player.GetDamage(DamageClass.Generic) += 0.04f * PartesConstruidas;

                // LA PARTE 12 — EL AURA DEL VACÍO: la obra maestra es
                // EL EFECTO ACTIVO (AuraDeJuicio2Item): mientras usa el
                // libro con las 12 partes, el CAMPO arde solo (sin
                // ítem: es un MIEMBRO del portador).
                if (PartesConstruidas >= PartesTotales)
                {
                    // el ancla del aura vive como proyectil de la casa
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

        // ============ LA COMPLETA — el ítem + LA MANIFESTACIÓN ============

        /// <summary>LA ÚLTIMA PARTE (la 12) se construyó: la Forma
        /// Eterna COMO TAL nace (el ítem cosmético entra) y LA
        /// MANIFESTACIÓN estalla (una sola vez).</summary>
        public void CulminarLaForma()
        {
            try
            {
                if (Player == null || !Player.active) return;

                // EL ÍTEM COSMÉTICO (la letra: «solo cuando esta este
                // completa se convierte en un item cosmetico el cual
                // seria La Forma Eterna como tal»)
                Player.QuickSpawnItem(Player.GetSource_GiftOrReward(),
                    ModContent.ItemType<Items.Cosmetics.FormaEternaItem>(), 1);

                // LA MANIFESTACIÓN (una sola vez): la explosión de luz
                // que borra la pantalla (la máquina v6.140 — vive en
                // DespertarGrimorioSistema).
                if (!ManifestacionVivida)
                {
                    ManifestacionVivida = true;
                    Content.Systems.DespertarGrimorioSistema.ManifestacionDeLaForma(Player);
                }
            }
            catch { }
        }

        // ============ EL PAGO (la oferta del portador) ============

        /// <summary>PAGA la parte siguiente (clic derecho del LIBRO en
        /// la mano con el dinero y las esencias — SIN tocar la mesa):
        /// devuelve true si construyó. SOLO el jugador local (el
        /// inventario es suyo).</summary>
        public bool PagarParteSiguiente()
        {
            try
            {
                if (!PuertaAbierta) return false;
                if (PartesConstruidas >= PartesTotales) return false;
                if (Player.whoAmI != Main.myPlayer) return false;

                int parte = PartesConstruidas;          // 0-based
                long cobre = MonedasParte(parte);
                int esencias = EsenciasParte(parte);

                // LAS ESENCIAS (el tipo de la casa)
                int tipoEsencia = ModContent.ItemType<Items.Altar.ItemCorrupto>();
                if (!ConsumeItems(tipoEsencia, esencias)) return false;

                // EL DINERO (el cobrador de la casa)
                if (!Player.BuyItem(cobre))
                {
                    // devolver las esencias si el oro no alcanzó
                    Player.QuickSpawnItem(Player.GetSource_Misc("Devolucion"),
                        tipoEsencia, esencias);
                    return false;
                }

                PartesConstruidas++;

                // EL AVISO (la voz de la casa)
                Main.NewText(Language.GetTextValue("Mods.AethonMod.FormaEterna.Construida",
                        PartesConstruidas, PartesTotales,
                        Language.GetTextValue(ClaveParte(parte))),
                    new Color(196, 138, 255));
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);

                // ¿LA COMPLETA?
                if (PartesConstruidas >= PartesTotales)
                    CulminarLaForma();
                return true;
            }
            catch { return false; }
        }

        private bool ConsumeItems(int tipo, int cantidad)
        {
            if (cantidad <= 0) return true;
            // contar primero
            int total = 0;
            for (int i = 0; i < 58; i++)
            {
                Item it = Player.inventory[i];
                if (it != null && !it.IsAir && it.type == tipo) total += it.stack;
            }
            if (total < cantidad) return false;
            // consumir
            int falta = cantidad;
            for (int i = 0; i < 58 && falta > 0; i++)
            {
                Item it = Player.inventory[i];
                if (it == null || it.IsAir || it.type != tipo) continue;
                int toma = Math.Min(falta, it.stack);
                it.stack -= toma;
                falta -= toma;
                if (it.stack <= 0) it.TurnToAir();
            }
            return true;
        }

        // ============ PERSISTENCIA ============
        public override void SaveData(Terraria.ModLoader.IO.TagCompound tag)
        {
            tag["fePartes"] = PartesConstruidas;
            tag["feManifestacion"] = ManifestacionVivida;
        }

        public override void LoadData(Terraria.ModLoader.IO.TagCompound tag)
        {
            PartesConstruidas = Math.Max(0, Math.Min(PartesTotales, tag.GetInt("fePartes")));
            ManifestacionVivida = tag.GetBool("feManifestacion");
        }
    }
}
