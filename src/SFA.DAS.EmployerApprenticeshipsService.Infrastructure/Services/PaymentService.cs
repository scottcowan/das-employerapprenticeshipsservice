﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using NLog;
using SFA.DAS.Commitments.Api.Client.Interfaces;
using SFA.DAS.Commitments.Api.Types.Apprenticeship;
using SFA.DAS.EAS.Domain.Interfaces;
using SFA.DAS.EAS.Domain.Models.ApprenticeshipCourse;
using SFA.DAS.EAS.Domain.Models.Payments;
using SFA.DAS.Provider.Events.Api.Client;

namespace SFA.DAS.EAS.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentsEventsApiClient _paymentsEventsApiClient;
        private readonly IEmployerCommitmentApi _commitmentsApiClient;
        private readonly IApprenticeshipInfoServiceWrapper _apprenticeshipInfoService;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public PaymentService(
            IPaymentsEventsApiClient paymentsEventsApiClient,
            IEmployerCommitmentApi commitmentsApiClient,
            IApprenticeshipInfoServiceWrapper apprenticeshipInfoService,
            IMapper mapper,
            ILogger logger)
        {
            _paymentsEventsApiClient = paymentsEventsApiClient;
            _commitmentsApiClient = commitmentsApiClient;
            _apprenticeshipInfoService = apprenticeshipInfoService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ICollection<PaymentDetails>> GetAccountPayments(string periodEnd, long employerAccountId)
        {
            var payments = await GetPayments(periodEnd, employerAccountId);

            if (!payments.Any())
            {
                return new List<PaymentDetails>();
            }

            foreach (var payment in payments)
            {
                payment.PeriodEnd = periodEnd;

                var provider = await GetProvider(Convert.ToInt32(payment.Ukprn));

                if (provider != null)
                {
                    payment.ProviderName = provider.ProviderName;

                    var apprenticeship = await GetApprenticeship(employerAccountId, payment.ApprenticeshipId);

                    if (apprenticeship != null)
                    {
                        payment.ApprenticeName = $"{apprenticeship.FirstName} {apprenticeship.LastName}";
                        payment.ApprenticeNINumber = apprenticeship.NINumber;
                        payment.CourseStartDate = apprenticeship.StartDate;
                    }
                }

                if (payment.StandardCode.HasValue)
                {
                    var standard = await GetStandard(payment.StandardCode.Value);

                    payment.CourseName = standard?.Title;
                    payment.CourseLevel = standard?.Level;
                }
                else if (payment.FrameworkCode.HasValue && payment.ProgrammeType.HasValue && payment.PathwayCode.HasValue)
                {
                    var framework = await GetFramework(
                        payment.FrameworkCode.Value,
                        payment.ProgrammeType.Value,
                        payment.PathwayCode.Value);

                    payment.CourseName = framework?.Title;
                    payment.CourseLevel = framework?.Level;
                }
                else
                {
                    payment.CourseName = string.Empty;
                }
            }

            return payments;
        }

        private async Task<Apprenticeship> GetApprenticeship(long employerAccountId, long apprenticeshipId)
        {
            try
            {
                return await _commitmentsApiClient.GetEmployerApprenticeship(employerAccountId, apprenticeshipId);
            }
            catch (Exception e)
            {
                _logger.Warn(e, $"Unable to get Apprenticeship with Employer Account ID {employerAccountId} and " +
                                 $"apprenticeship ID {apprenticeshipId} from commitments API.");
            }

            return null;
        }

        private async Task<ICollection<PaymentDetails>> GetPayments(string periodEnd, long employerAccountId)
        {
            var paymentDetails = new List<PaymentDetails>();

            try
            {
                var totalPages = 1;

                for (var index = 0; index < totalPages; index++)
                {
                    var payments = await _paymentsEventsApiClient.GetPayments(periodEnd, employerAccountId.ToString(), index + 1);

                    if (payments == null)
                    {
                        return paymentDetails;
                    }
                    
                    paymentDetails.AddRange(payments.Items.Select(x => _mapper.Map<PaymentDetails>(x)));

                    totalPages = payments.TotalNumberOfPages;
                }
            }
            catch (WebException ex)
            {
                _logger.Error(ex, $"Unable to get payment information for {periodEnd} accountid {employerAccountId}");
            }

            return paymentDetails;
        }

        private Task<Domain.Models.ApprenticeshipProvider.Provider> GetProvider(int ukPrn)
        {
            return Task.Run(() =>
            {
                try
                {
                    var providerView = _apprenticeshipInfoService.GetProvider(ukPrn);
                    return providerView?.Provider;
                }
                catch (Exception e)
                {
                    _logger.Warn(e, $"Unable to get provider details with UKPRN {ukPrn} from apprenticeship API.");
                }

                return null;
            });
        }

        private async Task<Standard> GetStandard(long standardCode)
        {
            try
            {
                var standardsView = await _apprenticeshipInfoService.GetStandardsAsync();

                return standardsView.Standards.SingleOrDefault(s => s.Code.Equals(standardCode));
            }
            catch (Exception e)
            {
                _logger.Warn(e, "Could not get standards from apprenticeship API.");
            }

            return null;
        }

        private async Task<Framework> GetFramework(int frameworkCode, int programType, int pathwayCode)
        {
            try
            {
                var frameworksView = await _apprenticeshipInfoService.GetFrameworksAsync();

                return frameworksView.Frameworks.SingleOrDefault(f =>
                                     f.FrameworkCode.Equals(frameworkCode) &&
                                     f.ProgrammeType.Equals(programType) &&
                                     f.PathwayCode.Equals(pathwayCode));
            }
            catch (Exception e)
            {
                _logger.Warn(e, "Could not get frameworks from apprenticeship API.");
            }

            return null;
        }
    }
}
