using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShowListCreator
{
    static class Program
    {
        static int playListSize = 60;
        static Boolean isRelease;
        static String currentDirectory;
        static Boolean shouldRandomize = true;
        static ArrayList directoryList = new ArrayList();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            isRelease = isReleaseVersion();
            if (isRelease)
                currentDirectory = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
            else
                currentDirectory = Directory.GetCurrentDirectory();

            String showsBaseDirectory = getSetting("showsDirectory");
            String showString = String.Join(" ", args);
            String videoFileTypes = "*.webm|*.mkv|*.flv|*.avi|*.mov|*.wmv|*.mp4|*.mpg|*.mpeg|*.m4v";
            Regex showRegex = new Regex(showString, RegexOptions.IgnoreCase);
            ArrayList fileList = new ArrayList();
            //directoryList = new ArrayList();

            if (!showsBaseDirectory.Equals("-1") && Directory.Exists(showsBaseDirectory))
            {
                //directoryList.AddRange(Directory.GetDirectories(showsBaseDirectory, "*", SearchOption.AllDirectories));
                ShowAllFoldersUnder(showsBaseDirectory, 0);
                foreach (String p in directoryList)
                {
                    if(showRegex.IsMatch(p)) {
                        String currentlyOnPath = p + "\\_currentlyOn.txt";
                        if (File.Exists(currentlyOnPath))
                        {
                            shouldRandomize = false;
                            startProgram(currentlyOnPath,"");
                            System.IO.StreamReader currentlyOnFile = new System.IO.StreamReader(currentlyOnPath);
                            String currentEpisode = currentlyOnFile.ReadLine().ToLower();
                            Boolean currentEpisodeHit = false;
                            foreach (String f in Directory.EnumerateFiles(p))
                            {
                                Console.WriteLine("!!! Filename: " + f);
                                if(f.ToLower().Contains(currentEpisode))
                                {
                                    currentEpisodeHit = true;
                                    fileList.Clear();
                                    fileList.Add(f);
                                }
                                else
                                {
                                    if (currentEpisodeHit && !f.ToLower().Contains(".txt"))
                                        fileList.Add(f);
                                    else
                                        Console.WriteLine("\t### Haven't hit desired start file yet");
                                }
                            }
                        }
                        else 
                            fileList.AddRange((Directory.EnumerateFiles(p).Where(s => videoFileTypes.Contains(Path.GetExtension(s).ToLower()))).ToList());
                    }
                }

                if (fileList.Count > 0)
                    createPlayList(fileList);
                else
                    createTooltip("### FileList enumeration error, possibly bad regex");
            }
            else
            {
                createTooltip("### Error trying to retrieve showDirectory\n" +
                                    "SettingsPath: " + showsBaseDirectory);
            }
        }

        private static void ShowAllFoldersUnder(string path, int indent)
        {
            try
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                {
                    foreach (string folder in Directory.GetDirectories(path))
                    {
                        //Console.WriteLine("{0}{1}", new string(' ', indent), Path.GetFileName(folder));
                        directoryList.Add(folder);
                        ShowAllFoldersUnder(folder, indent + 2);
                    }
                }
            }
            catch (UnauthorizedAccessException ex) {
                //Console.WriteLine("Exception: " + ex);
            }
        }

        static void createPlayList(ArrayList fileList)
        {
            String videoPlayer = getSetting("videoPlayer");
            String playlistPath = @"R:\zzDelete\zztemp.m3u";
            ArrayList playListFiles = new ArrayList();
            if (shouldRandomize)
            {
                Random rnd = new Random();
                for (int i = 0; i < playListSize; i++)
                {
                    int rand = rnd.Next(0, (fileList.Count) - 1);
                    playListFiles.Add(fileList[rand]);
                    fileList.RemoveAt(rand);                    // prevent duplicates in playlist
                }
            }
            else
                playListFiles = fileList;

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(playlistPath))
            {
                foreach (String s in playListFiles)
                {
                    file.WriteLine(s);
                }
            }

            startProgram(videoPlayer, playlistPath + " --extraintf=rc --rc-host=localhost:1234 --rc-quiet --rc-show-pos --fullscreen");
        }
        static void createTooltip(String info)
        {
            String tooltipPath = currentDirectory + "\\ToolTipper.exe";
            startProgram(tooltipPath, info);

            if(!isRelease)
                Console.WriteLine(info);
        }
        static String getSetting(String whichSetting)
        {
            String path = currentDirectory;
            String ret = "-1";
            String settingsPath = "";
            if(Directory.Exists(path))
            {
                settingsPath = Directory.GetParent(path).FullName + "\\settings.ini";
            }
            if(File.Exists(settingsPath))
            {
                String line;
                char delimiter = '=';
                System.IO.StreamReader settingsFile = new System.IO.StreamReader(settingsPath);
                while ((line = settingsFile.ReadLine()) != null)
                {
                    String[] substrings = line.Split(delimiter);
                    if(!substrings[0].Equals(null) && substrings[0].Equals(whichSetting))
                    {
                        ret = substrings[1];
                    }
                }

                settingsFile.Close();
            }

            return ret;
        }
        static void startProgram(String fileName, String arguments)
        {
            System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
            pProcess.StartInfo.FileName = fileName;
            pProcess.StartInfo.Arguments = arguments;
            pProcess.StartInfo.CreateNoWindow = true;
            //Console.WriteLine("!!! Starting Program: " + fileName);
            pProcess.Start();
        }
        static Boolean isReleaseVersion()
        {
            Assembly assembly = typeof(Program).Assembly;
            object[] attributes = assembly.GetCustomAttributes(typeof(DebuggableAttribute), true);
            if (attributes == null || attributes.Length == 0)
                return true;

            var d = (DebuggableAttribute)attributes[0];
            if ((d.DebuggingFlags & DebuggableAttribute.DebuggingModes.Default) == DebuggableAttribute.DebuggingModes.None)
                return true;

            return false;
        }
    }
}
