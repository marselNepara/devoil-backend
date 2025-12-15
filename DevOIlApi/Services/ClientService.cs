// Services/ClientService.cs
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
    public class ClientService : IClientService
    {
        private readonly DataContext _context;

        public ClientService(DataContext context)
        {
            _context = context;
        }

        public async Task<Client> GetOrCreateClientAsync(string firstName, string lastName, string email, string phone)
        {
            try
            {
                var user = await FindClientByEmailOrPhoneAsync(email, phone);
                if (user != null)
                    return user;

                // Шифруем только если ещё не зашифровано
                var encryptedFirstName = AesEncryption.Encrypt(firstName.Trim());
                var encryptedLastName = AesEncryption.Encrypt(lastName.Trim());
                var encryptedEmail = AesEncryption.Encrypt(email.Trim());
                var encryptedPhone = AesEncryption.Encrypt(phone.Trim());

                user = new Client
                {
                    First_Name = encryptedFirstName,
                    Last_Name = encryptedLastName,
                    Email = encryptedEmail,
                    Phone_Number = encryptedPhone,
                    Date_of_registration = DateTime.UtcNow
                };

                _context.Clients.Add(user);
                await _context.SaveChangesAsync();
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetOrCreateClientAsync: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        public async Task<Client?> FindClientByEmailOrPhoneAsync(string email, string phone)
        {
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
                return null;

            var clients = await _context.Clients.ToListAsync();

            foreach (var client in clients)
            {
                try
                {
                    var decryptedEmail = AesEncryption.Decrypt(client.Email);
                    var decryptedPhone = AesEncryption.Decrypt(client.Phone_Number);

                    if ((!string.IsNullOrEmpty(email) && string.Equals(decryptedEmail, email, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(phone) && string.Equals(decryptedPhone, phone, StringComparison.OrdinalIgnoreCase)))
                    {
                        return client;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }

        // 🔁 Теперь возвращает ClientDto напрямую
        public async Task<ClientDto> GetClientByIdAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return null;

            return await GetClientAsDtoAsync(client);
        }

        public async Task<IEnumerable<ClientDto>> GetAllClientsAsync()
        {
            var clients = await _context.Clients.ToListAsync();

            return clients.Select(async u => await GetClientAsDtoAsync(u))
                         .Select(t => t.Result)
                         .OrderByDescending(u => u.Date_Of_Registration)
                         .ToList();
        }

        public async Task<IEnumerable<ClientDto>> SearchClientsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<ClientDto>();

            query = query.Trim().ToLower();

            var clients = await _context.Clients.ToListAsync();

            var filtered = clients.Where(u =>
            {
                try
                {
                    var decryptedFirstName = AesEncryption.Decrypt(u.First_Name).ToLower();
                    var decryptedLastName = AesEncryption.Decrypt(u.Last_Name).ToLower();
                    var decryptedEmail = AesEncryption.Decrypt(u.Email).ToLower();
                    var decryptedPhone = AesEncryption.Decrypt(u.Phone_Number).ToLower();
                    var fullName = $"{decryptedFirstName} {decryptedLastName}";

                    return fullName.Contains(query) ||
                           decryptedEmail.Contains(query) ||
                           decryptedPhone.Contains(query);
                }
                catch
                {
                    return false;
                }
            });

            return filtered.Select(async u => await GetClientAsDtoAsync(u))
                          .Select(t => t.Result)
                          .OrderByDescending(u => u.Date_Of_Registration)
                          .ToList();
        }

        // 🔐 Единая точка создания DTO с расшифровкой
        private async Task<ClientDto> GetClientAsDtoAsync(Client client)
        {
            if (client == null) return null;

            return new ClientDto
            {
                Id = client.Id,
                FirstName = AesEncryption.Decrypt(client.First_Name),
                LastName = AesEncryption.Decrypt(client.Last_Name),
                Email = AesEncryption.Decrypt(client.Email),
                Phone = AesEncryption.Decrypt(client.Phone_Number),
                Date_Of_Registration = client.Date_of_registration,
                TotalBids = client.ClientBids?.Count ?? 0
            };
        }
    }
}