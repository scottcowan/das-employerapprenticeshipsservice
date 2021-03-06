﻿using System;
using System.Threading.Tasks;
using MediatR;
using NLog;
using SFA.DAS.EAS.Application.Validation;
using SFA.DAS.EAS.Domain.Interfaces;

namespace SFA.DAS.EAS.Application.Queries.GetPostcodeAddress
{
    public class GetPostcodeAddressHandler : IAsyncRequestHandler<GetPostcodeAddressRequest, GetPostcodeAddressResponse>
    {
        private readonly IAddressLookupService _addressLookupService;
        private readonly IValidator<GetPostcodeAddressRequest> _validator;
        private readonly ILogger _logger;

        public GetPostcodeAddressHandler(IAddressLookupService addressLookupService, IValidator<GetPostcodeAddressRequest> validator, ILogger logger)
        {
            _addressLookupService = addressLookupService;
            _validator = validator;
            _logger = logger;
        }

        public async Task<GetPostcodeAddressResponse> Handle(GetPostcodeAddressRequest request)
        {
            var validationResult = _validator.Validate(request);

            if (!validationResult.IsValid())
            {
                throw new InvalidRequestException(validationResult.ValidationDictionary);
            }

            var addresses = await _addressLookupService.GetAddressesByPostcode(request.Postcode);

            return new GetPostcodeAddressResponse {Addresses = addresses};
        }
    }
}
