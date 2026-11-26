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
    //  ProyectadoPlayer — v6.177 — EL PORTADOR DEL VISOR DEL PROYECTADO.
    //
    //  LA LETRA (sesión 30): «el arma del ciclope no tiene nada que ver
    //  te voy a dar el archivo .tmod del ciclope proyectado» — el arma
    //  del meme de Chopilas, RECREADA 100% por la casa (el .tmod fue
    //  solo el PLANO: mecánica y espíritu, cero código y cero sprites
    //  ajenos — la ley de la casa).
    //
    //  LA ENERGÍA PROYECTADA: una barra PROPIA (nada de maná) que
    //  empieza en 100 y crece con las MEJORAS DE JEFE (hasta 300); el
    //  rayo continuo la GASTA (20/s), se recarga a 15/s (×1.1 por cada
    //  mejora usada) tras 1 s de reposo, y si se AGOTA hay que esperar
    //  a que vuelva a 25. La del Señor de la Luna es INFINITA.
    //
    //  LA PROGRESIÓN: el daño escala SOLO — 11 etapas por los jefes de
    //  Terraria caídos (de «Recién proyectado» al «Señor de la Luna»),
    //  +5% por cada jefe OPCIONAL (Rey Gelatina, Reina Abeja, Deerclops,
    //  Reina Gelatina, Duque, Emperatriz) y un bono pequeño por cada
    //  enemigo caído con tope por etapa (la desesperación acumulada).
    //
    //  LA PROYECCIÓN (tecla X): sin gastar energía, con 20 s de
    //  enfriamiento — «te quitas el visor, se te encienden los ojos y
    //  sueltas un rayo enorme que atraviesa todo».
    // ======================================================================
    public class ProyectadoPlayer : ModPlayer
    {
        // === LA LÍNEA DE TIEMPO DE LA PROYECCIÓN (60 t = 1 s) ===
        public const int EnfriamientoEspecial = 1200;    // 20 s (la letra del plano)
        public const float MultiplicadorEspecial = 3f;    // el rayo grande golpea ×3

        // === EL ESTADO PERSISTIDO ===
        public int Muertes;              // la desesperación acumulada (enemigos caídos con el visor)
        public bool RecibioVisor;        // la entrega del primer mundo
        public int ObjetosUsados;        // bitmask de las 10 mejoras de jefe

        // === EL ESTADO VIVO ===
        public float Energia = 100f;
        public bool Agotado;
        public int Enfriamiento;
        private int _ticksSinDisparar;
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

        // LOS PUNTOS de cada mejora (el orden de los índices — ver MejoraProyectada.Indice)
        public static readonly int[] PuntosMejora = { 20, 20, 30, 20, 20, 20, 20, 20, 10, 20 };

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

        // === LOS CONTADORES DE LAS MEJORAS ===
        public bool Usado(int indice) => (ObjetosUsados & (1 << indice)) != 0;
        public int CantidadUsados
        {
            get
            {
                int n = 0;
                for (int i = 0; i < PuntosMejora.Length; i++)
                    if (Usado(i)) n++;
                return n;
            }
        }

        public bool Infinito => Usado(9);   // la LUNA que te ibas a bajar
        public float EnergiaMax
        {
            get
            {
                float m = 100f;
                for (int i = 0; i < PuntosMejora.Length; i++)
                    if (Usado(i)) m += PuntosMejora[i];
                return m;
            }
        }
        public float MultiplicadorRecarga => 1f + 0.1f * CantidadUsados;
        public bool PuedeDisparar => Infinito || (!Agotado && Energia > 0f);

        public bool GastarEnergia()
        {
            _ticksSinDisparar = 0;
            if (Infinito) return true;
            Energia -= 1f / 3f;                     // 20/s (el plano)
            if (Energia <= 0f)
            {
                Energia = 0f;
                Agotado = true;
                return false;
            }
            return true;
        }

        public bool AplicarMejora(int indice)
        {
            if (Usado(indice)) return false;
            ObjetosUsados |= 1 << indice;
            Energia = EnergiaMax;
            Agotado = false;
            return true;
        }

        public void MarcarAnimacion(float retiro, float brillo)
        {
            Retiro = retiro;
            BrilloOjos = brillo;
            _tickAnimacion = Main.GameUpdateCount;
        }

        // === LA PROYECCIÓN (el gatillo de la tecla X) ===
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            try
            {
                if (global::AethonMod.Content.Systems.ProyectadoSistema.TeclaProyectarse != null &&
                    global::AethonMod.Content.Systems.ProyectadoSistema.TeclaProyectarse.JustPressed)
                    IntentarProyectarse();
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ProyectadoPlayer", exRegistro); }
        }

        private void IntentarProyectarse()
        {
            try
            {
                if (Player.dead || Player.CCed || Player.noItems) return;
                Item held = Player.HeldItem;
                if (held == null || held.type != ModContent.ItemType
                        <global::AethonMod.Content.Items.Investigaciones.VisorDelProyectado>())
                {
                    if (Player.whoAmI == Main.myPlayer)
                        CombatText.NewText(Player.Hitbox, Color.Gray,
                            Language.GetTextValue("Mods.AethonMod.Proyectado.SinVisor"), false);
                    return;
                }
                if (Enfriamiento > 0)
                {
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        CombatText.NewText(Player.Hitbox, Color.Gray,
                            Language.GetTextValue("Mods.AethonMod.Proyectado.AunNo",
                                Enfriamiento / 60 + 1), false);
                        var tick = SoundID.MenuTick;
                        Terraria.Audio.SoundEngine.PlaySound(tick.WithPitchOffset(-0.8f), Player.Center);
                    }
                    return;
                }
                // EL RAYO CONTINUO SE RECOGE: la proyección es la ÚNICA en pantalla
                int tipoOptico = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Investigaciones.RayoOpticoProyectado>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p != null && p.active && p.owner == Player.whoAmI && p.type == tipoOptico)
                        p.Kill();
                }
                int dano = (int)(Player.GetWeaponDamage(held) * MultiplicadorEspecial);
                float kb = held.knockBack * 2f;
                Vector2 rumbo = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.UnitX * Player.direction);
                int tipoGrande = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Investigaciones.RayoProyectado>();
                Projectile.NewProjectile(Player.GetSource_ItemUse(held), Player.Center, rumbo,
                    tipoGrande, dano, kb, Player.whoAmI);
                Enfriamiento = EnfriamientoEspecial;
                Player.AddBuff(ModContent.BuffType
                    <global::AethonMod.Content.Buffs.RecuperacionProyectada>(), EnfriamientoEspecial, true);
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ProyectadoPlayer", exRegistro); }
        }

        // === LA ENERGÍA VIVA ===
        private void ActualizarEnergia()
        {
            float max = EnergiaMax;
            if (Infinito)
            {
                Energia = max;
                Agotado = false;
                return;
            }
            _ticksSinDisparar++;
            if (_ticksSinDisparar > 60 && Energia < max)
                Energia = System.Math.Min(max, Energia + 15f * MultiplicadorRecarga / 60f);
            if (Agotado && Energia >= 25f)
                Agotado = false;
        }

        public override void PostUpdate()
        {
            try
            {
                ActualizarEnergia();
                if (Enfriamiento > 0)
                {
                    Enfriamiento--;
                    int buff = ModContent.BuffType
                        <global::AethonMod.Content.Buffs.RecuperacionProyectada>();
                    if (Enfriamiento > 0 && !Player.dead && !Player.HasBuff(buff))
                        Player.AddBuff(buff, Enfriamiento, true);
                    if (Enfriamiento == 0 && Player.whoAmI == Main.myPlayer)
                    {
                        CombatText.NewText(Player.Hitbox, new Color(255, 80, 60),
                            Language.GetTextValue("Mods.AethonMod.Proyectado.Listo"), false);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Unlock.WithVolumeScale(0.9f), Player.Center);
                    }
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
            Energia = EnergiaMax;
            Agotado = false;
            if (Player.whoAmI != Main.myPlayer || RecibioVisor) return;
            RecibioVisor = true;
            Player.QuickSpawnItem(Player.GetSource_Misc("Proyectado"),
                ModContent.ItemType<global::AethonMod.Content.Items.Investigaciones.VisorDelProyectado>(), 1);
        }

        // === LA MEMORIA ===
        public override void SaveData(TagCompound tag)
        {
            tag["muertes"] = Muertes;
            tag["recibioVisor"] = RecibioVisor;
            tag["objetosUsados"] = ObjetosUsados;
        }

        public override void LoadData(TagCompound tag)
        {
            Muertes = tag.GetInt("muertes");
            RecibioVisor = tag.GetBool("recibioVisor");
            ObjetosUsados = tag.GetInt("objetosUsados");
            Energia = EnergiaMax;
        }
    }
}
