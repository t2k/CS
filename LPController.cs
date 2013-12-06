using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CS;




namespace ZD.Controllers
{



    public class LPController : ApiController
    {

        private ZDEntities data;


        public LPController()
        {
            data = new ZDEntities();
            data.ContextOptions.ProxyCreationEnabled = false;
            data.ContextOptions.LazyLoadingEnabled = true;
        }

        // GET /zd-api/<controller>
        [Authorize]
        public dynamic Get()
        {
            //Re
            int take = int.Parse(Request.RequestUri.ParseQueryString().Get("take")); //? 1000 : Convert.ToInt32(Request.QueryString["take"]);
            int skip = int.Parse(Request.RequestUri.ParseQueryString().Get("skip")); //? 1000 : Convert.ToInt32(Request.QueryString["take"]);
            //var skip = string.IsNullOrEmpty(Request.QueryString["skip"]) ? 1000 : Convert.ToInt32(Request.QueryString["skip"]);

            //var data = new ZDEntities();
            var qry = (from row in data.mxLPs
                       orderby row.TimeStampUTC descending
                       select new
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
                           row.FlowCount
                       }).Skip(skip).Take(take);
            return qry.ToList();
        }

        // GET /zd-api/<controller>
        [Authorize]
        public dynamic Get(int id)
        {
            //var data = new ZDEntities();
            return (from row in data.mxLPs
                    where row.id == id
                    select new
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
                        row.FlowCount
                        //lpflows = row.mxLPLBs.ToList()
                    }).FirstOrDefault();
        }




        // 
        /// <summary>
        /// GET /zd-api/<controller>/<id>   
        /// passing in one or two extn_nb 
        /// the external ref in murex currently refers to a SAP CML#
        /// </summary>
        /// <param name="id">########## or ##########_##########</param>
        /// <returns>json complex result</returns>
        /// 
        [Authorize]
        //public HttpResponseMessage<mxLP> Post(mxLP id)
        public dynamic Post(mxLP id)
        {
            var ctx = new CreditPricing.CreditPricingEntities();
            var ctx2 = new CreditPricing.hdbOracleEntities();
            // ui can pass in 1 or 2 SAP external numbers
            //string parm = string.Join(";", id.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
            if (id.Savedate != null && id.LPType != null)
            {
                CS.mx_rloan_flow sTrade;
                List<CS.mx_rloan_flow> sTradeFlows;

                sTrade = (from r in data.mx_rloan_flow
                          where r.SaveDate == id.Savedate && r.TradeID == id.TradeID && r.Current_period == "Y" && r.Portfolio.Contains("ZD")
                          select r).FirstOrDefault();

                sTradeFlows = (from r in data.mx_rloan_flow
                               where r.SaveDate == id.Savedate && r.TradeID == id.TradeID && r.PeriodEnd >= id.Savedate && r.Portfolio.Contains("ZD")
                               orderby r.PeriodEnd
                               select r).ToList();

                if (sTrade == null)
                {
                    throw new HttpResponseException(HttpStatusCode.NotFound);
                }

                if (id.LPType == "EM")
                {
                    id.ChangeOutstanding = sTrade.Notional;
                }

                id.ChangeOutstanding = -Math.Abs(id.ChangeOutstanding.Value);

                //var data = new CreditPricingEntities();
                CreditPricing.YldCurve ycTK = new CreditPricing.YldCurve("TK_UNGEDECKT", id.Savedate.Value);  // we store TK_Ungedeckt daily in CREDITNET
                CreditPricing.YldCurve ycBS = new CreditPricing.YldCurve(sTrade.CCY, id.Savedate.Value, true);  // note this constructor gathers data from HDB
                CreditPricing.YldCurve discount = new CreditPricing.YldCurve(sTrade.CCY + "CURVE", id.Savedate.Value);  // note this constructor gathers data from HDB

                Double bidTK = ycTK.IsValid ? ycTK.FwdRate(ycTK.spotDate(), sTrade.End.Value, CreditPricing.tkPrice.tkBID) : 0;  // extra sanity checking w/ IsValid (HDB db is not 100% reliable)

                // note: only use HDB Bss for over 1YR otherwise: 0
                Double bss = (ycBS.IsValid && ycBS.calendar.FarDate(ycBS.spotDate(),"1Y") < sTrade.End.Value ) ? ycBS.FwdRate(ycBS.spotDate(), sTrade.End.Value) : 0;

                Double LPR = (bidTK + bss - .00025) * 100;
                LPR = Math.Max(0, LPR);

                var lossBenefitFlows = (from f in sTradeFlows
                                        select
                                            new
                                            {
                                                f.PeriodStart,
                                                f.PeriodEnd,
                                                changeOutstanding = id.ChangeOutstanding.Value,
                                                f.Margin,
                                                LPR = LPR,
                                                discountFactor = discount.GetDF(f.PeriodEnd.Value),
                                                lossBenefit = id.ChangeOutstanding.Value * f.PeriodEnd.Value.Subtract(f.PeriodStart.Value).Days / 360 * (sTrade.Margin - LPR) / 100,
                                                PVlossBenefit = id.ChangeOutstanding.Value * f.PeriodEnd.Value.Subtract(f.PeriodStart.Value).Days / 360 * (sTrade.Margin - LPR) / 100 * discount.GetDF(f.PeriodEnd.Value)
                                            }).ToList();


                //remove existing LP record if found (allow do Overs)
                var lp = (from l in data.mxLPs where l.TradeID == id.TradeID && l.Savedate == id.Savedate select l).FirstOrDefault();

                if (lp != null)
                {
                    data.mxLPs.DeleteObject(lp);
                    data.SaveChanges();
                }

                mxLP lpNew = new mxLP();
                lpNew.CCY = sTrade.CCY;
                lpNew.SAPCML = "N/A";
                lpNew.TradeID = id.TradeID;
                lpNew.TimeStampUTC = DateTime.UtcNow;
                lpNew.Savedate = id.Savedate;
                lpNew.Portfolio = sTrade.Portfolio;
                lpNew.Counterparty = sTrade.Counterpart;
                lpNew.ChangeOutstanding = id.ChangeOutstanding;
                lpNew.FlowCount = sTradeFlows.Count; // lossBenefitFlows.Count;
                lpNew.LPR = LPR;
                lpNew.LPType = id.LPType; // isEM ? "EM" : isBF ? "BF" : isIF ? "IF" : isRF ? "RF" : "NA";
                lpNew.LS = sTrade.Margin;
                lpNew.NewEndDate = sTrade.End; // newEndDate;
                lpNew.OldEndDate = sTrade.End; // oldEnd
                lpNew.ScheduleIndex = sTrade.Index; // internalLoan.Index;
                lpNew.PVLossBenefit = (from f in lossBenefitFlows select f.PVlossBenefit).Sum();

                lpNew.UserName = Thread.CurrentPrincipal.Identity.Name; // Request.GetUserPrincipal().Identity.Name;

                foreach (var f in lossBenefitFlows)
                {
                    lpNew.mxLPLBs.Add(new mxLPLB()
                    {
                        PdStart = f.PeriodStart,
                        PdEnd = f.PeriodEnd,
                        LPR = f.LPR,
                        LS = f.Margin,
                        ChangeOutstanding = f.changeOutstanding,
                        DiscountFactor = f.discountFactor,
                        LossBenefit = f.lossBenefit,
                        PVLossBenefit = f.PVlossBenefit
                    });
                }
                data.mxLPs.AddObject(lpNew);
                data.SaveChanges();

                // re - retrieve to sort out serialization
                var retVal = new
                {
                    id = lpNew.id,
                    CCY = sTrade.CCY,
                    SAPCML = "N/A",
                    TradeID = id.TradeID,
                    TradeID_Internal = id.TradeID,
                    TradeID_External = 0,
                    TimeStampUTC = DateTime.UtcNow, // ().u lpNew.TimeStampUTC,
                    Savedate = id.Savedate,
                    Portfolio = sTrade.Portfolio,
                    Counterparty = sTrade.Counterpart,
                    ChangeOutstanding = id.ChangeOutstanding,
                    FlowCount = sTradeFlows.Count,
                    LPR = LPR,
                    bss=bss,
                    LPType = id.LPType,
                    LS = sTrade.Margin,
                    NewEndDate = sTrade.End,
                    OldEndDate = sTrade.End,
                    ScheduleIndex = sTrade.Index,
                    PVLossBenefit = lpNew.PVLossBenefit,
                    UserName = lpNew.UserName
                };
                return retVal;
            }
            else
            {

                DateTime? today = (from r in data.mx_rloan_flow select r.SaveDate).Max();
                string[] parms = id.SAPCML.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

                //  
                string oldSAP;
                string newSAP;

                //return string.Format("webapi value {0}",id);

                CS.mx_rloan_flow externalLoan;
                CS.mx_rloan_flow internalLoan;
                List<CS.mx_rloan_flow> externalFlows;
                List<CS.mx_rloan_flow> internalFlows;

                int? tradeID_Internal, tradeID_External;

                try
                {
                    if (parms.Length == 1)  // if one number is passed 
                    {
                        oldSAP = parms[0];

                        //find the external loan current balance by oldSAP# 
                        externalLoan = (from r in data.mx_rloan_flow
                                        where r.SaveDate == today && r.EXT_NB == oldSAP && r.Current_period == "Y" && r.Internal == "N"
                                        select r).FirstOrDefault();

                        tradeID_External = externalLoan != null ? externalLoan.TradeID : 0;

                        //find the internal LOAN current balance in portfolio contains 'ZD'
                        internalLoan = (from r in data.mx_rloan_flow
                                        where r.SaveDate == today && r.EXT_NB == oldSAP && r.Current_period == "Y" && r.Internal == "Y" && r.Portfolio.Contains("ZD")
                                        select r).FirstOrDefault();

                        tradeID_Internal = internalLoan != null ? internalLoan.TradeID : 0;

                        //find the external loan flows
                        externalFlows = (from r in data.mx_rloan_flow
                                         where r.SaveDate == today && r.TradeID == tradeID_External && r.PeriodEnd >= today
                                         orderby r.PeriodEnd
                                         select r).ToList();

                        //find the internal loan flows
                        internalFlows = (from r in data.mx_rloan_flow
                                         where r.SaveDate == today && r.TradeID == tradeID_Internal && r.PeriodEnd >= today && r.Portfolio.Contains("ZD")
                                         orderby r.PeriodEnd
                                         select r).ToList();
                    }
                    else  // two numbers were passed in the oldSAP is the first (left to right) and the newSAP is the second
                    {
                        oldSAP = parms[0];
                        newSAP = parms[1];

                        // passed in 2 SAP#'s => cancel and reissue
                        //find the external loan current balance
                        // external matched newSAP and r.Internal==N
                        externalLoan = (from r in data.mx_rloan_flow
                                        where r.SaveDate == today && r.EXT_NB == newSAP && r.Current_period == "Y" && r.Internal == "N"
                                        select r).FirstOrDefault();

                        tradeID_External = externalLoan != null ? externalLoan.TradeID : 0;


                        internalLoan = (from r in data.mx_rloan_flow
                                        where r.SaveDate == today && r.EXT_NB == oldSAP && r.Current_period == "Y" && r.Internal == "Y" && r.Portfolio.Contains("ZD")

                                        select r).FirstOrDefault();

                        tradeID_Internal = internalLoan != null ? internalLoan.TradeID : 0;

                        //find the external loan flows
                        externalFlows = (from r in data.mx_rloan_flow
                                         where r.SaveDate == today && r.TradeID == tradeID_External && r.PeriodEnd >= today
                                         orderby r.PeriodEnd
                                         select r).ToList();

                        //find the internal loan flows
                        internalFlows = (from r in data.mx_rloan_flow
                                         where r.SaveDate == today && r.TradeID == tradeID_Internal && r.PeriodEnd >= today && r.Portfolio.Contains("ZD")
                                         orderby r.PeriodEnd
                                         select r).ToList();
                    }

                    // early mature

                    DateTime newEndDate = externalFlows.Last().PeriodEnd.Value;
                    DateTime oldEndDate = internalFlows.Last().PeriodEnd.Value;
                    Boolean isEM = newEndDate < oldEndDate;
                    Boolean isBF = false;
                    Boolean isIF = false;
                    Boolean isRF = false;

                    Double newOutstanding;
                    Double changeOutstanding;

                    if (isEM)
                    {
                        changeOutstanding = -internalFlows.Last().Notional.Value;
                        newOutstanding = 0;
                    }
                    else
                    {
                        changeOutstanding = Math.Round(externalFlows.Last().Notional.Value, 2) - Math.Round(internalFlows.Last().Notional.Value, 2);
                        newOutstanding = externalFlows.Last().Notional.Value;
                        isBF = changeOutstanding < 0;
                        isIF = changeOutstanding > 0;
                        isRF = changeOutstanding == 0;
                    }

                    Double LS = internalLoan.Margin.Value;
                    string CCY = externalLoan != null ? externalLoan.CCY : "USD";
                    DateTime saveDate = today.Value;

                    DateTime maxBSS = (from b in ctx2.BSSes where b.SYMBOL.StartsWith(CCY) select b.TRADE_DATE).Max();
                    DateTime bssFixDate;

                    if (maxBSS < today.Value)
                    {
                        bssFixDate = maxBSS;
                    }
                    else
                    {
                        bssFixDate = today.Value;
                    }

                    //var data = new CreditPricingEntities();
                    CreditPricing.YldCurve ycTK = new CreditPricing.YldCurve("TK_UNGEDECKT", today.Value);  // we store TK_Ungedeckt daily in CREDITNET
                    CreditPricing.YldCurve ycBS = new CreditPricing.YldCurve(CCY, bssFixDate, true);  // note this constructor gathers data from HDB
                    CreditPricing.YldCurve discount = new CreditPricing.YldCurve(CCY + "CURVE", today.Value);  // note this constructor gathers data from HDB

                    Double bidTK = ycTK.IsValid ? ycTK.FwdRate(ycTK.spotDate(), oldEndDate, CreditPricing.tkPrice.tkBID) : 0;  // extra sanity checking w/ IsValid (HDB db is not 100% reliable)

                    // only use HDB bss for dates > spot 1YR otherwise, 0
                    Double bss = (ycBS.IsValid && ycBS.calendar.FarDate(ycBS.spotDate(),"1Y") < oldEndDate) ? ycBS.FwdRate(ycBS.spotDate(), oldEndDate) : 0;
                    Double LPR = (bidTK + bss - .00025) * 100;
                    // no negatives
                    LPR = Math.Max(0, LPR);

                    var lossBenefitFlows = (from f in internalFlows
                                            select
                                                new
                                                {
                                                    f.PeriodStart,
                                                    f.PeriodEnd,
                                                    oldOutstanding = f.Notional,
                                                    newOutstanding = newOutstanding,
                                                    changeOutstanding = changeOutstanding,
                                                    f.Margin,
                                                    LPR = LPR,
                                                    discountFactor = discount.GetDF(f.PeriodEnd.Value),
                                                    lossBenefit = changeOutstanding * f.PeriodEnd.Value.Subtract(f.PeriodStart.Value).Days / 360 * (LS - LPR) / 100,
                                                    PVlossBenefit = changeOutstanding * f.PeriodEnd.Value.Subtract(f.PeriodStart.Value).Days / 360 * (LS - LPR) / 100 * discount.GetDF(f.PeriodEnd.Value)
                                                }).ToList();

                    // persistence model storage
                    var lp = (from l in data.mxLPs where l.TradeID == internalLoan.TradeID && l.Savedate == today.Value select l).FirstOrDefault();

                    if (lp != null)
                    {
                        data.mxLPs.DeleteObject(lp);
                        data.SaveChanges();
                    }

                    mxLP lpNew = new mxLP();
                    lpNew.CCY = CCY;
                    lpNew.SAPCML = id.SAPCML;
                    lpNew.TradeID = internalLoan.TradeID;
                    lpNew.TimeStampUTC = DateTime.UtcNow;
                    lpNew.Savedate = saveDate;
                    lpNew.Portfolio = internalLoan.Counterpart;
                    lpNew.Counterparty = externalLoan.Counterpart;
                    lpNew.ChangeOutstanding = changeOutstanding;
                    lpNew.FlowCount = lossBenefitFlows.Count;
                    lpNew.LPR = LPR;
                    lpNew.LPType = isEM ? "EM" : isBF ? "BF" : isIF ? "IF" : isRF ? "RF" : "NA";
                    lpNew.LS = LS;
                    lpNew.NewEndDate = newEndDate;
                    lpNew.OldEndDate = oldEndDate;
                    lpNew.ScheduleIndex = internalLoan.Index;
                    lpNew.PVLossBenefit = (from f in lossBenefitFlows select f.PVlossBenefit).Sum();
                    lpNew.UserName = Thread.CurrentPrincipal.Identity.Name; // Request.GetUserPrincipal().Identity.Name;

                    foreach (var f in lossBenefitFlows)
                    {
                        lpNew.mxLPLBs.Add(new mxLPLB()
                        {
                            PdStart = f.PeriodStart,
                            PdEnd = f.PeriodEnd,
                            LPR = f.LPR,
                            LS = f.Margin,
                            ChangeOutstanding = f.changeOutstanding,
                            DiscountFactor = f.discountFactor,
                            LossBenefit = f.lossBenefit,
                            PVLossBenefit = f.PVlossBenefit
                        });
                    }
                    data.mxLPs.AddObject(lpNew);
                    data.SaveChanges();

                    // re - retrieve to sort out serialization
                    var test = new
                    {
                        id = lpNew.id,
                        CCY = lpNew.CCY,
                        SAPCML = lpNew.SAPCML,
                        TradeID = lpNew.TradeID,
                        TradeID_Internal = tradeID_Internal,
                        TradeID_External = tradeID_External,
                        TimeStampUTC = lpNew.TimeStampUTC,
                        Savedate = lpNew.Savedate,
                        Portfolio = lpNew.Portfolio,
                        Counterparty = lpNew.Counterparty,
                        ChangeOutstanding = lpNew.ChangeOutstanding,
                        FlowCount = lpNew.FlowCount,
                        LPR = lpNew.LPR,
                        bss = bss,
                        LPType = lpNew.LPType,
                        LS = lpNew.LS,
                        NewEndDate = lpNew.NewEndDate,
                        OldEndDate = lpNew.OldEndDate,
                        ScheduleIndex = lpNew.ScheduleIndex,
                        PVLossBenefit = lpNew.PVLossBenefit,
                        UserName = lpNew.UserName
                    };
                    //from row in data.mxLPs where row.id==lpNew.id select row).FirstOrDefault();


                    //var response = new HttpResponseMessage<dynamic>(test)
                    //	{ 
                    //		StatusCode = HttpStatusCode.Created
                    //	};
                    return test;  // response;
                }
                catch (Exception ex)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError) { ReasonPhrase = ex.Message });
                }
            }
        }

        // PUT /api/<controller>/5
        public void Put(int id, string value)
        {
        }

        // DELETE /api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}