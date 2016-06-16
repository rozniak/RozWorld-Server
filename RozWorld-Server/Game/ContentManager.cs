using Oddmatics.RozWorld.API.Server.Entity;
using Oddmatics.RozWorld.API.Server.Game;
using Oddmatics.RozWorld.Server.Entity;
using System;
using System.Collections.Generic;

namespace Oddmatics.RozWorld.Server.Game
{
    public class ContentManager : IContentManager
    {
        public IEntityFactory EntityFactory { get; private set; }


        private Dictionary<string, IEntityAttributes> AvailableEntityAttribs = new Dictionary<string, IEntityAttributes>();


        public ContentManager()
        {
            EntityFactory = new EntityFactory();
        }


        public IEntityAttributes GetEntityAttributes(string type)
        {
            string realType = type.ToLower();
            if (AvailableEntityAttribs.ContainsKey(realType))
                return AvailableEntityAttribs[realType];
            else
                throw new ArgumentException("Specified entity type does not exist.");
        }
    }
}
