using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Nest;
using Npgsql;
using Repositories.Implementations;
using Repositories.Interfaces;
using Repositories.Models;

var builder = WebApplication.CreateBuilder(args);

#region ================= ELASTIC SEARCH =================
builder.Services.AddSingleton<IElasticClient>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();

    var settings = new ConnectionSettings(new Uri(config["Elasticsearch:Uri"]))
        .DefaultIndex(config["Elasticsearch:DefaultIndex"])
        .BasicAuthentication(
            config["Elasticsearch:Username"],
            config["Elasticsearch:Password"]
        )
        .ServerCertificateValidationCallback((o, c, ch, e) => true);

    return new ElasticClient(settings);
});
#endregion

#region ================= CORE SERVICES =================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
#endregion

#region ================= CORS =================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMVC", policy =>
    {
        policy
            .WithOrigins("http://localhost:5229") // MVC URL
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
#endregion

#region ================= DATABASE =================
builder.Services.AddTransient<NpgsqlConnection>(sp =>
{
    var cs = sp.GetRequiredService<IConfiguration>()
               .GetConnectionString("pgconn");
    return new NpgsqlConnection(cs);
});
#endregion

#region ================= CONFIGURATION =================
builder.Services.Configure<t_email>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.Configure<HuggingFaceConfig>(
    builder.Configuration.GetSection("HuggingFace"));
#endregion

#region ================= DEPENDENCY INJECTION =================
builder.Services.AddScoped<IEmailInterface, EmailRepository>();
builder.Services.AddScoped<IOtpInterface, OtpRepository>();
builder.Services.AddScoped<IAdminInterface, AdminRepository>();
builder.Services.AddScoped<IApplicantInterface, ApplicantRepository>();
builder.Services.AddScoped<IRecruiterInterface, RecruiterRepository>();
builder.Services.AddScoped<IUserInterface, UserRepository>();
builder.Services.AddScoped<IElasticsearchInterface, ElasticsearchRepository>();
#endregion

#region ================= JWT AUTH =================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
        )
    };
});
#endregion

#region ================= SWAGGER + JWT =================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = HeaderNames.Authorization,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token as: Bearer {token}"
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
#endregion

var app = builder.Build();

#region ================= MIDDLEWARE =================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowMVC");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
#endregion

#region ================= TEST ENDPOINT =================
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild",
    "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )
    ).ToArray();

    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();
#endregion

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
