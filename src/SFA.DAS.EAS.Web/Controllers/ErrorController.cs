﻿using System.Net;
using System.Web.Mvc;
using NLog;
using SFA.DAS.EAS.Web.ViewModels;

namespace SFA.DAS.EAS.Web.Controllers
{
    [RoutePrefix("Error")]
    public class ErrorController : Controller
    {
        private readonly ILogger _logger;

        public ErrorController(ILogger logger)
        {
            _logger = logger;
        }
        
        [Route]
        public ActionResult Index()
        {
            try
            {
                var lastError = Server.GetLastError();

                if (lastError != null)
                {
                    _logger.Error(lastError);
                }
            }
            catch
            {

            }
            return View("Error");
        }

        [Route("NotFound")]
        public ActionResult NotFound()
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return View();
        }

        [Route("InvalidState/{hashedAccountId}")]
        public ActionResult InvalidState(string hashedAccountId)
        {
            var vm = new InvalidStateViewModel
            {
                HashedAccountId = hashedAccountId
            };

            return View(vm);
        }
    }
}