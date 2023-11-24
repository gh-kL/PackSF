using System.Text.RegularExpressions;
using Utf8Json;

namespace PackSF;

public class Program
{
    public static List<GenItem> _genItems;

    static void Main()
    {
        var runResult = Init();
        runResult = runResult && TryDelLastGenFile();
        runResult = runResult && CollectInfos();
        runResult = runResult && GenAll();

        if (runResult)
        {
            // 保存缓存
            Cache.Inst.configMD5 = GlobalVar.ConfigMD5;
            var cacheBytes = JsonSerializer.Serialize(Cache.Inst);
            File.WriteAllBytes(Paths.CacheUrl, cacheBytes);
            LogUtils.Info("程序执行完毕，按任意键退出！");
        }
        else
        {
            LogUtils.Error("程序执行中出现了错误，请往上翻阅错误信息！");
        }

        Console.ReadKey();
    }

    static bool Init()
    {
        // 读取配置
        if (File.Exists(Paths.ConfigUrl))
        {
            var configJson = File.ReadAllText(Paths.ConfigUrl);
            Config.Inst = JsonSerializer.Deserialize<Config>(configJson);
            GlobalVar.ConfigMD5 = IOUtils.GetFileMD5(Paths.ConfigUrl);
        }
        else
        {
            LogUtils.Error($"找不到配置文件！{Paths.ConfigUrl}");
            Console.ReadKey();
            return false;
        }

        if (!Directory.Exists(Config.Inst.rootUrl))
        {
            LogUtils.Error($"指定的根目录不存在！{Config.Inst.rootUrl}");
            Console.ReadKey();
            return false;
        }

        // 读取缓存
        if (File.Exists(Paths.CacheUrl))
        {
            var cache = File.ReadAllText(Paths.CacheUrl);
            Cache.Inst = JsonSerializer.Deserialize<Cache>(cache);
        }
        else
        {
            Cache.Inst = new Cache();
        }

        return true;
    }

    /// <summary>
    /// 尝试生成前删除所有
    /// </summary>
    /// <returns></returns>
    static bool TryDelLastGenFile()
    {
        if (!Config.Inst.deleteAllBeforeExport || Config.Inst.exportPaths == null)
            return true;

        for (var n = 0; n < Config.Inst.exportPaths.Count; n++)
        {
            IOUtils.ClearDirectory(Config.Inst.exportPaths[n], false);
        }

        return true;
    }

    /// <summary>
    /// 收集信息
    /// </summary>
    /// <returns></returns>
    static bool CollectInfos()
    {
        GlobalVar.RootDirName = new DirectoryInfo(Config.Inst.rootUrl).Name;
        _genItems = new List<GenItem>();

        var allDir = Directory.GetDirectories(Config.Inst.rootUrl, "*", SearchOption.AllDirectories).ToList();
        for (var n = 0; n < allDir.Count; n++)
        {
            var dir = allDir[n];
            dir = dir.Replace("\\", "/");
            var lastAtlas = new List<string>();
            var pngs = Directory.GetFiles(dir, "*.png", SearchOption.TopDirectoryOnly).ToList();
            var dirName = new DirectoryInfo(dir).Name;

            for (var m = pngs.Count - 1; m >= 0; m--)
            {
                var png = pngs[m];
                var pngName = Path.GetFileName(png);

                var pattern = $@"^.+@[0-9]*.png$";
                var regex = new Regex(pattern);
                var matches = regex.Matches(pngName);

                pattern = @"\d+";
                var matches2 = Regex.Matches(pngName, pattern);

                if (matches.Count > 0 || matches2.Count == 0)
                {
                    if (matches.Count > 0)
                        lastAtlas.Add(png);
                    pngs.RemoveAt(m);
                }
            }

            var genItem = new GenItem { dir = dir };

            if (pngs.Count > 0)
            {
                genItem.pngs = pngs;
                genItem.SortPngs();
                var propPath = Path.Join(dir, Paths.ScatteredPropFileName);
                if (File.Exists(propPath))
                {
                    var propJson = File.ReadAllText(propPath);
                    genItem.propData = JsonSerializer.Deserialize<PropData>(propJson);
                    genItem.nowPropDataMD5 = IOUtils.GetFileMD5(propPath);
                    genItem.outputName = string.IsNullOrWhiteSpace(genItem.propData.name) ? dirName : genItem.propData.name;
                }
                else
                {
                    genItem.outputName = dirName;
                }
            }

            if (lastAtlas.Count > 0)
            {
                genItem.lastAtlasList = lastAtlas;
            }

            if (genItem.pngs != null || genItem.lastAtlasList != null)
            {
                _genItems.Add(genItem);
            }
        }

        return true;
    }

    /// <summary>
    /// 生成全部
    /// </summary>
    /// <returns></returns>
    static bool GenAll()
    {
        for (var n = 0; n < _genItems.Count; n++)
        {
            if (!_genItems[n].Gen())
                return false;
        }

        return true;
    }
}