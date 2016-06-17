/**
 * Oddmatics.RozWorld.Server.Game.ContentManager -- RozWorld Server Content Manager Implementation
 *
 * This source-code is part of the server library for the RozWorld project by rozza of Oddmatics:
 * <<http://www.oddmatics.uk>>
 * <<http://roz.world>>
 * <<http://github.com/rozniak/RozWorld-Server>>
 *
 * Sharing, editing and general licence term information can be found inside of the "LICENCE.MD" file that should be located in the root of this project's directory structure.
 */

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
