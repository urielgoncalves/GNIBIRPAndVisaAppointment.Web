using System;
using System.IO;
using System.Linq;
using GNIBIRPAndVisaAppointment.Web.Business;
using GNIBIRPAndVisaAppointment.Web.Business.Information;
using GNIBIRPAndVisaAppointment.Web.Identity;
using GNIBIRPAndVisaAppointment.Web.Models;
using GNIBIRPAndVisaAppointment.Web.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GNIBIRPAndVisaAppointment.Web.Controllers
{
    [Route("Admin")]
    public partial class AdminController : Controller
    {
        IApplicationSettings ApplicationSettings;
        IDomainHub DomainHub;

        public AdminController(IApplicationSettings applicationSettings, IDomainHub domainHub)
        {
            ApplicationSettings = applicationSettings;
            DomainHub = domainHub;

            var adminAllowed = bool.Parse(applicationSettings["AdminAllowed"]);
            if (!adminAllowed)
            {
                throw new InvalidOperationException("AdminAllowed is set false is application settings.");
            }
        }
        
        [Authorize(Roles="Admin,Manager")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("BrowseImages")]
        [Authorize(Roles="Admin")]
        public IActionResult BrowseImages()
        {
            throw new NotImplementedException();
        }

        [Route("Upload")]
        [Authorize(Roles="Admin")]
        public IActionResult Upload(IFormFile upload)
        {
            var informationManager = DomainHub.GetDomain<IInformationManager>();
            var url = informationManager.UploadFile(upload.FileName, upload.ContentType, upload.OpenReadStream());

            return Ok(new
            {
                fileName = Path.GetFileNameWithoutExtension(url),
                uploaded = true,
                url = url
            });
        }

        [Route("Info")]
        [Authorize(Roles="Admin")]
        public IActionResult Infos()
        {
            var informationManager = DomainHub.GetDomain<IInformationManager>();
            var informationList = informationManager.GetList();
            var models = informationList.Select(entity => new InformationModel(entity));

            return View(models);
        }

        [Route("Info/{operation}/{key}/{language}/")]
        [Authorize(Roles="Admin")]
        public IActionResult Info(InformationModel model, string operation)
        {
            var informationManager = DomainHub.GetDomain<IInformationManager>();
            var information = informationManager[model.Key, model.Language];
            model = new InformationModel(information);
            ViewBag.operation = operation;

            return View(model);
        }

        [Route("Info/Add/")]
        [Authorize(Roles="Admin")]
        public IActionResult AddInfo(InformationModel model)
        {
            if (model.Key != null && model.Language != null)
            {
                var informationManager = DomainHub.GetDomain<IInformationManager>();
                informationManager.Add(model.Key, model.Title, model.Author, model.Content);
                return Redirect($"/Admin/Info?_={DateTime.Now.Ticks}");
            }

            return View(model);
        }

        [Route("Info/Update/")]
        [HttpPost]
        [Authorize(Roles="Admin")]
        public IActionResult UpdateInfo(InformationModel model)
        {
            var informationManager = DomainHub.GetDomain<IInformationManager>();
            informationManager.Update(model.Key, model.Language, model.Title, model.Author, model.Content, model.FacebookComment);
            return Redirect($"/Admin/Info?_={DateTime.Now.Ticks}");
        }

        [Route("Info/Delete/")]
        [HttpPost]
        [Authorize(Roles="Admin")]
        public IActionResult DeleteInfo(InformationModel model)
        {
            var informationManager = DomainHub.GetDomain<IInformationManager>();
            informationManager.Delete(model.Key, model.Language);
            return Redirect($"/Admin/Info?_={DateTime.Now.Ticks}");
        }
    }
}