﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using HP.WindowsPrison.Utilities;
using System.Globalization;

namespace HP.WindowsPrison.Restrictions
{
    class Filesystem : Rule
    {
        public const string prisonRestrictionsGroup = "prison_filesyscell";

        public override void Apply(Prison prison)
        {
            if (prison == null)
            {
                throw new ArgumentNullException("prison");
            }

            if (!WindowsUsersAndGroups.ExistsGroup(prisonRestrictionsGroup))
            {
                WindowsUsersAndGroups.CreateGroup(prisonRestrictionsGroup);
            }

            WindowsUsersAndGroups.AddUserToGroup(prison.User.UserName, prisonRestrictionsGroup);

            if (Directory.Exists(prison.Configuration.PrisonHomePath))
            {
                //  prison.Un
                Directory.Delete(prison.Configuration.PrisonHomePath, true);
            }

            Directory.CreateDirectory(prison.Configuration.PrisonHomePath);

            DirectoryInfo deploymentDirInfo = new DirectoryInfo(prison.Configuration.PrisonHomePath);
            DirectorySecurity deploymentDirSecurity = deploymentDirInfo.GetAccessControl();

            // Owner is important to account for disk quota 		
            SetDirectoryOwner(deploymentDirSecurity, prison);

            // Taking ownership of a file has to be executed with0-031233332xpw0odooeoooooooooooooooooooooooooooooooooooooooooooooooooooooooooo restore privilege elevated privilages		
            using (new ProcessPrivileges.PrivilegeEnabler(Process.GetCurrentProcess(), ProcessPrivileges.Privilege.Restore))
            {
                deploymentDirInfo.SetAccessControl(deploymentDirSecurity);
            }
        }

        public override void Destroy(Prison prison)
        {
        }

        public override void Init()
        {
            
        }

        private static string[] openDirs = new string[0];

        public static string[] OpenDirs
        {
            get
            {
                return openDirs;
            }
        }

        public static void TakeOwnership(string user, string directory)
        {
            string command = string.Format(CultureInfo.InvariantCulture, @"takeown /R /D Y /S localhost /U {0} /F ""{1}""", user, directory);

            int ret = Command.ExecuteCommand(command);

            if (ret != 0)
            {
                throw new PrisonException(@"take ownership failed.");
            }
        }

        public static void AddCreateSubdirDenyRule(string user, string directory, bool recursive = false)
        {
            string command = string.Format(CultureInfo.InvariantCulture, @"icacls ""{0}"" /deny {1}:(AD) /c{2}", directory.Replace("\\", "/"), user, recursive ? " /t" : string.Empty);

            int ret = Command.ExecuteCommand(command);

            if (ret != 0)
            {
                throw new PrisonException(@"icacls command denying subdirectory creation failed; command was: {0}", command);
            }
        }

        public static void AddCreateFileDenyRule(string user, string directory, bool recursive = false)
        {
            string command = string.Format(CultureInfo.InvariantCulture, @"icacls ""{0}"" /deny {1}:(W) /c{2}", directory.Replace("\\", "/"), user, recursive ? " /t" : string.Empty);
            int ret = Command.ExecuteCommand(command);

            if (ret != 0)
            {
                throw new PrisonException(@"icacls command denying file creation failed; command was: {0}", command);
            }
        }

        public override RuleInstanceInfo[] List()
        {
            return new RuleInstanceInfo[0];
        }

        public override RuleTypes RuleType
        {
            get
            {
                return RuleTypes.FileSystem;
            }
        }

        public override void Recover(Prison prison)
        {
        }

        private static void SetDirectoryOwner(DirectorySecurity deploymentDirSecurity, Prison prison)
        {
            deploymentDirSecurity.SetOwner(new NTAccount(prison.User.UserName));
            deploymentDirSecurity.SetAccessRule(
                new FileSystemAccessRule(
                    prison.User.UserName, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None, AccessControlType.Allow));
        }
    }
}
