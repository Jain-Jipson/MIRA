using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/employees")]
[ApiController]
public class EmployeesController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;

    public EmployeesController(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpClient = new HttpClient();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
    {
        return await _context.Employees.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Employee>> PostEmployee(Employee employee)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEmployees), new { id = employee.Id }, employee);
    }

    [HttpDelete("{id}")]
public async Task<IActionResult> DeleteEmployee(int id)
{
    var employee = await _context.Employees.FindAsync(id);
    if (employee == null)
    {
        return NotFound();
    }

    _context.Employees.Remove(employee);
    await _context.SaveChangesAsync();

    return NoContent();
}


    // ✅ Move Servo when Employee Button is Clicked
    [HttpPost("move-motor")]
public async Task<IActionResult> MoveMotor([FromBody] MoveMotorRequest request)
{
    if (request == null || request.EmployeeId <= 0)
    {
        return BadRequest(new { message = "Invalid Employee ID." });
    }

    var jsonData = System.Text.Json.JsonSerializer.Serialize(request);
    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

    var response = await _httpClient.PostAsync("http://192.168.1.6/move-servo", content);

    if (response.IsSuccessStatusCode)
        return Ok(new { message = "Servo moved successfully!" });

    return BadRequest(new { message = "Failed to move servo." });
}

// ✅ Create a Request Model for MoveMotor
public class MoveMotorRequest
{
    public int EmployeeId { get; set; }
}

}
