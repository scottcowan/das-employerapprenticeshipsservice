﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SFA.DAS.EmployerApprenticeshipsService.Domain.Interfaces;
using SFA.DAS.EmployerApprenticeshipsService.Domain.Models.HmrcEmployer;
using SFA.DAS.EmployerApprenticeshipsService.Infrastructure.Data;

namespace SFA.DAS.EmployerApprenticeshipsService.Infrastructure.Services
{
    public class EmpRefFileBasedService : FileSystemRepository, IEmpRefFileBasedService
    {
        public EmpRefFileBasedService() : base("EmpRefs")
        {

        }
        
        public async Task<string> GetEmpRef(string email, string id)
        {
            var empRefLookup = await ReadFileById<EmpRefLookup>(id);

            return empRefLookup.Data.FirstOrDefault(x => email.StartsWith(x.Username))?.EmpRef;
        }
    }
}