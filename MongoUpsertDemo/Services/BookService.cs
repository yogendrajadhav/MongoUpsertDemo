using MongoDB.Bson;
using MongoDB.Driver;
using MongoUpsertDemo.Models;

namespace MongoUpsertDemo.Services;

public class BookService
{
  private readonly IMongoCollection<Book> _booksCollection;

  public BookService(IConfiguration config)
  {
    var mongoClient = new MongoClient(config.GetConnectionString("MongoDb"));
    var mongoDatabase = mongoClient.GetDatabase("BookStore");
    _booksCollection = mongoDatabase.GetCollection<Book>("Books"); 
  }
  // Upsert (Update or Insert)
  public async Task UpsertBookAsync(Book book)
  {
    if (string.IsNullOrEmpty(book.Id))
    {
      book.Id = ObjectId.GenerateNewId().ToString();
    }

    var filter = Builders<Book>.Filter.Eq(b => b.Id, book.Id);
    
    var options = new ReplaceOptions { IsUpsert = true };
    // ReplaceOneAsync with isUpsert = true performs the upsert operation
   
    await _booksCollection.ReplaceOneAsync(filter, book, options);

  }

  public async Task<List<Book>> GetBooksAsync()
  {
    return await _booksCollection.Find(_ => true).ToListAsync();
  }
}