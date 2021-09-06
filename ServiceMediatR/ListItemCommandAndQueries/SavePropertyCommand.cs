using AutoMapper;
using EFDataBase;
using Microsoft.EntityFrameworkCore;
using ServiceMediatR.Wrappers;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMediatR.ListItemCommandAndQueries
{
    public class SavePropertyCommand : IRequestWrapper<ListItem>
    {
        public SavePropertyCommand(ListItem item, string propertyName, int listAggregationId)
        {
            Item = item;
            PropertyName = propertyName;
            ListAggregationId = listAggregationId;
        }

        public ListItem Item { get; }
        public string PropertyName { get; }
        public int ListAggregationId { get; }
    }


    public class SavePropertyCommandHandler : IHandlerWrapper<SavePropertyCommand, ListItem>
    {
        private readonly IListItemEndpoint _listItemEndpoint;

        public SavePropertyCommandHandler(ShopingListDBContext context, IMapper mapper, IListItemEndpoint listItemEndpoint)
        {
            _listItemEndpoint = listItemEndpoint;
        }

        public async Task<MessageAndStatusAndData<ListItem>> Handle(SavePropertyCommand request, CancellationToken cancellationToken)
        {

            if (!await CheckIntegrityListItemAsync(request.Item.ListItemId, request.ListAggregationId))
                return await Task.FromResult(MessageAndStatusAndData.Fail<ListItem>("Forbbidden"));

            var listItem = await _listItemEndpoint.SavePropertyAsync(request.Item, request.PropertyName);

            return await Task.FromResult(MessageAndStatusAndData.Ok(listItem, "OK"));
        }

        Task<bool> CheckIntegrityListItemAsync(int listItemId, int listAggregationId)
        {

            return  _listItemEndpoint.CheckIntegrityListItemAsync(listItemId, listAggregationId);
        }
    }
}
