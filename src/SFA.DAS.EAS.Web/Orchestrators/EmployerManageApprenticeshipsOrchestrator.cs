﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using MediatR;
using NLog;
using FluentValidation;

using SFA.DAS.EAS.Application.Queries.GetAllApprenticeships;
using SFA.DAS.EAS.Application.Queries.GetApprenticeship;
using SFA.DAS.EAS.Domain.Interfaces;
using SFA.DAS.EAS.Web.Orchestrators.Mappers;
using SFA.DAS.EAS.Web.ViewModels.ManageApprenticeships;
using SFA.DAS.EAS.Web.ViewModels;
using SFA.DAS.EAS.Domain.Models.ApprenticeshipCourse;

using SFA.DAS.Commitments.Api.Types.Apprenticeship;
using SFA.DAS.Commitments.Api.Types.Apprenticeship.Types;
using SFA.DAS.Commitments.Api.Types.DataLock.Types;
using SFA.DAS.EAS.Application.Commands.CreateApprenticeshipUpdate;
using SFA.DAS.EAS.Application.Commands.ReviewApprenticeshipUpdate;
using SFA.DAS.EAS.Application.Commands.UndoApprenticeshipUpdate;
using SFA.DAS.EAS.Application.Queries.GetApprenticeshipUpdate;
using SFA.DAS.EAS.Application.Queries.GetOverlappingApprenticeships;
using SFA.DAS.EAS.Application.Queries.GetTrainingProgrammes;
using SFA.DAS.EAS.Web.Exceptions;
using SFA.DAS.EAS.Web.Validators;
using SFA.DAS.EAS.Application.Queries.ValidateStatusChangeDate;
using SFA.DAS.EAS.Application.Commands.UpdateApprenticeshipStatus;
using SFA.DAS.EAS.Application.Queries.GetApprenticeshipDataLock;

namespace SFA.DAS.EAS.Web.Orchestrators
{
    public sealed class EmployerManageApprenticeshipsOrchestrator : CommitmentsBaseOrchestrator
    {
        private readonly IMediator _mediator;
        private readonly IHashingService _hashingService;
        private readonly IApprenticeshipMapper _apprenticeshipMapper;
        private readonly ILogger _logger;
        private readonly ICurrentDateTime _currentDateTime;

        private readonly ApprovedApprenticeshipViewModelValidator _apprenticeshipValidator;

        private readonly ICookieStorageService<UpdateApprenticeshipViewModel>
            _apprenticshipsViewModelCookieStorageService;

        private const string CookieName = "sfa-das-employerapprenticeshipsservice-apprentices";

        public EmployerManageApprenticeshipsOrchestrator(
            IMediator mediator, 
            IHashingService hashingService,
            IApprenticeshipMapper apprenticeshipMapper,
            ApprovedApprenticeshipViewModelValidator apprenticeshipValidator,
            ICurrentDateTime currentDateTime,
            ILogger logger,
            ICookieStorageService<UpdateApprenticeshipViewModel> apprenticshipsViewModelCookieStorageService) : base(mediator, hashingService, logger)
        {
            if (mediator == null)
                throw new ArgumentNullException(nameof(mediator));
            if (hashingService == null)
                throw new ArgumentNullException(nameof(hashingService));
            if (apprenticeshipMapper == null)
                throw new ArgumentNullException(nameof(apprenticeshipMapper));
            if (currentDateTime == null)
                throw new ArgumentNullException(nameof(currentDateTime));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (apprenticeshipValidator == null)
                throw new ArgumentNullException(nameof(apprenticeshipValidator));
            
            _mediator = mediator;
            _hashingService = hashingService;
            _apprenticeshipMapper = apprenticeshipMapper;
            _currentDateTime = currentDateTime;
            _logger = logger;
            _apprenticeshipValidator = apprenticeshipValidator;
            _apprenticshipsViewModelCookieStorageService = apprenticshipsViewModelCookieStorageService;
        }

