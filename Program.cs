using Microsoft.EntityFrameworkCore;
using PromoosTT.DB;
using PromoosTT.Rabbit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<RabbitMqService>();

builder.Services.AddDbContext<TaskDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Promoos"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();