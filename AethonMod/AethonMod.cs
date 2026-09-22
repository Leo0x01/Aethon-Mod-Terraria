using System.IO;

using Terraria;
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
            //
            // v6.50.2 — FIX: el barrendero NO cubría lo que prometía:
            // (a) RenderTarget2D declarado como tal (y demás
            //     GraphicsResource) no era ancla → se quedaba vivo (y sin
            //     Dispose) tras la descarga;
            // (b) arrays de anclas (Texture2D[], Asset<Texture2D>[],
            //     jagged Texture2D[][]) mantenían TODOS sus elementos;
            // (c) diccionarios con valor-ancla
            //     (Dictionary<string, Asset<Texture2D>>) sostenían las
            //     entradas. Ahora se barren los tres, conservando TODO el
            // patrón defensivo (solo estáticos, try/catch POR CAMPO —
            // tML ya pudo descargar content entre medias).
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
                            System.Type t = campo.FieldType;

                            // v6.50.5 — .NET 8 prohíbe por reflexión la
                            // escritura de campos initonly (static readonly):
                            // RtFieldInfo.SetValue lanza FieldAccessException
                            // (la traza del client.log v6.50.4 — _texturas/
                            // _capas/_puffs/_vapors). Para esos campos: los
                            // ELEMENTOS/entradas se limpian IGUAL (eso suelta
                            // casi todo el ancla de memoria) y el campo en sí
                            // se deja — el AssemblyLoadContext del reload
                            // trae estáticos frescos. (Los 4 ancla readonly
                            // históricos ya son mutables en v6.50.5; esto es
                            // la red para futuros.)
                            bool esInitOnly = campo.IsInitOnly && !campo.IsLiteral;

                            // v6.50.2 — FIX (b): ARRAYS de anclas — se
                            // anulan (y disponen si son GPU) los ELEMENTOS
                            // (recursivo: los jagged Texture2D[][] llevan
                            // arrays dentro) y el array entero se suelta.
                            if (t.IsArray)
                            {
                                System.Type elem = t.GetElementType();
                                if (elem != null && EsAnclaDeDescarga(elem))
                                {
                                    AnularAnclasArray(campo.GetValue(null) as System.Array);
                                    if (!esInitOnly) campo.SetValue(null, null);
                                }
                                continue;
                            }

                            // v6.50.2 — FIX (c): DICCIONARIOS con valor de
                            // ancla (Dictionary<string, Asset<Texture2D>>…)
                            // — Clear() suelta todas las entradas y el
                            // contenedor se anula como cualquier campo.
                            if (t.IsGenericType &&
                                typeof(System.Collections.IDictionary).IsAssignableFrom(t))
                            {
                                System.Type[] args = t.GetGenericArguments();
                                if (args.Length == 2 && EsAnclaDeDescarga(args[1]))
                                {
                                    try { (campo.GetValue(null) as System.Collections.IDictionary)?.Clear(); }
                                    catch { }
                                    if (!esInitOnly) campo.SetValue(null, null);
                                }
                                continue;
                            }

                            if (EsAnclaDeDescarga(t))
                            {
                                // v6.50.2 — FIX (a): si el valor es un
                                // GraphicsResource (RenderTarget2D, textura
                                // o blend propios) se DISPONE antes de
                                // anular — la GPU no se libera con el GC.
                                // (v6.50.5: el Dispose del VALOR sí corre
                                // para initonly — es una llamada al objeto,
                                // no una escritura del campo.)
                                SoltarGpu(campo.GetValue(null));
                                if (!esInitOnly) campo.SetValue(null, null);
                            }
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

            // v6.50.2 — FIX: TODO recurso de GPU cuenta. Antes solo los
            // tipos EXACTOS Effect/Texture2D/BlendState: un RenderTarget2D
            // declarado como RenderTarget2D (MediaResLib, la lente) NO era
            // ancla y sobrevivía a la descarga — igual que RasterizerState,
            // SamplerState o cualquier GraphicsResource futuro. (El Dispose
            // defensivo de estos valores corre en el barrido.)
            if (typeof(Microsoft.Xna.Framework.Graphics.GraphicsResource).IsAssignableFrom(t))
                return true;

            // v6.50.2 — FIX: ARRAYS de anclas — el ELEMENTO es el ancla
            // (Texture2D[], Asset<Texture2D>[], BlendState[]… y los
            // jagged Texture2D[][] por recursión): el barrido anula y
            // dispone los elementos y suelta el array entero.
            if (t.IsArray)
            {
                System.Type elem = t.GetElementType();
                return elem != null && EsAnclaDeDescarga(elem);
            }

            // Listas de delegados de render (los closures capturan las
            // instancias del pipeline — p.ej. las capas de VFXCore).
            if (t.IsGenericType &&
                t.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>) &&
                t.GetGenericArguments()[0].IsSubclassOf(typeof(System.MulticastDelegate)))
                return true;

            return false;
        }

        /// <summary>
        /// v6.50.2 — FIX: suelta un VALOR de GPU si es un GraphicsResource
        /// (RenderTarget2D, textura o blend PROPIOS del mod): el barrendero
        /// solo anulaba referencias y los recursos nativos pedían Dispose
        /// (la GPU no se libera con el GC — vanilla usa el mismo cast en su
        /// limpieza: ((GraphicsResource)target).Dispose()). Idempotente
        /// (re-Dispose es no-op) y 100% defensivo: TODO en try/catch para
        /// que la descarga jamás reviente.
        ///
        /// LECCIÓN v5.87 DE LA CASA (BlackHoleLensSystem/MediaResLib, ya
        /// vivida): Mod.Unload corre en el hilo secundario de carga de tML
        /// y FNA3D exige el Dispose de recursos gráficos en el hilo
        /// principal — el Dispose se ENCOLA vía Main.QueueMainThreadAction
        /// (cola drenada en Main.Update cada frame, incluso durante la
        /// pantalla de carga del reload) y el closure captura solo la
        /// referencia al recurso.
        /// </summary>
        private static void SoltarGpu(object valor)
        {
            if (valor is Microsoft.Xna.Framework.Graphics.GraphicsResource recurso)
            {
                try
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        try { recurso.Dispose(); }
                        catch { }
                    });
                }
                catch
                {
                    // Encolado imposible (apagado total del proceso): se
                    // abandona la referencia — el driver libera los recursos
                    // del proceso al terminar de todos modos.
                }
            }
        }

        /// <summary>
        /// v6.50.2 — FIX: anula (y dispone si son GPU) los elementos de un
        /// array de anclas — recursivo para los jagged (Texture2D[][]:
        /// cada elemento es otro array). Nada de reventar: cada paso en
        /// try/catch (el mismo contrato del barrendero por campo).
        /// </summary>
        private static void AnularAnclasArray(System.Array array)
        {
            if (array == null) return;
            for (int i = 0; i < array.Length; i++)
            {
                object v = null;
                try { v = array.GetValue(i); } catch { }
                if (v is System.Array interno)
                    AnularAnclasArray(interno);
                else
                    SoltarGpu(v);
                try { array.SetValue(null, i); } catch { }
            }
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            ShardSyncSystem.HandlePacket(reader, whoAmI);
        }
    }
}
