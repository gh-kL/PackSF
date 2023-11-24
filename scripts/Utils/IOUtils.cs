using System.Security.Cryptography;
using System.Text;

namespace PackSF;

public class IOUtils
{
    public static string GetFileMD5(string filePath)
    {
        if (!File.Exists(filePath))
            return null;
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            var sb = new StringBuilder();
            var md5 = new MD5CryptoServiceProvider();
            var bytes = md5.ComputeHash(fs);
            fs.Close();
            for (int n = 0; n < bytes.Length; n++)
            {
                sb.Append(bytes[n].ToString("x2"));
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// 清理文件夹内所有东西
    /// </summary>
    /// <param name="path"></param>
    public static void ClearDirectory(string path, bool includeSelf = true, int layer = 0)
    {
        if (!Directory.Exists(path))
            return;
        var files = Directory.GetFiles(path);
        foreach (string file in files)
        {
            File.Delete(file);
        }

        var subfolders = Directory.GetDirectories(path);
        foreach (string subfolder in subfolders)
        {
            ClearDirectory(subfolder, includeSelf, layer + 1);
        }

        if (includeSelf || layer != 0)
        {
            Directory.Delete(path, true);
        }
    }
}