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
using Oddmatics.RozWorld.Server.Accounts;
using Oddmatics.RozWorld.Server.Entities;
using Oddmatics.RozWorld.Server.Game;
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

namespace Oddmatics.RozWorld.Server
{
    public class RwServer : IRwServer
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
        public static string FILE_ACCOUNT_BANS = Directory.GetCurrentDirectory() + "\\namebans.txt";
        
        /// <summary>
        /// The config file for server variables.
        /// </summary>
        public static string FILE_CONFIG = Directory.GetCurrentDirectory() + "\\server.cfg";

        /// <summary>
        /// The text file containing banned ips.
        /// </summary>
        public static string FILE_IP_BANS = Directory.GetCurrentDirectory() + "\\ipbans.txt";

        #endregion

        public IAccountsManager AccountsManager { get; private set; }
        public string BrowserName { get; private set; }
        public IList<string> Commands { get; private set; }
        public IContentManager ContentManager { get; private set; }
        public string DisplayName { get { return "Server"; } }
        public IEconomySystem EconomySystem { get; private set; }
        public string FormattedName { get { return "<" + DisplayName + ">"; } } // TODO: Make this support config-loaded formatting
        public Difficulty GameDifficulty { get; set; }
        public GameMode GameMode { get; private set; }
        public ushort HostingPort { get; private set; }
        public bool IsLocal { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsWhitelisted { get; private set; }
        private ILogger _Logger;
        public ILogger Logger { get { return _Logger; } set { _Logger = _Logger == null ? value : _Logger; } }
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
        public byte TickRate { get; private set; }
        private List<string> _WhitelistedPlayers;
        public IList<string> WhitelistedPlayers { get { return _WhitelistedPlayers.AsReadOnly(); } }


        public event AccountSignUpEventHandler AccountSignUp;
        public event EventHandler FatalError;
        public event EventHandler Pause;
        public event PlayerChatEventHandler PlayerChatting;
        public event PlayerLogInEventHandler PlayerLogIn;
        public event EventHandler Started;
        public event EventHandler Starting;
        public event EventHandler Stopped;
        public event EventHandler Stopping;
        public event ServerTickEventHandler Tick;


        private Dictionary<string, string> AccountNameFromDisplay;
        private List<string> BannedAccountNames;
        private List<IPAddress> BannedIPs;
        private Dictionary<string, Command> InstalledCommands;
        private string[] CompatibleServerNames = new string[] { "vanilla", "*" };
        private ushort CompatibleVanillaVersion = 1;
        public string CurrentPluginLoading { get; private set; }
        public bool HasStarted { get; private set; }
        private Dictionary<string, RwPlayer> OnlineRealPlayers;
        private Dictionary<string, RwPlayer> OnlineBotPlayers;
        private string SpawnWorldGenerator = String.Empty;
        private string SpawnWorldGeneratorOptions = String.Empty;
        private RwUdpServer UdpServer;


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

        public string GetCommandDescription(string command)
        {
            string realCmd = command.ToLower();

            if (InstalledCommands.ContainsKey(realCmd))
                return InstalledCommands[realCmd].Description;

            return String.Empty;
        }

        public IList<string> GetCommandsByPlugin(string plugin)
        {
            return new List<string>().AsReadOnly(); // TODO: code this
        }

        public string GetCommandUsage(string command)
        {
            string realCmd = command.ToLower();

            if (InstalledCommands.ContainsKey(realCmd))
                return InstalledCommands[realCmd].Usage;

            return String.Empty;
        }

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

        public Player GetPlayerAbsolute(string name)
        {
            return null; // TODO: code this
        }

        public Player GetPlayerByUsername(string username)
        {
            string realUsername = username.ToLower();

            if (OnlineRealPlayers.ContainsKey(realUsername))
                return OnlineRealPlayers[realUsername];

            if (OnlineBotPlayers.ContainsKey(realUsername))
                return OnlineBotPlayers[realUsername];

            return null;
        }

        public Player GetPlayerByUsernameAbs(string username)
        {
            return null; // TODO: code this
        }

        public IWorld GetWorld(string name)
        {
            return null; // TODO: code this
        }

        public bool HasPermission(string key)
        {
            return true; // Server has full permissions
        }

        public bool IsValidEntity(ushort id)
        {
            return false; // TODO: code this
        }

        public bool Kick(Player player, string reason = "")
        {
            if (player != null)
            {
                ((RwPlayer)player).Disconnect(DisconnectReason.KICKED);
                return true;
            }

            return false;
        }

        public bool Kick(string name, string reason = "")
        {
            return Kick(GetPlayer(name), reason);
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

        public void Restart()
        {
            // Restart here
        }

        public bool SendCommand(ICommandCaller sender, string cmd)
        {
            try
            {
                Logger.Out("[CMD] " + sender.DisplayName + " issued command: /" + cmd, false);

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
                    Logger.Out("[ERR] Unknown command.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Out("[ERR] An internal error occurred whilst running the issued command: " +
                    ex.Message + ";\n" + ex.StackTrace);
                return false;
            }
        }

        public void SendMessage(string message)
        {
            Logger.Out(message);
        }

        public void Start()
        {
            // A logger must be set and this should be set as the current server in RwCore
            if (RwCore.Server != this)
                throw new InvalidOperationException("RwServer.Start: RwCore.Server must reference this server instance before calling Start().");

            if (Logger == null)
                throw new InvalidOperationException("RwServer.Start: An ILogger instance must be attached before calling Start().");

            if (HasStarted)
                throw new InvalidOperationException("RwServer.Start: Server is already started.");

            Logger.Out("[STAT] RozWorld server starting...");
            Logger.Out("[STAT] Initialising directories...");

            FileSystem.MakeDirectory(DIRECTORY_ACCOUNTS);
            FileSystem.MakeDirectory(DIRECTORY_LEVEL);
            FileSystem.MakeDirectory(DIRECTORY_PERMISSIONS);
            FileSystem.MakeDirectory(DIRECTORY_PLAYERS);
            FileSystem.MakeDirectory(DIRECTORY_PLUGINS);

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
                    if (accountName != null &&
                        RwPlayer.ValidName(accountName) &&
                        !BannedAccountNames.Contains(accountName))
                        BannedAccountNames.Add(accountName);
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

            Logger.Out("[STAT] Initialising systems...");

            AccountNameFromDisplay = new Dictionary<string, string>();
            OnlineBotPlayers = new Dictionary<string, RwPlayer>();
            OnlineRealPlayers = new Dictionary<string, RwPlayer>();
            AccountsManager = new RwAccountsManager();
            ContentManager = new RwContentManager();
            PermissionAuthority = new RwPermissionAuthority();
            InstalledCommands = new Dictionary<string, Command>();
            StatCalculator = new RwStatCalculator();

            ((RwPermissionAuthority)PermissionAuthority).Load(); // Load perm groups
            ServerCommands.Register(); // Register commands and permissions for the server

            Logger.Out("[STAT] Setting configs...");

            if (!File.Exists(FILE_CONFIG))
                MakeDefaultConfigs(FILE_CONFIG);

            LoadConfigs(FILE_CONFIG);

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

            // Store commands
            var commands = new List<string>(InstalledCommands.Keys);
            commands.Sort();
            Commands = commands.AsReadOnly();

            // Done loading plugins

            Logger.Out("[STAT] Finished loading plugins!");

            // Load worlds here

            Logger.Out("[STAT] Starting listener on UDP port " + HostingPort.ToString() + "...");

            try
            {
                UdpServer = new RwUdpServer(HostingPort);
                UdpServer.ChatMessageReceived += new PacketEventHandler(UdpServer_ChatMessageReceived);
                UdpServer.InfoRequestReceived += new PacketEventHandler(UdpServer_InfoRequestReceived);
                UdpServer.LogInRequestReceived += new PacketEventHandler(UdpServer_LogInRequestReceived);
                UdpServer.SignUpRequestReceived += new PacketEventHandler(UdpServer_SignUpRequestReceived);
                UdpServer.Begin();
            }
            catch (SocketException socketEx)
            {
                Logger.Out("[ERR] Failed to start listener - port unavailable.");

                if (FatalError != null)
                    FatalError(this, EventArgs.Empty);

                return;
            }
            catch (Exception ex)
            {
                Logger.Out("[ERR] Failed to start listener - Exception:\n" + ex.Message + "\nStack:\n" + ex.StackTrace);

                if (FatalError != null)
                    FatalError(this, EventArgs.Empty);

                return;
            }

            Logger.Out("[STAT] Server done loading!");

            if (Started != null)
                Started(this, EventArgs.Empty);

            Logger.Out("[STAT] Hello! This is " + ServerName + " (version " + ServerVersion + ").");

            HasStarted = true;
        }

        public void Stop()
        {
            // TODO: Finish this!

            Logger.Out("[STAT] Server stopping...");

            // TODO: Send disconnect packets here

            UdpServer.ChatMessageReceived -= UdpServer_ChatMessageReceived;
            UdpServer.InfoRequestReceived -= UdpServer_InfoRequestReceived;
            UdpServer.LogInRequestReceived -= UdpServer_LogInRequestReceived;
            UdpServer.SignUpRequestReceived -= UdpServer_SignUpRequestReceived;

            // Stop RwUdpServer here

            if (Stopping != null)
                Stopping(this, EventArgs.Empty);

            // TODO: Save world and stuff

            if (Stopped != null)
                Stopped(this, EventArgs.Empty);

            HasStarted = false;
        }

        public bool WorldAvailable(string name)
        {
            return false; // TODO: code this
        }


        private void UdpServer_ChatMessageReceived(object sender, PacketEventArgs e)
        {
            var chatPacket = (ChatPacket)e.Packet;

            Player player = GetPlayerByUsername(chatPacket.Username);

            if (chatPacket.Message.StartsWith("/") && chatPacket.Message.Length > 1)
                SendCommand(player, chatPacket.Message.Substring(1));
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

        private void UdpServer_InfoRequestReceived(object sender, PacketEventArgs e)
        {
            var infoPacket = (ServerInfoRequestPacket)e.Packet;

            Logger.Out("[UDP] Server info request received by " + infoPacket.SenderEndPoint.ToString());

            // Client is compatible if the server implemention matches and either the client isn't vanilla or if it is
            // vanilla, it must be the compatible version
            bool compatible = CompatibleServerNames.Contains(infoPacket.ServerImplementation.ToLower()) &&
                (!infoPacket.ClientImplementation.EqualsIgnoreCase("vanilla") ||
                    infoPacket.ClientVersionRaw == CompatibleVanillaVersion);

            UdpServer.Send(new ServerInfoResponsePacket(compatible, MaxPlayers, (short)OnlinePlayers.Count, "Vanilla",
                BrowserName), infoPacket.SenderEndPoint);
        }

        private void UdpServer_LogInRequestReceived(object sender, PacketEventArgs e)
        {
            var logInPacket = (LogInRequestPacket)e.Packet;
            string realUsername = logInPacket.Username.ToLower();
            byte result = ErrorMessage.INTERNAL_ERROR; // Default to generic error, on success this will be replaced

            Logger.Out("[UDP] Log in request received by " + logInPacket.SenderEndPoint.ToString());

            // Check bans
            if (BannedIPs.Contains(logInPacket.SenderEndPoint.Address) ||
                BannedAccountNames.Contains(logInPacket.Username))
                result = ErrorMessage.BANNED;
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

                            Logger.Out("[STAT/LOGIN] Player '" + logInPacket.Username + "' has logged on! " +
                                "(from " + logInPacket.SenderEndPoint.ToString() + ")");
                        }
                        else
                        {
                            // Something odd happened
                            Logger.Out("[ERR] Something strange occurred during log in, connected client instance could not be instated.");
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

        private void UdpServer_SignUpRequestReceived(object sender, PacketEventArgs e)
        {
            var signUpPacket = (SignUpRequestPacket)e.Packet;
            byte result;

            Logger.Out("[UDP] Sign up request received by " + signUpPacket.SenderEndPoint.ToString());

            // Check bans
            if (BannedIPs.Contains(signUpPacket.SenderEndPoint.Address) ||
                BannedAccountNames.Contains(signUpPacket.Username))
                result = ErrorMessage.BANNED;
            else
                result = ((RwAccountsManager)AccountsManager).CreateAccount(signUpPacket.Username,
                    signUpPacket.PasswordHash, signUpPacket.SenderEndPoint.Address);

            if (result == ErrorMessage.NO_ERROR)
                Logger.Out("[STAT] Account sign up complete for username '" + signUpPacket.Username +
                    "' from " + signUpPacket.SenderEndPoint.ToString() + ".");
            else
                Logger.Out("[STAT] Account sign up unsuccessful for username '" + signUpPacket.Username +
                    "' from " + signUpPacket.SenderEndPoint.ToString() + " - Error " + result.ToString() +
                    ".");

            UdpServer.Send(new SignUpResponsePacket(result == ErrorMessage.NO_ERROR, signUpPacket.Username, result),
                signUpPacket.SenderEndPoint);
        }
    }
}
