using System.Threading.Tasks;
using MediatR;
using Moq;
using NLog;
using NUnit.Framework;
using SFA.DAS.EmployerApprenticeshipsService.Application.Messages;
using SFA.DAS.LevyAggregationProvider.Worker.Providers;
using SFA.DAS.LevyAggregationProvider.Worker.Queries.GetLevyDeclaration;
using SFA.DAS.Messaging;
using SFA.DAS.Messaging.FileSystem;

namespace SFA.DAS.LevyAggregationProvider.Worker.UnitTests
{
    [TestFixture]
    public class LevyAggregationManagerTests
    {
        private Mock<IPollingMessageReceiver> _pollingMessageReceiver;
        private Mock<IMediator> _mediator;
        private Mock<ILogger> _logger;
        private LevyAggregationManager _manager;
        private FakeMessage _message;

        [SetUp]
        public void Setup()
        {
            _pollingMessageReceiver = new Mock<IPollingMessageReceiver>();
            _mediator = new Mock<IMediator>();
            _logger = new Mock<ILogger>();
            _manager = new LevyAggregationManager(_pollingMessageReceiver.Object, _mediator.Object, _logger.Object);

            _message = new FakeMessage(new EmployerRefreshLevyQueueMessage
            {
                AccountId = 1
            });
        }

        [Test]
        public async Task MessageDoesNotIncludeAccount()
        {
            _message = new FakeMessage(new EmployerRefreshLevyQueueMessage());

            _pollingMessageReceiver.Setup(x => x.ReceiveAsAsync<EmployerRefreshLevyQueueMessage>())
                .ReturnsAsync(_message);

            await _manager.Process();

            _mediator.Verify(x => x.SendAsync(It.IsAny<GetLevyDeclarationRequest>()), Times.Never);
        }

        [Test]
        public async Task MessageDoesHasAccountIdOfZero()
        {
            _message = new FakeMessage(new EmployerRefreshLevyQueueMessage
            {
                AccountId = 0
            });

            _pollingMessageReceiver.Setup(x => x.ReceiveAsAsync<EmployerRefreshLevyQueueMessage>())
                .ReturnsAsync(_message);

            await _manager.Process();

            _mediator.Verify(x => x.SendAsync(It.IsAny<GetLevyDeclarationRequest>()), Times.Never);
        }
    }
}