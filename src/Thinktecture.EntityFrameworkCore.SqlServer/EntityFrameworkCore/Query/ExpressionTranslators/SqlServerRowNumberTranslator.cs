using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Thinktecture.EntityFrameworkCore.Query.Expressions;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators
{
   /// <summary>
   /// Translated extension method "RowNumber"
   /// </summary>
   public class SqlServerRowNumberTranslator : IMethodCallTranslator
   {
      private static readonly MethodInfo _rowNumberWithPartitionByMethod;
      private static readonly MethodInfo _rowNumberMethod;
      private static readonly MethodInfo _descendingMethodInfo;

      static SqlServerRowNumberTranslator()
      {
         _rowNumberWithPartitionByMethod = typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.RowNumber), new[] { typeof(DbFunctions), typeof(object), typeof(object) });
         _rowNumberMethod = typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.RowNumber), new[] { typeof(DbFunctions), typeof(object) });
         _descendingMethodInfo = typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.Descending), BindingFlags.Public | BindingFlags.Static);
      }

      /// <inheritdoc />
      [NotNull]
      public Expression Translate(MethodCallExpression methodCallExpression)
      {
         if (methodCallExpression.Method == _rowNumberMethod)
         {
            var orderByParams = ExtractParam(methodCallExpression.Arguments[1]);
            var orderBy = ConvertOrderBy(orderByParams);

            return new RowNumberExpression(Array.Empty<Expression>(), orderBy);
         }

         if (methodCallExpression.Method == _rowNumberWithPartitionByMethod)
         {
            var partitionBy = ExtractParam(methodCallExpression.Arguments[1]);
            var orderByParams = ExtractParam(methodCallExpression.Arguments[2]);
            var orderBy = ConvertOrderBy(orderByParams);

            return new RowNumberExpression(partitionBy, orderBy);
         }

         if (methodCallExpression.Method == _descendingMethodInfo)
         {
            var column = methodCallExpression.Arguments[1];
            return new DescendingExpression(column);
         }

         return methodCallExpression;
      }

      [NotNull]
      private static ReadOnlyCollection<Expression> ExtractParam([NotNull] Expression parameter)
      {
         if (typeof(IEnumerable<Expression>).IsAssignableFrom(parameter.Type) && parameter is ConstantExpression constant)
            return ((IEnumerable<Expression>)constant.Value).ToList().AsReadOnly();

         return new List<Expression> { parameter }.AsReadOnly();
      }

      [NotNull]
      private static IReadOnlyCollection<Expression> ConvertOrderBy([NotNull] IEnumerable<Expression> orderByParams)
      {
         return orderByParams.Select(ConvertOrderBy).ToList().AsReadOnly();
      }

      [NotNull]
      private static Expression ConvertOrderBy([NotNull] Expression expression)
      {
         if (expression is DescendingExpression)
            return expression;

         return ExtractOrderBy(expression);
      }

      [NotNull]
      private static Expression ExtractOrderBy([NotNull] Expression expression)
      {
         if (expression is ColumnExpression)
            return expression;

         if (expression.NodeType == ExpressionType.Convert)
            return ExtractOrderBy(((UnaryExpression)expression).Operand);

         if (expression is MethodCallExpression methodCall && methodCall.Method == _descendingMethodInfo)
            return new DescendingExpression(methodCall.Arguments[1]);

         throw new Exception($"Unexpected 'order by' expression. Type: {expression.GetType().DisplayName()}.");
      }
   }
}