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
using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Oddmatics.RozWorld.Server.Accounts
{
    public class RwPermissionAuthority : IPermissionAuthority
    {
        public string DefaultGroupName { get; set; }
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


        public IPermissionGroup CreateNewGroup(string name)
        {
            string realName = name.ToLower();
            var syntaxCheck = new Regex(@"^[a-z]+$");

            if (!GroupRegistry.ContainsKey(realName))
            {
                var newGroup = new RwPermissionGroup();
                GroupRegistry.Add(realName, newGroup);
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

            foreach (string filename in groupFiles)
            {
                try
                {
                    var file = PermissionGroupFile.FromFile(filename);
                    string realName = file.Name.ToLower();

                    if (GroupRegistry.ContainsKey(realName))
                    {
                        RwCore.Server.Logger.Out("[WARNING] (PermGroups) Duplicate group entry for '"
                            + file.Name + "'.");
                        GroupRegistry.Remove(realName);
                    }

                    var group = new RwPermissionGroup(file);
                    GroupRegistry.Add(group.Name, group);

                    if (group.Default)
                        DefaultGroupName = group.Name;
                }
                catch (Exception ex)
                {
                    RwCore.Server.Logger.Out("[ERR] (PermGroups) Failed to load '" + filename + "'.");
                    RwCore.Server.Logger.Out(ex.Message);
                    RwCore.Server.Logger.Out(ex.StackTrace);
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
    }
}
