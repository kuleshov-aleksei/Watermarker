using NLog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Generic;
using System.IO;

namespace Watermarker
{
    internal sealed class ImageProcessor
    {
        private const float FONT_SCALING_FACTOR = 0.8f;

        private readonly FontProvider m_fontProvider;
        private readonly Logger m_consoleLogger = LogManager.GetLogger("ColoredConsole");

        public ImageProcessor()
        {
            m_fontProvider = new FontProvider();
        }

        public void Process(List<string> files, string outputDirectory)
        {
            foreach (string file in files)
            {
                ProcessFile(file, outputDirectory);
            }
        }

        private void ProcessFile(string file, string outputDirectory)
        {
            m_consoleLogger.Trace($"Обработка файла {file}");
            using (FileStream stream = File.OpenRead(file))
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                Image<Rgba32> image = Image.Load<Rgba32>(stream);

                Font font = m_fontProvider.GetDefault();

                FontRectangle size = TextMeasurer.MeasureSize(filename, new TextOptions(font));
                float scalingFactor = image.Width / size.Width;
                font = new Font(font, scalingFactor * FONT_SCALING_FACTOR * font.Size);

                FontRectangle finalTextRectangle = TextMeasurer.MeasureSize(filename, new TextOptions(font));
                int yPosition = (int)(image.Height - finalTextRectangle.Height - (float)image.Height * 0.1f);
                int xPosition = (int)(image.Width / 2 - finalTextRectangle.Width / 2);
                float borderWidth = font.Size / 30;

                image.Mutate(a => a.DrawText(filename,
                    font,
                    Brushes.Solid(Color.FromRgb(67, 198, 161)),
                    Pens.Solid(Color.FromRgb(0, 0, 0), borderWidth),
                    new PointF(xPosition, yPosition)));

                string outputPath = Path.Combine(outputDirectory, Path.GetFileName(file));
                m_consoleLogger.Trace($"Сохранение {file} в {outputPath}");

                image.Save(outputPath, CreateImageEncoder(Path.GetExtension(file)));
            }
        }

        private static ImageEncoder CreateImageEncoder(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".png" => new PngEncoder(),
                ".jpeg" or ".jpg" => new JpegEncoder(),
                ".bmp" => new BmpEncoder(),
                ".gif" => new GifEncoder(),
                ".tga" => new TgaEncoder(),
                ".tiff" => new TiffEncoder(),
                ".webp" => new WebpEncoder(),
                _ => new PngEncoder()
            };
        }
    }
}
