using System.ComponentModel.DataAnnotations;

namespace TodoListApi.Models
{
    public class TodoTask
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        public string Description { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public string UserId { get; set; } = string.Empty; // Nuevo campo para asociar la tarea al usuario
    }
}
