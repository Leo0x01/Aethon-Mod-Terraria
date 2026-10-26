// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

// ============================================================================
// PORTADO VERBATIM (Y PODADO) DE Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/AethonMod.Content.GoozmaPort) — Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz.
// Licencia original: GPL-3.0. Copia de investigación para AethonMod;
// pendiente la re-licencia/decisión del usuario.
//
// PORT NOTE (desviación documentada): el AssetDirectory original es el
// registro de TODO el mod (armas, alas, tintas…); aquí sobreviven SOLO los
// miembros que el puerto del jefe Goozma usa (verificado por grep del
// closure). El AssetPath apunta a la carpeta del puerto en AethonMod.
// ============================================================================
using System.Collections.Generic;
using AethonMod.Content.GoozmaPort.Common.Utilities;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.Audio;
using Terraria.ModLoader;

namespace AethonMod.Content.GoozmaPort;

public static class AssetDirectory
{
    public static readonly string AssetPath = "AethonMod/Content/GoozmaPort/Assets/";

    public static class Textures
    {
        public static readonly Asset<Texture2D> Sparkle = AssetUtilities.RequestImmediate<Texture2D>(AssetPath + "Textures/Extra/Sparkle");
        public static readonly Asset<Texture2D> ShockRing = AssetUtilities.RequestImmediate<Texture2D>(AssetPath + "Textures/Extra/ShockRing");

        public static readonly Asset<Texture2D>[] Noise = AssetUtilities.RequestArrayTotalImmediate<Texture2D>(AssetPath + "Textures/Extra/Noise_");
        public static readonly Asset<Texture2D>[] Space = AssetUtilities.RequestArrayTotalImmediate<Texture2D>(AssetPath + "Textures/Extra/Space_");
        public static readonly Asset<Texture2D>[] ColorMap = AssetUtilities.RequestArrayTotalImmediate<Texture2D>(AssetPath + "Textures/Extra/ColorMap_");
        public static readonly Asset<Texture2D>[] Glow = AssetUtilities.RequestArrayTotalImmediate<Texture2D>(AssetPath + "Textures/Extra/Glow_");

        public static readonly Asset<Texture2D> Balloons = AssetUtilities.RequestImmediate<Texture2D>(AssetPath + "Textures/Extra/Balloons");

        public static readonly Asset<Texture2D> ChromaticSoulEye = AssetUtilities.RequestImmediate<Texture2D>(AssetPath + "Textures/Extra/SoulEyes/ChromaticSoulEye");

        public static Dictionary<int, Asset<Texture2D>> FlyingSlime = new Dictionary<int, Asset<Texture2D>>();

        internal static readonly Asset<Texture2D> FrogParticle = AssetUtilities.RequestImmediate<Texture2D>(AssetPath + "Textures/Extra/FrogParticle");
        internal static readonly Asset<Texture2D> WideMoto = AssetUtilities.RequestImmediate<Texture2D>(AssetPath + "Textures/Extra/WideMoto");

        internal static readonly Asset<Texture2D> SantaHat = AssetUtilities.RequestImmediate<Texture2D>(AssetPath + "Textures/Extra/SantaHat");
        internal static readonly Asset<Texture2D> ElfHat = AssetUtilities.RequestImmediate<Texture2D>(AssetPath + "Textures/Extra/ElfHat");

        public static class Goozma
        {
            public static readonly string GoozmaPrefix = AssetPath + "Textures/NPCs/Bosses/GoozmaBoss/";

            #region Goozma
            public static readonly Asset<Texture2D> Sclera = AssetUtilities.RequestImmediate<Texture2D>(GoozmaPrefix + "GoozmaSclera");
            public static readonly Asset<Texture2D> Lightning = AssetUtilities.RequestImmediate<Texture2D>(GoozmaPrefix + "Lightning");
            public static readonly Asset<Texture2D> LightningGlow = AssetUtilities.RequestImmediate<Texture2D>(GoozmaPrefix + "LightningGlow");
            public static readonly Asset<Texture2D> LiquidTrail = AssetUtilities.RequestImmediate<Texture2D>(GoozmaPrefix + "LiquidTrail");
            #endregion

