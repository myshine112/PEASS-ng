﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Principal;
using winPEAS.Helpers;
using winPEAS.Helpers.AppLocker;
using winPEAS.Helpers.Search;
using winPEAS.Info.UserInfo;

namespace winPEAS.Checks
{
    public static class Checks
    {
        public static bool IsNoColor = false;
        public static bool ExecCmd = false;
        public static bool Banner = true;
        public static bool IsSearchFast = true;
        public static int SearchTime = 50;
        public static bool IsDebug = false;

        // Create Dynamic blacklists
        public static readonly string CurrentUserName = Environment.UserName;
        public static string CurrentUserDomainName = Environment.UserDomainName;
        public static string CurrentAdDomainName = "";
        public static bool IsPartOfDomain = false;
        public static bool IsCurrentUserLocal = true;
        public static ManagementObjectCollection Win32Users = null;
        public static Dictionary<string, string> CurrentUserSiDs = new Dictionary<string, string>();
        static string _paintActiveUsers = "";
        public static string PaintActiveUsersNoAdministrator = "";
        public static string PaintDisabledUsers = "";
        public static string PaintDisabledUsersNoAdministrator = "";
        //static string paint_lockoutUsers = "";
        public static string PaintAdminUsers = "";

        private static List<SystemCheck> _systemChecks;
        private static HashSet<string> _systemCheckSelectedKeysHashSet = new HashSet<string>();

        public const string LogFile = "out.txt";


        class SystemCheck
        {           
            public string Key { get; }
            public ISystemCheck Check { get; }

            public SystemCheck(string key, ISystemCheck check)
            {
                this.Key = key;
                this.Check = check;
            }
        }

        internal static void Run(string[] args)
        {
            //Check parameters
            bool isAllChecks = true;
            bool wait = false;
            FileStream fileStream = null;
            StreamWriter fileWriter = null;
            TextWriter oldOut = Console.Out;

            _systemChecks = new List<SystemCheck>
            {
                new SystemCheck("systeminfo", new SystemInfo()),
                new SystemCheck("userinfo", new UserInfo()),
                new SystemCheck("procesinfo", new ProcessInfo()),
                new SystemCheck("servicesinfo", new ServicesInfo()),
                new SystemCheck("applicationsinfo", new ApplicationsInfo()),
                new SystemCheck("networkinfo", new NetworkInfo()),
                new SystemCheck("windowscreds", new WindowsCreds()),
                new SystemCheck("browserinfo", new BrowserInfo()),
                new SystemCheck("filesinfo", new FilesInfo()),
            };

            var systemCheckAllKeys = new HashSet<string>(_systemChecks.Select(i => i.Key));

            foreach (string arg in args)
            {
                if (string.Equals(arg, "--help", StringComparison.CurrentCultureIgnoreCase) ||
                    string.Equals(arg, "help", StringComparison.CurrentCultureIgnoreCase) ||
                    string.Equals(arg, "/h", StringComparison.CurrentCultureIgnoreCase) ||
                    string.Equals(arg, "-h", StringComparison.CurrentCultureIgnoreCase))
                {
                    Beaprint.PrintUsage();
                    return;
                }

                if (string.Equals(arg, "log", StringComparison.CurrentCultureIgnoreCase))
                {
                    try
                    {
                        fileStream = new FileStream(LogFile, FileMode.OpenOrCreate, FileAccess.Write);
                        fileWriter = new StreamWriter(fileStream);
                    }
                    catch (Exception ex)
                    {
                        Beaprint.PrintException($"Cannot open \"{LogFile}\" for writing:\n {ex.Message}");
                        return;
                    }

                    Beaprint.ColorPrint($"\"log\" argument present, redirecting output to file \"{LogFile}\"", Beaprint.ansi_color_good);
                    Console.SetOut(fileWriter);
                }

                if (string.Equals(arg, "cmd", StringComparison.CurrentCultureIgnoreCase))
                {
                    ExecCmd = true;
                }

                if (string.Equals(arg, "notcolor", StringComparison.CurrentCultureIgnoreCase))
                {
                    IsNoColor = true;
                }

                if (string.Equals(arg, "quiet", StringComparison.CurrentCultureIgnoreCase))
                {
                    Banner = false;
                }

                if (string.Equals(arg, "searchslow", StringComparison.CurrentCultureIgnoreCase))
                {
                    IsSearchFast = false;
                }

                if (string.Equals(arg, "wait", StringComparison.CurrentCultureIgnoreCase))
                {
                    wait = true;
                }

                if (string.Equals(arg, "debug", StringComparison.CurrentCultureIgnoreCase))
                {
                    IsDebug = true;
                }

                string argToLower = arg.ToLower();
                if (systemCheckAllKeys.Contains(argToLower))
                {
                    _systemCheckSelectedKeysHashSet.Add(argToLower);
                    isAllChecks = false;
                }
            }

            try
            {
                CheckRunner.Run(() =>
                {
                    //Start execution
                    if (IsNoColor)
                    {
                        Beaprint.DeleteColors();
                    }
                    else
                    {
                        CheckRegANSI();
                    }

                    Beaprint.PrintInit();

                    CheckRunner.Run(CreateDynamicLists, IsDebug);

                    RunChecks(isAllChecks, wait);
                }, IsDebug, "Total time");

                if (IsDebug)
                {
                    MemoryHelper.DisplayMemoryStats();
                }
            }
            finally
            {
                Console.SetOut(oldOut);

                fileWriter?.Close();
                fileStream?.Close();
            }
        }

