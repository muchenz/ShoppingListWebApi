using MediatR;
using ServiceMediatR.UserCommandAndQuerry;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using SignalRService;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMediatR.SignalREvents
{



    public record ListItemAddedSignalRNotification(int ListItemId, int ListAggregationId, int ListId, string SignalRId):INotification;
    public record ListItemDeletedSignalRNotification(int ListItemId, int ListAggregationId, string SignalRId) : INotification;
    public record ListItemEditedSignalRNotification(int ListItemId, int ListAggregationId, string SignalRId) :INotification;



    public record SignaREnvelope(string SignalRId, string SiglREventName,  string SerializedEvent );
    public abstract record ListItemSignalREvent(int ListItemId, int ListAggregationId);
    public record ListItemAddedSignalREvent(int ListItemId, int ListAggregationId, int ListId): ListItemSignalREvent(ListItemId, ListAggregationId);
    public record ListItemDeletedSignalREvent(int ListItemId, int ListAggregationId) : ListItemSignalREvent(ListItemId, ListAggregationId);
    public record ListItemEditedSignalREvent(int ListItemId, int ListAggregationId) : ListItemSignalREvent(ListItemId, ListAggregationId);

    public class AddListItemSignalREventHandler : INotificationHandler<ListItemAddedSignalRNotification>
    {
        private readonly SignarRService _signarRService;
        private readonly IUserEndpoint _userEndpoint;

        public AddListItemSignalREventHandler(SignarRService signarRService, IUserEndpoint userEndpoint)
        {
            _signarRService = signarRService;
            _userEndpoint = userEndpoint;
        }

        public async Task Handle(ListItemAddedSignalRNotification item, CancellationToken cancellationToken)
        {

            var userList = await _userEndpoint.GetUserIdsFromListAggrIdAsync(item.ListAggregationId);

            var envelope = new SignaREnvelope(item.SignalRId, SiganalREventName.ListItemAdded,
                         JsonSerializer.Serialize(new ListItemAddedSignalREvent(item.ListItemId, item.ListAggregationId, item.ListId)));

            await _signarRService.SendListItemRefreshMessageToUsersAsync(userList,
              envelope.SiglREventName,
              JsonSerializer.Serialize(item));
        }


    }

    public class DeleteListItemSignalREventHandler : INotificationHandler<ListItemDeletedSignalRNotification>
    {
        private readonly SignarRService _signarRService;
        private readonly IUserEndpoint _userEndpoint;

        public DeleteListItemSignalREventHandler(SignarRService signarRService, IUserEndpoint userEndpoint)
        {
            _signarRService = signarRService;
            _userEndpoint = userEndpoint;
        }

        public async Task Handle(ListItemDeletedSignalRNotification item, CancellationToken cancellationToken)
        {

            var userList = await _userEndpoint.GetUserIdsFromListAggrIdAsync(item.ListAggregationId);

            var envelope = new SignaREnvelope(item.SignalRId, SiganalREventName.ListItemDeleted,
                                JsonSerializer.Serialize(new ListItemDeletedSignalREvent(item.ListItemId, item.ListAggregationId)));

            await _signarRService.SendListItemRefreshMessageToUsersAsync(userList,
                envelope.SiglREventName,
               JsonSerializer.Serialize(envelope));
        }

        public class EditListItemSignalREventHandler : INotificationHandler<ListItemEditedSignalRNotification>
        {
            private readonly SignarRService _signarRService;
            private readonly IUserEndpoint _userEndpoint;

            public EditListItemSignalREventHandler(SignarRService signarRService, IUserEndpoint userEndpoint)
            {
                _signarRService = signarRService;
                _userEndpoint = userEndpoint;
            }

            public async Task Handle(ListItemEditedSignalRNotification item, CancellationToken cancellationToken)
            {

                var userList = await _userEndpoint.GetUserIdsFromListAggrIdAsync(item.ListAggregationId);

                var envelope = new SignaREnvelope(item.SignalRId, SiganalREventName.ListItemEdited,
                         JsonSerializer.Serialize(new ListItemEditedSignalREvent(item.ListItemId, item.ListAggregationId)));


                await _signarRService.SendListItemRefreshMessageToUsersAsync(userList,
               envelope.SiglREventName,
               JsonSerializer.Serialize(envelope));
            }


        }

    }
}