        public async Task<OrchestratorResponse<ManageApprenticeshipsViewModel>> GetApprenticeships(
            string hashedAccountId, string externalUserId)
        {
            var accountId = _hashingService.DecodeValue(hashedAccountId);
            _logger.Info($"Getting On-programme apprenticeships for empployer: {accountId}");

            return await CheckUserAuthorization(async () =>
            {
                var data = await _mediator.SendAsync(new GetAllApprenticeshipsRequest {AccountId = accountId});

                var apprenticeships =
                    data.Apprenticeships
                        .OrderBy(m => m.ApprenticeshipName)
                        .Select(
                            m =>
                                _apprenticeshipMapper.MapToApprenticeshipDetailsViewModel(m))
                        .ToList();

                var model = new ManageApprenticeshipsViewModel
                {
                    HashedAccountId = hashedAccountId,
                    Apprenticeships = apprenticeships
                };

                return new OrchestratorResponse<ManageApprenticeshipsViewModel>
                {
                    Data = model
                };

            }, hashedAccountId, externalUserId);
        }

        public async Task<OrchestratorResponse<ApprenticeshipDetailsViewModel>> GetApprenticeship(string hashedAccountId, string hashedApprenticeshipId, string externalUserId)
        {
            var accountId = _hashingService.DecodeValue(hashedAccountId);
            var apprenticeshipId = _hashingService.DecodeValue(hashedApprenticeshipId);

            _logger.Info(
                $"Getting On-programme apprenticeships Provider: {accountId}, ApprenticeshipId: {apprenticeshipId}");

            return await CheckUserAuthorization(async () =>
            {
                var data = await _mediator.SendAsync(
                    new GetApprenticeshipQueryRequest {AccountId = accountId, ApprenticeshipId = apprenticeshipId});

                var detailsViewModel =
                    _apprenticeshipMapper.MapToApprenticeshipDetailsViewModel(data.Apprenticeship);

                return new OrchestratorResponse<ApprenticeshipDetailsViewModel> {Data = detailsViewModel};
            }, hashedAccountId, externalUserId);
        }

        public async Task<OrchestratorResponse<ExtendedApprenticeshipViewModel>> GetApprenticeshipForEdit(
            string hashedAccountId, string hashedApprenticeshipId, string externalUserId)
        {
            var accountId = _hashingService.DecodeValue(hashedAccountId);
            var apprenticeshipId = _hashingService.DecodeValue(hashedApprenticeshipId);

            _logger.Info(
                $"Getting Approved Apprenticeship for Editing, Account: {accountId}, ApprenticeshipId: {apprenticeshipId}");

            return await CheckUserAuthorization(async () =>
            {
                await AssertApprenticeshipStatus(accountId, apprenticeshipId);
                // TODO: LWA Assert that the apprenticeship can be edited - Story says should be allowed to go to edit details page??

                var data = await _mediator.SendAsync(new GetApprenticeshipQueryRequest
                {
                    AccountId = accountId,
                    ApprenticeshipId = apprenticeshipId
                });

                AssertApprenticeshipIsEditable(data.Apprenticeship);
                var apprenticeship = _apprenticeshipMapper.MapToApprenticeshipViewModel(data.Apprenticeship);

                apprenticeship.HashedAccountId = hashedAccountId;

                return new OrchestratorResponse<ExtendedApprenticeshipViewModel>
                {
                    Data = new ExtendedApprenticeshipViewModel
                    {
                        Apprenticeship = apprenticeship,
                        ApprenticeshipProgrammes = await GetTrainingProgrammes(),
                    }
                };
            }, hashedAccountId, externalUserId);
        }

        public async Task<OrchestratorResponse<UpdateApprenticeshipViewModel>> GetConfirmChangesModel(
            string hashedAccountId, string hashedApprenticeshipId, string externalUserId,
            ApprenticeshipViewModel apprenticeship)
        {
            var accountId = _hashingService.DecodeValue(hashedAccountId);
            var apprenticeshipId = _hashingService.DecodeValue(hashedApprenticeshipId);

            _logger.Debug($"Getting confirm change model: {accountId}, ApprenticeshipId: {apprenticeshipId}");

            return await CheckUserAuthorization(async () =>
            {
                await AssertApprenticeshipStatus(accountId, apprenticeshipId);

                var data = await _mediator.SendAsync(new GetApprenticeshipQueryRequest
                {
                    AccountId = accountId,
                    ApprenticeshipId = apprenticeshipId
                });

                var apprenticeships = _apprenticeshipMapper.CompareAndMapToApprenticeshipViewModel(data.Apprenticeship,
                    apprenticeship);

                return new OrchestratorResponse<UpdateApprenticeshipViewModel>
                {
                    Data = await apprenticeships
                };
            }, hashedAccountId, externalUserId);
        }

