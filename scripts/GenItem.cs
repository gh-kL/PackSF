using System.Text.RegularExpressions;
using Utf8Json;

namespace PackSF;

public class GenItem
{
    public string dir;
    public string outputName;
    public List<string> pngs;
    public List<string> lastAtlasList;
    public PropData propData;
    public string nowPropDataMD5;

    private SFData _sfData;
    private List<Image<Rgba32>> _outputAtlasList;

    public int PngTotalNum => pngs?.Count ?? 0;
    public bool HasPropData => propData != null;

    public override string ToString()
    {
        return $"[{dir} ({PngTotalNum} pngs)]";
    }

    /// <summary>
    /// 排序
    /// </summary>
    public void SortPngs()
    {
        if (pngs == null)
            return;
        pngs.Sort(delegate(string a, string b)
        {
            var aName = Path.GetFileNameWithoutExtension(a);
            var bName = Path.GetFileNameWithoutExtension(b);
            var aMatches = Regex.Matches(aName, @"\d+");
            var bMatches = Regex.Matches(bName, @"\d+");
            var aIdx = int.Parse(aMatches[aMatches.Count - 1].Value);
            var bIdx = int.Parse(bMatches[bMatches.Count - 1].Value);
            return aIdx.CompareTo(bIdx);
        });
    }

    /// <summary>
    /// 生成
    /// </summary>
    /// <returns></returns>
    public bool Gen()
    {
        var canUseCache = CanUseCache();
        LogUtils.Info($"[使用缓存 {canUseCache}] {this}");
        if (canUseCache)
            return GenByCache();
        return GenActual();
    }

