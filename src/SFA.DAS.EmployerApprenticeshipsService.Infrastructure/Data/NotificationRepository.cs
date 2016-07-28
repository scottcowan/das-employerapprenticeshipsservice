﻿using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NLog;
using SFA.DAS.EmployerApprenticeshipsService.Domain.Configuration;
using SFA.DAS.EmployerApprenticeshipsService.Domain.Data;
using SFA.DAS.EmployerApprenticeshipsService.Domain.Models.Notification;

namespace SFA.DAS.EmployerApprenticeshipsService.Infrastructure.Data
{
    public class NotificationRepository : BaseRepository, INotificationRepository
    {
        public NotificationRepository(EmployerApprenticeshipsServiceConfiguration configuration, ILogger logger)
            : base(configuration, logger)
        {
        }

        public async Task<int> Create(NotificationMessage message)
        {
            return await WithConnection(async c =>
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", message.UserId, DbType.Int32);
                parameters.Add("@DateTime", message.DateTime, DbType.DateTime);
                parameters.Add("@ForceFormat", message.ForceFormat, DbType.Boolean);
                parameters.Add("@TemplateId", message.TemplatedId, DbType.String);
                parameters.Add("@Data", message.Data, DbType.String);
                parameters.Add("@MessageFormat", message.MessageFormat, DbType.Int16);
                parameters.Add("@Id", null, DbType.Int32, ParameterDirection.Output, 4);

                await c.ExecuteAsync("[dbo].[CreateNotification]", parameters, commandType: CommandType.StoredProcedure);

                return parameters.Get<int>("@Id");
            });
        }

        public async Task<NotificationMessage> GetById(int expectedMessageId)
        {
            var result = await WithConnection(async c =>
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Id", expectedMessageId, DbType.Int32);

                return await c.QueryAsync<NotificationMessage>("[dbo].[GetNotification]", parameters, commandType: CommandType.StoredProcedure);

            });

            return result.SingleOrDefault();
        }
    }
}
