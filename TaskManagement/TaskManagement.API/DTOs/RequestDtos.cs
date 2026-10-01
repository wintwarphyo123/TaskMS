namespace TaskManagement.API.DTOs
{
    public class RequestDtos
    {
        public string Name { get; set; } = string.Empty;
    }
    public class TaskRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int CategoryId { get; set; }
    }
    public class UpdateTaskRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Todo"; // Todo, InProgress, Done
        //public int UserId { get; set; }
        public int CategoryId { get; set; }
    }
}
