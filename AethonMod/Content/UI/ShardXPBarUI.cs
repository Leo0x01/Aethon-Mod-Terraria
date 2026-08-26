using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.GameContent.UI.Elements;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// Barra de XP del Fragmento Génesis que aparece en pantalla.
    /// Muestra el nivel actual, la barra de progreso hacia el siguiente nivel,
    /// y la XP actual / XP necesaria.
    /// </summary>
    public class ShardXPBarUI : UIState
    {
        public bool IsVisible;
        private UIText _levelText = null!;
        private UIText _xpText = null!;
        private UIPanel _barBg = null!;
        private UIPanel _barFill = null!;

        public override void OnInitialize()
        {
            // Fondo de la barra
            _barBg = new UIPanel();
            _barBg.Width.Set(200f, 0f);
            _barBg.Height.Set(20f, 0f);
            _barBg.Top.Set(60f, 0f);
            _barBg.Left.Set(-210f, 1f);
            _barBg.BackgroundColor = new Color(20, 15, 40, 200);
            _barBg.BorderColor = new Color(245, 196, 81, 150);
            Append(_barBg);

            // Relleno de la barra (escala con XP)
            _barFill = new UIPanel();
            _barFill.Width.Set(0f, 0f);
            _barFill.Height.Set(16f, 0f);
            _barFill.Top.Set(2f, 0f);
            _barFill.Left.Set(2f, 0f);
            _barFill.BackgroundColor = new Color(245, 196, 81, 220);
            _barFill.BorderColor = Color.Transparent;
            _barBg.Append(_barFill);

            // Texto de nivel
            _levelText = new UIText("Fragmento Lv 1", 0.8f);
            _levelText.Width.Set(200f, 0f);
            _levelText.Height.Set(16f, 0f);
            _levelText.Top.Set(42f, 0f);
            _levelText.Left.Set(-210f, 1f);
            _levelText.TextColor = new Color(245, 196, 81);
            Append(_levelText);

            // Texto de XP
            _xpText = new UIText("0 / 80 XP", 0.7f);
            _xpText.Width.Set(200f, 0f);
            _xpText.Height.Set(14f, 0f);
            _xpText.Top.Set(82f, 0f);
            _xpText.Left.Set(-210f, 1f);
            _xpText.TextColor = new Color(179, 136, 255);
            Append(_xpText);
        }

        public override void Update(GameTime gameTime)
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted)
            {
                IsVisible = false;
                return;
            }
            IsVisible = true;

            // Actualizar textos
            _levelText.SetText($"Fragmento Lv {sp.ShardLevel}");
            int xpNeeded = sp.XPForNextLevel();
            _xpText.SetText($"{sp.ShardXP} / {xpNeeded} XP");

            // Actualizar barra de progreso
            float pct = xpNeeded > 0 ? (float)sp.ShardXP / xpNeeded : 0f;
            pct = System.Math.Clamp(pct, 0f, 1f);
            _barFill.Width.Set(System.Math.Max(2f, 196f * pct), 0f);

            base.Update(gameTime);
        }
    }

    /// <summary>
    /// Tarjetas flotantes de elección de rama.
    /// Cuando el fragmento está listo para imprimirse (tras suficientes kills),
    /// aparecen 3 tarjetas flotantes sobre el jugador: Distancia, Cuerpo a Cuerpo, Artes Mágicas.
    /// El jugador hace clic en una para elegir su rama.
    /// </summary>
    public class BranchChoiceUI : UIState
    {
        public bool IsVisible;
        private UIText _titleText = null!;
        private List<BranchCard> _cards = new();

        public override void OnInitialize()
        {
            // Titulo
            _titleText = new UIText("Elige la rama de tu Fragmento Génesis", 1.1f);
            _titleText.HAlign = 0.5f;
            _titleText.VAlign = 0.25f;
            _titleText.TextColor = new Color(245, 196, 81);
            Append(_titleText);

            // 3 tarjetas
            float cardWidth = 160f;
            float gap = 20f;
            float totalWidth = cardWidth * 3 + gap * 2;
            float startX = (Main.screenWidth - totalWidth) / 2f;

            _cards.Clear();
            var branches = new (string name, string desc, Color color, BranchType type)[]
            {
                ("DISTANCIA", "Arcos, munición\ny armas arrojadizas\n\nLumina, la Arcoestelar", new Color(245, 196, 81), BranchType.Distance),
                ("CUERPO A CUERPO", "Espadas, lanzas\ny yoyos\n\nSolbrand, Filo del Alba", new Color(255, 154, 60), BranchType.Melee),
                ("ARTES MÁGICAS", "Magia + Invocación\nfusionadas\n\nGrimorio del Eterno", new Color(179, 136, 255), BranchType.Magic),
            };

            for (int i = 0; i < 3; i++)
            {
                var card = new BranchCard(branches[i].name, branches[i].desc, branches[i].color, branches[i].type);
                card.Width.Set(cardWidth, 0f);
                card.Height.Set(200f, 0f);
                card.HAlign = 0.5f;
                card.VAlign = 0.4f;
                card.Left.Set((i - 1) * (cardWidth + gap), 0f);
                Append(card);
                _cards.Add(card);
            }
        }

        public void Show()
        {
            IsVisible = true;
        }

        public void Hide()
        {
            IsVisible = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Repositionar tarjetas centradas en pantalla
            float cardWidth = 160f;
            float gap = 20f;
            float totalWidth = cardWidth * 3 + gap * 2;
            float startX = (Main.screenWidth - totalWidth) / 2f;

            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].Left.Set(startX + i * (cardWidth + gap), 0f);
            }
        }
    }

    /// <summary>
    /// Tarjeta individual de elección de rama.
    /// Click para seleccionar esa rama.
    /// </summary>
    public class BranchCard : UIPanel
    {
        private string _name;
        private string _desc;
        private Color _color;
        private BranchType _type;

        public BranchCard(string name, string desc, Color color, BranchType type)
        {
            _name = name;
            _desc = desc;
            _color = color;
            _type = type;
        }

        public override void OnInitialize()
        {
            BackgroundColor = new Color(20, 15, 40, 230);
            BorderColor = _color;

            var nameText = new UIText(_name, 0.9f);
            nameText.HAlign = 0.5f;
            nameText.Top.Set(15f, 0f);
            nameText.TextColor = _color;
            Append(nameText);

            var descText = new UIText(_desc, 0.75f);
            descText.HAlign = 0.5f;
            descText.Top.Set(60f, 0f);
            descText.TextColor = new Color(200, 200, 220);
            Append(descText);

            var chooseBtn = new UITextPanel<string>("Elegir");
            chooseBtn.Width.Set(100f, 0f);
            chooseBtn.Height.Set(28f, 0f);
            chooseBtn.HAlign = 0.5f;
            chooseBtn.Top.Set(155f, 0f);
            chooseBtn.BackgroundColor = new Color(_color.R / 3, _color.G / 3, _color.B / 3, 200);
            chooseBtn.BorderColor = _color;
            chooseBtn.OnLeftClick += (evt, el) =>
            {
                var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
                if (sp == null) return;
                // Forzar la elección del jugador (no aleatorio).
                sp.ActiveBranch = _type;
                sp.SubForm = _type switch
                {
                    BranchType.Distance => WeaponSubForm.Bow,
                    BranchType.Melee => WeaponSubForm.Sword,
                    BranchType.Magic => WeaponSubForm.Spellbook,
                    _ => WeaponSubForm.None,
                };
                if (sp.SkillTreeSeed == 0)
                    sp.SkillTreeSeed = Main.rand.Next(1, 1_000_000);

                Main.NewText($"Has elegido la rama de {_name}!", _color);
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item4);

                // Reemplazar el item del inventario.
                ReplaceShardWithWeapon(Main.LocalPlayer, _type);

                // Cerrar la UI de elección.
                var ui = ModContent.GetInstance<UISystem>();
                ui?.BranchChoiceUI?.Hide();
            };
            Append(chooseBtn);
        }

        private void ReplaceShardWithWeapon(Player player, BranchType branch)
        {
            int weaponType = branch switch
            {
                BranchType.Distance => ModContent.ItemType<Weapons.LuminaStarbow>(),
                BranchType.Melee => ModContent.ItemType<Weapons.SolbrandEdge>(),
                BranchType.Magic => ModContent.ItemType<Weapons.GrimoireEternal>(),
                _ => ModContent.ItemType<Items.GenesisShard>(),
            };
            for (int i = 0; i < 58; i++)
            {
                if (player.inventory[i].type == ModContent.ItemType<Items.GenesisShard>())
                {
                    int prefix = player.inventory[i].prefix;
                    player.inventory[i].SetDefaults(weaponType);
                    player.inventory[i].prefix = (byte)prefix;
                    break;
                }
            }
        }
    }
}
