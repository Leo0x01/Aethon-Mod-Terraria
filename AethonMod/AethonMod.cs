using System.IO;

using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;

namespace AethonMod
{
    public class AethonMod : Mod
    {
        public static AethonMod Instance => ModContent.GetInstance<AethonMod>();

        public override void Load()
        {
            // Sistema simplificado: el SkillTree y el Codex fueron eliminados.
            // No hay inicializacion extra necesaria.

            // v6.43 — CIELOLIB: EL REGISTRO DE LAS TEXTURAS DE FONDO DEL
            // SAGRARIO (los slots de fondo de esta versión se dan de alta
            // aquí — el estilo de bioma las pide por su ruta relativa).
            // v6.50.1 — FIX: AddBackgroundTexture(Mod, string) recibe la ruta
            // COMPLETA con el prefijo del mod (lo pide tal cual vía
            // ModContent.Request y la guarda como clave del diccionario), a
            // diferencia de GetBackgroundSlot(Mod, string), que añade el
            // prefijo él solo. Sin el prefijo, el mod crashea en Load() con
            // MissingResourceException ("no se encontró un mod con el nombre
            // 'Content'"). Las constantes siguen siendo relativas porque así
            // las pide el estilo de bioma.
            BackgroundTextureLoader.AddBackgroundTexture(this, $"{Name}/{SanctumBackgroundStyle.RutaFar}");
            BackgroundTextureLoader.AddBackgroundTexture(this, $"{Name}/{SanctumBackgroundStyle.RutaMiddle}");
            BackgroundTextureLoader.AddBackgroundTexture(this, $"{Name}/{SanctumBackgroundStyle.RutaClose}");
        }

        public override void Unload()
        {
            // v6.50.1 — EL BARRENDERO DE MEMORIA (el warning del client.log:
            // "AethonMod mod class still using memory"). La auditoría P2-1
            // contó ~107 campos estáticos de assets (Asset<T>, Ref<Effect>,
            // Texture2D, BlendState, listas de delegados de render) en 43
            // archivos que JAMÁS se anulaban — cada uno es un ancla que
            // mantiene vivo el AssemblyLoadContext del mod tras la
            // descarga (los de ModProjectile ni siquiera TIENEN hook Unload:
            // por eso el barrendero es un barrido por reflexión y no 43
            // métodos a mano). Los disposals explícitos de RenderTargets
            // (BlackHoleLensSystem, MediaResLib) y las limpiezas de los
            // ModSystems ya corrieron: esto remata las referencias que
            // quedan, incluidas las de clases futuras.
            try
            {
                const System.Reflection.BindingFlags Flags =
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic;

                foreach (var tipo in GetType().Assembly.GetTypes())
                {
                    System.Reflection.FieldInfo[] campos;
                    try { campos = tipo.GetFields(Flags); }
                    catch { continue; } // tipos genéricos abiertos u otros raros

                    foreach (var campo in campos)
                    {
                        try
                        {
                            if (EsAnclaDeDescarga(campo.FieldType))
                                campo.SetValue(null, null);
                        }
                        catch { } // nunca dejar que la descarga reviente
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// ¿Este tipo de campo es un ancla que mantiene al mod vivo tras la
        /// descarga? Solo referencias a infraestructura del motor (assets,
        /// shaders, texturas, estados de blend) y listas de delegados de
        /// render — los estáticos de VALOR (contadores, configs, pools de
        /// structs) quedan como están.
        /// </summary>
        private static bool EsAnclaDeDescarga(System.Type t)
        {
            // Asset<Texture2D>, Asset<Effect>, ... (la referencia viva al
            // pipeline de assets de tML — vive en ReLogic.Content).
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ReLogic.Content.Asset<>))
                return true;

            // Ref<Effect> (el Ref sostiene el shader). Resolución por
            // nombre de metadatos: la ubicación del tipo Ref`1 ha mudado
            // entre builds de tML y la referencia calificada no compila
            // contra esta — el mod lo usa SIEMPRE como Ref<Effect>.
            if (t.IsGenericType && t.Name == "Ref`1" &&
                t.GetGenericArguments()[0] == typeof(Microsoft.Xna.Framework.Graphics.Effect))
                return true;

            // El shader, la textura o el estado de blend directamente.
            if (t == typeof(Microsoft.Xna.Framework.Graphics.Effect) ||
                t == typeof(Microsoft.Xna.Framework.Graphics.Texture2D) ||
                t == typeof(Microsoft.Xna.Framework.Graphics.BlendState))
                return true;

            // Listas de delegados de render (los closures capturan las
            // instancias del pipeline — p.ej. las capas de VFXCore).
            if (t.IsGenericType &&
                t.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>) &&
                t.GetGenericArguments()[0].IsSubclassOf(typeof(System.MulticastDelegate)))
                return true;

            return false;
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            ShardSyncSystem.HandlePacket(reader, whoAmI);
        }
    }
}
