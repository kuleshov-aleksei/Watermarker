using NLog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Diagnostics;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
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
            using (Image image = await Image.LoadAsync(stream))
            {
                string filename = Path.GetFileNameWithoutExtension(file);
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

                using (FileStream outputStream = File.Create(outputPath))
                {
                    await image.SaveAsync(outputStream, image.Metadata.DecodedImageFormat);
                }

                OnProcess?.Invoke();
            }
        }
    }
}
