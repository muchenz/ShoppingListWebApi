using MediatR;
using SignalRService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMediatR.SignalREvents
{
    public class AddEditSaveDeleteListItemEvent: INotification
    {
      

        public AddEditSaveDeleteListItemEvent(IEnumerable<int> usersIds, string command = null, int? id1 = null, int? listAggregationId = null, int? parentId = null)
        {
            UsersIds = usersIds;
            Command = command;
            Id1 = id1;
            ListAggregationId = listAggregationId;
            ParentId = parentId;
        }

        public IEnumerable<int> UsersIds { get; }
        public string Command { get; }
        public int? Id1 { get; }
        public int? ListAggregationId { get; }
        public int? ParentId { get; }
    }

    public class AddEditSaveDeleteListItemHandler : INotificationHandler<AddEditSaveDeleteListItemEvent>
    {
        private readonly SignarRService _signarRService;

        public AddEditSaveDeleteListItemHandler(SignarRService signarRService)
        {
            _signarRService = signarRService;
        }

        public async Task Handle(AddEditSaveDeleteListItemEvent item, CancellationToken cancellationToken)
        {

            await _signarRService.SendRefreshMessageToUsersAsync(item.UsersIds,
                item.Command,
                item.Id1,
                item.ListAggregationId,item.ParentId);
        }


    }
}
