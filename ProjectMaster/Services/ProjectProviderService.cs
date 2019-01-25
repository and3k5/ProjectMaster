using System;
using System.Collections.Generic;
using ProjectMaster.DataModels;

namespace ProjectMaster.Services
{
    public class ProjectProviderService
    {
        private readonly DataProvider dataProvider;

        public ProjectProviderService(DataProvider dataProvider)
        {
            this.dataProvider = dataProvider;
        }

        public IEnumerable<GitProjectDataModel> GetProjects()
        {
            var collection = dataProvider.GetGitProjectCollection();
            return collection.FindAll();
        }

        public GitProjectDataModel GetProject(int projectId)
        {
            return dataProvider.GetGitProjectCollection().FindById(projectId);
        }

        public void SaveProject(GitProjectDataModel dataModel)
        {
            var collection = dataProvider.GetGitProjectCollection();
            collection.Upsert(dataModel);
        }

        public void DeleteProject(int modelId)
        {
            var collection = dataProvider.GetGitProjectCollection();
            collection.Delete(modelId);
        }

        public bool ProjectExists(string projectName, string projectUrl)
        {
            var collection = dataProvider.GetGitProjectCollection();
            return collection.Exists(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase) || p.GitUrl.Equals(projectUrl, StringComparison.OrdinalIgnoreCase));
        }
    }
}