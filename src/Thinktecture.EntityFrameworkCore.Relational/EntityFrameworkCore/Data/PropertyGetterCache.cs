using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Builds and caches property getters.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public class PropertyGetterCache : IPropertyGetterCache
   {
      private readonly ILogger<PropertyGetterCache> _logger;
      private readonly ConcurrentDictionary<IProperty, Delegate> _propertyGetterLookup;

      /// <summary>
      /// Initializes new instance of <see cref="PropertyGetterCache"/>.
      /// </summary>
      /// <param name="loggerFactory">Logger factory.</param>
      public PropertyGetterCache(ILoggerFactory loggerFactory)
      {
         _logger = loggerFactory?.CreateLogger<PropertyGetterCache>() ?? throw new ArgumentNullException(nameof(loggerFactory));
         _propertyGetterLookup = new ConcurrentDictionary<IProperty, Delegate>();
      }

      /// <inheritdoc />
      public Func<DbContext, TEntity, object?> GetPropertyGetter<TEntity>(IProperty property)
         where TEntity : class
      {
         return (Func<DbContext, TEntity, object?>)_propertyGetterLookup.GetOrAdd(property, BuildPropertyGetter<TEntity>);
      }

      private Func<DbContext, TEntity, object?> BuildPropertyGetter<TEntity>(IProperty property)
         where TEntity : class
      {
         var hasSqlDefaultValue = property.GetDefaultValueSql() != null;
         var hasDefaultValue = property.GetDefaultValue() != null;

         if ((hasSqlDefaultValue || hasDefaultValue) && !property.IsNullable)
         {
            if (property.ClrType.IsClass)
            {
               _logger.LogWarning("The corresponding column of '{Entity}.{Property}' has a DEFAULT value constraint in the database and is NOT NULL. Depending on the database vendor the .NET value `null` may lead to an exception because the tool for bulk insert of data may prevent sending `null`s for NOT NULL columns. Use 'PropertiesToInsert/PropertiesToUpdate' on corresponding options to specify properties to insert/update and skip '{Entity}.{Property}' so database uses the DEFAULT value.",
                                  property.DeclaringEntityType.ClrType.Name, property.Name, property.DeclaringEntityType.ClrType.Name, property.Name);
            }
            else if (!property.ClrType.IsGenericType ||
                     !property.ClrType.IsGenericTypeDefinition &&
                     property.ClrType.GetGenericTypeDefinition() != typeof(Nullable<>))
            {
               _logger.LogWarning("The corresponding column of '{Entity}.{Property}' has a DEFAULT value constraint in the database and is NOT NULL. Depending on the database vendor the \".NET default values\" (`false`, `0`, `00000000-0000-0000-0000-000000000000` etc.) may lead to unexpected results because these values are sent to the database as-is, i.e. the DEFAULT value constraint will NOT be used by database. Use 'PropertiesToInsert/PropertiesToUpdate' on corresponding options to specify properties to insert and skip '{Entity}.{Property}' so database uses the DEFAULT value.",
                                  property.DeclaringEntityType.ClrType.Name, property.Name, property.DeclaringEntityType.ClrType.Name, property.Name);
            }
         }

         var getter = BuildGetter<TEntity>(property);
         var converter = property.GetValueConverter();

         if (converter != null)
            getter = UseConverter(getter, converter);

         return getter;
      }

      private static Func<DbContext, TEntity, object?> BuildGetter<TEntity>(IProperty property)
         where TEntity : class
      {
         if (property.IsShadowProperty())
         {
            var shadowPropGetter = CreateShadowPropertyGetter(property);
            return shadowPropGetter.GetValue;
         }

         var getter = property.GetGetter();

         if (getter == null)
            throw new ArgumentException($"The property '{property.Name}' of entity '{property.DeclaringEntityType.Name}' has no property getter.");

         return (_, entity) => getter.GetClrValue(entity);
      }

      private static Func<DbContext, T, object?> UseConverter<T>(Func<DbContext, T, object?> getter, ValueConverter converter)
      {
         var convert = converter.ConvertToProvider;

         return (ctx, e) =>
                {
                   var value = getter(ctx, e);

                   if (value != null)
                      value = convert(value);

                   return value;
                };
      }

      private static IShadowPropertyGetter CreateShadowPropertyGetter(IProperty property)
      {
         var currentValueGetter = property.GetPropertyAccessors().CurrentValueGetter;
         var shadowPropGetterType = typeof(ShadowPropertyGetter<>).MakeGenericType(property.ClrType);
         var shadowPropGetter = Activator.CreateInstance(shadowPropGetterType, currentValueGetter)
                                ?? throw new Exception($"Could not create shadow property getter of type '{shadowPropGetterType.ShortDisplayName()}'.");

         return (IShadowPropertyGetter)shadowPropGetter;
      }
   }
}
