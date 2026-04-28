using DragonPlacementApi.Endpoints;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddEntityFrameworkSqlite()
    .AddDbContext<DragonPlacementContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DragonPlacementDb")));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IAssignmentUnitOfWork, AssignmentUnitOfWork>();

var app = builder.Build();

app.MapGet("/", () => "Dragon Placement API!");
app.MapGet("/dragon", DragonEndpoints.GetDragons);
app.MapGet("/job", JobEndpoints.GetJobs);
app.MapPost("/assignment", AssignmentEndpoints.AssignDragonToJobAsync);

app.Run();
