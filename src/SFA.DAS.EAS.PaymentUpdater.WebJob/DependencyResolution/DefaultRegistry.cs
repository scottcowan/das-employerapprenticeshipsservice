﻿using System;
using SFA.DAS.EAS.Domain.Configuration;
using SFA.DAS.EAS.Domain.Interfaces;
using SFA.DAS.EAS.PaymentUpdater.WebJob.Updater;
using StructureMap;
using StructureMap.Graph;

namespace SFA.DAS.EAS.PaymentUpdater.WebJob.DependencyResolution
{
    public class DefaultRegistry : Registry
    {
        public DefaultRegistry()
        {
            Scan(
                scan =>
                {
                    scan.AssembliesFromApplicationBaseDirectory(a => a.GetName().Name.StartsWith("SFA.DAS"));
                    scan.RegisterConcreteTypesAgainstTheFirstInterface();
                });

            For<IConfiguration>().Use<EmployerApprenticeshipsServiceConfiguration>();
            For<IPaymentProcessor>().Use<PaymentProcessor>();
        }
    }
}