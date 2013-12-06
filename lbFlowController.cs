using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace ZD.Controllers
{
    public class lbFlowController : ApiController
    {
        private CS.ZDEntities data;

        public lbFlowController()
        {
            data = new CS.ZDEntities();
            data.ContextOptions.ProxyCreationEnabled = false;
            data.ContextOptions.LazyLoadingEnabled = true;
        }

        // GET /api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }


        [Authorize]
        public dynamic Get(int id)
        {
            //var matched = table1.Where (t =>table2.Any (ta =>ta.EXT_NB==t.ContractNumber ) ).ToList();
            CS.mxLP row = data.mxLPs.Where(x => x.id == id).Select(i => i).SingleOrDefault();
            var rows = (from i in row.mxLPLBs
                        select new
                        {
                            i.PdStart,
                            i.PdEnd,
                            i.LS,
                            i.LPR,
                            i.LossBenefit,
                            i.DiscountFactor,
                            i.PVLossBenefit
                        }).ToList();


            //var data = new ZDEntities();
            return (new
                    {
                        row.id,
                        row.UserName,
                        row.Savedate,
                        row.TimeStampUTC,
                        row.TradeID,
                        row.CCY,
                        row.Portfolio,
                        row.Counterparty,
                        row.ScheduleIndex,
                        row.LPType,
                        row.SAPCML,
                        row.LS,
                        row.LPR,
                        row.ChangeOutstanding,
                        row.PVLossBenefit,
                        row.NewEndDate,
                        row.OldEndDate,
                        row.FlowCount,
                        lbflows = rows
                    });
        }


        /*
                // POST /api/<controller>
                public void Post(string value)
                {
                }

                // PUT /api/<controller>/5
                public void Put(int id, string value)
                {
                }
        */

    }
}