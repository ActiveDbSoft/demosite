using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ActiveQueryBuilder.Core;
using ActiveQueryBuilder.Core.QueryTransformer;
using ActiveQueryBuilder.Web.Server;
using ActiveQueryBuilder.Web.Server.Infrastructure.Factories;
using ActiveQueryBuilder.Web.Server.Services;
using DemoSite.Controllers;

namespace DemoSite.Services
{
    public class QueryResultsService
    {
        private readonly IQueryBuilderService _queryBuilderService;
        private readonly IQueryTransformerService _queryTransformerService;
        private readonly DataBaseHelper _dbHelper;

        public QueryResultsService(IQueryBuilderService queryBuilderService,
            IQueryTransformerService queryTransformerService, DataBaseHelper dbHelper)
        {
            _queryBuilderService = queryBuilderService;
            _queryTransformerService = queryTransformerService;
            _dbHelper = dbHelper;
        }

        public QueryBuilder CreateQueryBuilder(string name)
        {
            var qb = _queryBuilderService.Get(name) ?? Create(name);
            return qb;
        }

        public  QueryTransformer CreateQueryTransformer(string name)
        {
            var qt = _queryTransformerService.Get(name);

            if (qt == null)
            {
                var query = CreateQueryBuilder(name).SQLQuery;
                qt = CreateQueryTransformer(name, query);
            }

            return qt;
        }

        private  List<Dictionary<string, object>> GetData(QueryTransformer qt, Param[] _params)
        {
            var conn = _dbHelper.CreateMySqlConnection();

            if (_params != null)
                foreach (var p in _params)
                    p.DataType = qt.Query.QueryParameters.First(qp => qp.FullName == p.Name).DataType;

            return _dbHelper.GetData(conn, qt.SQL, _params);
        }

        private  QueryBuilder Create(string name)
        {
            var queryBuilder = _queryBuilderService.Create(name);
            queryBuilder.SyntaxProvider = new MySQLSyntaxProvider();
            queryBuilder.BehaviorOptions.AllowSleepMode = true;
            queryBuilder.SyntaxProvider.DenyIntoClause = true;

            using (var stream = GetType().Assembly.GetManifestResourceStream("DemoSite.adventureworks.xml"))
                queryBuilder.MetadataContainer.ImportFromXML(stream);

            queryBuilder.SQL = GetDefaultSql();

            return queryBuilder;
        }

        private  string GetDefaultSql()
        {
            return @"Select product.Name,
                     product.StandardCost
                     From product";
        }

        private  QueryTransformer CreateQueryTransformer(string name, SQLQuery query)
        {
            var qt = _queryTransformerService.Create(name);

            qt.QueryProvider = query;
            qt.AlwaysExpandColumnsInQuery = true;

            return qt;
        }

        public  List<Dictionary<string, object>> GetData(string name, GridModel m)
        {
            var qt = _queryTransformerService.Get(name);

            qt.Skip((m.Pagenum * m.Pagesize).ToString());
            qt.Take(m.Pagesize == 0 ? "" : m.Pagesize.ToString());

            if (!string.IsNullOrEmpty(m.Sortdatafield))
            {
                qt.Sortings.Clear();

                if (!string.IsNullOrEmpty(m.Sortorder))
                {
                    var c = qt.Columns.FindColumnByResultName(m.Sortdatafield);
                    qt.OrderBy(c, m.Sortorder.ToLower() == "asc");
                }
            }

            return GetData(qt, m.Params);
        }

        public  PreviewData GetPreviewData(string name)
        {
            var qb = _queryBuilderService.Get(name);
            var conn = _dbHelper.CreateMySqlConnection();

            var sqlQuery = new SQLQuery(qb.SQLContext)
            {
                SQL = qb.ActiveSubQuery.GetSqlForDataPreview()
            };

            if (GetDataSourceGroups(qb.ActiveUnionSubQuery).Count > 1)
                throw new ApplicationException("Data preview is suspended due to the presence of objects or groups which aren't joined with the others. It is recommended to ensure that all objects in the query are joined with each other to maintain data consistency.");

            if (sqlQuery.QueryParameters.Count > 0)
                throw new ApplicationException("The sub-query results preview in this demo does not work for queries with parameters. Please switch to the Query Results tab to enter parameter values and see the result data.");

            QueryTransformer qt = new QueryTransformer
            {
                QueryProvider = sqlQuery,
                AlwaysExpandColumnsInQuery = true
            };

            qt.Take("10");

            var columns = qt.Columns.Select(c => c.ResultName).ToList();

            var data = _dbHelper.GetDataList(conn, qt.SQL);
            return CreatePreviewData(columns, data);
        }

        public  IList<IList<DataSource>> GetDataSourceGroups(UnionSubQuery unionSubQuery)
        {
            var links = unionSubQuery.GetChildrenRecursive<Link>(false);
            var dataSources = unionSubQuery.GetChildrenRecursive<DataSource>(false);

            var result = new List<IList<DataSource>>();
            var processedDatasources = new System.Collections.Generic.HashSet<DataSource>();

            foreach (var dataSource in dataSources)
            {
                if (processedDatasources.Contains(dataSource))
                    continue;

                var group = new List<DataSource>();
                CollectReferencedDataSources(dataSource, group, links);

                result.Add(group);

                foreach (var item in group)
                    processedDatasources.Add(item);
            }

            return result;
        }

        private  void CollectReferencedDataSources(DataSource startDataSource, ICollection<DataSource> collectedDataSources, IList<Link> links)
        {
            var startDataSourceLinks = links.Where(l => l.ReferencedDataSources.Contains(startDataSource));
            var startDataSourceNeighbours = startDataSourceLinks.SelectMany(l => l.ReferencedDataSources).Distinct();

            collectedDataSources.Add(startDataSource);

            foreach (var neighbour in startDataSourceNeighbours)
            {
                if (!collectedDataSources.Contains(neighbour))
                    CollectReferencedDataSources(neighbour, collectedDataSources, links);
            }
        }

        private  PreviewData CreatePreviewData(List<string> columns, List<List<object>> data)
        {
            return new PreviewData
            {
                Columns = columns,
                Data = data
            };
        }
    }
}
