using Microsoft.EntityFrameworkCore;

namespace DTN.EFCore.AdvancedExtensions
{
    public static class EFCoreCompatibilityChecker
    {
        public static void EnsureCompatibility()
        {
            var efCoreVersion = typeof(DbContext).Assembly.GetName().Version;

            if (efCoreVersion != null && (efCoreVersion.Major != 8 || (efCoreVersion.Minor == 0 && efCoreVersion.Build < 0) || efCoreVersion.Build > 7))
            {
                throw new InvalidOperationException($"This version of YourCompany.EFCore.AdvancedExtensions is only compatible with EntityFrameworkCore versions 8.0.0 to 8.0.7. Detected version: {efCoreVersion}");
            }

            Console.WriteLine($"Compatible EF Core version detected: {efCoreVersion}");
        }
    }
}
