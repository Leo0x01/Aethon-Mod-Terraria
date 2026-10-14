using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.NPCs
{
    // ======================================================================
    //  v6.50.117 — EL GEMELO DE OSCURIDAD (la letra del usuario): «el
    //  jefe gemelo de oscuridad… misma IA y fases, paleta NEGRO y
    //  MORADO OSCURO, su "sol" es un SOL MORIBUNDO (negro-morado, NO
    //  ilumina)».
    //
    //  LO QUE ES: la mitad oscura del MISMO recuerdo — por eso su nombre
    //  es IDÉNTICO al del jefe de luz («Fragmentos de un recuerdo lejano
    //  - Aethon»): no son dos dioses, son UN recuerdo partido en dos.
    //  Nace junto a su hermano de luz cuando el Nombre de Aethon se
    //  pronuncia (300 px arriba-derecha del portador) y pelea con la
    //  MISMA IA y las MISMAS cinco fases del original — vestido de
    //  negro y morado profundo.
    //
    //  LAS REGLAS DEL GEMELO (las tres de la letra):
    //   · NUNCA toca Main.time ni corre la coreografía de llegada (el
    //     reloj del mundo es del hermano de LUZ — nadie pelea por el
    //     mediodía): HaceLlegada = false;
    //   · SU sol es MORIBUNDO: el cuerpo se dibuja negro-morado y NO
    //     inunda el mundo con su luz (Ilumina = false) — solo una brasa
    //     violeta tenue queda de su pernería;
    //   · a VidaFusion (1000) orbita a su hermano INTOCABLE, y cuando
    //     AMBOS están a mil, la cinemática de FUSIÓN los funde en EL
    //     RECUERDO COMPLETO (todo ese motor vive en la clase base).
    //
    //  EL BATÓN: los gemelos NUNCA ejecutan un ataque grande a la vez —
    //  el TurnoAtaque compartido (en la base) hace que este espere
    //  reposicionándose suave mientras el hermano ataca.
    //
    //  EL BOTÍN: NUNCA paga (la mitad oscura de un recuerdo no deja
    //  herencia — el pago entero es del recuerdo que se COMPLETA: el
    //  fusionado; o del hermano de luz si él es el que sobrevive).
    // ======================================================================
    public class AethonBossOscuro : AethonBoss
    {
        // El cuerpo es 100% código (el PreDraw de la base): se reusa la
        // textura del jefe de luz como ancla del motor (jamás se ve).
        public override string Texture => "AethonMod/Content/NPCs/AethonBoss";

        // La cabeza de la barra de jefe: EL MISMO SOL del recuerdo (son el
        // mismo recuerdo partido — la barra no los distingue, y así debe ser).
        public override string BossHeadTexture => "AethonMod/Content/NPCs/AethonBoss_Head_Boss";

        // === LAS TRES REGLAS DEL GEMELO (la letra, exacta) ===

        /// <summary>NUNCA hace LA LLEGADA: nace YA en la órbita de pelea
        /// (el reloj del mundo y la coreografía son del hermano de luz).</summary>
        protected override bool HaceLlegada => false;

        /// <summary>SU sol es MORIBUNDO: no inunda el mundo con luz — solo
        /// el cuerpo negro-morado y su brasa violeta (que se lee de noche).</summary>
        protected override bool Ilumina => false;

        // === LA PALETA DEL SOL MORIBUNDO: NEGRO y MORADO OSCURO ===
        // (los mismos tonos de la cinemática de fusión: el (90,20,140) de
        // la oscuridad que la casa ya decretó para este capítulo).

        /// <summary>EL HALO: morado profundo (lo que fue oro, moribundo).</summary>
        protected override Color PaletaOro => new(90, 20, 140);

        /// <summary>EL VELO: negro violeta — la oscuridad de la mitad que
        /// la luz no quiso recordar.</summary>
        protected override Color PaletaVioleta => new(24, 10, 38);

        /// <summary>EL NÚCLEO: morado apagado — la brasa que le queda.</summary>
        protected override Color PaletaNucleo => new(150, 90, 220);

        /// <summary>Sus PERNOS viajan en ai[1]=2 (la lluvia masiva del
        /// gemelo se dibuja oscura: perno oscuro, luz de brasa violeta).</summary>
        protected override float PaletaPer => 2f;

        /// <summary>Su hermano es el jefe de LUZ (la IA de fusión de la
        /// base los empareja: a 1000, órbita mutua y fusión).</summary>
        protected override int TipoHermano => ModContent.NPCType<AethonBoss>();

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            // El DisplayName vive en la localización (NPCs.AethonBossOscuro):
            // EL MISMO NOMBRE que el de luz — son el mismo recuerdo partido.
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            // LA MITAD OSCURA (la letra): 1.4 M de vida — la misma furia
            // (daño, defensa, IA y fases INTACTOS heredados de la base).
            NPC.lifeMax = 1_400_000;
        }

        /// <summary>
        /// EL GEMELO NUNCA PAGA BOTÍN: es la mitad oscura del mismo
        /// recuerdo — la herencia completa la cobra quien se COMPLETA (el
        /// Recuerdo Completo tras la fusión, con UNA ESENCIA EXTRA), o el
        /// hermano de luz si es él el fragmento que sobrevive. Sin esto,
        /// matar a los dos por separado pagaba el botín DOBLE.
        /// </summary>
        protected override void DropBotin()
        {
            _yaDropeo = true;   // el OnKill de respaldo también calla
        }
    }
}
