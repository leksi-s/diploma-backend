using diploma_be.bll.Services;
using diploma_be.dal;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Database - hardcoded connection string to avoid appsettings issues
var connectionString = "Host=localhost;Database=psychapp;Username=postgres;Password=1234;Port=5432";
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(connectionString));

// Add our services
builder.Services.AddScoped<ITopsisService, TopsisService>();

// JWT - hardcoded to avoid appsettings issues
var jwtKey = "YourSuperSecretKeyForJWTWhichShouldBeLongEnough123456789";
var jwtIssuer = "PsychApp";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = jwtIssuer,
			ValidAudience = jwtIssuer,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
		};
	});

builder.Services.AddAuthorization();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "Psychology Matching API",
		Version = "v1",
		Description = "API для підбору психологів з алгоритмом TOPSIS"
	});

	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
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
			Array.Empty<string>()
		}
	});
});

// CORS
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
	{
		policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
	});
});

var app = builder.Build();

// Create database with detailed error handling
using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	try
	{
		Console.WriteLine("🔄 Attempting to connect to PostgreSQL...");
		Console.WriteLine($"Connection: {connectionString.Replace("Password=1234", "Password=***")}");

		// Test connection first
		var canConnect = await context.Database.CanConnectAsync();
		if (canConnect)
		{
			Console.WriteLine("✅ Connected to PostgreSQL successfully!");

			// Create database and tables
			await context.Database.EnsureCreatedAsync();
			Console.WriteLine("✅ Database and tables created successfully!");

			// Check if we have seed data
			var usersCount = await context.Users.CountAsync();
			Console.WriteLine($"📊 Current users in database: {usersCount}");

			if (usersCount == 0)
			{
				Console.WriteLine("⚠️  No users found - seed data might not have been created");
				Console.WriteLine("💡 Check DbContext.OnModelCreating for seed data configuration");
			}
		}
		else
		{
			Console.WriteLine("❌ Cannot connect to PostgreSQL database");
			Console.WriteLine("🔍 Please check:");
			Console.WriteLine("   - PostgreSQL service is running");
			Console.WriteLine("   - Database 'psychapp' exists");
			Console.WriteLine("   - Username/password are correct");
			Console.WriteLine("   - Port 5432 is available");
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine($"❌ Database error: {ex.Message}");
		Console.WriteLine($"🔍 Inner exception: {ex.InnerException?.Message}");
		Console.WriteLine("💡 Common solutions:");
		Console.WriteLine("   1. Install PostgreSQL from https://www.postgresql.org/download/");
		Console.WriteLine("   2. Create database: CREATE DATABASE psychapp;");
		Console.WriteLine("   3. Check connection string credentials");
		Console.WriteLine("   4. Ensure PostgreSQL service is started");
	}
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(c =>
	{
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "Psychology Matching API V1");
		c.RoutePrefix = string.Empty;
		c.DocumentTitle = "Psychology API";
	});
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine();
Console.WriteLine("🚀 Psychology Matching API started!");
Console.WriteLine("📖 Swagger UI: https://localhost:7227");
Console.WriteLine();

// Only show test accounts if database connection was successful
try
{
	using var scope = app.Services.CreateScope();
	var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	if (await context.Database.CanConnectAsync())
	{
		Console.WriteLine("🔑 Test Accounts:");
		Console.WriteLine("┌─────────────┬─────────────────────────┬─────────────┐");
		Console.WriteLine("│ Role        │ Email                   │ Password    │");
		Console.WriteLine("├─────────────┼─────────────────────────┼─────────────┤");
		Console.WriteLine("│ Admin       │ admin@psychapp.com      │ admin123    │");
		Console.WriteLine("│ Specialist  │ anna@psychapp.com       │ password123 │");
		Console.WriteLine("│ Specialist  │ petro@psychapp.com      │ password123 │");
		Console.WriteLine("│ Client      │ client@psychapp.com     │ password123 │");
		Console.WriteLine("└─────────────┴─────────────────────────┴─────────────┘");
	}
	else
	{
		Console.WriteLine("⚠️  Database connection failed - test accounts not available");
	}
}
catch
{
	Console.WriteLine("⚠️  Database not accessible - test accounts not available");
}

Console.WriteLine();
Console.WriteLine("🎯 Main Features:");
Console.WriteLine("• JWT Authentication & Authorization");
Console.WriteLine("• TOPSIS Algorithm for specialist matching");
Console.WriteLine("• Client questionnaire and preferences");
Console.WriteLine("• Specialist profile management");
Console.WriteLine("• Admin panel for specialist management");
Console.WriteLine("• Appointment booking system");
Console.WriteLine();
Console.WriteLine("📋 Available Endpoints:");
Console.WriteLine("• POST /api/auth/login - Login");
Console.WriteLine("• POST /api/auth/register - Register");
Console.WriteLine("• GET /api/client/topsis/recommendations - Get TOPSIS recommendations");
Console.WriteLine("• POST /api/client/topsis/calculate - Calculate custom TOPSIS");
Console.WriteLine("• GET /api/admin/specialists - Manage specialists (Admin only)");
Console.WriteLine("• GET /api/specialist/appointments - View appointments (Specialist only)");

await app.RunAsync();