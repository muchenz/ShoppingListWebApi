using FirebaseDatabase;
using Microsoft.Extensions.Hosting;
using Shared.DataEndpoints.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Data;

public class ToDeleteService : BackgroundService
{
    private readonly ToDeleteEndpoint _toDeleteEndpoint;
    private readonly DeleteChannel _deleteChannel;

    public ToDeleteService(ToDeleteEndpoint toDeleteEndpoint, DeleteChannel deleteChannel)
    {
        _toDeleteEndpoint = toDeleteEndpoint;
        _deleteChannel = deleteChannel;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {

            await _deleteChannel.Reader.ReadAsync();

            var itemsToDelete = await _toDeleteEndpoint.GetToDelete();

            foreach (var toDelete in itemsToDelete)
            {
                try
                {
                    if (toDelete.ItemTodelete.DeletedAt == null)
                    {
                        if (toDelete.ItemTodelete.Type == nameof(ListAggregator))
                        {
                            if (int.TryParse(toDelete.ItemTodelete.ItemToDeleteId, out var id))

                                await _toDeleteEndpoint.DeleteListAggrBatchAsync(id);
                        }
                        else if (toDelete.ItemTodelete.Type == nameof(List))
                        {
                            if (int.TryParse(toDelete.ItemTodelete.ItemToDeleteId, out var id))

                                await _toDeleteEndpoint.DeleteListBatchAsync(id);
                        }

                    }


                    await _toDeleteEndpoint.Deleted(toDelete.ToDeleteRecordId);

                }
                catch (System.Exception ex)
                {
                    // Log the exception or handle it as needed
                    Console.WriteLine($"Error processing ToDelete item with ID {toDelete.ToDeleteRecordId}: {ex.Message}");
                }
            }


           // await Task.Delay(1000);
        }

    }
}
