using System.Text;

namespace PackSF;

public static class StrUtils
{
    public static string Cut(this string str, int start, int end)
    {
        var len = end - start;
        return str.Substring(start, len);
    }

    /// <summary>
    /// 转换到小驼峰命名
    /// </summary>
    /// <param name="???"></param>
    public static string ToLowerCamelCase(this string str, bool withUnderline = false)
    {
        if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str))
            return str;
        str = str.ToNoUnderline();
        return (withUnderline ? '_' : "") + str[0].ToString().ToLower() + str.Cut(1, str.Length);
    }

    /// <summary>
    /// 转换到大驼峰命名
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToUpperCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str))
            return str;
        str = str.ToNoUnderline();
        return str[0].ToString().ToUpper() + str.Cut(1, str.Length);
    }

    /// <summary>
    /// 转换成无下划线命名
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToNoUnderline(this string str)
    {
        var firstIndex = str.IndexOf('_');
        if (firstIndex == -1)
            return str;
        var sb = new StringBuilder();
        var splits = str.Split('_');
        for (var n = 0; n < splits.Length; n++)
        {
            var clip = splits[n];
            if (n != 0)
            {
                sb.Append(clip[0].ToString().ToUpperInvariant() + clip.Cut(1, clip.Length));
            }
            else
            {
                sb.Append(clip);
            }
        }

        return sb.ToString();
    }
}