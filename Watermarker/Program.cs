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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Watermarker
{
    internal class Program
    {
        private static readonly Logger m_consoleLogger = LogManager.GetLogger("ColoredConsole");
        private const float FONT_SCALING_FACTOR = 0.8f;

        static void Main(string[] args)
        {
            ValidateArgs(args, out string directory);

            FontProvider fontProvider = new FontProvider();
            List<string> files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).ToList();
            m_consoleLogger.Info($"Найдено {files.Count} файлов");
            foreach (string file in files)
            {
                ProcessFile(fontProvider, file);
            }
        }

        private static void ProcessFile(FontProvider fontProvider, string file)
        {
            m_consoleLogger.Trace($"Обработка файла {file}");
            using (FileStream stream = File.OpenRead(file))
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                Image<Rgba32> image = Image.Load<Rgba32>(stream);

                Font font = fontProvider.GetDefault();

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

                string outputPath = Path.Combine("tmp", Path.GetFileName(file));
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

        private static void ValidateArgs(string[] args, out string directory)
        {
            if (args.Length != 1)
            {
                m_consoleLogger.Fatal("Укажите директорию для обработки");
                Environment.Exit(1);
            }

            directory = args[0];
            if (!Directory.Exists(directory))
            {
                m_consoleLogger.Fatal($"Директорию \"{directory}\" не существует");
                Environment.Exit(1);
            }
        }
    }
}