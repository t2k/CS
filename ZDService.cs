using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Script.Services;
using System.Data.Objects;
using CS;
using ZD;
// nice!  the compilation order here 
using CreditPricing;
using VB;
using LinqStatistics;
using Newtonsoft.Json.Linq;





/// <summary>
/// Summary description for ZDService
/// </summary>
[WebService(Namespace = "http://dnyias20/dzservice/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
[ScriptService]
public class ZDService : System.Web.Services.WebService
{
    public ZDService()
    {
        //this.Session.Timeout = System.Threading.Timeout.Infinite;
        //Uncomment the following line if using designed components 
        //InitializeComponent(); 
    }


    /// <summary>
    /// hard coded for now... will pass in an int passed in from the client UI selection...
    /// </summary>
    /// <returns>JSON anonymous type for our client (web or otherwise) to sort it out!</returns>
    [WebMethod]
    public dynamic GetAnSerMaturityBuckets(AnserReportParms option)
    {
        double mvAdj = option.mvDelta; // -.05;
        double mil = 1000000;
        int modelID = option.anserID;  // hard coded for now...
        var ctx = new ZDEntities();
        ctx.CommandTimeout = 180;
        var model = (from r in ctx.anserModelIFaces
                     where r.ID == modelID
                     select r).FirstOrDefault();

        DateTime _date = (DateTime)model.evalDate;

        AnserBonds abonds = new AnserBonds();


        List<AnSerBond> bonds = abonds._AnSerBonds(modelID);

        List<mx_rloan_flow> ANSERassetflows = abonds._flowAnSerBonds(bonds, _date, 0);
        // 
        List<mx_rloan_flow> ANSERassetflows2 = abonds._flowAnSerBonds(bonds, _date, mvAdj);


        // new July 29 2011  add in the CI_SEC_RMBS funding flows
        List<string> ports = new List<string>();

        // hard coded RMBS FUNDING portfolios
        ports.Add("CI_SEC_RMBS");
        ports.Add("CI_SEC_RMBS_NPF");


        MurexFlows mxflows = new MurexFlows();

        var murexFlows = mxflows.GetPortFlows(ports, "USD", option.evalDate, false); // _date, false); //DateTime.Today, false);

        try
        {
            var maturityset = new MaturitySet(_date, TimeBuckets.AnSer);

            // apply each asset flow across our ALM maturity buckets
            foreach (var flow in ANSERassetflows)
            {
                maturityset.ApplyANSERFlow(flow);
            }

            // liability flows as booked in MUREX
            foreach (var flow in murexFlows)
            {
                maturityset.ApplyFlow(flow);
            }

            maturityset.FindOptimalANSERTrades();



            // version2
            var maturityset2 = new MaturitySet(_date, TimeBuckets.AnSer);

            // apply each asset flow across our ALM maturity buckets
            foreach (var flow in ANSERassetflows2)
            {
                maturityset2.ApplyANSERFlow(flow);
            }

            // liability flows as booked in MUREX
            foreach (var flow in murexFlows)
            {
                maturityset2.ApplyFlow(flow);
            }

            maturityset2.FindOptimalANSERTrades();

            // return an array will be JSONified...
            return new
            {
                status = "success",
                mvDelta = mvAdj,
                title = string.Format("RMBS Funding Report: NFE @ Market Value: Pro-forma AnSer v{0} {1} scenario", model.version, model.scenario),
                filter = String.Format("portfolio: {0}; dates: {2:d} / {3:d}", "CI_SEC_RMBS, CI_SEC_RMBS_NPF", "USD", model.evalDate, option.evalDate),
                //cusips = from s in ctx.Securities orderby s.Security where s.SecurityClass.StartsWith("RMBS")  select new {
                //    label = s.Security + "  (" + s.CUSIP +")",
                //    value = s.CUSIP
                //},
                rows = (from row in maturityset
                        select new  // C# anonymous types put to good use here
                        {
                            Period = row.ID,
                            PdStart = row.Start,
                            PdEnd = row.End,

                            Liabilities = row.Liab / mil,
                            IntPaid = (row.Pay * row.Days() / 360) / mil,
                            PayRate = row.Liab == 0 ? 0 : (row.Pay / row.Liab),

                            ANSERAssets = row.ANSERAsset / mil,
                            MVAssets = row.MVAsset / mil,
                            ANSERRecv = row.ANSERRecv != double.NaN ? (row.ANSERRecv * row.Days() / 360) / mil : 0,
                            ANSERRecvRate = (row.MVAsset == 0 || row.ANSERRecv == double.NaN) ? 0 : (row.ANSERRecv / row.ANSERAsset),

                            Assets = row.Asset / mil,
                            IntRecv = row.Recv != double.NaN ? (row.Recv * row.Days() / 360) / mil : 0,
                            RecvRate = (row.Asset == 0 || row.Recv == double.NaN) ? 0 : (row.Recv / row.Asset),
                            Net_L_A = (row.Liab - (row.MVAsset + row.Asset)) / mil,
                            OptimalTrade = row.OptimalAdj / mil
                        }).ToList(),
                rows2 = (from row in maturityset2
                         select new  // C# anonymous types put to good use here
                         {
                             Period = row.ID,
                             PdStart = row.Start,
                             PdEnd = row.End,

                             Liabilities = row.Liab / mil,
                             IntPaid = (row.Pay * row.Days() / 360) / mil,
                             PayRate = row.Liab == 0 ? 0 : (row.Pay / row.Liab),

                             ANSERAssets = row.ANSERAsset / mil,
                             MVAssets = row.MVAsset / mil,
                             ANSERRecv = row.ANSERRecv != double.NaN ? (row.ANSERRecv * row.Days() / 360) / mil : 0,
                             ANSERRecvRate = (row.MVAsset == 0 || row.ANSERRecv == double.NaN) ? 0 : (row.ANSERRecv / row.ANSERAsset),

                             Assets = row.Asset / mil,
                             IntRecv = row.Recv != double.NaN ? (row.Recv * row.Days() / 360) / mil : 0,
                             RecvRate = (row.Asset == 0 || row.Recv == double.NaN) ? 0 : (row.Recv / row.Asset),
                             Net_L_A = (row.Liab - (row.MVAsset + row.Asset)) / mil,
                             OptimalTrade = row.OptimalAdj / mil
                         }).ToList()
            };
        }
        catch (Exception e)
        {
            return new
            {
                status = "error",
                message = e.Message
            };
        }
    }


    /// <summary>
    /// return list of portfolios, CCYs and the maxDate
    /// </summary>
    /// <returns></returns>
    [WebMethod]
    public dynamic GetZDStaticData()
    {
        var ctx = new ZDEntities();
        try
        {
            var maxdt = (from d in ctx.mx_rloan_flow
                         select d.SaveDate).Max();

            return new
            {
                status = "success",
                maxdate = maxdt.HasValue ? maxdt : DateTime.Today,
                portfolios = (from x in ctx.mx_rloan_flow
                              where x.SaveDate == maxdt
                              select new { x.Portfolio }).Distinct().ToList(),
                ccys = (from x in ctx.mx_rloan_flow
                        where x.SaveDate == maxdt
                        select new { x.CCY }).Distinct().ToList()
            };
        }
        catch (Exception e)
        {
            return new
            {
                status = "error",
                message = e.Message
            };
        }
        finally
        {
            ctx.Dispose();
        }
    }


    /// <summary>
    /// Time Bucket Report  
    /// </summary>
    /// <param name="options">ZDOptions1</param>
    /// <returns>(jSend API) json data envelope</returns>
    [WebMethod]
    public dynamic ALMAnalysis(ZDOptions1 options)
    {
        try
        {
            MurexFlows mxFlows = new MurexFlows();
            // then use the selected ports.contains() in the where clause...
            var PIFlows = mxFlows.GetPortFlows(options.portfolios, options.ccy, options.evalDate, false);
            // create a maturity set (ALM periodic buckets)
            var maturityset = new MaturitySet(options.evalDate, TimeBuckets.ZD);
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
                    title = "Forward Liquidity Exposure (Maturity Bucket Report)",
                    ccy = options.ccy,
                    evaldate = options.evalDate,
                    portlist = string.Join(",", options.portfolios),
                    filter = String.Format("portfolio: {0}; ccy: {1}; date: {2:d}", string.Join(",", options.portfolios), options.ccy, options.evalDate), // deprecated bad to return formatted dates
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
        catch (Exception e)
        {
            return new
            {
                status = "error",
                message = e.Message
            };
        }
    }

    /// <summary>
    /// Loan Volume Analysis
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    /// to do: return volatility, standard deviation and trend data
    [WebMethod]
    public dynamic ZD_LoanAnalysis(ZDOptions1 options)
    {
        var ctx = new ZDEntities();
        try
        {
            double mm = 1000000;
            //ctx = new ZDEntities();

            ctx.CommandTimeout = 180;

            var qry = (from l in ctx.mx_rloan_flow
                       where l.CCY == options.ccy && options.portfolios.Contains(l.Portfolio) && l.SaveDate > EntityFunctions.AddMonths(options.evalDate, -12) && l.SaveDate <= options.evalDate && l.Current_period == "Y" && l.Internal == "N" && l.Group == "RLOAN"
                       group new { l.Notional } by new { l.SaveDate } into g // g is the 'group'

                       select new
                       {
                           Date = g.Key.SaveDate,
                           LoanCount = g.Count(),
                           LoanTotal = g.Sum(p => p.Notional)
                       }).ToList();


            var q2 = (from l in qry
                      orderby l.Date
                      select new
                      {
                          l.Date,
                          l.LoanCount,
                          l.LoanTotal
                      }).ToList();


            IEnumerable<double?> loans = from row in q2 select row.LoanTotal;

            // use a little LINQ to tidy up/ reformat our results and bind it to our gridview control...
            // return an array will be JSONified...
            return new
            {
                status = "success",
                title = "Daily Loan Balance",
                filter = String.Format("portfolio: {0}; ccy: {1}; date: {2:d}", string.Join(",", options.portfolios), options.ccy, options.evalDate),
                min = loans.Min() / mm,
                max = loans.Max() / mm,
          