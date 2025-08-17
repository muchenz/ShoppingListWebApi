using FirebaseDatabase;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared.DataEndpoints.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Data;

internal class ToDeleteService : BackgroundService
{
    private readonly ToDeleteEndpoint _toDeleteEndpoint;
    private readonly DeleteChannel _deleteChannel;
    private readonly FirebaseFDOptions _firebaseFDOptions;

    public ToDeleteService(ToDeleteEndpoint toDeleteEndpoint, DeleteChannel deleteChannel, IOptions<FirebaseFDOptions> optionsFire)
    {
        _toDeleteEndpoint = toDeleteEndpoint;
        _deleteChannel = deleteChannel;
        _firebaseFDOptions = optionsFire.Value;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {

            if (_firebaseFDOptions.UseChannel) {
                await _deleteChannel.Reader.ReadAsync(stoppingToken);
            }
            else
            {
                await Task.Delay(_firebaseFDOptions.PollingDelay, stoppingToken);
            }


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

                       
        }

    }
}