            #region Hallow Slime
            public static readonly Asset<Texture2D> PixieBeachBall = AssetUtilities.RequestImmediate<Texture2D>(GoozmaPrefix + "PixieBeachBall");
            public static readonly Asset<Texture2D> PixieHitMeSign = AssetUtilities.RequestImmediate<Texture2D>(GoozmaPrefix + "PixieHitMeSign");
            public static readonly Asset<Texture2D> PixieHitMeHand = AssetUtilities.RequestImmediate<Texture2D>(GoozmaPrefix + "PixieHitMeHand");
            #endregion

            #region Stellar Slime
            public static readonly Asset<Texture2D>[] SwirlingRocks = AssetUtilities.RequestArrayTotalImmediate<Texture2D>(AssetPath + "Textures/Extra/SwirlingRocks_");
            public static readonly Asset<Texture2D> ConstellationArea = AssetUtilities.RequestImmediate<Texture2D>(GoozmaPrefix + "ConstellationArea");
            public static readonly Asset<Texture2D> SpaceTrail = AssetUtilities.RequestImmediate<Texture2D>(GoozmaPrefix + "SpaceTrail");
            public static readonly Asset<Texture2D> SpaceTrailGlow = AssetUtilities.RequestImmediate<Texture2D>(GoozmaPrefix + "SpaceTrailGlow");

            public static readonly Asset<Texture2D> Wormhole = AssetUtilities.RequestImmediate<Texture2D>(GoozmaPrefix + "Wormhole");
            #endregion

            #region Goozmite
            public static readonly Asset<Texture2D> MicroTentacle = AssetUtilities.RequestImmediate<Texture2D>(GoozmaPrefix + "GoozmiteMicroTentacle");
            #endregion

            #region Phase 2
            public static readonly Asset<Texture2D>[] FusionRay = AssetUtilities.RequestArrayImmediate<Texture2D>(GoozmaPrefix + "Projectiles/FusionRay_", 4);
            #endregion
        }

        public static class SlimeMonsoon
        {
            public static readonly Asset<Texture2D> MapBG = AssetUtilities.RequestImmediate<Texture2D>(AssetPath + "Textures/SlimeMonsoonBG");
            public static readonly Asset<Texture2D> Lightning = AssetUtilities.RequestImmediate<Texture2D>(AssetPath + "Textures/Graphics/SlimeMonsoon/Lightning");
            public static readonly Asset<Texture2D> LightningGlow = AssetUtilities.RequestImmediate<Texture2D>(AssetPath + "Textures/Graphics/SlimeMonsoon/LightningGlow");
        }
    }

    public static class Music
    {
        #region Goozma
        public static readonly int ExiledGoozma1 = MusicLoader.GetMusicSlot(AssetPath + "Music/Goozma/ExiledGoozma1");
        public static readonly int ExiledGoozma2 = MusicLoader.GetMusicSlot(AssetPath + "Music/Goozma/ExiledGoozma2");

        public static readonly int JteohGoozma1 = MusicLoader.GetMusicSlot(AssetPath + "Music/Goozma/JteohGoozma1");
        public static readonly int JteohGoozma2 = MusicLoader.GetMusicSlot(AssetPath + "Music/Goozma/JteohGoozma2");
        #endregion

        #region Auric Souls
        public static readonly int GoozmaSoul = MusicLoader.GetMusicSlot(AssetPath + "Music/Souls/GoozmaSoul");
        #endregion
    }

