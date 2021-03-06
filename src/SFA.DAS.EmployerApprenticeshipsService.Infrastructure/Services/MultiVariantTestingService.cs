﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web.UI.WebControls;
using NLog;
using SFA.DAS.EAS.Domain.Interfaces;
using SFA.DAS.EAS.Domain.Models.UserView;
using SFA.DAS.EAS.Infrastructure.Caching;

namespace SFA.DAS.EAS.Infrastructure.Services
{
    public class MultiVariantTestingService : AzureServiceBase<MultiVariantViewLookup>, IMultiVariantTestingService
    {
        private readonly ICacheProvider _cacheProvider;
        public override string ConfigurationName => "SFA.DAS.EmployerApprenticeshipsService.MultiVariantTesting";
        public sealed override ILogger Logger { get; set; }

        public MultiVariantTestingService(ICacheProvider cacheProvider, ILogger logger)
        {
            _cacheProvider = cacheProvider;
            Logger = logger;
        }

        public MultiVariantViewLookup GetMultiVariantViews()
        {
            var views = _cacheProvider.Get<MultiVariantViewLookup>(nameof(MultiVariantViewLookup));

            if (views == null)
            {
                views = GetDataFromStorage();
                if (views.Data != null && views.Data.Any())
                {
                    _cacheProvider.Set(nameof(MultiVariantViewLookup),views,new TimeSpan(0,30,0));
                }
            }

            return views;
        }

        public string GetRandomViewNameToShow(List<ViewAccess> views)
        {
            var randomNumber = new Random().Next(views.Count);

            if (randomNumber == views.Count)
            {
                return null;
            }
            return views[randomNumber].ViewName;
        }
    }
}