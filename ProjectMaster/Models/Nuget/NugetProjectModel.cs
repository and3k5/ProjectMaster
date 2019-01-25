using System.Collections.Generic;

namespace ProjectMaster.Models.Nuget
{
    public class NugetProjectModel
    {
        public NugetPackageModel Self { get; set; }
        public List<NugetPackageModel> Dependencies { get; set; } = new List<NugetPackageModel>();
        public string AssemblyName { get; set; }
        public string NuspecPath { get; set; }
        public string CsprojFilePath { get; set; }
    }
}