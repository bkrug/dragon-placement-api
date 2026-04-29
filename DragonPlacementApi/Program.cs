using DragonPlacementApi.Endpoints;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Repositories;
using Microsoft.EntityFrameworkCore;

const string allowedOriginsPolicy = "DragonPlacementAllowedOrigins";

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetValue<string>("AllowedOrigins")?.Split(",") ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(
        allowedOriginsPolicy,
        policy => policy.WithOrigins(allowedOrigins).AllowCredentials().AllowAnyHeader().AllowAnyMethod()
    )
);

builder.Services
    .AddEntityFrameworkSqlite()
    .AddDbContext<DragonPlacementContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DragonPlacementDb")));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IAssignmentUnitOfWork, AssignmentUnitOfWork>();

var app = builder.Build();
app.UseCors(allowedOriginsPolicy);

app.MapGet("/", () => "Dragon Placement API!");
app.MapGet("/dragon", DragonEndpoints.GetDragons);
app.MapGet("/job", JobEndpoints.GetJobs);
app.MapGet("/job/{jobId}/assigned-dragon", JobEndpoints.GetAssignedDragons);
app.MapPost("/assignment", AssignmentEndpoints.AssignDragonToJobAsync);

app.Run();
