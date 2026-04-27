using DragonPlacementDataLayer.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddEntityFrameworkSqlite()
    .AddDbContext<DragonPlacementContext>((options) =>
        options.UseSqlite("Data Source=../Database/DragonPlacement.db"));

var app = builder.Build();

app.MapGet("/", () => "Dragon Placement API!");
app.MapGet("/dragons", (DragonPlacementContext context) => context.Dragons.Take(20).ToList());

app.Run();
