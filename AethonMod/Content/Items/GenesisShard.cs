using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// El Fragmento Genesis — item central del mod.
    /// Al usarlo (clic derecho): muestra un mensaje guia.
    /// Si el jugador ya mato suficientes enemigos, abre la UI de eleccion de rama.
    /// </summary>
    public class GenesisShard : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.damage = 8;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28;
            Item.height = 28;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item4;
            Item.noMelee = true;
            Item.autoReuse = false;
        }

        public override bool? UseItem(Player player)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return null;

            if (!sp.IsImprinted)
            {
                int totalKills = sp.DistanceKills + sp.MeleeKills + sp.MagicKills;
                int threshold = Players.ShardPlayer.KILLS_TO_IMPRINT;

                if (totalKills < threshold)
                {
                    Main.NewText($"El Fragmento Genesis aun no tiene forma. Mata {threshold - totalKills} enemigos mas para despertarlo.",
                        new Color(180, 160, 220));
                }
                else
                {
                    // Mostrar la UI de eleccion de rama.
                    var ui = ModContent.GetInstance<Content.Systems.UISystem>();
                    if (ui != null && ui.BranchChoiceUI != null)
                    {
                        ui.BranchChoiceUI.Show();
                        Main.NewText("Tu Fragmento Genesis esta listo. Elige tu rama!", new Color(245, 196, 81));
                    }
                    else
                    {
                        // Fallback: si la UI no carga, elegir automaticamente la rama con mas kills.
                        Main.NewText("El Fragmento Genesis despierta!", new Color(245, 196, 81));
                        if (sp.DistanceKills >= sp.MeleeKills && sp.DistanceKills >= sp.MagicKills)
                        {
                            sp.ActiveBranch = Players.BranchType.Distance;
                            sp.SubForm = Players.WeaponSubForm.Bow;
                        }
                        else if (sp.MeleeKills >= sp.MagicKills)
                        {
                            sp.ActiveBranch = Players.BranchType.Melee;
                            sp.SubForm = Players.WeaponSubForm.Sword;
                        }
                        else
                        {
                            sp.ActiveBranch = Players.BranchType.Magic;
                            sp.SubForm = Players.WeaponSubForm.Spellbook;
                        }
                        if (sp.SkillTreeSeed == 0)
                            sp.SkillTreeSeed = Main.rand.Next(1, 1_000_000);
                        ReplaceShard(player, sp.ActiveBranch);
                    }
                }
                return true;
            }

            // Si ya esta imprintado, mostrar info del nivel.
            Main.NewText($"Fragmento Genesis — Nivel {sp.ShardLevel} | XP: {sp.ShardXP}/{sp.XPForNextLevel()} | Rama: {sp.ActiveBranch}",
                new Color(245, 196, 81));
            return true;
        }

        private void ReplaceShard(Player player, Players.BranchType branch)
        {
            int weaponType = branch switch
            {
                Players.BranchType.Distance => ModContent.ItemType<Weapons.LuminaStarbow>(),
                Players.BranchType.Melee => ModContent.ItemType<Weapons.SolbrandEdge>(),
                Players.BranchType.Magic => ModContent.ItemType<Weapons.GrimoireEternal>(),
                _ => ModContent.ItemType<GenesisShard>(),
            };
            for (int i = 0; i < 58; i++)
            {
                if (player.inventory[i].type == ModContent.ItemType<GenesisShard>())
                {
                    int prefix = player.inventory[i].prefix;
                    player.inventory[i].SetDefaults(weaponType);
                    player.inventory[i].prefix = (byte)prefix;
                    Main.NewText($"El Fragmento Genesis se ha transformado en {player.inventory[i].Name}!",
                        new Color(245, 196, 81));
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.Center);
                    break;
                }
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 1)
                .Register();
        }
    }
}
