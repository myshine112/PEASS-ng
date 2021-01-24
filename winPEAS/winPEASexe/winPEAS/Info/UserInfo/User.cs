﻿using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Management;
using System.Security.Principal;
using winPEAS.Helpers;
using winPEAS.KnownFileCreds;

namespace winPEAS.Info.UserInfo
{
    internal static class User
    {
        public static List<string> GetMachineUsers(bool onlyActive, bool onlyDisabled, bool onlyLockout, bool onlyAdmins, bool fullInfo)
        {
            List<string> retList = new List<string>();
            
            try
            {
                foreach (ManagementObject user in Checks.Checks.Win32Users)
                {
                    if (onlyActive && !(bool)user["Disabled"] && !(bool)user["Lockout"]) retList.Add((string)user["Name"]);
                    else if (onlyDisabled && (bool)user["Disabled"] && !(bool)user["Lockout"]) retList.Add((string)user["Name"]);
                    else if (onlyLockout && (bool)user["Lockout"]) retList.Add((string)user["Name"]);
                    else if (onlyAdmins)
                    {
                        string domain = (string)user["Domain"];
                        if (string.Join(",", GetUserGroups((string)user["Name"], domain)).Contains("Admin")) retList.Add((string)user["Name"]);
                    }
                    else if (fullInfo)
                    {
                        string domain = (string)user["Domain"];
                        string userLine = user["Caption"] + ((bool)user["Disabled"] ? "(Disabled)" : "") + ((bool)user["Lockout"] ? "(Lockout)" : "") + ((string)user["Fullname"] != "false" ? "" : "(" + user["Fullname"] + ")") + (((string)user["Description"]).Length > 1 ? ": " + user["Description"] : "");
                        List<string> user_groups = GetUserGroups((string)user["Name"], domain);
                        string groupsLine = "";
                        if (user_groups.Count > 0)
                        {
                            groupsLine = "\n        |->Groups: " + string.Join(",", user_groups);
                        }
                        string passLine = "\n        |->Password: " + ((bool)user["PasswordChangeable"] ? "CanChange" : "NotChange") + "-" + ((bool)user["PasswordExpires"] ? "Expi" : "NotExpi") + "-" + ((bool)user["PasswordRequired"] ? "Req" : "NotReq") + "\n";
                        retList.Add(userLine + groupsLine + passLine);
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }

            return retList;
        }


        public static bool IsLocaluser(string UserName, string domain)
        {
            return Checks.Checks.CurrentAdDomainName != Checks.Checks.CurrentUserDomainName && domain != Checks.Checks.CurrentUserDomainName;
        }

        // https://stackoverflow.com/questions/3679579/check-for-groups-a-local-user-is-a-member-of/3681442#3681442
        public static List<string> GetUserGroups(string sUserName, string domain)
        {
            List<string> myItems = new List<string>();
            try
            {
                if (Checks.Checks.IsCurrentUserLocal && domain != Checks.Checks.CurrentUserDomainName)
                {
                    return myItems; //If local user and other domain, do not look
                }

                UserPrincipal oUserPrincipal = GetUser(sUserName, domain);
                if (oUserPrincipal != null)
                {
                    PrincipalSearchResult<Principal> oPrincipalSearchResult = oUserPrincipal.GetGroups();
                    foreach (Principal oResult in oPrincipalSearchResult)
                    {
                        myItems.Add(oResult.Name);
                    }
                }
                else
                {
                    Beaprint.GrayPrint("  [-] Controlled exception, info about " + domain + "\\" + sUserName + " not found");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return myItems;
        }

        public static UserPrincipal GetUser(string sUserName, string domain)
        {
            UserPrincipal user;
            try
            {
                if (Checks.Checks.IsPartOfDomain && !Checks.Checks.IsCurrentUserLocal) //Check if part of domain and notlocal users
                {
                    user = GetUserDomain(sUserName, domain) ?? GetUserLocal(sUserName);
                }
                else //If not part of a domain, then check local
                {
                    user = GetUserLocal(sUserName);
                }
            }
            catch
            { 
                //If error, then some error ocurred trying to find a user inside an unexistant domain, check if local user
                user = GetUserLocal(sUserName);
            }
            return user;
        }

        public static UserPrincipal GetUserLocal(string sUserName)
        {
            // Extract local user information
            //https://stackoverflow.com/questions/14594545/query-local-administrator-group
            var context = new PrincipalContext(ContextType.Machine);
            var user = new UserPrincipal(context);
            user.SamAccountName = sUserName;
            var searcher = new PrincipalSearcher(user);
            user = searcher.FindOne() as UserPrincipal;
            return user;
        }

        public static UserPrincipal GetUserDomain(string sUserName, string domain)
        {
            //if not local, try to extract domain user information
            //https://stackoverflow.com/questions/12710355/check-if-user-is-a-domain-user-or-local-user/12710452
            //var domainContext = new PrincipalContext(ContextType.Domain, Environment.UserDomainName);
            var domainContext = new PrincipalContext(ContextType.Domain, domain);
            UserPrincipal domainUser = UserPrincipal.FindByIdentity(domainContext, IdentityType.SamAccountName, sUserName);
            return domainUser;
        }

        public static List<string> GetLoggedUsers()
        {
            List<string> retList = new List<string>();
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserProfile WHERE Loaded = True");
                foreach (ManagementObject user in searcher.Get())
                {
                    string username = new SecurityIdentifier(user["SID"].ToString()).Translate(typeof(NTAccount)).ToString();
                    if (!username.Contains("NT AUTHORITY")) retList.Add(username);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return retList;
        }

        public static List<string> GetEverLoggedUsers()
        {
            List<string> retList = new List<string>();
            try
            {
                SelectQuery query = new SelectQuery("Win32_UserProfile");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                foreach (ManagementObject user in searcher.Get())
                {
                    try
                    {
                        string username = new SecurityIdentifier(user["SID"].ToString()).Translate(typeof(NTAccount)).ToString();
                        if (!username.Contains("NT AUTHORITY"))
                        {
                            retList.Add(username);
                        }
                    }
                    // user SID could not be translated, ignore
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return retList;
        }

        public static List<string> GetUsersFolders()
        {
            return MyUtils.ListFolder("Users");
        }

        public static HashSet<string> GetOtherUsersFolders()
        {
            HashSet<string> result = new HashSet<string>();
            string currentUsername = Environment.UserName?.ToLower();
            var usersBaseDirectory = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "Users");

            SelectQuery query = new SelectQuery("Win32_UserAccount");
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                foreach (ManagementObject envVar in searcher.Get())
                {
                    string username = (string)envVar["Name"];
                    username = username?.ToLower();

                    if (currentUsername != username)
                    {
                        string userDirectory = Path.Combine(usersBaseDirectory, username);

                        if (Directory.Exists(userDirectory))
                        {
                            result.Add(userDirectory.ToLower());
                        }
                    }
                }
            }

            return result;
        }
    }
}
