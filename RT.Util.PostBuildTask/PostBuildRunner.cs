using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;

namespace RT.Util.PostBuildTask
{
    public class PostBuildRunner : AppDomainIsolatedTask
    {
        public string AssemblyPath { get; set; }
        public string SourcePath { get; set; }

        public override bool Execute()
        {
            Log.LogMessage("Running RT.Util post build checks...");
            var assy = Assembly.LoadFrom(AssemblyPath);
            return 0 == Ut.RunPostBuildChecks(new MsbuildPostBuildReporter(SourcePath, Log), assy);
        }
    }
}
