using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DemoSite.Models;
using DemoSite.Services;

namespace DemoSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly QueryResultsService _queryResultsService;
        private readonly AlternateNamesService _alternateNamesService;
        private readonly VirtualObjectsService _virtualObjectsService;
        private readonly QueryStatisticService _queryStatisticService;

        public HomeController(QueryResultsService queryResultsService, AlternateNamesService alternateNamesService,
            VirtualObjectsService virtualObjectsService, QueryStatisticService queryStatisticService)
        {
            _queryResultsService = queryResultsService;
            _alternateNamesService = alternateNamesService;
            _virtualObjectsService = virtualObjectsService;
            _queryStatisticService = queryStatisticService;
        }

        public ActionResult Index()
        {
            CreateQueryResults("QueryResults");
            return View();
        }

        private void CreateQueryResults(string name)
        {
            _queryResultsService.CreateQueryBuilder(name);
            _queryResultsService.CreateQueryTransformer(name);
        }

        public void CreateAlternateNames(string name)
        {
            _alternateNamesService.CreateQueryBuilder(name);
        }

        public void CreateVirtualObjects(string name)
        {
            _virtualObjectsService.CreateQueryBuilder(name);
        }

        public ActionResult GetSyntaxes()
        {
            return null;
        }

        public ActionResult GetData(GridModel m)
        {
            try
            {
                var data = _queryResultsService.GetData("QueryResults", m);
                return Json(data);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public ActionResult GetPreviewData(string name)
        {
            try
            {
                var data = _queryResultsService.GetPreviewData(name);
                return Json(data);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public void Toggle(string name)
        {
            _alternateNamesService.Toggle(name);
        }

        public ActionResult QueryStatistics()
        {
            return Json(_queryStatisticService.GetQueryStatistic());
        }

        public ActionResult ChangeSyntax(string syntax, string syntaxVersion)
        {
            return Json(_queryStatisticService.ChangeSyntax(syntax, syntaxVersion));
        }
    }

    public class GridModel
    {
        public int Pagenum { get; set; }
        public int Pagesize { get; set; }
        public string Sortdatafield { get; set; }
        public string Sortorder { get; set; }
        public Param[] Params { get; set; }
    }

    public class Param
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public DbType DataType { get; set; }
    }

    public class DemoExchangeObject
    {
        public string Reaction { get; set; }
        public SyntaxProviderParam Data { get; set; }
        public string QueryStatistics { get; set; }
    }

    public class SyntaxProviderParam
    {
        public string Syntax;
        public string SyntaxVersion;
    }

    public class PreviewData
    {
        public List<List<object>> Data { get; set; }
        public List<string> Columns { get; set; }
    }
}
