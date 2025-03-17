public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RFID { get; set; } = string.Empty;  // Unique RFID Code
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
