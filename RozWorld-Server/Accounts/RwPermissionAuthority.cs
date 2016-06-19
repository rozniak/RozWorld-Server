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
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Oddmatics.RozWorld.Server.Accounts
{
    public class RwPermissionAuthority : IPermissionAuthority
    {
        public string DefaultGroupName { get; set; }
        public IList<string> GroupNames { get { return new List<string>(GroupRegistry.Keys).AsReadOnly(); } }
        public IList<string> RegisteredPermissions { get { return new List<string>(PermissionRegistry.Keys).AsReadOnly(); } }

        private Dictionary<string, PermissionInfo> PermissionRegistry;
        private Dictionary<string, IPermissionGroup> GroupRegistry;


        public RwPermissionAuthority()
        {
            PermissionRegistry = new Dictionary<string, PermissionInfo>();
            GroupRegistry = new Dictionary<string, IPermissionGroup>();
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
            else
                throw new ArgumentException("A permission group with the same name already exists.");
        }

        public IPermissionGroup GetGroup(string name)
        {
            return GroupRegistry[name];
        }

        public PermissionInfo GetPermissionInfo(string key)
        {
            return PermissionRegistry[key];
        }

        public bool RegisterPermission(string key, string description)
        {
            string realKey = key.ToLower();
            var server = (RwServer)RwCore.Server;
            var syntaxCheck = new Regex(@"^([a-z]+\.)+(([a-z]+)|\*)$");

            if (!syntaxCheck.IsMatch(realKey))
                throw new ArgumentException("Invalid format for permission key.");

            if (server.HasStarted)
                throw new InvalidOperationException("The server has already been started, permissions must be " +
                    "registered in the starting up phase.");

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
