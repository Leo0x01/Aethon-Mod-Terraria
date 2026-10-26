// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReLogic.Content;
using ReLogic.Content.Sources;

namespace AethonMod.Content.GoozmaPort.Common.Utilities
{
    /// <summary>
    ///     A wrapper around a given <see cref="IContentSource"/> instance which
    ///     allows for finer control over content loading.
    /// </summary>
    public class SmartContentSource : IContentSource
    {
        public IContentValidator ContentValidator
        {
            get => _source.ContentValidator;
            set => _source.ContentValidator = value;
        }

        public RejectedAssetCollection Rejections => _source.Rejections;

        private readonly IContentSource _source;
        private readonly Dictionary<string, string> _redirects = new Dictionary<string, string>();

        public SmartContentSource(IContentSource source)
        {
            _source = source;
        }

        public void AddDirectoryRedirect(string fromDir, string toDir)
        {
            _redirects.Add(fromDir, toDir);
        }

        private string RewritePath(string path)
        {
            foreach ((string from, string to) in _redirects) {
                if (path.StartsWith(from)) {
                    return path.Replace(from, to);
                }
            }

            return path;
        }

        IEnumerable<string> IContentSource.EnumerateAssets()
        {
            return _source.EnumerateAssets().Select(RewritePath);
        }

        string IContentSource.GetExtension(string assetName)
        {
            return _source.GetExtension(RewritePath(assetName));
        }

        Stream IContentSource.OpenStream(string fullAssetName)
        {
            return _source.OpenStream(RewritePath(fullAssetName));
        }
    }
}
