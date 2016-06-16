using Oddmatics.RozWorld.API.Server.Entity;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Oddmatics.RozWorld.Server.Entity
{
    public class EntityFactory : IEntityFactory
    {
        private Dictionary<string, Type> AvailableTypes = new Dictionary<string, Type>();


        public IEntity CreateEntity(string type)
        {
            string realType = type.ToLower();

            if (AvailableTypes.ContainsKey(realType))
            {
                return (IEntity)Activator.CreateInstance(AvailableTypes[realType]);
            }
            else
                throw new ArgumentException("Specified entity type does not exist.");
        }

        public IPlayer CreatePlayer(string name)
        {
            // Make sure the name is valid (alphanumeric and underscores)
            Regex nameRule = new Regex("^[A-Za-z0-9_]$");
            if (nameRule.IsMatch(name))
                return new Player(name);
            else
                throw new ArgumentException("Invalid characters in player name.");
        }

        public bool IsLoaded(string type)
        {
            return AvailableTypes.ContainsKey(type.ToLower());
        }
    }
}