        public async Task<OrchestratorResponse<UpdateApprenticeshipViewModel>> GetViewChangesViewModel(
            string hashedAccountId, string hashedApprenticeshipId, string externalUserId)
        {
            var accountId = _hashingService.DecodeValue(hashedAccountId);
            var apprenticeshipId = _hashingService.DecodeValue(hashedApprenticeshipId);

            _logger.Debug($"Getting confirm change model: {accountId}, ApprenticeshipId: {apprenticeshipId}");

            return await CheckUserAuthorization(
                async () =>
                {
                    var data = await _mediator.SendAsync(
                        new GetApprenticeshipUpdateRequest
                        {
                            AccountId = accountId,
                            ApprenticeshipId = apprenticeshipId
                        });

                    var apprenticeshipResult = await _mediator.SendAsync(
                        new GetApprenticeshipQueryRequest
                        {
                            AccountId = accountId,
                            ApprenticeshipId = apprenticeshipId
                        });

                    var viewModel = _apprenticeshipMapper.MapFrom(data.ApprenticeshipUpdate);

                    var apprenticeship = _apprenticeshipMapper.MapToApprenticeshipDetailsViewModel(apprenticeshipResult.Apprenticeship);
                    viewModel.OriginalApprenticeship = apprenticeship;
                    viewModel.HashedAccountId = hashedAccountId;
                    viewModel.HashedApprenticeshipId = hashedApprenticeshipId;
                    viewModel.ProviderName = apprenticeship.ProviderName;
                    viewModel.IsDataLockOrigin = 
                        apprenticeship.DataLockTriageStatus == TriageStatus.Change;

                    return new OrchestratorResponse<UpdateApprenticeshipViewModel>
                    {
                        Data = viewModel
                    };
                }, hashedAccountId, externalUserId);
        }

        public async Task SubmitUndoApprenticeshipUpdate(string hashedAccountId, string hashedApprenticeshipId, string userId, string userName, string userEmail)
        {
            var accountId = _hashingService.DecodeValue(hashedAccountId);
            var apprenticeshipId = _hashingService.DecodeValue(hashedApprenticeshipId);

            _logger.Debug($"Undoing pending update for : AccountId {accountId}, ApprenticeshipId: {apprenticeshipId}");

            await CheckUserAuthorization(async () =>
                {
                    await _mediator.SendAsync(new UndoApprenticeshipUpdateCommand
                    {
                        AccountId = accountId,
                        ApprenticeshipId = apprenticeshipId,
                        UserId = userId,
                        UserDisplayName = userName,
                        UserEmailAddress = userEmail
                    });
                }
                , hashedAccountId, userId);
        }

        public async Task<Dictionary<string, string>> ValidateApprenticeship(ApprenticeshipViewModel apprenticeship)
        {
            var overlappingErrors = await _mediator.SendAsync(
                new GetOverlappingApprenticeshipsQueryRequest
                {
                    Apprenticeship = new List<Apprenticeship> {await _apprenticeshipMapper.MapFrom(apprenticeship)}
                });

            var result = _apprenticeshipMapper
                .MapOverlappingErrors(overlappingErrors)
                .ToDictionary(overlap => overlap.Key, overlap => overlap.Value);

            foreach (var error in _apprenticeshipValidator.ValidateToDictionary(apprenticeship))
            {
                result.Add(error.Key, error.Value);
            }

            return result;
        }

        public async Task<OrchestratorResponse<ChangeStatusChoiceViewModel>> GetChangeStatusChoiceNavigation(string hashedAccountId, string hashedApprenticeshipId, string externalUserId)
           
        {
            var accountId = _hashingService.DecodeValue(hashedAccountId);
            var apprenticeshipId = _hashingService.DecodeValue(hashedApprenticeshipId);

            _logger.Info(
                $"Determining navigation for type of change status selection. AccountId: {accountId}, ApprenticeshipId: {apprenticeshipId}");

            return await CheckUserAuthorization(async () =>
            {
                var data =
                    await
                        _mediator.SendAsync(new GetApprenticeshipQueryRequest
                        {
                            AccountId = accountId,
                            ApprenticeshipId = apprenticeshipId
                        });

                CheckApprenticeshipStateValidForChange(data.Apprenticeship);

                var isPaused = data.Apprenticeship.PaymentStatus == PaymentStatus.Paused;

                return new OrchestratorResponse<ChangeStatusChoiceViewModel> { Data = new ChangeStatusChoiceViewModel { IsCurrentlyPaused = isPaused } };

            }, hashedAccountId, externalUserId);
        }

