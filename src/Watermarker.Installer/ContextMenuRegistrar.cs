using Microsoft.Win32;
using System;
using System.IO;

namespace Watermarker.Installer
{
    internal static class ContextMenuRegistrar
    {
        private const string SHELL_KEY_NAME = "Watermarker";

        internal static void UnregisterContextMenu(Action<string> onLog)
        {
            try
            {
                UnregisterContextMenu();
            }
            catch (Exception ex)
            {
                onLog($"Failed to unregister command for context menu: {ex.Message}");
            }
        }

        internal static void RegisterContextMenu(string executablePath, string iconUrl, Action<string> onLog)
        {
            try
            {
                RegisterContextMenu(executablePath, iconUrl);
            }
            catch (Exception ex)
            {
                onLog($"Failed to register command for context menu: {ex.Message}");
            }
        }

        private static void RegisterContextMenu(string executablePath, string iconUrl)
        {
            UnregisterContextMenu();

            string regPath = $"Software\\Classes\\directory\\shell\\{SHELL_KEY_NAME}";
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regPath))
            {
                key.SetValue(null, "Add watermark");
                key.SetValue("Icon", iconUrl);
            }

            string commandPath = $"{regPath}\\command";
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(commandPath))
            {
                string baseDirectory = Directory.GetParent(executablePath).FullName;
                string menuCommand = $"\"{executablePath}\" \"%1\"";
                key.SetValue(null, menuCommand);
            }
        }

        private static void UnregisterContextMenu()
        {
            string regPath = $"Software\\Classes\\directory\\shell\\{SHELL_KEY_NAME}";
            Registry.CurrentUser.DeleteSubKeyTree(regPath, throwOnMissingSubKey: false);
        }

    }
}
