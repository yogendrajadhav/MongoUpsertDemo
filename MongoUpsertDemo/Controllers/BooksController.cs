using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoUpsertDemo.Models;
using MongoUpsertDemo.Services;

namespace MongoUpsertDemo.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class BooksController : ControllerBase
  {
    private readonly BookService _bookService;

    public BooksController(BookService bookService)
    {
      _bookService = bookService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Book>>> GetBooks()
    {
      return await _bookService.GetAsync();
    }

    [HttpPost("upsert")]
    public async Task<IActionResult> UpsertBook(Book book)
    {
      await _bookService.UpsertBookAsync(book);
      return Ok(new { message = "Book upserted successfully." });
    }

    [HttpPost("upsert-partial")]
    public async Task<IActionResult> UpsertPartial(Book book)
    {
      await _bookService.UpsertPartialAsync(book);
      return Ok(new { message = "Book upserted (partial update) successfully." });
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<Book>> GetById(string id)
    {
      var book = await _bookService.GetByIdAsync(id);
      if (book == null)
        return NotFound();
      return Ok(book);
    }

// Soft delete with audit info
   [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> SoftDelete(string id,[FromQuery] string deletedBy = "system")
    {
      var deleted = await _bookService.SoftDeleteAsync(id, deletedBy);
      if (!deleted)
        return NotFound(new { message = "Book not found or already deleted." });

      return Ok(new { message = "Book soft-deleted successfully." });
    }

    // Restore
    [HttpPut("restore/{id:length(24)}")]
    public async Task<IActionResult> Restore(string id)
    {
      var restored = await _bookService.RestoreAsync(id);
      if (!restored)
        return NotFound(new { message = "Book not found or already active." });

      return Ok(new { message = "Book restored successfully." });
    }

  }
}
