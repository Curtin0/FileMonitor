using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace FileMonitor
{
    public static class MainProcess
    {
        public static Regex PathRegex = new Regex(@"^[A-Z]:\\(.+?\\)*.*$");

        public static Config Config = new Config();

        private static List<FileSystemWatcher> Watchers = new List<FileSystemWatcher>();

        static MainProcess()
        {
            var configStr = FileHelper.Read(Config.ConfigPath);
            try
            {
                if (!string.IsNullOrEmpty(configStr))
                    Config = JsonConvert.DeserializeObject<Config>(configStr);
            }
            catch { }
        }

        public static void SaveConfig(List<Form> forms)
        {
            foreach (var form in forms)
            {
                Config.WindowPos[form.Name] = new Pos(form.Top, form.Left, form.Height, form.Width);
            }
            FileHelper.Write(Config.ConfigPath, JsonConvert.SerializeObject(Config));
        }

        public static void InitWatchers()
        {
            Watchers.ForEach(t => t.Dispose());
            Watchers = new List<FileSystemWatcher>();
            //为设置的路径初始化监视器
            if (Config?.FilePaths?.Count != 0)
            {
                foreach (var item in Config.FilePaths)
                {
                    if (item.OriginPath == null || item.BackupPath == null)
                        continue;
                    if (item.Started && PathRegex.IsMatch(item.OriginPath) && PathRegex.IsMatch(item.BackupPath))
                    {
                        FileSystemWatcher watcher;
                        try
                        {
                            watcher = new FileSystemWatcher(item.OriginPath)
                            {
                                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.Size,
                                Filter = "*"
                            };
                            watcher.Created += Watcher_Changed;
                            watcher.Changed += Watcher_Changed;
                            watcher.Renamed += Watcher_Changed;

                            Watchers.Add(watcher);
                            watcher.EnableRaisingEvents = true;
                        }
                        catch (Exception ex)
                        {
                            new string[] { Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileMonitor", "Log.json" }.Write_Append(JsonConvert.SerializeObject(ex));
                            item.Started = false;
                        }
                    }
                    else
                    {
                        item.Started = false;
                    }
                }
            }
        }

        public static void Dispose(int index)
        {
            Watchers.FirstOrDefault(t => t.Path == Config.FilePaths[index].OriginPath)?.Dispose();
            Watchers.RemoveAll(t => t.Path == Config.FilePaths[index].OriginPath);
            Config.FilePaths[index].Started = false;
        }

        public static void InitWatcher(int index)
        {
            var item = Config.FilePaths[index];
            if (item.OriginPath == null || item.BackupPath == null)
                return;
            if (PathRegex.IsMatch(item.OriginPath) && PathRegex.IsMatch(item.BackupPath))
            {
                FileSystemWatcher watcher;
                try
                {
                    watcher = new FileSystemWatcher(item.OriginPath)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.Size,
                        Filter = "*"
                    };
                    watcher.Created += Watcher_Changed;
                    watcher.Changed += Watcher_Changed;
                    watcher.Renamed += Watcher_Changed;

                    Watchers.Add(watcher);
                    watcher.EnableRaisingEvents = true;
                    item.Started = true;
                }
                catch (Exception ex)
                {
                    new string[] { Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileMonitor", "Log.json" }.Write_Append(JsonConvert.SerializeObject(ex));
                    item.Started = false;
                }
            }
            else
            {
                item.Started = false;
            }
        }

        private static void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            try {
                Thread.Sleep(0);
                var originDic = e.FullPath.Replace($@"\{e.Name}", "");
                string targetDicPath = Config.FilePaths.First(t => t.OriginPath == originDic).BackupPath;//目标备份文件夹
                #region 备份目录下全部文件+文件夹
                //DirectoryInfo originRoot = new DirectoryInfo(originDic);
                //FileInfo[] originFiles = originRoot.GetFiles();

                //DirectoryInfo targetRoot = new DirectoryInfo(targetDicPath);
                //var targetFileNames = targetRoot.GetFiles().Select(t => t.Name);

                //var needBackup = originFiles;//.Where(t => !targetFileNames.Contains(t.Name));
                //foreach (var file in needBackup) {
                //    var backupFilePath = targetDicPath.TrimEnd('\\') + $@"\[{DateTime.Now:yyyy年MM月dd日 HH时mm分ss秒}]{file.Name}";
                //    file.CopyTo(backupFilePath, true);
                //}
                #endregion
                var file = new FileInfo(e.FullPath);
                //var folder = new FileInfo(e.FullPath);
                //var backupFilePath = targetDicPath.TrimEnd('\\') + $@"\{DateTime.Now:【yyyy-MM-dd_HH_mm_ss】}{file.Name}";  
                var backupFilePath = targetDicPath.TrimEnd('\\') + $@"\{DateTime.Now:【yyyy-MM-dd_HH_mm_ss】}{file.Name}";
                //var backupFilePath2 = targetDicPath.TrimEnd('\\') + $@"\{DateTime.Now:【yyyy-MM-dd_HH_mm_ss】}{2}";

                //file.CopyTo(backupFilePath, true);
                //folder.CopyTo(backupFilePath2, true);
                //CheckDirectory();

                CreateFolder(targetDicPath, $@"{DateTime.Now:yyyy-MM-dd_HH_mm_ss}");
                string dest = targetDicPath +"/"+ $@"{DateTime.Now:yyyy-MM-dd_HH_mm_ss}";
                //CreateFolder(targetDicPath, $@"{DateTime.Now:yyyy-MM-dd}");
                //string dest = targetDicPath +"/"+ $@"{DateTime.Now:yyyy-MM-dd}";
                CopyFolder(originDic, dest);
                //string[] folders = System.IO.Directory.GetDirectories(targetDicPath);
                //foreach (string folder in folders) {
                //    string name = Path.GetFileName(folder);
                //    string dest = Path.Combine(targetDicPath.TrimEnd('\\') + $@"\{DateTime.Now:【yyyy-MM-dd_HH_mm_ss】}", name);
                //    CopyFolder(folder, dest);//构建目标路径,递归复制文件
                //}
            }
            catch (Exception ex)
            {
                new string[] { Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileMonitor", "Log.json" }.Write_Append(JsonConvert.SerializeObject(ex));
            }

        }
        /// <summary>
         /// 复制文件夹及文件
        /// </summary>
         /// <param name="sourceFolder">原文件路径</param>
          /// <param name="destFolder">目标文件路径</param>
          /// <returns></returns>
          public static int CopyFolder(string sourceFolder, string destFolder)
          {
              try
             {
                 //如果目标路径不存在,则创建目标路径
                if (!System.IO.Directory.Exists(destFolder))
                {
                     System.IO.Directory.CreateDirectory(destFolder);
                 }
                 //得到原文件根目录下的所有文件
                 string[] files = System.IO.Directory.GetFiles(sourceFolder);
                 foreach (string file in files)
                 {
                     string name = System.IO.Path.GetFileName(file);
                     string dest = System.IO.Path.Combine(destFolder, name);
                    System.IO.File.Copy(file, dest);//复制文件
                 }
                 //得到原文件根目录下的所有文件夹
                string[] folders = System.IO.Directory.GetDirectories(sourceFolder);
                foreach (string folder in folders)
                     {
                         string name = System.IO.Path.GetFileName(folder);
                         string dest = System.IO.Path.Combine(destFolder, name);
                         CopyFolder(folder, dest);//构建目标路径,递归复制文件
                     }
                 return 1;
             }
             catch (Exception e)
             {
                     MessageBox.Show(e.Message);
                     return 0;
                 }

         }
        /// <summary>
        /// 创建新的文件夹
        /// </summary>
        private static void CheckDirectory() {
            //string path = Path.Combine(Application.StartupPath, dtpDate.Value.ToString("yyyy-MM-dd_HH:MM:ss"));
            //if (!Directory.Exists(path)) {
            //    Directory.CreateDirectory(path);
            //}
        }
        /// <summary>
        /// 创建文件夹
        /// </summary>
        /// <param name="dirPath">文件夹路径</param>
        /// <param name="name">文件夹名</param>
        public static void CreateFolder(string dirPath, string name) {
            foreach (string d in Directory.GetFileSystemEntries(dirPath)) {
                if (File.Exists(dirPath + @"\" + name)) {
                    Console.WriteLine("创建文件夹 " + name + " 失败,文件夹已经存在");
                    return;
                }
            }//end of for
            DirectoryInfo info = new DirectoryInfo(dirPath);
            info.CreateSubdirectory(name);
            //info.Parent.CreateSubdirectory(name);//可以在父目录生成文件夹，很方便

        }//end of CreateFolder

        /// <summary>
        /// 创建文件
        /// </summary>
        /// <param name="dirPath">文件路径</param>
        /// <param name="name">文件名</param>
        public static void CreateFile(string dirPath, string name) {
            foreach (string d in Directory.GetFileSystemEntries(dirPath)) {
                if (File.Exists(dirPath + @"\" + name)) {
                    Console.WriteLine("创建文件 " + name + " 失败,文件已经存在");
                    return;
                }
            }//end of for
            File.Create(dirPath + @"\" + name);
        }//end of CreateFile
    }
}
