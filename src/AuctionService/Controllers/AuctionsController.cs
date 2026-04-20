using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly AuctionDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;

    public AuctionsController(AuctionDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
    }

    // Controller actions for creating, updating, retrieving auctions would go here
    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
    {
        var query = _context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

        if (!string.IsNullOrEmpty(date))
        {
            
                query = query.Where(a => a.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime())>0);
        }

        return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
    {
        var auction = await _context.Auctions.Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (auction == null) return NotFound();

        return Ok(_mapper.Map<AuctionDto>(auction));
    }    

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto createAuctionDto)
    {
        var auction = _mapper.Map<Auction>(createAuctionDto);   
        //TODO: add current user as seller
        auction.Seller = User.Identity.Name;
        _context.Auctions.Add(auction);
        var result = await _context.SaveChangesAsync()>0; 
        var newauction = _mapper.Map<AuctionDto>(auction);
        await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newauction));
        if(!result) return BadRequest("Failed to create auction");
        return CreatedAtAction(nameof(GetAuctionById), new { id = auction.Id }, newauction);
    }   

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
    {
        var auction = await _context.Auctions
        .Include(x => x.Item)
        .FirstOrDefaultAsync(x => x.Id == id);

        if (auction == null) return NotFound();

        if(auction.Seller != User.Identity.Name) return Forbid(); // Ensure only the seller can update the auction  

        //TODO: check seller == username

        _mapper.Map(updateAuctionDto, auction);
        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;  
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage; 

        await _publishEndpoint.Publish(_mapper.Map<AuctionUpdated>(auction));
        var result = await _context.SaveChangesAsync()>0;
        if (!result) return BadRequest("Failed to update auction");

        return Ok();
    }    

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var auction = await _context.Auctions.FindAsync(id);

        if (auction == null) return NotFound();

        if(auction.Seller != User.Identity.Name) return Forbid(); // Ensure only the seller can delete the auction

        //TODO: check seller == username

        _context.Auctions.Remove(auction);
        await _publishEndpoint.Publish<AuctionDeleted>(new {Id = auction.Id.ToString()});
        var result = await _context.SaveChangesAsync()>0;
        if (!result) return BadRequest("Failed to delete auction");

        return Ok();
    }
}
