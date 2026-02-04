using System.Reflection;
using System.Text;
using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Expense.Core.Domain.IdentityEntities;
using Expense.Core.Features.Common;
using Expense.Core.Features.DependencyInjection;
using FluentValidation.AspNetCore;
using Expense.Infrastructure.Data;
using Expense.Infrastructure.Authentication;
using Expense.Core.Abstractions.Authentication;
using Expense.Core.Common.Options;

namespace Expense.API.Extensions.StartupExtensions;

public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Configures services for the Expense.API application.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    public static WebApplicationBuilder ConfigureApplicationServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddControllers();
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddAuthValidation();

        // Assuming Program is in the entry assembly. 
        // We can use Assembly.GetExecutingAssembly() or pass the type if needed, 
        // but since this is an extension method, we might not have access to Program class if it's not public.
        // Usually Program is internal in .NET 6+. 
        // However, we can use a marker type or just GetEntryAssembly.
        // The original code used typeof(Program).
        // If Program is internal, we can't access it here easily unless we change visibility or use Assembly.GetEntryAssembly().
        // Let's use Assembly.GetEntryAssembly() which usually points to Program's assembly.
        var entryAssembly = Assembly.GetEntryAssembly();

        if (entryAssembly != null)
        {
            services.AddValidatorsFromAssembly(entryAssembly);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(entryAssembly));
        }

        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[] { "en", "ar" };
            options.SetDefaultCulture("en")
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);
        });

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Expense API",
                Version = "v1",
                Description = "Expense microservice for Expense application",
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityDefinition("Accept-Language", new OpenApiSecurityScheme
            {
                Description = "Accept-Language header",
                Name = "Accept-Language",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Accept-Language"
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
                    Array.Empty<string>()
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Accept-Language"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            c.EnableAnnotations();

            var xmlFile = $"{Assembly.GetEntryAssembly()?.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

            if (builder.Environment.IsDevelopment())
            {
                options.LogTo(Console.WriteLine, LogLevel.Information)
                       .EnableSensitiveDataLogging();
            }
        });

        // Configure Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;

            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Register services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddHttpContextAccessor();

        var jwtSection = configuration.GetSection("JwtSettings");
        var jwtSettings = jwtSection.Get<JwtSettings>();

        services.Configure<JwtSettings>(jwtSection);
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings?.Issuer,
                ValidAudience = jwtSettings?.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.Key ?? string.Empty)),
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                    var principal = context.Principal;
                    var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var tokenVersionClaim = principal?.FindFirst("TokenVersion")?.Value;
                    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tokenVersionClaim))
                    {
                        context.Fail("Invalid token");
                        return;
                    }
                    var user = await userManager.FindByIdAsync(userId);
                    if (user == null || !user.IsActive)
                    {
                        context.Fail("User invalid");
                        return;
                    }
                    if (!int.TryParse(tokenVersionClaim, out var tokenVersion) || user.TokenVersion != tokenVersion)
                    {
                        context.Fail("Token revoked");
                    }
                }
            };
        });

        // TODO: Add endpoints when EndpointExtensions is implemented
        // services.AddEndpoints();

        return builder;
    }
}
