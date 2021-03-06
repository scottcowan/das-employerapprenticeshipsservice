﻿CREATE VIEW [employer_financial].[GetLevyDeclaration]
AS 
SELECT 
	ld.Id AS Id,	
	ld.AccountId as AccountId,
	ld.empRef AS EmpRef,
	ld.SubmissionDate AS SubmissionDate,
	ld.SubmissionId AS SubmissionId,
	ld.LevyDueYTD AS LevyDueYTD,
	isnull(t.Amount,1) AS EnglishFraction,
	w.amount as TopUpPercentage,
	ld.PayrollYear as PayrollYear,
	ld.PayrollMonth as PayrollMonth,
	CASE 
		x.submissionDate 
	when 
		ld.SubmissionDate then 1 
	else 0 end as LastSubmission,
	ld.CreatedDate,
	ld.EndOfYearAdjustment,
	ld.EndOfYearAdjustmentAmount,
	ld.LevyAllowanceForYear AS LevyAllowanceForYear,
	ld.DateCeased AS DateCeased,
	ld.InactiveFrom AS InactiveFrom,
	ld.InactiveTo AS InactiveTo,
	ld.HmrcSubmissionId AS HmrcSubmissionId
FROM [employer_financial].[LevyDeclaration] ld
left join
(
	select 
		Max(submissionDate) submissionDate, 
		empRef ,
		PayrollYear,
		PayrollMonth
	FROM 
		[employer_financial].LevyDeclaration 
	WHERE EndOfYearAdjustment = 0
	group by empRef,PayrollYear,PayrollMonth
)x on x.empRef = ld.empRef and x.PayrollMonth = ld.PayrollMonth and x.PayrollYear = ld.PayrollYear AND ld.EndOfYearAdjustment = 0

OUTER APPLY
(
	SELECT TOP 1 Amount
	FROM [employer_financial].[EnglishFraction] ef
	WHERE ef.EmpRef = ld.empRef 
		AND ef.[DateCalculated] <= ld.[SubmissionDate]
	ORDER BY [DateCalculated] DESC
) t
outer apply
(
	SELECT top 1 Amount
	from [employer_financial].[TopUpPercentage] tp
	WHERE tp.[DateFrom] <= ld.[SubmissionDate]
) w
