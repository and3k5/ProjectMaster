using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using LibGit2Sharp;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectMaster.Models;
using ProjectMaster.Models.Nuget;
using ProjectMaster.Utilities;
using Formatting = Newtonsoft.Json.Formatting;
using Tree = ProjectMaster.Models.Tree;

namespace ProjectMaster.Controllers
{
    public class NugetController : ControllerBase
    {
        public IActionResult ViewAll()
        {
            return View();
        }

        public IActionResult GetNugetProjects()
        {
            var projects = mainService.GetProjectModels();

            var ids = projects.Select(p => p.Id).ToArray();

            return Json(new {projectIds = ids});
        }

        public IActionResult GetNugetInformation(int projectId)
        {
            var project = mainService.GetProjectModel(projectId);

            var information = GetNugetInformation(project, StringComparison.OrdinalIgnoreCase);

            var token = NugetInfoJson(information);

            return Content(token.ToString(Program.Debug ? Formatting.Indented : Formatting.None), "application/json");
        }

        private static JToken NugetInfoJson(NugetInformationModel nugetInformationModel)
        {
            var writer = new JTokenWriter();
            writer.WriteStartObject();

            writer.WritePropertyName("name");
            writer.WriteValue(nugetInformationModel.Project.Name);

            writer.WritePropertyName("id");
            writer.WriteValue(nugetInformationModel.Project.Id);

            writer.WritePropertyName("ignored");
            writer.WriteValue(nugetInformationModel.Project.Ignored);

            writer.WritePropertyName("projects");
            writer.WriteStartArray();

            foreach (var project in nugetInformationModel.ProjectModels)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("assemblyName");
                writer.WriteValue(project.AssemblyName);

                writer.WritePropertyName("csprojFilePath");
                writer.WriteValue(project.CsprojFilePath);

                writer.WritePropertyName("nuspecPath");
                writer.WriteValue(project.NuspecPath);

                writer.WritePropertyName("nugetSpecification");
                if (project.Self == null)
                {
                    writer.WriteValue((object) null);
                }
                else
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("id");
                    writer.WriteValue(project.Self.Id);

                    writer.WritePropertyName("version");
                    writer.WriteValue(project.Self.Version);

                    writer.WriteEndObject();
                }

                writer.WritePropertyName("dependencies");
                writer.WriteStartArray();

                foreach (var dependency in project.Dependencies)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("id");
                    writer.WriteValue(dependency.Id);

                    writer.WritePropertyName("version");
                    writer.WriteValue(dependency.Version);

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();

            return writer.Token;
        }

