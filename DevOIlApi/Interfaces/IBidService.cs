using DevOIlApi.DTOs;
using DevOIlApi.Models;

namespace DevOIlApi.Interfaces
{
    public interface IBidService
    {
        Task CreateBidAsync(Bid bid);
        Task<IEnumerable<GetBidRequest>> GetUnprocessedBidsAsync();
        Task<IEnumerable<GetBidRequest>> GetProcessedBidsAsync();
        Task<IEnumerable<GetBidRequest>> GetAllBidsAsync();
        Task ToggleBidStatusAsync(int bidId);
        Task<GetBidRequest> GetBidByIdAsync(int id);
        Task<bool> DeleteBidAsync(int id);
        Task<IEnumerable<GetBidRequest>> GetBidsByClientIdAsync(int clientId);
    }
}
