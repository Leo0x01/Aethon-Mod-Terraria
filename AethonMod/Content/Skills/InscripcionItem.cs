using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.Weapons.Escriba;

namespace AethonMod.Content.Skills
{
    /// <summary>
    /// INSCRIPCIONITEM — v6.52.0 — LOS DATOS QUE VIAJAN CON EL ARMA (la
    /// lección estructural de CWR/LegendData): las TRES RANURAS de
    /// inscripciones equipadas y las PRUEBAS de nivel vencidas viven EN
    /// EL ÍTEM — tira la pluma al suelo, véndela, guárdala en un cofre:
    /// SU alma (lo equipado, su nivel) viaja con ella. La BIBLIOTECA de
    /// inscripciones desbloqueadas vive con el JUGADOR
    /// (<see cref="InscripcionJugador"/>) — el modelo asimétrico de CWR,
    /// tal cual.
    /// </summary>
    public class InscripcionItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(Item item, bool lateInstantiation)
            => item.type == ModContent.ItemType<PlumaConsciente>();

        /// <summary>
        /// Las tres ranuras (una por <see cref="RanuraInscripcion"/>): la
        /// CLAVE de la inscripción equipada, o null/vacía si está libre.
        /// </summary>
        public string[] Ranuras = new string[3];

        /// <summary>Las claves estables de las pruebas vencidas (el nivel del arma).</summary>
        public List<string> Pruebas = new List<string>();

        /// <summary>El NIVEL de la pluma: cuántas pruebas lleva vencidas (0..11).</summary>
        public int Nivel => Pruebas.Count;

        /// <summary>El multiplicador de daño por nivel: +12% por prueba vencida.</summary>
        public float DañoPorNivel => 1f + 0.12f * Nivel;

        /// <summary>El dato de un ítem cualquiera (null-safe y TOTAL —
        /// v6.52.2: GetGlobalItem lanza KeyNotFoundException si el ítem
        /// no es una Pluma; el hook ModifyWeaponDamage puede dispararse
        /// con el HeldItem cambiado a mitad de vuelo — el client.log lo
        /// confesó. AHORA De() JAMÁS revienta: no-Pluma = null).</summary>
        public static InscripcionItem De(Item item)
        {
            if (item == null || item.IsAir) return null;
            if (item.type != ModContent.ItemType<PlumaConsciente>()) return null;
            return item.GetGlobalItem<InscripcionItem>();
        }

