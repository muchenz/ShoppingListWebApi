using AutoMapper;
using EFDataBase;
using ServiceMediatR.Wrappers;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMediatR.ListCommandAndQueries
{
    public class EditListCommand: IRequestWrapper<List>
    {
        public EditListCommand(List list, int listAggregationId)
        {
            List = list;
            ListAggregationId = listAggregationId;
        }

        public List List { get; }
        public int ListAggregationId { get; }
    }


    //public class EditListHandler : BaseForHandler, IHandlerWrapper<EditListCommand, List>
    public class EditListHandler : IHandlerWrapper<EditListCommand, List>
    {
        private readonly IListEndpoint _listEndpoint;

        public EditListHandler(IListEndpoint listEndpoint) 
        {
            _listEndpoint = listEndpoint;
        }
        public async Task<MessageAndStatusAndData<List>> Handle(EditListCommand request, CancellationToken cancellationToken)
        {



             if (!await _listEndpoint.CheckIntegrityListAsync(request.List.ListId, request.ListAggregationId)) 
                return MessageAndStatusAndData<List>.Fail("Forbbidden");

            //_context.ListItems.Remove(_context.ListItems.Single(a => a.ListItemId == ItemId));

            var listItem = await _listEndpoint.EditListAsync(request.List, request.ListAggregationId);

            return MessageAndStatusAndData<List>.Ok(listItem);

        }
    }
}
