// Controllers/ClientsController.cs
using DevOIlApi.DTOs;
using DevOIlApi.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevOIlApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        /// <summary>
        /// 🔹 Получить всех пользователей (все поля расшифрованы)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientDto>>> GetAllClients()
        {
            try
            {
                var clients = await _clientService.GetAllClientsAsync();
                return Ok(clients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Не удалось загрузить список клиентов",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 🔹 Получить пользователя по ID (все поля расшифрованы)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientDto>> GetClientById(int id)
        {
            try
            {
                var clientDto = await _clientService.GetClientByIdAsync(id);
                if (clientDto == null)
                    return NotFound(new { message = $"Клиент с ID {id} не найден" });

                return Ok(clientDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Ошибка при получении пользователя",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 🔹 Поиск пользователей по email или телефону (результаты расшифрованы)
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ClientDto>>> SearchClients([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new
                {
                    message = "Параметр 'q' обязателен. Используйте ?q=поиск"
                });

            try
            {
                var clients = await _clientService.SearchClientsAsync(q.Trim());
                return Ok(clients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Ошибка при выполнении поиска",
                    error = ex.Message
                });
            }
        }
    }
}