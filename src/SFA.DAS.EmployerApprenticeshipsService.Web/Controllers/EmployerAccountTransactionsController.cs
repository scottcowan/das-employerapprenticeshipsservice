﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SFA.DAS.EmployerApprenticeshipsService.Web.Authentication;
using SFA.DAS.EmployerApprenticeshipsService.Web.Orchestrators;

namespace SFA.DAS.EmployerApprenticeshipsService.Web.Controllers
{
    public class EmployerAccountTransactionsController : Controller
    {
        private readonly IOwinWrapper _owinWrapper;
        private readonly EmployerAccountTransactionsOrchestrator _accountTransactionsOrchestrator;

        public EmployerAccountTransactionsController(IOwinWrapper owinWrapper, EmployerAccountTransactionsOrchestrator accountTransactionsOrchestrator)
        {
            _owinWrapper = owinWrapper;
            _accountTransactionsOrchestrator = accountTransactionsOrchestrator;
        }

        // GET: EmployerAccountTransactions
        [Authorize]
        public async Task<ActionResult> Index(int accountId)
        {
            
            var transactionViewResult  = await _accountTransactionsOrchestrator.GetAccountTransactions(accountId);

            if (transactionViewResult.Account == null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(transactionViewResult.Model);
        }

        public async Task<ActionResult> Detail(int accountId, string itemId)
        {
            var transactionViewResult = await _accountTransactionsOrchestrator.GetAccounTransactionLineItem(accountId, itemId);

            if (transactionViewResult.Account == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (transactionViewResult.Model.LineItem == null)
            {
                return RedirectToAction("Index", "EmployerAccountTransactions", new {accountId = accountId});
            }
           
            return View(transactionViewResult.Model);
        }
    }
}