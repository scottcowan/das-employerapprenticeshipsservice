using System.Threading.Tasks;
using SFA.DAS.EmployerApprenticeshipsService.Application.Messages;
using SFA.DAS.Messaging;

namespace SFA.DAS.LevyAggregationProvider.Worker.UnitTests
{
    public class FakeMessage : Message<EmployerRefreshLevyQueueMessage>
    {
        public FakeMessage(EmployerRefreshLevyQueueMessage content) : base(content)
        {
            
        }

        public override Task CompleteAsync()
        {
            throw new System.NotImplementedException();
        }

        public override Task AbortAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}