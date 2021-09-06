using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public interface IListEndpoint
    {

        Task<List> AddListAsync(int parentId, List list, int ListAggregationId);
        Task<int> DeleteListAsync(int listId, int listAggregationId);
        Task<bool> CheckIntegrityListAsync(int listId, int listAggregationId);
        Task<bool> CheckIntegrityListAggrAsync(int listAggrId, int listAggregationId);
        Task<List> EditListAsync(List list);
    }
}
