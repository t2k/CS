using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;


// Liquidity Spread Controllers
namespace ZD.Controllers
{
    public class LSCurrencyController : ApiController
    {
        // GET /api/<controller>
        public dynamic Get()
        {
            try
            {
                var data = new CS.ZDEntities();

                //last saved dates
                var qdates = (from r in data.ratesaves
                              orderby r.TimeStamp descending
                              select r).Take(1).SingleOrDefault();

				var ccys = (from u in data.zd_ccyterms 
					orderby u.Currency select u).ToList();

                var qry = from u in ccys
						  where u.Period=="3M"
                          select new
                          {
							id = u.id,
							symbol=u.Currency,
							name=u.Currency,
							description=u.RefIndex,
							arbCCY=u.Currency,
							shortModel = (from m in data.zdShortModels where m.arbCCY == u.Currency && m.curve1 == "TK_LIQSHORT" select m.id).SingleOrDefault(),
							liquidity = (from c in ccys where c.Currency==u.Currency select new { 
								c.Period,
								c.RefIndex
							  }).ToList(),
							u.DCC,
							fixingCalendar = u.FixingCalendar,
							valueCalendar=u.ValueCalendar,
							valueDay=u.ValueDay,
							maxTenor=u.MaxTenor,
							fixingdate = qdates.SaveDate
						};

				return new
				{
					status = "success",
					data = new
					{
						timestamp = qdates.TimeStamp,
						timestampUTC = qdates.TimeStampUTC,
						results = qry.ToList()
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


        // GET /api/<controller>/aud
        public dynamic Get(int id)
        {
            try
            {
                var data = new CS.ZDEntities();
                var stmodel = (from m in data.zdShortModels where m.id == id select m).FirstOrDefault();


				var ccys = (from u in data.zd_ccyterms 
					orderby u.Currency select u).ToList();

                var qry = from u in ccys where u.Period=="3M" && u.Currency==stmodel.arbCCY
                          select new
                          {
                            id = u.id,
                            symbol=u.Currency,
                            name=u.Currency,
                            description=u.RefIndex,
                            arbCCY=u.Currency,
                            shortModel = (from m in data.zdShortModels where m.arbCCY == u.Currency && m.curve1 == "TK_LIQSHORT" select m.id).SingleOrDefault(),
                            liquidity = (from c in ccys where c.Currency==u.Currency select new { 
                                c.Period,
                                c.RefIndex
                                }).ToList(),
                            u.DCC,
                            fixingCalendar = u.FixingCalendar,
                            valueCalendar=u.ValueCalendar,
                            valueDay=u.ValueDay,
                            maxTenor=u.MaxTenor,
                            fixingdate = DateTime.Today
                         };

				return new
				{
					status = "success",
					data = new
					{
						timestamp = DateTime.Now,
						timestampUTC = DateTime.UtcNow,
						results = qry
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



                    // GET /api/<controller>/aud
        public dynamic Get(string ccy)
        {
            try
            {
                var data = new CS.ZDEntities();


                var ccys = (from u in data.zd_ccyterms
                            orderby u.Currency
                            select u).ToList();

                var qry = from u in ccys
                          where u.Period == "3M" && u.Currency == ccy
                          select new
                          {
                              id = u.id,
                              symbol = u.Currency,
                              name = u.Currency,
                              description = u.RefIndex,
                              arbCCY = u.Currency,
                              shortModel = (from m in data.zdShortModels where m.arbCCY == u.Currency && m.curve1 == "TK_LIQSHORT" select m.id).SingleOrDefault(),
                              liquidity = (from c in ccys
                                           where c.Currency == u.Currency
                                           select new
                                           {
                                               c.Period,
                                               c.RefIndex
                                           }).ToList(),
                              u.DCC,
                              fixingCalendar = u.FixingCalendar,
                              valueCalendar = u.ValueCalendar,
                              valueDay = u.ValueDay,
                              maxTenor = u.MaxTenor,
                              fixingdate = DateTime.Today
                          };

                return new
                {
                    status = "success",
                    data = new
                    {
                        timestamp = DateTime.Now,
                        timestampUTC = DateTime.UtcNow,
                        results = qry
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

            
            /*

        // POST /api/<controller>
        public void Post(string value)
        {
        }

        // PUT /api/<controller>/5
        public void Put(int id, string value)
        {
        }

        // DELETE /api/<controller>/5
        public void Delete(int id)
        {
        }
         */
    }
}