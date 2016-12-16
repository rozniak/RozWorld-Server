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

using Oddmatics.RozWorld.API.Generic;
using Oddmatics.RozWorld.API.Generic.Event;
using Oddmatics.RozWorld.API.Generic.Game;
using Oddmatics.RozWorld.API.Server;
using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.API.Server.Entities;
using Oddmatics.RozWorld.API.Server.Event;
using Oddmatics.RozWorld.API.Server.Game;
using Oddmatics.RozWorld.API.Server.Game.Economy;
using Oddmatics.RozWorld.API.Server.Level;
using Oddmatics.RozWorld.Net.Packets;
using Oddmatics.RozWorld.Net.Packets.Event;
using Oddmatics.RozWorld.Net.Server;
using Oddmatics.RozWorld.Net.Server.Event;
using Oddmatics.RozWorld.Server.Accounts;
using Oddmatics.RozWorld.Server.Entities;
using Oddmatics.RozWorld.Server.Game;
using Oddmatics.RozWorld.Server.Properties;
using Oddmatics.Util.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;

namespace Oddmatics.RozWorld.Server
{
    public sealed class RwServer : IRwServer
    {
        #region Path Constants

        /// <summary>
        /// The accounts directory.
        /// </summary>
        public static string DIRECTORY_ACCOUNTS = Directory.GetCurrentDirectory() + @"\accounts";

        /// <summary>
        /// The level/worlds directory.
        /// </summary>
        public static string DIRECTORY_LEVEL = Directory.GetCurrentDirectory() + @"\level";

        /// <summary>
        /// The permissions directory.
        /// </summary>
        public static string DIRECTORY_PERMISSIONS = Directory.GetCurrentDirectory() + @"\permissions";

        /// <summary>
        /// The players directory.
        /// </summary>
        public static string DIRECTORY_PLAYERS = Directory.GetCurrentDirectory() + @"\players";
        
        /// <summary>
        /// The plugins directory.
        /// </summary>
        public static string DIRECTORY_PLUGINS = Directory.GetCurrentDirectory() + @"\plugins";


        /// <summary>
        /// The text file containing banned account names.
        /// </summary>
        public static string FILE_ACCOUNT_BANS = Directory.GetCurrentDirectory() + @"\namebans.txt";

        /// <summary>
        /// The text file containing whitelisted account names.
        /// </summary>
        public static string FILE_ACCOUNT_WHITELIST = Directory.GetCurrentDirectory() + @"\whitelist.txt";

        /// <summary>
        /// The text file containing trusted plugins.
        /// </summary>
        public static string FILE_TRUSTED_PLUGINS = Directory.GetCurrentDirectory() + @"\trustedplugins.txt";
        
        /// <summary>
        /// The config file for server variables.
        /// </summary>
        public static string FILE_CONFIG = Directory.GetCurrentDirectory() + @"\server.cfg";

        /// <summary>
        /// The text file containing banned ips.
        /// </summary>
        public static string FILE_IP_BANS = Directory.GetCurrentDirectory() + @"\ipbans.txt";

        /// <summary>
        /// The special argument trigger to pass 'default' options to a function.
        /// </summary>
        public static string SPECIAL_ARG_DEFAULT = ":default:";

        #endregion


