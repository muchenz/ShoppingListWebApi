using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseDatabase;
public class ToDeleteEndpoint
{
    FirestoreDb Db;

    CollectionReference _toDelete;
    CollectionReference _userListAggrCol;
    CollectionReference _listAggrCol;
    CollectionReference _listCol;
    CollectionReference _listItemCol;


    public ToDeleteEndpoint()
    {
        Db = FirestoreDb.Create("testnosqldb1");

        _listAggrCol = Db.Collection("listAggregator");
        _listCol = Db.Collection("list");
        _listItemCol = Db.Collection("listItem");
        _toDelete = Db.Collection("toDelete");
        _userListAggrCol = Db.Collection("userListAggregator");

    }

    public async Task Deleted(string id)
    {

        var deletedRef =  _toDelete.Document(id);

        await deletedRef.UpdateAsync(nameof(ToDelete.DeletedAt), DateTime.UtcNow);

    }

    public async Task<IEnumerable<(ToDelete ItemTodelete, string id)>> GetToDelete()
    {
        var snapToDeleteFD = await _toDelete.WhereEqualTo(nameof(ToDelete.DeletedAt), null).GetSnapshotAsync();

        return snapToDeleteFD.Select(a => (a.ConvertTo<ToDelete>(), a.Id));
    }


    private async Task<int> DeleteListBatchAsync(int listId)
    {
        var totalAmountDeleted = 0;


        var listRef = _listCol.Document(listId.ToString());
        var listSnap = await listRef.GetSnapshotAsync();

        if (!listSnap.Exists) return totalAmountDeleted;

        var list = listSnap.ConvertTo<ListFD>();


        totalAmountDeleted += await DeleteDocumentsBatchAsync(_listItemCol, list.ListItems);


        await listRef.DeleteAsync();
        totalAmountDeleted++;
        return totalAmountDeleted;
    }


    private async Task<int> DeleteUserListAggrBatchAsync(int listAggregationId)
    {
        var totalAmountDeleted = 0;

        var userListAggrQuery = _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregationId);
        var userListAggrSnapshot = await userListAggrQuery.GetSnapshotAsync();

        var batch = Db.StartBatch();
        var batchCounter = 0;

        foreach (var item in userListAggrSnapshot)
        {
            batch.Delete(item.Reference);
            batchCounter++;
            totalAmountDeleted++;

            if (batchCounter == 500)
            {
                await batch.CommitAsync();
                batch = Db.StartBatch();
                batchCounter = 0;
            }
        }

        if (batchCounter > 0)
        {
            await batch.CommitAsync();
        }

        return totalAmountDeleted;
    }

    private async Task<int> DeleteDocumentsBatchAsync(CollectionReference collectionReference, IEnumerable<int> itemIds)
    {
        var totalAmountDeleted = 0;

        var batch = Db.StartBatch();
        var batchCounter = 0;

        var chunkedIds = itemIds.Chunk(10);
        var listDocuments = new List<DocumentSnapshot>();

        foreach (var id in chunkedIds)
        {
            var query = collectionReference.WhereIn(FieldPath.DocumentId, id.Select(i => i.ToString()));
            var snapshot = await query.GetSnapshotAsync();
            listDocuments.AddRange(snapshot.Documents);

        }
        foreach (var item in listDocuments)
        {
            batch.Delete(item.Reference);
            batchCounter++;
            totalAmountDeleted++;
            if (batchCounter == 500)
            {
                await batch.CommitAsync();
                batch = Db.StartBatch();
                batchCounter = 0;
            }
        }



        if (batchCounter > 0)
        {
            await batch.CommitAsync();
        }

        return totalAmountDeleted;
    }



    public async Task<int> DeleteListAggrAsync2(int listAggregationId)
    {
        try
        {
            int amount = 0;
            amount += await DeleteUserListAggrBatchAsync(listAggregationId);

            var listAggrRef = _listAggrCol.Document(listAggregationId.ToString());
            var listAggrSnap = await listAggrRef.GetSnapshotAsync();

            if (!listAggrSnap.Exists) return amount;

            var listAggr = listAggrSnap.ConvertTo<ListAggregatorFD>();


            var allLists = new List<ListFD>();

            var chunkedListIds = listAggr.Lists.Chunk(10);


            foreach (var listId in chunkedListIds)
            {
                var query = _listCol.WhereIn(FieldPath.DocumentId, listId.Select(i => i.ToString()));

                var listsSanp = await query.GetSnapshotAsync();

                foreach (var listSnap in listsSanp.Documents)
                {

                    var list = listSnap.ConvertTo<ListFD>();
                    allLists.Add(list);
                }
            }


            foreach (var list in allLists)
            {

                amount += await DeleteDocumentsBatchAsync(_listItemCol, list.ListItems);
            }


            amount += await DeleteDocumentsBatchAsync(_listCol, allLists.Select(a => a.ListId));
            await listAggrRef.DeleteAsync();
            amount++;
            return amount;
        }
        catch (Exception ex)
        {
            // Handle exceptions as needed
            Console.WriteLine($"Error deleting list aggregator: {ex.Message}");
            return 0;
        }
    }
}
