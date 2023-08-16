using System.Collections.Generic;

namespace MC_SVFleetControlExtended
{
    internal class Config
    {
        // Energy Barrier
        internal const int DEFAULT_ENERGY_BARRIER_THRESHOLD = 3;

        // Cloak
        internal const bool DEFAULT_CLOAK_WITH_PLAYER_STATE = false;

        // Escort
        internal const int PLAYER_ESCORT_ID = -1;
        internal const bool DEFAULT_DEDICATED_DEFENDER_STATE = false;

        //Desired distance
        internal const int DEFAULT_DESIRED_DISTANCE_OPT = 0;
        internal static int[] DESIRED_DISTANCE_OPTIONS = new int[]
        {
            -1,
            0,
            50,
            100,
            150,
            200,
            250,
            300,
            350,
            400,
            450,
            500,
            550,
            600,
            650,
            700,
            750,
            800,
            850,
            900,
            950,
            1000
        };
    }
}
