﻿using System.IO;
using System.Linq;
using System.Collections.Generic;
#if UNITY_EDITOR
#endif

namespace USource
{
    public struct VmtAsset : ISourceAsset
    {
        public Location Location => location;
        Location location;
        public VmtAsset(Location loc)
        {
            location = loc;
        }
        public void GetDependencies(Stream stream, List<Location> depdendencies)
        {
            depdendencies.Add(location);
            KeyValues keys = KeyValues.FromStream(stream);
            string shader = keys.Keys.First();
            KeyValues.Entry entry = keys[shader];
            if (entry.ContainsKey("$basetexture"))
            {
                depdendencies.Add(new Location("materials/" + entry["$basetexture"] + ".vtf", Location.Type.Source, depdendencies[0].ResourceProvider));
            }
        }
    }
}