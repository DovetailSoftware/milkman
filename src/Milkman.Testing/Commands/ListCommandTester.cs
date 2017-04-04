using System;
using System.IO;
using Bottles.Deployment.Commands;
using FubuCore;
using NUnit.Framework;

namespace Bottles.Tests.Commands
{
    [TestFixture]
    public class ListCommandTester
    {
        [Test]
        public void Test()
        {
            var dir = Path.GetDirectoryName(typeof(ListCommandTester).Assembly.CodeBase);
            dir = new Uri(dir).LocalPath;
            Directory.SetCurrentDirectory(dir);

            var input = new ListInput()
            {
                PointFlag = @"..{0}..{0}..{0}..".ToFormat(Path.DirectorySeparatorChar)
            };

            var cmd = new ListCommand();

            cmd.Execute(input);

            //REVIEW: how do I test the console out put?
        }
    }
}