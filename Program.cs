using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using pharmEasyClone_backend.Data;
using pharmEasyClone_backend.Services;

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
    options.AddPolicy("AngularClientPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Default Angular local server port
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

// Bind settings from appsettings.json
builder.Services.Configure<BrevoSettings>(builder.Configuration.GetSection("BrevoSettings"));

// Register custom services for Dependency Injection
builder.Services.AddHttpClient<IEmailService, BrevoEmailService>();

// ==========================================
// 4. AUTHENTICATION & JWT CONFIGURATION
// ==========================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? "A_Very_Secure_And_Ultra_Long_Secret_Key_For_PharmEasy_Clone_Authentication_2026";

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

// ==========================================
// 6. HTTP REQUEST PIPELINE (MIDDLEWARE)
// ==========================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PharmEasy Clone API v1"));
}

app.UseHttpsRedirection();

// CORS must be executed before Authentication and Authorization
app.UseCors("AngularClientPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ==========================================
// 7. DATABASE SEEDING
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        DataSeeder.SeedData(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();