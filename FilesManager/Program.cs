using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
namespace FilesManager
{
    class Program
    {
        static void Main(string[] args)
        {
            
            Configuration roaming = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
            var fileMap = new ExeConfigurationFileMap { ExeConfigFilename = roaming.FilePath };
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            config.AppSettings.Settings.Clear();
            string curPath = null;
            if (config.AppSettings.Settings.Count == 0)
            {
                curPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // задаем путь по умолчанию при первом запуске приложения
            }
            else
            {
                curPath = config.AppSettings.Settings["CurrentPath"].Value;
            }    
            var treeLevel = 2;
            var result = new List<string>();
            ShowFilesAndDirsTree(curPath, treeLevel, ref result);
            PrintPage(result, 1);
            bool exit = false;
            while (!exit)
            {
                var tuple = CatchUserCmd();
                var userInput = tuple.Item2;
                var command = tuple.Item1;
                switch (command)
                {
                    case "exit":                       
                        exit = true; 
                        break;
                    case "pg":
                        bool exitProcess = false;
                        var curPage = 1;
                        while (!exitProcess)
                        {                    
                            var userKey = Console.ReadKey();
                            if (userKey.Key != ConsoleKey.Q)
                                Paging(result, ref curPage, userKey);
                            else
                            {
                                exitProcess = true;
                                break;
                            }

                        }
                        break;
                    case "cd":
                        string newPath = null;               
                        ChangeDir(ref newPath, userInput, ref config);
                        Console.Clear();
                        result.Clear();
                        ShowFilesAndDirsTree(newPath, treeLevel, ref result);
                        PrintPage(result, 1);
                        break;
                    case "cp":
                        var usrCmd = userInput.Split('"');
                        if (usrCmd.Length != 5)
                        {
                            Console.WriteLine("Неверно введена команда");
                        }
                        string sourceName = usrCmd[1];
                        string destName = usrCmd[3];

                        if (Directory.Exists(sourceName))
                        {
                            CopyDir(sourceName, destName);
                        }
                        else if (File.Exists(sourceName))
                        {
                            CopyFile(sourceName, destName);
                        }
                        else
                            Console.WriteLine("Неверно указан путь источника");
                        break;
                    case "dl":
                        var usrsCmd = userInput.Split('"');
                        if (usrsCmd.Length != 3)
                        {
                            Console.WriteLine("Неверно введена команда");
                        }
                        string mustDel = usrsCmd[1];
                        Delete(mustDel);
                        break;
                    case "inm":
                        var usrsCmnd = userInput.Split('"');
                        if (usrsCmnd.Length != 3)
                        {
                            Console.WriteLine("Неверно введена команда");
                        }
                        string target = usrsCmnd[1];
                        Info(target);
                        break;
                }
            }
                
        }
        static void ShowFilesAndDirsTree(string path, int treeLevel, ref List<string> files)
        {
            try
            {
                if (treeLevel > 1)
                {
                    string[] subdirectoryEntries = Directory.GetDirectories(path);
                    foreach (string subdirectory in subdirectoryEntries)
                        ShowFilesAndDirsTree(subdirectory, treeLevel - 1,ref files);
                }
                string[] fileEntries = Directory.GetFileSystemEntries(path);
                foreach (string fileName in fileEntries)
                {                   
                    files.Add(fileName);
                }
            }
            catch (ArgumentNullException)
            {
                System.Console.WriteLine("Путь пуст");
            }
            catch (UnauthorizedAccessException)
            {}
            catch (ArgumentException)
            {
                System.Console.WriteLine("Неверное написание пути, попробуйте снова");
            }
            catch (DirectoryNotFoundException)
            {
                System.Console.WriteLine("Путь не существует");
            }
        }
        static void PrintPage (List<string>tree, int numOfPage)
        {
            var numOfStringsPerPage = Convert.ToInt32(ConfigurationManager.AppSettings["NumOfStringsForPage"]);
            Console.Clear();
            var numOfAllPages = (int)Math.Ceiling((decimal)tree.Count / numOfStringsPerPage);
            if (numOfPage == 1 && numOfAllPages > 1)
            {
                for (var i = 0; i < numOfStringsPerPage; i++)
                {
                    Console.WriteLine(tree[i]);
                }
            }
            else if (numOfPage == numOfAllPages)
            {
                if (numOfAllPages > 1)
                {
                    for (var i = ((numOfAllPages - 1) * numOfStringsPerPage - 1); i < tree.Count; i++)
                    {
                        Console.WriteLine(tree[i]);
                    }
                }
                else
                {
                    for (var i = 0; i < tree.Count; i++)
                    {
                        Console.WriteLine(tree[i]);
                    }
                }
                
            }
            else
            {
                for (var i = ((numOfPage - 1) * numOfStringsPerPage); i < ((numOfPage - 1) * numOfStringsPerPage + numOfStringsPerPage - 1); i++)
                {
                    Console.WriteLine(tree[i]);
                }
            }
            Console.WriteLine();
            Console.Write("==============================================================================");
            Console.WriteLine();
        }
        static void Paging (List<string> result, ref int curPage, ConsoleKeyInfo userKey)
        {
            var numOfStringsPerPage = Convert.ToInt32(ConfigurationManager.AppSettings["NumOfStringsForPage"]);
            var numOfAllPages = (int)Math.Ceiling((decimal)result.Count / numOfStringsPerPage);
            if (userKey.Key == ConsoleKey.RightArrow)
            {
                if (curPage < numOfAllPages)
                {
                    curPage += 1;
                    PrintPage(result, curPage);
                }
            }
            if (userKey.Key == ConsoleKey.LeftArrow)
            {
                if (curPage > 1)
                {
                    curPage -= 1;
                    PrintPage(result, curPage);
                }
            }
        }
        static void ChangeDir (ref string newPath, string usersInput, ref Configuration config)
        {
            var usrCmd = usersInput.Split('"');
            if (usrCmd.Length != 3)
            {
                Console.WriteLine("Неверно введена команда");
                return;
            }

            var destPath = usrCmd[1];
            if (!Directory.Exists(destPath))
                Console.WriteLine("Данной директории не существует");
            else
            {
                newPath = destPath;
                config.AppSettings.Settings.Clear();
                config.AppSettings.Settings.Add("CurrentPath", newPath);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
            }
                
        
        }
        static void CopyFile(string sourceFileName, string destFileName)
        {
            try
            {
                File.Copy(sourceFileName, destFileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        static void CopyDir(string sourceDirName, string destDirName)
           {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(sourceDirName);
                DirectoryInfo[] dirs = dir.GetDirectories();

                if (!Directory.Exists(destDirName))
                {
                    Directory.CreateDirectory(destDirName);
                }

                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string temppath = Path.Combine(destDirName, file.Name);
                    file.CopyTo(temppath, true);
                }

                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    CopyDir(subdir.FullName, temppath);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
           }
        static Tuple<string, string> CatchUserCmd()
        {
            bool rightCmd = false;
            string input;
            string[] usrCmd;
            do
            {
                input = Console.ReadLine();
                usrCmd = input.Split(' ');
                if (usrCmd[0] != "exit" && usrCmd[0] != "pg" && usrCmd[0] != "cd" && usrCmd[0] != "cp" && usrCmd[0] != "dl" && usrCmd[0] != "inm")
                    Console.WriteLine("Неверно введена команда");
                else
                    rightCmd = true;
            }
            while (rightCmd == false);
            return Tuple.Create(usrCmd[0], input);
        }
        static void Delete(string mustDel)
        {
            if (Directory.Exists(mustDel))
            {
                try
                {
                    Directory.Delete(mustDel, true);
                }

                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else if (File.Exists(mustDel))
            {
                try
                {
                    File.Delete(mustDel);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else
                Console.WriteLine("Неверно указан путь удаления");
        }
        static void Info (string target)
        {
            try
            {
                FileInfo file = new FileInfo(target);
                FileAttributes attributes = File.GetAttributes(target);
            
            if (Directory.Exists(target))
            {
                    Console.WriteLine($"Атрибуты директории {target}: {attributes}");
                }
            else if (File.Exists(target))
            {               
                Console.WriteLine($"Размер файла {target}: {file.Length} байт");
                Console.WriteLine($"Атрибуты файла {target}: {attributes}");
            }
            else
                Console.WriteLine("Неверно указан путь");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine();
            Console.Write("==============================================================================");
            Console.WriteLine();
        }
    }
}
