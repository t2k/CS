using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using CS;

// Liquidity Spread Controllers
namespace ZD.Controllers
{
    public class IFaceLogController : ApiController
    {
        // GET /zd-api/<controller>
        public dynamic Get()
        {
            int skip = int.Parse(Request.RequestUri.ParseQueryString().Get("skip")); //? 1000 : Convert.ToInt32(Request.QueryString["take"]);
            int take = int.Parse(Request.RequestUri.ParseQueryString().Get("take")); //? 1000 : Convert.ToInt32(Request.QueryString["take"]);
            string name = Request.RequestUri.ParseQueryString().Get("name"); //? 1000 : Convert.ToInt32(Request.QueryString["take"]);

            try
            {
                var ctx = new InterfaceEntities();

                return new
                {
                    status = "success",
                    ifacename = name,
                    timestamp = DateTime.Now,
                    rows = (from i in ctx.IFaceLogs
                            where i.IFaceName.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                            orderby i.IFaceTimeStamp descending
                            select new
                            {
                                i.IFaceDate,
                                i.IFaceTimeStamp,
                                i.IFaceName,
                                i.IFaceCount,
                                i.IFaceNote,
                                i.IFaceUser,
                                i.IFaceStatus
                            }).Skip(skip).Take(take).ToList()
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
    }
}