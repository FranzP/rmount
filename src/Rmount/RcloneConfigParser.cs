using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rmount
{
    /// <summary>
    /// Parser for rclone configuration files
    /// </summary>
    public class RcloneConfigParser
    {
        private static AppConfig _config;

        /// <summary>
        /// Initialize the parser with configuration
        /// </summary>
        public static void Initialize(AppConfig config)
        {
            _config = config;
        }

        private class ConfigSection
        {
            public string Name { get; set; }
            public Dictionary<string, string> Properties { get; set; }
            public List<string> Comments { get; set; }

            public ConfigSection()
            {
                Properties = new Dictionary<string, string>();
                Comments = new List<string>();
            }
        }

        /// <summary>
        /// Get all remote names from rclone.conf
        /// </summary>
        public static List<string> GetRemoteNames()
        {
            var remotes = new List<string>();
            string configPath = GetRcloneConfigPath();

            if (!File.Exists(configPath))
            {
                return remotes;
            }

            try
            {
                string[] lines = File.ReadAllLines(configPath);
                foreach (string line in lines)
                {
                    // Search for [remote_name]
                    var match = Regex.Match(line.Trim(), @"^\[(.+)\]$");
                    if (match.Success)
                    {
                        remotes.Add(match.Groups[1].Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading rclone config: {ex.Message}");
            }

            return remotes;
        }

        /// <summary>
        /// Get a property value from a remote section
        /// </summary>
        public static string GetRemoteProperty(string remoteName, string propertyName)
        {
            string configPath = GetRcloneConfigPath();
            if (!File.Exists(configPath))
            {
                return null;
            }

            try
            {
                string[] lines = File.ReadAllLines(configPath);
                bool inSection = false;

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();

                    // Check if we're in the right section
                    var sectionMatch = Regex.Match(trimmed, @"^\[(.+)\]$");
                    if (sectionMatch.Success)
                    {
                        inSection = sectionMatch.Groups[1].Value == remoteName;
                        continue;
                    }

                    if (inSection && !string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("#") && !trimmed.StartsWith(";"))
                    {
                        var propMatch = Regex.Match(trimmed, @"^([^=]+)\s*=\s*(.*)$");
                        if (propMatch.Success)
                        {
                            string key = propMatch.Groups[1].Value.Trim();
                            string value = propMatch.Groups[2].Value.Trim();

                            if (key == propertyName)
                            {
                                return value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading property: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Set a property value in a remote section
        /// </summary>
        public static void SetRemoteProperty(string remoteName, string propertyName, string propertyValue)
        {
            string configPath = GetRcloneConfigPath();
            
            // If config doesn't exist, create it
            if (!File.Exists(configPath))
            {
                string dir = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(configPath, "");
            }

            try
            {
                var sections = ParseConfig(configPath);
                
                // Find the remote section
                var section = sections.FirstOrDefault(s => s.Name == remoteName);
                
                if (section == null)
                {
                    // Section doesn't exist - create it
                    section = new ConfigSection { Name = remoteName };
                    sections.Add(section);
                }

                // Set or update the property
                section.Properties[propertyName] = propertyValue;

                // Write config back
                WriteConfig(configPath, sections);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing property: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove a property from a remote section
        /// </summary>
        public static void RemoveRemoteProperty(string remoteName, string propertyName)
        {
            string configPath = GetRcloneConfigPath();
            if (!File.Exists(configPath))
            {
                return;
            }

            try
            {
                var sections = ParseConfig(configPath);
                var section = sections.FirstOrDefault(s => s.Name == remoteName);
                
                if (section != null && section.Properties.ContainsKey(propertyName))
                {
                    section.Properties.Remove(propertyName);
                    WriteConfig(configPath, sections);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing property: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse the config file into sections
        /// </summary>
        private static List<ConfigSection> ParseConfig(string configPath)
        {
            var sections = new List<ConfigSection>();
            ConfigSection currentSection = null;

            string[] lines = File.ReadAllLines(configPath);

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                // Comments or empty lines before a section
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                {
                    if (currentSection != null)
                    {
                        currentSection.Comments.Add(line);
                    }
                    continue;
                }

                // New section
                var sectionMatch = Regex.Match(trimmed, @"^\[(.+)\]$");
                if (sectionMatch.Success)
                {
                    currentSection = new ConfigSection
                    {
                        Name = sectionMatch.Groups[1].Value
                    };
                    sections.Add(currentSection);
                    continue;
                }

                // Property
                if (currentSection != null)
                {
                    var propMatch = Regex.Match(trimmed, @"^([^=]+)\s*=\s*(.*)$");
                    if (propMatch.Success)
                    {
                        string key = propMatch.Groups[1].Value.Trim();
                        string value = propMatch.Groups[2].Value.Trim();
                        currentSection.Properties[key] = value;
                    }
                }
            }

            return sections;
        }

        /// <summary>
        /// Write sections back to config file
        /// </summary>
        private static void WriteConfig(string configPath, List<ConfigSection> sections)
        {
            var sb = new StringBuilder();

            foreach (var section in sections)
            {
                // Write section header
                sb.AppendLine($"[{section.Name}]");

                // Write properties
                foreach (var prop in section.Properties)
                {
                    sb.AppendLine($"{prop.Key} = {prop.Value}");
                }

                // Empty line between sections
                sb.AppendLine();
            }

            File.WriteAllText(configPath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Get the rclone config path from configuration
        /// </summary>
        public static string GetRcloneConfigPath()
        {
            if (_config != null)
            {
                return _config.Config;
            }

            // Fallback to default
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "rclone", "rclone.conf");
        }
    }
}
