namespace PackSF;

public enum ExportMethod
{
    /// <summary>
    /// 仅在序列帧资源目录发布
    /// </summary>
    SelfDirectoryOnly,

    /// <summary>
    /// 将所有图集集中发布到路径
    /// </summary>
    SameDirectory,

    /// <summary>
    /// 将所有图集分文件夹发布到路径
    /// </summary>
    TopDirectory,

    /// <summary>
    /// 将所有图集按序列帧资源目录的形式集中发布到路径
    /// </summary>
    RelativeRootSameDirectory,

    /// <summary>
    /// 将所有图集按序列帧资源目录的形式分文件夹发布到路径
    /// </summary>
    RelativeRootTopDirectory,
}