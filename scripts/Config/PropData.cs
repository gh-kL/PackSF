namespace PackSF;

public class PropData
{
    public string name;
    public float pivotX = Config.Inst.defaultPivotX;
    public float pivotY = Config.Inst.defaultPivotY;
    public int fps = Config.Inst.defaultFPS;
    public int edgeGap = Config.Inst.defaultEdgeGap;
    public int itemGap = Config.Inst.defaultItemGap;
    public int atlasMaxSize = Config.Inst.defaultAtlasMaxSize;
    public int playMode = (int)SequenceFramePlayMode.Forward;
}