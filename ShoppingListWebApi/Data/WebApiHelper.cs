using EFDataBase;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Data
{
    public class WebApiHelper
    {
      

        public static async Task<IEnumerable<int>> GetuUserIdFromListAggrIdAsync(int listAggrId, ShopingListDBContext _context)
        {
            var userList = await _context.UserListAggregators.Where(a => a.ListAggregatorId == listAggrId).Select(a => a.UserId).ToListAsync();

            return userList;
    }

    }

}
