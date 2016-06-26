/**
 * Oddmatics.RozWorld.Server.Entity.EntityFactory -- RozWorld Entity Factory Implementation
 *
 * This source-code is part of the server library for the RozWorld project by rozza of Oddmatics:
 * <<http://www.oddmatics.uk>>
 * <<http://roz.world>>
 * <<http://github.com/rozniak/RozWorld-Server>>
 *
 * Sharing, editing and general licence term information can be found inside of the "LICENCE.MD" file that should be located in the root of this project's directory structure.
 */

using Oddmatics.RozWorld.API.Server.Entities;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Oddmatics.RozWorld.API.Generic;

namespace Oddmatics.RozWorld.Server.Entities
{
    public class RwEntityFactory : IEntityFactory
    {
        private Dictionary<string, Type> AvailableTypes = new Dictionary<string, Type>();


        public Entity CreateEntity(string type)
        {
            string realType = type.ToLower();

            if (AvailableTypes.ContainsKey(realType))
            {
                return (Entity)Activator.CreateInstance(AvailableTypes[realType]);
            }
            else
                throw new ArgumentException("Specified entity type does not exist.");
        }

        public Player CreateBotPlayer(string name)
        {
            // Make sure the name is valid (alphanumeric and underscores)
            if (RwPlayer.ValidName(name))
                return new RwPlayer(name);
            else
                throw new ArgumentException("Invalid characters in player name.");
        }

        public Player CreateChatBotPlayer(string name)
        {
            // Make sure the name is valid (alphanumeric and underscores)
            if (RwPlayer.ValidName(name))
            {
                // TODO: Perform player check here
                return null;

                //if (!RwCore.Server.OnlinePlayers.Contains(name))
                //    return new RwPlayer(name);
                //else
                //    return null;
            }
            else
                throw new ArgumentException("Invalid characters in player name.");
        }

        public bool IsLoaded(string type)
        {
            return AvailableTypes.ContainsKey(type.ToLower());
        }

        public bool RegisterEntity(Type entityType)
        {
            if (!(entityType is Entity))
                throw new ArgumentException("RwEntityFactory.RegisterEntity: entityType must be an Entity.");

            string fullTypeName = entityType.FullName.ToLower();

            if (!AvailableTypes.ContainsKey(fullTypeName))
            {
                AvailableTypes.Add(fullTypeName, entityType);
                return true;
            }

            return false;
        }
    }
}
