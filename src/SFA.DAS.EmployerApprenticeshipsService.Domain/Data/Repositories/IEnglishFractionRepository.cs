﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SFA.DAS.EAS.Domain.Models.Levy;

namespace SFA.DAS.EAS.Domain.Data.Repositories
{
    public interface IEnglishFractionRepository
    {
        Task<DateTime> GetLastUpdateDate();
        Task<DasEnglishFraction> GetEmployerFraction(DateTime dateCalculated, string employerReference);
        Task<IEnumerable<DasEnglishFraction>> GetAllEmployerFractions(string employerReference);
        Task CreateEmployerFraction(DasEnglishFraction fractions, string employerReference);
        Task SetLastUpdateDate(DateTime dateUpdated);
        Task<DasEnglishFraction> GetCurrentFractionForScheme(string employerReference);
    }
}
