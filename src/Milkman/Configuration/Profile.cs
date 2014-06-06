using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Bottles.Deployment.Configuration;
using FubuCore;
using FubuCore.Configuration;
using System.Linq;

namespace Bottles.Deployment
{
    [DebuggerDisplay("Profile:{Name}")]
    public class Profile : ProfileBase
    {
        public static readonly string RecipePrefix = "recipe:";
        public static string ProfileDependencyPrefix = "dependency:";
        public static string Comment = "#";

        private readonly IList<string> _recipes = new List<string>();
        private readonly IList<string> _childProfileNames = new List<string>();
        private readonly IList<Profile> _childProfiles = new List<Profile>();

        public Profile(string profileName) : base(SettingCategory.profile, "Profile:  " + profileName)
        {
            Name = profileName;
        }

        public string Name { get; private set; }
        
        public IEnumerable<string> Recipes
        {
            get { return _recipes; }
        }

        public IEnumerable<string> AllRecipesFlattened
        {
            get { return _recipes.Union(_childProfiles.SelectMany(p => p.AllRecipesFlattened)); }
        }

        public IEnumerable<string> ProfileDependencyNames
        {
            get { return _childProfileNames; }
        }

        public IEnumerable<Profile> ProfileDependencies
        {
            get { return _childProfiles; }
        }

        public static Profile ReadFrom(DeploymentSettings settings, string profileName, string settingsProfileName = null)
        {
            var profile = new Profile(profileName);
            var profileFile = settings.ProfileFileNameFor(profileName);

            var fileSystem = new FileSystem();
            if (!fileSystem.FileExists(profileFile))
            {
                var sb = new StringBuilder();
                sb.AppendFormat("Couldn't find the profile '{0}'", profileFile);
                sb.AppendLine();
                sb.AppendLine("Looked in:");
                settings.Directories.Each(d => sb.AppendLine("  {0}".ToFormat(d)));

                throw new Exception(sb.ToString());
            }

            // Settings profile goes first
            if (settingsProfileName != null)
            {
                profile.AddProfileDependency(settingsProfileName);
            }

            fileSystem.ReadTextFile(profileFile, profile.ReadText);

            profile._childProfileNames.Each(childName =>
                {
                    var childProfile = ReadFrom(settings, childName);
                    profile._childProfiles.Add(childProfile);
                    childProfile.Data.AllKeys.Each(childKey =>
                        {
                            // do not override main profile settings from dependencies. 
                            // NOTE: Has(childKey) doesn't work here because SettingsData has a weird inner dictionary with a special key structure
                            if (profile.Data.AllKeys.Any(k => k == childKey)) return; 

                            profile.Data[childKey] = childProfile.Data[childKey];
                        });
                });

            return profile;
        }

        public void ReadText(string text)
        {
            if (text.IsEmpty()) return;

            if(text.StartsWith(Comment))
            {
                return;
            }

            if (text.StartsWith(RecipePrefix))
            {
                var recipeName = text.Substring(RecipePrefix.Length).Trim();
                AddRecipe(recipeName);
            }
            else if(text.StartsWith(ProfileDependencyPrefix))
            {
                var profileName = text.Substring(ProfileDependencyPrefix.Length).Trim();
                AddProfileDependency(profileName);
            }
            else
            {
                Data.Read(text);
            }
        }
        
        private void AddProfileDependency(string profileName)
        {
            _childProfileNames.Add(profileName);
        }

        public void AddRecipe(string recipe)
        {
            _recipes.Fill(recipe);
        }
    }
}