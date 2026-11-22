namespace Seven
{
    /// <summary>QF25 (1): the count of shots with Veyra in them, and how many were mainly the planet (must be 0).</summary>
    public static class QF25Emphasis
    {
        public static int Shots, Planet;
        public static void Count(bool ok) { Shots++; if (!ok) Planet++; }
    }
}
