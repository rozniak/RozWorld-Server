/**
 * Oddmatics.RozWorld.Server.RwServer -- RozWorld Server Implementation
 *
 * This source-code is part of the server library for the RozWorld project by rozza of Oddmatics:
 * <<http://www.oddmatics.uk>>
 * <<http://roz.world>>
 * <<http://github.com/rozniak/RozWorld-Server>>
 *
 * Sharing, editing and general licence term information can be found inside of the "LICENCE.MD" file that should be located in the root of this project's directory structure.
 */

using Oddmatics.RozWorld.API.Generic.Game;
using Oddmatics.RozWorld.API.Server;
using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.API.Server.Entities;
using Oddmatics.RozWorld.API.Server.Event;
using Oddmatics.RozWorld.API.Server.Game;
using Oddmatics.RozWorld.API.Server.Level;
using Oddmatics.RozWorld.Server.Accounts;
using Oddmatics.RozWorld.Server.Entities;
using Oddmatics.RozWorld.Server.Game;
using Oddmatics.Util.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Oddmatics.RozWorld.Server
{
    public class RwServer : IRwServer
    {
        #region Path Constants

        /// <summary>
        /// The root directory this library is active in.
        /// </summary>
        public static string DIRECTORY_CURRENT = Directory.GetCurrentDirectory();

        /// <summary>
        /// The accounts directory.
        /// </summary>
        public static string DIRECTORY_ACCOUNTS = DIRECTORY_CURRENT + @"\accounts";

        /// <summary>
        /// The permissions directory.
        /// </summary>
        public static string DIRECTORY_PERMISSIONS = DIRECTORY_CURRENT + @"\permissions";
        
        /// <summary>
        /// The plugins directory.
        /// </summary>
        public static string DIRECTORY_PLUGINS = DIRECTORY_CURRENT + @"\plugins";

        /// <summary>
        /// The level/worlds directory.
        /// </summary>
        public static string DIRECTORY_LEVEL = DIRECTORY_CURRENT + @"\level";

        

        /// <summary>
        /// The config file for server variables.
        /// </summary>
        public static string FILE_CONFIG = "server.cfg";

        #endregion

        public string BrowserName { get; private set; }
        public IContentManager ContentManager { get; private set; }
        public Difficulty GameDifficulty { get; set; }
        public GameMode GameMode { get; private set; }
        public ushort HostingPort { get; private set; }
        public bool IsLocal { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsWhitelisted { get; private set; }
        private ILogger _Logger;
        public ILogger Logger { get { return _Logger; } set { _Logger = _Logger == null ? value : _Logger; } }
        public short MaxPlayers { get; private set; }
        public IList<Player> OnlinePlayers { get { return new List<Player>().AsReadOnly(); } }
        public IPermissionAuthority PermissionAuthority { get; private set; }
        private List<IPlugin> _Plugins;
        public IList<IPlugin> Plugins { get { return _Plugins.AsReadOnly(); } }
        public string RozWorldVersion { get { return "0.01"; } }
        public string ServerName { get { return "Vanilla RozWorld Server"; } }
        public string ServerVersion { get { return "0.01"; } }
        public string SpawnWorldName { get; private set; }
        public IStatCalculator StatCalculator { get; private set; }
        public byte TickRate { get; private set; }
        private List<string> _WhitelistedPlayers;
        public IList<string> WhitelistedPlayers { get { return _WhitelistedPlayers.AsReadOnly(); } }

        public event EventHandler Pause;
        public event EventHandler Started;
        public event EventHandler Starting;
        public event EventHandler Stopped;
        public event EventHandler Stopping;
        public event EventHandler Tick;


        private Dictionary<string, CommandSentCallback> Commands;
        public string CurrentPluginLoading { get; private set; }
        public RwAccount ServerAccount { get; private set; }
        private string SpawnWorldGenerator = String.Empty;
        private string SpawnWorldGeneratorOptions = String.Empty;
        public bool HasStarted { get; private set; }


        /// <summary>
        /// Sends a message to all players connected to this RwServer.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void BroadcastMessage(string message)
        {
            if (HasStarted)
            {
                Logger.Out("[CHAT] " + message);

                foreach (Player player in OnlinePlayers)
                {
                    player.SendMessage(message);
                }
            }
        }

        public Player GetPlayer(string name)
        {
            return null; // TODO: code this
        }

        public Player GetPlayerAbsolute(string name)
        {
            return null; // TODO: code this
        }

        public IWorld GetWorld(string name)
        {
            return null; // TODO: code this
        }

        public bool IsValidEntity(ushort id)
        {
            return false; // TODO: code this
        }

        private void LoadConfigs(string configFile)
        {
            var configs = FileSystem.ReadINIToDictionary(configFile);

            foreach (var item in configs)
            {
                switch (item.Key.ToLower())
                {
                        // Server specific options
                    case "browser-name":
                        BrowserName = item.Value;
                        break;

                    case "host-port":
                        HostingPort = StringConversion.ToUShort(item.Value, true, HostingPort); 
                        break;

                    case "max-players":
                        MaxPlayers = StringConversion.ToShort(item.Value, true, MaxPlayers);
                        break;

                    case "tick-rate":
                        TickRate = StringConversion.ToByte(item.Value, true, TickRate);
                        break;

                    case "whitelist":
                        IsWhitelisted = StringConversion.ToBool(item.Value, true, IsWhitelisted);
                        break;

                        // World generation options
                    case "generator":
                        SpawnWorldGenerator = item.Value;
                        break;

                    case "generator-options":
                        SpawnWorldGeneratorOptions = item.Value;
                        break;

                        // Game settings
                    case "game-mode":
                        switch(item.Value.ToLower())
                        {
                            case "adventure": GameMode = GameMode.Adventure; break;
                            case "books": 
                            default:
                                GameMode = GameMode.Books; break;
                        }
                        break;

                    case "difficulty":
                        GameDifficulty = (Difficulty)StringConversion.ToByte(item.Value, true, (byte)GameDifficulty);
                        break;

                        // Player settings
                    case "default-group":
                        PermissionAuthority.DefaultGroupName = item.Value;
                        break;

                    case "enable-clans":
                        // TODO: put in clan manager
                        break;

                    case "max-clan-members":
                        // TODO: as above
                        break;

                    default: Logger.Out("Unknown var: \"" + item.Key + "\"."); continue;
                }
            }
        }

        private void MakeDefaultConfigs(string targetFile)
        {
            FileSystem.PutTextFile(targetFile, new string[] { Properties.Resources.DefaultConfigs });
        }

        public bool RegisterCommand(string cmd, CommandSentCallback func)
        {
            string realCmd = cmd.ToLower();

            if (!Commands.ContainsKey(realCmd))
            {
                Commands.Add(realCmd, func);
                return true;
            }

            return false;
        }

        public void Restart()
        {
            // Restart here
        }

        public bool SendCommand(RwAccount sender, string cmd)
        {
            try
            {
                Logger.Out("[CMD] " + sender.Username + " issued command: " + cmd);

                var args = new List<string>();
                string[] cmdSplit = cmd.Split();
                string realCmd = cmdSplit[0].ToLower();

                if (cmdSplit.Length > 1)
                {
                    for (int i = 1; i < cmdSplit.Length; i++)
                    {
                        args.Add(cmdSplit[i]);
                    }
                }

                // Call the attached command delegate - commands are all lowercase
                Commands[realCmd](sender, args.AsReadOnly());

                return true;
            }
            catch (KeyNotFoundException ex)
            {
                Logger.Out("[ERR] Unknown command.");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Out("[ERR] An internal error occurred whilst running the issued command: " +
                    ex.Message + ";\n" + ex.StackTrace);
                return false;
            }
        }

        public void Start()
        {
            // Start here - a logger is required
            if (Logger != null)
            {
                Logger.Out("[STAT] RozWorld server starting...");
                Logger.Out("[STAT] Initialising directories...");

                FileSystem.MakeDirectory(DIRECTORY_ACCOUNTS);
                FileSystem.MakeDirectory(DIRECTORY_LEVEL);
                FileSystem.MakeDirectory(DIRECTORY_PERMISSIONS);
                FileSystem.MakeDirectory(DIRECTORY_PLUGINS);

                Logger.Out("[STAT] Initialising systems...");

                Commands = new Dictionary<string, CommandSentCallback>();
                StatCalculator = new RwStatCalculator();
                ServerAccount = new RwAccount("server"); // Create the server account (max privileges)
                PermissionAuthority = new RwPermissionAuthority();
                ContentManager = new RwContentManager();

                ServerCommands.Register(); // Register commands and permissions for the server

                Logger.Out("[STAT] Setting configs...");

                string configPath = DIRECTORY_CURRENT + "\\" + FILE_CONFIG;

                if (!File.Exists(configPath))
                    MakeDefaultConfigs(configPath);

                LoadConfigs(configPath);

                Logger.Out("[STAT] Loading plugins...");

                _Plugins = new List<IPlugin>();

                // Load plugins here
                var pluginClasses = new List<Type>();

                foreach (string file in Directory.GetFiles(DIRECTORY_PLUGINS, "*.dll"))
                {
                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(file);
                        Type[] detectedObjects = assembly.GetTypes();

                        foreach (var detectedObject in detectedObjects)
                        {
                            if (detectedObject.GetInterface("IPlugin") == typeof(IPlugin))
                                pluginClasses.Add(detectedObject);
                        }
                    }
                    catch (ReflectionTypeLoadException reflectionEx)
                    {
                        Logger.Out("[ERR] An error occurred trying to enumerate the types inside of the plugin \""
                            + Path.GetFileName(file) + "\", this plugin cannot be loaded. It may have been built for" +
                            " a different version of the RozWorld API.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Out("[ERR] An error occurred trying to load plugin \"" + Path.GetFileName(file) + "\", this " +
                            "plugin cannot be loaded. The exception that occurred reported the following:\n" +
                            ex.Message + "\nStack:\n" + ex.StackTrace);
                    }
                }

                foreach (var plugin in pluginClasses)
                {
                    CurrentPluginLoading = plugin.Name;
                    _Plugins.Add((IPlugin)Activator.CreateInstance(plugin));
                }

                if (Starting != null)
                    Starting(this, EventArgs.Empty);

                // Done loading plugins

                Logger.Out("[STAT] Finished loading plugins!");

                // Load worlds here

                Logger.Out("[STAT] Server done loading!");

                if (Started != null)
                    Started(this, EventArgs.Empty);

                Logger.Out("[STAT] Hello! This is " + ServerName + " (version " + ServerVersion + ").");

                HasStarted = true;
            }
            else
                throw new InvalidOperationException("An ILogger instance must be attached before calling Start().");
        }

        public void Stop()
        {
            // Stop here
        }

        public bool WorldAvailable(string name)
        {
            return false; // TODO: code this
        }
    }
}
