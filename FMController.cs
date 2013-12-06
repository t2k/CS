using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Collections.Generic;
using System.Linq;
using CS;
using Newtonsoft.Json.Linq;


namespace ZD.Controllers
{
    public class fmReport
    {
        public string report { get; set; }
        public string ccy { get; set; }
        public DateTime evaldate { get; set; }
        public string portfolio { get; set; }
    }

    public class FMController : ApiController
    {
        private ZDEntities data;
        public FMController()
        {
            data = new ZDEntities();
            data.ContextOptions.ProxyCreationEnabled = false;
            data.ContextOptions.LazyLoadingEnabled = true;
        }

        /// <summary>
        /// Funding Manager Reports
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public dynamic Get()
        {
            string evaldate = Request.RequestUri.ParseQueryString().Get("evaldate"); //? 1000 : Convert.ToInt32(Request.QueryString["take"]);
            string ccy = Request.RequestUri.ParseQueryString().Get("ccy"); //? 1000 : Convert.ToInt32(Request.QueryString["take"]);
            string report = Request.RequestUri.ParseQueryString().Get("report"); //? 1000 : Convert.ToInt32(Request.QueryString["take"]);
            string portfolio = Request.RequestUri.ParseQueryString().Get("portfolio"); //? 1000 : Convert.ToInt32(Request.QueryString["take"]);
            DateTime evalDate = DateTime.Parse(evaldate);

            List<string> portfolios = portfolio.Split(',').ToList();
            try
            {
                MurexFlows mxFlows = new MurexFlows();
                // then use the selected ports.contains() in the where clause...
                var PIFlows = mxFlows.GetPortFlows(portfolios, ccy, evalDate, false);
                // create a maturity set (ALM periodic buckets)
                var maturityset = new MaturitySet(evalDate, TimeBuckets.ZD);
                // apply each flow across our ALM maturity buckets
                foreach (var flow in PIFlows)
                {
                    maturityset.ApplyFlow(flow);
                }

                maturityset.FindOptimalTrades();

                // use a little LINQ to tidy up/ reformat our results and bind it to our gridview control...
                // return an array will be JSONified...
                return new
                {
                    status = "success",
                    data = new
                    {
                        title = "Forward Liquidity Exposure",
                        ccy = ccy,
                        evaldate = evalDate,
                        portlist = string.Join(",", portfolios),
                        filter = String.Format("portfolio: {0}; ccy: {1}; date: {2:d}", string.Join(",", portfolios), ccy, evalDate), // deprecated bad to return formatted dates
                        rows = (from row in maturityset
                                select new  // C# anonymous types put to good use here
                                {
                                    Period = row.ID,
                                    PdStart = row.Start,
                                    PdEnd = row.End,
                                    Liabilities = row.Liab,
                                    IntPaid = row.Pay * row.Days() / 360,
                                    PayRate = row.Liab == 0 ? 0 : (row.Pay / row.Liab),
                                    Assets = row.Asset,
                                    IntRecv = row.Recv * row.Days() / 360,
                                    RecvRate = row.Asset == 0 ? 0 : (row.Recv / row.Asset),
                                    Net_L_A = (row.Liab - row.Asset),
                                    OptimalTrade = row.OptimalAdj
                                }).ToList()
                    }
                };
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError) { ReasonPhrase = ex.Message });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        public dynamic Post(fmReport id)
        {
            List<string> portfolios = id.portfolio.Split(',').ToList();
            try
            {
                MurexFlows mxFlows = new MurexFlows();
                // then use the selected ports.contains() in the where clause...
                var PIFlows = mxFlows.GetPortFlows(portfolios, id.ccy, id.evaldate, false);
                // create a maturity set (ALM periodic buckets)
                var maturityset = new MaturitySet(id.evaldate, TimeBuckets.ZD);
                // apply each flow across our ALM maturity buckets
                foreach (var flow in PIFlows)
                {
                    maturityset.ApplyFlow(flow);
                }

                maturityset.FindOptimalTrades();

                // use a little LINQ to tidy up/ reformat our results and bind it to our gridview control...
                // return an array will be JSONified...
                return new
                {
                    status = "success",
                    data = new
                    {
                        title = "Forward Liquidity Exposure",
                        ccy = id.ccy,
                        evaldate = id.evaldate,
                        portlist = string.Join(",", portfolios),
                        filter = String.Format("portfolio: {0}; ccy: {1}; date: {2:d}", string.Join(",", portfolios), id.ccy, id.evaldate), // deprecated bad to return formatted dates
                        rows = (from row in maturityset
                                select new  // C# anonymous types put to good use here
                                {
                                    Period = row.ID,
                                    PdStart = row.Start,
                                    PdEnd = row.End,
                                    Liabilities = row.Liab,
                                    IntPaid = row.Pay * row.Days() / 360,
                                    PayRate = row.Liab == 0 ? 0 : (row.Pay / row.Liab),
                                    Assets = row.Asset,
                                    IntRecv = row.Recv * row.Days() / 360,
                                    RecvRate = row.Asset == 0 ? 0 : (row.Recv / row.Asset),
                                    Net_L_A = (row.Liab - row.Asset),
                                    OptimalTrade = row.OptimalAdj
                                }).ToList()
                    }

                };
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError) { ReasonPhrase = ex.Message });
            }
        }
    }
}