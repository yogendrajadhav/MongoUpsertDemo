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
    // If the connection string is not present or invalid, MongoClient will still construct but operations may fail.
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
  /// Partial Upsert: updates only specified fields, inserts when no match is found.
  /// </summary>
  /// <remarks>
  /// Uses UpdateOneAsync with UpdateOptions.IsUpsert=true so only the provided fields are set on insert/update.
  /// This avoids replacing the entire document when only a subset of fields should change.
  /// </remarks>
  /// <param name="book">The book containing fields to set. Id must be set or will be generated.</param>
  public async Task UpsertPartialAsync(Book book) 
  {
    // Ensure the book has an Id so Mongo stores it as an ObjectId string.
    if (string.IsNullOrEmpty(book.Id))
    {
      // Generate a new BSON ObjectId and assign its string representation to the model.
      book.Id = ObjectId.GenerateNewId().ToString();
    }

    // Match by Id.
    var filter = Builders<Book>.Filter.Eq(b => b.Id, book.Id);

    // Create an update definition that sets Title, Author and Price fields.
    var update = Builders<Book>.Update
      .Set(b => b.Title, book.Title)
      .Set(b => b.Author, book.Author)
      .Set(b => b.Price, book.Price);

    // If no document matches the filter, insert a new one with the provided fields.
    var options = new UpdateOptions { IsUpsert = true };

    // Apply the update (or insert).
    await _booksCollection.UpdateOneAsync(filter, update, options);
  }

  /// <summary>
  /// Soft delete a book by setting IsDeleted flag and audit fields.
  /// </summary>
  /// <param name="id">Book Id to soft-delete.</param>
  /// <param name="deletedBy">User or process performing the delete.</param>
  /// <returns>True if a document was modified (soft-deleted).</returns>
  public async Task<bool> SoftDeleteAsync(string id, string deletedBy)
  {
    // Match by Id.
    var filter = Builders<Book>.Filter.Eq(b => b.Id, id);

    // Set the soft-delete marker and audit metadata.
    var update = Builders<Book>.Update
      .Set(b => b.IsDeleted, true)
      .Set(b => b.DeletedAt, DateTime.UtcNow)
      .Set(b => b.DeletedBy, deletedBy); 

    // Execute the update and return whether a document was modified.
    var result = await _booksCollection.UpdateOneAsync(filter, update);

    return result.ModifiedCount > 0;
  }

  /// <summary>
  /// Get all non-deleted books.
  /// </summary>
  /// <returns>List of non-deleted Book documents.</returns>
  public async Task<List<Book>> GetAsync()
  {
    // Filter out soft-deleted documents.
    return await _booksCollection.Find(b => !b.IsDeleted).ToListAsync();
  }

  /// <summary>
  /// Retrieve all books from the collection (including deleted ones).
  /// </summary>
  /// <returns>List of Book documents.</returns>
  public async Task<List<Book>> GetBooksAsync()
  {
    // Find all documents in the collection and return them as a list.
    return await _booksCollection.Find(_ => true).ToListAsync();
  }

  /// <summary>
  /// Get a single non-deleted book by its Id.
  /// </summary>
  /// <param name="id">The book's Id.</param>
  /// <returns>The Book if found and not deleted; otherwise null.</returns>
  public async Task<Book?> GetByIdAsync(string id)
  {
    // Find the document that matches the Id and is not soft-deleted.
    return await _booksCollection.Find(b => b.Id == id && !b.IsDeleted).FirstOrDefaultAsync();
  }

  /// <summary>
  /// Restore a previously soft-deleted book.
  /// </summary>
  /// <param name="id">The book's Id to restore.</param>
  /// <returns>True if a document was modified (restored).</returns>
  public async Task<bool> RestoreAsync(string id)
  {
    // Match by Id.
    var filter = Builders<Book>.Filter.Eq(b => b.Id, id);

    // Unset the soft-delete fields and clear audit info.
    var update = Builders<Book>.Update
      .Set(b => b.IsDeleted, false)
      .Unset(b => b.DeletedAt)
      .Unset(b => b.DeletedBy);

    var result = await _booksCollection.UpdateOneAsync(filter, update);
    return result.ModifiedCount > 0;
  }

}