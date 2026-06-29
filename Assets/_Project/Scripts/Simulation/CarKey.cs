using CarFactoryIdle.Data;

namespace CarFactoryIdle.Simulation
{
    /// <summary>Helpers for the flat inventory keys like \"autobahn911_A\".</summary>
    public static class CarKey
    {
        public static string Build(string vehicleId, Grade grade) => vehicleId + "_" + grade.ToKey();

        public static bool TryParse(string key, out string vehicleId, out Grade grade)
        {
            vehicleId = null;
            grade = Grade.C;
            if (string.IsNullOrEmpty(key)) return false;
            int i = key.LastIndexOf('_');
            if (i <= 0 || i >= key.Length - 1) return false;
            vehicleId = key.Substring(0, i);
            grade = GradeExtensions.FromKey(key.Substring(i + 1));
            return true;
        }
    }
}
