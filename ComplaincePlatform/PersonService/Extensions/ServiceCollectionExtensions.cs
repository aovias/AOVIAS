using FluentValidation;
using PersonService.Application.Contracts;
using PersonService.Application.Services;
using PersonService.Infrastructure.MasterData;
using PersonService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Services;
using System.Text;

namespace PersonService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersonDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PersonDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance));

        services.AddScoped<IPlatformSequenceStore, EfPersonSequenceStore>();
        services.AddScoped<IPlatformIdGenerator, PlatformIdGenerator>();
        return services;
    }

    public static IServiceCollection AddPersonAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey is required");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer is required");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience is required");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddInternalApiAuthorization();
        return services;
    }

    public static IServiceCollection AddPersonApplication(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IPersonAppService, PersonAppService>();
        services.AddScoped<IPersonAuditService, PersonAuditService>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(PersonAppService).Assembly));
        services.AddValidatorsFromAssemblyContaining<Application.Validators.CreatePersonCommandValidator>();
        services.AddHostedService<Infrastructure.Background.PersonOutboxProcessor>();
        services.AddHostedService<Infrastructure.Background.PersonMasterDataBootstrapHostedService>();
        return services;
    }

    public static IServiceCollection AddPersonApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });
        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });
        return services;
    }
}
