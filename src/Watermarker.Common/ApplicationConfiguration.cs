using NLog;
using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Watermarker.Common
{
    public sealed class ApplicationConfiguration
    {
        [JsonPropertyName("ParallelThreads")]
        public int ParallelThreads { get; set; }

        [JsonPropertyName("FontName")]
        public string FontName { get; set; }

        [JsonPropertyName("FontScalingFactor")]
        public float FontScalingFactor { get; set; }

        [JsonPropertyName("OffsetFromBottom")]
        public float OffsetFromBottom { get; set; }

        [JsonPropertyName("InfillColor")]
        public string InfillColorString { get; set; }

        [JsonPropertyName("BorderColor")]
        public string BorderColorString { get; set; }

        [JsonIgnore]
        public Color InfillColor { get; private set; }

        [JsonIgnore]
        public Color BorderColor { get; private set; }

        private const string PUBLISHER = "Encamy";
        private const string APPLICATION_TITLE = "Watermarker";
        private const string CONFIG_FILENAME = "config.json";
        private readonly Regex m_colorRegex = new Regex(@"^rgba?\((?'red'\d+)\, *(?'green'\d+)\, *(?'blue'\d+)[, ]*(?'alpha'\d+)?\)$", RegexOptions.Compiled);
        private static readonly Logger m_logger = LogManager.GetLogger("ColoredConsole");

        public static ApplicationConfiguration Load()
        {
            string configPath = CONFIG_FILENAME;
            if (!File.Exists(configPath))
            {
                string programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
                string installPath = Path.Combine(programFiles, PUBLISHER, APPLICATION_TITLE);
                configPath = Path.Combine(installPath, CONFIG_FILENAME);
                if (!File.Exists(configPath))
                {
                    m_logger.Fatal($"Config file \"{configPath}\" does not exists");
                    Environment.Exit(1);
                }
            }

            m_logger.Info($"Reading config from {configPath}");
            ApplicationConfiguration configuration = JsonSerializer.Deserialize<ApplicationConfiguration>(File.ReadAllText(configPath));
            configuration.Process();
            return configuration;
        }

        private void Process()
        {
            InfillColor = ParseColor(InfillColorString);
            BorderColor = ParseColor(BorderColorString);
        }

        private Color ParseColor(string color)
        {
            Match match = m_colorRegex.Match(color);
            if (!match.Success)
            {
                m_logger.Fatal($"Failed to parse string \"{color}\" as color. Use convention \"rgb(123, 123, 123)\" or \"rgba(123, 123, 123, 123)\"");
                Environment.Exit(1);
            }

            string redString = match.Groups["red"].Value;
            string greenString = match.Groups["green"].Value;
            string blueString = match.Groups["blue"].Value;
            string alphaString = match.Groups.ContainsKey("alpha") ? match.Groups["alpha"].Value : string.Empty;

            byte red = ParseColorByte(redString);
            byte green = ParseColorByte(greenString);
            byte blue = ParseColorByte(blueString);

            if (string.IsNullOrEmpty(alphaString))
            {
                return Color.FromRgb(red, green, blue);
            }
            else
            {
                byte alpha = ParseColorByte(alphaString);
                return Color.FromRgba(red, green, blue, alpha);
            }
        }

        private static byte ParseColorByte(string value)
        {
            bool parsed = byte.TryParse(value, out byte byteValue);
            if (!parsed)
            {
                m_logger.Fatal($"Failed to parse string \"{value}\" as integer");
                Environment.Exit(1);
            }

            return byteValue;
        }
    }
}
