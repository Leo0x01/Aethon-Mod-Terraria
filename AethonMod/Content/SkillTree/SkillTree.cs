using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using AethonMod.Content.SkillTree.Entities;

namespace AethonMod.Content.SkillTree.RPGModule
{
    public class SkillTree
    {
        public ClassNode ActiveClass;
        public NodeList nodeList;

        public int GetStats(Stat stat)
        {
            int value = 0;
            if (nodeList == null) return 0;
            foreach (StatNode node in nodeList.GetStatsList)
            {
                if (node.GetStatType == stat)
                    value += (int)(node.GetValue * node.GetLevel);
            }
            foreach (LimitBreakNode node in nodeList.GetLBList)
            {
                value += (int)(node.GetValue * node.GetLevel);
            }
            return value;
        }

        private float CalcDamage(List<DamageNode> _list, bool flat)
        {
            float value = 0;
            if (_list == null) return 0;
            foreach (DamageNode node in _list)
            {
                if (node.GetFlat == flat)
                    value += node.GetValue* node.GetLevel;
            }
            return value;
        }

        private float CalcSpeed(List<SpeedNode> _list)
        {
            float value = 0;
            if (_list == null) return 0;
            foreach (SpeedNode node in _list)
            {
                value += node.GetValue * node.GetLevel;
            }
            return value;
        }

        private float CalcLeech(List<LeechNode> _list,LeechType _type)
        {
            float value = 0;
            if (_list == null) return 0;
            foreach (LeechNode node in _list)
            {
                if (node.GetLeechType == LeechType.Both || node.GetLeechType == _type)
                    value += node.GetValue * node.GetLevel;
            }
            return value;
        }

       public int GetSummonSlot()
        {
            int slot = 0;
            if (ActiveClass == null)
                return 0;
            var charList = JsonCharacterClass.GetJsonCharList;
            if (charList == null) return 0;
            try
            {
                slot = charList.GetClass(ActiveClass.GetClassType).Summons;
            }
            catch { /* defensive: never throw on gameplay path */ }
            return slot;
        }

        private float GetClassDamage(DamageType _type)
        {
            float value = 1;
            RPGPlayer pEntity = Main.player[Main.myPlayer].GetModPlayer<RPGPlayer>();
            if (ActiveClass == null)
            {
                return 1;
            }
            var charList = JsonCharacterClass.GetJsonCharList;
            if (charList == null) return 1;
            JsonChrClass actualClass;
            try
            {
                actualClass = charList.GetClass(ActiveClass.GetClassType);
            }
            catch { return 1; }
            value *= 1+actualClass.Damage[(int)_type];
            if (_type == DamageType.Ranged)
            {
                if (false)
                    value *= 1+actualClass.Damage[5];
                if(false && !false)
                    value *= 1+actualClass.Damage[6];
            }
            return value;
        }
        public float GetDamageMult(DamageType _type)
        {
            float value = 0;
            if (nodeList == null) return 0;

            value += CalcDamage(nodeList.GetDamageList(_type), false);
            value += GetClassDamage(_type);
            return value;
        }
        public int GetDamageFlat(DamageType _type)
        {
            float value = 0;
            if (nodeList == null) return 0;

            value += CalcDamage(nodeList.GetDamageList(_type), true);

            return (int)value;
        }
        public float GetDamageSpeed(DamageType _type)
        {
            float value = 0;
            if (nodeList == null) return 0;

            value += CalcDamage(nodeList.GetDamageList(_type), true);

            return value;
        }

        public float GetLeech(LeechType _leechType)
        {
            float value = 0;
            if (nodeList == null) return 0;

            value += CalcLeech(nodeList.GetLeech, _leechType);

            return value;
        }

        public bool HavePerk(Perk _perk)
        {
            if (nodeList == null) return false;
            List<PerkNode> list = nodeList.GetPerks;
            if (list == null) return false;
            for (int i = 0;i< list.Count; i++)
            {
                if (list[i].GetPerk == _perk && list[i].GetEnable)
                    return true;
            }
            return false;
        }

