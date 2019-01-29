using System.IO;
using LiteDB;
using ProjectMaster.DataModels;
using ProjectMaster.Utilities;

namespace ProjectMaster.Services
{
    public class DataProvider
    {
        private LiteDatabase Database;

        public DataProvider()
        {
            Database = new LiteDatabase(Path.Combine(GetBasePath(), "Data.db"));
        }

        private static string GetBasePath()
        {
            return ApplicationDataUtility.GetBaseFolder(true);
        }

        public LiteCollection<GitProjectDataModel> GetGitProjectCollection()
        {
            return this.Database.GetCollection<GitProjectDataModel>("GitProjects");
        }
    }
}