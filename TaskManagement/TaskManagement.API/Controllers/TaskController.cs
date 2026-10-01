using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Data;
using TaskManagement.API.Models;
using TaskManagement.API.DTOs;
namespace TaskManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController(AppDbContext context): ControllerBase
    {
        [HttpGet]
        [EndpointSummary("Get all tasks")]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            return await context.Tasks
                .Include(t=>t.Category)
                .Include(t=>t.User)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        [EndpointSummary("Get task by id")]
        public async Task<ActionResult<TaskItem>> GetById(int id)
        {
            var task = await context.Tasks
                .Include(t => t.Category)
                .Include(t => t.User)
                .FirstOrDefaultAsync(t=>t.Id==id);
            if (task == null)
            {
                return NotFound();
            }
            return task;
        }

        [HttpPost]
        [EndpointSummary("Create new Task")]
        public async Task<ActionResult> CreateTask(TaskRequestDto taskDto)
        {//title,description, useid, categoryid
            var task = new TaskItem
            {
                Title = taskDto.Title,
                Description = taskDto.Description,
                UserId=taskDto.UserId,
                CategoryId=taskDto.CategoryId,
            };
            context.Tasks.Add(task);
            await context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTasks), new { id = task.Id }, task);
        }

        [HttpPut("{id}")]
        [EndpointSummary("Update Task")]
        public async Task<ActionResult<TaskItem>> UpdateTask(int id, UpdateTaskRequestDto taskDto)
        {
            var task = await context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            task.Title = taskDto.Title;
            task.Description = taskDto.Description;
            task.Status = taskDto.Status;
            task.CategoryId = taskDto.CategoryId;
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [EndpointSummary("Delete Task")]
        public async Task<ActionResult> DeleteTask(int id)
        {
            var task = await context.Tasks.FindAsync(id);
            if(task==null)
            {
                return NotFound();
            }
            context.Tasks.Remove(task);
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