    public static class Sounds
    {
        public static class Goozma
        {
            #region Goozma
            // General
            public static readonly SoundStyle SlimeAbsorb = new(AssetPath + "Sounds/Goozma/GoozmaSlimeAbsorb", 8) { MaxInstances = 0, Volume = 0.1f, PitchVariance = 0.2f };
            public static readonly SoundStyle Intro = new(AssetPath + "Sounds/Goozma/GoozmaIntro") { MaxInstances = 0, PlayOnlyIfFocused = true };
            public static readonly SoundStyle Awaken = new(AssetPath + "Sounds/Goozma/GoozmaAwaken") { MaxInstances = 0 };
            public static readonly SoundStyle Hurt = new(AssetPath + "Sounds/Goozma/GoozmaHurt", 1, 3) { MaxInstances = 0, PitchVariance = 0.1f };
            public static readonly SoundStyle SpawnSlime = new(AssetPath + "Sounds/Goozma/GoozmaSpawnSlime") { MaxInstances = 0 };
            public static readonly SoundStyle EyeAppear = new(AssetPath + "Sounds/Goozma/GoozmaEyeAppear") { MaxInstances = 0 };
            public static readonly SoundStyle Reform = new(AssetPath + "Sounds/Goozma/GoozmaReform") { MaxInstances = 0 };
            public static readonly SoundStyle Reawaken = new(AssetPath + "Sounds/Goozma/GoozmaReawaken") { MaxInstances = 0 };
            public static readonly SoundStyle Dash = new(AssetPath + "Sounds/Goozma/GoozmaDash", 1, 2) { MaxInstances = 0, PitchVariance = 0.15f };

            // Phase 2
            public static readonly SoundStyle Sizzle = new(AssetPath + "Sounds/Goozma/GoozmaSizzle") { MaxInstances = 0 };
            public static readonly SoundStyle WarbleLoop = new(AssetPath + "Sounds/Goozma/GoozmaDeepWarbleLoop") { MaxInstances = 0, IsLooped = true };
            public static readonly SoundStyle ShootLoop = new(AssetPath + "Sounds/Goozma/GoozmaShootLoop") { MaxInstances = 0, IsLooped = true };
            public static readonly SoundStyle SimmerLoop = new(AssetPath + "Sounds/Goozma/GoozmaSimmerLoop") { MaxInstances = 0, IsLooped = true };
            public static readonly SoundStyle FusionRayLoop = new(AssetPath + "Sounds/Goozma/GoozmaFusionRayLoop") { MaxInstances = 0, IsLooped = true };

            // Cutscene
            public static readonly SoundStyle DeathBuildup = new(AssetPath + "Sounds/Goozma/GoozmaDeathBuildup") { MaxInstances = 0 };
            public static readonly SoundStyle Pop = new(AssetPath + "Sounds/Goozma/GoozmaPop") { MaxInstances = 0 };
            public static readonly SoundStyle Explode = new(AssetPath + "Sounds/Goozma/GoozmaExplode") { MaxInstances = 0 };
            public static readonly SoundStyle EarRinging = new(AssetPath + "Sounds/Goozma/GoozmaEarRinging") { MaxInstances = 0 };
            public static readonly SoundStyle WindLoop = new(AssetPath + "Sounds/Goozma/GoozmaWindLoop") { MaxInstances = 0, IsLooped = true };
            #endregion