        /// <summary>
        /// GRABA una inscripción en su ranura (clave null = DESgrabar).
        /// La ranura la fija la PROPIA inscripción (jamás se cruza).
        /// </summary>
        public void Grabar(Item item, string clave)
        {
            if (clave != null)
            {
                InscripcionDefinicion def = InscripcionDefinicion.PorClave(clave);
                if (def == null) return;                       // clave muerta: no existe
                int r = (int)def.Ranura;
                if (r < 0 || r >= Ranuras.Length) return;
                Ranuras[r] = clave;
            }
            else
            {
                for (int i = 0; i < Ranuras.Length; i++) Ranuras[i] = null;
            }
            if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient && item != null)
                item.NetStateChanged();
        }

        /// <summary>¿Tiene esta ranura ocupada por esta inscripción?</summary>
        public bool Equipada(int ranura, string clave) =>
            ranura >= 0 && ranura < Ranuras.Length && Ranuras[ranura] == clave;

        // ==================================================================
        //  LA PERSISTENCIA (el patrón ShardLevelItem de la casa: try/catch
        //  SIEMPRE — ItemIO llama sin red de seguridad)
        // ==================================================================

        public override void SaveData(Item item, TagCompound tag)
        {
            try
            {
                // LAS RANURAS: lista de {ranura:byte, clave} — solo las ocupadas.
                List<TagCompound> ranuras = new List<TagCompound>();
                for (int r = 0; r < Ranuras.Length; r++)
                {
                    if (string.IsNullOrEmpty(Ranuras[r])) continue;
                    TagCompound t = new TagCompound();
                    t["r"] = (byte)r;
                    t["k"] = Ranuras[r];
                    ranuras.Add(t);
                }
                if (ranuras.Count > 0) tag["insRanuras"] = ranuras;
                if (Pruebas.Count > 0) tag["insPruebas"] = Pruebas;
            }
            catch { }
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            try
            {
                // EL SANEO DE CWR (la disciplina de OniMeiStore): al cargar,
                // cada clave se valida contra el registro (¿existe? ¿su
                // ranura coincide?) — un guardado viejo con claves muertas
                // JAMÁS corrompe el estado.
                for (int r = 0; r < Ranuras.Length; r++) Ranuras[r] = null;
                Pruebas = new List<string>();

                if (tag.TryGet<List<TagCompound>>("insRanuras", out var ranuras) && ranuras != null)
                {
                    foreach (TagCompound t in ranuras)
                    {
                        int r = t.GetByte("r");
                        string k = t.GetString("k");
                        InscripcionDefinicion def = InscripcionDefinicion.PorClave(k);
                        if (def == null || (int)def.Ranura != r) continue;   // saneo
                        if (r >= 0 && r < Ranuras.Length) Ranuras[r] = k;
                    }
                }
                if (tag.TryGet<List<string>>("insPruebas", out var pruebas) && pruebas != null)
                {
                    foreach (string k in pruebas)
                        if (!string.IsNullOrEmpty(k) && !Pruebas.Contains(k))
                            Pruebas.Add(k);
                }
            }
            catch
            {
                for (int r = 0; r < Ranuras.Length; r++) Ranuras[r] = null;
                Pruebas = new List<string>();
            }
        }

        // === MP: los datos del ítem viajan SIMÉTRICOS (la lección v6.50.62) ===
        public override void NetSend(Item item, BinaryWriter writer)
        {
            try
            {
                for (int r = 0; r < 3; r++)
                {
                    string k = Ranuras[r];
                    writer.Write(!string.IsNullOrEmpty(k));
                    if (!string.IsNullOrEmpty(k)) writer.Write(k);
                }
                writer.Write((byte)Math.Min(255, Pruebas.Count));
                for (int i = 0; i < Math.Min(255, Pruebas.Count); i++)
                    writer.Write(Pruebas[i]);
            }
            catch { }
        }

        public override void NetReceive(Item item, BinaryReader reader)
        {
            try
            {
                for (int r = 0; r < 3; r++)
                {
                    if (reader.ReadBoolean())
                    {
                        string k = reader.ReadString();
                        InscripcionDefinicion def = InscripcionDefinicion.PorClave(k);
                        if (def != null && (int)def.Ranura == r) Ranuras[r] = k;
                    }
                    else Ranuras[r] = null;
                }
                int n = reader.ReadByte();
                Pruebas = new List<string>(n);
                for (int i = 0; i < n; i++)
                {
                    string k = reader.ReadString();
                    if (!Pruebas.Contains(k)) Pruebas.Add(k);
                }
            }
            catch
            {
                for (int r = 0; r < Ranuras.Length; r++) Ranuras[r] = null;
                Pruebas = new List<string>();
            }
        }

        /// <summary>
        /// EL CLON PROFUNDO (la lección CWR: dos plumas tiradas al suelo
        /// NUNCA comparten alma): ranuras y pruebas se COPIAN.
        /// </summary>
        public override GlobalItem Clone(Item from, Item to)
        {
            InscripcionItem clon = (InscripcionItem)base.Clone(from, to);
            clon.Ranuras = new string[Ranuras.Length];
            for (int i = 0; i < Ranuras.Length; i++) clon.Ranuras[i] = Ranuras[i];
            clon.Pruebas = new List<string>(Pruebas);
            return clon;
        }
    }
}
