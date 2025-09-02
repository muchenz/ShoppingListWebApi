using AutoMapper;
using EFDataBase;
using Microsoft.EntityFrameworkCore;
using ServiceMediatR.Wrappers;
using Shared.DataEndpoints.Abstaractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shared.DataEndpoints.Models;

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

        public SavePropertyCommandHandler(IMapper mapper, IListItemEndpoint listItemEndpoint)
        {
            _listItemEndpoint = listItemEndpoint;
        }

        public async Task<Result<ListItem>> Handle(SavePropertyCommand request, CancellationToken cancellationToken)
        {

            if (!await CheckIntegrityListItemAsync(request.Item.ListItemId, request.ListAggregationId))
                return Result<ListItem>.Error("Forbbidden");

            var listItem = await _listItemEndpoint.SavePropertyAsync(request.Item, request.PropertyName, request.ListAggregationId);

            return Result<ListItem>.Ok(listItem);
        }

        Task<bool> CheckIntegrityListItemAsync(int listItemId, int listAggregationId)
        {

            return  _listItemEndpoint.CheckIntegrityListItemAsync(listItemId, listAggregationId);
        }
    }
}
