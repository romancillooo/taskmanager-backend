using System.ComponentModel.DataAnnotations;

namespace TodoListApi.Models
{
    public class TaskDto
    {
        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [MaxLength(100, ErrorMessage = "La descripción no puede exceder los 100 caracteres.")]
        public string Description { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }
    }
}