        private static void RunChecks(bool isAllChecks, bool wait)
        {
            for (int i = 0; i < _systemChecks.Count; i++)
            {
                var systemCheck = _systemChecks[i];

                if (_systemCheckSelectedKeysHashSet.Contains(systemCheck.Key) || isAllChecks)
                {
                    systemCheck.Check.PrintInfo(IsDebug);

                    if ((i < _systemCheckSelectedKeysHashSet.Count - 1) && wait)
                    {
                        WaitInput();
                    }
                }
            }
        }

        private static void CreateDynamicLists()
        {
            try
            {
                Beaprint.GrayPrint("   Creating Dynamic lists, this could take a while, please wait...");
                Beaprint.GrayPrint("   - Checking if domain...");
                CurrentAdDomainName = DomainHelper.IsDomainJoined();
                IsPartOfDomain = !string.IsNullOrEmpty(CurrentAdDomainName);
                IsCurrentUserLocal = CurrentAdDomainName != CurrentUserDomainName;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while getting AD info: " + ex);
            }

            try
            {
                Beaprint.GrayPrint("   - Getting Win32_UserAccount info...");
                var query = new SelectQuery("Win32_UserAccount");

                using (var searcher = new ManagementObjectSearcher(query))
                {
                    Win32Users = searcher.Get();
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while getting Win32_UserAccount info: " + ex);
            }

            try
            {
                Beaprint.GrayPrint("   - Creating current user groups list...");
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                CurrentUserSiDs[identity.User.ToString()] = Environment.UserName;
                IdentityReferenceCollection currentSIDs = identity.Groups;
                foreach (IdentityReference group in identity.Groups)
                {
                    string gName = "";
                    try
                    {
                        gName = UserInfoHelper.SID2GroupName(group.ToString());
                    }
                    catch (Exception ex)
                    {
                        Beaprint.GrayPrint("Error obtaining current SIDs: " + ex);
                    }
                    CurrentUserSiDs[group.ToString()] = gName;
                }

            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating current user groups list: " + ex);
            }

            try
            {
                Beaprint.GrayPrint("   - Creating active users list...");
                _paintActiveUsers = string.Join("|", User.GetMachineUsers(true, false, false, false, false));
                PaintActiveUsersNoAdministrator = _paintActiveUsers.Replace("|Administrator", "").Replace("Administrator|", "").Replace("Administrator", "");
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating active users list: " + ex);
            }

            try
            {
                Beaprint.GrayPrint("   - Creating disabled users list...");
                Checks.PaintDisabledUsers = string.Join("|", User.GetMachineUsers(false, true, false, false, false));
                PaintDisabledUsersNoAdministrator = Checks.PaintDisabledUsers.Replace("|Administrator", "").Replace("Administrator|", "").Replace("Administrator", "");
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating disabled users list: " + ex);
            }

            //paint_lockoutUsers = string.Join("|", UserInfo.GetMachineUsers(false, false, true, false, false));

            try
            {
                Beaprint.GrayPrint("   - Admin users list...");
                PaintAdminUsers = string.Join("|", User.GetMachineUsers(false, false, false, true, false));
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating admin users groups list: " + ex);
            }

            //create AppLocker lists
            try
            {
                Beaprint.GrayPrint("   - Creating AppLocker bypass list...");
                AppLockerHelper.CreateLists();
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating AppLocker bypass list: " + ex);
            }

            //create the file lists
            try
            {
                Beaprint.GrayPrint("   - Creating files/directories list for search...");
                SearchHelper.CreateSearchDirectoriesList();
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating directory list: " + ex);
            }
        }

        private static void CheckRegANSI()
        {
            try
            {
                if (RegistryHelper.GetRegValue("HKCU", "CONSOLE", "VirtualTerminalLevel") == "" && RegistryHelper.GetRegValue("HKCU", "CONSOLE", "VirtualTerminalLevel") == "")
                    System.Console.WriteLine(@"ANSI color bit for Windows is not set. If you are execcuting this from a Windows terminal inside the host you should run 'REG ADD HKCU\Console /v VirtualTerminalLevel /t REG_DWORD /d 1' and then start a new CMD");
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while checking ansi color registry: " + ex);
            }
        }

        private static void WaitInput()
        {
            Console.Write("\n -- Press a key to continue... ");
            Console.ReadLine();
        }
    }
}
