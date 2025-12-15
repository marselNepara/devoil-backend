// Interfaces/IClientService.cs
using DevOIlApi.DTOs;
using DevOIlApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevOIlApi.Interfaces
{
    public interface IClientService
    {
        Task<Client> GetOrCreateClientAsync(string firstName, string lastName, string email, string phone);
        Task<Client?> FindClientByEmailOrPhoneAsync(string email, string phone);

        Task<ClientDto> GetClientByIdAsync(int id);

        Task<IEnumerable<ClientDto>> GetAllClientsAsync();
        Task<IEnumerable<ClientDto>> SearchClientsAsync(string query);
    }
}