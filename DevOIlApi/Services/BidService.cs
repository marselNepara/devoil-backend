using DevOIlApi.DBConnection;
using DevOIlApi.DTOs;
using DevOIlApi.Interfaces;
using DevOIlApi.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevOIlApi.Services
{
    public class BidService : IBidService
    {
        private readonly DataContext _context;

        public BidService(DataContext context)
        {
            _context = context;
        }

        public async Task CreateBidAsync(Bid bid)
        {
            _context.Bids.Add(bid);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<GetBidRequest>> GetUnprocessedBidsAsync()
        {
            return await MapQueryToDto(
                _context.Bids.Where(b => b.IsProcessedByAdmin == false)
            ).ToListAsync();
        }

        public async Task<IEnumerable<GetBidRequest>> GetProcessedBidsAsync()
        {
            return await MapQueryToDto(
                _context.Bids.Where(b => b.IsProcessedByAdmin == true)
            ).ToListAsync();
        }

        public async Task<IEnumerable<GetBidRequest>> GetAllBidsAsync()
        {
            return await MapQueryToDto(_context.Bids).OrderByDescending(b => b.Date_of_Bid).ToListAsync();
        }

        public async Task<GetBidRequest> GetBidByIdAsync(int id)
        {
            var bid = await MapQueryToDto(_context.Bids).FirstOrDefaultAsync(b => b.Id == id);
            return bid;
        }

        public async Task<bool> DeleteBidAsync(int id)
        {
            var bid = await _context.Bids.FindAsync(id);
            if (bid == null) return false;

            _context.Bids.Remove(bid);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ToggleBidStatusAsync(int id)
        {
            var bid = await _context.Bids.FindAsync(id);
            if (bid == null)
                throw new KeyNotFoundException($"Заявка с ID {id} не найдена.");

            bid.IsProcessedByAdmin = !bid.IsProcessedByAdmin;
            await _context.SaveChangesAsync();
        }

        private IQueryable<GetBidRequest> MapQueryToDto(IQueryable<Bid> query)
        {
            return query
                .Include(b => b.Client)
                .Select(b => new GetBidRequest
                {
                    Id = b.Id,
                    Comment = b.Comment,
                    IsProcessedByAdmin = b.IsProcessedByAdmin,
                    ClientFirstName = AesEncryption.Decrypt(b.Client.First_Name),
                    ClientLastName = AesEncryption.Decrypt(b.Client.Last_Name),
                    ClientEmail = AesEncryption.Decrypt(b.Client.Email),
                    ClientPhone = AesEncryption.Decrypt(b.Client.Phone_Number),
                    Date_of_Bid = b.Date_of_bid
                });
        }

        public async Task<IEnumerable<GetBidRequest>> GetBidsByClientIdAsync(int clientId)
        {
            return await _context.Bids
                .Where(b => b.Id_Client == clientId)
                .Include(b => b.Client)
                .Select(b => new GetBidRequest
                {
                    Id = b.Id,
                    Comment = b.Comment,
                    IsProcessedByAdmin = b.IsProcessedByAdmin,
                    ClientFirstName = AesEncryption.Decrypt(b.Client.First_Name),
                    ClientLastName = AesEncryption.Decrypt(b.Client.Last_Name),
                    ClientEmail = AesEncryption.Decrypt(b.Client.Email),
                    ClientPhone = AesEncryption.Decrypt(b.Client.Phone_Number),
                    Date_of_Bid = b.Date_of_bid
                })
                .OrderByDescending(b => b.Date_of_Bid)
                .ToListAsync();
        }
    }
}