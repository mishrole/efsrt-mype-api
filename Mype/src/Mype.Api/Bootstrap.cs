using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Mype.Api
{
    [ExcludeFromCodeCoverage]
    public static class Bootstrap
    {
        public static void LoadEnvironmentVariables(string contentRootPath)
        {
            var filePath = Path.Combine(contentRootPath, ".env");

            if (!File.Exists(filePath))
            {
                return;
            }

            foreach (var line in File.ReadAllLines(filePath))
            {
                var parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                {
                    continue;
                }

                Environment.SetEnvironmentVariable(parts[0], parts[1]);
            }
        }
    }
}
