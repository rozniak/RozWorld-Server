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

using Oddmatics.RozWorld.API.Server.Entities;
using Oddmatics.RozWorld.API.Server.Game;
using Oddmatics.RozWorld.Server.Entities;
using System;
using System.Collections.Generic;

namespace Oddmatics.RozWorld.Server.Game
{
    public class RwContentManager : IContentManager
    {
        public IEntityFactory EntityFactory { get; private set; }
        private Dictionary<Type, List<byte>> EntityStates;


        public RwContentManager()
        {
            EntityFactory = new RwEntityFactory();
            EntityStates = new Dictionary<Type, List<byte>>();
        }

        public bool CheckEntityState(byte state, Type entityType)
        {
            return EntityStates.ContainsKey(entityType) &&
                EntityStates[entityType].Contains(state);
        }

        public bool RegisterEntity(Type entityType)
        {
            return ((RwEntityFactory)EntityFactory).RegisterEntity(entityType);
        }

        public bool RegisterEntityState(byte state, Type entityType)
        {
            if (EntityStates.ContainsKey(entityType) &&
                !EntityStates[entityType].Contains(state))
            {
                EntityStates[entityType].Add(state);
                return true;
            }

            return false;
        }
    }
}
