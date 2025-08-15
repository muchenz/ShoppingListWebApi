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

    public ToDeleteService(ToDeleteEndpoint toDeleteEndpoint)
    {
        _toDeleteEndpoint = toDeleteEndpoint;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {

            var itemsToDelete = await _toDeleteEndpoint.GetToDelete();

            foreach (var toDelete in itemsToDelete)
            {
                try
                {
                    if (toDelete.Item1.DeletedAt == null)
                    {
                        if (toDelete.ItemTodelete.Type == nameof(ListAggregator))
                        {
                            if (int.TryParse(toDelete.ItemTodelete.Id, out var id))

                                await _toDeleteEndpoint.DeleteListAggrAsync2(id);
                        }
                        else if (toDelete.ItemTodelete.Type == nameof(List))
                        {
                            //TODO: Implement List deletion
                        }

                    }


                    await _toDeleteEndpoint.Deleted(toDelete.id);

                }
                catch (System.Exception ex)
                {
                    // Log the exception or handle it as needed
                    Console.WriteLine($"Error processing ToDelete item with ID {toDelete.id}: {ex.Message}");
                }
            }


            await Task.Delay(1000);
        }

    }
}
