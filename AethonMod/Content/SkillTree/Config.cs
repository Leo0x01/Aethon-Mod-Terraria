namespace AethonMod.Content.SkillTree
{
    public static class AnRPGConfig
    {
        public static VisualConfig vConfig = new VisualConfig();
        public static GamePlayConfig gpConfig = new GamePlayConfig();
        public static NPCConfigData NPCConfig = new NPCConfigData();

        public class VisualConfig { public float UI_Scale = 1f; }
        public class GamePlayConfig { public bool RPGPlayer = true; public bool UseCustomSkillTree = false; public bool ItemTree = false; }
        public class NPCConfigData { public bool Enabled = true; }
    }
}
