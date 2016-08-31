/**
 * Oddmatics.RozWorld.Server.Accounts.PermissionAuthority -- RozWorld Server Permission Authority Implementation
 *
 * This source-code is part of the server library for the RozWorld project by rozza of Oddmatics:
 * <<http://www.oddmatics.uk>>
 * <<http://roz.world>>
 * <<http://github.com/rozniak/RozWorld-Server>>
 *
 * Sharing, editing and general licence term information can be found inside of the "LICENCE.MD" file that should be located in the root of this project's directory structure.
 */

using Oddmatics.RozWorld.API.Generic;
using Oddmatics.RozWorld.API.Generic.Chat;
using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.Formats;
using Oddmatics.Util.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Oddmatics.RozWorld.Server.Accounts
{
    public class RwPermissionAuthority : IPermissionAuthority
    {
        private IPermissionGroup _DefaultGroup;
        public IPermissionGroup DefaultGroup
        {
            get { return _DefaultGroup; }
            set
            {
                // Verify that the group being set is actually registered or is loading
                if (GroupRegistry.ContainsValue(value) || !Loaded)
                    _DefaultGroup = value;
            }
        }
        public IList<string> GroupNames { get { return new List<string>(GroupRegistry.Keys).AsReadOnly(); } }
        public IList<string> RegisteredPermissions { get { return new List<string>(PermissionRegistry.Keys).AsReadOnly(); } }

        private Dictionary<string, IPermissionGroup> GroupRegistry;
        private bool Loaded;
        private Dictionary<string, PermissionInfo> PermissionRegistry;


        public RwPermissionAuthority()
        {
            GroupRegistry = new Dictionary<string, IPermissionGroup>();
            Loaded = false;
            PermissionRegistry = new Dictionary<string, PermissionInfo>();
        }


        public void CreateDefaultPlayerFile(string name)
        {
            string realName = name.ToLower();

            // Make sure the account exists first
            if (Directory.GetFiles(RwServer.DIRECTORY_ACCOUNTS, realName + ".*.acc").Length == 1)
            {
                var permFile = new PlayerPermissionFile();

                permFile.Colour = ChatColour.DEFAULT;
                permFile.Denied = new string[] { };
                permFile.Granted = new string[] { };
                permFile.Group = DefaultGroup.Name;
                permFile.Name = name;
                permFile.Prefix = String.Empty;
                permFile.Suffix = String.Empty;

                permFile.Save(RwServer.DIRECTORY_PERMISSIONS + "\\player-" + realName + ".json");
            }
            else
                throw new ArgumentException("RwPermissionAuthority.CreateDefaultPlayerFile: Player username does not exist!");
        }

        public IPermissionGroup CreateNewGroup(string name)
        {
            string realName = name.ToLower();
            var syntaxCheck = new Regex(@"^[a-z]+$");

            if (!GroupRegistry.ContainsKey(realName))
            {
                var newGroup = new RwPermissionGroup();
                newGroup.Name = name;
                GroupRegistry.Add(realName, newGroup);
                newGroup.Save();
                return newGroup;
            }

            return null;
        }

        public IPermissionGroup GetGroup(string name)
        {
            string realName = name.ToLower();

            if (GroupRegistry.ContainsKey(realName))
                return GroupRegistry[realName];

            return null;
        }

        public PermissionInfo? GetPermissionInfo(string key)
        {
            string realKey = key.ToLower();

            if (PermissionRegistry.ContainsKey(realKey))
                return PermissionRegistry[realKey];

            return null;
        }

        public void Load()
        {
            if (Loaded)
                throw new InvalidOperationException("RwPermissionAuthority.Load: Already loaded.");

            if (RwCore.Server == null || RwCore.Server.PermissionAuthority != this)
                throw new InvalidOperationException("RwPermissionAuthority.Load: Server has not been set up correctly yet.");

            string[] groupFiles = Directory.GetFiles(RwServer.DIRECTORY_PERMISSIONS, "group-*.json");
            RwServer server = (RwServer)RwCore.Server;

            foreach (string filename in groupFiles)
            {
                try
                {
                    var file = PermissionGroupFile.FromFile(filename);
                    string realName = file.Name.ToLower();

                    if (GroupRegistry.ContainsKey(realName))
                    {
                        server.LogWithContext(RwServer.LOGGING_CONTEXT_WARNING, "PermissionAuthority: Duplicate group entry for '"
                            + file.Name + "'.");
                        GroupRegistry.Remove(realName);
                    }

                    var group = new RwPermissionGroup(file);
                    GroupRegistry.Add(group.Name, group);

                    if (group.IsDefault)
                        DefaultGroup = group;
                }
                catch (Exception ex)
                {
                    server.LogWithContext(RwServer.LOGGING_CONTEXT_ERROR,
                        "PermissionAuthority: Failed to load '" +filename + "'.");
                    server.Logger.Out(ex.Message);
                    server.Logger.Out(ex.StackTrace);
                }
            }

            Loaded = true;
        }

        public bool RegisterPermission(string key, string description)
        {
            string realKey = key.ToLower();
            var server = (RwServer)RwCore.Server;
            var syntaxCheck = new Regex(@"^([a-z]+\.)+(([a-z]+))$");

            if (!syntaxCheck.IsMatch(realKey))
                throw new ArgumentException("RwPermissionAuthority.RegisterPermission: Invalid format for permission key.");

            if (server.HasStarted)
                throw new InvalidOperationException("RwPermissionAuthority.RegisterPermission: The server has already" +
                    "been started, permissions must be registered in the starting up phase.");

            if (!PermissionRegistry.ContainsKey(realKey))
            {
                var permInfo = new PermissionInfo(server.CurrentPluginLoading, description);
                PermissionRegistry.Add(realKey, permInfo);
                return true;
            }

            return false;
        }

        public void Save()
        {
            foreach (IPermissionGroup group in GroupRegistry.Values)
            {
                group.Save();
            }
        }

        public void UpdateGroupKey(string oldKey)
        {
            if (GroupRegistry.ContainsKey(oldKey))
            {
                var group = (RwPermissionGroup)GroupRegistry[oldKey];

                if (group.Name != oldKey)
                {
                    // Name updated - renew the key with new name
                    GroupRegistry.Remove(oldKey);
                    GroupRegistry.Add(group.Name, group);
                }
            }
        }
    }
}
