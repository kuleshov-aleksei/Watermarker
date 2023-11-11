using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Watermarker
{
    internal class Program
    {
        private static readonly Logger m_consoleLogger = LogManager.GetLogger("ColoredConsole");

        static void Main(string[] args)
        {
            ValidateArgs(args, out string directory);

            List<string> files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).ToList();
            m_consoleLogger.Info($"Найдено {files.Count} файлов");
            
            ImageProcessor processor = new ImageProcessor();
            processor.Process(files);
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