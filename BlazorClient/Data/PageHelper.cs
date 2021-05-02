using BlazorClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorClient.Data
{
    public class PageHelper
    {
      
        public static async Task<ICollection<T>> ChangeOrderItemsInList<T>(T dropItem, T dragItem,
       ICollection<T> choosedList, Func<List<T>, Task> function) where T : IModelItem
        {

            List<T> listOfListItemsToChange = null;
            int tempOrder = dropItem.Order;

            if (dropItem.Order > dragItem.Order)
            {

                listOfListItemsToChange = choosedList.Where(a => a.Order > dragItem.Order && a.Order <= dropItem.Order).ToList();
                listOfListItemsToChange.ForEach(a => a.Order -= 1);

            }
            else if (dropItem.Order < dragItem.Order)
            {

                listOfListItemsToChange = choosedList.Where(a => a.Order < dragItem.Order && a.Order >= dropItem.Order).ToList();
                listOfListItemsToChange.ForEach(a => a.Order += 1);

            }

            if (listOfListItemsToChange != null)
            {
                dragItem.Order = tempOrder;

                listOfListItemsToChange.Add(dragItem);



                switch (dropItem)
                {
                    case ListItem i:
                      //  await function.Invoke(listOfListItemsToChange);
                        break;
                    case List i:
                   //     await function.Invoke(listOfListItemsToChange);
                        break;
                    case ListAggregator i:
                    //    await function.Invoke(listOfListItemsToChange);
                        break;

                }

                choosedList = choosedList.OrderByDescending(a => a.Order).ToList();

            }

            return choosedList;
        }
    }
}
