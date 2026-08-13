namespace RuntimeDebugger
{
    /// <summary>
    /// FNV-1a string hashing. Stable across runs (un unlike string.GetHashCode()).
    /// </summary>
    public static class HashUtil
    {
        public static int HashString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;

            uint hash = 2166136261u;
            for (int i = 0; i < s.Length; i++)
            {
                hash ^= (uint)s[i];
                hash *= 16777619u;
            }
            return (int)hash;
        }
    }
}
