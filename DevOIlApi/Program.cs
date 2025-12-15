    using DevOIlApi.DBConnection;
    using DevOIlApi.Interfaces;
    using DevOIlApi.Services;
    using Microsoft.EntityFrameworkCore;

    var builder = WebApplication.CreateBuilder(args);

    // Загружаем переменные окружения из .env через Docker
    builder.Configuration.AddEnvironmentVariables();

    // Загружаем AES ключи
    AesEncryption.LoadConfiguration(builder.Configuration);

    // Контроллеры + JSON
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

    // PostgreSQL
    builder.Services.AddDbContext<DataContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Сервисы
    builder.Services.AddScoped<IClientService, ClientService>();
    builder.Services.AddScoped<IBidService, BidService>();
    builder.Services.AddHttpClient();

    // CORS
    //builder.Services.AddCors(options =>
    //{
    //    options.AddPolicy("AllowFrontend", policy =>
    //    {
    //        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
    //              .AllowAnyMethod()
    //              .AllowAnyHeader();
    //    });
    //});

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // 🔹 Автоприменение миграций
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        db.Database.Migrate();
    }

    // CORS
    app.UseCors("AllowFrontend");

    // Swagger доступен всегда
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
