using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using AethonMod.Content.SkillTree.RPGModule;
using AethonMod.Content.Players;
namespace AethonMod.Content.SkillTree.RPGModule
{
    public class ClassNode : Node
    {
        ClassType classType;
        public ClassType GetClassType
        {
            get
            {
                return classType;
            }
        }
        public ClassNode(ClassType _classType, NodeType _type, bool _unlocked = false, float _value = 1, int _levelrequirement = 0, int _maxLevel = 1, int _pointsPerLevel = 1, bool _ascended = false) : base(_type, _unlocked, _value, _levelrequirement, _maxLevel, _pointsPerLevel, _ascended)
        {
            classType = _classType;
        }

        public void loadingUpgrade()
        {
            base.Upgrade();
        }

        public override void Upgrade()
        {
            base.Upgrade();
            UpdateClass();
        }

        public void Disable(ShardPlayer p)
        {
            if (p.GetskillTree.ActiveClass == this)
                p.GetskillTree.ActiveClass = null;
            enable = false;
        }

        public void Disable()
        {
            if (Main.player[Main.myPlayer].GetModPlayer<ShardPlayer>().GetskillTree.ActiveClass == this)
                Main.player[Main.myPlayer].GetModPlayer<ShardPlayer>().GetskillTree.ActiveClass = null;
            enable = false;
        }

        public void UpdateClass()
        {
            NodeParent _node;
            ClassType Active = ClassType.Hobo;
            if (Main.player[Main.myPlayer].GetModPlayer<ShardPlayer>().GetskillTree.ActiveClass != null)
                Active = Main.player[Main.myPlayer].GetModPlayer<ShardPlayer>().GetskillTree.ActiveClass.GetClassType;
            for (int i = 0; i < Main.player[Main.myPlayer].GetModPlayer<ShardPlayer>().GetskillTree.nodeList.nodeList.Count; i++)
            {
                _node = Main.player[Main.myPlayer].GetModPlayer<ShardPlayer>().GetskillTree.nodeList.nodeList[i];
                if (_node.GetNodeType == NodeType.Class)
                {
                    ClassNode classNode = (ClassNode)_node.GetNode;
                    if (Active != classNode.GetClassType && classNode.GetActivate)
                    {
                        classNode.Disable();
                    }

                }
            }
        }

        public override void ToggleEnable()
        {
            base.ToggleEnable();
            
            if (enable)
                Main.player[Main.myPlayer].GetModPlayer<ShardPlayer>().GetskillTree.ActiveClass = this;
            else
            {
                Main.player[Main.myPlayer].GetModPlayer<ShardPlayer>().GetskillTree.ActiveClass = null;
            }
            
            ShardPlayer player = Main.player[Main.myPlayer].GetModPlayer<ShardPlayer>();
            

            UpdateClass();
            if (Main.netMode == NetmodeID.MultiplayerClient)
                player.SendClientChanges(player);
        }


    }
}
