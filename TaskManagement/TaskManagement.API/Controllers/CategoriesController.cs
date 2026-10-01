using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Data;
using TaskManagement.API.Models;
using TaskManagement.API.DTOs;
using Microsoft.AspNetCore.Components.Forms;

namespace TaskManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController(AppDbContext context):ControllerBase
    {
        [HttpGet]
        [EndpointSummary("Get all categories")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategory()
        {
            return await context.Categories.ToListAsync();
        }

        [HttpGet("{id}")]
        [EndpointSummary("Get by id")]
        public async Task<ActionResult<Category>> GetById(int id)
        {
            var category = await context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return category;
        }

        [HttpPost]
        [EndpointSummary("Create new")]
        public async Task<ActionResult<Category>> CreateNew(RequestDtos categoryDto)
        {
            var category = new Category
            {
                Name = categoryDto.Name
            };
            context.Categories.Add(category);
            await context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        [HttpDelete("{id}")]
        [EndpointSummary("Delete categories")]
        public async Task<ActionResult> DeleteC(int id)
        {
            var category = await context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            context.Categories.Remove(category);
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
