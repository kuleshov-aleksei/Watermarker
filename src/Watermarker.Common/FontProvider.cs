using SixLabors.Fonts;
using System.IO;
using System.Reflection;

namespace Watermarker.Common
{
    public sealed class FontProvider
    {
        private readonly FontCollection m_fonts = new FontCollection();
        private readonly Font m_defaultFont;

        public FontProvider(string fontName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            foreach (string resourceName in resources)
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    m_fonts.Add(stream);
                }
            }

            m_defaultFont = m_fonts.Get(fontName).CreateFont(16, FontStyle.Regular);
        }

        public Font GetDefault()
        {
            return m_defaultFont;
        }
    }
}