        public async Task<OrchestratorResponse<WhenToMakeChangeViewModel>> GetChangeStatusDateOfChangeViewModel(
            string hashedAccountId, string hashedApprenticeshipId,
            ChangeStatusType changeType, string externalUserId)
        {
            var accountId = _hashingService.DecodeValue(hashedAccountId);
            var apprenticeshipId = _hashingService.DecodeValue(hashedApprenticeshipId);

            _logger.Info(
                $"Determining navigation for type of change status selection. AccountId: {accountId}, ApprenticeshipId: {apprenticeshipId}");

            return await CheckUserAuthorization(async () =>
            {
                var data =
                    await
                        _mediator.SendAsync(new GetApprenticeshipQueryRequest
                        {
                            AccountId = accountId,
                            ApprenticeshipId = apprenticeshipId
                        });

                CheckApprenticeshipStateValidForChange(data.Apprenticeship);

                return new OrchestratorResponse<WhenToMakeChangeViewModel>
                {
                    Data = new WhenToMakeChangeViewModel
                    {
                        StartDate = data.Apprenticeship.StartDate.Value,
                        SkipStep = CanChangeDateStepBeSkipped(changeType, data),
                        ChangeStatusViewModel = new ChangeStatusViewModel
                        {
                            ChangeType = changeType
                        }
                    }
                };

            }, hashedAccountId, externalUserId);
        }

        private bool CanChangeDateStepBeSkipped(ChangeStatusType changeType, GetApprenticeshipQueryResponse data)
        {
            return data.Apprenticeship.IsWaitingToStart(_currentDateTime) // Not started
                || (data.Apprenticeship.PaymentStatus == PaymentStatus.Paused && changeType == ChangeStatusType.Resume) // Resuming 
                || (data.Apprenticeship.PaymentStatus == PaymentStatus.Active && changeType == ChangeStatusType.Pause); // Pausing
        }

        public async Task<ValidateWhenToApplyChangeResult> ValidateWhenToApplyChange(string hashedAccountId,
            string hashedApprenticeshipId, ChangeStatusViewModel model)
        {
            var accountId = _hashingService.DecodeValue(hashedAccountId);
            var apprenticeshipId = _hashingService.DecodeValue(hashedApprenticeshipId);

            _logger.Info(
                $"Validating Date for when to apply change. AccountId: {accountId}, ApprenticeshipId: {apprenticeshipId}, ChangeType: {model.ChangeType}, ChangeDate: {model.DateOfChange.DateTime}");

            var response = await _mediator.SendAsync(new ValidateStatusChangeDateQuery
            {
                AccountId = accountId,
                ApprenticeshipId = apprenticeshipId,
                ChangeOption = (Domain.Models.Apprenticeship.ChangeOption) model.WhenToMakeChange,
                DateOfChange = model.DateOfChange.DateTime
            });

            return new ValidateWhenToApplyChangeResult
            {
                ValidationResult = response.ValidationResult,
                DateOfChange = response.ValidatedChangeOfDate
            };
        }

        public async Task<OrchestratorResponse<ConfirmationStateChangeViewModel>> GetChangeStatusConfirmationViewModel(string hashedAccountId, string hashedApprenticeshipId, ChangeStatusType changeType, WhenToMakeChangeOptions whenToMakeChange, DateTime? dateOfChange, string externalUserId)
        {
            var accountId = _hashingService.DecodeValue(hashedAccountId);
            var apprenticeshipId = _hashingService.DecodeValue(hashedApprenticeshipId);

            _logger.Info(
                $"Getting Change Status Confirmation ViewModel. AccountId: {accountId}, ApprenticeshipId: {apprenticeshipId}, ChangeType: {changeType}");

            return await CheckUserAuthorization(async () =>
            {
                var data =
                    await
                        _mediator.SendAsync(new GetApprenticeshipQueryRequest
                        {
                            AccountId = accountId,
                            ApprenticeshipId = apprenticeshipId
                        });

                CheckApprenticeshipStateValidForChange(data.Apprenticeship);

                return new OrchestratorResponse<ConfirmationStateChangeViewModel>
                {
                    Data = new ConfirmationStateChangeViewModel
                    {
                        ApprenticeName = data.Apprenticeship.ApprenticeshipName,
                        DateOfBirth = data.Apprenticeship.DateOfBirth.Value,
                        ChangeStatusViewModel = new ChangeStatusViewModel
                        {
                            DateOfChange = DetermineChangeDate(changeType, data.Apprenticeship, whenToMakeChange, dateOfChange),
                            ChangeType = changeType,
                            WhenToMakeChange = whenToMakeChange,
                            ChangeConfirmed = false
                        }
                    }
                };

            }, hashedAccountId, externalUserId);
        }

