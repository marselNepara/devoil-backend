using DevOIlApi.DTOs;
using DevOIlApi.Interfaces;
using DevOIlApi.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DevOIlApi.Controllers
{
    /// <summary>
    /// Контроллер для управления заявками клиентов.
    /// Поддерживает: создание, просмотр, фильтрацию, изменение статуса, удаление.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BidsController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly IBidService _bidService;

        public BidsController(
            IClientService clientService,
            IBidService bidService)
        {
            _clientService = clientService;
            _bidService = bidService;;
        }

        /// <summary>
        /// Получить все заявки (отсортированы по дате, новые сверху)
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<GetBidRequest>>> GetAllBids()
        {
            try
            {
                var bids = await _bidService.GetAllBidsAsync();
                return Ok(bids);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Ошибка при получении списка заявок",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Получить только необработанные заявки (IsProcessedByAdmin == false)
        /// </summary>
        [HttpGet("unprocessed")]
        public async Task<ActionResult<IEnumerable<GetBidRequest>>> GetUnprocessedBids()
        {
            try
            {
                var bids = await _bidService.GetUnprocessedBidsAsync();
                return Ok(bids);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Ошибка при получении необработанных заявок",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Получить только обработанные заявки (IsProcessedByAdmin == true)
        /// </summary>
        [HttpGet("processed")]
        public async Task<ActionResult<IEnumerable<GetBidRequest>>> GetProcessedBids()
        {
            try
            {
                var bids = await _bidService.GetProcessedBidsAsync();
                return Ok(bids);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Ошибка при получении обработанных заявок",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Получить заявку по ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<GetBidRequest>> GetBidById(int id)
        {
            try
            {
                var bid = await _bidService.GetBidByIdAsync(id);
                if (bid == null)
                {
                    return NotFound(new { message = $"Заявка с ID {id} не найдена" });
                }

                return Ok(bid);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Ошибка при получении заявки",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Создать новую заявку (с консультацией или на поставку)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateBid([FromBody] CreateBidRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Некорректные данные формы",
                    errors = ModelState
                });
            }
            try
            {
                var client = await _clientService.GetOrCreateClientAsync(
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.Phone
                );

                var bid = new Bid
                {
                    Comment = request.Comment ?? "",
                    IsProcessedByAdmin = false,
                    Id_Client = client.Id,
                    Date_of_bid = DateTime.UtcNow
                };

                await _bidService.CreateBidAsync(bid);

                return Ok(new
                {
                    message = "Заявка успешно отправлена",
                    bidId = bid.Id,
                    clientId = client.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Произошла ошибка при сохранении заявки",
                    error = ex.Message
                });
            }
        }

        [HttpGet("by-client/{clientId}")]
        public async Task<ActionResult<IEnumerable<GetBidRequest>>> GetBidsByClientId(int clientId)
        {
            if (clientId <= 0)
                return BadRequest(new { message = "Некорректный ID клиента" });

            try
            {
                var bids = await _bidService.GetBidsByClientIdAsync(clientId);
                return Ok(bids);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка", error = ex.Message });
            }
        }
        /// <summary>
        /// Переключить статус заявки: IsProcessedByAdmin = !IsProcessedByAdmin
        /// </summary>
        [HttpPut("{id}/toggle")]
        public async Task<IActionResult> ToggleBidStatus(int id)
        {
            try
            {
                await _bidService.ToggleBidStatusAsync(id);
                return Ok(new { success = true, message = "Статус обновлён" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "Заявка не найдена" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ошибка сервера",
                    error = ex.Message
                });
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBid(int id)
        {
            try
            {
                var success = await _bidService.DeleteBidAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Заявка не найдена" });
                }

                return Ok(new { message = "Заявка успешно удалена" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Ошибка при удалении заявки",
                    error = ex.Message
                });
            }
        }
    }
}