using System.Collections.Generic;

namespace ProjectMaster.Models.Nuget
{
    public class NugetInformationModel
    {
        public List<NugetProjectModel> ProjectModels { get; set; } = new List<NugetProjectModel>();
        public GitProjectModel Project { get; set; }
    }
}