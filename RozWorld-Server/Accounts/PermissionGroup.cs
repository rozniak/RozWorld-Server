/**
 * Oddmatics.RozWorld.Server.Accounts.PermissionGroup -- RozWorld Server Permission Group Implementation
 *
 * This source-code is part of the server library for the RozWorld project by rozza of Oddmatics:
 * <<http://www.oddmatics.uk>>
 * <<http://roz.world>>
 * <<http://github.com/rozniak/RozWorld-Server>>
 *
 * Sharing, editing and general licence term information can be found inside of the "LICENCE.MD" file that should be located in the root of this project's directory structure.
 */

using Oddmatics.RozWorld.API.Generic.Chat;
using Oddmatics.RozWorld.API.Server.Accounts;
using System.Collections.Generic;

namespace Oddmatics.RozWorld.Server.Accounts
{
    public class PermissionGroup : IPermissionGroup
    {
        public string ChatPrefix { get; set; }
        public string ChatSuffix { get; set; }

        private string _ColourModifier;
        public string ColourModifier
        {
            get { return _ColourModifier; }
            set { if (ChatColour.IsChatColour(value)) _ColourModifier = value; }
        }

        private List<IAccount> _Members;
        public IList<IAccount> Members
        {
            get { return _Members.AsReadOnly(); }
        }

        private List<string> _Permissions;
        public IList<string> Permissions
        {
            get { return _Permissions.AsReadOnly(); }
        }


        public void AddPermission(string key)
        {
            string realKey = key.ToLower();

            if (!_Permissions.Contains(realKey))
                _Permissions.Add(realKey);
        }

        public bool HasPermission(string key)
        {
            return _Permissions.Contains(key.ToLower());
        }

        public void RecalculateMembers()
        {
            // DO this much later
            throw new System.NotImplementedException();
        }

        public void RemovePermission(string key)
        {
            string realKey = key.ToLower();

            if (_Permissions.Contains(realKey))
                _Permissions.Remove(realKey);
        }
    }
}
