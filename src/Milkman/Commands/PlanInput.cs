using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Bottles.Deployment.Runtime;
using FubuCore;
using FubuCore.CommandLine;
using System.Linq;

namespace Bottles.Deployment.Commands
{
    public class PlanInput
    {
        [Description("The profile to execute.  'default' is the default.")]
        public string ProfileFlag { get; set; }

        [Description("The optional settings profile that contains extra settings.")]
        [FlagAlias("settings", 's')]
        public string SettingsProfileFlag { get; set; }

        [Description("Path to where the deployment folder is ~/deployment")]
        public string DeploymentFlag { get; set; }

        [Description("Tacks on ONE additional recipie. Great for including tests.")] //until fubu command gets better at parsing command lines
        [FlagAlias("recipe", 'r')]
        public string RecipeFlag { get; set; }

        [Description("File where the installation report should be written.  Default is installation_report.htm")]
        [FlagAlias("report", 'o')]
        public string ReportFlag { get; set; }

        protected virtual void enhanceDeploymentOptions(DeploymentOptions options)
        {

        }

        public DeploymentOptions CreateDeploymentOptions()
        {
            var options = new DeploymentOptions(GetProfile()){
                ReportName = ReportFlag
            };
            enhanceDeploymentOptions(options);

            if(RecipeFlag != null)
            {
                options.RecipeNames.Fill(RecipeFlag);
            }

            if (SettingsProfileFlag != null)
            {
                options.SettingProfileName = SettingsProfileFlag;
            }

            return options;
        }

        public string GetProfile()
        {
            if(ProfileFlag.IsNotEmpty())
            {
                return ProfileFlag;
            }

            var profile = "default";
            if(ProfileFlag.IsEmpty())
            {
                var dir = ".".ToFullPath().AppendPath(GetDeployment(), Milkman.ProfileFiles.ProfilesDirectory);

                if(!Directory.Exists(dir))
                {
                    return profile;
                }

                var files = Directory.GetFiles(dir);
                if(files.Count()==1)
                {
                    profile = files.First();
                    profile = Path.GetFileNameWithoutExtension(profile);
                }
            }

            return profile;
        }

        public string GetDeployment()
        {
            return DeploymentFlag ?? ".".ToFullPath().AppendPath(Milkman.ProfileFiles.DeploymentFolder);
        }
    }
}