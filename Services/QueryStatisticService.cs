using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActiveQueryBuilder.Core;
using ActiveQueryBuilder.Web.Server;
using ActiveQueryBuilder.Web.Server.Services;
using DemoSite.Controllers;

namespace DemoSite.Services
{
    public class QueryStatisticService
    {
        private readonly IQueryBuilderService _queryBuilderService;

        public QueryStatisticService(IQueryBuilderService queryBuilderService)
        {
            _queryBuilderService = queryBuilderService;
        }

        public DemoExchangeObject ChangeSyntax(string syntax, string version)
        {
            var outputData = new DemoExchangeObject();

            ActionChangeSyntax(syntax, version, outputData);

            return outputData;
        }

        public DemoExchangeObject GetQueryStatistic()
        {
            var qb = GetQueryBuilder();

            var outputData = new DemoExchangeObject();

            QueryStatistics qs = qb.SQLQuery.QueryStatistics;

            string stats = "<b>Used Objects (" + qs.UsedDatabaseObjects.Count + "):</b><br/>";

            for (int i = 0; i < qs.UsedDatabaseObjects.Count; i++)
                stats += "<br />" + qs.UsedDatabaseObjects[i].ObjectName.QualifiedName;

            stats += "<br /><br />" + "<b>Used Columns (" + qs.UsedDatabaseObjectFields.Count + "):</b><br />";

            for (int i = 0; i < qs.UsedDatabaseObjectFields.Count; i++)
                stats += "<br />" + qs.UsedDatabaseObjectFields[i].FullName.QualifiedName;

            stats += "<br /><br />" + "<b>Output Expressions (" + qs.OutputColumns.Count + "):</b><br />";

            for (int i = 0; i < qs.OutputColumns.Count; i++)
                stats += "<br />" + qs.OutputColumns[i].Expression;

            outputData.Reaction = "QueryStatistics";
            outputData.QueryStatistics = stats;

            return outputData;
        }

        private void ActionChangeSyntax(string syntax, string version, DemoExchangeObject outputData)
        {
            var qb = GetQueryBuilder();

            var syntaxChanged = UpdateSyntaxProvider(qb, syntax, version);

            if (syntaxChanged)
            {
                try
                {
                    qb.SQLQuery.NotifyUpdatedRecursive();
                }
                catch (Exception ex)
                {
                    new Logger().Error(ex.Message);
                }
                outputData.Reaction = "update";
            }
        }

        private QueryBuilder GetQueryBuilder()
        {
            return _queryBuilderService.Get("QueryResults");
        }

