using System;
using System.Diagnostics;
using System.IO;

namespace PubComp.Building.NuGetPack
{
    /// <summary>
    /// Run as post-build on a project
    /// e.g. NuGetPack.exe ""$(ProjectPath)"" ""$(TargetPath)"" $(ConfigurationName) [nopkg]"
    /// </summary>
    public class Program
    {
        static string nuget_setting_file = "nuget-apikey.txt";
        static string publish_setting_file = "nuget-publish-flag.txt";
        public static void Main(string[] args)
        {

            Console.WriteLine("--JGWill mod--");

            CommandLineArguments cla;

            string apiKey;
            bool doPublish;

            doPublish = false;
            apiKey = "";

            string scde_conf_dir = Path.Combine(
                                       Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ".SCDE");

            string apiKeyFilePath =
                Path.Combine(scde_conf_dir, nuget_setting_file);

            string publish_setting_filepath = Path.Combine(scde_conf_dir, publish_setting_file);

            if (File.Exists(apiKeyFilePath))
            {
                //file exist
                apiKey = File.ReadAllText(apiKeyFilePath).Trim();
                if (apiKey.Length < 23) apiKey = ""; //not the api key

                if (File.Exists(publish_setting_filepath))
                {
                    string c = File.ReadAllText(publish_setting_filepath).Trim();
                    doPublish = (c == "1") || (c.ToLower() == "true");
                }
            }
            else
            {
                Directory.CreateDirectory(scde_conf_dir);
                Console.WriteLine("--------IMPORTANT------------------");
                Console.WriteLine("-- A File where to put your key was created under : " + apiKeyFilePath);
                Console.WriteLine("-- FILL YOUR KEY THERE --");
                File.WriteAllText(apiKeyFilePath, "API_key_here");
                File.WriteAllText(publish_setting_filepath, "true");
                Process.Start("notepad.exe", apiKeyFilePath);
                Environment.Exit(4);
            }

            //if (File.Exists(nuget_setting_file))
            //{


            //}
            //else
            //{
            //    Console.WriteLine("no api key in file : " + nuget_setting_file);
            //    Console.WriteLine("no 1 or true in file to enable publishing : " + publish_setting_file);
            //    File.WriteAllText(nuget_setting_file, "API_key_here");
            //    File.WriteAllText(publish_setting_file, "false");
            //    Console.WriteLine("The two files were created so you can config them");

            //}


            if (!TryParseArguments(args, out cla))
            {
                WriteError();
                return;
            }

            var creator = new NuspecCreator();

            if (cla.Mode != Mode.Solution)
            {
                creator.CreatePackage(
                    cla.ProjPath, cla.DllPath, cla.IsDebug, cla.DoCreateNuPkg, cla.DoIncludeCurrentProj, cla.PreReleaseSuffixOverride, doPublish, apiKey);
            }
            else
            {
                creator.CreatePackages(
                    cla.BinFolder, cla.SolutionFolder, cla.IsDebug, cla.DoCreateNuPkg, cla.DoIncludeCurrentProj, cla.PreReleaseSuffixOverride);
            }
        }

        public enum Mode { Solution, Project };

        public class CommandLineArguments
        {
            public Mode Mode { get; set; }
            public string ProjPath { get; set; }
            public string DllPath { get; set; }
            public string BinFolder { get; set; }
            public string SolutionFolder { get; set; }
            public bool IsDebug { get; set; }
            public bool DoCreateNuPkg { get; set; }
            public bool DoIncludeCurrentProj { get; set; }
            public string PreReleaseSuffixOverride { get; set; }
        }

