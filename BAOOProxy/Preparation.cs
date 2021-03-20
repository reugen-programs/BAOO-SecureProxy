using System;

namespace BAOOProxy
{
    class AppConfig
    {
        public string BAOInstallationFolder { get; set; }
        public bool BackupEnabled { get; set; }
        public int ProfileHistoryAmount { get; set; } = 100;
        public int InventoryHistoryAmount { get; set; } = 25;
    }
    class Preparation
    {
        private static AppConfig LoadConfig()
        {
            AppConfig AppConfig = null;
            if (!System.IO.Directory.Exists(Constants.FileData.ConfigPath))
            {
                System.IO.Directory.CreateDirectory(Constants.FileData.ConfigPath);
            }
            string ConfigFile = System.IO.Path.Combine(Constants.FileData.ConfigPath, "Config.json");
            if (System.IO.File.Exists(ConfigFile))
            {
                try
                {
                    AppConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<AppConfig>(System.IO.File.ReadAllText(ConfigFile));
                }
                catch
                { }
            }

            if (AppConfig == null)
            {
                using System.IO.StreamWriter SWriter = System.IO.File.CreateText(ConfigFile);
                Newtonsoft.Json.JsonSerializer Serializer = new()
                {
                    DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Include
                };
                AppConfig = new AppConfig();
                Serializer.Serialize(SWriter, AppConfig);
                Console.WriteLine("A new config file was created with default values.");
            }
            return AppConfig;
        }
        public static AppConfig FindAndSaveBAOPAth()
        {
            AppConfig AppConfig = LoadConfig();
            if (!string.IsNullOrEmpty(AppConfig.BAOInstallationFolder) && System.IO.File.Exists(System.IO.Path.Combine(AppConfig.BAOInstallationFolder, Constants.FileData.OnlineDefaultWBIDVarsPath)))
            {
                return AppConfig;
            }

            string SteamPath = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) ?? (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath", null);
            if (!string.IsNullOrEmpty(SteamPath))
            {
                // Do stuff
                string BAOPath = FindBAOPath(SteamPath);
                if (!string.IsNullOrEmpty(BAOPath))
                {
                    AppConfig.BAOInstallationFolder = BAOPath;
                }
                else
                {
                    try
                    {
                        string SteamLibraryFoldersFile = System.IO.Path.Combine(SteamPath, Constants.FileData.SteamLibraryFoldersFilePath);
                        if (System.IO.File.Exists(SteamLibraryFoldersFile))
                        {
                            Gameloop.Vdf.Linq.VProperty SteamLibraryFolders = Gameloop.Vdf.VdfConvert.Deserialize(System.IO.File.ReadAllText(SteamLibraryFoldersFile));
                            int i = 1;
                            bool StillGotFolders = true;
                            while (StillGotFolders)
                            {
                                string NextSteamPath = SteamLibraryFolders.Value[i.ToString()].ToString();
                                if (!string.IsNullOrEmpty(NextSteamPath))
                                {
                                    BAOPath = FindBAOPath(NextSteamPath);
                                    if (!string.IsNullOrEmpty(BAOPath))
                                    {
                                        AppConfig.BAOInstallationFolder = BAOPath;
                                        StillGotFolders = false;
                                    }
                                }
                                else
                                {
                                    StillGotFolders = false;
                                }
                                i++;
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                if (!string.IsNullOrEmpty(BAOPath))
                {
                    string ConfigFile = System.IO.Path.Combine(Constants.FileData.ConfigPath, "Config.json");
                    using System.IO.StreamWriter SWriter = System.IO.File.CreateText(ConfigFile);
                    Newtonsoft.Json.JsonSerializer Serializer = new()
                    {
                        DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Include
                    };
                    Serializer.Serialize(SWriter, AppConfig);
                }
            }

            if (string.IsNullOrEmpty(SteamPath) || string.IsNullOrEmpty(AppConfig.BAOInstallationFolder))
            {
                Console.WriteLine(@"I am so sorry but I couldn't find the Batman: Arkham Origins installation folder.
Please enter it manually in the Config file.
Example value: ""C:\\Program Files (x86)\\Steam\\steamapps\\common\\Batman Arkham Origins""
Press Enter to exit.");
                Console.ReadLine();
                Environment.Exit(0);
            }

            return AppConfig;
        }
        private static string FindBAOPath(string SteamPath)
        {
            string BAOPath = null;
            try
            {
                string BAOManifestFile = System.IO.Path.Combine(SteamPath, Constants.FileData.BAOManifestFilePath);
                if (System.IO.File.Exists(BAOManifestFile))
                {
                    Gameloop.Vdf.Linq.VProperty BAOManifest = Gameloop.Vdf.VdfConvert.Deserialize(System.IO.File.ReadAllText(BAOManifestFile));
                    if (!string.IsNullOrEmpty(BAOManifest.Value["installdir"].ToString()))
                    {
                        string Path = System.IO.Path.Combine(SteamPath, Constants.FileData.SteamGamesFolderPath, BAOManifest.Value["installdir"].ToString(), Constants.FileData.OnlineDefaultWBIDVarsPath);
                        if (System.IO.File.Exists(Path))
                        {
                            BAOPath = System.IO.Path.Combine(SteamPath, Constants.FileData.SteamGamesFolderPath, BAOManifest.Value["installdir"].ToString());
                        }
                    }
                }
            }
            catch
            {
            }

            if (string.IsNullOrEmpty(BAOPath))
            {
                //Try the common path
                string Path = System.IO.Path.Combine(SteamPath, Constants.FileData.SteamGamesFolderPath, "Batman Arkham Origins", Constants.FileData.OnlineDefaultWBIDVarsPath);
                if (System.IO.File.Exists(Path))
                {
                    BAOPath = System.IO.Path.Combine(SteamPath, Constants.FileData.SteamGamesFolderPath, "Batman Arkham Origins");
                }
            }
            return BAOPath;
        }
        public static void RerouteBAOOToProxy(AppConfig AppConfig, System.Net.EndPoint ProxyEndPoint)
        {
            try
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(AppConfig.BAOInstallationFolder, Constants.FileData.OnlineDefaultWBIDVarsPath), string.Format(Constants.NewData.SF_NewOnlineDefaultWBIDVarsContent, ProxyEndPoint.ToString()));
            }
            catch
            {
                throw;
            }
        }
    }
}
