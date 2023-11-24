using System.Text.Json.Serialization;

namespace PackSF;

public class Config
{
    [JsonIgnore] public static Config Inst;

    public string rootUrl;
    public float defaultPivotX;
    public float defaultPivotY;
    public int defaultFPS;
    public int defaultEdgeGap;
    public int defaultItemGap;
    public int defaultAtlasMaxSize;
    public ExportMethod exportMethod;
    public bool deleteAllBeforeExport;
    public List<string> exportPaths;
}