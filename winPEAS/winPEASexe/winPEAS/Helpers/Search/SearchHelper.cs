﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace winPEAS.Helpers.Search
{
    static class SearchHelper
    {
        private static List<CustomFileInfo> RootDirUsers;
        private static List<CustomFileInfo> RootDirCurrentUser;
        private static List<CustomFileInfo> ProgramFiles;
        private static List<CustomFileInfo> ProgramFilesX86;
        private static List<CustomFileInfo> DocumentsAndSettings;
        private static List<CustomFileInfo> GroupPolicyHistory;
       // private static List<CustomFileInfo> GroupPolicyHistoryLegacy;
       

        public static List<CustomFileInfo> GetFilesFast(string folder, string pattern = "*", HashSet<string> excludedDirs = null)
        {
            ConcurrentBag<CustomFileInfo> files = new ConcurrentBag<CustomFileInfo>();
            // ConcurrentBag<string> files = new ConcurrentBag<string>();

            //Beaprint.InfoPrint($"[*] folder 1: '{folder}'");

            IEnumerable<DirectoryInfo> startDirs = GetStartDirectories(folder, files, pattern);

            IList<DirectoryInfo> startDirsExcluded = new List<DirectoryInfo>();

            if (excludedDirs != null)
            {
                foreach (var startDir in startDirs)
                {
                    bool shouldAdd = true;
                    string startDirLower = startDir.FullName.ToLower();

                    foreach (var excludedDirPattern in excludedDirs)
                    {
                        if (Regex.IsMatch(startDirLower, excludedDirPattern, RegexOptions.IgnoreCase))
                        {
                            //files2.Add(file + $"  [pattern: '{pattern}']");
                            shouldAdd = false;
                            break;
                        }

                        //if (startDirLower.StartsWith(excludedDir))
                        //{
                        //    shouldAdd = false;
                        //    break;
                        //}
                    }

                    if (shouldAdd)
                    {
                        startDirsExcluded.Add(startDir);
                    }
                }


                //startDirsExcluded =
                //    (from startDir in startDirs
                //     from excludedDir in excludedDirs
                //     where !startDir.FullName.Contains(excludedDir)
                //     select startDir).ToList();
            }
            else
            {
                startDirsExcluded = startDirs.ToList();
            }


            // !!!! TODO
            // probably we need to exclude the excluded dirs here - not in parallel processing

            Parallel.ForEach(startDirsExcluded, (d) =>
            {
                Parallel.ForEach(GetStartDirectories(d.FullName, files, pattern), (dir) =>
                {
                    GetFiles(dir.FullName, pattern).ForEach(
                        (f) =>
                            //files.Add(f.FullName)
                            files.Add(new CustomFileInfo(f.Name, f.Extension, f.FullName))
                        );
                });
            });

            return files.ToList();
        }
       
        public static List<FileInfo> GetFiles(string folder, string pattern = "*")
        {
            DirectoryInfo dirInfo;
            DirectoryInfo[] directories;
            try
            {
                dirInfo = new DirectoryInfo(folder);
                directories = dirInfo.GetDirectories();

                if (directories.Length == 0)
                {
                    return new List<FileInfo>(dirInfo.GetFiles(pattern));
                }
            }
            catch (UnauthorizedAccessException)
            {
                return new List<FileInfo>();
            }
            catch (PathTooLongException)
            {
                return new List<FileInfo>();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<FileInfo>();
            }

            List<FileInfo> result = new List<FileInfo>();

            foreach (var d in directories)
            {
                result.AddRange(GetFiles(d.FullName, pattern));
            }

            try
            {
                result.AddRange(dirInfo.GetFiles(pattern));
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (PathTooLongException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }

            return result;
        }

        

        private static List<DirectoryInfo> GetStartDirectories(string folder, ConcurrentBag<CustomFileInfo> files, string pattern)
        {
            DirectoryInfo dirInfo = null;
            DirectoryInfo[] directories = null;
            try
            {
                dirInfo = new DirectoryInfo(folder);
                directories = dirInfo.GetDirectories();

                foreach (var f in dirInfo.GetFiles(pattern))
                {
                    //files.Add(f.FullName);
                    files.Add(new CustomFileInfo(f.Name, f.Extension, f.FullName));
                }

                if (directories.Length > 1)
                    return new List<DirectoryInfo>(directories);

                if (directories.Length == 0)
                    return new List<DirectoryInfo>();

            }
            catch (UnauthorizedAccessException ex)
            {
                return new List<DirectoryInfo>();
            }
            catch (PathTooLongException ex)
            {
                return new List<DirectoryInfo>();
            }
            catch (DirectoryNotFoundException ex)
            {
                return new List<DirectoryInfo>();
            }

            return GetStartDirectories(directories[0].FullName, files, pattern);
        }

        internal static void CreateSearchDirectoriesList()
        {
            string globalPattern = "*";
            string systemDrive = Environment.GetEnvironmentVariable("SystemDrive");

            // c:\users
            string rootUsersSearchPath = $"{systemDrive}\\Users\\";
            SearchHelper.RootDirUsers = SearchHelper.GetFilesFast(rootUsersSearchPath, globalPattern);

            // c:\users\current_user
            string rootCurrentUserSearchPath = Environment.GetEnvironmentVariable("USERPROFILE");
            SearchHelper.RootDirCurrentUser = SearchHelper.GetFilesFast(rootCurrentUserSearchPath, globalPattern);

            // c:\Program Files\
            string rootProgramFiles = $"{systemDrive}\\Program Files\\";
            SearchHelper.ProgramFiles = SearchHelper.GetFilesFast(rootProgramFiles, globalPattern);

            // c:\Program Files (x86)\
            string rootProgramFilesX86 = $"{systemDrive}\\Program Files (x86)\\";
            SearchHelper.ProgramFilesX86 = SearchHelper.GetFilesFast(rootProgramFilesX86, globalPattern);

            // c:\Documents and Settings\
            string documentsAndSettings = $"{systemDrive}\\Documents and Settings\\";
            SearchHelper.DocumentsAndSettings = SearchHelper.GetFilesFast(documentsAndSettings, globalPattern);

            // c:\ProgramData\Microsoft\Group Policy\History
            string groupPolicyHistory = $"{systemDrive}\\ProgramData\\Microsoft\\Group Policy\\History";
            SearchHelper.GroupPolicyHistory = SearchHelper.GetFilesFast(groupPolicyHistory, globalPattern);

            // c:\Documents and Settings\All Users\Application Data\\Microsoft\\Group Policy\\History
            string groupPolicyHistoryLegacy = $"{documentsAndSettings}\\All Users\\Application Data\\Microsoft\\Group Policy\\History";
            //SearchHelper.GroupPolicyHistoryLegacy = SearchHelper.GetFilesFast(groupPolicyHistoryLegacy, globalPattern);
            var groupPolicyHistoryLegacyFiles = SearchHelper.GetFilesFast(groupPolicyHistoryLegacy, globalPattern);

            SearchHelper.GroupPolicyHistory.AddRange(groupPolicyHistoryLegacyFiles);
        }

        internal static List<CustomFileInfo> SearchUserCredsFiles()
        {
            var result = new List<CustomFileInfo>();
            var patterns = new List<string>
            {
                ".*credential.*",
                ".*password.*"
            };

            foreach (var file in SearchHelper.RootDirUsers)
            {                
                //string extLower = file.Extension.ToLower();
                string nameLower = file.Filename.ToLower();
                //  string nameExtLower = nameLower + "." + extLower;

                foreach (var pattern in patterns)
                {
                    if (Regex.IsMatch(nameLower, pattern, RegexOptions.IgnoreCase))
                    {
                        result.Add(new CustomFileInfo(file.Filename, file.Extension, file.FullPath));

                        break;
                    }
                }
            }

            return result;
        }

        internal static List<string> SearchUsersInterestingFiles()
        {           
            //SearchHelper.FindFiles(searchPath, _patternsFileCreds, colorF);
            //string patterns = string.Join(";", patternsFileCreds);

            var result = new List<string>();

            foreach (var file in SearchHelper.RootDirCurrentUser)
            {
                // !!! too slow - regexp
                //foreach (var pattern in Patterns.PatternsFileCreds2)
                //{
                //    if (Regex.IsMatch(file, pattern, RegexOptions.IgnoreCase))
                //    {
                //        //files2.Add(file + $"  [pattern: '{pattern}']");
                //        files2.Add(file);
                //        break;
                //    }
                //}

                string extLower = file.Extension.ToLower();
                string nameLower = file.Filename.ToLower();
                //  string nameExtLower = nameLower + "." + extLower;

                if (Patterns.WhitelistExtensions.Contains(extLower) ||
                    //   Patterns.WhiteListFilenames.Contains(nameLower) ||
                    Patterns.WhiteListExactfilenamesWithExtensions.Contains(nameLower))
                {
                    result.Add(file.FullPath);
                }
                else
                {
                    foreach (var pattern in Patterns.WhiteListRegexp)
                    {
                        if (Regex.IsMatch(nameLower, pattern, RegexOptions.IgnoreCase))
                        {
                            result.Add(file.FullPath);

                            break;
                        }
                    }
                }
            }

            return result;
        }

        internal static List<string> FindCachedGPPPassword()
        {
            //SearchHelper.FindFiles(searchPath, _patternsFileCreds, colorF);
            //string patterns = string.Join(";", patternsFileCreds);

            var result = new List<string>();

            var allowedExtensions = new HashSet<string>
            {
                ".xml"
            };

            foreach (var file in SearchHelper.GroupPolicyHistory)
            {                
                string extLower = file.Extension.ToLower();                              

                if (allowedExtensions.Contains(extLower))
                {
                    result.Add(file.FullPath);
                }              
            }

            return result;
        }

        internal static List<string> SearchMcAfeeSitelistFiles()
        {
            var result = new List<string>();
           
            HashSet<string> allowedFilenames = new HashSet<string>()
            {
                "sitelist.xml"
            };

            //string[] searchLocations =
            //{
            //    $"{drive}\\Program Files\\",
            //    $"{drive}\\Program Files (x86)\\",
            //    $"{drive}\\Documents and Settings\\",
            //    $"{drive}\\Users\\",
            //};

            var searchFiles = new List<CustomFileInfo>();            
            searchFiles.AddRange(SearchHelper.ProgramFiles);
            searchFiles.AddRange(SearchHelper.ProgramFilesX86);
            searchFiles.AddRange(SearchHelper.DocumentsAndSettings);
            searchFiles.AddRange(SearchHelper.RootDirUsers);

            foreach (var file in searchFiles)
            {
                string filenameToLower = file.Filename.ToLower();

                if (allowedFilenames.Contains(filenameToLower))
                {
                    result.Add(file.FullPath);
                }
            }

            return result;
        }

        internal static List<string> SearchCurrentUserDocs()
        {
            var result = new List<string>();

            string patterns = "*diagram*;*.pdf;*.vsd;*.doc;*docx;*.xls;*.xlsx";

            var allowedRegexp = new List<string>
            {
                ".*diagram.*",
            };

            var allowedExtensions = new HashSet<string>()
            {
                ".doc",
                ".docx",
                ".vsd",
                ".xls",
                ".xlsx",
                ".pdf",
            };

            foreach (var file in SearchHelper.RootDirCurrentUser)
            {
                string extLower = file.Extension.ToLower();
                string nameLower = file.Filename.ToLower();
                //  string nameExtLower = nameLower + "." + extLower;

                if (allowedExtensions.Contains(extLower))
                {
                    result.Add(file.FullPath);
                }
                else
                {
                    foreach (var pattern in allowedRegexp)
                    {
                        if (Regex.IsMatch(nameLower, pattern, RegexOptions.IgnoreCase))
                        {
                            result.Add(file.FullPath);

                            break;
                        }
                    }
                }
            }

            return result;
        }

        internal static List<string> SearchUsersDocs()
        {
            var result = new List<string>();

            string patterns = "*diagram*;*.pdf;*.vsd;*.doc;*docx;*.xls;*.xlsx";

            var allowedRegexp = new List<string>
            {
                ".*diagram.*",
            };

            var allowedExtensions = new HashSet<string>()
            {
                ".doc",
                ".docx",
                ".vsd",
                ".xls",
                ".xlsx",
                ".pdf",
            };

            foreach (var file in SearchHelper.RootDirUsers)
            {
                string extLower = file.Extension.ToLower();
                string nameLower = file.Filename.ToLower();
                //  string nameExtLower = nameLower + "." + extLower;

                if (allowedExtensions.Contains(extLower))
                {
                    result.Add(file.FullPath);
                }
                else
                {
                    foreach (var pattern in allowedRegexp)
                    {
                        if (Regex.IsMatch(nameLower, pattern, RegexOptions.IgnoreCase))
                        {
                            result.Add(file.FullPath);

                            break;
                        }
                    }
                }
            }

            return result;
        }
    }
}
