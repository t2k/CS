using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using CreditPricing;

// Liquidity Spread Controllers
namespace ZD.Controllers
{
    public class LSSTController : ApiController
    {
        // GET /api/<controller>
        public dynamic Get(int id, DateTime? _date)
        {
            string _maturity = Request.RequestUri.ParseQueryString().Get("maturity"); //? 1000 : Convert.ToInt32(Request.QueryString["take"]);
            try
            {
                fxarbModel model = new fxarbModel(id, _date.Value);

                DateTime mDate;
                if (_maturity != null) //if maturity is parsed in via QUERYSTRING then create a List<Tenor> of just 1 item (the BESPOKE date)
                {
                    Boolean dateparse = DateTime.TryParse(_maturity, out mDate);
                    return model.arbReportLSapi(mDate);  //new
                }
                else
                {
                    return model.arbReportLSapi();  //new
                }
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