        private static NugetInformationModel GetNugetInformation(GitProjectModel project, StringComparison oic)
        {
            void IterateTree(Tree[] trees, List<NugetMetaObject> nugetMetaObjects)
            {
                foreach (var tree in trees)
                {
                    if (tree.Type == Tree.TreeType.File)
                    {
                        var culture = StringComparison.OrdinalIgnoreCase;
                        if (tree.Name.Equals("packages.config", culture))
                        {
                            nugetMetaObjects.Add(new NugetMetaObject(tree.Path, NugetMetaObject.NugetMetaObjectType.PackagesConfig));
                            continue;
                        }

                        if (tree.Name.EndsWith(".csproj", culture))
                        {
                            nugetMetaObjects.Add(new NugetMetaObject(tree.Path, NugetMetaObject.NugetMetaObjectType.Csproj));
                            continue;
                        }

                        if (tree.Name.EndsWith(".nuspec", culture))
                        {
                            nugetMetaObjects.Add(new NugetMetaObject(tree.Path, NugetMetaObject.NugetMetaObjectType.Nuspec));
                            continue;
                        }
                    }
                    else if (tree.Type == Tree.TreeType.Folder)
                    {
                        IterateTree(tree.Children, nugetMetaObjects);
                    }
                }
            }

            {
                var projectIdentifierMsg = project.Name + " (id:" + project.Id + ")";

                var information = project.GetInformation();
                var nugetMeta = new List<NugetMetaObject>();
                var folderTree = information.Tree;
                IterateTree(folderTree, nugetMeta);

                var nugetInfo = new NugetInformationModel();
                nugetInfo.Project = project;
                // search for definitions
                var nuspecs = nugetMeta.Where(m => m.Type == NugetMetaObject.NugetMetaObjectType.Nuspec).ToArray();
                //if (nuspecs.Length > 1)
                //    throw new Exception($"{projectIdentifierMsg} has {nuspecs.Length} nuspec files");

                #region Nuspec fetch

                JObject specialConfigJson = null;
                var specialConfiguredNugetProjects = new Dictionary<string, string>();


                var specialConfig = folderTree.SingleOrDefault(f => f.Type == Tree.TreeType.File && f.Name == "config.aprmst");
                if (specialConfig != null)
                {
                    var specialConfigBlob = (Blob) information.Commit[specialConfig.Path].Target;
                    specialConfigJson = JsonConvert.DeserializeObject<JObject>(specialConfigBlob.GetContentText());

                    var nugetProjects = (specialConfigJson["nuget-projects"] as JArray);
                    if (nugetProjects != null)
                    {
                        foreach (var nugetProject in nugetProjects.Values<string>())
                        {
                            var treeEntry = information.Commit.Tree[nugetProject];
                            var csprojBlob = (Blob) treeEntry.Target;
                            var csprojContent = csprojBlob.GetContentText();

                            XmlDocument csprojDoc = new XmlDocument();
                            csprojDoc.LoadXml(csprojContent);

                            string csprojJson = JsonConvert.SerializeXmlNode(csprojDoc);
                            var csprojToken = JsonConvert.DeserializeObject<JToken>(csprojJson);

                            var assemblyName = csprojToken["Project"]["PropertyGroup"].Values<string>("AssemblyName").Single();

                            specialConfiguredNugetProjects.Add(treeEntry.Path, assemblyName);
                        }
                    }
                }

                foreach (var nugetMetaObject in nuspecs)
                {
                    var nugetProjectModel = new NugetProjectModel();

                    var blob = (Blob) information.Commit.Tree[nugetMetaObject.Path].Target;
                    var content = blob.GetContentText();

                    var doc = new XmlDocument();
                    doc.LoadXml(content);

                    string json = JsonConvert.SerializeXmlNode(doc);
                    var token = JsonConvert.DeserializeObject<JToken>(json);

                    var nuspecTokens = new Dictionary<string, string>();
                    var rawPackageId = token["package"]["metadata"].Value<string>("id");

                    if (string.IsNullOrWhiteSpace(rawPackageId))
                        throw new Exception($"{projectIdentifierMsg} is missing id in nuspec: {nugetMetaObject.Path}");

                    var packageId = new NuspecString(rawPackageId, nuspecTokens);

                    string relatedCsprojFile = null;

                    var nuspecNamespace = new XmlNamespaceManager(doc.NameTable);
                    nuspecNamespace.AddNamespace("ns", doc.DocumentElement.GetAttribute("xmlns"));

                    var metaData = doc.SelectNodes("/ns:package/ns:metadata/comment()", nuspecNamespace);

                    const string forcedRelatedCsprojName = "and3k5:relatedCsproj";

                    foreach (XmlComment comment in metaData)
                    {
                        if (comment.Value == null)
                            continue;
                        var matches = Regex.Matches(comment.Value, @"and3k5:relatedCsproj\s*=\s*""(?<csprojPath>[^""]+)""", RegexOptions.Multiline);

                        var path = matches.IterateThrough<Match>().SingleOrDefault()?.Groups["csprojPath"];

                        if (path != null)
                            relatedCsprojFile = path.Value.Replace("\\", "/");
                    }

                    if (relatedCsprojFile == null)
                    {
                        var filesToken = token["package"]["files"]?["file"];

                        if (filesToken == null)
                            throw new Exception($"{projectIdentifierMsg} has no files in nuspec: {nugetMetaObject.Path}");

                        var files = new List<string>();

                        switch (filesToken)
                        {
                            case JObject _:
                                files.Add(new NuspecString(filesToken.Value<string>("@src"), nuspecTokens).ToString());
                                break;
                            case JArray _:
                                foreach (var fileItem in (JArray) filesToken)
                                    files.Add(new NuspecString(fileItem.Value<string>("@src"), nuspecTokens).ToString());

                                break;
                            default:
                                throw new Exception($"{projectIdentifierMsg} has unknown filesToken type: {filesToken.GetType().Name} nuspec: {nugetMetaObject.Path}");
                        }

                        var folderPaths = files.Select(f => f.Split(new char[] {'\\', '/'}, StringSplitOptions.None)).Select(x => x.Take(x.Length - 1).ToArray()).GroupBy(x => string.Join("/", x)).Select(x => x.First());

                        var relatedCsprojFiles = new List<string>();
                        foreach (var folderPath in folderPaths)
                        {
                            for (var i = folderPath.Length - 1; i >= 0; i--)
                            {
                                var path = string.Join("/", folderPath.Take(i + 1).ToArray());
                                var entry = information.Commit[path];
                                if (entry == null) continue;
                                var tree = (LibGit2Sharp.Tree) entry.Target;
                                foreach (var treeEntry in tree)
                                {
                                    if (treeEntry.TargetType != TreeEntryTargetType.Blob)
                                        continue;
                                    if (treeEntry.Name.EndsWith(".csproj", oic))
                                        relatedCsprojFiles.Add(treeEntry.Path);
                                }
                            }
                        }

                        relatedCsprojFiles = relatedCsprojFiles.Distinct().ToList();
                        //if (relatedCsprojFiles.Count == 0)
                        //    throw new Exception($"{projectIdentifierMsg} has no related csproj files for nuspec: {nuspecs[0].Path}");
                        if (relatedCsprojFiles.Count > 1)
                            throw new Exception($"{projectIdentifierMsg} has multiple related csproj files for nuspec: {nugetMetaObject.Path} {Environment.NewLine}Files: " + string.Join(Environment.NewLine, relatedCsprojFiles.OrderByDescending(x => x.Split(new char[] {'\\', '/'}, StringSplitOptions.None).Length)));

                        relatedCsprojFile = relatedCsprojFiles.ElementAtOrDefault(0);
                    }

                    nugetProjectModel.Self = new NugetPackageModel()
                    {
                        Id = packageId.ToString(),
                    };

                    nugetProjectModel.CsprojFilePath = relatedCsprojFile;
                    nugetProjectModel.NuspecPath = nugetMetaObject.Path;
                    if (relatedCsprojFile != null)
                    {
                        var csprojBlob = (Blob) information.Commit.Tree[relatedCsprojFile].Target;
                        var csprojContent = csprojBlob.GetContentText();

                        XmlDocument csprojDoc = new XmlDocument();
                        csprojDoc.LoadXml(csprojContent);

                        string csprojJson = JsonConvert.SerializeXmlNode(csprojDoc);
                        var csprojToken = JsonConvert.DeserializeObject<JToken>(csprojJson);

                        nugetProjectModel.AssemblyName = csprojToken["Project"]["PropertyGroup"].Values<string>("AssemblyName").Single();

                        nugetInfo.ProjectModels.Add(nugetProjectModel);
                    }
                }

                #endregion

                var csprojs = nugetMeta.Where(m => m.Type == NugetMetaObject.NugetMetaObjectType.Csproj).ToArray();
                foreach (var csproj in csprojs)
                {
                    NugetProjectModel nugetProject = nugetInfo.ProjectModels.SingleOrDefault(p => p.CsprojFilePath.Equals(csproj.Path));

                    var csprojBlob = (Blob) information.Commit.Tree[csproj.Path].Target;
                    var csprojContent = csprojBlob.GetContentText();

                    XmlDocument csprojDoc = new XmlDocument();
                    csprojDoc.LoadXml(csprojContent);

                    string csprojJson = JsonConvert.SerializeXmlNode(csprojDoc);
                    var csprojToken = JsonConvert.DeserializeObject<JToken>(csprojJson);

                    if (nugetProject == null)
                    {
                        if (csprojToken["Project"] == null)
                        {
                            continue;
                        }

                        if (IsCoreOrStandard(csprojToken))
                        {
                            continue;
                        }

                        nugetProject = new NugetProjectModel
                        {
                            AssemblyName = csprojToken["Project"]["PropertyGroup"].Values<string>("AssemblyName").Single(),
                            CsprojFilePath = csproj.Path,
                        };

                        if (specialConfiguredNugetProjects.ContainsKey(csproj.Path))
                        {
                            nugetProject.Self = new NugetPackageModel()
                            {
                                Id = nugetProject.AssemblyName,
                            };
                        }

                        nugetInfo.ProjectModels.Add(nugetProject);
                    }

                    var csprojSegments = csproj.Path.Split(new char[] {'\\', '/'}, StringSplitOptions.None);
                    var csprojFolderPath = csprojSegments.Take(csprojSegments.Length - 1);
                    var packagesConfigFile = nugetMeta.Where(p => p.Type == NugetMetaObject.NugetMetaObjectType.PackagesConfig).Select(m => m.Path.Split(new char[] {'\\', '/'}, StringSplitOptions.None)).SingleOrDefault(p => p.Take(p.Length - 1).SequenceEqual(csprojFolderPath));
                    if (packagesConfigFile != null)
                    {
                        var packagesConfigBlob = (Blob) information.Commit.Tree[string.Join("/", packagesConfigFile)].Target;
                        var packagesConfigContent = packagesConfigBlob.GetContentText();

                        XmlDocument packagesConfigDoc = new XmlDocument();
                        packagesConfigDoc.LoadXml(packagesConfigContent);

                        string packagesConfigJson = JsonConvert.SerializeXmlNode(packagesConfigDoc);
                        var packagesConfigToken = JsonConvert.DeserializeObject<JToken>(packagesConfigJson);


                        foreach (var dependencyToken in packagesConfigToken["packages"]["package"].ForceToArray())
                        {
                            nugetProject.Dependencies.Add(new NugetPackageModel()
                            {
                                Id = dependencyToken.Value<string>("@id"),
                                Version = dependencyToken.Value<string>("@version"),
                            });
                        }
                    }
                }

                if (csprojs.Length == 0 && nugetInfo.ProjectModels.Count == 0)
                {
                    // this library has no csproj files. maybe its just a dll file

                    foreach (var nugetMetaObject in nuspecs)
                    {
                        var blob = (Blob) information.Commit.Tree[nugetMetaObject.Path].Target;
                        var content = blob.GetContentText();

                        var doc = new XmlDocument();
                        doc.LoadXml(content);

                        string json = JsonConvert.SerializeXmlNode(doc);
                        var token = JsonConvert.DeserializeObject<JToken>(json);

                        var rawPackageId = token["package"]["metadata"].Value<string>("id");

                        if (string.IsNullOrWhiteSpace(rawPackageId))
                            throw new Exception($"{projectIdentifierMsg} is missing id in nuspec: {nugetMetaObject.Path}");

                        var packageId = new NuspecString(rawPackageId);

                        var nugetProjectModel = new NugetProjectModel();
                        nugetProjectModel.Self = new NugetPackageModel()
                        {
                            Id = packageId.ToString(),
                        };
                        nugetProjectModel.NuspecPath = nugetMetaObject.Path;
                        nugetInfo.ProjectModels.Add(nugetProjectModel);
                    }
                }

                return nugetInfo;
            }
        }

        private static bool IsCoreOrStandard(JToken csprojToken)
        {
            var propertyGroup = (csprojToken["Project"] as JObject)?["PropertyGroup"].ForceToArray();
            var singleStr = propertyGroup?.GetObjectOfArrayChildren("TargetFramework").SingleOrDefault()
                ?.Value<string>();
            if (MatchesCoreOrStandard(singleStr))
                return true;
            var multiString = propertyGroup?.GetObjectOfArrayChildren("TargetFrameworks").SingleOrDefault()
                ?.Value<string>();
            var multiStrings = multiString?.Split(";");

            return multiStrings?.Any(MatchesCoreOrStandard) == true;
        }

        private static bool MatchesCoreOrStandard(string t)
        {
            return t != null && (t.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) ||
                                 t.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase));
        }
    }
}