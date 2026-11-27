using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  InteriorInvocaGlobalItem — v6.177 — LA PUERTA SIEMPRE ABIERTA DEL
    //  MUNDO INTERIOR.
    //
    //  LA LETRA (sesión 30): «asegurate de hacer que el mundo interior sea
    //  valido para invocar a todos los jefes de terraria sin importar el
    //  clima, tiempo o bioma, simplemente en el mundo interior se pueden
    //  invocar siempre».
    //
    //  LA LEY: DENTRO del Mundo Interior del Eterno, TODO objeto de
    //  invocación de jefe de Terraria FUNCIONA SIEMPRE — la noche, el
    //  bioma, el clima y la progresión de vanilla NO manda dentro del
    //  cuerpo del dios: ahí el único dios es el Eterno. La vitrina del
    //  pilar ya invoca a cada jefe DEVORADO; ESTA puerta cubre los
    //  OBJETOS de invocación (el ojo sospechoso al mediodía, la comida
    //  de gusano sin corrupción, el gusano mecánico de día…).
    //
    //  LA MECÁNICA: CanUseItem intercepta el uso DENTRO del interior →
    //  el jefe nace delante del portador (NPC.SpawnBoss — con su
    //  «ha despertado») y el objeto se consume él solo (los
    //  consumibles de vanilla pagan su unidad). Fuera del interior:
    //  NADA cambia (vanilla manda como siempre).
    // ======================================================================
    public class InteriorInvocaGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstancing)
            => Mapa.TryGetValue(item.type, out _);

        private static readonly Dictionary<int, int> Mapa = new()
        {
            { ItemID.SuspiciousLookingEye, NPCID.EyeofCthulhu },
            { ItemID.WormFood, NPCID.EaterofWorldsHead },
            { ItemID.BloodySpine, NPCID.BrainofCthulhu },
            { ItemID.Abeemination, NPCID.QueenBee },
            { ItemID.LihzahrdPowerCell, NPCID.Golem },
            { ItemID.MechanicalWorm, NPCID.TheDestroyer },
            { ItemID.MechanicalEye, NPCID.Retinazer },      // vanilla pare él: los gemelos nacen del spawn
            { ItemID.MechanicalSkull, NPCID.SkeletronPrime },
            { ItemID.TruffleWorm, NPCID.DukeFishron },
            { ItemID.CelestialSigil, NPCID.MoonLordCore },
            { ItemID.ClothierVoodooDoll, NPCID.SkeletronHead },   // sin muñeco que matar dentro: el jefe directo
        };

        public override bool CanUseItem(Item item, Player player)
        {
            // v6.179 — LA MARCA DE LA LLAMADA (la letra: «se invoca mas de
            // 2 veces despues de borrar la lista del festin y no lo hace»):
            // el ítem de invocación del Devorador en la MANO es una
            // llamada del PORTADOR — la marca pendiente vive en el
            // ShardPlayer y SOLO la cabeza de Devorador que nace con ella
            // cuenta una invocación (el vigía de FaucesGlobalNPC la gasta).
            // Cubre FUERA del interior (el camino de vanilla que sigue a
            // este return)…
            // SOLO dentro del Mundo Interior — afuera vanilla manda
            if (!Systems.SubmundoGrimorio.Activo || Main.gameMenu) return true;
            if (!Mapa.TryGetValue(item.type, out int tipoJefe)) return true;
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return false;

                // LA POSICIÓN: delante del portador, en alto — clavada
                // DENTRO del cuerpo del dios (el interior es 800×600:
                // nada nace fuera del mundo)
                Vector2 pos = player.Center + new Vector2(player.direction * 420f, -240f);
                pos.X = MathHelper.Clamp(pos.X, 80f, (Main.maxTilesX - 5) * 16f);
                pos.Y = MathHelper.Clamp(pos.Y, 80f, (Main.maxTilesY - 5) * 16f);
                NPC.SpawnBoss((int)pos.X, (int)pos.Y, tipoJefe, player.whoAmI);

                // EL PAGO: el consumible paga su unidad (los reutilizables
                // — como el muñeco del sastre — no pagan)
                if (item.consumable && item.stack > 0)
                {
                    item.stack--;
                    if (item.stack <= 0) item.TurnToAir();
                }
                if (player.whoAmI == Main.myPlayer)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, pos);
                return false;   // el uso de vanilla YA NO corre: el jefe ya nació
            }
            catch (System.Exception exRegistro)
            {
                global::AethonMod.Content.Systems.RegistroSistema.Fallo("InteriorInvoca", exRegistro);
                return false;
            }
        }
    }
}
