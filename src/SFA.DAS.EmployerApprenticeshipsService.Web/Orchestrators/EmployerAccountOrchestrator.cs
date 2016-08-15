using System.Threading.Tasks;
using System.Web;
using MediatR;
using Newtonsoft.Json;
using NLog;
using SFA.DAS.EmployerApprenticeshipsService.Application.Commands.CreateEmployerAccount;
using SFA.DAS.EmployerApprenticeshipsService.Web.Models;

namespace SFA.DAS.EmployerApprenticeshipsService.Web.Orchestrators
{
    public class EmployerAccountOrchestrator : EmployerVerificationOrchestratorBase
    {
        private const string CookieName = "sfa-das-employerapprenticeshipsservice-employeraccount";

        //Needed for tests
        protected EmployerAccountOrchestrator()
        {
            
        }

        public EmployerAccountOrchestrator(IMediator mediator, ILogger logger, ICookieService cookieService) : base(mediator,logger,cookieService)
        {
        }

        public async Task CreateAccount(CreateAccountModel model)
        {
            await Mediator.SendAsync(new CreateAccountCommand
            {
                ExternalUserId = model.UserId,
                CompanyNumber = model.CompanyNumber,
                CompanyName = model.CompanyName,
                CompanyRegisteredAddress = model.CompanyRegisteredAddress,
                CompanyDateOfIncorporation = model.CompanyDateOfIncorporation,
                EmployerRef = model.EmployerRef,
                AccessToken = model.AccessToken,
                RefreshToken = model.RefreshToken
            });
        }

        public EmployerAccountData GetCookieData(HttpContextBase context)
        {
            var cookie = (string)CookieService.Get(context, CookieName);
            if (string.IsNullOrEmpty(cookie))
            {
                return new EmployerAccountData();
            }
            return JsonConvert.DeserializeObject<EmployerAccountData>(cookie);
        }

        public void SetCookieData(HttpContextBase context, object data)
        {
            var json = JsonConvert.SerializeObject(data);
            CookieService.Set(context, CookieName, json, 365);
        }
        
    }
}