        public IAccountsManager AccountsManager { get; private set; }
        public bool AutosaveEnabled { get; private set; }
        public long AutosaveInterval { get; private set; }
        public string BrowserName { get; private set; }
        public IList<string> Commands { get; private set; }
        public IContentManager ContentManager { get; private set; }
        public string DisplayName { get { return "Server"; } }
        public IEconomySystem EconomySystem { get; private set; }
        public string FormattedName { get { return FormattingString.Replace("%disp%", DisplayName); } }
        public string FormattingString { get; private set; }
        public Difficulty GameDifficulty { get; set; }
        public GameMode GameMode { get; private set; }
        public ushort HostingPort { get; private set; }
        public bool IsLocal { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsWhitelisted { get; private set; }
        private ILogger _Logger;
        public ILogger Logger { get { return _Logger; } set { if (_Logger == null) _Logger = value; } }
        public short MaxPlayers { get; private set; }
        public IList<Player> OnlinePlayers
        {
            get
            {
                var allPlayers = new List<Player>();
                allPlayers.AddRange(OnlineBotPlayers.Values);
                allPlayers.AddRange(OnlineRealPlayers.Values);
                return allPlayers.AsReadOnly();
            }
        }
        public IPermissionAuthority PermissionAuthority { get; private set; }
        private List<IPlugin> _Plugins;
        public IList<IPlugin> Plugins { get { return _Plugins.AsReadOnly(); } }
        public string RozWorldVersion { get { return "0.01"; } }
        public string ServerName { get { return "Vanilla RozWorld Server"; } }
        public string ServerVersion { get { return "0.01"; } }
        public string SpawnWorldName { get; private set; }
        public IStatCalculator StatCalculator { get; private set; }
        public byte TickRate { get { return (byte)GameTime.Interval; } }
        private List<string> _WhitelistedPlayers;
        public IList<string> WhitelistedPlayers { get { return _WhitelistedPlayers.AsReadOnly(); } }


        public event AccountSignUpEventHandler AccountSignUp;
        public event EventHandler FatalError;
        public event EventHandler Pause;
        public event PlayerChatEventHandler PlayerChatting;
        public event PlayerCommandEventHandler PlayerCommanding;
        public event PlayerLogInEventHandler PlayerLogIn;
        public event EventHandler Started;
        public event EventHandler Starting;
        public event EventHandler Stopped;
        public event EventHandler Stopping;
        public event ServerTickEventHandler Tick;


        private Dictionary<string, string> AccountNameFromDisplay;
        private List<string> BannedAccountNames;
        private List<IPAddress> BannedIPs;
        private ChatHookCallback ChatHook;
        private bool ChatHooked { get { return ChatHook != null; } }
        private int ChatHookToken;
        private string[] CompatibleServerNames = new string[] { "vanilla", "*" };
        private ushort CompatibleVanillaVersion = 1;
        public string CurrentPluginLoading { get; private set; }
        private Timer GameTime;
        public bool HasStarted { get; private set; }
        private Dictionary<string, Command> InstalledCommands;
        private Dictionary<string, RwPlayer> OnlineBotPlayers;
        private Dictionary<string, RwPlayer> OnlineRealPlayers;
        public Random Random { get; private set; }
        private long SinceLastAutosave = 0;
        private string SpawnWorldGenerator = String.Empty;
        private string SpawnWorldGeneratorOptions = String.Empty;
        private TrustedPluginCheckCallback _TrustedPluginCheck;
        public TrustedPluginCheckCallback TrustedPluginCheck { get { return _TrustedPluginCheck; } set { if(_TrustedPluginCheck == null) _TrustedPluginCheck = value; } }
        private RwUdpServer UdpServer;
        public SessionInfo UdpSessionInfo { get { return UdpServer.SessionInfo; } }


        /// <summary>
        /// Sends a message to all players connected to this RwServer.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void BroadcastMessage(string message)
        {
            if (HasStarted)
            {
                Logger.Out("[CHAT] " + message, LogLevel.Info);

                foreach (Player player in OnlinePlayers)
                {
                    player.SendMessage(message);
                }
            }
        }

        /// <summary>
        /// Passes in and handles a message/command from the console.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        /// <returns>True if the message was handled successfully.</returns>
        public bool Do(string message)
        {
            if (ChatHooked)
                return ChatHook(this, message);

            if (message.StartsWith("/") && message.Length > 1)
                return SendCommand(this, message.Substring(1));
            else
                Logger.Out("[CMD] Unknown command.", LogLevel.Error);

            return false;
        }

        /// <summary>
        /// Drops a player from the game.
        /// </summary>
        /// <param name="player">The instance of the Player to drop.</param>
        private void DropPlayer(Player player)
        {
            ((RwPlayer)player).Save();
            OnlineRealPlayers.Remove(player.Account.Username.ToLower());
            AccountNameFromDisplay.Remove(player.DisplayName.ToLower());
        }

        /// <summary>
        /// Implementation of IRwServer.GetCommandDescription.
        /// </summary>
        public string GetCommandDescription(string command)
        {
            string realCmd = command.ToLower();

            if (InstalledCommands.ContainsKey(realCmd))
                return InstalledCommands[realCmd].Description;

            return String.Empty;
        }

        /// <summary>
        /// Implementation of IRwServer.GetCommandsByPlugin.
        /// </summary>
        public IList<string> GetCommandsByPlugin(string plugin)
        {
            var commands = new List<string>();

            foreach (var registeredCommand in InstalledCommands)
            {
                string commandString = registeredCommand.Key;
                Command command = registeredCommand.Value;

                if (command.PluginRegistrar.EqualsIgnoreCase(plugin))
                    commands.Add(commandString);
            }

            return commands.AsReadOnly();
        }

        /// <summary>
        /// Implementation of IRwServer.GetCommandUsage.
        /// </summary>
        public string GetCommandUsage(string command)
        {
            string realCmd = command.ToLower();

            if (InstalledCommands.ContainsKey(realCmd))
                return InstalledCommands[realCmd].Usage;

            return String.Empty;
        }

        /// <summary>
        /// Implementation of IRwServer.GetPlayer.
        /// </summary>
        public Player GetPlayer(string name)
        {
            Player player = null;
            string realName = name.ToLower();

            if (AccountNameFromDisplay.ContainsKey(realName))
            {
                player = GetPlayerByUsername(AccountNameFromDisplay[realName]);

                // If the key isn't found, there's a syncing error
                // (Account name recorded, yet the player isn't on the server)
                if (player == null)
                    AccountNameFromDisplay.Remove(realName);
            }

            return player;
        }

        /// <summary>
        /// Implementation of IRwServer.GetPlayerAbsolute.
        /// </summary>
        public Player GetPlayerAbsolute(string name)
        {
            return null; // TODO: code this
        }

        /// <summary>
        /// Implementation of IRwServer.GetPlayerByUsername.
        /// </summary>
        public Player GetPlayerByUsername(string username)
        {
            string realUsername = username.ToLower();

            if (OnlineRealPlayers.ContainsKey(realUsername))
                return OnlineRealPlayers[realUsername];

            if (OnlineBotPlayers.ContainsKey(realUsername))
                return OnlineBotPlayers[realUsername];

            return null;
        }

        /// <summary>
        /// Implementation of IRwServer.GetPlayerByUsernameAbs.
        /// </summary>
        public Player GetPlayerByUsernameAbs(string username)
        {
            return null; // TODO: code this
        }

        /// <summary>
        /// Implementation of IRwServer.GetWorld.
        /// </summary>
        public IWorld GetWorld(string name)
        {
            return null; // TODO: code this
        }

        /// <summary>
        /// Implementation of ICommandCaller.HasPermission.
        /// </summary>
        public bool HasPermission(string key)
        {
            return true; // Server has full permissions
        }

        /// <summary>
        /// Implementation of ICommandCaller.HookChatToCallback.
        /// </summary>
        public int HookChatToCallback(ChatHookCallback callback)
        {
            if (!ChatHooked)
            {
                ChatHook = callback;
                ChatHookToken = Random.Next(1, int.MaxValue);
                return ChatHookToken;
            }

            return -1;
        }

        /// <summary>
        /// Implementation of IRwServer.IsValidEntity.
        /// </summary>
        public bool IsValidEntity(ushort id)
        {
            return false; // TODO: code this
        }

        /// <summary>
        /// Implementation of IRwServer.Kick.
        /// </summary>
        public bool Kick(Player player, string reason = "")
        {
            if (player != null)
            {
                ((RwPlayer)player).Disconnect(DisconnectReason.KICKED);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Implementation of IRwServer.Kick.
        /// </summary>
        public bool Kick(string name, string reason = "")
        {
            return Kick(GetPlayer(name), reason);
        }

        /// <summary>
        /// Loads a server configuration from a file on disk.
        /// </summary>
        /// <param name="file">The configuration file to load from.</param>
        private void LoadConfigs(IList<string> file)
        {
            var configs = FileSystem.ReadINIToDictionary(file);

            foreach (var item in configs)
            {
                switch (item.Key.ToLower())
                {
                        // Server specific options
                    case "autosave-enabled":
                        AutosaveEnabled = StringConversion.ToBool(item.Value, true, AutosaveEnabled);
                        break;

                    case "autosave-interval":
                        AutosaveInterval = StringConversion.ToLong(item.Value, true, AutosaveInterval);
                        break;

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
                        GameTime.Interval = StringConversion.ToByte(item.Value, true, TickRate);
                        break;

                    case "whitelist":
                        IsWhitelisted = StringConversion.ToBool(item.Value, true, IsWhitelisted);
                        break;

                        // Chat related options
                    case "name-formatting":
                        FormattingString = item.Value;
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
                    case "enable-clans":
                        // TODO: put in clan manager
                        break;

                    case "max-clan-members":
                        // TODO: as above
                        break;

                    default: Logger.Out("Unknown var: \"" + item.Key + "\".", LogLevel.Error); continue;
                }
            }
        }

        /// <summary>
        /// Writes the default server configuration file to the disk.
        /// </summary>
        /// <param name="targetFile">The target filename to write to.</param>
        private void MakeDefaultConfigs(string targetFile)
        {
            FileSystem.PutTextFile(targetFile, new string[] { Properties.Resources.DefaultConfigs });
        }

        /// <summary>
        /// Implementation of IRwServer.RegisterCommand.
        /// </summary>
        public bool RegisterCommand(string cmd, CommandSentCallback func, string description, string usage)
        {
            if (HasStarted)
                throw new InvalidOperationException("RwServer.RegisterCommand: Cannot register commands " +
            "after the server has completed startup.");

            string realCmd = cmd.ToLower();

            if (new Regex(@"^\/*[a-z]+$").IsMatch(realCmd) &&
                !InstalledCommands.ContainsKey(realCmd))
            {
                var command = new Command(func, CurrentPluginLoading, description, usage);
                InstalledCommands.Add(realCmd, command);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Implementation of ICommandCaller.ReleaseChatHook.
        /// </summary>
        public bool ReleaseChatHook(int token)
        {
            if (ChatHooked && token == ChatHookToken)
            {
                ChatHook = null;
                ChatHookToken = 0;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Restarts this server instance.
        /// </summary>
        public void Restart()
        {
            // Restart here
        }

        /// <summary>
        /// Implementation of IRwServer.Save.
        /// </summary>
        public void Save()
        {
            // Save players
            foreach (Player player in OnlineRealPlayers.Values)
            {
                player.Save();
            }

            // Save permission information
            PermissionAuthority.Save();

            // TODO: Save worlds here and anything else that needs saving
        }

        /// <summary>
        /// Issues a command to the server as a specified ICommandCaller.
        /// </summary>
        /// <param name="sender">The sender of this command.</param>
        /// <param name="cmd">The command to issue.</param>
        /// <returns>True if the command was successfully issued.</returns>
        public bool SendCommand(ICommandCaller sender, string cmd)
        {
            try
            {
                Logger.Out("[CMD] " + sender.DisplayName + " issued command: /" + cmd,
                    LogLevel.Info, false);

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
                if (InstalledCommands.ContainsKey(realCmd))
                    InstalledCommands[realCmd].Delegate(sender, args.AsReadOnly());
                else
                {
                    Logger.Out("[CMD] Unknown command.", LogLevel.Error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Out("[CMD] An internal error occurred whilst running the issued command: " +
                    ex.Message + ";\n" + ex.StackTrace, LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Implements ICommandCaller.SendMessage.
        /// </summary>
        public void SendMessage(string message)
        {
            Logger.Out("[CHAT] " + message, LogLevel.Info);
        }

        /// <summary>
        /// Starts this server instance.
        /// </summary>
        public void Start()
        {
            // A logger must be set and this should be set as the current server in RwCore
            if (RwCore.Server != this)
                throw new InvalidOperationException("RwServer.Start: RwCore.Server must reference this server instance before calling Start().");

            if (Logger == null)
                throw new InvalidOperationException("RwServer.Start: An ILogger instance must be attached before calling Start().");

            if (TrustedPluginCheck == null)
                throw new InvalidOperationException("RwServer.Start: A TrustedPluginCheckCallback delegate must be attached before calling Start().");

            if (HasStarted)
                throw new InvalidOperationException("RwServer.Start: Server is already started.");

            Logger.Out("RozWorld server starting...", LogLevel.Info);
            Logger.Out("Initialising directories...", LogLevel.Info);

            FileSystem.MakeDirectory(DIRECTORY_ACCOUNTS);
            FileSystem.MakeDirectory(DIRECTORY_LEVEL);
            FileSystem.MakeDirectory(DIRECTORY_PERMISSIONS);
            FileSystem.MakeDirectory(DIRECTORY_PLAYERS);
            FileSystem.MakeDirectory(DIRECTORY_PLUGINS);


            // TODO: Improve this section - reduce alike code blocks

            // Load bans
            BannedAccountNames = new List<string>();
            BannedIPs = new List<IPAddress>();

            if (!File.Exists(FILE_ACCOUNT_BANS))
                File.Create(FILE_ACCOUNT_BANS);
            else
            {
                var accountNames = FileSystem.GetTextFile(FILE_ACCOUNT_BANS);

                foreach (string accountName in accountNames)
                {
                    if (accountName != null)
                    {
                        string realName = accountName.ToLower();

                        if (RwPlayer.ValidName(realName) &&
                            !BannedAccountNames.Contains(realName))
                            BannedAccountNames.Add(realName);
                    }
                }
            }

            if (!File.Exists(FILE_IP_BANS))
                File.Create(FILE_IP_BANS);
            else
            {
                var ips = FileSystem.GetTextFile(FILE_IP_BANS);

                foreach (string ipString in ips)
                {
                    IPAddress ip;

                    if (IPAddress.TryParse(ipString, out ip) &&
                        !BannedIPs.Contains(ip))
                        BannedIPs.Add(ip);
                }
            }

            // Load whitelists
            _WhitelistedPlayers = new List<string>();

            if (!File.Exists(FILE_ACCOUNT_WHITELIST))
                File.Create(FILE_ACCOUNT_WHITELIST);
            else
            {
                var accountNames = FileSystem.GetTextFile(FILE_ACCOUNT_WHITELIST);

                foreach (string accountName in accountNames)
                {
                    if (accountName != null)
                    {
                        string realName = accountName.ToLower();

                        if (RwPlayer.ValidName(realName) &&
                            !_WhitelistedPlayers.Contains(realName))
                            _WhitelistedPlayers.Add(realName);
                    }
                }
            }

            Logger.Out("Initialising systems...", LogLevel.Info);

            Random = new Random();
            AccountNameFromDisplay = new Dictionary<string, string>();
            OnlineBotPlayers = new Dictionary<string, RwPlayer>();
            OnlineRealPlayers = new Dictionary<string, RwPlayer>();
            AccountsManager = new RwAccountsManager();
            ContentManager = new RwContentManager();
            PermissionAuthority = new RwPermissionAuthority();
            InstalledCommands = new Dictionary<string, Command>();
            StatCalculator = new RwStatCalculator();
            GameTime = new Timer();
            GameTime.Elapsed += new ElapsedEventHandler(GameTime_Elapsed);


            // Permission group / auth stuff

            var permAuth = (RwPermissionAuthority)PermissionAuthority;
            permAuth.Load(); // Load perm groups
            ServerCommands.Register(); // Register commands and permissions for the server

            if (permAuth.DefaultGroup == null)
            {
                Logger.Out("No default group set, attempting to create a new default group.", LogLevel.Warning);

                IPermissionGroup group = permAuth.CreateNewGroup("default");

                if (group != null)
                {
                    group.IsDefault = true;

                    // TODO: Add default perm file to resources and use that instead
                    foreach (string perm in Resources.DefaultPermissions.Replace("\n", "").Split('\r'))
                    {
                        group.AddPermission(perm);
                    }

                    group.Save();
                }
                else
                {
                    Logger.Out("Failed to load/create default group - cannot proceed.", LogLevel.Fatal);

                    if (FatalError != null)
                        FatalError(this, EventArgs.Empty);

                    return;
                }
            }


            // Settings and plugins
            Logger.Out("Setting configs...", LogLevel.Info);

            if (!File.Exists(FILE_CONFIG))
                MakeDefaultConfigs(FILE_CONFIG);

            LoadConfigs(Properties.Resources.DefaultConfigs.Split('\n')); // Load defaults first!
            LoadConfigs(FileSystem.GetTextFile(FILE_CONFIG));

            Logger.Out("Loading plugins...", LogLevel.Info);

            _Plugins = new List<IPlugin>();

            Logger.Out("Detecting plugin files...", LogLevel.Info);

            // Load trusted plugins
            IList<string> allowedPluginFiles = File.Exists(FILE_TRUSTED_PLUGINS) ?
                FileSystem.GetTextFile(FILE_TRUSTED_PLUGINS) :
                new List<string>();

            List<string> pluginFilesToLoad = new List<string>();

            foreach (string file in Directory.GetFiles(DIRECTORY_PLUGINS, "*.dll"))
            {
                if (allowedPluginFiles.Contains(file))
                    pluginFilesToLoad.Add(file);
                else
                {
                    Logger.Out("Checking trust for " + file, LogLevel.Info);

                    if (TrustedPluginCheck(file))
                        pluginFilesToLoad.Add(file);
                }
            }

            // Save the trusted plugins to disk
            FileSystem.PutTextFile(FILE_TRUSTED_PLUGINS, pluginFilesToLoad.ToArray());

            // Now load the plugins
            var pluginClasses = new List<Type>();

            foreach (string file in pluginFilesToLoad)
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
                    Logger.Out("An error occurred trying to enumerate the types inside of the plugin \""
                        + Path.GetFileName(file) + "\", this plugin cannot be loaded. It may have been built for" +
                        " a different version of the RozWorld API.", LogLevel.Error);
                }
                catch (Exception ex)
                {
                    Logger.Out("An error occurred trying to load plugin \"" + Path.GetFileName(file) + "\", this " +
                        "plugin cannot be loaded. The exception that occurred reported the following:\n" +
                        ex.Message + "\nStack:\n" + ex.StackTrace, LogLevel.Error);
                }
            }

            foreach (var plugin in pluginClasses)
            {
                CurrentPluginLoading = plugin.Name;
                _Plugins.Add((IPlugin)Activator.CreateInstance(plugin));
            }

            if (Starting != null)
                Starting(this, EventArgs.Empty);

            // Store commands
            var commands = new List<string>(InstalledCommands.Keys);
            commands.Sort();
            Commands = commands.AsReadOnly();

            // Done loading plugins

            Logger.Out("Finished loading plugins!", LogLevel.Info);

            GameTime.Start(); // Start game timer

            // Load worlds here

            Logger.Out("Starting listener on UDP port " + HostingPort.ToString() + "...", LogLevel.Info);

            try
            {
                UdpServer = new RwUdpServer(HostingPort);
                UdpServer.ChatMessageReceived += new PacketEventHandler(UdpServer_ChatMessageReceived);
                UdpServer.ClientDropped += new ClientDropEventHandler(UdpServer_ClientDropped);
                UdpServer.InfoRequestReceived += new PacketEventHandler(UdpServer_InfoRequestReceived);
                UdpServer.LogInRequestReceived += new PacketEventHandler(UdpServer_LogInRequestReceived);
                UdpServer.SignUpRequestReceived += new PacketEventHandler(UdpServer_SignUpRequestReceived);
                UdpServer.Begin();
            }
            catch (SocketException socketEx)
            {
                Logger.Out("Failed to start listener - port unavailable.", LogLevel.Fatal);

                if (FatalError != null)
                    FatalError(this, EventArgs.Empty);

                return;
            }
            catch (Exception ex)
            {
                Logger.Out("Failed to start listener - Exception:\n" + ex.Message + "\nStack:\n" + ex.StackTrace,
                    LogLevel.Fatal);

                if (FatalError != null)
                    FatalError(this, EventArgs.Empty);

                return;
            }

            Logger.Out("Server done loading!", LogLevel.Info);

            if (Started != null)
                Started(this, EventArgs.Empty);

            Logger.Out("Hello! This is " + ServerName + " (version " + ServerVersion + ").", LogLevel.Info);

            HasStarted = true;
        }

        /// <summary>
        /// Stops this server instance.
        /// </summary>
        public void Stop()
        {
            // TODO: Finish this!

            Logger.Out("Server stopping...", LogLevel.Info);
            Logger.Out("Disconnecting clients...", LogLevel.Info);

            // TODO: Send disconnect packets here

            Logger.Out("Detaching listener...", LogLevel.Info);

            UdpServer.ChatMessageReceived -= UdpServer_ChatMessageReceived;
            UdpServer.ClientDropped -= UdpServer_ClientDropped;
            UdpServer.InfoRequestReceived -= UdpServer_InfoRequestReceived;
            UdpServer.LogInRequestReceived -= UdpServer_LogInRequestReceived;
            UdpServer.SignUpRequestReceived -= UdpServer_SignUpRequestReceived;

            // Stop RwUdpServer here

            Logger.Out("Stopping plugins...", LogLevel.Info);

            if (Stopping != null)
                Stopping(this, EventArgs.Empty);

            GameTime.Stop(); // Stop game timer

            Logger.Out("Saving server data...", LogLevel.Info);

            Save();

            if (Stopped != null)
                Stopped(this, EventArgs.Empty);

            HasStarted = false;
        }

        /// <summary>
        /// Implementation of IRwServer.ThrowFatalError.
        /// </summary>
        public void ThrowFatalError(string message)
        {
            Logger.Out("------", LogLevel.Fatal);
            Logger.Out("A fatal error has been thrown, the server will now shut down.", LogLevel.Fatal);
            Logger.Out("Message reported: " + message, LogLevel.Fatal);
            Logger.Out("------", LogLevel.Fatal);

            Stop();
        }

        /// <summary>
        /// Implementation of IRwServer.WorldAvailable.
        /// </summary>
        public bool WorldAvailable(string name)
        {
            return false; // TODO: code this
        }


        /// <summary>
        /// [Event] GameTimer timer ticked.
        /// </summary>
        private void GameTime_Elapsed(object sender, ElapsedEventArgs e)
        {
            // TODO: process game updates here
            if (AutosaveEnabled)
            {
                SinceLastAutosave += TickRate;

                if (SinceLastAutosave > AutosaveInterval)
                {
                    // TODO: Perform server save here
                    SinceLastAutosave = 0;
                }
            }

            if (Tick != null)
                Tick(this, new ServerTickEventArgs(TickRate));
        }

        /// <summary>
        /// [Event] UDP Server received a player chat message.
        /// </summary>
        private void UdpServer_ChatMessageReceived(object sender, PacketEventArgs e)
        {
            var chatPacket = (ChatPacket)e.Packet;

            RwPlayer player = (RwPlayer)GetPlayerByUsername(chatPacket.Username);

            if (player.ChatHooked)
            {
                player.ChatHook(player, chatPacket.Message);
                return;
            }

            if (chatPacket.Message.StartsWith("/") && chatPacket.Message.Length > 1)
            {
                string cmd = chatPacket.Message.Substring(1);
                var commandEventArgs = new PlayerCommandEventArgs(player, cmd);

                if (PlayerCommanding != null)
                    PlayerCommanding(this, commandEventArgs);

                if (!commandEventArgs.Cancel)
                    SendCommand(player, cmd);
            }
            else if (player.HasPermission("rwcore.say.self"))
            {
                var chatEventArgs = new PlayerChatEventArgs(player, chatPacket.Message, true);

                if (PlayerChatting != null)
                    PlayerChatting(this, chatEventArgs);

                if (!chatEventArgs.Cancel)
                {
                    string message = chatEventArgs.UseServerFormatting ?
                        player.FormattedName + " " + chatPacket.Message :
                        chatPacket.Message;

                    BroadcastMessage(message);
                }
            }
        }

        /// <summary>
        /// [Event] UDP Server dropped a remote client.
        /// </summary>
        private void UdpServer_ClientDropped(object sender, ClientDropEventArgs e)
        {
            IList<string> usernames = e.Client.Usernames;

            foreach (string user in usernames)
            {
                Player player = GetPlayerByUsername(user);
                Logger.Out("[DISC] " + player.DisplayName + " (" + player.Account.Username +
                    ") has been disconnected (client timeout).", LogLevel.Info);
                DropPlayer(player);
            }
        }

        /// <summary>
        /// [Event] UDP Server received a server information request packet.
        /// </summary>
        private void UdpServer_InfoRequestReceived(object sender, PacketEventArgs e)
        {
            var infoPacket = (ServerInfoRequestPacket)e.Packet;

            Logger.Out("[UDP] Server info request received by " + infoPacket.SenderEndPoint.ToString(),
                LogLevel.Info);

            // Client is compatible if the server implemention matches and either the client isn't vanilla or if it is
            // vanilla, it must be the compatible version
            bool compatible = CompatibleServerNames.Contains(infoPacket.ServerImplementation.ToLower()) &&
                (!infoPacket.ClientImplementation.EqualsIgnoreCase("vanilla") ||
                    infoPacket.ClientVersionRaw == CompatibleVanillaVersion);

            UdpServer.Send(new ServerInfoResponsePacket(compatible, MaxPlayers, (short)OnlinePlayers.Count, "Vanilla",
                BrowserName), infoPacket.SenderEndPoint);
        }

        /// <summary>
        /// [Event] UDP Server received a log in request.
        /// </summary>
        private void UdpServer_LogInRequestReceived(object sender, PacketEventArgs e)
        {
            var logInPacket = (LogInRequestPacket)e.Packet;
            string realUsername = logInPacket.Username.ToLower();
            byte result = ErrorMessage.INTERNAL_ERROR; // Default to generic error, on success this will be replaced

            Logger.Out("[UDP] Log in request received by " + logInPacket.SenderEndPoint.ToString(),
                LogLevel.Info);

            // Check bans and whitelist status
            if (BannedIPs.Contains(logInPacket.SenderEndPoint.Address) ||
                BannedAccountNames.Contains(realUsername))
                result = ErrorMessage.BANNED;
            else if (IsWhitelisted && !WhitelistedPlayers.Contains(realUsername))
                result = ErrorMessage.NOT_ON_WHITELIST;
            else if (logInPacket.ValidHashTime)
            {
                // TODO: Revise a LOT of this code to work with both bots and real users :)

                // Check if there's already a user (not a bot) logged in, kick them if they are
                if (OnlineRealPlayers.ContainsKey(realUsername))
                    Kick(GetPlayer(realUsername), "Another player has logged into this account on this server.");

                if (!OnlineBotPlayers.ContainsKey(realUsername))
                {
                    var account = new RwAccount(logInPacket.Username);
                    result = account.LogIn(logInPacket.PasswordHash, logInPacket.UtcHashTime);

                    if (result == ErrorMessage.NO_ERROR)
                    {
                        UdpServer.AddClient(realUsername, logInPacket.SenderEndPoint); // Attempt to add the client
                        ConnectedClient client = UdpServer.GetConnectedClient(logInPacket.SenderEndPoint);

                        if (client != null) // Everything was successful
                        {
                            // Add the player to the list and update dictionaries
                            RwPlayer player = account.InstatePlayerInstance(client);
                            OnlineRealPlayers.Add(realUsername, player);
                            AccountNameFromDisplay.Add(player.DisplayName.ToLower(), realUsername);

                            if (PlayerLogIn != null)
                                PlayerLogIn(this, new PlayerLogInEventArgs(player));

                            Logger.Out("[LOGIN] Player '" + logInPacket.Username + "' has logged on! " +
                                "(from " + logInPacket.SenderEndPoint.ToString() + ")", LogLevel.Info);
                        }
                        else
                        {
                            // Something odd happened
                            Logger.Out("[LOGIN] Something strange occurred during log in, connected client" +
                                " instance could not be instated.", LogLevel.Error);
                            result = ErrorMessage.INTERNAL_ERROR; // Update error message since it was a failure
                        }
                    }
                }
                else
                    result = ErrorMessage.ACCOUNT_NAME_TAKEN; // Bot rules over everything
            }
            else
                result = ErrorMessage.HASHTIME_INVALID;

            UdpServer.Send(new LogInResponsePacket(result == ErrorMessage.NO_ERROR, logInPacket.Username,
                result), logInPacket.SenderEndPoint);
        }

        /// <summary>
        /// [Event] UDP Server received a sign up request.
        /// </summary>
        private void UdpServer_SignUpRequestReceived(object sender, PacketEventArgs e)
        {
            var signUpPacket = (SignUpRequestPacket)e.Packet;
            string realUsername = signUpPacket.Username.ToLower();
            byte result;

            Logger.Out("[UDP] Sign up request received by " + signUpPacket.SenderEndPoint.ToString(),
                LogLevel.Info);

            // Check bans
            if (BannedIPs.Contains(signUpPacket.SenderEndPoint.Address) ||
                BannedAccountNames.Contains(realUsername))
                result = ErrorMessage.BANNED;
            else if (IsWhitelisted && !WhitelistedPlayers.Contains(realUsername))
                result = ErrorMessage.NOT_ON_WHITELIST;
            else
                result = ((RwAccountsManager)AccountsManager).CreateAccount(signUpPacket.Username,
                    signUpPacket.PasswordHash, signUpPacket.SenderEndPoint.Address);

            if (result == ErrorMessage.NO_ERROR)
            {
                if (AccountSignUp != null)
                    AccountSignUp(this, new AccountSignUpEventArgs(signUpPacket.SenderEndPoint.Address,
                        signUpPacket.Username));

                Logger.Out("[SIGN] Account sign up complete for username '" + signUpPacket.Username + "' from " +
                    signUpPacket.SenderEndPoint.ToString() + ".", LogLevel.Info);
            }
            else
                Logger.Out("[SIGN] Account sign up unsuccessful for username '" + signUpPacket.Username + "' from " +
                    signUpPacket.SenderEndPoint.ToString() + " - Error " + result.ToString() + ".", LogLevel.Error);

            UdpServer.Send(new SignUpResponsePacket(result == ErrorMessage.NO_ERROR, signUpPacket.Username, result),
                signUpPacket.SenderEndPoint);
        }
    }
}
