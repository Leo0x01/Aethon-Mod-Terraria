using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ReLogic.Content;
using System;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    /// <summary>
    /// v6.149 — EL OJO DEL PILAR DEL ETERNO (la letra: «el pilar del eterno
    /// en el mundo interior, debe usar a un minion como ojo, ya tiene la
    /// animacion y todo y debe siempre mirar al jugador»). Un minion-ojo
    /// cosmético que flota sobre EL PRISMA del Pilar del Eterno: parpadea
    /// con el ciclo de la casa (abierto → medio → cerrado → medio) y su
    /// IRIS sigue SIEMPRE al jugador — el ojo de Aethon volteado hacia
    /// adentro, mirando al portador cultivar recursos dentro de su propio
    /// cuerpo (el canon del Compendio: «el ojo del pilar mira la puerta
    /// del 120 más de lo que mira al portador… pero lo mira»).
    ///
    /// Nace solo en el mundo interior, pegado al pilar (lo spawnea el
    /// SubmundoGrimorio al generar y el PilarPrismaTile lo mantiene vivo).
    /// </summary>
    public class PilarOjoMinion : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/OjoEterno_Abierto";

        /// <summary>El parpadeo del ojo (el mapa del pilar de siempre).</summary>
        private static Asset<Texture2D>[] _ojos;

        public override void SetStaticDefaults()
        {
            Main.projPet[Type] = false;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = false;
            Projectile.netImportant = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.aiStyle = -1;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            // EL OJO VIVE SOLO EN EL MUNDO INTERIOR, sobre EL PILAR
            if (!Systems.SubmundoGrimorio.Activo) { Projectile.Kill(); return; }
            int tipoPilar = ModContent.TileType<Tiles.PilarPrismaTile>();
            bool pilarVive = false;
            Vector2 posPilar = Vector2.Zero;
            // el pilar está en (PlazaX, LlanoY): buscarlo por la región del llano
            int cx = 400, cy = 140;   // SubmundoGrimorio.PlazaX/LlanoY (const privadas: las de la casa)
            for (int ty = cy - 6; ty <= cy + 1 && !pilarVive; ty++)
                for (int tx = cx - 2; tx <= cx + 2; tx++)
                {
                    Tile tl = Main.tile[tx, ty];
                    if (tl != null && tl.HasTile && tl.TileType == tipoPilar)
                    {
                        pilarVive = true;
                        // la punta del pilar: topLeft + el prisma (1.5 tiles arriba del centro del capitel)
                        posPilar = new Vector2((cx + 0.5f) * 16f, (ty) * 16f - 26f);
                        break;
                    }
                }
            if (!pilarVive) { Projectile.Kill(); return; }

            // LA FLOTACIÓN VIVA: un vaivén suave sobre el prisma (±3px)
            float t = Main.GlobalTimeWrappedHourly;
            Vector2 ancla = posPilar + new Vector2(0f, -8f + MathF.Sin(t * 1.1f) * 3f);
            Projectile.Center = ancla;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 90;
            Projectile.spriteDirection = 1;

            // LA LUZ del ojo (el faro del pilar: violeta con el latido)
            float latido = 0.5f + 0.5f * MathF.Sin(t * 2.2f);
            Lighting.AddLight(Projectile.Center, 0.38f + 0.14f * latido, 0.22f + 0.08f * latido, 0.60f + 0.16f * latido);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // EL PARPADEO (el mapa del pilar: abierto 55 % · medio 13 % · cerrado 22 % · medio 10 %)
            float t = Main.GlobalTimeWrappedHourly + Projectile.whoAmI * 0.7f;
            float fase = (t * 0.55f) % 1f;
            string estado = fase < 0.55f ? "Abierto"
                : fase < 0.68f ? "Medio" : "Cerrado";
            // el cierre medio en la vuelta (cerrado → medio → abierto)
            if (fase >= 0.90f) estado = "Medio";

            _ojos ??= new Asset<Texture2D>[]
            {
                ModContent.Request<Texture2D>("AethonMod/Content/Projectiles/OjoEterno_Abierto"),
                ModContent.Request<Texture2D>("AethonMod/Content/Projectiles/OjoEterno_Medio"),
                ModContent.Request<Texture2D>("AethonMod/Content/Projectiles/OjoEterno_Cerrado"),
            };
            Texture2D tex = estado switch
            {
                "Medio" => _ojos[1].Value,
                "Cerrado" => _ojos[2].Value,
                _ => _ojos[0].Value,
            };

            // EL IRIS MIRA SIEMPRE AL JUGADOR (la letra): el desplazamiento
            // del iris dentro del ojo, hacia el jugador más cercano.
            Vector2 mirada = Vector2.Zero;
            Player p = Main.player[Main.myPlayer];
            if (p != null && p.active && !p.dead)
            {
                Vector2 dir = p.Center - Projectile.Center;
                if (dir.LengthSquared() > 4f)
                {
                    dir.Normalize();
                    mirada = dir * 3.2f;   // el iris se desplaza HACIA el jugador
                }
            }

            Vector2 pos = Projectile.Center - Main.screenPosition;
            Color luz = Lighting.GetColor(Projectile.Center.ToTileCoordinates());

            // EL HALO del ojo (aditivo, tenue: la lectura del pilar)
            try
            {
                global::AethonMod.Content.VFX.VFXCore.AbrirAdditive();
                Main.spriteBatch.Draw(tex, pos, null,
                    new Color(150, 90, 230) * 0.28f, 0f,
                    new Vector2(tex.Width, tex.Height) * 0.5f,
                    1.25f, SpriteEffects.None, 0f);
                global::AethonMod.Content.VFX.VFXCore.CerrarLoteSiAbierto();
            }
            catch { global::AethonMod.Content.VFX.VFXCore.CerrarLoteSiAbierto(); }

            // EL OJO (en el lote del pase de proyectiles)
            Main.spriteBatch.Draw(tex, pos + mirada, null, luz, 0f,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                0.80f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
