using DevOilBot_ForAdmins.DTOs;
using System.Net.Http.Json;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DevOilBot_ForAdmins
{
    public class Host
    {
        private readonly TelegramBotClient _bot;
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly long[] _adminIds;

        private const int PageSize = 5;

        // Для отслеживания состояния "ожидания поиска"
        private readonly HashSet<long> _awaitingSearchQuery = new();

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public Host(string token, long[] adminIds, string apiUrl)
        {
            _bot = new TelegramBotClient(token);
            _httpClient = new HttpClient();
            _adminIds = adminIds ?? throw new ArgumentNullException(nameof(adminIds));
            _apiUrl = apiUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(apiUrl));
        }

        public void StartBot()
        {
            Console.WriteLine("🤖 Админ-бот DevOil запущен...");
            Console.WriteLine($"🔐 Доступ разрешён для {_adminIds.Length} администраторов.");

            _bot.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync
            );
        }

        private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message && update.Message?.Text != null)
                {
                    var message = update.Message;
                    var chatId = message.Chat.Id;
                    var text = message.Text.Trim().ToLower();

                    if (!_adminIds.Contains(chatId))
                    {
                        await _bot.SendMessage(chatId, "❌ Доступ к боту ограничен.");
                        return;
                    }

                    // Если бот ожидает запрос для поиска
                    if (_awaitingSearchQuery.Contains(chatId))
                    {
                        _awaitingSearchQuery.Remove(chatId);

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            await SearchClients(chatId, text, 1);
                        }
                        else
                        {
                            await _bot.SendMessage(chatId, "❌ Запрос не может быть пустым.");
                            await ShowMainMenu(chatId);
                        }

                        return;
                    }

                    switch (text)
                    {
                        case "/start":
                        case "/menu":
                            await ShowMainMenu(chatId);
                            break;
                        case "/all":
                            await ShowBidsList(chatId, "all", 1);
                            return;
                        case "/unprocessed":
                            await ShowBidsList(chatId, "unprocessed", 1);
                            return;
                        case "/processed":
                            await ShowBidsList(chatId, "processed", 1);
                            return;
                        default:
                            await _bot.SendMessage(chatId, "Используйте /menu для возврата в главное меню.");
                            break;
                    }
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    var callback = update.CallbackQuery;
                    var chatId = callback.Message.Chat.Id;
                    var messageId = callback.Message.MessageId;
                    var data = callback.Data;

                    if (!_adminIds.Contains(chatId))
                    {
                        await _bot.AnswerCallbackQuery(callback.Id, "У вас нет прав.");
                        return;
                    }

                    if (data == "main_menu")
                    {
                        await ShowMainMenu(chatId, messageId);
                    }
                    else if (data.StartsWith("list_"))
                    {
                        var parts = data.Split('_');
                        var filter = parts[1];
                        var page = int.Parse(parts[2]);
                        await ShowBidsList(chatId, filter, page, messageId);
                    }
                    else if (data.StartsWith("view_bid_"))
                    {
                        var bidId = int.Parse(data["view_bid_".Length..]);
                        await ShowBidDetails(chatId, bidId, messageId);
                    }
                    else if (data.StartsWith("confirm_delete_bid_"))
                    {
                        var bidId = int.Parse(data["confirm_delete_bid_".Length..]);
                        await ShowDeleteConfirmation(chatId, bidId, messageId);
                    }
                    else if (data.StartsWith("delete_bid_"))
                    {
                        var bidId = int.Parse(data["delete_bid_".Length..]);
                        await DeleteBid(chatId, bidId, messageId);
                    }
                    else if (data.StartsWith("toggle_bid_"))
                    {
                        var bidId = int.Parse(data["toggle_bid_".Length..]);
                        await ToggleBidStatus(chatId, bidId, messageId);
                    }
                    else if (data.StartsWith("clients_page_"))
                    {
                        var page = int.Parse(data["clients_page_".Length..]);
                        await ShowAllClients(chatId, page, messageId);
                    }
                    else if (data.StartsWith("client_") && !data.StartsWith("client_bids_"))
                    {
                        var clientId = int.Parse(data["client_".Length..]);
                        await ShowClientDetails(chatId, clientId, messageId);
                    }
                    else if (data.StartsWith("client_bids_"))
                    {
                        var parts = data["client_bids_".Length..].Split('_');
                        var clientId = int.Parse(parts[0]);
                        var page = parts.Length > 1 ? int.Parse(parts[1]) : 1;
                        await ShowClientBids(chatId, clientId, page, messageId);
                    }
                    else if (data == "search_clients")
                    {
                        _awaitingSearchQuery.Add(chatId);
                        await _bot.EditMessageText(
                            chatId: chatId,
                            messageId: messageId,
                            text: "🔍 Введите имя, фамилию, email или телефон:",
                            replyMarkup: new InlineKeyboardMarkup(
                                InlineKeyboardButton.WithCallbackData("⬅️ Назад", "clients_page_1")
                            )
                        );
                        return;
                    }
                    else if (data.StartsWith("search_page_"))
                    {
                        var parts = data.Split('_');
                        var page = int.Parse(parts[2]);
                        var query = parts.Skip(3).FirstOrDefault() ?? "";
                        await SearchClients(chatId, query, page, messageId);
                    }

                    await _bot.AnswerCallbackQuery(callback.Id);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
                Console.ResetColor();
                await _bot.SendMessage(update.Message?.Chat.Id ?? 0, "Произошла ошибка.");
            }
        }

        // 🏠 Главное меню
        private async Task ShowMainMenu(long chatId, int? messageId = null)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("📬 Заявки", "list_all_1")
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData("👥 Клиенты", "clients_page_1"),
                }
            });

            var text = $"""
                👋 Привет, администратор!

                Добро пожаловать в <b>DevOil Admin Panel</b>.

                Выберите действие:
                """;

            if (messageId.HasValue)
            {
                await _bot.EditMessageText(
                    chatId: chatId,
                    messageId: messageId.Value,
                    text: text,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard
                );
            }
            else
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: text,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard
                );
            }
        }

        // 👥 Показать всех клиентов (с пагинацией)
        private async Task ShowAllClients(long chatId, int page, int? messageId = null)
        {
            var response = await _httpClient.GetAsync($"{_apiUrl}/clients");
            var clients = await response.Content.ReadFromJsonAsync<List<ClientDto>>(_jsonOptions);

            if (clients == null || !clients.Any())
            {
                await EditWithMenu(chatId, messageId ?? 0, "📭 Нет зарегистрированных клиентов.");
                return;
            }

            var totalCount = clients.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            page = Math.Clamp(page, 1, totalPages);

            var pagedClients = clients
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var keyboard = new List<List<InlineKeyboardButton>>();

            foreach (var client in pagedClients)
            {
                var buttonText = $"#{client.Id} {client.LastName} {client.FirstName}";
                var button = InlineKeyboardButton.WithCallbackData(buttonText, $"client_{client.Id}");
                keyboard.Add(new List<InlineKeyboardButton> { button });
            }

            // Пагинация
            var paginationRow = new List<InlineKeyboardButton>();
            if (page > 1)
                paginationRow.Add(InlineKeyboardButton.WithCallbackData("◀️", $"clients_page_{page - 1}"));
            if (page < totalPages)
                paginationRow.Add(InlineKeyboardButton.WithCallbackData("▶️", $"clients_page_{page + 1}"));

            if (paginationRow.Any())
                keyboard.Add(paginationRow);

            // Поиск
            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🔍 Поиск клиента", "search_clients")
            });

            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", "main_menu")
            });

            var replyMarkup = new InlineKeyboardMarkup(keyboard);
            var textMsg = $"<b>👥 Все клиенты</b> ({page}/{totalPages})\n\nВыберите клиента:";

            if (messageId.HasValue)
                await _bot.EditMessageText(chatId, messageId.Value, textMsg, parseMode: ParseMode.Html, replyMarkup: replyMarkup);
            else
                await _bot.SendMessage(chatId, textMsg, parseMode: ParseMode.Html, replyMarkup: replyMarkup);
        }

        // 🔍 Поиск клиентов
        private async Task SearchClients(long chatId, string query, int page = 1, int? messageId = null)
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var response = await _httpClient.GetAsync($"{_apiUrl}/clients/search?q={encodedQuery}");
            var clients = await response.Content.ReadFromJsonAsync<List<ClientDto>>(_jsonOptions);

            if (clients == null || !clients.Any())
            {
                await EditWithMenu(chatId, messageId ?? 0, $"❌ Ничего не найдено по запросу: <i>{query}</i>");
                return;
            }

            var totalCount = clients.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            page = Math.Clamp(page, 1, totalPages);

            var pagedClients = clients
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var keyboard = new List<List<InlineKeyboardButton>>();
            foreach (var client in pagedClients)
            {
                var buttonText = $"#{client.Id} {client.LastName} {client.FirstName}";
                var button = InlineKeyboardButton.WithCallbackData(buttonText, $"client_{client.Id}");
                keyboard.Add(new List<InlineKeyboardButton> { button });
            }

            var paginationRow = new List<InlineKeyboardButton>();
            if (page > 1)
                paginationRow.Add(InlineKeyboardButton.WithCallbackData("◀️", $"search_page_{page - 1}_{query}"));
            if (page < totalPages)
                paginationRow.Add(InlineKeyboardButton.WithCallbackData("▶️", $"search_page_{page + 1}_{query}"));

            if (paginationRow.Any())
                keyboard.Add(paginationRow);

            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", "clients_page_1")
            });

            var replyMarkup = new InlineKeyboardMarkup(keyboard);
            var textMsg = $"<b>🔍 Результаты поиска</b>: <i>{query}</i> ({page}/{totalPages})\n\nВыберите клиента:";

            if (messageId.HasValue)
                await _bot.EditMessageText(chatId, messageId.Value, textMsg, parseMode: ParseMode.Html, replyMarkup: replyMarkup);
            else
                await _bot.SendMessage(chatId, textMsg, parseMode: ParseMode.Html, replyMarkup: replyMarkup);
        }

        // 👤 Детали клиента
        private async Task ShowClientDetails(long chatId, int clientId, int messageId)
        {
            var response = await _httpClient.GetAsync($"{_apiUrl}/clients/{clientId}");
            if (!response.IsSuccessStatusCode)
            {
                await EditWithMenu(chatId, messageId, "❌ Клиент не найден.");
                return;
            }

            var client = await response.Content.ReadFromJsonAsync<ClientDto>(_jsonOptions);

            var details = $"""
                <b>👤 Клиент #{client.Id}</b>

                <b>ФИО:</b> {client.LastName} {client.FirstName}
                <b>Email:</b> {client.Email}
                <b>Телефон:</b> {client.Phone}
                <b>Дата регистрации:</b> {client.Date_Of_Registration:dd.MM.yyyy}
                <b>Заявок подано:</b> <b>{client.TotalBids}</b>
                """;

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new [] { InlineKeyboardButton.WithCallbackData("📋 Заявки клиента", $"client_bids_{clientId}_1") },
                new [] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "clients_page_1") }
            });

            await _bot.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: details,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );
        }

        // 📋 Заявки клиента
        private async Task ShowClientBids(long chatId, int clientId, int page, int? messageId = null)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiUrl}/bids/by-client/{clientId}");
                if (!response.IsSuccessStatusCode)
                {
                    await EditWithMenu(chatId, messageId ?? 0, $"❌ Не удалось загрузить заявки клиента.");
                    return;
                }

                var bids = await response.Content.ReadFromJsonAsync<List<BidDto>>(_jsonOptions);

                if (bids == null || !bids.Any())
                {
                    // Кнопка "Назад к клиенту"
                    var replyMarkup = new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"client_{clientId}")
                    );

                    var text = $"📭 У клиента #{clientId} пока нет заявок.";

                    if (messageId.HasValue)
                        await _bot.EditMessageText(chatId, messageId.Value, text, replyMarkup: replyMarkup);
                    else
                        await _bot.SendMessage(chatId, text, replyMarkup: replyMarkup);

                    return;
                }

                // Пагинация
                var totalCount = bids.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
                page = Math.Clamp(page, 1, totalPages);

                var pagedBids = bids
                    .OrderByDescending(b => b.Date_of_Bid)
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Собираем кнопки построчно
                var buttons = new List<List<InlineKeyboardButton>>();

                foreach (var bid in pagedBids)
                {
                    var statusEmoji = bid.IsProcessedByAdmin ? "🟢" : "🟡";
                    var button = InlineKeyboardButton.WithCallbackData($"{statusEmoji} #{bid.Id}", $"view_bid_{bid.Id}");
                    buttons.Add(new List<InlineKeyboardButton> { button });
                }

                // Пагинация
                var paginationRow = new List<InlineKeyboardButton>();
                if (page > 1)
                    paginationRow.Add(InlineKeyboardButton.WithCallbackData("◀️", $"client_bids_{clientId}_{page - 1}"));
                if (page < totalPages)
                    paginationRow.Add(InlineKeyboardButton.WithCallbackData("▶️", $"client_bids_{clientId}_{page + 1}"));

                if (paginationRow.Any())
                    buttons.Add(paginationRow);

                // Кнопка "Назад"
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"client_{clientId}")
        });

                // Создаём финальную разметку
                var replyMarkupFinal = new InlineKeyboardMarkup(buttons);

                var textMsg = $"<b>📋 Заявки клиента #{clientId}</b> ({page}/{totalPages})";

                if (messageId.HasValue)
                    await _bot.EditMessageText(
                        chatId: chatId,
                        messageId: messageId.Value,
                        text: textMsg,
                        parseMode: ParseMode.Html,
                        replyMarkup: replyMarkupFinal
                    );
                else
                    await _bot.SendMessage(
                        chatId: chatId,
                        text: textMsg,
                        parseMode: ParseMode.Html,
                        replyMarkup: replyMarkupFinal
                    );
            }
            catch (Exception ex)
            {
                await EditWithMenu(chatId, messageId ?? 0, $"❌ Ошибка при загрузке заявок: {ex.Message}");
            }
        }

        // 📄 Список заявок
        private async Task ShowBidsList(long chatId, string filter, int page, int? messageId = null)
        {
            string endpoint = filter switch
            {
                "unprocessed" => "/unprocessed",
                "processed" => "/processed",
                _ => "/all"
            };

            var response = await _httpClient.GetAsync($"{_apiUrl}/bids{endpoint}");
            var bids = await response.Content.ReadFromJsonAsync<List<BidDto>>(_jsonOptions);

            if (bids == null || !bids.Any())
            {
                var text = filter switch
                {
                    "unprocessed" => "📭 Нет новых заявок.",
                    "processed" => "📭 Нет обработанных заявок.",
                    _ => "📭 Заявок пока нет."
                };

                var replyMarkup = new InlineKeyboardMarkup(new[]
                {
                    new [] { InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu") }
                });

                if (messageId.HasValue)
                    await _bot.EditMessageText(chatId, messageId.Value, text, replyMarkup: replyMarkup);
                else
                    await _bot.SendMessage(chatId, text, replyMarkup: replyMarkup);

                return;
            }

            var totalCount = bids.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            page = Math.Clamp(page, 1, totalPages);

            var pagedBids = bids
                .OrderByDescending(b => b.Date_of_Bid)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var keyboard = new List<List<InlineKeyboardButton>>();

            foreach (var bid in pagedBids)
            {
                var statusEmoji = bid.IsProcessedByAdmin ? "🟢" : "🟡";
                var buttonText = $"{statusEmoji} #{bid.Id}";
                var button = InlineKeyboardButton.WithCallbackData(buttonText, $"view_bid_{bid.Id}");
                keyboard.Add(new List<InlineKeyboardButton> { button });
            }

            var paginationRow = new List<InlineKeyboardButton>();
            if (page > 1)
                paginationRow.Add(InlineKeyboardButton.WithCallbackData("◀️", $"list_{filter}_{page - 1}"));
            if (page < totalPages)
                paginationRow.Add(InlineKeyboardButton.WithCallbackData("▶️", $"list_{filter}_{page + 1}"));

            if (paginationRow.Any())
                keyboard.Add(paginationRow);

            if (filter != "all")
            {
                keyboard.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("📬 Все заявки", "list_all_1")
                });
            }

            if (filter == "all")
            {
                keyboard.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("🟡 Необработанные", "list_unprocessed_1"),
                    InlineKeyboardButton.WithCallbackData("🟢 Обработанные", "list_processed_1")
                });
            }

            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu")
            });

            var replyMarkupFinal = new InlineKeyboardMarkup(keyboard);

            var filterText = filter switch
            {
                "unprocessed" => "🟡 Необработанные",
                "processed" => "🟢 Обработанные",
                _ => "📬 Все заявки"
            };

            var textMsg = $"<b>{filterText}</b> ({page}/{totalPages})\n\nВыберите заявку:";

            if (messageId.HasValue)
                await _bot.EditMessageText(
                    chatId: chatId,
                    messageId: messageId.Value,
                    text: textMsg,
                    parseMode: ParseMode.Html,
                    replyMarkup: replyMarkupFinal
                );
            else
                await _bot.SendMessage(
                    chatId: chatId,
                    text: textMsg,
                    parseMode: ParseMode.Html,
                    replyMarkup: replyMarkupFinal
                );
        }

        // 🔍 Детали заявки
        private async Task ShowBidDetails(long chatId, int bidId, int messageId)
        {
            var response = await _httpClient.GetAsync($"{_apiUrl}/bids/{bidId}");
            if (!response.IsSuccessStatusCode)
            {
                await EditWithMenu(chatId, messageId, "❌ Заявка не найдена.");
                return;
            }

            var bid = await response.Content.ReadFromJsonAsync<BidDto>(_jsonOptions);
            var statusText = bid.IsProcessedByAdmin ? "🟢 Обработана" : "🟡 В обработке";

            var details = $"""
                <b>📋 Заявка #{bid.Id}</b>

                <b>👤 Клиент:</b> {bid.ClientFirstName} {bid.ClientLastName}
                <b>📧 Email:</b> {bid.ClientEmail}
                <b>📞 Телефон:</b> {bid.ClientPhone}
                <b>💬 Комментарий:</b>
                {bid.Comment}

                <b>📅 Дата подачи:</b> {bid.Date_of_Bid:dd.MM.yyyy HH:mm}
                <b>📊 Статус:</b> {statusText}
                """;

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new [] { InlineKeyboardButton.WithCallbackData("🔄 Изменить статус", $"toggle_bid_{bidId}") },
                new [] { InlineKeyboardButton.WithCallbackData("🗑 Удалить", $"confirm_delete_bid_{bidId}") },
                new []
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад", "back"),
                    InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu")
                }
            });

            await _bot.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: details,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );
        }

        // ⚠️ Подтверждение удаления заявки
        private async Task ShowDeleteConfirmation(long chatId, int bidId, int messageId)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("✅ Да", $"delete_bid_{bidId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Нет", "back")
                },
                new [] { InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu") }
            });

            await _bot.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: $"<b>⚠️ Удалить заявку #{bidId}?</b>\nЭто действие нельзя отменить.",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );
        }

        // 🗑 Удаление заявки
        private async Task DeleteBid(long chatId, int bidId, int messageId)
        {
            var response = await _httpClient.DeleteAsync($"{_apiUrl}/bids/{bidId}");
            if (response.IsSuccessStatusCode)
            {
                await EditWithMenu(chatId, messageId, $"✅ Заявка #{bidId} удалена.");
            }
            else
            {
                await EditWithMenu(chatId, messageId, $"❌ Не удалось удалить заявку.");
            }
        }

        // 🔁 Переключить статус заявки
        private async Task ToggleBidStatus(long chatId, int bidId, int messageId)
        {
            var response = await _httpClient.PutAsync($"{_apiUrl}/bids/{bidId}/toggle", null);
            if (response.IsSuccessStatusCode)
            {
                await ShowBidDetails(chatId, bidId, messageId);
            }
            else
            {
                await EditWithMenu(chatId, messageId, "❌ Ошибка при изменении статуса.");
            }
        }

        // 🛠 Универсальный метод: редактировать + главное меню
        private async Task EditWithMenu(long chatId, int messageId, string text)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new [] { InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu") }
            });

            await _bot.EditMessageText(chatId, messageId, text, replyMarkup: keyboard);
        }

        // ❌ Обработка ошибок
        private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"🚨 Ошибка Telegram: {exception.Message}");
            Console.ResetColor();
            return Task.CompletedTask;
        }
    }
}