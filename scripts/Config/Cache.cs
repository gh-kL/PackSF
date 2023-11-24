using System.Text.Json.Serialization;

namespace PackSF;

public class Cache
{
    [JsonIgnore] public static Cache Inst;

    public string configMD5;
    public Dictionary<string, CacheItem> dict = new Dictionary<string, CacheItem>();
}