        public async Task UpdateStatus(string hashedAccountId, string hashedApprenticeshipId, ChangeStatusViewModel model, string externalUserId, string userName, string userEmail)
        {
            var accountId = _hashingService.DecodeValue(hashedAccountId);
            var apprenticeshipId = _hashingService.DecodeValue(hashedApprenticeshipId);

            _logger.Info(
                $"Updating Apprenticeship status to {model.ChangeType}. AccountId: {accountId}, ApprenticeshipId: {apprenticeshipId}");

            await CheckUserAuthorization(async () =>
            {
                var data =
                    await
                        _mediator.SendAsync(new GetApprenticeshipQueryRequest
                        {
                            AccountId = accountId,
                            ApprenticeshipId = apprenticeshipId
                        });

                CheckApprenticeshipStateValidForChange(data.Apprenticeship);

                await _mediator.SendAsync(new UpdateApprenticeshipStatusCommand
                {
                    UserId = externalUserId,
                    ApprenticeshipId = apprenticeshipId,
                    EmployerAccountId = accountId,
                    ChangeType = (Domain.Models.Apprenticeship.ChangeStatusType) model.ChangeType,
                    DateOfChange = model.DateOfChange.DateTime.Value,
                    UserEmailAddress = userEmail,
                    UserDisplayName = userName
                });

            }, hashedAccountId, externalUserId);
        }

        private void CheckApprenticeshipStateValidForChange(Apprenticeship apprentice)
        {
            if (!IsActiveOrPaused(apprentice))
            {
                throw new InvalidStateException(
                    $"Apprenticeship not is correct state for change: Current:{apprentice.PaymentStatus}");
            }
        }

        private bool IsActiveOrPaused(Apprenticeship apprenticeship)
        {
            return apprenticeship.PaymentStatus != PaymentStatus.Withdrawn ||
                   apprenticeship.PaymentStatus != PaymentStatus.Completed;

        }

        private DateTimeViewModel DetermineChangeDate(ChangeStatusType changeType, Apprenticeship apprenticeship, WhenToMakeChangeOptions whenToMakeChange, DateTime? dateOfChange)
        {
            if (changeType == ChangeStatusType.Pause || changeType == ChangeStatusType.Resume)
            {
                return new DateTimeViewModel(_currentDateTime.Now.Date);
            }

            if (apprenticeship.IsWaitingToStart(_currentDateTime))
            {
                return new DateTimeViewModel(apprenticeship.StartDate);
            }

            if (whenToMakeChange == WhenToMakeChangeOptions.Immediately)
            {
                return new DateTimeViewModel(_currentDateTime.Now.Date);
            }

            return new DateTimeViewModel(dateOfChange);
        }

        private async Task<List<ITrainingProgramme>> GetTrainingProgrammes()
        {
            var programmes = await _mediator.SendAsync(new GetTrainingProgrammesQueryRequest());

            return programmes.TrainingProgrammes;
        }

        public async Task CreateApprenticeshipUpdate(UpdateApprenticeshipViewModel apprenticeship, string hashedAccountId, string userId, string userName, string userEmail)
        {
            var employerId = _hashingService.DecodeValue(hashedAccountId);
            await _mediator.SendAsync(new CreateApprenticeshipUpdateCommand
            {
                EmployerId = employerId,
                ApprenticeshipUpdate = _apprenticeshipMapper.MapFrom(apprenticeship),
                UserId = userId,
                UserEmailAddress = userEmail,
                UserDisplayName = userName
            });
        }

        private async Task AssertApprenticeshipStatus(long accountId, long apprenticeshipId)
        {
            var result = await _mediator.SendAsync(new GetApprenticeshipUpdateRequest
            {
                AccountId = accountId,
                ApprenticeshipId = apprenticeshipId
            });

            if (result.ApprenticeshipUpdate != null)
                throw new InvalidStateException("Pending apprenticeship update");
        }

