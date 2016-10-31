﻿CREATE TABLE levy.TransactionLine
(
	AccountId BIGINT NOT NULL,
	SubmissionId BIGINT NULL,
	PaymentId UNIQUEIDENTIFIER NULL,
	TransactionDate DATETIME NOT NULL,
	TransactionType TINYINT NOT NULL DEFAULT 0, 
	Amount DECIMAL(18,4) NOT NULL DEFAULT 0, 
    CONSTRAINT [AK_TransactionLine_Column] UNIQUE ([SubmissionId],[TransactionType],[PaymentId]),
)