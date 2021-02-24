using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable InconsistentNaming

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests
{
   // ReSharper disable once InconsistentNaming
   public class BulkUpdateAsync : IntegrationTestsBase
   {
      private SqliteBulkOperationExecutor? _sut;

      private SqliteBulkOperationExecutor SUT => _sut ??= ActDbContext.GetService<SqliteBulkOperationExecutor>();

      public BulkUpdateAsync(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public void Should_not_throw_if_entities_is_empty()
      {
         SUT.Invoking(sut => sut.BulkUpdateAsync(new List<TestEntity>(), new SqliteBulkUpdateOptions()))
            .Should().NotThrow();
      }

      [Fact]
      public void Should_not_throw_if_key_property_is_not_part_of_PropertiesToUpdate()
      {
         var propertiesProvider = EntityPropertiesProvider.From<TestEntity>(entity => new { entity.Name });

         SUT.Invoking(sut => sut.BulkUpdateAsync(new List<TestEntity>(), new SqliteBulkUpdateOptions
                                                                         {
                                                                            PropertiesToUpdate = propertiesProvider
                                                                         }))
            .Should().NotThrow();
      }

      [Fact]
      public async Task Should_update_column_with_converter()
      {
         var entity = new TestEntity();
         ArrangeDbContext.Add(entity);
         await ArrangeDbContext.SaveChangesAsync();

         entity.ConvertibleClass = new ConvertibleClass(43);

         await SUT.BulkUpdateAsync(new List<TestEntity> { entity }, new SqliteBulkUpdateOptions());

         var loadedEntity = AssertDbContext.TestEntities.Single();
         loadedEntity.ConvertibleClass.Should().NotBeNull();
         loadedEntity.ConvertibleClass!.Key.Should().Be(43);
      }

      [Fact]
      public async Task Should_update_entities()
      {
         var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
         var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE") };
         ArrangeDbContext.AddRange(entity_1, entity_2);
         await ArrangeDbContext.SaveChangesAsync();

         entity_1.Name = "Name";
         entity_1.Count = 1;
         entity_2.Name = "OtherName";
         entity_2.Count = 2;

         await SUT.BulkUpdateAsync(new[] { entity_1, entity_2 }, new SqliteBulkUpdateOptions());

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(2)
                       .And.Subject
                       .Should().BeEquivalentTo(entity_1, entity_2);
      }

      [Fact]
      public async Task Should_update_entities_based_on_non_pk_property()
      {
         var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "value" };
         var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = null };
         ArrangeDbContext.AddRange(entity_1, entity_2);
         await ArrangeDbContext.SaveChangesAsync();

         entity_1.Name = null;
         entity_1.Count = 1;
         entity_2.Name = "value";
         entity_2.Count = 2;

         var properties = EntityPropertiesProvider.From<TestEntity>(e => new { e.Name, e.Count });
         var keyProperties = EntityPropertiesProvider.From<TestEntity>(e => e.Name);
         await SUT.BulkUpdateAsync(new[] { entity_1, entity_2 }, new SqliteBulkUpdateOptions { PropertiesToUpdate = properties, KeyProperties = keyProperties });

         entity_1.Name = "value";
         entity_2.Name = null;

         entity_1.Count = 2;
         entity_2.Count = 1;

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(2)
                       .And.Subject
                       .Should().BeEquivalentTo(entity_1, entity_2);
      }

      [Fact]
      public async Task Should_update_provided_entity_only()
      {
         var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
         var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE") };
         ArrangeDbContext.AddRange(entity_1, entity_2);
         await ArrangeDbContext.SaveChangesAsync();

         entity_1.Name = "Name";
         entity_1.Count = 1;
         entity_2.Name = "OtherName";
         entity_2.Count = 2;

         await SUT.BulkUpdateAsync(new[] { entity_1 }, new SqliteBulkUpdateOptions());

         entity_2.Name = default;
         entity_2.Count = default;

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(2)
                       .And.Subject
                       .Should().BeEquivalentTo(entity_1, entity_2);
      }

      [Fact]
      public async Task Should_update_private_property()
      {
         var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
         ArrangeDbContext.Add(entity);
         await ArrangeDbContext.SaveChangesAsync();

         entity.SetPrivateField(1);

         await SUT.BulkUpdateAsync(new[] { entity }, new SqliteBulkUpdateOptions());

         var loadedEntity = await AssertDbContext.TestEntities.FirstOrDefaultAsync();
         loadedEntity.GetPrivateField().Should().Be(1);
      }

      [Fact]
      public async Task Should_update_shadow_properties()
      {
         var entity = new TestEntityWithShadowProperties { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
         ArrangeDbContext.Add(entity);
         await ArrangeDbContext.SaveChangesAsync();

         ActDbContext.Entry(entity).Property("ShadowStringProperty").CurrentValue = "value";
         ActDbContext.Entry(entity).Property("ShadowIntProperty").CurrentValue = 42;

         await SUT.BulkUpdateAsync(new[] { entity }, new SqliteBulkUpdateOptions());

         var loadedEntity = await AssertDbContext.TestEntitiesWithShadowProperties.FirstOrDefaultAsync();
         AssertDbContext.Entry(loadedEntity).Property("ShadowStringProperty").CurrentValue.Should().Be("value");
         AssertDbContext.Entry(loadedEntity).Property("ShadowIntProperty").CurrentValue.Should().Be(42);
      }

      [Fact]
      public async Task Should_write_not_nullable_structs_as_is_despite_sql_default_value()
      {
         var entity = new TestEntityWithSqlDefaultValues
                      {
                         Id = new Guid("53A5EC9B-BB8D-4B9D-9136-68C011934B63"),
                         Int = 1,
                         String = "value",
                         NullableInt = 2,
                         NullableString = "otherValue"
                      };
         ArrangeDbContext.Add(entity);
         await ArrangeDbContext.SaveChangesAsync();

         entity.Int = 0;
         entity.String = "other value";
         entity.NullableInt = null;
         entity.NullableString = null;

         await SUT.BulkUpdateAsync(new[] { entity }, new SqliteBulkUpdateOptions());

         var loadedEntity = await AssertDbContext.TestEntitiesWithDefaultValues.FirstOrDefaultAsync();
         loadedEntity.Should().BeEquivalentTo(new TestEntityWithSqlDefaultValues
                                              {
                                                 Id = new Guid("53A5EC9B-BB8D-4B9D-9136-68C011934B63"),
                                                 Int = 0,            // persisted as-is
                                                 NullableInt = null, // persisted as-is
                                                 String = "other value",
                                                 NullableString = null // persisted as-is
                                              });
      }

      [Fact]
      public async Task Should_update_specified_properties_only()
      {
         var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "original value" };
         ArrangeDbContext.Add(entity);
         await ArrangeDbContext.SaveChangesAsync();

         entity.Name = "Name"; // this would not be updated
         entity.Count = 42;
         entity.PropertyWithBackingField = 7;
         entity.SetPrivateField(3);

         var idProperty = typeof(TestEntity).GetProperty(nameof(TestEntity.Id)) ?? throw new Exception($"Property {nameof(TestEntity.Id)} not found.");
         var countProperty = typeof(TestEntity).GetProperty(nameof(TestEntity.Count)) ?? throw new Exception($"Property {nameof(TestEntity.Count)} not found.");
         var propertyWithBackingField = typeof(TestEntity).GetProperty(nameof(TestEntity.PropertyWithBackingField)) ?? throw new Exception($"Property {nameof(TestEntity.PropertyWithBackingField)} not found.");
         var privateField = typeof(TestEntity).GetField("_privateField", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new Exception($"Field _privateField not found.");

         await SUT.BulkUpdateAsync(new[] { entity },
                                   new SqliteBulkUpdateOptions
                                   {
                                      PropertiesToUpdate = new EntityPropertiesProvider(new MemberInfo[]
                                                                                  {
                                                                                     idProperty,
                                                                                     countProperty,
                                                                                     propertyWithBackingField,
                                                                                     privateField
                                                                                  })
                                   });

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(1);
         var loadedEntity = loadedEntities[0];
         loadedEntity.Should().BeEquivalentTo(new TestEntity
                                              {
                                                 Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                                 Count = 42,
                                                 PropertyWithBackingField = 7,
                                                 Name = "original value"
                                              });
         loadedEntity.GetPrivateField().Should().Be(3);
      }
   }
}