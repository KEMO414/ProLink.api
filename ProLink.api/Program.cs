using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ProLink.Data.Data;
using ProLink.Infrastructure;
using ProLink.Application;
using Microsoft.AspNetCore.Identity;
using ProLink.Data.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ProLink.Application.Mail;
using ProLink.api;
using ProLink.Application.Hubs;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

#region Connection String
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
#endregion

#region Identity
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
//reset password
builder.Services.Configure<DataProtectionTokenProviderOptions>
    (options => options.TokenLifespan = TimeSpan.FromMinutes(1));

#endregion
#region Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
}
    ).AddJwtBearer(o =>
    {
        o.IncludeErrorDetails = true;
        o.RequireHttpsMetadata = false;
        o.SaveToken = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
            ValidAudience = builder.Configuration["JWT:ValidAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"])),
            ClockSkew = TimeSpan.Zero
        };
    });
#endregion

#region Dependency Injection
builder.Services.AddInfrastructureServices().
    AddReposetoriesServices();
#endregion

#region mailing
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("Mailing"));
builder.Services.Configure<IdentityOptions>(opts=>opts.SignIn.RequireConfirmedEmail=true);
#endregion

#region swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo", Version = "v1" });
});
builder.Services.AddSwaggerGen(swagger =>
{
    //This?is?to?generate?the?Default?UI?of?Swagger?Documentation????
    swagger.SwaggerDoc("v2", new OpenApiInfo
    {
        Version = "v1",
        Title = "ASP.NET?5?Web?API",
        Description = " ITI Projrcy"
    });

    //?To?Enable?authorization?using?Swagger?(JWT)????
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter?'Bearer'?[space]?and?then?your?valid?token?in?the?text?input?below.\r\n\r\nExample:?\"Bearer?eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\"",
    });
    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                    new string[] {}
                    }
                });
});
#endregion

#region Cors policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
#endregion

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("CorsPolicy");
app.UseMiddleware<PerformanceMiddleware>();
app.MapControllers();
app.MapHub<ChatHub>("/chatHub");
app.Run();
