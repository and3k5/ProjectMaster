namespace ProjectMaster.Models.Nuget
{
    public class NugetMetaObject
    {
        public enum NugetMetaObjectType
        {
            PackagesConfig = 1,
            Csproj,
            Nuspec,
        }

        public NugetMetaObject(string path, NugetMetaObjectType type)
        {
            this.Path = path;
            this.Type = type;
        }

        public NugetMetaObjectType Type { get; set; }
        public string Path { get; set; }
    }
}