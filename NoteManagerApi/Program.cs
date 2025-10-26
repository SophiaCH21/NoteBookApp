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
        if (!string.IsNullOrEmpty(connStr))
        {
            var maskedConnStr = connStr.Length > 50 ? connStr.Substring(0, 50) + "..." : connStr;
            logger.LogInformation("Connection string начинается с: {ConnectionStart}", maskedConnStr);
            
            if (connStr.Contains("postgresql://") || connStr.Contains("postgres://"))
            {
                logger.LogInformation("Используется PostgreSQL провайдер");
            }
            else
            {
                logger.LogInformation("Используется SQL Server провайдер");
            }
        }
        else
        {
            logger.LogError("Connection string пуст или не найден!");
        }

        // Проверяем подключение к БД
        logger.LogInformation("Проверяем подключение к базе данных...");
        if (db.Database.CanConnect())
        {
            logger.LogInformation("✅ База данных доступна! Применяем миграции...");
            db.Database.Migrate();
            logger.LogInformation("✅ Миграции успешно применены.");
        }
        else
        {
            logger.LogError("❌ База данных НЕДОСТУПНА. Проверьте connection string и доступность БД.");
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

