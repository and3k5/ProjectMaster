using System.Linq;
using ProjectMaster.DataModels;
using ProjectMaster.Models;
using ProjectMaster.Utilities;

namespace ProjectMaster.Services
{
    public class MainService
    {
        public static MainService StaticInstance = new MainService();
        private readonly DataProvider dataProvider;
        private readonly ProjectProviderService projectProviderService;

        public MainService()
        {
            this.dataProvider = new DataProvider();
            this.projectProviderService = new ProjectProviderService(this.dataProvider);
        }

        public GitProjectModel[] GetProjectModels()
        {
            var projects = projectProviderService.GetProjects().ToArray();

            return projects
                .Select(project => project.ConvertToModel<GitProjectDataModel, GitProjectModel>())
                .ToArray();
        }

        public GitProjectModel GetProjectModel(int projectId)
        {
            return projectProviderService.GetProject(projectId).ConvertToModel<GitProjectDataModel, GitProjectModel>();
        }

        public void SaveProjectModel(GitProjectModel model)
        {
            var dataModel = model.ConvertToDataModel<GitProjectModel, GitProjectDataModel>();
            projectProviderService.SaveProject(dataModel);
            ObjectUtility.CopyCommonValues(dataModel, model);
        }

        public void DeleteProject(GitProjectModel model)
        {
            if (model.Id > 0)
            {
                projectProviderService.DeleteProject(model.Id);
            }
        }

        public bool ProjectExists(string projectName, string projectUrl)
        {
            return projectProviderService.ProjectExists(projectName, projectUrl);
        }
    }
}