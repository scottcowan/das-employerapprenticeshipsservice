﻿namespace SFA.DAS.EAS.Web.Models
{
    public sealed class SubmitCommitmentModel
    {
        public string HashedAccountId { get; set; }
        public string HashedCommitmentId { get; set; }
        public string Message { get; set; }
    }
}