using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Players
{
    // ======================================================================
    //  ProyectadoPlayer — v6.179 — EL PORTADOR DEL VISOR DEL PROYECTADO.
    //
    //  LA LETRA (sesión 32): «solo necesito el arma visor del proyectado
    //  cualquier otra cosa del mod puedes borrarlo, ademas que el arma
    //  ya este en su maxima capacidad y pueda cambiar entre las
    //  diferentes fases».
    //
    //  v6.179 — SOLO EL ARMA, YA MÁXIMA (la cirugía de la letra):
    //   · LAS DIEZ MEJORAS MUEREN (la carta que nunca le diste, la luna
    //     que te ibas a bajar…): el arma NACE a SU MÁXIMA CAPACIDAD —
    //     la energía PROYECTADA es INFINITA (300/300 que jamás se gasta:
    //     la luna ya está bajada de fábrica).
    //   · LAS DOS FASES DEL ARMA se CAMBIAN en combate: la tecla X (la
    //     de siempre) o el CLIC DERECHO con el visor en la mano —
    //       FASE 1 · EL RAYO CONTINUO: mantén el clic y el haz fino
    //                 sigue al cursor (la de siempre);
    //       FASE 2 · LA PROYECCIÓN: cada clic suelta EL RAYO ENORME que
    //                 atraviesa todo (el cine completo de la X vieja,
    //                 sin enfriamiento — una proyección a la vez).
    //   · La fase viaja con el jugador (se guarda).
    //
    //  La progresión que queda es la DEL MUNDO (las 11 etapas suben solas
    //  con los jefes de Terraria caídos — v6.177) y la desesperación
    //  acumulada (los enemigos caídos con el visor).
    // ======================================================================
    public class ProyectadoPlayer : ModPlayer
    {
        // === LA CAPACIDAD MÁXIMA (la letra: «ya esté en su máxima
        //     capacidad») — 300, el tope de las diez mejoras muertas ===
        public const float EnergiaTope = 300f;

        // === LA FASE ACTIVA (1 = rayo continuo · 2 = la proyección) ===
        public int Fase = 1;

        // === EL ESTADO PERSISTIDO ===
        public int Muertes;              // la desesperación acumulada (enemigos caídos con el visor)
        public bool RecibioVisor;        // la entrega del primer mundo

        // === EL ESTADO VIVO (la animación de la proyección) ===
        public float Retiro;             // la animación: el visor se LEVANTA de la cara
        public float BrilloOjos;         // y el ojo de abajo SE ENCIENDE
        private uint _tickAnimacion;

        public bool AnimacionActiva => Main.GameUpdateCount - _tickAnimacion <= 2;
        public bool VisorFuera => AnimacionActiva && Retiro > 0.08f;

        // === LA PROGRESIÓN (las 11 ETAPAS del plano — el daño base de cada una) ===
        public static readonly (string nombre, int dano)[] Etapas =
        {
            ("Recién proyectado", 10),
            ("Ojo de Cthulhu", 15),
            ("Devorador de mundos / Cerebro de Cthulhu", 21),
            ("Esqueletrón", 28),
            ("Muro carnoso", 40),
            ("Primer jefe mecánico", 50),
            ("Los tres jefes mecánicos", 62),
            ("Plantera", 76),
            ("Gólem", 88),
            ("Sectario lunático", 102),
            ("Señor de la Luna", 135),
        };

        public static int EtapaActual()
        {
            int etapa = 0;
            if (NPC.downedBoss1) etapa = 1;
            if (NPC.downedBoss2) etapa = 2;
            if (NPC.downedBoss3) etapa = 3;
            if (Main.hardMode) etapa = 4;
            if (NPC.downedMechBossAny) etapa = 5;
            if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3) etapa = 6;
            if (NPC.downedPlantBoss) etapa = 7;
            if (NPC.downedGolemBoss) etapa = 8;
            if (NPC.downedAncientCultist) etapa = 9;
            if (NPC.downedMoonlord) etapa = 10;
            return etapa;
        }

        // v6.178 — EL TOTAL DE LOS OPCIONALES (la cura del client.log: la
        // clave Opcionales pide {1} = el total y nadie lo traía)
        public const int TotalOpcionales = 6;   // Slime · Abeja · Ciervo · Reina Gelatina · Pez Dragón · Emperatriz

        public static int JefesOpcionales()
        {
            int n = 0;
            if (NPC.downedSlimeKing) n++;
            if (NPC.downedQueenBee) n++;
            if (NPC.downedDeerclops) n++;
            if (NPC.downedQueenSlime) n++;
            if (NPC.downedFishron) n++;
            if (NPC.downedEmpressOfLight) n++;
            return n;
        }

        public static float BonoOpcionales() => JefesOpcionales() * 0.05f;

        public static float TopeMuertes(int etapa) => 0.05f + 0.025f * etapa;

        public static float BonoMuertes(int muertes, int etapa)
            => System.Math.Min(muertes * 0.0005f, TopeMuertes(etapa));

        // === LA CAPACIDAD MÁXIMA, SIEMPRE (la letra: «ya esté en su
        //     máxima capacidad»): INFINITA — 300/300 que no se gasta ===
        public bool Infinito => true;                 // la luna ya está bajada (de fábrica)
        public float EnergiaMax => EnergiaTope;       // el tope de las diez mejoras muertas
        public float Energia => EnergiaTope;          // siempre LLENA: la barra jamás baja
        public bool Agotado => false;                 // jamás
        public bool PuedeDisparar => true;            // siempre

        public bool GastarEnergia() => true;           // no gasta NADA (capacidad máxima infinita)

        public void MarcarAnimacion(float retiro, float brillo)
        {
            Retiro = retiro;
            BrilloOjos = brillo;
            _tickAnimacion = Main.GameUpdateCount;
        }

        // ================================================================
        //  EL CAMBIO DE FASE (la letra: «pueda cambiar entre las
        //  diferentes fases»): la TECLA X de siempre o el CLIC DERECHO
        //  con el visor en la mano — un solo camino, la misma voz.
        // ================================================================
        private static bool _clicDerechoAntes;         // el flanco del clic derecho (sin spam)

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            try
            {
                bool tecla = global::AethonMod.Content.Systems.ProyectadoSistema.TeclaProyectarse != null &&
                    global::AethonMod.Content.Systems.ProyectadoSistema.TeclaProyectarse.JustPressed;
                bool clicDerecho = triggersSet.MouseRight && !_clicDerechoAntes &&
                    !Main.LocalPlayer.mouseInterface && !Main.playerInventory && !Main.ingameOptionsWindow;
                _clicDerechoAntes = triggersSet.MouseRight;
                if (tecla || clicDerecho) CambiarFase();
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ProyectadoPlayer", exRegistro); }
        }

        /// <summary>EL INTERRUPTOR: fase 1 (rayo continuo) ⇄ fase 2 (la
        /// proyección) — con voz en pantalla y su sonido.</summary>
        public void CambiarFase()
        {
            try
            {
                Item held = Player.HeldItem;
                if (held == null || held.type != ModContent.ItemType
                        <global::AethonMod.Content.Items.Investigaciones.VisorDelProyectado>())
                {
                    if (Player.whoAmI == Main.myPlayer)
                        CombatText.NewText(Player.Hitbox, Color.Gray,
                            Language.GetTextValue("Mods.AethonMod.Proyectado.SinVisor"), false);
                    return;
                }
                Fase = Fase == 1 ? 2 : 1;
                if (Player.whoAmI == Main.myPlayer)
                {
                    CombatText.NewText(Player.Hitbox, new Color(255, 90, 70),
                        Language.GetTextValue("Mods.AethonMod.Proyectado.FaseCambio",
                            Language.GetTextValue("Mods.AethonMod.Proyectado.Fase" + Fase)), true);
                    Terraria.Audio.SoundEngine.PlaySound(
                        Terraria.ID.SoundID.MenuTick.WithPitchOffset(Fase == 2 ? 0.35f : -0.35f), Player.Center);
                }
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ProyectadoPlayer", exRegistro); }
        }

        // === LA DESPERACIÓN ACUMULADA: cada caída con el visor suma ===
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                if (Player.whoAmI != Main.myPlayer || target.life > 0) return;
                if (target.friendly || target.immortal || target.lifeMax <= 5) return;
                if (target.SpawnedFromStatue || Terraria.ID.NPCID.Sets.CountsAsCritter[target.type]) return;
                if (target.realLife >= 0 && target.realLife != target.whoAmI) return;
                // la proyección del rayo grande también cuenta: el proyectil dueño es NUESTRO
                Muertes += target.boss ? 50 : 1;
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ProyectadoPlayer", exRegistro); }
        }

        // === LA ENTREGA: el visor llega al PRIMER mundo (el plano) ===
        public override void OnEnterWorld()
        {
            if (Player.whoAmI != Main.myPlayer || RecibioVisor) return;
            RecibioVisor = true;
            Player.QuickSpawnItem(Player.GetSource_Misc("Proyectado"),
                ModContent.ItemType<global::AethonMod.Content.Items.Investigaciones.VisorDelProyectado>(), 1);
        }

        // === LA MEMORIA (la fase viaja con el jugador) ===
        public override void SaveData(TagCompound tag)
        {
            tag["muertes"] = Muertes;
            tag["recibioVisor"] = RecibioVisor;
            tag["fase"] = Fase;
        }

        public override void LoadData(TagCompound tag)
        {
            Muertes = tag.GetInt("muertes");
            RecibioVisor = tag.GetBool("recibioVisor");
            Fase = tag.ContainsKey("fase") ? System.Math.Clamp(tag.GetInt("fase"), 1, 2) : 1;
        }
    }
}
