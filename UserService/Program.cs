
using Microsoft.OpenApi.Models;
using System.Reflection;
using UserService.Repositories;
using UserService.Repositories.DBContext;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.Commons;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region Vault

var EndPoint = Environment.GetEnvironmentVariable("vaultUrl");
var httpClientHandler = new HttpClientHandler();
httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => { return true; };

IAuthMethodInfo authMethod = new TokenAuthMethodInfo("00000000-0000-0000-0000-000000000000"); //unders�g om er korrekt

var vaultClientSettings = new VaultClientSettings(EndPoint, authMethod)
{
    Namespace = "",
    MyHttpClientProviderFunc = handler => new HttpClient(httpClientHandler) { BaseAddress = new Uri(EndPoint) }
};

IVaultClient vaultClient = new VaultClient(vaultClientSettings);

Secret<SecretData> vaultSecret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: "Secrets", mountPoint: "secret");

//register the secrets as a singleton so we can use dependency injection in the classes that need it
builder.Services.AddSingleton<Secret<SecretData>>(vaultSecret);

#endregion   

#region Authentication

string jwtSecret = vaultSecret.Data.Data["jwtSecret"].ToString() ?? string.Empty;
string jwtIssuer = vaultSecret.Data.Data["jwtIssuer"].ToString() ?? string.Empty;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = "http://localhost", //tr�k fra nginx
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
                    };
                });

#endregion

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<Secret<SecretData>>(vaultSecret);
builder.Services.AddScoped<MongoDBContext>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

#region Swagger

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
            },
            new List<string>()
        },
    });

    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
});

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
