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

  // Partial Upsert: Only update provided fields
  public async Task UpsertPartialAsync(Book book) 
  {
    // Ensure the book has an Id so Mongo stores it as an ObjectId string.
    if (string.IsNullOrEmpty(book.Id))
    {
      // Generate a new BSON ObjectId and assign its string representation to the model.
      book.Id = ObjectId.GenerateNewId().ToString();
    }

    var filter = Builders<Book>.Filter.Eq(b => b.Id, book.Id);
    var update =Builders<Book>.Update.Set(b=>b.Title,book.Title).Set(b => b.Author, book.Author).Set(b => b.Price, book.Price);
    var options = new UpdateOptions { IsUpsert = true };
    // isUpsert = true means it will insert if no match found
    await _booksCollection.UpdateOneAsync(filter, update, options);
  }

  // Soft delete method
  public async Task<bool> SoftDeleteAsync(string id, string deletedBy)
  {
    var filter = Builders<Book>.Filter.Eq(b => b.Id, id);
    var update = Builders<Book>.Update
      .Set(b => b.IsDeleted, true)
      .Set(b => b.DeletedAt, DateTime.UtcNow)
      .Set(b => b.DeletedBy, deletedBy); 


    var result = await _booksCollection.UpdateOneAsync(filter, update);

    return result.ModifiedCount > 0;
  }

  // Get all non-deleted books
  public async Task<List<Book>> GetAsync()
  {
    return await _booksCollection.Find(b => !b.IsDeleted).ToListAsync();
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

  // Get a book by Id if not deleted
  public async Task<Book?> GetByIdAsync(string id)
  {
    return await _booksCollection.Find(b => b.Id == id && !b.IsDeleted).FirstOrDefaultAsync();
  }

  // (Optional) Restore soft-deleted book
  public async Task<bool> RestoreAsync(string id)
  {
    var filter = Builders<Book>.Filter.Eq(b => b.Id, id);
    var update = Builders<Book>.Update
      .Set(b => b.IsDeleted, false)
      .Unset(b => b.DeletedAt)
      .Unset(b => b.DeletedBy);


    var result = await _booksCollection.UpdateOneAsync(filter, update);
    return result.ModifiedCount > 0;
  }

}