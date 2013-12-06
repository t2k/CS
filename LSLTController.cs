using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using CreditPricing;

// Liquidity Spread Controllers
namespace ZD.Controllers
{
    public class Tenor
    {
        public DateTime tenorDateTK { get; set; }
        public DateTime tenorDateBS { get; set; }
        public String tenorName { get; set; }
    }

    public class LSLTController : ApiController
    {
        // GET /zd-api/LSLT/USD/2013-08-22  example api
        /// <summary>
        /// rest API: return global liquidity spread policy
        /// </summary>
        /// <param name="id">CCY</param>
        /// <param name="_date">FIXING DATE</param>
        /// <returns></returns>
        public dynamic Get(string id, DateTime? _date)
        {
            try
            {
                string _maturity = Request.RequestUri.ParseQueryString().Get("maturity"); //? 1000 : Convert.ToInt32(Request.QueryString["take"]);

                var data = new CS.ZDEntities();
                var ctx = new hdbOracleEntities();
                DateTime maxBSS = (from b in ctx.BSSes where b.SYMBOL.StartsWith(id) select b.TRADE_DATE).Max();
                DateTime bssFixDate;

                if (maxBSS < _date.Value)
                {
                    bssFixDate = maxBSS;
                }
                else
                {
                    bssFixDate = _date.Value;
                }

                // liquidity is offered on the following basis
                var liquidityBasis = (from z in data.zd_ccyterms where z.Currency == id select z).ToList();
                //FYI: 
                //Id    Currency    Period  RefIndex    DCC     FixingCalendar  ValueCalendar               ValueDay    MaxTenor
                //40    USD         1M      LIBOR 1M    ACT/360 London, Target  New York, London, Target    2           30 
                //41    USD         3M      LIBOR 3M    ACT/360 London, Target  New York, London, Target    2           30 
                //42    USD         6M      LIBOR 6M    ACT/360 London, Target  New York, London, Target    2           30 
                //43    USD         12M     LIBOR 12M   ACT/360 London, Target  New York, London, Target    2           30 

                int valueDay = (int)(from l in liquidityBasis select l.ValueDay).First();
                // new: we create a dictionary, bsCurve: string mapped YldCurve ie 1M, 3M, 6M, 12M etc
                Dictionary<string, YldCurve> bsCurves = new Dictionary<string, YldCurve>();  // USAGE: YldCurve yc = bsCurves["3M"];  C'est bon  

                // the TreasuryKurve "TK"  from whence all global liquidty shall cometh-frometh ;p
                bsCurves.Add("TK", new YldCurve("TK_UNGEDECKT", _date.Value));  // we store TK_Ungedeckt daily in CREDITNET

                //populate our dictionary
                foreach (var basis in liquidityBasis)
                {
                    bsCurves.Add(basis.Period, new YldCurve(id, bssFixDate, basis.Period));  // note this constructor gathers data from HDB
                }

                // NEW NEW NEW Oct. 2013 
                List<Tenor> tenors = new List<Tenor>();
                DateTime mDate;
                if (_maturity != null) //if maturity is parsed in via QUERYSTRING then create a List<Tenor> of just 1 item (the BESPOKE date)
                {
                    Boolean dateparse = DateTime.TryParse(_maturity, out mDate);
                    if (dateparse) {
                        tenors.Add(new Tenor() {tenorName = "Bespoke", tenorDateTK = mDate, tenorDateBS = mDate});
                    }
                }
                else // otherwise create a list if tenors from the following periods
                {
                    string[] tenorPeriods = { "1Y", "2Y", "3Y", "4Y", "5Y", "7Y", "10Y", "12Y", "15Y", "20Y", "30Y" };
                    foreach (string pd in tenorPeriods)
                    {
                        DateTime tDate = bsCurves["3M"].calendar.FarDate(bsCurves["3M"].calendar.Workdays(_date.Value, valueDay), pd);
                        DateTime tDate2 = bsCurves["TK"].calendar.FarDate(bsCurves["TK"].calendar.Workdays(_date.Value, valueDay), pd);
                        tenors.Add(new Tenor() { tenorName = pd, tenorDateBS = tDate, tenorDateTK = tDate2 });
                    }
                }

                var liquidityTenors = from tenor in tenors
                                      select new
                                      {
                                          tenor = tenor.tenorName,
                                          tenorDate = tenor.tenorDateBS, // bsCurves["3M"].calendar.FarDate(bsCurves["3M"].calendar.Workdays(_date.Value, valueDay), tenor),
                                          availableLiquidity = from lb in liquidityBasis
                                                               select new
                                                               {
                                                                   currency = lb.Currency,
                                                                   basis = lb.Period,
                                                                   refIndex = lb.RefIndex,
                                                                   valueDate = bsCurves["3M"].calendar.Workdays(_date.Value, lb.ValueDay.Value),
                                                                   tenorDate = tenor.tenorDateBS,
                                                                   DCC = lb.DCC.Trim(),
                                                                   fixingCalendar = lb.FixingCalendar,
                                                                   valueCalendar = lb.ValueCalendar,
                                                                   maxTenor = lb.MaxTenor,
                                                                   valueDay = lb.ValueDay,
                                                                   spreads = new
                                                                   {
                                                                       //TK = bsCurves["TK"].IsValid ? bsCurves["TK"].FwdRate(bsCurves["TK"].spotDate(), bsCurves["TK"].calendar.FarDate(bsCurves["TK"].spotDate(), tenor), tkPrice.tkASK, int.Parse(lb.DCC.Trim().Substring(lb.DCC.Trim().Length - 3, 3))) : 0,
                                                                       TK = bsCurves["TK"].IsValid ? bsCurves["TK"].FwdRate(bsCurves["TK"].spotDate(), tenor.tenorDateTK, tkPrice.tkASK, int.Parse(lb.DCC.Trim().Substring(lb.DCC.Trim().Length - 3, 3))) : 0,
                                                                       curve1 = bsCurves["TK"].CurveName,
                                                                       XCCY = double.IsNaN(bsCurves["3M"].FwdRate(bsCurves["3M"].spotDate(), tenor.tenorDateBS, tkPrice.tkASK, int.Parse(lb.DCC.Trim().Substring(lb.DCC.Trim().Length - 3, 3)))) ? 0 : bsCurves["3M"].FwdRate(bsCurves["3M"].spotDate(), tenor.tenorDateBS, tkPrice.tkASK, int.Parse(lb.DCC.Trim().Substring(lb.DCC.Trim().Length - 3, 3))),
                                                                       curve2 = bsCurves["3M"].CurveName,
                                                                       BS = lb.Period != "3M" ? bsCurves[lb.Period].IsValid ? bsCurves[lb.Period].FwdRate(bsCurves[lb.Period].spotDate(), tenor.tenorDateBS, tkPrice.tkASK, int.Parse(lb.DCC.Trim().Substring(lb.DCC.Trim().Length - 3, 3))) : 0 : 0,
                                                                       curve3 = bsCurves[lb.Period].CurveName,
                                                                       BSadd = lb.Period == "1M" ? true : false,
                                                                       BSvisible = lb.Period == "3M" ? false : true
                                                                   }
                                                               }
                                      };

                // return this API back to client:dynamic/JSON
                return new
                {
                    status = "success",
                    data = new
                    {
                        currency = id,
                        fixingDate = _date,
                        bssFixingDate = bssFixDate,
                        maturity = _maturity,
                        timeStamp = bsCurves["TK"].prices.TimeStamp,
                        timeStampUTC = bsCurves["TK"].prices.TimeStampUTC,
                        tenors = liquidityTenors
                    }
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    status = "error",
                    message = ex.Message
                };
            }
        }


    }
}
