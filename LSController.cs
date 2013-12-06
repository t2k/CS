using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using CreditPricing;

// Liquidity Spread Controllers
namespace ZD.Controllers
{
    public class LSController : ApiController
    {

        // GET /api/<controller>
        public dynamic Get(string id, DateTime? _date)
        {
            try
            {
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

                //var data = new CreditPricingEntities();
                YldCurve ycTK = new YldCurve("TK_UNGEDECKT", _date.Value);  // we store TK_Ungedeckt daily in CREDITNET
                YldCurve ycBS = new YldCurve(id, bssFixDate, true);  // note this constructor gathers data from HDB
                //YldCurve ycBS3x1 = new YldCurve(id, bssFixDate, false); // note this constructor gathers data from HDB

                // use the basis swap curve to set the spot dates... (multi city holidays)
                DateTime valueDate = ycBS.calendar.Workdays(_date.Value, 2);  // use the calendar from this yieldcurve object.. (gets holidays from HDB)

                List<lsReport> lsReport = new List<lsReport>();  // lsReport defined in this webservice see above
                // new/best practice?  if the odddate is not passed in from the client, then we return multiple rows of data 

                string[] periods = { "1Y", "2Y", "3Y", "4Y", "5Y", "6Y", "7Y", "8Y", "9Y", "10Y", "12Y", "15Y", "20Y", "30Y" };


                foreach (string pd in periods)
                {
                    lsReport row = new lsReport();  // liquidity spread report...
                    double pricecheck;  // new with system.data.oracleclient, must check for NaN 
                    row.Period = pd;
                    row.Date = ycBS.calendar.FarDate(valueDate, pd);

                    // calendar trick, must use own calendars basis/holidays to generate dates...
                    pricecheck = ycTK.IsValid ? ycTK.FwdRate(ycTK.spotDate(), ycTK.calendar.FarDate(ycTK.spotDate(), pd),tkPrice.tkBID) : 0;  // extra sanity checking w/ IsValid (HDB db is not 100% reliable)
                    row.TK = double.IsNaN(pricecheck) ? 0 : pricecheck;

                    pricecheck = ycBS.IsValid ? ycBS.FwdRate(ycBS.spotDate(), ycBS.calendar.FarDate(ycBS.spotDate(), pd)) : 0;
                    row.xCCYBSS = double.IsNaN(pricecheck) ? 0 : pricecheck;

                    //pricecheck = ycBS3x1.IsValid ? ycBS3x1.FwdRate(ycBS3x1.spotDate(), ycBS3x1.calendar.FarDate(ycBS3x1.spotDate(), pd)) : 0;
                    //row.BSS3x1 = double.IsNaN(pricecheck) ? 0 : pricecheck;

                    // bid side we subtract the bid offer  (market maker)
                    row.bidOffer = -.00025;

                    row.LS = (row.TK + row.xCCYBSS + row.bidOffer);

                    lsReport.Add(row);
                }



                // return this dynamic/JSON
                return new
                {
                    status = "success",
                    data = new
                    {
                        ccy = id,
                        curve1 = ycTK.CurveName,  // treasury Kurve
                        curve2 = ycBS.CurveName,  // basis swap curve  (3m vs 3m)
                        //c2Rates = ycBS.DisplayZero(),
                        //curve3 = ycBS3x1.CurveName,  // domestic market's 3x1 curve
                        holidays = ycBS.calendar.holidayCities,
                        fixingdate = _date,
                        bssfixingdate = bssFixDate,
                        valuedate = valueDate,
                        timestamp = ycTK.prices.TimeStamp,
                        timestampUTC = ycTK.prices.TimeStampUTC,
                        rows = lsReport
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