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
    public class VirtualObjectsService
    {
        private readonly IQueryBuilderService _queryBuilderService;

        public VirtualObjectsService(IQueryBuilderService queryBuilderService)
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
            queryBuilder.SyntaxProvider = new MSSQLSyntaxProvider();
            queryBuilder.MetadataLoadingOptions.OfflineMode = true;

            // Turn this property on to suppress parsing error messages when user types non-SELECT statements in the text editor.
            queryBuilder.BehaviorOptions.AllowSleepMode = true;

            using (var stream = GetType().Assembly.GetManifestResourceStream("DemoSite.Northwind.xml"))
                queryBuilder.MetadataContainer.ImportFromXML(stream);

            MetadataObject o;
            MetadataField f;

            // Virtual fields for real object
            // ===========================================================================
            o = queryBuilder.MetadataContainer.FindItem<MetadataObject>("Orders");

            // first test field - simple expression
            f = o.AddField("OrderId_plus_1");
            f.Expression = "orders.OrderId + 1";

            // second test field - correlated sub-query
            f = o.AddField("CustomerCompanyName");
            f.Expression = "(select c.CompanyName from Customers c where c.CustomerId = orders.CustomerId)";

            // Virtual object (table) with virtual fields
            // ===========================================================================

            o = queryBuilder.MetadataContainer.AddTable("MyOrders");
            o.Expression = "Orders";

            // first test field - simple expression
            f = o.AddField("OrderId_plus_1");
            f.Expression = "MyOrders.OrderId + 1";

            // second test field - correlated sub-query
            f = o.AddField("CustomerCompanyName");
            f.Expression = "(select c.CompanyName from Customers c where c.CustomerId = MyOrders.CustomerId)";

            // Virtual object (sub-query) with virtual fields
            // ===========================================================================

            o = queryBuilder.MetadataContainer.AddTable("MyBetterOrders");
            o.Expression = "(select OrderId, CustomerId, OrderDate from Orders)";

            // first test field - simple expression
            f = o.AddField("OrderId_plus_1");
            f.Expression = "MyBetterOrders.OrderId + 1";

            // second test field - correlated sub-query
            f = o.AddField("CustomerCompanyName");
            f.Expression = "(select c.CompanyName from Customers c where c.CustomerId = MyBetterOrders.CustomerId)";

            queryBuilder.SQLQuery.SQLUpdated += (sender, args) => OnSQLUpdated(name);

            queryBuilder.SQL = "SELECT mbo.OrderId_plus_1, mbo.CustomerCompanyName FROM MyBetterOrders mbo";

            return queryBuilder;
        }

        public void OnSQLUpdated(string name)
        {
            var qb = _queryBuilderService.Get(name);

            var opts = new SQLFormattingOptions();

            opts.Assign(qb.SQLFormattingOptions);
            opts.KeywordFormat = KeywordFormat.UpperCase;

            opts.ExpandVirtualObjects = true;
            var sql = FormattedSQLBuilder.GetSQL(qb.SQLQuery.QueryRoot, opts);

            qb.ExchangeData = new { sql, SessionID = name };
        }
    }
}
