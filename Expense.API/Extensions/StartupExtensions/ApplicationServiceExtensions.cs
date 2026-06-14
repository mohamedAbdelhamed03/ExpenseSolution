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
using Expense.Infrastructure.Identity;
using Expense.Core.Features.Common;
using Expense.Core.Features.DependencyInjection;
using FluentValidation.AspNetCore;
using Expense.Infrastructure.Data;
using Expense.Infrastructure.Authentication;
using Expense.Core.Application.Authentication;
using Expense.Core.Common.Options;
using Expense.Core.Application.Groups;
using Expense.Core.Application.Expenses;
using Expense.Core.Application.Balances;
using Expense.Infrastructure.Groups;
using Expense.Infrastructure.Expenses;
using Expense.Infrastructure.Balances;
using Expense.Infrastructure.Authentication.Social;
using Expense.Infrastructure.Repositories;
using Expense.Core.Application.Persistence;
using Expense.Core.Application.Categories;
using Expense.Infrastructure.Categories;
using Expense.Core.Application.ActivityLogs;
using Expense.Infrastructure.ActivityLogs;
using Expense.Core.Application.Settlements;
using Expense.Infrastructure.Settlements;
using Expense.Core.Application.Debts;
using Expense.Infrastructure.Debts;
using Expense.Infrastructure.Notifications;
using Expense.Core.Application.Notifications;
using Expense.Core.Application.Insights;
using Expense.Infrastructure.Insights;
using Expense.Core.Application.Home;
using Expense.Infrastructure.Home;
using Expense.Core.Application.Services;
using Expense.Infrastructure.Services.Cloudinary;
using Expense.Core.Application.Common.Interfaces;

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
        var environment = builder.Environment;

        var jwtSection = configuration.GetSection("JwtSettings");
        var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();

        if (string.IsNullOrWhiteSpace(jwtSettings.Key))
        {
            throw new InvalidOperationException("Missing required configuration: JwtSettings:Key (set env var JwtSettings__Key).");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
        {
            throw new InvalidOperationException("Missing required configuration: JwtSettings:Issuer (set env var JwtSettings__Issuer).");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
        {
            throw new InvalidOperationException("Missing required configuration: JwtSettings:Audience (set env var JwtSettings__Audience).");
        }

        var cloudinarySettings = configuration.GetSection("Cloudinary").Get<CloudinarySettings>() ?? new CloudinarySettings();
        if (string.IsNullOrWhiteSpace(cloudinarySettings.ApiSecret))
        {
            throw new InvalidOperationException("Missing required configuration: Cloudinary:ApiSecret (set env var Cloudinary__ApiSecret).");
        }

        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        if (!environment.IsEnvironment("Test") && string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("Missing required configuration: ConnectionStrings:DefaultConnection (set env var ConnectionStrings__DefaultConnection).");
        }

        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });
        // services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddCoreValidation();

        var coreAssembly = typeof(ValidationBehavior<,>).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(coreAssembly));

        services.AddLocalization();
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
            var allowedOrigins = configuration.GetSection("AllowedOrigins")
                .Get<string[]>()?
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.Trim())
                .Distinct()
                .ToArray() ?? Array.Empty<string>();

            options.AddPolicy("FrontendPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
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
            // var provider = configuration["DatabaseProvider"]?.ToLowerInvariant();
            // var connectionString = configuration.GetConnectionString("DefaultConnection");
            // if (provider == "postgres" || provider == "postgresql")
            // {
            //     options.UseNpgsql(connectionString);
            // }
            // else
            // {
            //     options.UseSqlServer(connectionString);
            // }
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
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
        services.AddScoped<IBalanceService, BalanceService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<IDebtSimplificationService, DebtSimplificationService>();
        services.AddScoped<IInsightsService, InsightsService>();
        services.AddScoped<IHomeFeedService, HomeFeedService>();
        services.AddScoped<IPersonalExpenseService, PersonalExpenseService>();
        
        // Notifications
        services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>();
        services.AddScoped<IRealtimeNotifier, NativeWebSocketNotifier>();

        // Cloudinary
        services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));
        services.AddScoped<IFileUploader, CloudinaryFileUploader>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        // Social Auth
        services.AddHttpClient();
        services.AddScoped<ISocialAuthService, SocialAuthService>();
        services.AddScoped<ISocialTokenValidator, GoogleTokenValidator>();
        services.AddScoped<ISocialTokenValidator, FacebookTokenValidator>();

        services.AddHttpContextAccessor();

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
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws/notifications"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
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
