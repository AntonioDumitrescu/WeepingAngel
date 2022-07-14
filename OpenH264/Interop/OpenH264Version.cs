namespace OpenH264.Interop
{
    public struct OpenH264Version
    {
        public readonly int Major;
        public readonly int Minor;
        public readonly int Revision;
        public readonly int Reserved;

        public override string ToString() => $"{Major}.{Minor}.{Revision}";

        public Version ToVersion() => new(Major, Minor, 0, Revision);
    }
}