        private bool UpdateSyntaxProvider(QueryBuilder qb, string syntax, string version)
        {
            var sqlContext = qb.SQLContext;

            var result = false;
            var syntaxProvider = sqlContext.SyntaxProvider;

            if (syntaxProvider is AutoSyntaxProvider)
                syntaxProvider = ((AutoSyntaxProvider)sqlContext.SyntaxProvider).DetectedSyntaxProvider;

            if (syntaxProvider == null)
                return false;

            switch (syntax)
            {
                case "ANSI SQL-2003":
                    if (!(syntaxProvider is SQL2003SyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new SQL2003SyntaxProvider();
                        result = true;
                    }
                    break;
                case "ANSI SQL-92":
                    if (!(syntaxProvider is SQL92SyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new SQL92SyntaxProvider();
                        result = true;
                    }
                    break;
                case "ANSI SQL-89":
                    if (!(syntaxProvider is SQL89SyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new SQL89SyntaxProvider();
                        result = true;
                    }
                    break;
                case "Firebird":
                    if (!(syntaxProvider is FirebirdSyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new FirebirdSyntaxProvider();
                        result = true;
                    }
                    break;
                case "IBM DB2":
                    if (!(syntaxProvider is DB2SyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new DB2SyntaxProvider();
                        result = true;
                    }
                    break;
                case "IBM Informix":
                    if (!(syntaxProvider is InformixSyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new InformixSyntaxProvider();
                        result = true;
                    }
                    break;
                case "Microsoft Access":
                    if (!(syntaxProvider is MSAccessSyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new MSAccessSyntaxProvider();
                        result = true;
                    }
                    break;
                case "Microsoft SQL Server":
                    if (!(syntaxProvider is MSSQLSyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new MSSQLSyntaxProvider();
                        result = true;
                    }
                    break;
                case "MySQL":
                    if (!(syntaxProvider is MySQLSyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new MySQLSyntaxProvider();
                        result = true;
                    }
                    break;
                case "Oracle":
                    if (!(syntaxProvider is OracleSyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new OracleSyntaxProvider();
                        result = true;
                    }
                    break;
                case "PostgreSQL":
                    if (!(syntaxProvider is PostgreSQLSyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new PostgreSQLSyntaxProvider();
                        result = true;
                    }
                    break;
                case "SQLite":
                    if (!(syntaxProvider is SQLiteSyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new SQLiteSyntaxProvider();
                        result = true;
                    }
                    break;
                case "Sybase":
                    if (!(syntaxProvider is SybaseSyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new SybaseSyntaxProvider();
                        result = true;
                    }
                    break;
                case "VistaDB":
                    if (!(syntaxProvider is VistaDBSyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new VistaDBSyntaxProvider();
                        result = true;
                    }
                    break;
                case "Universal":
                    if (!(syntaxProvider is AutoSyntaxProvider))
                    {
                        sqlContext.SyntaxProvider = new GenericSyntaxProvider();
                        ((GenericSyntaxProvider)sqlContext.SyntaxProvider).RedetectServer(sqlContext);
                        result = true;
                    }
                    break;
            }
            result = UpdateSyntaxProviderVersion(version, sqlContext) || result;
            return result;
        }

        private bool UpdateSyntaxProviderVersion(string syntaxVersion, SQLContext sqlContext)
        {
            var result = false;
            var syntaxProvider = sqlContext.SyntaxProvider;

            if (syntaxProvider is AutoSyntaxProvider)
                syntaxProvider = ((AutoSyntaxProvider)sqlContext.SyntaxProvider).DetectedSyntaxProvider;

            if (syntaxProvider == null)
                return false;

            if (syntaxProvider is FirebirdSyntaxProvider)
            {
                if (syntaxVersion == "Firebird 1.0" && (((FirebirdSyntaxProvider)syntaxProvider).ServerVersion != FirebirdVersion.Firebird10))
                {
                    ((FirebirdSyntaxProvider)syntaxProvider).ServerVersion = FirebirdVersion.Firebird10;
                    result = true;

                }
                else if (syntaxVersion == "Firebird 1.5" && ((FirebirdSyntaxProvider)syntaxProvider).ServerVersion != FirebirdVersion.Firebird15)
                {
                    ((FirebirdSyntaxProvider)syntaxProvider).ServerVersion = FirebirdVersion.Firebird15;
                    result = true;
                }
                else if (syntaxVersion == "Firebird 2.0" && ((FirebirdSyntaxProvider)syntaxProvider).ServerVersion != FirebirdVersion.Firebird20)
                {
                    ((FirebirdSyntaxProvider)syntaxProvider).ServerVersion = FirebirdVersion.Firebird20;
                    result = true;
                }
            }
            else if (syntaxProvider is InformixSyntaxProvider)
            {
                if (syntaxVersion == "Informix 8" && (((InformixSyntaxProvider)syntaxProvider).ServerVersion != InformixVersion.DS8))
                {
                    ((InformixSyntaxProvider)syntaxProvider).ServerVersion = InformixVersion.DS8;
                    result = true;

                }
                else if (syntaxVersion == "Informix 9" && ((InformixSyntaxProvider)syntaxProvider).ServerVersion != InformixVersion.DS9)
                {
                    ((InformixSyntaxProvider)syntaxProvider).ServerVersion = InformixVersion.DS9;
                    result = true;
                }
                else if (syntaxVersion == "Informix 10" && ((InformixSyntaxProvider)syntaxProvider).ServerVersion != InformixVersion.DS10)
                {
                    ((InformixSyntaxProvider)syntaxProvider).ServerVersion = InformixVersion.DS10;
                    result = true;
                }
            }
            else if (syntaxProvider is MSAccessSyntaxProvider)
            {
                if (syntaxVersion == "MS Jet 3" && ((MSAccessSyntaxProvider)syntaxProvider).ServerVersion != MSAccessServerVersion.MSJET3)
                {
                    ((MSAccessSyntaxProvider)syntaxProvider).ServerVersion = MSAccessServerVersion.MSJET3;
                    result = true;
                }
                else if (syntaxVersion == "MS Jet 4" && ((MSAccessSyntaxProvider)syntaxProvider).ServerVersion != MSAccessServerVersion.MSJET4)
                {
                    ((MSAccessSyntaxProvider)syntaxProvider).ServerVersion = MSAccessServerVersion.MSJET4;
                    result = true;
                }
            }
            else if (syntaxProvider is MSSQLSyntaxProvider)
            {
                if (syntaxVersion == "Auto" && ((MSSQLSyntaxProvider)syntaxProvider).ServerVersion != MSSQLServerVersion.Auto)
                {
                    ((MSSQLSyntaxProvider)syntaxProvider).ServerVersion = MSSQLServerVersion.Auto;
                    result = true;
                }
                else if (syntaxVersion == "SQL Server 7" && ((MSSQLSyntaxProvider)syntaxProvider).ServerVersion != MSSQLServerVersion.MSSQL7)
                {
                    ((MSSQLSyntaxProvider)syntaxProvider).ServerVersion = MSSQLServerVersion.MSSQL7;
                    result = true;
                }
                else if (syntaxVersion == "SQL Server 2000" && ((MSSQLSyntaxProvider)syntaxProvider).ServerVersion != MSSQLServerVersion.MSSQL2000)
                {
                    ((MSSQLSyntaxProvider)syntaxProvider).ServerVersion = MSSQLServerVersion.MSSQL2000;
                    result = true;
                }
                else if (syntaxVersion == "SQL Server 2005" && ((MSSQLSyntaxProvider)syntaxProvider).ServerVersion != MSSQLServerVersion.MSSQL2005)
                {
                    ((MSSQLSyntaxProvider)syntaxProvider).ServerVersion = MSSQLServerVersion.MSSQL2005;
                    result = true;
                }
            }
            else if (syntaxProvider is MySQLSyntaxProvider)
            {
                if (syntaxVersion == "3.0" && ((MySQLSyntaxProvider)syntaxProvider).ServerVersionInt != 39999)
                {
                    ((MySQLSyntaxProvider)syntaxProvider).ServerVersionInt = 39999;
                    result = true;
                }
                else if (syntaxVersion == "4.0" && ((MySQLSyntaxProvider)syntaxProvider).ServerVersionInt != 49999)
                {
                    ((MySQLSyntaxProvider)syntaxProvider).ServerVersionInt = 49999;
                    result = true;
                }
                else if (syntaxVersion == "5.0" && ((MySQLSyntaxProvider)syntaxProvider).ServerVersionInt != 50000)
                {
                    ((MySQLSyntaxProvider)syntaxProvider).ServerVersionInt = 50000;
                    result = true;
                }
            }
            else if (syntaxProvider is OracleSyntaxProvider)
            {
                if (syntaxVersion == "Oracle 7" && ((OracleSyntaxProvider)syntaxProvider).ServerVersion != OracleServerVersion.Oracle7)
                {
                    ((OracleSyntaxProvider)syntaxProvider).ServerVersion = OracleServerVersion.Oracle7;
                    result = true;
                }
                else if (syntaxVersion == "Oracle 8" && ((OracleSyntaxProvider)syntaxProvider).ServerVersion != OracleServerVersion.Oracle8)
                {
                    ((OracleSyntaxProvider)syntaxProvider).ServerVersion = OracleServerVersion.Oracle8;
                    result = true;
                }
                else if (syntaxVersion == "Oracle 9" && ((OracleSyntaxProvider)syntaxProvider).ServerVersion != OracleServerVersion.Oracle9)
                {
                    ((OracleSyntaxProvider)syntaxProvider).ServerVersion = OracleServerVersion.Oracle9;
                    result = true;
                }
                else if (syntaxVersion == "Oracle 10" && ((OracleSyntaxProvider)syntaxProvider).ServerVersion != OracleServerVersion.Oracle10)
                {
                    ((OracleSyntaxProvider)syntaxProvider).ServerVersion = OracleServerVersion.Oracle10;
                    result = true;
                }
            }
            else if (syntaxProvider is SybaseSyntaxProvider)
            {
                if (syntaxVersion == "ASE" && ((SybaseSyntaxProvider)syntaxProvider).ServerVersion != SybaseServerVersion.SybaseASE)
                {
                    ((SybaseSyntaxProvider)syntaxProvider).ServerVersion = SybaseServerVersion.SybaseASE;
                    result = true;
                }
                else if (syntaxVersion == "SQL Anywhere" && ((SybaseSyntaxProvider)syntaxProvider).ServerVersion != SybaseServerVersion.SybaseASA)
                {
                    ((SybaseSyntaxProvider)syntaxProvider).ServerVersion = SybaseServerVersion.SybaseASA;
                    result = true;
                }
            }
            return result;
        }
    }
}
