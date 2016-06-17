// Remove in future

using Oddmatics.RozWorld.API.Server.Entity;

namespace Oddmatics.RozWorld.Server.Entity
{
    class PlayerAttributes : ILivingEntityAttributes
    {
        public bool IsFreezable { get { return true; } }

        public bool IsPoisonable { get { return true; } }

        public bool IsStunnable { get { return true; } }

        public bool IsFlammable { get { return true; } }
    }
}
