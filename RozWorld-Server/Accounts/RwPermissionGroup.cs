﻿/**
 * Oddmatics.RozWorld.Server.Accounts.RwPermissionGroup -- RozWorld Server Permission Group Implementation
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Oddmatics.RozWorld.Server.Accounts
{
    public sealed class RwPermissionGroup : IPermissionGroup
    {
        public string ChatPrefix { get; set; }
        public string ChatSuffix { get; set; }
        private string _ColourModifier;
        public string ColourModifier
        {
            get { return _ColourModifier; }
            set { if (ChatColour.IsChatColour(value)) _ColourModifier = value; }
        }
        public bool IsDefault
        {
            get { return RwCore.Server.PermissionAuthority.DefaultGroup == this; }
            set { RwCore.Server.PermissionAuthority.DefaultGroup = this; }
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
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set
            {
                var permAuthority = (RwPermissionAuthority)RwCore.Server.PermissionAuthority;
                string originalValue = _Name;
                string realValue = value.ToLower();

                if (String.IsNullOrEmpty(_Name))
                    _Name = realValue;
                else if (RwCore.Server.PermissionAuthority.GetGroup(realValue) == null) // Make sure group name is free
                {
                    _Name = realValue;
                    permAuthority.UpdateGroupKey(originalValue); // Sync key in authority
                }
            }
        }
        

        private PermissionGroupFile File;


        public RwPermissionGroup()
        {
            ChatPrefix = String.Empty;
            ChatSuffix = String.Empty;
            ColourModifier = ChatColour.DEFAULT;
            IsDefault = false;
            _Name = String.Empty;
            _Members = new List<IAccount>();
            _Permissions = new List<string>();
            File = new PermissionGroupFile();
        }

        public RwPermissionGroup(PermissionGroupFile data)
        {
            ChatPrefix = data.Prefix;
            ChatSuffix = data.Suffix;
            ColourModifier = data.Colour;
            IsDefault = data.Default;
            _Name = data.Name;
            _Members = new List<IAccount>();
            _Permissions = new List<string>(data.Permissions);
            File = data;
        }


        public bool AddPermission(string key)
        {
            string realKey = key.ToLower();

            if (!RwCore.Server.PermissionAuthority.RegisteredPermissions.Contains(key))
                return false; // Invalid permission, do not add

            if (!_Permissions.Contains(realKey))
                _Permissions.Add(realKey);

            return true;
        }

        public bool HasPermission(string key)
        {
            string realKey = key.ToLower();
            string keyCheck = String.Empty;
            string[] keySplit = realKey.Split('.');

            if (_Permissions.Contains("*"))
                return true;

            for (int i = 0; i < keySplit.Length; i++)
            {
                keyCheck += keySplit[i];

                if (i < keySplit.Length - 1)
                {
                    if (_Permissions.Contains(keyCheck + ".*"))
                        return true;
                }
                else
                {
                    if (_Permissions.Contains(keyCheck) ||
                        _Permissions.Contains(keyCheck + ".*"))
                        return true;
                }

                keyCheck += ".";
            }

            return false; // Failed to match
        }

        public void RecalculateMembers()
        {
            // DO this much later
            throw new System.NotImplementedException();
        }

        public bool RemovePermission(string key)
        {
            string realKey = key.ToLower();

            if (_Permissions.Contains(realKey))
                _Permissions.Remove(realKey);

            return true;
        }

        public void Save(string destination = "")
        {
            string fileDestination = destination == String.Empty ?
                RwServer.DIRECTORY_PERMISSIONS + @"\group-" + Name.ToLower() + ".json":
                destination;

            File = new PermissionGroupFile();

            File.Colour = ColourModifier;
            File.Default = RwCore.Server.PermissionAuthority.DefaultGroup == this;
            File.Name = Name;
            File.Permissions = Permissions.ToArray();
            File.Prefix = ChatPrefix;
            File.Suffix = ChatSuffix;

            File.Save(fileDestination);
        }
    }
}
