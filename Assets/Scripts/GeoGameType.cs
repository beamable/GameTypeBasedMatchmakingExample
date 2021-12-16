using System;
using Beamable;
using Beamable.Common.Content;

namespace MatchmakingExample
{
    [Serializable]
    public class GeoGameTypeLink : ContentLink<GeoGameType>{}
    
    [Serializable]
    public class GeoGameTypeRef : ContentRef<GeoGameType> {}
    
    [ContentType("geo")]
    [Serializable]
    [Agnostic]
    public class GeoGameType : SimGameType
    {
        public string RegionName;
    }
}