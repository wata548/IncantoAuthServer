using System.Collections.Concurrent;

namespace Setting;

public static class Env {
    private static ConcurrentDictionary<string, string> _envs;

    public static string Get(string pKey) => _envs[pKey];
    
    public static void Setup() {
        _envs = new(File.ReadAllLines(".env")
            .Select(line => {
                var idx = line.IndexOf('=');
                var key = line[..idx];
                var value = line[(idx + 1)..];
                return new KeyValuePair<string, string>(key, value);
            })
            .ToDictionary());
    }
}