        public async Task<OrchestratorResponse<UpdateApprenticeshipViewModel>>
            GetOrchestratorResponseUpdateApprenticeshipViewModelFromCookie(string hashedAccountId,
                string hashedApprenticeshipId)
        {
            var mappedModel = _apprenticshipsViewModelCookieStorageService.Get(CookieName);

            var apprenticeshipId = _hashingService.DecodeValue(hashedApprenticeshipId);
            var accountId = _hashingService.DecodeValue(hashedAccountId);

            var apprenticeshipResult = await _mediator.SendAsync(
                new GetApprenticeshipQueryRequest
                {
                    AccountId = accountId,
                    ApprenticeshipId = apprenticeshipId
                });
            var apprenticeship = _apprenticeshipMapper.MapToApprenticeshipDetailsViewModel(apprenticeshipResult.Apprenticeship);
            mappedModel.OriginalApprenticeship = apprenticeship;
            mappedModel.HashedAccountId = hashedAccountId;
            mappedModel.HashedApprenticeshipId = hashedApprenticeshipId;

            return new OrchestratorResponse<UpdateApprenticeshipViewModel> {Data = mappedModel};
        }

        public void CreateApprenticeshipViewModelCookie(UpdateApprenticeshipViewModel model)
        {
            _apprenticshipsViewModelCookieStorageService.Delete(CookieName);
            model.OriginalApprenticeship = null;
            _apprenticshipsViewModelCookieStorageService.Create(model, CookieName);
        }

        public async Task SubmitReviewApprenticeshipUpdate(string hashedAccountId, string hashedApprenticeshipId, string userId, bool isApproved, string userName, string userEmail)
        {
            var accountId = _hashingService.DecodeValue(hashedAccountId);
            var apprenticeshipId = _hashingService.DecodeValue(hashedApprenticeshipId);

            await CheckUserAuthorization(async () =>
                {
                    await _mediator.SendAsync(new ReviewApprenticeshipUpdateCommand
                    {
                        AccountId = accountId,
                        ApprenticeshipId = apprenticeshipId,
                        UserId = userId,
                        IsApproved = isApproved,
                        UserDisplayName = userName,
                        UserEmailAddress = userEmail
                    });
                }
                , hashedAccountId, userId);
        }

        private void AssertApprenticeshipIsEditable(Apprenticeship apprenticeship)
        {
            var editable = new[] { PaymentStatus.Active, PaymentStatus.Paused, }.Contains(apprenticeship.PaymentStatus);

            if (!editable)
            {
                throw new ValidationException("Unable to edit apprenticeship - status not active or paused");
            }
        }

        public async Task<OrchestratorResponse<DataLockStatusViewModel>> GetDataLockStatus(string hashedAccountId, string hashedApprenticeshipId, string userId)
        {
            var accountId = _hashingService.DecodeValue(hashedAccountId);
            var apprenticeshipId = _hashingService.DecodeValue(hashedApprenticeshipId);

            return await CheckUserAuthorization(
                async () =>
                    {
                        var dataLock = await _mediator.SendAsync(
                                new GetApprenticeshipDataLockRequest { ApprenticeshipId = apprenticeshipId });

                        var apprenticeship = await _mediator.SendAsync(
                            new GetApprenticeshipQueryRequest { AccountId = accountId, ApprenticeshipId = apprenticeshipId });
                        
                        var programms = await GetTrainingProgrammes();
                        var currentProgram = programms.Single(m => m.Id == apprenticeship.Apprenticeship.TrainingCode);
                        var newProgram = programms.Single(m => m.Id == dataLock.DataLockStatus.IlrTrainingCourseCode);

                        return new OrchestratorResponse<DataLockStatusViewModel>
                            {
                                Data = new DataLockStatusViewModel
                                    {
                                        HashedAccountId = hashedAccountId,
                                        HashedApprenticeshipId = hashedApprenticeshipId,
                                        CurrentProgram = currentProgram,
                                        IlrProgram = newProgram,
                                        PeriodStartData = new DateTime(2017, 08, 08),
                                        ProviderName = apprenticeship.Apprenticeship.ProviderName,
                                        TriageStatus = dataLock.DataLockStatus.TriageStatus
                                    }
                           };
            }, hashedAccountId, userId);
        }
    }
}
