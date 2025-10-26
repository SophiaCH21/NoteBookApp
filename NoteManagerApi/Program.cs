using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

// Функция конвертации PostgreSQL URL в key=value формат
static string ToKeyValuePg(string raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return raw;
    if (!(raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
          raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
        return raw;

    var uri = new Uri(raw);
    var userInfo = Uri.UnescapeDataString(uri.UserInfo ?? "");
    var creds = userInfo.Split(':', 2);
    var user = creds.ElementAtOrDefault(0);
    var pass = creds.ElementAtOrDefault(1);
    var host = uri.Host;
    var port = uri.IsDefaultPort ? 5432 : uri.Port;
    var db   = uri.AbsolutePath.TrimStart('/');

    var csb = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = host,
        Port = port,
        Database = db,
        Username = user,
        Password = pass,
        SslMode = Npgsql.SslMode.Require,      // по умолчанию
        Pooling = false,                        // для отладки
        Timeout = 10,
        CommandTimeout = 10
    };

    var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
    foreach (var kv in query)
    {
        var parts = kv.Split('=', 2);
        if (parts.Length != 2) continue;
        var key = parts[0];
        var value = Uri.UnescapeDataString(parts[1]);

        if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase) &&
            Enum.TryParse<Npgsql.SslMode>(value, true, out var mode))
            csb.SslMode = mode;

        if (key.Equals("pooling", StringComparison.OrdinalIgnoreCase) &&
            bool.TryParse(value, out var pooling))
            csb.Pooling = pooling;
    }

    return csb.ConnectionString;
}

// Конвертируем URL в key=value формат для EF Core
var fixedConnectionString = ToKeyValuePg(connectionString);

// Поддержка SQL Server и PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString) && 
        (connectionString.Contains("postgresql://") || connectionString.Contains("postgres://") || connectionString.Contains("PostgreSQL")))
    {
        // PostgreSQL для Render с исправленным connection string
        options.UseNpgsql(fixedConnectionString)
               .EnableDetailedErrors()
               .EnableSensitiveDataLogging()
               .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }
    else
    {
        // SQL Server для локальной разработки
        options.UseSqlServer(connectionString)
               .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
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
        logger.LogInformation("🔍 RAW Connection String: {ConnStr}", connStr ?? "NULL");
        logger.LogInformation("🔧 FIXED Connection String for EF Core: {FixedConnStr}", fixedConnectionString ?? "NULL");
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
                var dbName = uri.AbsolutePath.TrimStart('/');

                logger.LogInformation("🔍 DB host: {Host}, port: {Port}, db: {Db}, user: {User}", host, port, dbName, user);

                // DNS
                try {
                    var entry = Dns.GetHostEntry(host);
                    logger.LogInformation("✅ DNS resolve {Host} OK: {IPs}", host, string.Join(", ", entry.AddressList.Select(a => a.ToString())));
                } catch (Exception ex) {
                    logger.LogError(ex, "❌ DNS resolve FAILED for host {Host}", host);
                }

                // TCP
                if (!string.IsNullOrEmpty(host))
                {
                    try {
                        using var tcp = new TcpClient();
                        tcp.ReceiveTimeout = 10000; tcp.SendTimeout = 10000;
                        tcp.Connect(host, port);
                        logger.LogInformation("✅ TCP connect to {Host}:{Port} OK", host, port);
                    } catch (Exception ex) {
                        logger.LogError(ex, "❌ TCP connect to {Host}:{Port} FAILED", host, port);
                    }
                }
                else
                {
                    logger.LogError("❌ Host is null or empty");
                }

                logger.LogInformation("ℹ️ Пропускаем прямую проверку Npgsql - полагаемся только на EF Core");
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
                    SslMode = Npgsql.SslMode.Require
                };

                logger.LogInformation("🔍 DB host: {Host}, port: {Port}, db: {Db}, user: {User}", csb.Host, csb.Port, csb.Database, csb.Username);

                if (!string.IsNullOrEmpty(csb.Host))
                {
                    using var tcp = new TcpClient();
                    tcp.ReceiveTimeout = 10000; tcp.SendTimeout = 10000;
                    tcp.Connect(csb.Host, csb.Port);
                    logger.LogInformation("✅ TCP connect to {Host}:{Port} OK", csb.Host, csb.Port);
                }

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

        // Принудительно очищаем все кэши Npgsql
        try 
        {
            Npgsql.NpgsqlConnection.ClearAllPools();
            logger.LogInformation("🧹 Очистили все connection pools Npgsql");
        } 
        catch (Exception ex) 
        {
            logger.LogWarning(ex, "⚠️ Не удалось очистить connection pools");
        }

        // Явная проверка подключения с детальными ошибками
        try
        {
            var efConn = db.Database.GetDbConnection();
            var original = efConn.ConnectionString;
            efConn.ConnectionString = fixedConnectionString; // тот же, что в DbContext
            efConn.Open();
            logger.LogInformation("✅ EF raw connection open OK");
            efConn.Close();
            efConn.ConnectionString = original;
        }
        catch (Npgsql.PostgresException ex)
        {
            logger.LogError(ex, "❌ PostgresException: {SqlState} {MessageText}", ex.SqlState, ex.MessageText);
        }
        catch (Npgsql.NpgsqlException ex)
        {
            logger.LogError(ex, "❌ NpgsqlException: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ General connection error: {Message}", ex.Message);
        }

        // Проверяем подключение к БД через EF Core
        logger.LogInformation("Проверяем подключение к базе данных через EF Core...");
        logger.LogInformation("🔍 EF Core Connection String: {EfConnStr}", db.Database.GetConnectionString() ?? "NULL");
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