            #region Goozma Projectiles
            public static readonly SoundStyle SlimeShoot = new(AssetPath + "Sounds/Goozma/GoozmaSlimeShoot", 1, 3) { MaxInstances = 0, PitchVariance = 0.1f, Volume = 0.66f };
            public static readonly SoundStyle Dart = new(AssetPath + "Sounds/Goozma/GoozmaDartShoot", 1, 2) { MaxInstances = 0, PitchVariance = 0.2f };
            public static readonly SoundStyle Shot = new(AssetPath + "Sounds/Goozma/GoozmaShot", 1, 2) { MaxInstances = 0 };
            public static readonly SoundStyle RainbowShoot = new(AssetPath + "Sounds/Goozma/GoozmaRainbowShoot", 1, 2) { MaxInstances = 0, PitchVariance = 0.15f };
            public static readonly SoundStyle BombCharge = new(AssetPath + "Sounds/Goozma/GoozmaBombCharge") { MaxInstances = 0 };
            public static readonly SoundStyle BloatedBlastShoot = new(AssetPath + "Sounds/Goozma/GoozmaBloatedBlastShoot") { MaxInstances = 0 };
            public static readonly SoundStyle GoozmiteShoot = new(AssetPath + "Sounds/Goozma/GoozmiteShoot", 1, 2) { MaxInstances = 0 };
            public static readonly SoundStyle BigThunder = new(AssetPath + "Sounds/Goozma/GoozmaBigThunder") { MaxInstances = 0, PitchVariance = 0.15f };
            public static readonly SoundStyle SmallThunder = new(AssetPath + "Sounds/Goozma/GoozmaSmallThunder", 1, 3) { MaxInstances = 0, PitchVariance = 0.15f };
            #endregion

            public static readonly SoundStyle SlimeJump = new(AssetPath + "Sounds/Goozma/GoozmaSlimeJump", 1, 2) { MaxInstances = 0, PitchVariance = 0.1f };
        }

        public static class Goozmite
        {
            public static readonly SoundStyle Ambient = new(AssetPath + "Sounds/Goozma/Goozmite/GoozmiteAmbient", 1, 3) { MaxInstances = 0, Volume = 0.5f };
            public static readonly SoundStyle Death = new(AssetPath + "Sounds/Goozma/Goozmite/GoozmiteDeath", 1, 3) { MaxInstances = 0 };
            public static readonly SoundStyle Impact = new(AssetPath + "Sounds/Goozma/Goozmite/GoozmiteImpact", 1, 3) { MaxInstances = 0 };
        }

        public static class GoozmaMinions
        {
            #region Generic
            public static readonly SoundStyle SlimeSlam = new(AssetPath + "Sounds/Goozma/Slimes/GoozmaSlimeSlam", 1, 3) { MaxInstances = 0 };
            #endregion

            #region Crimson
            public static readonly SoundStyle CrimslimeTelegraph = new(AssetPath + "Sounds/Goozma/Slimes/CrimslimeTelegraph") { MaxInstances = 0 };
            #endregion

            #region Hallow
            public static readonly SoundStyle PrismDestroyerTelegraph = new(AssetPath + "Sounds/Goozma/Slimes/PrismDestroyerTelegraph") { MaxInstances = 0, PitchVariance = 0.1f };
            public static readonly SoundStyle PrismDestroyerExpand = new(AssetPath + "Sounds/Goozma/Slimes/PrismDestroyerExpand") { MaxInstances = 0, PitchVariance = 0.1f };
            public static readonly SoundStyle PixiePrismDestroyed = new(AssetPath + "Sounds/Goozma/Slimes/PixiePrismDestroyed") { MaxInstances = 0 };
            public static readonly SoundStyle PixieBallExplode = new(AssetPath + "Sounds/Goozma/Slimes/PixieBallExplode") { MaxInstances = 0 };
            public static readonly SoundStyle PixieBallBounce = new(AssetPath + "Sounds/Goozma/Slimes/PixieBallBounce") { MaxInstances = 0, PitchVariance = 0.1f };
            public static readonly SoundStyle PixieBallLoop = new(AssetPath + "Sounds/Goozma/Slimes/PixieBallLoop") { MaxInstances = 0, IsLooped = true };
            public static readonly SoundStyle Warning = new(AssetPath + "Sounds/Alarm") { MaxInstances = 0 };
            #endregion

