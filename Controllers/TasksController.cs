using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PromoosTT.DB;
using PromoosTT.Events;
using PromoosTT.Models;
using PromoosTT.Rabbit;

namespace PromoosTT.Controllers;

[ApiController]
[Route("[controller]")]
public class TasksController : Controller
{
    private readonly TaskDbContext dbContext; // конкретный экземпляр, без интерфейса, для простаты. 
    private readonly RabbitMqService mqService; // конкретный экземпляр, без интерфейса, для простаты. 

    private const int MaxTitleSymbols = 200; // Надо наверное объявлять где то в другом месте, к примеру брать из appsettings или ещё где то. Но для простаты тут константа. 
    private const string MessageRoute = "task.completed"; // Так же вероятно надо объявлять где то в другом месте.

    public TasksController(TaskDbContext dbContext, RabbitMqService mqService)
    {
        this.dbContext = dbContext;
        this.mqService = mqService;
    }

    [HttpPost(Name = "Create")]
    public async Task<IActionResult> Create(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return BadRequest("Please use not empty title");

        if (title.Length > MaxTitleSymbols)
            return BadRequest($"Title is too large, please use less than {MaxTitleSymbols} symbols");

        var id = Guid.NewGuid();

        var item = new TaskItem
        {
            Id = id,
            Title = title,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = null, // можно не указывать, но почему бы и явно null не указать для большей понятности и читаемости
            Priority = Priority.Low,
        };

        try
        {
            await dbContext.AddAsync(item);

            await dbContext.SaveChangesAsync();

            return Ok(id);
        }
        catch
        {
            return StatusCode(500, "Oops, Something went wrong"); // Можно ещё и ошибку нам залогать
        }
    }

    [HttpGet(Name = "GetAll")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var items = await dbContext.tasks.AsNoTracking().ToListAsync();

            if (items == null || items.Count == 0)
                return StatusCode(404, "There is currently no items");

            return Ok(items);
        }
        catch
        {
            return StatusCode(500, "Oops, Something went wrong"); // Можно ещё и ошибку нам залогать
        }
    }

    [HttpPut(Name = "Complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var item = await dbContext.tasks.FirstOrDefaultAsync(t => t.Id == id);

        if (item == null)
            return NotFound($"There is no such task with id {id}");

        if (item.IsCompleted)
            return Conflict($"Task {id} is already completed");

        item.IsCompleted = true;

        var completedAt = DateTime.UtcNow;

        item.CompletedAt = completedAt;

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict($"Task {id} was modified by another request");
        }

        await mqService.PublishAsync(new TaskCompletedEvent(item.Id, item.Title, completedAt, item.Priority), MessageRoute);

        return Ok(item);
    }

    [HttpDelete(Name = "Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await dbContext.tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

        if (item == null)
            return NotFound($"There is no such task with id {id}");

        try
        {
            dbContext.tasks.Remove(item);

            await dbContext.SaveChangesAsync();

            return Ok();
        }
        catch
        {
            return StatusCode(500, "Oops, Something went wrong"); // Можно ещё и ошибку нам залогать
        }
    }

}