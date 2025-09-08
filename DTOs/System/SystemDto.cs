
namespace Starter.Api.DTOs.System;

public class SystemDto
{
    public int Id { get; set; }   
    public DateTime SysCurrentDate { get; set; }   
    public DateTime? SysClosedDate { get; set; }   
    public string? SysName { get; set; }
}