        public bool IsLimitBreak()
        {
            if (nodeList == null) return false;
            List<LimitBreakNode> list = nodeList.GetLBList;
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].GetEnable)
                    return true;
            }
            return false;
        }

        public bool HaveImmunity(Immunity _immunity)
        {
            if (nodeList == null) return false;
            List<ImmunityNode> list = nodeList.GetImmunities;
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].GetImmunity == _immunity && list[i].GetEnable)
                    return true;
            }
            return false;
        }

        public void  ResetConnection()
        {
            if (nodeList == null) return;
            int count = nodeList.nodeList.Count;
            for (int i = 0;i< count; i++)
            {
                nodeList.nodeList[i].connectedNeighboor = new List<NodeParent>();
            }
        }

        public SkillTree()
        {
            nodeList = new NodeList();

            // CRITICAL DEFENSIVE: this constructor is invoked from ModPlayer.LoadData
            // (ShardPlayer.LoadData + RPGPlayer.LoadData). If it throws, tModLoader
            // marks the whole player save as failed ("UnknownError") and the user
            // loses access to their character. We must NEVER throw here. Any failure
            // during node parsing is swallowed, leaving a (possibly empty but valid)
            // nodeList that Init() and the rest of the game can safely handle.
            JsonNodeList NodeSaved = JsonSkillTree.GetJsonNodeList;
            if (NodeSaved == null || NodeSaved.jsonList == null)
            {
                return;
            }

            NodeType nodeT;
            ClassType classT;
            DamageType damageT;
            LeechType leechT;
            Immunity immunityT;
            Stat StatT;
            Perk perkT;

            foreach (JsonNode actualNode in NodeSaved.jsonList)
            {
                try
                {
                    nodeT = (NodeType)Enum.Parse(typeof(NodeType), actualNode.baseType);
                    switch (nodeT)
                    {
                        case (NodeType.Damage):
                            damageT = (DamageType)Enum.Parse(typeof(DamageType), actualNode.specificType);
                            nodeList.AddNode(new DamageNode(damageT, actualNode.flatDamage, NodeType.Damage, actualNode.unlocked, actualNode.valuePerLevel, actualNode.levelRequirement, actualNode.maxLevel, actualNode.pointsPerLevel, actualNode.ascended));
                            break;
                        case (NodeType.Class):
                            classT = (ClassType)Enum.Parse(typeof(ClassType), actualNode.specificType);
                            nodeList.AddNode(new ClassNode(classT, NodeType.Class, actualNode.unlocked, actualNode.valuePerLevel, actualNode.levelRequirement, 1, actualNode.pointsPerLevel, actualNode.ascended));
                            break;
                        case (NodeType.Speed):
                            damageT = (DamageType)Enum.Parse(typeof(DamageType), actualNode.specificType);
                            nodeList.AddNode(new SpeedNode(damageT, NodeType.Speed, actualNode.unlocked, actualNode.valuePerLevel, actualNode.levelRequirement, actualNode.maxLevel, actualNode.pointsPerLevel, actualNode.ascended));
                            break;
                        case (NodeType.Immunity):
                            immunityT = (Immunity)Enum.Parse(typeof(Immunity), actualNode.specificType);
                            nodeList.AddNode(new ImmunityNode(immunityT, NodeType.Immunity, actualNode.unlocked, actualNode.valuePerLevel, actualNode.levelRequirement, actualNode.maxLevel, actualNode.pointsPerLevel, actualNode.ascended));
                            break;
                        case (NodeType.Leech):
                            leechT = (LeechType)Enum.Parse(typeof(LeechType), actualNode.specificType);
                            nodeList.AddNode(new LeechNode(leechT, NodeType.Leech, actualNode.unlocked, actualNode.valuePerLevel, actualNode.levelRequirement, actualNode.maxLevel, actualNode.pointsPerLevel, actualNode.ascended));
                            break;
                        case (NodeType.Perk):
                            perkT = (Perk)Enum.Parse(typeof(Perk), actualNode.specificType);
                            nodeList.AddNode(new PerkNode(perkT, NodeType.Perk, actualNode.unlocked, actualNode.valuePerLevel, actualNode.levelRequirement, actualNode.maxLevel, actualNode.pointsPerLevel, actualNode.ascended));
                            break;
                        case (NodeType.Stats):
                            StatT = (Stat)Enum.Parse(typeof(Stat), actualNode.specificType);
                            nodeList.AddNode(new StatNode(StatT, actualNode.flatDamage, NodeType.Stats, actualNode.unlocked, actualNode.valuePerLevel, actualNode.levelRequirement, actualNode.maxLevel, actualNode.pointsPerLevel, actualNode.ascended));
                            break;
                        case (NodeType.LimitBreak):
                            nodeList.AddNode(new LimitBreakNode(actualNode.specificType, NodeType.LimitBreak, actualNode.unlocked, actualNode.valuePerLevel, actualNode.levelRequirement, actualNode.maxLevel, actualNode.pointsPerLevel, actualNode.ascended));
                            break;
                    }

                    int lastIdx = nodeList.nodeList.Count - 1;
                    if (lastIdx >= 0)
                        nodeList.nodeList[lastIdx].menuPos = new Vector2(actualNode.posX, actualNode.posY);
                }
                catch
                {
                    // Skip a single bad node rather than corrupting the player save.
                }
            }

            // Wire neighbors — index-safe, never throw.
            int nodeCount = nodeList.nodeList.Count;
            var jsonList = NodeSaved.jsonList;
            for (int i = 0; i < jsonList.Length && i < nodeCount; i++)
            {
                try
                {
                    JsonNode actualNode = jsonList[i];
                    if (actualNode.neigthboorlist == null) continue;
                    foreach (int nbID in actualNode.neigthboorlist)
                    {
                        if (nbID >= 0 && nbID < nodeCount)
                            nodeList.nodeList[i].AddNeighboor(nodeList.nodeList[nbID]);
                    }
                }
                catch { /* skip bad neighbor wiring */ }
            }
        }


        public readonly static int SKILLTREEVERSION = 2;
        public void Init()
        {
            // CRITICAL DEFENSIVE: Init() is called from ModPlayer.LoadData. Never throw.
            try
            {
                NodeParent.ResetID();
                if (nodeList != null && nodeList.nodeList.Count > 0)
                    nodeList.nodeList[0].Upgrade();
            }
            catch
            {
                // Swallow — an empty/partial tree must not corrupt the player save.
            }
        }
    }
}