    /// <summary>
    /// 是否可以使用缓存
    /// </summary>
    /// <returns></returns>
    public bool CanUseCache()
    {
        if (string.IsNullOrWhiteSpace(Cache.Inst.configMD5) || !GlobalVar.ConfigMD5.Equals(Cache.Inst.configMD5))
            return false;

        var hasProp = propData != null;

        if (!Cache.Inst.dict.TryGetValue(dir, out var cacheItem))
            return false;

        var hasLastProp = !string.IsNullOrEmpty(cacheItem.propMD5);

        if (hasProp != hasLastProp)
            return false;

        if (hasProp)
        {
            var nowPropMD5 = nowPropDataMD5;
            if (!nowPropMD5.Equals(cacheItem.propMD5))
                return false;
        }

        if (cacheItem.pngsMD5 == null || cacheItem.pngsMD5.Count != PngTotalNum)
            return false;

        for (int n = 0; n < PngTotalNum; n++)
        {
            var nowPngMD5 = IOUtils.GetFileMD5(pngs[n]);
            if (cacheItem.pngsMD5.FindIndex(delegate(string pp) { return pp.Equals(nowPngMD5); }) == -1)
                return false;
        }

        var jsonDataPath = Path.Join(dir, $"{outputName}.json");
        if (!File.Exists(jsonDataPath) || !IOUtils.GetFileMD5(jsonDataPath).Equals(cacheItem.dataMD5))
            return false;

        // 判断合图
        if (lastAtlasList == null)
            return false;

        if (cacheItem.atlasPngsMD5 == null || cacheItem.atlasPngsMD5.Count != lastAtlasList.Count)
            return false;

        for (var n = 0; n < lastAtlasList.Count; n++)
        {
            var lastAtlasPngMD5 = IOUtils.GetFileMD5(lastAtlasList[n]);
            if (cacheItem.atlasPngsMD5.FindIndex(delegate(string pp) { return pp.Equals(lastAtlasPngMD5); }) == -1)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 生成（真正的生成）
    /// </summary>
    /// <returns></returns>
    private bool GenActual()
    {
        // 删除旧文件
        if (lastAtlasList != null)
        {
            for (var n = lastAtlasList.Count - 1; n >= 0; n--)
            {
                File.Delete(lastAtlasList[n]);
            }
        }

        if (PngTotalNum == 0)
            return true;

        _sfData = new SFData
        {
            count = PngTotalNum,
            pivotX = Config.Inst.defaultPivotX,
            pivotY = Config.Inst.defaultPivotY,
            fps = Config.Inst.defaultFPS,
            playMode = (int)SequenceFramePlayMode.Forward,
        };

        if (HasPropData)
        {
            _sfData.fps = propData.fps;
            _sfData.pivotX = propData.pivotX;
            _sfData.pivotY = propData.pivotY;
            _sfData.playMode = propData.playMode;
        }

        // 检查图片尺寸是否都一致
        for (int n = 0; n < PngTotalNum; n++)
        {
            var png = pngs[n];
            using (var image = Image.Load(png))
            {
                if (n == 0)
                {
                    _sfData.width = image.Width;
                    _sfData.height = image.Height;
                }
                else if (image.Width != _sfData.width || image.Height != _sfData.height)
                {
                    LogUtils.Error("图片尺寸不一致！", dir);
                    return false;
                }
            }
        }

        var edgeGap = HasPropData ? propData.edgeGap : Config.Inst.defaultEdgeGap; // 边距
        var itemGap = HasPropData ? propData.itemGap : Config.Inst.defaultItemGap; // 间距
        var atlasMaxSize = propData != null ? propData.atlasMaxSize : Config.Inst.defaultAtlasMaxSize; // 合图最大尺寸

        #region 计算输出的图片尺寸

        var outputSize = CommonUtils.GetGreaterPowerOf2(Math.Max(
            _sfData.width + (edgeGap * 2),
            _sfData.height + (edgeGap * 2)
        ));

        if (outputSize > atlasMaxSize)
        {
            LogUtils.Error($"散图尺寸超过合图设定尺寸 {atlasMaxSize}！{dir}");
            return false;
        }

        var leftCount = PngTotalNum;
        var pointer = new Vector2Int(edgeGap, edgeGap);
        for (int n = 0; n < PngTotalNum; n++)
        {
            // 如果剩余空间不足以塞下一个
            if (outputSize - pointer.x < _sfData.width + edgeGap)
            {
                // 如果剩余空间足以容纳剩下的碎图，就换行，反之，就扩大
                var nowCol = (pointer.x / _sfData.width); // 当前列数
                var leftY = outputSize - (pointer.y + _sfData.height + itemGap);
                var leftRowF = (float)leftY / (_sfData.height + itemGap);
                var leftRow = leftRowF;
                if (leftRow > 0 && Math.Truncate(leftRowF) * (_sfData.height + itemGap) < edgeGap)
                {
                    leftRow--;
                }

                var capacity = leftRow * nowCol;

                if (capacity >= leftCount)
                {
                    pointer.x = edgeGap;
                    pointer.y += _sfData.height + itemGap;
                }
                else
                {
                    outputSize = CommonUtils.GetGreaterPowerOf2(outputSize);
                    if (outputSize > atlasMaxSize)
                    {
                        outputSize = atlasMaxSize;
                        break;
                    }
                }

                n--;
                continue;
            }

            pointer.x += _sfData.width + itemGap;
            leftCount--;
        }

        #endregion

        #region 合图

        var trimLen = outputSize - (edgeGap * 2);
        var colTotal = (int)Math.Floor((float)(trimLen + itemGap) / (_sfData.width + itemGap)); // 总列数
        var rowTotal = (int)Math.Floor((float)(trimLen + itemGap) / (_sfData.height + itemGap)); // 总行数
        var singleAtlasCapacity = colTotal * rowTotal;
        var atlasCount = (int)Math.Ceiling((float)PngTotalNum / singleAtlasCapacity);

        _outputAtlasList = new List<Image<Rgba32>>();

        var pngCnt = 0;
        for (int n = 0; n < atlasCount; n++)
        {
            var colNum = 0;
            var rowNum = 0;
            var atlasImg = new Image<Rgba32>(outputSize, outputSize);

            for (int m = 0; m < singleAtlasCapacity && pngCnt < PngTotalNum; m++)
            {
                using (var img = Image.Load<Rgba32>(pngs[pngCnt]))
                {
                    var loc = new Point(
                        edgeGap + (colNum * _sfData.width) + (colNum * itemGap),
                        edgeGap + (rowNum * _sfData.height) + (rowNum * itemGap)
                    );
                    _sfData.clips.Add(new[] { n, loc.X, loc.Y });
                    atlasImg.Mutate(ctx => ctx.DrawImage(img, loc, 1f));
                }

                colNum++;
                if (colNum >= colTotal)
                {
                    colNum = 0;
                    rowNum++;
                }

                pngCnt++;
            }

            _outputAtlasList.Add(atlasImg);
        }

        #endregion

        #region 导出数据

        var outputAtlasTuples = new List<Tuple<string, Image<Rgba32>>>();
        for (int n = 0; n < _outputAtlasList.Count; n++)
        {
            outputAtlasTuples.Add(new Tuple<string, Image<Rgba32>>(
                $"{dir}/{outputName}@{n}.png",
                _outputAtlasList[n]
            ));
        }

        var outputDataPaths = new List<string> { $"{dir}/{outputName}.json" };

        if (Config.Inst.exportPaths != null)
        {
            foreach (var exportPath in Config.Inst.exportPaths)
            {
                switch (Config.Inst.exportMethod)
                {
                    case ExportMethod.SameDirectory:
                    {
                        for (int n = 0; n < _outputAtlasList.Count; n++)
                        {
                            outputAtlasTuples.Add(new Tuple<string, Image<Rgba32>>(
                                Path.Join(exportPath, $"{outputName}@{n}.png"),
                                _outputAtlasList[n]
                            ));
                        }

                        outputDataPaths.Add(Path.Join(exportPath, outputName + ".json"));
                        break;
                    }
                    case ExportMethod.TopDirectory:
                    {
                        for (int n = 0; n < _outputAtlasList.Count; n++)
                        {
                            outputAtlasTuples.Add(new Tuple<string, Image<Rgba32>>(
                                Path.Join(exportPath, outputName, $"{outputName}@{n}.png"),
                                _outputAtlasList[n]
                            ));
                        }

                        outputDataPaths.Add(Path.Join(exportPath, outputName, outputName + ".json"));
                        break;
                    }
                    case ExportMethod.RelativeRootSameDirectory:
                    case ExportMethod.RelativeRootTopDirectory:
                    {
                        var rootNameIdx = dir.LastIndexOf(GlobalVar.RootDirName, StringComparison.Ordinal);
                        var rPath = dir.Cut(rootNameIdx != -1 ? (rootNameIdx + GlobalVar.RootDirName.Length + 1) : 3, dir.Length);
                        if (Config.Inst.exportMethod == ExportMethod.RelativeRootSameDirectory)
                        {
                            var subDirIdx = rPath.LastIndexOf(outputName, StringComparison.Ordinal);
                            rPath = rPath.Cut(0, subDirIdx);
                        }

                        for (int n = 0; n < _outputAtlasList.Count; n++)
                        {
                            outputAtlasTuples.Add(new Tuple<string, Image<Rgba32>>(
                                Path.Join(exportPath, rPath, $"{outputName}@{n}.png"),
                                _outputAtlasList[n]
                            ));
                        }

                        outputDataPaths.Add(Path.Join(exportPath, rPath, outputName + ".json"));
                        break;
                    }
                }
            }
        }

        for (var n = 0; n < outputAtlasTuples.Count; n++)
        {
            var tuple = outputAtlasTuples[n];
            var directoryPath = Path.GetDirectoryName(tuple.Item1);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            tuple.Item2.Save(tuple.Item1);
            LogUtils.ConsoleWriteLine(ConsoleColor.Green, $"导出纹理：{tuple.Item1}");
        }

        var dataBytes = JsonSerializer.Serialize(_sfData);
        for (var n = 0; n < outputDataPaths.Count; n++)
        {
            var exportPath = outputDataPaths[n];
            var directoryPath = Path.GetDirectoryName(exportPath);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            File.WriteAllBytes(exportPath, dataBytes);
            LogUtils.ConsoleWriteLine(ConsoleColor.Green, $"导出数据：{exportPath}");
        }

        #endregion

        #region 保存缓存

        var cacheItem = new CacheItem
        {
            pngsMD5 = new List<string>(),
            atlasPngsMD5 = new List<string>(),
        };
        cacheItem.propMD5 = nowPropDataMD5;
        for (int n = 0; n < PngTotalNum; n++)
        {
            cacheItem.pngsMD5.Add(IOUtils.GetFileMD5(pngs[n]));
        }

        for (int n = 0; n < atlasCount; n++)
        {
            cacheItem.atlasPngsMD5.Add(IOUtils.GetFileMD5($"{dir}/{outputName}@{n}.png"));
        }

        cacheItem.dataMD5 = IOUtils.GetFileMD5($"{dir}/{outputName}.json");

        Cache.Inst.dict[dir] = cacheItem;

        #endregion

        return true;
    }

    /// <summary>
    /// 生成（通过缓存）
    /// </summary>
    /// <returns></returns>
    private bool GenByCache()
    {
        if (Config.Inst.exportMethod == ExportMethod.SelfDirectoryOnly)
            return true;

        var dataPath = Path.Join(dir, $"{outputName}.json");
        var dataFileNameNoExt = Path.GetFileNameWithoutExtension(dataPath);

        var copys = new List<Tuple<string, string>>();

        foreach (var exportPath in Config.Inst.exportPaths)
        {
            switch (Config.Inst.exportMethod)
            {
                case ExportMethod.SameDirectory:
                {
                    for (var n = 0; n < lastAtlasList.Count; n++)
                    {
                        var lastAtlas = lastAtlasList[n];
                        var fileNameNoExt = Path.GetFileNameWithoutExtension(lastAtlas);
                        copys.Add(new Tuple<string, string>(
                            lastAtlas,
                            Path.Join(exportPath, fileNameNoExt + ".png")
                        ));
                    }

                    copys.Add(new Tuple<string, string>(
                        dataPath,
                        Path.Join(exportPath, dataFileNameNoExt + ".json")
                    ));
                    break;
                }
                case ExportMethod.TopDirectory:
                {
                    for (var n = 0; n < lastAtlasList.Count; n++)
                    {
                        var lastAtlas = lastAtlasList[n];
                        var fileNameNoExt = Path.GetFileNameWithoutExtension(lastAtlas);
                        var atIdx = fileNameNoExt.LastIndexOf("@");
                        var noAtName = fileNameNoExt.Cut(0, atIdx);
                        copys.Add(new Tuple<string, string>(
                            lastAtlas,
                            Path.Join(exportPath, noAtName, fileNameNoExt + ".png")
                        ));
                    }

                    copys.Add(new Tuple<string, string>(
                        dataPath,
                        Path.Join(exportPath, dataFileNameNoExt, dataFileNameNoExt + ".json")
                    ));
                    break;
                }
                case ExportMethod.RelativeRootSameDirectory:
                case ExportMethod.RelativeRootTopDirectory:
                {
                    var rootNameIdx = dir.LastIndexOf(GlobalVar.RootDirName, StringComparison.Ordinal);
                    var rPath = dir.Cut(rootNameIdx != -1 ? (rootNameIdx + GlobalVar.RootDirName.Length + 1) : 3, dir.Length);
                    string rPath2 = rPath;

                    for (var n = 0; n < lastAtlasList.Count; n++)
                    {
                        var lastAtlas = lastAtlasList[n];
                        var fileNameNoExt = Path.GetFileNameWithoutExtension(lastAtlas);

                        if (Config.Inst.exportMethod == ExportMethod.RelativeRootSameDirectory)
                        {
                            var atIdx = fileNameNoExt.LastIndexOf("@");
                            var noAtName = fileNameNoExt.Cut(0, atIdx);
                            var subDirIdx = rPath.LastIndexOf(noAtName, StringComparison.Ordinal);
                            rPath2 = rPath.Cut(0, subDirIdx);
                        }

                        copys.Add(new Tuple<string, string>(
                            lastAtlas,
                            Path.Join(exportPath, rPath2, fileNameNoExt + ".png")
                        ));
                    }

                    if (Config.Inst.exportMethod == ExportMethod.RelativeRootSameDirectory)
                    {
                        var subDirIdx = rPath.LastIndexOf(dataFileNameNoExt, StringComparison.Ordinal);
                        rPath2 = rPath.Cut(0, subDirIdx);
                    }

                    copys.Add(new Tuple<string, string>(
                        dataPath,
                        Path.Join(exportPath, rPath2, dataFileNameNoExt + ".json")
                    ));
                    break;
                }
            }
        }

        var fileMD5Dict = new Dictionary<string, string>();
        for (var n = 0; n < copys.Count; n++)
        {
            var cp = copys[n];
            if (!fileMD5Dict.TryGetValue(cp.Item1, out var fromMD5))
            {
                fromMD5 = IOUtils.GetFileMD5(cp.Item1);
                fileMD5Dict[cp.Item1] = fromMD5;
            }

            if (!fileMD5Dict.TryGetValue(cp.Item2, out var toMD5))
            {
                toMD5 = IOUtils.GetFileMD5(cp.Item2);
                fileMD5Dict[cp.Item2] = toMD5;
            }

            if (!Config.Inst.deleteAllBeforeExport && fromMD5.Equals(toMD5))
                continue;

            var directoryPath = Path.GetDirectoryName(cp.Item2);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            File.Copy(cp.Item1, cp.Item2, true);

            var ext = Path.GetExtension(cp.Item2);
            switch (ext)
            {
                case ".png":
                {
                    LogUtils.ConsoleWriteLine(ConsoleColor.Green, $"导出纹理：{cp.Item2}");
                    break;
                }
                case ".json":
                {
                    LogUtils.ConsoleWriteLine(ConsoleColor.Green, $"导出数据：{cp.Item2}");
                    break;
                }
            }
        }

        return true;
    }
}