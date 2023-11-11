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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Watermarker.Common
{
    public sealed class ImageProcessor
    {
        private readonly FontProvider m_fontProvider;
        private readonly Logger m_logger = LogManager.GetCurrentClassLogger();
        private readonly ApplicationConfiguration m_applicationConfiguration;

        public event Action OnProcess;

        public ImageProcessor(ApplicationConfiguration applicationConfiguration)
        {
            m_applicationConfiguration = applicationConfiguration;
            m_fontProvider = new FontProvider(m_applicationConfiguration.FontName);
        }

        public async Task Process(List<string> files, string outputDirectory)
        {
            if (m_applicationConfiguration.ParallelThreads == 1)
            {
                foreach (string file in files)
                {
                    await ProcessFile(file, outputDirectory);
                }
            }
            else
            {
                await Parallel.ForEachAsync(files, new ParallelOptions
                {
                    MaxDegreeOfParallelism = m_applicationConfiguration.ParallelThreads,
                },
                async (file, _) => await ProcessFile(file, outputDirectory));
            }
        }

        private async Task ProcessFile(string file, string outputDirectory)
        {
            m_logger.Trace($"Processing file {file}");
            using (FileStream stream = File.OpenRead(file))
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                Image<Rgba32> image = await Image.LoadAsync<Rgba32>(stream);

                Font font = m_fontProvider.GetDefault();

                FontRectangle size = TextMeasurer.MeasureSize(filename, new TextOptions(font));
                float scalingFactor = image.Width / size.Width;
                font = new Font(font, scalingFactor * m_applicationConfiguration.FontScalingFactor * font.Size);

                FontRectangle finalTextRectangle = TextMeasurer.MeasureSize(filename, new TextOptions(font));
                int yPosition = (int)(image.Height - finalTextRectangle.Height - (float)image.Height * m_applicationConfiguration.OffsetFromBottom);
                int xPosition = (int)(image.Width / 2 - finalTextRectangle.Width / 2);
                float borderWidth = font.Size / 30;

                image.Mutate(a => a.DrawText(filename,
                    font,
                    Brushes.Solid(m_applicationConfiguration.InfillColor),
                    Pens.Solid(m_applicationConfiguration.BorderColor, borderWidth),
                    new PointF(xPosition, yPosition)));

                string outputPath = Path.Combine(outputDirectory, Path.GetFileName(file));
                m_logger.Trace($"Saving {file} to {outputPath}");

                await image.SaveAsync(outputPath, CreateImageEncoder(Path.GetExtension(file)));
                OnProcess?.Invoke();
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
