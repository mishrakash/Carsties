using System.Data.SqlTypes;
using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace SearchService;

public class AuctionDeletedConsumer : IConsumer<AuctionDeleted>
{
    public async Task Consume(ConsumeContext<AuctionDeleted> context)
    {
        Console.WriteLine($"Received AuctionDeleted event for auction ID: {context.Message.Id}");
        var result = await DB.DeleteAsync<Item>(context.Message.Id);

        if(!result.IsAcknowledged)
        {
            Console.WriteLine($"Failed to delete item with ID: {context.Message.Id}");
        }   
    }
}   