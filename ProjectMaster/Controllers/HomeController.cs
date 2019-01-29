using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectMaster.DataModels;
using ProjectMaster.Models;
using RestSharp;
using Formatting = Newtonsoft.Json.Formatting;

namespace ProjectMaster.Controllers
{
    public class HomeController : ControllerBase
    {
        public IActionResult Index()
        {
            var models = mainService.GetProjectModels();
            return View(models);
        }

        public IActionResult ViewFiles(int projectId)
        {
            var model = mainService.GetProjectModel(projectId);
            var information = model.GetInformation();
            var fileTree = information.Tree;
            return View(fileTree);
        }

        [HttpGet]
        public IActionResult Config(int projectId = 0)
        {
            GitProjectModel model;
            if (projectId > 0)
            {
                model = mainService.GetProjectModel(projectId);
            }
            else
            {
                model = new GitProjectModel();
            }

            return View(model);
        }

        public IActionResult ImportProjects()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Config(GitProjectModel model)
        {
            mainService.SaveProjectModel(model);
            return RedirectToAction("Config", new {projectId = model.Id});
        }

        [HttpPost]
        public IActionResult Delete(GitProjectModel model)
        {
            if (model.Id > 0)
            {
                mainService.DeleteProject(model);
            }

            return RedirectToAction("Index");
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }

        [HttpPost]
        public IActionResult ImportProjectAjax(string method)
        {
            var result = false;
            var message = "";
            var array = new JTokenWriter();
            array.WriteStartArray();

            try
            {
                switch (method)
                {
                    case "gitlab":
                        var gitlabImportRequest = new GitlabImportRequest(Request);
                        if (string.IsNullOrWhiteSpace(gitlabImportRequest.GroupName))
                            throw new ImportRequestException("Missing group name");
                        if (string.IsNullOrWhiteSpace(gitlabImportRequest.PrivateToken))
                            throw new ImportRequestException("Missing private token");

                        var client = new RestClient("https://gitlab.com/api/v4/");

                        client.DefaultParameters.Add(new Parameter("private_token", gitlabImportRequest.PrivateToken, ParameterType.QueryString));

                        int itemsCount = -1;
                        int page = 1;
                        const int perPage = 100;
                        int itemsAdded = 0;
                        do
                        {
                            var request = new RestRequest("groups/{username}/projects", Method.GET, DataFormat.Json);

                            request.Parameters.Add(new Parameter("username", gitlabImportRequest.GroupName, ParameterType.UrlSegment));
                            request.Parameters.Add(new Parameter("per_page", perPage, ParameterType.QueryString));
                            request.Parameters.Add(new Parameter("page", page++, ParameterType.QueryString));

                            var response = client.Execute(request);

                            var json = response.Content;

                            var projects = (JToken) JsonConvert.DeserializeObject(json);

                            if (!(projects is JArray))
                                throw new ImportRequestException("Unexpected response from server");

                            var projectsArray = (JArray) projects;

                            foreach (var project in projectsArray)
                            {
                                array.WriteStartObject();

                                array.WritePropertyName("name");
                                var name = project.Value<string>("name");
                                array.WriteValue(name);

                                array.WritePropertyName("url");
                                var url = project.Value<string>("http_url_to_repo");
                                array.WriteValue(url);

                                array.WritePropertyName("added");
                                array.WriteValue(mainService.ProjectExists(name, url));

                                array.WriteEndObject();

                                itemsAdded++;
                            }
                        } while (itemsCount == perPage);

                        message = $"{itemsAdded} projects fetched";
                        result = true;
                        break;

                    default:
                        throw new ImportRequestException("Unknown provider: " + method);
                }
            }
            catch (ImportRequestException ex)
            {
                result = false;
                message = ex.Message;
            }
            catch (Exception ex)
            {
                result = false;
                message = "Unexpected exception: " + ex.Message;
            }

            var writer = new JTokenWriter();

            array.WriteEndArray();

            writer.WriteStartObject();

            writer.WritePropertyName("status");
            writer.WriteValue(result ? "OK" : "ERROR");

            writer.WritePropertyName("message");
            writer.WriteValue(message);

            writer.WritePropertyName("projects");
            writer.WriteToken(new JTokenReader(array.Token));

            writer.WriteEndObject();

            var content = writer.Token.ToString(Program.Debug ? Formatting.Indented : Formatting.None);
            return Content(content, "text/javascript");
        }

        private class ImportRequestException : Exception
        {
            public ImportRequestException(string message) : base(message)
            {
            }

            public ImportRequestException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }

        private class GitlabImportRequest
        {
            public GitlabImportRequest(HttpRequest request)
            {
                var json = new System.IO.StreamReader(request.Body).ReadToEnd();
                var obj = JsonConvert.DeserializeObject<JObject>(json);
                this.GroupName = obj.Value<string>("GroupName");
                this.PrivateToken = obj.Value<string>("PrivateToken");
            }

            public string GroupName { get; set; }
            public string PrivateToken { get; set; }
        }

        [HttpPost]
        public IActionResult AddNewProject()
        {
            try
            {
                var json = new System.IO.StreamReader(Request.Body).ReadToEnd();
                var obj = JsonConvert.DeserializeObject<JObject>(json);
                var projectName = obj.Value<string>("name");
                var projectUrl = obj.Value<string>("url");

                var authObj = (JObject) obj["auth"];
                var projectUsername = authObj.Value<string>("username");
                var projectPassword = authObj.Value<string>("password");

                var model = new GitProjectModel()
                {
                    Name = projectName,
                    GitUrl = projectUrl,
                    Authentication = new GitProjectAuthentication()
                    {
                        Username = projectUsername,
                        Password = projectPassword,
                    },
                };

                mainService.SaveProjectModel(model);

                return Json(new {status = "OK", message = "", projectId = model.Id});
            }
            catch (Exception e)
            {
                return Json(new {status = "ERROR", message = e.Message});
            }
        }

        [HttpPost]
        public IActionResult FetchProject()
        {
            try
            {
                var json = new System.IO.StreamReader(Request.Body).ReadToEnd();
                var obj = JsonConvert.DeserializeObject<JObject>(json);
                var projectId = obj.Value<int>("projectId");

                var project = mainService.GetProjectModel(projectId);

                var information = project.GetInformation();

                return Json(new {status = "OK", message = ""});
            }
            catch (Exception e)
            {
                return Json(new {status = "ERROR", message = e.Message});
            }
        }

        public IActionResult NugetOverview() => RedirectToAction("ViewAll", "Nuget");
    }
}