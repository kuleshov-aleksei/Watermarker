﻿using PropertyChanged.SourceGenerator;
using System.IO;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Watermarker.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Watermarker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Logger m_logger = LogManager.GetCurrentClassLogger();
        private int m_filesProcessed = 0;
        private readonly string m_directory;
        private readonly ApplicationConfiguration m_applicationConfiguration;

        [Notify] private string _status;
        [Notify] private string _itemsRemaining;
        [Notify] private string _progress;

        public MainWindow(string[] args)
        {
            InitializeComponent();
            ValidateArgs(args, out m_directory);
            m_applicationConfiguration = ApplicationConfiguration.Load();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Status = "Enumerating files";

            List<string> files = Directory.EnumerateFiles(m_directory, "*", SearchOption.AllDirectories).ToList();
            if (files.Count == 0)
            {
                m_logger.Error("No files were found");
                MessageBox.Show("No files were found", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                Environment.Exit(1);
            }

            m_logger.Info($"Found {files.Count} files");
            Status = $"Processing {files.Count} files";
            ImageProcessorProgressBar.Minimum = 0;
            ImageProcessorProgressBar.Maximum = files.Count;
            ImageProcessorProgressBar.Value = 0;

            string rootDirectory = Path.GetDirectoryName(m_directory);
            string folder = Path.GetFileName(m_directory);
            DateTime now = DateTime.Now;
            string folderName = $"{folder}_{now:yyyy\\yMM\\mdd\\d_HH\\hmm\\mss\\s}";
            string outputDirectory = Path.Combine(rootDirectory, folderName);
            Directory.CreateDirectory(outputDirectory);
            Status = $"Processing {files.Count} files. Output: {folderName}";

            ImageProcessor processor = new ImageProcessor(m_applicationConfiguration);
            processor.OnProcess += () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    int currentValue = Interlocked.Increment(ref m_filesProcessed);
                    ImageProcessorProgressBar.Value = currentValue;
                    ItemsRemaining = $"Items remaining: {files.Count - currentValue}";
                    Progress = $"{((float)currentValue / files.Count * 100):N2}% ({currentValue} / {files.Count})";
                });
            };

            await processor.Process(files, outputDirectory);

            Application.Current.Shutdown();
        }

        private static void ValidateArgs(string[] args, out string directory)
        {
            if (args.Length != 1)
            {
                MessageBox.Show("Invalid path", "Error");
                Environment.Exit(1);
            }

            directory = args[0];
            if (!Directory.Exists(directory))
            {
                MessageBox.Show($"Directory \"{directory}\" does not exists", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                Environment.Exit(1);
            }
        }
    }
}
