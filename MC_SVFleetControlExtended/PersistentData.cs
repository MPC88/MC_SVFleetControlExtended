using System;
using System.Collections.Generic;

namespace MC_SVFleetControlExtended
{
    [Serializable]
    internal class PersistentData
    {
        internal Dictionary<int, int> energyBarrierThresholds;
        internal Dictionary<int, bool> cloakWithPlayerStates;
        internal Dictionary<int, int> escorts;
        internal Dictionary<int, int> desiredDistances;
        internal Dictionary<int, bool> dedicatedDefenderStates;

        internal PersistentData()
        {
            energyBarrierThresholds = new Dictionary<int, int>();
            cloakWithPlayerStates = new Dictionary<int, bool>();
            escorts = new Dictionary<int, int>();
            desiredDistances = new Dictionary<int, int>();
        }
    }
}
