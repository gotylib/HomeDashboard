namespace Home.Api.Configuration;

/// <summary>
/// Loads a dotenv-style file into process environment (does not override existing vars).
/// Works with Dokploy-mounted .env or a local file next to the app / in the working directory.
/// </summary>
public static class EnvFileLoader
{
    public static void Load(params string[] candidatePaths)
    {
        foreach (var path in candidatePaths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var full = Path.GetFullPath(path);
            if (!File.Exists(full))
                continue;

            foreach (var raw in File.ReadLines(full))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                var idx = line.IndexOf('=');
                if (idx <= 0)
                    continue;

                var key = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim();

                if (value.Length >= 2
                    && ((value.StartsWith('"') && value.EndsWith('"'))
                        || (value.StartsWith('\'') && value.EndsWith('\''))))
                {
                    value = value[1..^1];
                }

                if (string.IsNullOrWhiteSpace(key))
                    continue;

                // Do not override vars already set by Dokploy / Docker / shell.
                if (Environment.GetEnvironmentVariable(key) is null)
                    Environment.SetEnvironmentVariable(key, value);
            }

            break;
        }
    }
}