        public static bool TryParseArguments(
            string[] args,
            out CommandLineArguments commandLineArguments)
        {
            Mode mode = Mode.Project;
            Mode? modeVar = null;
            string projPath = null;
            string dllPath = null;
            bool isDebug = false;
            bool doCreateNuPkg = true;
            bool doIncludeCurrentProj = false;
            string binFolder = null;
            string solutionFolder = null;
            string preReleaseSuffixOverride = null;
            commandLineArguments = null;

            string config = null;

            foreach (var arg in args)
            {
                if (arg.ToLower() == "solution")
                {
                    if (modeVar.HasValue)
                        return false;

                    modeVar = Mode.Solution;
                }
                else if (arg.ToLower() == "project")
                {
                    if (modeVar.HasValue)
                        return false;

                    modeVar = Mode.Project;
                }
                else if (arg.ToLower().StartsWith("bin="))
                {
                    if (binFolder != null)
                        return false;

                    binFolder = arg.Substring(4);
                }
                else if (arg.ToLower().StartsWith("sln=") || arg.ToLower().StartsWith("src="))
                {
                    if (solutionFolder != null)
                        return false;

                    solutionFolder = arg.Substring(4);
                }
                else if (arg.EndsWith(".csproj"))
                {
                    if (projPath != null)
                        return false;

                    projPath = arg;
                }
                else if (arg.EndsWith(".dll"))
                {
                    if (dllPath != null)
                        return false;

                    dllPath = arg;
                }
                else if (arg.ToLower() == "debug" || arg.ToLower() == "release")
                {
                    if (config != null)
                        return false;

                    config = arg;
                }
                else if (arg.ToLower() == "nopkg")
                {
                    if (doCreateNuPkg == false)
                        return false;

                    doCreateNuPkg = false;
                }
                else if (arg.ToLower() == "includecurrentproj")
                {
                    if (doIncludeCurrentProj == true)
                        return false;

                    doIncludeCurrentProj = true;
                }
                else if (arg.ToLower().StartsWith("pre=") || arg.ToLower().StartsWith("prereleasesuffixoverride="))
                {
                    if (preReleaseSuffixOverride != null)
                        return false;

                    preReleaseSuffixOverride = arg.Substring(arg.IndexOf('=') + 1);

                    if (preReleaseSuffixOverride.StartsWith("-"))
                        preReleaseSuffixOverride = preReleaseSuffixOverride.Substring(1);
                }
                else
                {
                    return false;
                }
            }

            if (modeVar != Mode.Solution)
            {
                if (projPath == null || dllPath == null)
                    return false;

                if (binFolder != null || solutionFolder != null)
                    return false;
            }
            else
            {
                if (binFolder == null || solutionFolder == null)
                    return false;

                if (projPath != null || dllPath != null)
                    return false;
            }

            mode = modeVar ?? Mode.Project;
            isDebug = (config ?? string.Empty).ToLower() == "debug";

            commandLineArguments = new CommandLineArguments
            {
                Mode = mode,
                ProjPath = projPath,
                DllPath = dllPath,
                BinFolder = binFolder,
                SolutionFolder = solutionFolder,
                IsDebug = isDebug,
                DoCreateNuPkg = doCreateNuPkg,
                DoIncludeCurrentProj = doIncludeCurrentProj,
                PreReleaseSuffixOverride = preReleaseSuffixOverride,
            };

            return true;
        }

        private static void WriteError()
        {
            Console.WriteLine(
                @"Correct usage: NuGetPack.exe [project] <pathToCsProj> <pathToDll> [<Debug|Release>] [nopkg] [pre=|preReleaseSuffixOverride=<suffixForPreRelease>]");
            Console.WriteLine(
                @"Via post build event: NuGetPack.exe [project] ""$(ProjectPath)"" ""$(TargetPath)"" $(ConfigurationName)");
            Console.WriteLine(
                @"or: NuGetPack.exe [project] ""$(ProjectPath)"" ""$(TargetPath)"" $(ConfigurationName) nopkg");
            Console.WriteLine();
            Console.WriteLine(
                @"or for solution level:");
            Console.WriteLine();
            Console.WriteLine(
                @"Correct usage: NuGetPack.exe solution bin=<binFolder> src=<solutionFolder> [<Debug|Release>] [nopkg]");
        }
    }
}
