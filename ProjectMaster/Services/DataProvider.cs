using System;
using System.IO;
using LiteDB;
using ProjectMaster.DataModels;

namespace ProjectMaster.Services
{
    public class DataProvider
    {
        private LiteDatabase Database;

        public DataProvider()
        {
            this.Database = new LiteDatabase(Path.Combine(GetBasePath(), "And3k5", "ProjectMaster", "Data.db"));
        }

        private static string GetBasePath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        }

        public LiteCollection<GitProjectDataModel> GetGitProjectCollection()
        {
            return this.Database.GetCollection<GitProjectDataModel>("GitProjects");
        }
    }
}