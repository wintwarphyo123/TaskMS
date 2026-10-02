using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagement.API.Data;
using TaskManagement.API.DTOs;
using TaskManagement.API.Models;
namespace TaskManagement.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController(AppDbContext context): ControllerBase
    {
        [HttpGet]
        [EndpointSummary("Get all tasks")]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            int userId = GetCurrentUserId();
            return await context.Tasks
                .Where(t => t.UserId == userId)
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
            int userId= GetCurrentUserId();
            var task = new TaskItem
            {
                Title = taskDto.Title,
                Description = taskDto.Description,
                UserId=userId,
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

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }
    }
}
