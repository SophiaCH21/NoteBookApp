using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NoteManagerApi.Data;
using NoteManagerApi.Helpers;
using NoteManagerApi.Repositories;
using NoteManagerApi.Services;
using System.Text;
using Microsoft.OpenApi.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

//БД + Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Поддержка SQL Server и PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString) && 
        (connectionString.Contains("postgresql://") || connectionString.Contains("postgres://") || connectionString.Contains("PostgreSQL")))
    {
        // PostgreSQL для Render
        options.UseNpgsql(connectionString);
    }
    else
    {
        // SQL Server для локальной разработки
        options.UseSqlServer(connectionString);
    }
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAutoMapper(typeof(MappingProfile));

// Регистрация сервисов
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<INoteService, NoteService>();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Notes API", Version = "v1" });
    
    // JWT Authentication для Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtBearer";
    options.DefaultChallengeScheme = "JwtBearer";
})
.AddJwtBearer("JwtBearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

// builder.Services.AddCors(options => {
//      options.AddPolicy("AllowFrontend", builder => {
//        builder.WithOrigins("http://localhost:5173", "https://localhost:5173")
//               .AllowAnyMethod()
//               .AllowAnyHeader()
//               .AllowCredentials();
//      });
// });

// Получаем список разрешенных origin'ов из конфигурации или используем дефолтные
var allowedOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:5173", "https://localhost:5173" };

builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtTokenGenerator>();

var app = builder.Build();

// Детальное логирование и применение миграций
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Логируем информацию о подключении
        var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
        // Детальная диагностика подключения к PostgreSQL (поддержка URL и key=value)
        if (!string.IsNullOrEmpty(connStr) && (connStr.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || connStr.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var uri = new Uri(connStr);
                var userInfo = Uri.UnescapeDataString(uri.UserInfo ?? "");
                var creds = userInfo.Split(':', 2);
                var user = creds.ElementAtOrDefault(0);
                var pass = creds.ElementAtOrDefault(1);
                var host = uri.Host;
                var port = uri.IsDefaultPort ? 5432 : uri.Port;
                var db   = uri.AbsolutePath.TrimStart('/');

                logger.LogInformation("🔍 DB host: {Host}, port: {Port}, db: {Db}, user: {User}", host, port, db, user);

                // DNS
                try {
                    var entry = Dns.GetHostEntry(host);
                    logger.LogInformation("✅ DNS resolve {Host} OK: {IPs}", host, string.Join(", ", entry.AddressList.Select(a => a.ToString())));
                } catch (Exception ex) {
                    logger.LogError(ex, "❌ DNS resolve FAILED for host {Host}", host);
                }

                // TCP
                try {
                    using var tcp = new TcpClient();
                    tcp.ReceiveTimeout = 10000; tcp.SendTimeout = 10000;
                    tcp.Connect(host, port);
                    logger.LogInformation("✅ TCP connect to {Host}:{Port} OK", host, port);
                } catch (Exception ex) {
                    logger.LogError(ex, "❌ TCP connect to {Host}:{Port} FAILED", host, port);
                }

                // Прямая проверка Npgsql
                try {
                    var urlForOpen = connStr.Contains("sslmode=", StringComparison.OrdinalIgnoreCase)
                        ? connStr
                        : (connStr.Contains("?") ? connStr + "&sslmode=require" : connStr + "?sslmode=require");

                    using var npg = new Npgsql.NpgsqlConnection(urlForOpen);
                    npg.Open();
                    logger.LogInformation("✅ Npgsql connection open OK");
                    npg.Close();
                } catch (Exception ex) {
                    logger.LogError(ex, "❌ Npgsql connection FAILED: {Message}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ URL parsing failed for PostgreSQL connection string");
            }
        }
        else
        {
            // Старый путь: key=value формат
            try
            {
                var csb = new Npgsql.NpgsqlConnectionStringBuilder(connStr)
                {
                    Timeout = 10,
                    CommandTimeout = 10,
                    SslMode = Npgsql.SslMode.Require,
                    TrustServerCertificate = true
                };

                logger.LogInformation("🔍 DB host: {Host}, port: {Port}, db: {Db}, user: {User}", csb.Host, csb.Port, csb.Database, csb.Username);

                using var tcp = new TcpClient();
                tcp.ReceiveTimeout = 10000; tcp.SendTimeout = 10000;
                tcp.Connect(csb.Host, csb.Port);
                logger.LogInformation("✅ TCP connect to {Host}:{Port} OK", csb.Host, csb.Port);

                using var npg = new Npgsql.NpgsqlConnection(csb.ConnectionString);
                npg.Open();
                logger.LogInformation("✅ Npgsql connection open OK");
                npg.Close();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ key=value parsing or connection failed");
            }
        }

        // Проверяем подключение к БД через EF Core
        logger.LogInformation("Проверяем подключение к базе данных через EF Core...");
        if (db.Database.CanConnect())
        {
            logger.LogInformation("✅ База данных доступна! Применяем миграции...");
            db.Database.Migrate();
            logger.LogInformation("✅ Миграции успешно применены.");
        }
        else
        {
            logger.LogError("❌ База данных НЕДОСТУПНА через EF Core. Проверьте результаты диагностики выше.");
        }
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException != null && 
        (ex.InnerException.Message.Contains("already exists") || ex.InnerException.Message.Contains("duplicate")))
    {
        logger.LogInformation("ℹ️ Таблицы уже существуют. Миграции не требуются.");
    }
    catch (Npgsql.NpgsqlException ex)
    {
        logger.LogError(ex, "❌ Ошибка PostgreSQL: {ErrorMessage}. Код: {ErrorCode}", ex.Message, ex.ErrorCode);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Общая ошибка при работе с БД: {ErrorMessage}", ex.Message);
        logger.LogWarning("⚠️ Продолжаем запуск приложения без БД...");
    }
}

// Swagger доступен для всех сред (включая Production)
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notes API v1"));

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

