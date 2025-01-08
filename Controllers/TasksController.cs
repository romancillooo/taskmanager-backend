using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TodoListApi.Data;
using TodoListApi.Models;

namespace TodoListApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly TodoListDbContext _context;

        public TasksController(TodoListDbContext context)
        {
            _context = context;
        }

        // Método privado para obtener el UserId del usuario autenticado
        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoTask>>> GetTasks()
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Usuario no autenticado.");
            }

            // Si el usuario tiene el rol de Admin, retorna todas las tareas
            if (User.IsInRole("Admin"))
            {
                return await _context.Tasks.ToListAsync();
            }

            // Si el usuario es un usuario normal, retorna solo sus tareas
            var tasks = await _context.Tasks
                                      .Where(t => t.UserId == userId)
                                      .ToListAsync();

            if (tasks == null || tasks.Count == 0)
            {
                return NotFound("No se encontraron tareas para este usuario.");
            }

            return Ok(tasks);
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<TodoTask>> AddTask(TaskDto taskDto)
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Usuario no autenticado.");
            }

            var task = new TodoTask
            {
                Description = taskDto.Description,
                IsCompleted = taskDto.IsCompleted,
                UserId = userId
            };

            try
            {
                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "Error al guardar la tarea: " + ex.Message);
            }

            return CreatedAtAction(nameof(GetTasks), new { id = task.Id }, task);
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskDto taskDto)
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Usuario no autenticado.");
            }

            var task = await _context.Tasks.FindAsync(id);

            // Validar si el usuario tiene permiso para modificar la tarea
            if (task == null || (task.UserId != userId && !User.IsInRole("Admin")))
            {
                return NotFound("Tarea no encontrada o no pertenece al usuario.");
            }

            task.Description = taskDto.Description;
            task.IsCompleted = taskDto.IsCompleted;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "Error al actualizar la tarea: " + ex.Message);
            }

            return NoContent();
        }

        // PATCH: api/tasks/{id}/toggle-status
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleTaskStatus(int id)
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Usuario no autenticado.");
            }

            var task = await _context.Tasks.FindAsync(id);

            // Validar si el usuario tiene permiso para cambiar el estado de la tarea
            if (task == null || (task.UserId != userId && !User.IsInRole("Admin")))
            {
                return NotFound("Tarea no encontrada o no pertenece al usuario.");
            }

            task.IsCompleted = !task.IsCompleted;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "Error al cambiar el estado de la tarea: " + ex.Message);
            }

            return NoContent();
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Usuario no autenticado.");
            }

            var task = await _context.Tasks.FindAsync(id);

            // Validar si el usuario tiene permiso para eliminar la tarea
            if (task == null || (task.UserId != userId && !User.IsInRole("Admin")))
            {
                return NotFound("Tarea no encontrada o no pertenece al usuario.");
            }

            try
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "Error al eliminar la tarea: " + ex.Message);
            }

            return NoContent();
        }
    }
}
