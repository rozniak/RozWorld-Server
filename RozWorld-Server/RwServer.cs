using Oddmatics.RozWorld.API.Server;
using Oddmatics.RozWorld.API.Server.Entity;
using Oddmatics.RozWorld.API.Server.Event;
using Oddmatics.RozWorld.Server.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Oddmatics.Util.IO;

namespace Oddmatics.RozWorld.Server
{
    public class RwServer : IRwServer
    {
        // Constants
        public static string DIRECTORY_CURRENT = Directory.GetCurrentDirectory();
        public static string DIRECTORY_PLUGINS = DIRECTORY_CURRENT + @"\plugins";
        public static string DIRECTORY_LEVEL = DIRECTORY_CURRENT + @"\level";
        public static string DIRECTORY_ACCOUNTS = DIRECTORY_CURRENT + @"\accounts";
        public static string FILE_CONFIG = "server.cfg";

        public string BrowserName { get; private set; }
        public ushort HostingPort { get; private set; }
        public bool IsLocal { get; private set; }
        private ILogger _Logger;
        public ILogger Logger { get { return _Logger; } set { _Logger = _Logger == null ? value : _Logger; } }
        public short MaxPlayers { get; private set; }
        public IList<IPlayer> OnlinePlayers { get { return null; } }
        public List<IPlugin> _Plugins;
        public IList<IPlugin> Plugins { get { return _Plugins.AsReadOnly(); } }
        public string RozWorldVersion { get { return "0.01"; } }
        public string ServerName { get { return "Vanilla RozWorld Server"; } }
        public string ServerVersion { get { return "0.01"; } }
        public byte TickRate { get; private set; }
        public IList<string> WhitelistedPlayers { get { return null; } }

        public event EventHandler Starting;
        public event EventHandler Stopping;
        public event EventHandler Tick;


        private Dictionary<string, CommandSentCallback> Commands;
        private bool Started = false;

        
        public void BroadcastMessage(string message)
        {
            if (Started)
            {
                Logger.Out("<SERVER> " + message);

                foreach (Player player in OnlinePlayers)
                {
                    player.SendMessage(message);
                }
            }
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

        public void Start()
        {
            // Start here - a logger is required
            if (Logger != null)
            {
                Logger.Out("RozWorld server starting...");
                Logger.Out("Checking directories...");

                FileSystem.MakeDirectory(DIRECTORY_ACCOUNTS);
                FileSystem.MakeDirectory(DIRECTORY_LEVEL);
                FileSystem.MakeDirectory(DIRECTORY_PLUGINS);

                Logger.Out("Setting configs...");

                var configFile = FileSystem.ReadINIToDictionary(DIRECTORY_CURRENT + "\\" + FILE_CONFIG);



                Logger.Out("Loading plugins...");

                _Plugins = new List<IPlugin>();
                Commands = new Dictionary<string, CommandSentCallback>();


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
                        Logger.Out("An error occurred trying to enumerate the types inside of the plugin \""
                            + Path.GetFileName(file) + "\", this plugin cannot be loaded. It may have been built for" +
                            " a different version of the RozWorld API.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Out("An error occurred trying to load plugin \"" + Path.GetFileName(file) + "\", this " +
                            "plugin cannot be loaded. The exception that occurred reported the following:\n" +
                            ex.Message + "\nStack:\n" + ex.StackTrace);
                    }
                }

                foreach (var plugin in pluginClasses)
                {
                    _Plugins.Add((IPlugin)Activator.CreateInstance(plugin));
                }

                if (Starting != null)
                    Starting(this, new EventArgs());

                // Done loading plugins

                Logger.Out("Finished loading plugins!");
                
                Logger.Out("Server done loading!");
                Logger.Out("Hello! This is " + ServerName + " (version " + ServerVersion + ").");

                Started = true;
            }
        }

        public void Stop()
        {
            // Stop here
        }
    }
}
