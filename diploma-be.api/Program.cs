using diploma_be.bll.Services;
using diploma_be.dal;
using diploma_be.dal.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var connectionString = "Host=localhost;Database=psychapp;Username=postgres;Password=1234;Port=5432";
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(connectionString));

builder.Services.AddScoped<ITopsisService, TopsisService>();

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
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
			ClockSkew = TimeSpan.Zero,
			RequireExpirationTime = true
		};

		options.Events = new JwtBearerEvents
		{
			OnAuthenticationFailed = context =>
			{
				Console.WriteLine($"Authentication failed: {context.Exception.Message}");
				return Task.CompletedTask;
			},
			OnTokenValidated = context =>
			{
				var userEmail = context.Principal?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
				var userRole = context.Principal?.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
				Console.WriteLine($"Token validated for user: {userEmail} with role: {userRole}");
				return Task.CompletedTask;
			},
			OnChallenge = context =>
			{
				Console.WriteLine($"JWT Challenge: {context.Error} - {context.ErrorDescription}");
				return Task.CompletedTask;
			}
		};
	});

builder.Services.AddAuthorization();

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

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
	{
		policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
	});
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	try
	{
		Console.WriteLine("Setting up database...");

		var deleted = await context.Database.EnsureDeletedAsync();
		if (deleted)
		{
			Console.WriteLine("Existing database deleted");
		}

		var created = await context.Database.EnsureCreatedAsync();
		if (created)
		{
			Console.WriteLine("Database and tables created successfully!");
		}
		else
		{
			Console.WriteLine("Database already existed");
		}

		var canConnect = await context.Database.CanConnectAsync();
		if (!canConnect)
		{
			throw new Exception("Cannot connect to database after creation");
		}

		Console.WriteLine("Database connection confirmed");

		var userCount = await context.Users.CountAsync();
		Console.WriteLine($"Existing users: {userCount}");

		if (userCount == 0)
		{
			Console.WriteLine("No users found, creating seed data...");
			await CreateSeedData(context);
		}

		var finalUserCount = await context.Users.CountAsync();
		var specialistCount = await context.Specialists.CountAsync();
		var clientCount = await context.Clients.CountAsync();

		Console.WriteLine($"Final counts - Users: {finalUserCount}, Specialists: {specialistCount}, Clients: {clientCount}");

		if (finalUserCount > 0)
		{
			Console.WriteLine("Database setup completed successfully!");
		}
		else
		{
			throw new Exception("Failed to create users!");
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Database setup error: {ex.Message}");
		Console.WriteLine($"Stack trace: {ex.StackTrace}");
		Console.WriteLine("Continuing without database...");
	}
}

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

await app.RunAsync();

static async Task CreateSeedData(AppDbContext context)
{
	try
	{
		Console.WriteLine("Creating seed data...");

		var adminId = Guid.NewGuid();
		var specialist1Id = Guid.NewGuid();
		var specialist2Id = Guid.NewGuid();
		var client1Id = Guid.NewGuid();

		var users = new List<User>
		{
			new User
			{
				Id = adminId,
				FirstName = "Admin",
				LastName = "User",
				Email = "admin@psychapp.com",
				Phone = "+380501234567",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
				Role = "Admin",
				CreatedAt = DateTime.UtcNow
			},
			new User
			{
				Id = specialist1Id,
				FirstName = "Anna",
				LastName = "Kovalenko",
				Email = "anna@psychapp.com",
				Phone = "+380507654321",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
				Role = "Specialist",
				CreatedAt = DateTime.UtcNow
			},
			new User
			{
				Id = specialist2Id,
				FirstName = "Petro",
				LastName = "Ivanov",
				Email = "petro@psychapp.com",
				Phone = "+380509876543",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
				Role = "Specialist",
				CreatedAt = DateTime.UtcNow
			},
			new User
			{
				Id = client1Id,
				FirstName = "Oleksandr",
				LastName = "Petrenko",
				Email = "client@psychapp.com",
				Phone = "+380661234567",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
				Role = "Client",
				CreatedAt = DateTime.UtcNow
			}
		};

		context.Users.AddRange(users);
		await context.SaveChangesAsync();
		Console.WriteLine("✅ Users created");

		var specialists = new List<Specialist>
		{
			new Specialist
			{
				Id = Guid.NewGuid(),
				UserId = specialist1Id,
				Education = "PhD Psychology, KNU",
				Experience = "8 years",
				Specialization = "Anxiety",
				Price = 800,
				Online = true,
				Offline = true,
				Gender = "Female",
				Language = "Ukrainian",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			},
			new Specialist
			{
				Id = Guid.NewGuid(),
				UserId = specialist2Id,
				Education = "Master Family Therapy",
				Experience = "12 years",
				Specialization = "Relationships",
				Price = 1200,
				Online = false,
				Offline = true,
				Gender = "Male",
				Language = "Ukrainian",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			}
		};

		context.Specialists.AddRange(specialists);
		await context.SaveChangesAsync();
		Console.WriteLine("✅ Specialists created");

		var client = new Client
		{
			Id = Guid.NewGuid(),
			UserId = client1Id,
			Budget = 1000,
			PreferOnline = true,
			PreferOffline = false,
			PreferredGender = "Female",
			PreferredLanguage = "Ukrainian",
			Issue = "Anxiety",
			CreatedAt = DateTime.UtcNow
		};

		context.Clients.Add(client);
		await context.SaveChangesAsync();
		Console.WriteLine("Client created");

		Console.WriteLine("Seed data creation completed!");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error creating seed data: {ex.Message}");
		throw;
	}
}