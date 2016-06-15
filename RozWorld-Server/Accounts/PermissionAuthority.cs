using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.API.Generic;
using System;
using System.Collections.Generic;

namespace Oddmatics.RozWorld.Server.Accounts
{
    public class PermissionAuthority : IPermissionAuthority
    {
        public string DefaultGroupName { get; set; }
        public IList<string> GroupNames { get { return null; } }
        public IList<string> RegisteredPermissions { get { return null; } }
        private Dictionary<string, PermissionInfo> PermissionRegistry;
        private Dictionary<string, IPermissionGroup> GroupRegistry;


        public PermissionAuthority()
        {
            PermissionRegistry = new Dictionary<string, PermissionInfo>();
        }


        public IPermissionGroup CreateNewGroup(string name)
        {
            // TODO: code this when IPermissionGroup is implemented
            throw new NotImplementedException();
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
