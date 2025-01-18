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
