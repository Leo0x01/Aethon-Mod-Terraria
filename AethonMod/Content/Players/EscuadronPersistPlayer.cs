using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Players
{
    // ======================================================================
    //  EscuadronPersistPlayer — v6.86 — EL ESCUADRÓN QUE NUNCA SE VA (la
    //  letra del usuario: «algo que debes añadir es que los minions del
    //  grimorio sean persistentes, aunque el jugador muera o aunque salga
    //  de la partida, estos siempre permanecen invocados a no ser que el
    //  jugador los desinvoque usando su buff»).
    //
    //  LAS TRES LEYES:
    //   · LA MUERTE NO LOS TOCA (OjoEternoMinion.AI ya no mata al dueño
    //     caído — lo orbitan hasta el renacer).
    //   · LA SALIDA LOS GUARDA: al guardar el .plr, SE FOTOGRAFÍA el
    //     escuadrón VIVO (cuántos ojos, su nivel, sus mejoras, su daño)
    //     — nada de estados derivados: el retrato exacto.
    //   · LA VUELTA LOS RESUCITA: OnEnterWorld re-invoca el escuadrón
    //     retratado (ai[2] = nivel + mejoras en la fracción — el
    //     contrato de la casa), con su daño de nacimiento y el buff
    //     re-anclado (Sostener hace el resto).
    //  LA DESPEDIDA LEGÍTIMA sigue siendo el CLIC DERECHO del buff
    //  (EscuadronUtil.Despedir): despedidos = retrato vacío = nada
    //  vuelve. El patrón es el de SolEnMiniaturaPlayer (v6.85), ahora
    //  para el escuadrón ENTERO.
    // ======================================================================
    public class EscuadronPersistPlayer : ModPlayer
    {
        /// <summary>EL RETRATO: el nivel de libro de cada ojo (ai[2]
        /// enteros — viaja solo en el proyectil).</summary>
        public List<int> Niveles = new List<int>();

        /// <summary>EL RETRATO: las mejoras de cada ojo (0..4 — la
        /// fracción de ai[2] en centésimas).</summary>
        public List<int> MejorasList = new List<int>();

        /// <summary>EL RETRATO: el daño de nacimiento de cada ojo (lo
        /// hornea la invocación original — DanoOjosDelEterno).</summary>
        public List<int> Danos = new List<int>();

        public override void SaveData(TagCompound tag)
        {
            try
            {
                // LA FOTOGRAFÍA del escuadrón VIVO (nada de contadores
                // paralelos: se mira el mundo de verdad).
                Niveles.Clear(); MejorasList.Clear(); Danos.Clear();
                int tipo = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.OjoEternoMinion>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || p.type != tipo) continue;
                    if (p.owner != Player.whoAmI) continue;

                    float a2 = p.ai[2];
                    int nivel = (int)System.MathF.Floor(a2);
                    if (nivel < 1) nivel = 1;
                    int mejoras = global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasDe(p);
                    int dano = p.damage;
                    if (dano < 1) dano = 1;

                    Niveles.Add(nivel);
                    MejorasList.Add(mejoras);
                    Danos.Add(dano);
                }

                if (Niveles.Count > 0)
                {
                    tag["ojosNiveles"] = Niveles;
                    tag["ojosMejoras"] = MejorasList;
                    tag["ojosDanos"] = Danos;
                }
            }
            catch { }
        }

        public override void LoadData(TagCompound tag)
        {
            try
            {
                Niveles = new List<int>(tag.GetList<int>("ojosNiveles"));
                MejorasList = new List<int>(tag.GetList<int>("ojosMejoras"));
                Danos = new List<int>(tag.GetList<int>("ojosDanos"));
                // LA HIGIENE DEL RETRATO: las tres listas del mismo largo.
                int n = Niveles.Count;
                if (MejorasList.Count != n) MejorasList = new List<int>();
                if (Danos.Count != n) Danos = new List<int>();
                if (MejorasList.Count == 0 && n > 0)
                { MejorasList = new List<int>(new int[n]); Danos = new List<int>(new int[n]); for (int k = 0; k < n; k++) { MejorasList[k] = 0; Danos[k] = 10; } }
            }
            catch { Niveles = new List<int>(); MejorasList = new List<int>(); Danos = new List<int>(); }
        }

        public override void OnEnterWorld()
        {
            try
            {
                if (Main.netMode == NetmodeID.Server) return;
                if (Player.whoAmI != Main.myPlayer) return;
                if (Niveles.Count == 0) return;   // retrato vacío: despedidos o jamás invocados

                int tipo = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.OjoEternoMinion>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active) continue;
                    if (p.type == tipo && p.owner == Player.whoAmI)
                        return;   // ya vive (carga de mundo con él presente)
                }

                // LA RESURRECCIÓN: el retrato, ojo por ojo (ai[2] = nivel
                // + mejoras en la fracción — el contrato de la casa; el
                // daño de nacimiento del retrato).
                int despertar = Systems.DespertarGrimorio.NivelDe(Player);
                var fuente = Player.GetSource_Misc("PersistenciaEscuadron");
                for (int k = 0; k < Niveles.Count && k < 22; k++)
                {
                    int nivel = System.Math.Max(1, Niveles[k]);
                    int mejoras = System.Math.Max(0, System.Math.Min(4, MejorasList[k]));
                    int dano = System.Math.Max(1, Danos[k]);
                    int idx = Projectile.NewProjectile(fuente,
                        Player.Center - Vector2.UnitY * 40f, Vector2.Zero,
                        tipo, dano, 0f, Player.whoAmI, 0f, 0f,
                        nivel + mejoras * 0.01f);
                    if (idx >= 0)
                    {
                        // v6.51.9 — EL ×1,25 DEL DESPERTAR MÁXIMO: si YA
                        // está en el 100 al volver, el ojo NACE horneado.
                        var ojo = Main.projectile[idx].ModProjectile
                            as global::AethonMod.Content.Projectiles.OjoEternoMinion;
                        if (ojo != null && despertar >= Systems.DespertarGrimorio.NivelMax)
                            ojo.Bonus50Horneado = true;
                    }
                }

                // EL BUFF DEL ESCUADRÓN re-anclado (Sostener lo mantiene).
                Player.AddBuff(ModContent.BuffType
                    <global::AethonMod.Content.Buffs.OjoEternoBuff>(), 18000);
            }
            catch { }
        }
    }
}
