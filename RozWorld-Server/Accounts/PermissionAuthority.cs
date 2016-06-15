using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.API.Generic;
using System;
using System.Collections.Generic;

namespace Oddmatics.RozWorld.Server.Accounts
{
    public class PermissionAuthority : IPermissionAuthority
    {
        public string DefaultGroupName { get; set; }
        public IList<string> GroupNames { get { return new List<string>(GroupRegistry.Keys).AsReadOnly(); } }
        public IList<string> RegisteredPermissions { get { return new List<string>(PermissionRegistry.Keys).AsReadOnly(); } }

        private Dictionary<string, PermissionInfo> PermissionRegistry;
        private Dictionary<string, IPermissionGroup> GroupRegistry;


        public PermissionAuthority()
        {
            PermissionRegistry = new Dictionary<string, PermissionInfo>();
            GroupRegistry = new Dictionary<string, IPermissionGroup>();
        }


        public IPermissionGroup CreateNewGroup(string name)
        {
            string realName = name.ToLower();

            if (!GroupRegistry.ContainsKey(realName))
            {
                var newGroup = new PermissionGroup();
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

        public void RegisterPermission(string key, string description)
        {
            string realKey = key.ToLower();
            var server = (RwServer)RwCore.Server;

            if (server.Started)
                throw new InvalidOperationException("The server has already been started, permissions must be " +
                    "registered in the starting up phase.");

            if (!PermissionRegistry.ContainsKey(realKey))
            {
                var permInfo = new PermissionInfo(server.CurrentPluginLoading, description);
                PermissionRegistry.Add(realKey, permInfo);
            }
            else
                throw new ArgumentException("A permission with the same key already exists.");
        }
    }
}
