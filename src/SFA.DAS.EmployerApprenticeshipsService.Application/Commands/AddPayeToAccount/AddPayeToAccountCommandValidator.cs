using System;
using System.Threading.Tasks;
using SFA.DAS.EAS.Application.Validation;
using SFA.DAS.EAS.Domain;
using SFA.DAS.EAS.Domain.Data;

namespace SFA.DAS.EAS.Application.Commands.AddPayeToAccount
{
    public class AddPayeToAccountCommandValidator : IValidator<AddPayeToAccountCommand>
    {
        private readonly IMembershipRepository _membershipRepository;

        public AddPayeToAccountCommandValidator(IMembershipRepository membershipRepository)
        {
            _membershipRepository = membershipRepository;
        }

        public ValidationResult Validate(AddPayeToAccountCommand item)
        {
            throw new System.NotImplementedException();
        }

        public async Task<ValidationResult> ValidateAsync(AddPayeToAccountCommand item)
        {
            var validationResult = new ValidationResult();

            CheckFieldsArePopulated(item, validationResult);

            if (validationResult.IsValid())
            {
                var member = await _membershipRepository.GetCaller(item.HashedId, item.ExternalUserId);

                if (member == null)
                {
                    validationResult.AddError(nameof(member),"Unauthorised: User not connected to account");
                }
                else
                {
                    if (member.RoleId != (short) Role.Owner)
                    {
                        validationResult.AddError(nameof(member), "Unauthorised: User is not an owner");
                    }
                }
            }

            return validationResult;
        }

        private static void CheckFieldsArePopulated(AddPayeToAccountCommand item, ValidationResult validationResult)
        {
            if (string.IsNullOrEmpty(item.AccessToken))
            {
                validationResult.AddError(nameof(item.AccessToken), "AccessToken has not been supplied");
            }
            if (string.IsNullOrEmpty(item.HashedId))
            {
                validationResult.AddError(nameof(item.HashedId), "HashedId has not been supplied");
            }
            if (string.IsNullOrEmpty(item.Empref))
            {
                validationResult.AddError(nameof(item.Empref), "Empref has not been supplied");
            }
            if (string.IsNullOrEmpty(item.ExternalUserId))
            {
                validationResult.AddError(nameof(item.ExternalUserId), "ExternalUserId has not been supplied");
            }
            if (string.IsNullOrEmpty(item.RefreshToken))
            {
                validationResult.AddError(nameof(item.RefreshToken), "RefreshToken has not been supplied");
            }
        }
    }
}