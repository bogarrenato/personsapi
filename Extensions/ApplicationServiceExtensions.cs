using API.Data;
using API.Helpers;
using API.Interfaces;
using API.Services;
using API.SignalR;
using Microsoft.EntityFrameworkCore;
using personsapi.Attributes;
using personsapi.Interfaces;
using personsapi.Services;
using StackExchange.Redis;

namespace API.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers();
        services.AddDbContext<DataContext>(options =>
              {
                  options.UseSqlite(config.GetConnectionString("DefaultConnection"));
              });

        services.AddCors();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        // services.AddSingleton<ICacheService, RedisCacheService>();
        // services.AddScoped<CacheResultAttribute>();
        // services.AddScoped<InvalidateCacheAttribute>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ILikesRepository, LikesRepository>();
        services.AddSingleton<IConnectionMultiplexer>(c =>
        {
            var configuration = config.GetConnectionString("Redis") ?? throw new Exception("Redis connection string is missing");
            return ConnectionMultiplexer.Connect(configuration);
        });
        services.AddHttpContextAccessor();

        services.AddScoped<LogUserActivity>();
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


        services.Configure<CloudinarySettings>(config.GetSection("CloudinarySettings"));
        services.AddSignalR();
        services.AddSingleton<PresenceTracker>();

        return services;
    }
}
