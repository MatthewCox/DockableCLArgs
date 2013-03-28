using System;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MattC.DockableCLArgs
{
    class IDEUtils
    {
        private static DTE2 _dte = null;
        public static DTE2 DTE
        {
            get
            {
                if (_dte == null) _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                return _dte;
            }
        }

        public enum ProjLang
        {
            CS,
            CPP,
            UNKNOWN
        }

        private static ProjLang lang;
        public static ProjLang LANG
        {
            get
            {
                lang = ProjLang.UNKNOWN;

                EnvDTE.Properties props = GetDtePropertiesFromHierarchy();
                string commandArgs = GetProperty(props, "CommandArguments") as string;
                if (commandArgs == null)
                {
                    commandArgs = GetProperty(props, "StartArguments") as string;
                    if (commandArgs != null)
                        lang = ProjLang.CS;
                }
                else
                    lang = ProjLang.CPP;

                return lang;
            }
        }

        private static IVsHierarchy GetStartupProjectHierarchy()
        {
            IVsSolutionBuildManager build = DockableCLArgsPackage.GetGlobalService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
            IVsHierarchy hierarchy;

            if (ErrorHandler.Failed(build.get_StartupProject(out hierarchy)))
                return null;

            return hierarchy;
        }

        private static EnvDTE.Project GetDTEProjectFromHierarchy(IVsHierarchy hierarchy = null)
        {
            if (hierarchy == null)
                hierarchy = GetStartupProjectHierarchy();

            if (hierarchy == null)
                return null;

            object projectobj;
            if (ErrorHandler.Failed(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out projectobj)))
                return null;

            EnvDTE.Project project = projectobj as EnvDTE.Project;
            if (project == null)
                return null;

            return project;
        }

        public static EnvDTE.Properties GetDtePropertiesFromHierarchy(IVsHierarchy hierarchy = null)
        {
            EnvDTE.Project project = GetDTEProjectFromHierarchy(hierarchy);
            if (project == null)
                return null;

            EnvDTE.ConfigurationManager configManager = project.ConfigurationManager;
            if (configManager == null)
                return null;

            EnvDTE.Configuration activeConfig = configManager.ActiveConfiguration;
            if (activeConfig == null)
                return null;

            return activeConfig.Properties;
        }

        public static string GetStartupProjectDirectory()
        {
            EnvDTE.Project project = GetDTEProjectFromHierarchy();
            if (project == null)
                return null;

            return new FileInfo(project.FullName).Directory.FullName;
        }

        public static object GetProperty(EnvDTE.Properties properties, string name)
        {
            if (properties == null || string.IsNullOrEmpty(name))
                return null;

            try
            {
                EnvDTE.Property property = properties.Cast<EnvDTE.Property>().FirstOrDefault(p => p.Name == name);
                if (property == null)
                    return null;

                return property.Value;
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }

        public static bool SetProperty(EnvDTE.Properties properties, string name, object value)
        {
            if (properties == null || string.IsNullOrEmpty(name))
                return false;

            try
            {
                EnvDTE.Property property = properties.Cast<EnvDTE.Property>().FirstOrDefault(p => p.Name == name);
                if (property == null)
                    return false;

                property.Value = value;

                return true;
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        public static string GetCommandArgs()
        {
            EnvDTE.Properties props = IDEUtils.GetDtePropertiesFromHierarchy();
            if (props == null)
                return DockableCLArgs.Resources.StartupMessage;

            string commandArgs = string.Empty;
            switch (lang)
            {
                case ProjLang.CPP:
                    commandArgs = IDEUtils.GetProperty(props, "CommandArguments") as string ?? string.Empty;
                    break;
                case ProjLang.CS:
                    commandArgs = IDEUtils.GetProperty(props, "StartArguments") as string ?? string.Empty;
                    break;
            }

            return commandArgs;
        }

        public static void SetCommandArgs(string value)
        {
            EnvDTE.Properties props = IDEUtils.GetDtePropertiesFromHierarchy();
            if (props == null)
                return;
            switch (lang)
            {
                case ProjLang.CPP:
                    IDEUtils.SetProperty(props, "CommandArguments", value);
                    break;
                case ProjLang.CS:
                    IDEUtils.SetProperty(props, "StartArguments", value);
                    break;
            }
        }
    }
}
