namespace CarFactoryIdle.Data
{
    /// <summary>Gacha quality grades. Order matters (worst -> best).</summary>
    public enum Grade { D, C, B, A, S, SPlus }

    public static class GradeExtensions
    {
        /// <summary>String suffix used in flat inventory keys, e.g. "tokyoCommuter_S".</summary>
        public static string ToKey(this Grade g) => g switch
        {
            Grade.D => "D",
            Grade.C => "C",
            Grade.B => "B",
            Grade.A => "A",
            Grade.S => "S",
            Grade.SPlus => "Splus",
            _ => "C"
        };

        public static Grade FromKey(string suffix) => suffix switch
        {
            "D" => Grade.D,
            "C" => Grade.C,
            "B" => Grade.B,
            "A" => Grade.A,
            "S" => Grade.S,
            "Splus" => Grade.SPlus,
            _ => Grade.C
        };
    }
}
