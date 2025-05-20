using System.Text;
using MagicVilla_VillaAPI;
using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Repository;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

#region entity framework configuration

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultSQLConnection")));

#endregion entity framework configuration

#region automapper configuration

builder.Services.AddAutoMapper(typeof(MappingConfig)); //download just the automapper nuget package

#endregion automapper configuration

#region Serilog congiguration

Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
    .WriteTo.File("log/villaLogs.txt", rollingInterval: RollingInterval.Day).CreateLogger();

builder.Host.UseSerilog(); //so it won't use the built-in logging system

#endregion Serilog congiguration

#region cache configuration

builder.Services.AddResponseCaching(); // to cache the response of the API

#endregion cache configuration

#region repository DI

builder.Services.AddScoped<IVillaRepository, VillaRepository>();
builder.Services.AddScoped<IRepository<Villa>, Repository<Villa>>(); // generic repository
builder.Services.AddScoped<IVillaNumberRepository, VillaNumberRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

#endregion repository DI

#region versioning configuration

builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true; // if no version is specified, use the default version
    options.DefaultApiVersion = new ApiVersion(1, 0); // Sets the default API version to 1.0, which will be used when a client does not specify a version in the request.
    options.ReportApiVersions = true; // Adds the "api-supported-versions" and "api-deprecated-versions" headers to the response, which can be used by clients to determine which versions of the API are supported or deprecated.
});
builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // Sets the format for the version group name. The 'v' prefix is added to the version number.
    options.SubstituteApiVersionInUrl = true; // Replaces the version number in the URL with the version specified in the request.
});

#endregion versioning configuration

#region authentication configuration

var key = builder.Configuration.GetValue<string>("ApiSettings:Secret");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // for development only
        options.SaveToken = true; // save token in the request
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

#endregion authentication configuration

#region response accetable format configuration and caching

builder.Services.AddControllers(options =>
{
    options.CacheProfiles.Add("Default30", new CacheProfile
    {
        Duration = 30 // cache the response for 30 seconds
    });

    //options.ReturnHttpNotAcceptable = true; // to support only json format in the request and response (can't retrive text/plain)
}).AddNewtonsoftJson().AddXmlDataContractSerializerFormatters(); // to support XML also

#endregion response accetable format configuration and caching

#region Swagger configuration

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n + " +
        "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n +" +
        "Example: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1.0",
        Title = "Magic Villa V1",
        Description = "API for managing villas",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Abdullrahman Ghazal",
            Url = new Uri("https://www.linkedin.com/in/abdullrahman-ghazal/")
        },
        License = new OpenApiLicense
        {
            Name = "Exaple License",
            Url = new Uri("https://example.com/license")
        }
    });
    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Version = "v2.0",
        Title = "Magic Villa V2",
        Description = "API for managing villas",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Abdullrahman Ghazal",
            Url = new Uri("https://www.linkedin.com/in/abdullrahman-ghazal/")
        },
        License = new OpenApiLicense
        {
            Name = "Example License",
            Url = new Uri("https://example.com/license")
        }
    });
});

#endregion Swagger configuration

#region Idetity configuration

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

#endregion Idetity configuration

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        #region versions url

        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Magic_VillaV1");
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "Magic_VillaV2");

        #endregion versions url
    });
}

app.UseHttpsRedirection();

app.UseAuthentication(); // add authentication before authorization

app.UseAuthorization();

app.MapControllers();

app.Run();