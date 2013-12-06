using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;



// Liquidity Spread Controllers
namespace ZD.Controllers
{
    public class appStateController : ApiController
    {
        // GET /zd-api/<controller>
        public dynamic Get()
        {
            try
            {
                DateTime bssDate;
                try
                {
                    //var ctx = new CreditPricing.hdbOracleEntities();
                    //bssDate = ctx.BSSes.Where(i => i.SYMBOL.StartsWith("USD")).Max(i => i.TRADE_DATE);
                    bssDate = DateTime.Today;
                    //ctx.Dispose();

                }
                catch (Exception)
                {
                    bssDate = DateTime.Today;
                }
                var ctx2 = new CreditPricing.CreditPricingEntities();
                DateTime rateDate = ctx2.RATETimeStamps.Max(r => r.SaveDate);
                ctx2.Dispose();
                var ctx3 = new CS.ZDEntities();
                DateTime? mxDate = ctx3.mx_rloan_flow.Max(i => i.SaveDate);
                var ports = ctx3.mx_rloan_flow.Where(i => i.SaveDate == mxDate.Value && i.Internal == "N").Select(i => i.Portfolio).Distinct().ToList();
                var extNBs = ctx3.mx_rloan_flow.Where(i => i.SaveDate == mxDate.Value && i.Internal == "N").Select(i => i.EXT_NB).Distinct().ToList();
                var CCYs = ctx3.mx_rloan_flow.Where(i => i.SaveDate == mxDate.Value && i.Internal == "N").Select(i => i.CCY).Distinct().ToList();
                var LPTypes = new List<string>() { "BF", "EM" };
                var ZDReports = new List<string>() { "FLE", "LVA", "ARF" };


                return new
                {
                    CCY = "USD",
                    status = "success",
                    bssDate = bssDate,
                    rateDate = rateDate,
                    mxDate = mxDate,
                    ports = ports,
                    extNBs = extNBs,
                    CCYs = CCYs,
                    lptypes = LPTypes.ToList(),
                    ZDReports = ZDReports.ToList()
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