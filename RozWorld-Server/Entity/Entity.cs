/**
 * Oddmatics.RozWorld.Server.Entity.Entity -- RozWorld Entity
 *
 * This source-code is part of the server library for the RozWorld project by rozza of Oddmatics:
 * <<http://www.oddmatics.uk>>
 * <<http://roz.world>>
 * <<http://github.com/rozniak/RozWorld-Server>>
 *
 * Sharing, editing and general licence term information can be found inside of the "LICENCE.MD" file that should be located in the root of this project's directory structure.
 */

using Oddmatics.RozWorld.API.Generic.Level;
using Oddmatics.RozWorld.API.Server.Entity;

namespace Oddmatics.RozWorld.Server.Entity
{
    class Entity : IEntity
    {
        public int Health { get; set; }
        public Location Location { get; set; }
        public int MaxHealth { get; set; }

        public void ChangeState(short newState) { }
        public void Kill() { }
        public void TeleportTo(int segX, int segY, int segZ, int tileX, int tileY) { }
        public void TeleportTo(Location target) { }
        public void TeleportTo(IEntity target) { }
    }
}
