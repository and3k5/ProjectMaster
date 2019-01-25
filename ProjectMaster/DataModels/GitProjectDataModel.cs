namespace ProjectMaster.DataModels
{
    public class GitProjectDataModel : IDataModel
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string GitUrl { get; set; }

        public bool Ignored { get; set; }
        public GitProjectAuthentication Authentication { get; set; }
    }
}