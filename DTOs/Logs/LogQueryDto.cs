namespace Starter.Api.DTOs.Logs;

public class LogQueryDto
{
    public string? User { get; set; }          // busca por username OU name (contains)
    public DateTime? StartDate { get; set; }   // data somente (yyyy-MM-dd)
    public DateTime? EndDate { get; set; }     // data somente (yyyy-MM-dd)
}
