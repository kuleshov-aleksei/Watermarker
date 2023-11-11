using PropertyChanged.SourceGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace Watermarker.Installer
{
    public partial class MainWindow : Window
    {
        private const string BUNDLED_APPLICATION_PATTERN = "watermarker-*.zip";
        private readonly Regex m_bundleVersionRegex = new Regex(@"^watermarker-v(?'version'\d+\.\d+.\d+).zip$", RegexOptions.Compiled);
        private const string PUBLISHER = "Encamy";
        private const string APPLICATION_TITLE = "Watermarker";
        private const string APPLICATION_FILE = "Watermarker.exe";
        private readonly string m_iconUrl = Path.Combine("Assets", "icon-72x72.ico");
        private bool m_installed;

        private string m_selectedBundleFile = string.Empty;

        [Notify] private string _detectedApplicationVersion;
        [Notify] private string _logMessages;

        public MainWindow()
        {
            InitializeComponent();

            DetectedApplicationVersion = "Unknown";
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            List<string> bundles = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), BUNDLED_APPLICATION_PATTERN).ToList();
            if (bundles.Count == 0)
            {
                DetectedApplicationVersion = "Bundle not found";
                InstallButton.IsEnabled = false;
                return;
            }

            Version maximumVersion = null;
            string selectedBundle = string.Empty;
            string selectedVersion = string.Empty;
            foreach (string bundle in bundles.Select(x => Path.GetFileName(x)))
            {
                Match match = m_bundleVersionRegex.Match(bundle);
                if (match.Success)
                {
                    Version version = new Version(match.Groups["version"].Value);
                    if (maximumVersion == null || version > maximumVersion)
                    {
                        maximumVersion = version;
                        selectedBundle = bundle;
                        selectedVersion = match.Groups["version"].Value;
                    }
                }
            }

            m_selectedBundleFile = selectedBundle;
            DetectedApplicationVersion = selectedVersion;
            InstallButton.IsEnabled = true;
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InstallInternal();
            }
            catch (Exception ex)
            {
                Log($"Installation failed: {ex.Message}");
            }
        }
        
        private void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UninstallInternal();
            }
            catch (Exception ex)
            {
                Log($"Installation failed: {ex.Message}");
            }
        }

        private void UninstallInternal()
        {
            Log($"Removing context menu");
            ContextMenuRegistrar.UnregisterContextMenu(Log);

            string programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
            string installPath = Path.Combine(programFiles, PUBLISHER, APPLICATION_TITLE);
            Log($"Cleaning-up installation files from {installPath}");
            if (Directory.Exists(installPath))
            {
                Directory.Delete(installPath, true);
            }
        }

        private void InstallInternal()
        {
            if (m_installed)
            {
                Application.Current.Shutdown();
                return;
            }

            string programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
            string installPath = Path.Combine(programFiles, PUBLISHER, APPLICATION_TITLE);
            Log($"Installing to {installPath}");

            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
                Log($"Installation directory created");
            }

            List<string> existingFiles = Directory.EnumerateFiles(installPath, "*", SearchOption.AllDirectories).ToList();
            string ignorePath = Path.Combine(installPath, "old");
            existingFiles = existingFiles.Where(x => !x.StartsWith(ignorePath)).ToList();
            string previousRelease = Path.Combine(installPath, "old");
            if (existingFiles.Count > 0)
            {
                Log($"Cleaning up previous release");
                if (Directory.Exists(previousRelease))
                {
                    Directory.Delete(previousRelease, true);
                }

                Directory.CreateDirectory(previousRelease);
                foreach (string file in existingFiles)
                {
                    File.Move(file, Path.Combine(previousRelease, Path.GetFileName(file)));
                }

                Log($"Previous release moved to {previousRelease}");
            }

            using (ZipArchive archive = ZipFile.OpenRead(m_selectedBundleFile))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string entryFullPath = Path.Combine(installPath, entry.FullName);
                    string directoryName = Path.GetDirectoryName(entryFullPath);
                    if (directoryName.Length > 0)
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    // Skip directory entry
                    if (Path.GetFileName(entryFullPath).Length == 0)
                    {
                        continue;
                    }

                    Log($"Extracting file {entry.FullName}");
                    entry.ExtractToFile(entryFullPath);
                }
            }

            Log("Installed. Registering shell extension");
            ContextMenuRegistrar.RegisterContextMenu(
                Path.Combine(installPath, APPLICATION_FILE),
                Path.Combine(installPath, m_iconUrl),
                Log);
            Log("Shell extension added");

            m_installed = true;
            InstallButton.Content = "Exit";
        }

        private void Log(string message)
        {
            LogMessages += message + Environment.NewLine;
        }
    }
}
