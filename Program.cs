using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

class Program
{
    private static void CreateLogger()
    {
        var config = new LoggingConfiguration();
        var fileTarget = new FileTarget
        {
            FileName = "${basedir}/logs/${shortdate}.log",
            Layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss} [${uppercase:${level}}] ${message}",
        };
        config.AddRule(LogLevel.Trace, LogLevel.Fatal, fileTarget);
        LogManager.Configuration = config;
    }
    static void Main(string[] args)
    {
        // 1.不使用外部設定檔
        // CreateLogger();
        // 2.使用外部設定檔
        LogManager.Configuration = new XmlLoggingConfiguration("Configs/NLog.config");
        Logger logger = LogManager.GetCurrentClassLogger();

        logger.Trace("Start");

        // 檢查是否提供了根目錄路徑參數
        if (args.Length == 0)
        {
            Console.WriteLine("請輸入根目錄路徑：");
            string userInput = Console.ReadLine().Trim();
            if (string.IsNullOrEmpty(userInput))
            {
                logger.Warn("根目錄路徑不能為空");
                return;
            }
            args = new[] { userInput };
        }

        string rootFolderPath = args[0];

        if (!Directory.Exists(rootFolderPath))
        {
            logger.Warn($"根目錄 '{rootFolderPath}' 不存在。");
            return;
        }

        // 搜尋指定路徑下的所有資料夾
        var folders = Directory.GetDirectories(rootFolderPath, "*", SearchOption.AllDirectories);
        Array.Sort(folders, (x, y) => y.Length.CompareTo(x.Length));

        foreach (var folderPath in folders)
        {
            // 取得資料夾名稱
            var folderName = new DirectoryInfo(folderPath).Name;
            // 檢查是否有與其同名的父資料夾
            var parentFolder = new DirectoryInfo(folderPath).Parent;
            while (parentFolder != null && parentFolder.Name == folderName)
            {
                var subFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                foreach (var file in subFiles)
                {
                    var relativePath = file.Substring(folderPath.Length + 1);
                    var newFilePath = Path.Combine(parentFolder.FullName, relativePath);
                    var newFileDirectory = Path.GetDirectoryName(newFilePath);
                    if (!Directory.Exists(newFileDirectory))
                        Directory.CreateDirectory(newFileDirectory);
                    File.Move(file, newFilePath);
                    logger.Debug("Move " + file + " -> " + newFilePath);
                }
                var subFolders = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories);
                foreach (var subFolder in subFolders)
                {
                    var relativePath = subFolder.Substring(folderPath.Length + 1);
                    var newFolderPath = Path.Combine(parentFolder.FullName, relativePath);
                    if (!Directory.Exists(newFolderPath))
                    {
                        Directory.Move(subFolder, newFolderPath);
                        logger.Debug("Move " + subFolder + " -> " + newFolderPath);
                    }
                }

                // 刪除子資料夾
                if (Directory.Exists(folderPath))
                {
                    if (Directory.GetFiles(folderPath).Length == 0)
                    {

                        Directory.Delete(folderPath, true);
                        logger.Debug("delete " + folderPath);
                    }
                }

                folderName = parentFolder.FullName;
                parentFolder = parentFolder.Parent;
            }
        }
        logger.Trace("End");
    }
}