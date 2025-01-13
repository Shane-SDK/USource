using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USource
{
    [System.Flags]
    public enum MaterialFlags
    {
        Invisible = 1 << 0,
        NoShadows = 1 << 1,
        NonSolid = 1 << 2,
        Skybox = 1 << 3,
    }
}
