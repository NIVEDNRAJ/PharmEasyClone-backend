using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using pharmEasyClone_backend.Data;
using pharmEasyClone_backend.Services;

// Load environment variables from .env file if it exists
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        var trimmedLine = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
        {
            continue;
        }

        var parts = trimmedLine.Split('=', 2);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var val = parts[1].Trim();

            // Strip surrounding quotes if present
            if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
            {
                val = val.Substring(1, val.Length - 2);
            }

            Environment.SetEnvironmentVariable(key, val);
        }
    }
}

// Clean surrounding quotes from environment variables if they were loaded via Docker env-file
void CleanEnvVar(string name)
{
    var val = Environment.GetEnvironmentVariable(name);
    if (!string.IsNullOrEmpty(val))
    {
        val = val.Trim();
        if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
        {
            val = val.Substring(1, val.Length - 2);
            Environment.SetEnvironmentVariable(name, val);
        }
    }
}

CleanEnvVar("MYSQL_CONNECTION_STRING");
CleanEnvVar("DB_CONNECTION_STRING");
CleanEnvVar("JWT_SECRET");
CleanEnvVar("JWT_ISSUER");
CleanEnvVar("JWT_AUDIENCE");
CleanEnvVar("BREVO_API_KEY");
CleanEnvVar("BREVO_SENDER_EMAIL");
CleanEnvVar("BREVO_SENDER_NAME");
CleanEnvVar("RAZORPAY_KEY_ID");
CleanEnvVar("RAZORPAY_KEY_SECRET");
CleanEnvVar("ASPNETCORE_ENVIRONMENT");

// Map friendly environment variables to ASP.NET Core configuration keys
void MapEnvVar(string envName, string configName)
{
    var val = Environment.GetEnvironmentVariable(envName);
    if (!string.IsNullOrEmpty(val))
    {
        Environment.SetEnvironmentVariable(configName, val);
    }
}

MapEnvVar("MYSQL_CONNECTION_STRING", "ConnectionStrings__DefaultConnection");
MapEnvVar("DB_CONNECTION_STRING", "ConnectionStrings__DefaultConnection");
MapEnvVar("JWT_SECRET", "JwtSettings__Secret");
MapEnvVar("JWT_ISSUER", "JwtSettings__Issuer");
MapEnvVar("JWT_AUDIENCE", "JwtSettings__Audience");
MapEnvVar("BREVO_API_KEY", "BrevoSettings__ApiKey");
MapEnvVar("BREVO_SENDER_EMAIL", "BrevoSettings__SenderEmail");
MapEnvVar("BREVO_SENDER_NAME", "BrevoSettings__SenderName");
MapEnvVar("RAZORPAY_KEY_ID", "Razorpay__KeyId");
MapEnvVar("RAZORPAY_KEY_SECRET", "Razorpay__KeySecret");

var builder = WebApplication.CreateBuilder(args);
    
// ==========================================
// 1. DATABASE CONFIGURATION (MySQL)
// ==========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ==========================================
// 2. CORS POLICY (For Angular Integration)
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ==========================================
// 3. CORE APPLICATION SERVICES & DI
// ==========================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Bind settings from appsettings.json and environment variables
builder.Services.Configure<BrevoSettings>(options =>
{
    var section = builder.Configuration.GetSection("BrevoSettings");
    options.ApiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY") ?? section["ApiKey"] ?? "";
    options.SenderEmail = section["SenderEmail"] ?? "";
    options.SenderName = section["SenderName"] ?? "";
});

// Register custom services for Dependency Injection
builder.Services.AddHttpClient<IEmailService, BrevoEmailService>();

// ==========================================
// 4. AUTHENTICATION & JWT CONFIGURATION
// ==========================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? "A_Very_Secure_And_Ultra_Long_Secret_Key_For_PharmEasy_Clone_Authentication_2026";

builder.Services.AddAuthorization();

builder.Services.AddAuthentication(options =>
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
        ValidIssuer = jwtSettings["Issuer"] ?? "PharmEasyCloneBackend",
        ValidAudience = jwtSettings["Audience"] ?? "PharmEasyCloneAngular",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // Removes the default 5-minute grace period for token expiration
    };
});

// ==========================================
// 5. SWAGGER / API DOCUMENTATION
// ==========================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PharmEasy Clone API", Version = "v1" });
    
    // Add JWT Bearer Authentication UI to Swagger
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Custom CORS Middleware to handle all origins, headers, and OPTIONS preflights
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers["Origin"].ToString();
    if (!string.IsNullOrEmpty(origin))
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = origin;
        context.Response.Headers["Access-Control-Allow-Headers"] = "*";
        context.Response.Headers["Access-Control-Allow-Methods"] = "*";
        context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
    }

    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        return;
    }

    await next();
});

// ==========================================
// 6. HTTP REQUEST PIPELINE (MIDDLEWARE)
// ==========================================
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PharmEasy Clone API v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ==========================================
// 7. DATABASE SEEDING
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Retry logic for DB connection to wait for MySQL in container environments
        int retries = 12;
        while (retries > 0)
        {
            try
            {
                logger.LogInformation("Attempting to ensure database is created...");
                context.Database.EnsureCreated();
                logger.LogInformation("Database is ready and created.");
                break;
            }
            catch (Exception ex)
            {
                retries--;
                logger.LogWarning($"Database connection failed. Retrying in 5 seconds... ({retries} retries remaining). Error: {ex.Message}");
                if (retries == 0) throw;
                Thread.Sleep(5000);
            }
        }
        
        DataSeeder.SeedData(context);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();