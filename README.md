# PackSF

序列帧合图工具。

名称解释：
- Pack = 打包
- S = Sequence = 序列
- F = Frame = 帧

假如你想把序列帧碎图打成合图（图集），那么你来对地方了。

这是一堆序列帧碎图：

![碎图](doc/sfs.png)

打成合图之后：

![小黄鸭合图](doc/xhy@0.png)

打包成合图后，还会有一个描述文件（*.json）。

通常来说，生成合图是为了在游戏引擎中去使用，所以，我也做了 **Runtime**，[点此跳转](https://github.com/gh-kL/PackSF-Runtimes)

本工具基于 .net 开发，使用的第三方插件：
- [SixLabors.ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp)
- [Utf8Json](https://www.nuget.org/packages/Utf8Json)

> 我的邮箱：klk0@qq.com
