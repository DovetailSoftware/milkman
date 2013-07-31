using System.IO;
using System.Linq;
using Bottles.Deployment;
using Bottles.Deployment.Deployers.Configuration;
using Bottles.Deployment.Writing;
using FubuCore;
using FubuCore.Configuration;
using FubuTestingSupport;
using NUnit.Framework;

namespace Bottles.Tests.Deployment.Writing
{
    [TestFixture]
    public class ProfileConfigWriterRoundTripIntegratedTester
    {
        private static readonly string _testFolderRoot = "profile_config_writer_tests";
        private static readonly string _deploymentFolderPath = _testFolderRoot + "/deployment";
        private static readonly string _deployedFolderProfilePath = _testFolderRoot + "/deployed/Profile";

        private Profile _theProfile;
        private SettingsProvider _settingsProvider;
        private ProfileConfigRoundTripSettings _settings;

        [SetUp]
        public void SetUp()
        {
            writeDeploymentFiles();

            readDotProfileFile();

            writeProfileDotConfigFile();

            readSettingsFromProfileDotConfigFile();

            _settings = _settingsProvider.SettingsFor<ProfileConfigRoundTripSettings>();
        }

        [Test]
        public void main_profile_should_override_dependency_profile_settings()
        {
            _settings.Bar.ShouldEqual("Bar value");
        }

        [Test]
        public void dependency_profile_settings_should_flow_through_to_main_profile()
        {
            _settings.Foo.ShouldEqual("foo depprofile profile-db");
        }

        #region Private Setup Helper Methods

        public class ProfileConfigRoundTripSettings
        {
            public string Foo { get; set; }
            public string Bar { get; set; }
        }

        private void writeDeploymentFiles()
        {
            new FileSystem().CleanDirectory(_deploymentFolderPath);
            var writer = new DeploymentWriter(_deploymentFolderPath);

            writer.RecipeFor("r1");
            writer.RecipeFor("r2");
            writer.RecipeFor("r3");

            writer.ProfileFor("depprofile").AddRecipe("r3");
            writer.ProfileFor("depprofile").AddProperty<ProfileConfigRoundTripSettings>(x => x.Bar, "original bar value");
            writer.ProfileFor("depprofile").AddProperty<ProfileConfigRoundTripSettings>(x => x.Foo, "foo depprofile {dbName}");

            writer.ProfileFor("main").AddRecipe("r1");
            writer.ProfileFor("main").AddRecipe("r2");
            writer.ProfileFor("main").AddProfileDependency("depprofile");
            writer.ProfileFor("main").AddProperty("dbName", "profile-db");
            writer.ProfileFor("main").AddProperty<ProfileConfigRoundTripSettings>(x => x.Bar, "Bar value");

            writer.Flush(FlushOptions.Wipeout);
            
            //var settings = new DeploymentSettings("profile_config_writer_tests/deployment");
            //var reader = new DeploymentGraphReader(settings);
            //var options = new DeploymentOptions("main");
            //var graph = reader.Read(options);
            //thePlan = new DeploymentPlan(options, graph);
        }

        private void readDotProfileFile()
        {
            var settings = new DeploymentSettings(_deploymentFolderPath);
            _theProfile = Profile.ReadFrom(settings, "main");
        }

        private void writeProfileDotConfigFile()
        {
            new FileSystem().CleanDirectory(_deployedFolderProfilePath);
            var writer = new ConfigurationWriter();
            writer.Write(Path.Combine(_deployedFolderProfilePath, "Profile.config"), _theProfile);
        }

        private void readSettingsFromProfileDotConfigFile()
        {
            var settingsData = new FolderAppSettingsXmlSource(_deployedFolderProfilePath).FindSettingData();
            _settingsProvider = SettingsProvider.For(settingsData.ToArray());
        }

        #endregion
    }
}