            #region Corruption
            public static readonly SoundStyle EbonstoneRaise = new(AssetPath + "Sounds/Goozma/Slimes/EbonstoneRaise") { MaxInstances = 0 };
            public static readonly SoundStyle EbonstoneToothTelegraph = new(AssetPath + "Sounds/Goozma/Slimes/EbonstoneToothTelegraph") { MaxInstances = 0, PitchVariance = 0.2f };
            public static readonly SoundStyle EbonstoneToothEmerge = new(AssetPath + "Sounds/Goozma/Slimes/EbonstoneToothEmerge", 1, 2) { MaxInstances = 0, PitchVariance = 0.3f };
            public static readonly SoundStyle EbonstoneCrumble = new(AssetPath + "Sounds/Goozma/Slimes/EbonstoneCrumble") { MaxInstances = 0 };
            #endregion

            #region Stellar
            public static readonly SoundStyle StellarSlimeStarfallTelegraph = new(AssetPath + "Sounds/Goozma/Slimes/StellarSlimeStarfallTelegraph") { MaxInstances = 0 };
            public static readonly SoundStyle StellarSlimeImpact = new(AssetPath + "Sounds/Goozma/Slimes/StellarSlimeImpact") { MaxInstances = 0 };
            public static readonly SoundStyle StellarReform = new(AssetPath + "Sounds/Goozma/Slimes/StellarSlimeReform") { MaxInstances = 0 };
            public static readonly SoundStyle StellarBlackHoleSummon = new(AssetPath + "Sounds/Goozma/Slimes/StellarBlackHoleSummon") { MaxInstances = 0 };
            public static readonly SoundStyle StellarBlackHoleLoop = new(AssetPath + "Sounds/Goozma/Slimes/StellarBlackHoleLoop") { MaxInstances = 0, IsLooped = true };
            public static readonly SoundStyle StellarBlackHoleGulp = new(AssetPath + "Sounds/Goozma/Slimes/StellarSlimeBlackHoleGulp") { MaxInstances = 0 };
            public static readonly SoundStyle StellarConstellationWave = new(AssetPath + "Sounds/Goozma/Slimes/StellarSlimeConstellationWave") { MaxInstances = 0 };
            public static readonly SoundStyle StellarConstellationForm = new(AssetPath + "Sounds/Goozma/Slimes/StellarSlimeConstellationForm") { MaxInstances = 0 };
            #endregion
        }

        public static readonly SoundStyle MonsoonThunder = new(AssetPath + "Sounds/SlimeMonsoon/GoozmaMonsoonThunder", 3, SoundType.Ambient) { MaxInstances = 0 };
    }

    public static class Effects
    {
        #region Basic
        public static readonly Asset<Effect> LightningBeam = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/LightningBeam");
        public static readonly Asset<Effect> FlameDissolve = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/FlameDissolve");
        public static readonly Asset<Effect> FlameDissolveMetaball = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/FlameDissolveMetaball");
        #endregion

        #region Goozma Adjacent
        public static readonly Asset<Effect> SlimeMonsoonCloudLayer = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/SlimeMonsoonOldCloudLayer");
        public static readonly Asset<Effect> SlimeMonsoonDistortion = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/SlimeMonsoonDistortion");
        public static readonly Asset<Effect> MonsoonLightning = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/MonsoonLightningEffect");
        public static readonly Asset<Effect> PluripotentDistortion = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/PluripotentDistortion");
        #endregion

        #region Goozma
        public static readonly Asset<Effect> GoozmaCordMap = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/GoozmaCordMap");
        public static readonly Asset<Effect> GooLightning = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/GooLightningEffect");
        public static readonly Asset<Effect> Cosmos = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/CosmosEffect");
        public static readonly Asset<Effect> StellarRing = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/StellarRing");
        public static readonly Asset<Effect> RainbowGel = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/RainbowGel");
        public static readonly Asset<Effect> HolographicGel = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/HolographEffect");
        public static readonly Asset<Effect> BlackHole = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/SpaceHole");
        #endregion

        #region Goozma Weapons
        public static readonly Asset<Effect> CometKunaiTrail = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/CometKunaiTrail");
        public static readonly Asset<Effect> FusionRay = AssetUtilities.RequestImmediate<Effect>(AssetPath + "Effects/FusionRayEffect");
        #endregion
    }
}
