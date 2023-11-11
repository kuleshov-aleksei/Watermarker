using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Watermarker.Common;

namespace Watermarker
{
    internal class Program
    {
        private static readonly Logger m_consoleLogger = LogManager.GetLogger("ColoredConsole");

        static async Task Main(string[] args)
        {
            ValidateArgs(args, out string directory);
            ApplicationConfiguration applicationConfiguration = ApplicationConfiguration.Load();

            List<string> files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).ToList();
            if (files.Count == 0)
            {
                m_consoleLogger.Error("No files were found");
                return;
            }

            m_consoleLogger.Info($"Found {files.Count} files");

            string rootDirectory = Path.GetDirectoryName(directory);
            string folder = Path.GetFileName(directory);
            DateTime now = DateTime.Now;
            string outputDirectory = Path.Combine(rootDirectory, $"{folder}_{now:yyyy\\yMM\\mdd\\d_HH\\hmm\\mss\\s}");
            Directory.CreateDirectory(outputDirectory);

            ImageProcessor processor = new ImageProcessor(applicationConfiguration);
            await processor.Process(files, outputDirectory);
        }

        private static void ValidateArgs(string[] args, out string directory)
        {
            if (args.Length != 1)
            {
                m_consoleLogger.Fatal("Specify directory");
                Environment.Exit(1);
            }

            directory = args[0];
            if (!Directory.Exists(directory))
            {
                m_consoleLogger.Fatal($"Directory \"{directory}\" does not exists");
                Environment.Exit(1);
            }
        }
    }
}