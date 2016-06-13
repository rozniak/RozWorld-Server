using Oddmatics.RozWorld.API.Server.Accounts;

namespace Oddmatics.RozWorld.Server.Accounts
{
    public class PermissionAuthority : IPermissionAuthority
    {

        public bool CheckPermissionByAccount(string name, string key)
        {
            throw new System.NotImplementedException();
        }

        public bool CheckPermissionByAccount(IAccount account, string key)
        {
            throw new System.NotImplementedException();
        }

        public IPermissionGroup CreateNewGroup(string name)
        {
            throw new System.NotImplementedException();
        }

        public string DefaultGroupName { get; set; }

        public IPermissionGroup GetGroup(string name)
        {
            throw new System.NotImplementedException();
        }

        public PermissionInfo GetPermissionInfo(string key)
        {
            throw new System.NotImplementedException();
        }

        public System.Collections.Generic.IList<string> GroupNames
        {
            get { throw new System.NotImplementedException(); }
        }

        public void RegisterPermission(string key, string description)
        {
            throw new System.NotImplementedException();
        }

        public System.Collections.Generic.IList<string> RegisteredPermissions
        {
            get { throw new System.NotImplementedException(); }
        }

        public void SetPermissionByAccount(string name, string key, bool status)
        {
            throw new System.NotImplementedException();
        }

        public void SetPermissionByAccount(IAccount account, string key, bool status)
        {
            throw new System.NotImplementedException();
        }
    }
}
