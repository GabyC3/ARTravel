namespace ARTravel.API.Configuration;

public static class DotEnvLoader
{
    public static void Load()
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (DirectoryInfo? directory = new(startPath);
                 directory is not null;
                 directory = directory.Parent)
            {
                var filePath = Path.Combine(directory.FullName, ".env");
                if (!File.Exists(filePath))
                {
                    continue;
                }

                LoadFile(filePath);
                return;
            }
        }
    }

    private static void LoadFile(string filePath)
    {
        var lineNumber = 0;
        foreach (var line in File.ReadLines(filePath))
        {
            lineNumber++;
            var trimmedLine = line.Trim();
            if (trimmedLine.Length == 0 || trimmedLine.StartsWith('#'))
            {
                continue;
            }

            var assignment = trimmedLine.StartsWith("export ", StringComparison.Ordinal)
                ? trimmedLine["export ".Length..]
                : trimmedLine;
            var separator = assignment.IndexOf('=');
            if (separator <= 0)
            {
                throw new InvalidDataException(
                    $"Invalid .env entry at line {lineNumber}: expected KEY=VALUE.");
            }

            var key = assignment[..separator].Trim();
            var value = assignment[(separator + 1)..].Trim();
            if (key.Length == 0)
            {
                throw new InvalidDataException(
                    $"Invalid .env entry at line {lineNumber}: the key is empty.");
            }

            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') ||
                 (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
