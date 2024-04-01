using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ActiveQueryBuilder.Core;
using ActiveQueryBuilder.Web.Server;
using ActiveQueryBuilder.Web.Server.Infrastructure.Factories;
using ActiveQueryBuilder.Web.Server.Services;

namespace DemoSite.Services
{
    public class AlternateNamesService
    {
        private readonly IQueryBuilderService _queryBuilderService;

        public AlternateNamesService(IQueryBuilderService queryBuilderService)
        {
            _queryBuilderService = queryBuilderService;
        }

        public QueryBuilder CreateQueryBuilder(string name)
        {
            var qb = _queryBuilderService.Get(name);

            if (qb == null)
                qb = Create(name);

            return qb;
        }

        private QueryBuilder Create(string name)
        {
            // Create an instance of the QueryBuilder object
            var queryBuilder = _queryBuilderService.Create(name);
            queryBuilder.SyntaxProvider = new DB2SyntaxProvider();
            queryBuilder.BehaviorOptions.AllowSleepMode = true;

            // Turn displaying of alternate names on in the text of result SQL query and in the visual UI
            queryBuilder.SQLFormattingOptions.UseAltNames = true;
            queryBuilder.MetadataLoadingOptions.OfflineMode = true;

            using (var stream = GetType().Assembly.GetManifestResourceStream("DemoSite.db2_sample_with_alt_names.xml"))
                queryBuilder.MetadataContainer.ImportFromXML(stream);

            queryBuilder.SQLQuery.SQLUpdated += (sender, args) => OnSQLUpdated(name);

            //Set default query
            queryBuilder.SQL = GetDefaultSql();

            return queryBuilder;
        }

        private string GetDefaultSql()
        {
            return @"Select ""Employees"".""Employee ID"", ""Employees"".""First Name"", ""Employees"".""Last Name"", ""Employee Photos"".""Photo Image"", ""Employee Resumes"".Resume From ""Employee Photos"" Inner Join
            ""Employees"" On ""Employee Photos"".""Employee ID"" = ""Employees"".""Employee ID"" Inner Join
            ""Employee Resumes"" On ""Employee Resumes"".""Employee ID"" = ""Employees"".""Employee ID""";
        }

        public void OnSQLUpdated(string name)
        {
            var qb = _queryBuilderService.Get(name);

            var opts = new SQLFormattingOptions();

            opts.Assign(qb.SQLFormattingOptions);
            opts.KeywordFormat = KeywordFormat.UpperCase;

            // get SQL query with real object names
            opts.UseAltNames = false;
            var plainSql = FormattedSQLBuilder.GetSQL(qb.SQLQuery.QueryRoot, opts);

            // prepare additional data to be sent to the client
            qb.ExchangeData = new
            {
                sql = plainSql,
                SessionID = name
            };
        }

        public void Toggle(string name)
        {
            var qb = _queryBuilderService.Get(name);
            qb.SQLFormattingOptions.UseAltNames = !qb.SQLFormattingOptions.UseAltNames;
            qb.MetadataStructure.Refresh();
        }
    }
}
