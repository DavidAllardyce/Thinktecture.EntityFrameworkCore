using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.Database;
using Thinktecture.EntityFrameworkCore;

namespace Thinktecture
{
   public class SamplesContext
   {
      private static readonly Lazy<SamplesContext> _lazy = new Lazy<SamplesContext>(CreateTestConfiguration);

      public static SamplesContext Instance => _lazy.Value;

      public IConfiguration Configuration { get; }

      public string ConnectionString => Configuration.GetConnectionString("default");

      [NotNull]
      private static SamplesContext CreateTestConfiguration()
      {
         var config = GetConfiguration();
         return new SamplesContext(config);
      }

      public SamplesContext([NotNull] IConfiguration config)
      {
         Configuration = config ?? throw new ArgumentNullException(nameof(config));
      }

      private static IConfiguration GetConfiguration()
      {
         return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
      }

      public IServiceProvider CreateServiceProvider([CanBeNull] string schema = null)
      {
         var services = new ServiceCollection()
            .AddDbContext<DemoDbContext>(builder => builder
                                                    .UseSqlServer(ConnectionString, sqlOptions =>
                                                                                    {
                                                                                       if (schema != null)
                                                                                          sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", schema);

                                                                                       sqlOptions.AddRowNumberSupport()
                                                                                                 .AddTempTableSupport()
                                                                                                 .UseThinktectureSqlServerMigrationsSqlGenerator();
                                                                                    })
                                                    .AddSchemaAwareComponents());

         if (schema != null)
            services.AddSingleton<IDbContextSchema>(new DbContextSchema(schema));

         return services.BuildServiceProvider();
      }
   }
}
