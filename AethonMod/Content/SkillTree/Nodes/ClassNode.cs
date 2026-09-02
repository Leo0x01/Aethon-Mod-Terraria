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
        public ClassNode(ClassType _classType, NodeType _type, bool _unlocked = false, float _value = 1, int _levelrequirement = 0, int _maxLevel = 1, int _pointsPerLevel = 1, bool _ascended = false) : base(_type, _unlocked, _value, _levelrequirement, _maxLevel, _pointsPerLevel,_ascended)
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
            if (p == null || p.GetskillTree == null) { enable = false; return; }
            if (p.GetskillTree.ActiveClass == this)
                p.GetskillTree.ActiveClass = null;
            enable = false;
        }

        public void Disable()
        {
            ShardPlayer sp;
            try { sp = Main.player[Main.myPlayer].GetModPlayer<ShardPlayer>(); }
            catch { enable = false; return; }
            if (sp == null || sp.GetskillTree == null) { enable = false; return; }
            if (sp.GetskillTree.ActiveClass == this)
                sp.GetskillTree.ActiveClass = null;
            enable = false;
        }

        public void UpdateClass()
        {
            // Defensive: never NPE on the gameplay path. If GetskillTree isn't ready, bail.
            ShardPlayer sp;
            try { sp = Main.player[Main.myPlayer].GetModPlayer<ShardPlayer>(); }
            catch { return; }
            if (sp == null || sp.GetskillTree == null || sp.GetskillTree.nodeList == null)
                return;

            NodeParent _node;
            ClassType Active = ClassType.Hobo;
            if (sp.GetskillTree.ActiveClass != null)
                Active = sp.GetskillTree.ActiveClass.GetClassType;
            for (int i = 0; i < sp.GetskillTree.nodeList.nodeList.Count; i++)
            {
                _node = sp.GetskillTree.nodeList.nodeList[i];
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

            ShardPlayer player;
            try { player = Main.player[Main.myPlayer].GetModPlayer<ShardPlayer>(); }
            catch { return; }
            if (player == null || player.GetskillTree == null)
                return;

            if (enable)
                player.GetskillTree.ActiveClass = this;
            else
            {
                player.GetskillTree.ActiveClass = null;
            }

            UpdateClass();
            if (Main.netMode == NetmodeID.MultiplayerClient)
                player.SendClientChanges(player);
        }


    }
}
