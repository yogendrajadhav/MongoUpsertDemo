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
      return await _bookService.GetBooksAsync();
    }

    [HttpPost("upsert")]
    public async Task<IActionResult> UpsertBook(Book book)
    {
      await _bookService.UpsertBookAsync(book);
      return Ok(new { message = "Book upserted successfully." });
    }
  }
}
