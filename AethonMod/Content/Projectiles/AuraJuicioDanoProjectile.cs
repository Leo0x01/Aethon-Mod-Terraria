using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Buffs;
using AethonMod.Content.Items.Accessories;

namespace AethonMod.Content.Projectiles
{
    // ======================================================================
    //  AuraJuicioDano — v6.104 — EL GOLPE DE LA QUEMADURA DEL AURA DE
    //  JUICIO (el proyectil invisible teletransportado del original).
    //
    //  LA TÉCNICA DEL ORIGINAL: la quemadura NO llama a StrikeNPC a
    //  pelo — cada golpe de fuego es un PROYECTIL FANTASMA que se
    //  TELETRANSPORTA al cuerpo del impío marcado, pega (con la
    //  crítica del dueño y 50 de penetración de armadura) y MUERE en
    //  el mismo tick. Nada de hitboxes, nada de colisiones: el golpe
    //  ES el proyectil.
    //
    //  EL CAUCE DE LA CASA (decisión documentada): el golpe corre por
    //  GolpeMotor (el cauce del motor v6.50 — Projectile.Damage con la
    //  puerta del blanco exacto): crítica REAL con las stats del
    //  dueño, varianza ±15% con suerte, on-hit del juego entero,
    //  crédito de kill y difusión MP — en vez del StrikeNPC crudo. El
    //  RESULTADO es el del original (daño, 50 AP, crítica del mejor
    //  crit de clase) con la tubería de la casa.
    //
    //  EL CONTRATO DE LOS CAMPOS:
    //   · ai[0]        → whoAmI del NPC objetivo.
    //   · ai[1]        → el daño final (quemadura × intensidad).
    //   · localAI[0]   → 1 cuando el golpe YA se cobró (guard del tick).
    //   · localAI[1]   → whoAmI del jugador dueño de la marca.
    // ======================================================================
    public class AuraJuicioDano : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 4;            // vive un suspiro: golpea y muere
            Projectile.aiStyle = -1;
            Projectile.hide = true;             // el fantasma JAMÁS se dibuja
            // LA PENETRACIÓN DEL ORIGINAL: 50 puntos de armadura atravesados.
            Projectile.ArmorPenetration = 50;
        }

        public override bool? CanDamage() => false;   // el daño lo cobra el motor (GolpeMotor), no la colisión

        public override bool PreDraw(ref Color lightColor) => false;

        public override void AI()
        {
            // === EL GUARD DEL TICK (un golpe, una sola vez) ===
            if (Projectile.localAI[0] == 1f)
            {
                Projectile.Kill();
                return;
            }
            Projectile.localAI[0] = 1f;

            // === EL BLANCO — el impío marcado (vivo aún, o el golpe muere) ===
            int idx = (int)Projectile.ai[0];
            NPC objetivo = idx >= 0 && idx < Main.maxNPCs ? Main.npc[idx] : null;
            if (objetivo == null || !objetivo.active)
            {
                Projectile.Kill();
                return;
            }

            // LA TELEPORTACIÓN: el fantasma SE POSA en el cuerpo del impío.
            Projectile.Center = objetivo.Center;

            // === EL DUEÑO (quien pagó la marca — sus stats calzan el golpe) ===
            int idxDueno = (int)Projectile.localAI[1];
            Player dueno = idxDueno >= 0 && idxDueno < Main.maxPlayers ? Main.player[idxDueno] : null;
            if (dueno == null || !dueno.active)
            {
                Projectile.Kill();
                return;
            }

            // ==============================================================
            //  LAS STATS DEL DUEÑO — la fidelidad del original:
            //   · LA CLASE DOMINANTE: el golpe viste la clase con MÁS
            //     daño del portador (el «MejorDañoDeClase» original).
            //   · EL MEJOR CRÍTICO: la mayor chance crítica entre melé,
            //     distancia, magia e invocación («puede crítico»).
            //  (GolpeMotor solo SUBE estos valores si el dueño trae
            //  mejores — aquí ya van calzados a la medida.)
            // ==============================================================
            Projectile.DamageType = AuraDeJuicioItem.ClaseDominante(dueno);
            int critMejor = (int)AuraDeJuicioItem.MejorCritica(dueno);
            if (critMejor > Projectile.CritChance)
                Projectile.CritChance = critMejor;

            // === EL GOLPE — el cauce del motor de la casa ===
            int dano = (int)Projectile.ai[1];
            if (dano > 0)
                Content.Systems.GolpeMotor.Golpear(Projectile, objetivo, dano, 0f, true);

            // === EL DEBUFF — las Llamas del Juicio, 30 ticks por golpe
            //     (la autoridad del buff: SP/servidor, como la casa).
            //     (NPC no tiene .dead — ese es del Player: active basta.) ===
            if (objetivo.active && Main.netMode != NetmodeID.MultiplayerClient)
                objetivo.AddBuff(ModContent.BuffType<LlamasDelJuicio>(), 30, false);

            Projectile.Kill();
        }
    }
}
