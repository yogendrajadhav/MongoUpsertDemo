using MongoDB.Bson;
using MongoDB.Driver;
using MongoUpsertDemo.Models;

namespace MongoUpsertDemo.Services;

/// <summary>
/// Service that provides access to the Books collection in MongoDB.
/// Registered as a singleton in DI and used by controllers to read/write books.
/// </summary>
public class BookService
{
  // MongoDB collection wrapper for the Book documents.
  // IMongoCollection<T> provides CRUD operations for the collection.
  private readonly IMongoCollection<Book> _booksCollection;

  /// <summary>
  /// Construct the service using configuration to obtain the MongoDB connection string.
  /// </summary>
  /// <param name="config">Application configuration (injected by DI)</param>
  public BookService(IConfiguration config)
  {
    // Create a MongoClient using the connection string named "MongoDb" from appsettings.
    // If the connection string is not present or invalid, the MongoClient may throw when used.
    var mongoClient = new MongoClient(config.GetConnectionString("MongoDb"));

    // Get (or create) the "BookStore" database reference.
    var mongoDatabase = mongoClient.GetDatabase("BookStore");

    // Get the "Books" collection from the database and map it to the Book model.
    _booksCollection = mongoDatabase.GetCollection<Book>("Books"); 
  }

  /// <summary>
  /// Upsert (update if exists, otherwise insert) a book document.
  /// </summary>
  /// <remarks>
  /// If the Book.Id is empty, a new ObjectId string is generated.
  /// ReplaceOneAsync with IsUpsert=true will insert the document when the filter matches no documents.
  /// </remarks>
  /// <param name="book">The book to upsert.</param>
  public async Task UpsertBookAsync(Book book)
  {
    // Ensure the book has an Id so Mongo stores it as an ObjectId string.
    if (string.IsNullOrEmpty(book.Id))
    {
      // Generate a new BSON ObjectId and assign its string representation to the model.
      book.Id = ObjectId.GenerateNewId().ToString();
    }

    // Build a filter to match a document by its Id property.
    var filter = Builders<Book>.Filter.Eq(b => b.Id, book.Id);
    
    // Configure replace options to perform an upsert (insert when no match found).
    var options = new ReplaceOptions { IsUpsert = true };

    // Perform the replace (upsert) operation. If a document with the same Id exists, it will be replaced;
    // otherwise, the document will be inserted.
    await _booksCollection.ReplaceOneAsync(filter, book, options);
  }

  /// <summary>
  /// Retrieve all books from the collection.
  /// </summary>
  /// <returns>List of Book documents.</returns>
  public async Task<List<Book>> GetBooksAsync()
  {
    // Find all documents in the collection and return them as a list.
    return await _booksCollection.Find(_ => true).ToListAsync();
  }
}