using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using ProjectMaster.DataModels;
using ProjectMaster.Utilities;

namespace ProjectMaster.Models
{
    public class GitProjectModel : GitProjectDataModel
    {
        public GitProjectInformationModel GetInformation()
        {
            var informationModel = new GitProjectInformationModel(this);
            return informationModel;
        }

        public class GitProjectInformationModel
        {
            private readonly GitProjectModel _project;
            private readonly Repository _repository;

            public GitProjectInformationModel(GitProjectModel project)
            {
                this._project = project;
                this._repository = CreateRepo();
                Fetch();
            }

            private Repository CreateRepo()
            {
                var repoUrl = this._project.GitUrl;
                var folderName = this._project.Id + "-" + this._project.Name;
                var invalidChars = Path.GetInvalidFileNameChars();
                folderName = new string(folderName.ToCharArray().Where(x => !invalidChars.Contains(x)).ToArray());

                var gitRootDir = GetGitRootDir();

                var repoDir = new DirectoryInfo(Path.Combine(gitRootDir.FullName, folderName));
                if (!repoDir.Exists)
                {
                    repoDir.Create();
                    Repository.Init(repoDir.FullName);
                }

                if (!Repository.IsValid(repoDir.FullName))
                    Repository.Init(repoDir.FullName);

                var repo = new Repository(repoDir.FullName);

                if (repo.Network.Remotes["origin"] == null)
                    repo.Network.Remotes.Add("origin", repoUrl);

                return repo;
            }

            private static DirectoryInfo GetGitRootDir()
            {
                var baseDir = ApplicationDataUtility.GetBaseFolder(true);
                var gitRootStr = Path.Combine(baseDir, "projects", "git");
                return ApplicationDataUtility.CreatePathIfMissing(gitRootStr);
            }

            private void Fetch()
            {
                var repo = this._repository;
                var remote = repo.Network.Remotes["origin"];
                FetchRemote(repo, remote);

                var headRef = GetHeadReference(repo, remote);
                var commit = (Commit) ((DirectReference) headRef.Target).Target;
                this.Commit = commit;
                var tree = commit.Tree;
                this.Tree = Models.Tree.Generator.Create(tree.GetEnumerator(), (t) => t.Name, (t) => t.Path, (t) => t.TargetType == TreeEntryTargetType.Blob ? Models.Tree.TreeType.File : (t.TargetType == TreeEntryTargetType.Tree ? Models.Tree.TreeType.Folder : 0), t => ((LibGit2Sharp.Tree) t.Target).GetEnumerator());
            }

            public Commit Commit { get; private set; }

            private SymbolicReference GetHeadReference(Repository repo, Remote remote)
            {
                var credentialsProvider = GetCredentialsProvider();
                var references = credentialsProvider == null ? repo.Network.ListReferences(remote) : repo.Network.ListReferences(remote, credentialsProvider);
                return (SymbolicReference) references.Single(x => x.CanonicalName == "HEAD");
            }

            public Tree[] Tree { get; private set; }

            private void FetchRemote(Repository repository, Remote remote)
            {
                var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                var fetchOptions = GetFetchOptions();
                Commands.Fetch(repository, remote.Name, refSpecs, fetchOptions, "");
            }

            private FetchOptions GetFetchOptions()
            {
                var credentialsProvider = GetCredentialsProvider();
                if (credentialsProvider == null)
                    return null;

                var fetchOptions = new FetchOptions()
                {
                    CredentialsProvider = credentialsProvider
                };
                return fetchOptions;
            }

            private CredentialsHandler GetCredentialsProvider()
            {
                if (_project.Authentication == null || string.IsNullOrWhiteSpace(_project.Authentication.Username) && string.IsNullOrWhiteSpace(_project.Authentication.Password))
                    return null;

                return (url, usernameFromUrl, types) =>
                    new UsernamePasswordCredentials()
                    {
                        Username = _project.Authentication.Username,
                        Password = _project.Authentication.Password,
                    };
            }
        }
    }
}