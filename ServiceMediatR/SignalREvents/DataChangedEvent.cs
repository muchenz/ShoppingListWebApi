using MediatR;
using SignalRService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMediatR.SignalREvents
{
    public class DataChangedEvent : INotification
    {
        public DataChangedEvent(IEnumerable<int> userList)
        {
            UserList = userList;
        }

        public IEnumerable<int> UserList { get; }
    }

    public class DataChangedEventHandler : INotificationHandler<DataChangedEvent>
    {
        private readonly SignarRService _signarRService;

        public DataChangedEventHandler(SignarRService signarRService)
        {
            _signarRService = signarRService;
        }
        public async Task Handle(DataChangedEvent notification, CancellationToken cancellationToken)
        {
            await _signarRService.SendRefreshMessageToUsersAsync(notification.UserList);
        }
    }
}
