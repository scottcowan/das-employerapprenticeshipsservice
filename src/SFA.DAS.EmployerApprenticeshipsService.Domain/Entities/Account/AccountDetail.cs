﻿using System;
using System.Collections.Generic;

namespace SFA.DAS.EAS.Domain.Entities.Account
{
    public class AccountDetail
    {
        public string HashedId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public string OwnerEmail { get; set; }
        public List<long> LegalEntities { get; set; } = new List<long>();
        public List<string> PayeSchemes { get; set; } = new List<string>();
    }
}