using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sandstorm.Core.Logger;

namespace Sandstorm.Core.Providers;

public class FsProvider
{
    private static readonly string[] UnsafeFiles = new string[] { "desktop.ini", "thumbs.db", };

    public static bool IsPathSafe(string file) => UnsafeFiles.Contains(file);

    public static bool Exists(string directory) => Directory.Exists(directory) || File.Exists(directory);

    public static void CopyFile(string folderPath, string destinationPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

        if (File.Exists(folderPath))
        {
            File.Copy(folderPath, destinationPath, true);
        }
    }

    public static void CopyFile(string sourcePath, string fileName, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);
        string srcPathToFile = Path.Combine(sourcePath, fileName);
        string destPathToFile = Path.Combine(destinationPath, fileName);

        if (File.Exists(srcPathToFile))
        {
            File.Copy(srcPathToFile, destPathToFile, true);
        }
    }

    public static void WriteFile(string folderPath, string fileName, string String)
    {
        Directory.CreateDirectory(folderPath);
        string pathToFile = Path.Combine(folderPath, fileName);

        using FileStream fs = File.Create(pathToFile);
        byte[] data = new UTF8Encoding(true).GetBytes(String);
        fs.Write(data, 0, data.Length);
    }

    public static void DeleteFile(string pathToFile)
    {
        if (File.Exists(pathToFile))
        {
            File.Delete(pathToFile);
        }
    }

    public static byte[] ReadFile(string pathToFile)
    {
        byte[] fileData = null;

        if (File.Exists(pathToFile))
        {
            using (FileStream fs = File.OpenRead(pathToFile))
            {
                using BinaryReader binaryReader = new(fs);
                fileData = binaryReader.ReadBytes((int)fs.Length);
            }

            return fileData;
        }

        return fileData;
    }

    public static string ReadAllText(string pathToFile)
    {
        string fileData = null;

        if (File.Exists(pathToFile))
        {
            fileData = File.ReadAllText(pathToFile);
        }

        return fileData;
    }

    public static void CreateDirectory(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
    }

    public static void DeleteDirectory(string folderPath, bool recursive)
    {
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, recursive);
        }
    }

    public static string UnkBytesToStr(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        using StreamReader streamReader = new(stream);
        return streamReader.ReadToEnd();
    }

    public static DirectoryInfo[] GetDirectories(string folderPath, string searchPattern, SearchOption searchOption)
    {
        return new DirectoryInfo(folderPath).GetDirectories(searchPattern, searchOption);
    }

    public static FileInfo[] GetFiles(string folderPath, string searchPattern, SearchOption searchOption)
    {
        return new DirectoryInfo(folderPath).GetFiles(searchPattern, searchOption);
    }

    public static List<string> GetFiles(string folderPath, string searchPattern)
    {
        List<string> files = new();

        if (Directory.Exists(folderPath))
        {
            foreach (string file in Directory.EnumerateFiles(folderPath, searchPattern, SearchOption.AllDirectories))
            {
                files.Add(file);
            }
        }

        return files;
    }

    public static List<string> GetFiles(string folderPath, string searchPattern, bool withoutExtension = false)
    {
        List<string> files = new();

        if (Directory.Exists(folderPath))
        {
            foreach (string file in Directory.EnumerateFiles(folderPath, searchPattern, SearchOption.AllDirectories))
            {
                if (withoutExtension == true)
                {
                    files.Add(Path.GetFileNameWithoutExtension(file));
                }
                else
                {
                    files.Add(file);
                }
            }
        }

        return files;
    }

    public static void DeleteEmptyDirectories(string startPath)
    {
        if (string.IsNullOrEmpty(startPath))
        {
            throw new ArgumentException("Starting directory is a null reference or an empty string", nameof(startPath));
        }

        try
        {
            foreach (string directory in Directory.EnumerateDirectories(startPath))
            {
                DeleteEmptyDirectories(directory);
            }

            IEnumerable<string> entries = Directory.EnumerateFileSystemEntries(startPath);
            if (!entries.Any())
            {
                try
                {
                    if (Directory.Exists(startPath))
                    {
                        Directory.Delete(startPath);
                    }
                }
                catch (Exception e)
                {
                    LogBase.Error($"Failed to remove directory: {startPath}.");
                    LogBase.Error(e.ToString());
                }
            }
        }
        catch (Exception e)
        {
            LogBase.Error($"Failed to remove directory: {startPath}.");
            LogBase.Error(e.ToString());
        }
    }
}
