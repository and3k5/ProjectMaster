using Microsoft.AspNetCore.Mvc;
using ProjectMaster.Services;

namespace ProjectMaster.Controllers
{
    public abstract class ControllerBase : Controller
    {
        protected MainService mainService => MainService.StaticInstance;
    }
}