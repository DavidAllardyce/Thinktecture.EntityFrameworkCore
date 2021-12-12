using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.TempTables;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="DbContext"/>.
/// </summary>
public static class BulkOperationsDbContextExtensions
{
   /// <summary>
   /// Creates a temp table using custom type '<typeparamref name="T"/>'.
   /// </summary>
   /// <param name="ctx">Database context to use.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Type of custom temp table.</typeparam>
   /// <returns>Table name</returns>
   /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">The provided type <typeparamref name="T"/> is not known by the provided <paramref name="ctx"/>.</exception>
   public static Task<ITempTableReference> CreateTempTableAsync<T>(
      this DbContext ctx,
      ITempTableCreationOptions options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      return ctx.CreateTempTableAsync(typeof(T), options, cancellationToken);
   }

   /// <summary>
   /// Creates a temp table.
   /// </summary>
   /// <param name="ctx">Database context to use.</param>
   /// <param name="type">Type of the entity.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Table name</returns>
   /// <exception cref="ArgumentNullException">
   /// <paramref name="ctx"/> is <c>null</c>
   /// - or  <paramref name="type"/> is <c>null</c>
   /// - or  <paramref name="options"/> is <c>null</c>.
   /// </exception>
   /// <exception cref="ArgumentException">The provided type <paramref name="type"/> is not known by provided <paramref name="ctx"/>.</exception>
   public static Task<ITempTableReference> CreateTempTableAsync(
      this DbContext ctx,
      Type type,
      ITempTableCreationOptions options,
      CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      var entityType = ctx.Model.GetEntityType(type);
      return ctx.GetService<ITempTableCreator>().CreateTempTableAsync(entityType, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="entities"/> into a table.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert.</param>
   /// <param name="propertiesToInsert">Properties to insert. If <c>null</c> then all properties are used.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static async Task BulkInsertAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      Expression<Func<T, object?>>? propertiesToInsert = null,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var bulkInsertExecutor = ctx.GetService<IBulkInsertExecutor>();
      var options = bulkInsertExecutor.CreateOptions(propertiesToInsert is null ? null : EntityPropertiesProvider.From(propertiesToInsert));

      await bulkInsertExecutor.BulkInsertAsync(entities, options, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Copies <paramref name="entities"/> into a table.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static async Task BulkInsertAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      IBulkInsertOptions options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      await ctx.GetService<IBulkInsertExecutor>()
               .BulkInsertAsync(entities, options, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Updates <paramref name="entities"/> in the table.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to update.</param>
   /// <param name="propertiesToUpdate">Properties to update. If <c>null</c> then all properties are used.</param>
   /// <param name="propertiesToMatchOn">Properties to match on. If <c>null</c> then the primary key of the entity is used.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Number of affected rows.</returns>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static async Task<int> BulkUpdateAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      Expression<Func<T, object?>>? propertiesToUpdate = null,
      Expression<Func<T, object?>>? propertiesToMatchOn = null,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var bulkUpdateExecutor = ctx.GetService<IBulkUpdateExecutor>();

      var options = bulkUpdateExecutor.CreateOptions(propertiesToUpdate is null ? null : EntityPropertiesProvider.From(propertiesToUpdate),
                                                     propertiesToMatchOn is null ? null : EntityPropertiesProvider.From(propertiesToMatchOn));

      return await bulkUpdateExecutor.BulkUpdateAsync(entities, options, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Updates <paramref name="entities"/> in the table.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to update.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Number of affected rows.</returns>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static async Task<int> BulkUpdateAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      IBulkUpdateOptions options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      return await ctx.GetService<IBulkUpdateExecutor>()
                      .BulkUpdateAsync(entities, options, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Updates <paramref name="entities"/> that are in the table, the rest will be inserted.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert or update.</param>
   /// <param name="propertiesToInsert">Properties to insert. If <c>null</c> then all properties are used.</param>
   /// <param name="propertiesToUpdate">Properties to update. If <c>null</c> then all properties are used.</param>
   /// <param name="propertiesToMatchOn">Properties to match on. If <c>null</c> then the primary key of the entity is used.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Number of affected rows.</returns>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static async Task<int> BulkInsertOrUpdateAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      Expression<Func<T, object?>>? propertiesToInsert = null,
      Expression<Func<T, object?>>? propertiesToUpdate = null,
      Expression<Func<T, object?>>? propertiesToMatchOn = null,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var bulkOperationExecutor = ctx.GetService<IBulkInsertOrUpdateExecutor>();

      var options = bulkOperationExecutor.CreateOptions(propertiesToInsert is null ? null : EntityPropertiesProvider.From(propertiesToInsert),
                                                        propertiesToUpdate is null ? null : EntityPropertiesProvider.From(propertiesToUpdate),
                                                        propertiesToMatchOn is null ? null : EntityPropertiesProvider.From(propertiesToMatchOn));

      return await bulkOperationExecutor.BulkInsertOrUpdateAsync(entities, options, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Updates <paramref name="entities"/> that are in the table, the rest will be inserted.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert or update.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Number of affected rows.</returns>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static async Task<int> BulkInsertOrUpdateAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      IBulkInsertOrUpdateOptions options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      return await ctx.GetService<IBulkInsertOrUpdateExecutor>()
                      .BulkInsertOrUpdateAsync(entities, options, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Copies <paramref name="values"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="values">Values to insert.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="TColumn1">Type of the values to insert.</typeparam>
   /// <returns>A query for accessing the inserted values.</returns>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="values"/> is <c>null</c>.</exception>
   public static Task<ITempTableQuery<TColumn1>> BulkInsertValuesIntoTempTableAsync<TColumn1>(
      this DbContext ctx,
      IEnumerable<TColumn1> values,
      ITempTableBulkInsertOptions? options = null,
      CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(values);

      var executor = ctx.GetService<ITempTableBulkInsertExecutor>();
      options ??= executor.CreateOptions();

      return executor.BulkInsertValuesIntoTempTableAsync(values, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="values"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="values">Values to insert.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="TColumn1">Type of the column 1.</typeparam>
   /// <typeparam name="TColumn2">Type of the column 2.</typeparam>
   /// <returns>A query for accessing the inserted values.</returns>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="values"/> is <c>null</c>.</exception>
   public static Task<ITempTableQuery<TempTable<TColumn1, TColumn2>>> BulkInsertValuesIntoTempTableAsync<TColumn1, TColumn2>(
      this DbContext ctx,
      IEnumerable<(TColumn1 column1, TColumn2 column2)> values,
      ITempTableBulkInsertOptions? options = null,
      CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(values);

      var entities = values.Select(t => new TempTable<TColumn1, TColumn2>(t.column1, t.column2));

      return ctx.BulkInsertIntoTempTableAsync(entities, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="entities"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert.</param>
   /// <param name="propertiesToInsert">Properties to insert. If <c>null</c> then all properties are used.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <returns>A query for accessing the inserted values.</returns>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      Expression<Func<T, object?>>? propertiesToInsert = null,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var executor = ctx.GetService<ITempTableBulkInsertExecutor>();
      var options = executor.CreateOptions(propertiesToInsert is null ? null : EntityPropertiesProvider.From(propertiesToInsert));

      return executor.BulkInsertIntoTempTableAsync(entities, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="entities"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <returns>A query for accessing the inserted values.</returns>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      ITempTableBulkInsertOptions? options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var executor = ctx.GetService<ITempTableBulkInsertExecutor>();
      options ??= executor.CreateOptions();

      return executor.BulkInsertIntoTempTableAsync(entities, options, cancellationToken);
   }

   /// <summary>
   /// Truncates the table of the entity of type <typeparamref name="T"/>.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Type of the entity to truncate.</typeparam>
   public static Task TruncateTableAsync<T>(
      this DbContext ctx,
      CancellationToken cancellationToken = default)
      where T : class
   {
      return ctx.GetService<ITruncateTableExecutor>()
                .TruncateTableAsync<T>(cancellationToken);
   }
}
