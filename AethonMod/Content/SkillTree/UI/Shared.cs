using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System;
using Terraria.ModLoader;
using AethonMod.Content.SkillTree.RPGModule;
//using AethonMod.Items;

namespace AethonMod.Content.SkillTree.UI
{
    public class Skill : UIElement
    {
        private Texture2D _texture;
        public Color color = Color.White;
        public Skill(Texture2D texture)
        {
            _texture = texture;
            Width.Set(_texture.Width * SkillTreeUi.Instance.sizeMultplier, 0f);
            Height.Set(_texture.Height * SkillTreeUi.Instance.sizeMultplier, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();

            spriteBatch.Draw(_texture, dimensions.Position(), null, color, 0f, Vector2.Zero, SkillTreeUi.Instance.sizeMultplier, SpriteEffects.None, 0f);
        }
    }
    // REMOVED: public class ItemSkill : UIElement
    // } REMOVED

    public class SkillText : UIText
    {
        public NodeParent node;

        public SkillText(string text, NodeParent node, float textScale = 1, bool large = false) : base(text, textScale, large)
        {
            this.node = node;
        }
    }

    // REMOVED: public class ItemSkillText : UIText
    // } REMOVED

    // REMOVED: public class ItemSkillPanel : UIPanel
    // } REMOVED


    public class SkillPanel : UIPanel
    {
        public NodeParent node;
        public Skill skill;
        public Vector2 basePos;
        private Texture2D _texture;
        public Color color = Color.White;
        public SkillPanel(Texture2D texture)
        {
            _texture = texture;
            Width.Set(_texture.Width * SkillTreeUi.Instance.sizeMultplier, 0f);
            Height.Set(_texture.Height * SkillTreeUi.Instance.sizeMultplier, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();

            spriteBatch.Draw(_texture, dimensions.Position(), null, color, 0f, Vector2.Zero, SkillTreeUi.Instance.sizeMultplier, SpriteEffects.None, 0f);
        }
    }


    // REMOVED: public class ItemConnection : UIElement
    // } REMOVED

    public class Connection : UIElement
    {
        public NodeParent node;
        public NodeParent neighboor;
        private Texture2D texture = ModContent.Request<Microsoft.Xna.Framework.Graphics.Texture2D>("AnotherRpgMod/Textures/UI/Blank").Value;
        public Color color;
        public Vector2 basePos;
        public bool bg = false;
        public float m_rotation;

        public Connection(float rotation, float distance, float height)
        {
            Width.Set(distance * SkillTreeUi.Instance.sizeMultplier, 0f);
            Height.Set(height * SkillTreeUi.Instance.sizeMultplier, 0f);
            m_rotation = rotation;
            this.color = Color.White;
        }


        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            Point point1 = new Point((int)dimensions.X, (int)dimensions.Y);
            int width = (int)Math.Ceiling(dimensions.Width);
            int height = (int)Math.Ceiling(dimensions.Height);

            spriteBatch.Draw(texture, dimensions.Position(), new Rectangle(point1.X, point1.Y, width, height), color, m_rotation, bg ? new Vector2(0, 5) : new Vector2(0, 3), 1, SpriteEffects.None, 0f);
        }
    }
}
