CREATE TABLE [dbo].[SmsLog] (
    [_RowId]       INT              IDENTITY (1, 1) NOT NULL,
    [SmsSid]       VARCHAR (40)     NOT NULL,
    [Phone]        VARCHAR (50)     NOT NULL,
    [SentDate]     DATETIME2 (0)    NOT NULL,
    [ElectionGuid] UNIQUEIDENTIFIER NULL,
    [PersonGuid]   UNIQUEIDENTIFIER NULL,
    [LastStatus]   VARCHAR (50)     NULL,
    [LastDate]     DATETIME2 (7)    NULL,
    [ErrorCode]    INT              NULL,
    CONSTRAINT [PK_SmsLog] PRIMARY KEY CLUSTERED ([_RowId] ASC)
);




GO
CREATE NONCLUSTERED INDEX [IX_SmsLog]
    ON [dbo].[SmsLog]([SmsSid] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_SmsLog_Election_Date]
    ON [dbo].[SmsLog]([ElectionGuid] ASC, [LastDate] DESC)
    INCLUDE([SmsSid], [Phone], [SentDate], [PersonGuid], [LastStatus], [ErrorCode]);

