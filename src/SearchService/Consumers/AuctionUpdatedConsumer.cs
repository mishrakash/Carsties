using System.Data.SqlTypes;
using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace SearchService;

public class AuctionUpdatedConsumer : IConsumer<AuctionUpdated>
{
    private readonly IMapper _mapper;
    public AuctionUpdatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }
    

    public async Task Consume(ConsumeContext<AuctionUpdated> context)
    {
        Console.WriteLine($"Received AuctionUpdated event for auction ID: {context.Message.Id}");
        var item = _mapper.Map<Item>(context.Message);
        var result = await DB.Update<Item>()
        .Match(i => i.ID == context.Message.Id)
        .ModifyOnly(x => new
        {
            x.Color,
            x.Make,
            x.Model,
            x.Year,
            x.Mileage
        }, item)
        .ExecuteAsync();

        if(!result.IsAcknowledged)
        {
            Console.WriteLine($"Failed to update item with ID: {context.Message.Id}");
        }
    }
}   