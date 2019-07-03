using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace iRacingSettingsHelper
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                return 1;
            }

            if (args[0].ToLower().Equals("install"))
            {
                InstallDefaultConfiguration();
                return 0;
            }
            else
            {
                return UpdateSettings(args[0]);
            }
        }

        private static int UpdateSettings(string role)
        {
            using (StreamWriter log = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "iRacing Auto Settings", "Helper.log"), true))
            {
                log.AutoFlush = true;

                try
                {
                    log.WriteLine(DateTime.Now.ToString());
                    log.WriteLine("Loading configuration.");

                    if (!File.Exists(ConfigurationPath))
                    {
                        log.WriteLine("Could not find Configuration.xml");
                        return 1;
                    }

                    XmlDocument configuration = new XmlDocument();

                    configuration.Load(ConfigurationPath);

                    var actionNode = configuration.SelectSingleNode(string.Format("//actions[@role='{0}']", role));
                    if (actionNode == null)
                    {
                        log.WriteLine(string.Format("Could not find actions for role {0}.", role));
                        return 1;
                    }

                    log.WriteLine(string.Format("Executing actions for role {0}.", role));

                    var iniFileNodes = actionNode.SelectNodes("ini_files/ini_file");

                    foreach (XmlNode iniFileNode in iniFileNodes)
                    {
                        var filepathNode = iniFileNode.Attributes["path"];
                        if (filepathNode == null)
                        {
                            log.WriteLine("Missing filepath.");
                            return 1;
                        }

                        var filePath = filepathNode.Value.ToLower();
                        filePath = filePath.Replace("%iracing%", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "iRacing"));

                        if (!File.Exists(filePath))
                        {
                            log.WriteLine(string.Format("{0} does not exist.", filePath));
                            return 1;
                        }

                        var propertieslog = new System.Text.StringBuilder();

                        propertieslog.AppendLine(string.Format("Writing settings to '{0}':", filePath));

                        var iniFile = new IniFile(filePath);

                        var sections = iniFileNode.SelectNodes("sections/section");
                        if (sections.Count == 0)
                        {
                            log.WriteLine("Could not find section configurations.");
                            return 1;
                        }

                        foreach (XmlNode section in sections)
                        {
                            var properties = section.SelectNodes("properties/property");

                            if (properties.Count == 0)
                            {
                                log.WriteLine("Could not find section property configurations.");
                                return 1;
                            }

                            foreach (XmlNode property in properties)
                            {
                                var sectionNameNode = section.Attributes["name"];
                                if (sectionNameNode == null)
                                {
                                    log.WriteLine("Missing section name.");
                                    return 1;
                                }

                                var propertyNameNode = property.Attributes["name"];
                                if (propertyNameNode == null)
                                {
                                    log.WriteLine("Missing section name.");
                                    return 1;
                                }

                                var propertyValueNode = property.Attributes["value"];

                                if (propertyValueNode != null)
                                {
                                    propertieslog.AppendLine(string.Format("\t{0}.{1} = {2}", sectionNameNode.Value, propertyNameNode.Value, propertyValueNode.Value));
                                    iniFile.IniWriteValue(sectionNameNode.Value, propertyNameNode.Value, propertyValueNode.Value);
                                }
                                else
                                {
                                    propertieslog.AppendLine(string.Format("\tDeleting {0}.{1}", sectionNameNode.Value, propertyNameNode.Value));
                                    iniFile.IniWriteValue(sectionNameNode.Value, propertyNameNode.Value, null);
                                }
                            }
                        }

                        log.WriteLine(propertieslog.ToString());
                    }

                    var executableNodes = actionNode.SelectNodes("executables/executable");

                    foreach (XmlNode executableNode in executableNodes)
                    {
                        var filepathNode = executableNode.Attributes["path"];
                        if (filepathNode == null)
                        {
                            log.WriteLine("Missing filepath.");
                            return 1;
                        }

                        var arguments = executableNode.Attributes["arguments"] == null ? string.Empty : executableNode.Attributes["arguments"].Value;

                        bool alreadyRunning = false;
                        var processes = Process.GetProcesses();
                        foreach (Process p in processes)
                        {
                            if (string.Equals(p.ProcessName, Path.GetFileNameWithoutExtension(filepathNode.Value), StringComparison.InvariantCultureIgnoreCase))
                            {
                                alreadyRunning = true;
                                break;
                            }
                        }

                        if (!alreadyRunning)
                        {
                            var process = new Process();
                            process.StartInfo.FileName = filepathNode.Value;
                            process.StartInfo.Arguments = arguments;
                            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(filepathNode.Value);
                            process.StartInfo.UseShellExecute = true;

                            log.WriteLine(string.Format("Executing '{0} {1}'", filepathNode.Value, arguments));

                            process.Start();

                            if (executableNode.Attributes["wait"] != null && executableNode.Attributes["wait"].Value.ToLower() == "true")
                            {
                                process.WaitForExit();
                            }
                        }
                    }

                    return 0;
                }
                catch (Exception e)
                {
                    log.WriteLine(e.ToString());
                    return 1;
                }
            }
        }

        private static string ConfigurationPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "iRacing Auto Settings", "Configuration.xml"); } }

        private static void InstallDefaultConfiguration()
        {
            var defaultConfiguration = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "iRacing Auto Settings", "Configuration.xml");
            if (File.Exists(defaultConfiguration) && !File.Exists(ConfigurationPath))
            {
                File.Copy(defaultConfiguration, ConfigurationPath);                
            }
        }
    }
}
