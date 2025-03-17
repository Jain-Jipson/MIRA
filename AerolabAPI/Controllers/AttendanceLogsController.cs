using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class AttendanceLogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AttendanceLogsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/attendancelogs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AttendanceLog>>> GetAttendanceLogs()
    {
        return await _context.AttendanceLogs.ToListAsync();
    }

    // GET: api/attendancelogs/5
    [HttpGet("{id}")]
    public async Task<ActionResult<AttendanceLog>> GetAttendanceLog(int id)
    {
        var log = await _context.AttendanceLogs.FindAsync(id);
        if (log == null)
        {
            return NotFound();
        }
        return log;
    }

    // POST: api/attendancelogs (Standard Manual Entry)
    [HttpPost]
    public async Task<ActionResult<AttendanceLog>> PostAttendanceLog(AttendanceLog log)
    {
        _context.AttendanceLogs.Add(log);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAttendanceLog), new { id = log.Id }, log);
    }

    // PUT: api/attendancelogs/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutAttendanceLog(int id, AttendanceLog log)
    {
        if (id != log.Id)
        {
            return BadRequest();
        }

        _context.Entry(log).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/attendancelogs/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAttendanceLog(int id)
    {
        var log = await _context.AttendanceLogs.FindAsync(id);
        if (log == null)
        {
            return NotFound();
        }

        _context.AttendanceLogs.Remove(log);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ✅ RFID Attendance Logging (ESP32 will send POST requests here)
[HttpPost("log")]
[Consumes("application/json")]
public async Task<IActionResult> LogAttendance([FromBody] RFIDScanRequest request)
{
    if (request == null || string.IsNullOrEmpty(request.RFID))
    {
        return BadRequest(new { message = "Invalid request, RFID is required" });
    }

    // ✅ Find Employee by RFID
    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.RFID == request.RFID);
    if (employee == null)
    {
        return NotFound(new { message = "RFID not registered." });
    }

    // ✅ Fetch Last Attendance Entry
    var lastLog = await _context.AttendanceLogs
        .Where(l => l.EmployeeId == employee.Id)
        .OrderByDescending(l => l.CheckInTime)
        .FirstOrDefaultAsync();

    if (lastLog == null || lastLog.CheckOutTime != null)
    {
        // ✅ Log Check-In
        var newLog = new AttendanceLog
        {
            EmployeeId = employee.Id,
            CheckInTime = DateTime.UtcNow
        };
        _context.AttendanceLogs.Add(newLog);
    }
    else
    {
        // ✅ Log Check-Out
        lastLog.CheckOutTime = DateTime.UtcNow;
    }

    await _context.SaveChangesAsync();
    return Ok(new { message = "Attendance logged successfully.", EmployeeId = employee.Id });
}

}
