using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CS;
using ZD;
// nice!  the compilation order here 
using CreditPricing;


namespace ZD
{
    public enum TimeBuckets
    {
        ZD,
        MM,
        AnSer
    }

    // javascript class properties...
    // contructor for drag drop object...
    //function DropTrade(_type, _sizeM, _period, _endDate) {
    //    this.zdtradeID = ++zdtradeID;
    //    this.typeAL = _type;
    //    this.sizeM = _size;
    //    this.period = _period;
    //    this.endDate = _endDate;
    //    this.ccy = null;
    //    this.valDate = null;
    //    this.rateLS = null;
    //    this.applyRpt = function (strfilter) {
    //        // portfolio: CI_ZD_LS,NY_ZD_LS; ccy: USD; date: 5/20/2011
    //        var tmp = strfilter.split(";")
    //        this.ccy = tmp[1].split(":")[1].trim();
    //        this.valDate = tmp[2].split(":")[1].trim();
    //    }
    //};

    public class dropTrade  // drag/drop trade passed in from client
    {
        public int zdtradeID { get; set; }
        public string typeAL { get; set; }
        public string sizeM { get; set; }
        public string period { get; set; }
        public DateTime endDate { get; set; }
        public string ccy { get; set; }
        public DateTime valDate { get; set; }
        public double rateLS { get; set; }
    }

    public class ZDTradeOptions
    {
        public DateTime SaveDate { get; set; }
        public int TradeID { get; set; }
        public string PayRec { get; set; }
    }

    /// <summary>
    /// STANARD: report options: list of portfolios, a CCY and a savedate
    /// </summary>
    public class ZDOptions1
    {
        public DateTime evalDate { get; set; }  //savedate
        public string ccy { get; set; }           // currency
        public List<string> portfolios { get; set; }  // list of portfolios
    }

    /// <summary>
    ///  report options#2  exend options#1 adding periodStartDate and periodEndDate
    /// </summary>
    public class ZDOptions2 : ZDOptions1
    {
        public DateTime periodStartDate { get; set; }
        public DateTime periodEndDate { get; set; }
    }

    /// <summary>
    /// report options#3  exend options#1 by adding a list of droptrades (user defined trades added) date, ccy, portfolios List
    /// </summary>
    public class ZDOptions3 : ZDOptions1
    {
        public List<dropTrade> clientTrades { get; set; }  // passed in list of client objects
    }


    /// <summary>
    /// pass in an integer for reporting
    /// </summary>
    public class AnserReportParms
    {
        public int anserID { get; set; }
        public DateTime evalDate { get; set; }
        public double mvDelta { get; set; }
    }


    /// <summary>
    /// 
    /// </summary>
    public class AnSerOptions
    {
        public DateTime evalDate { get; set; }
        public Boolean fedElig { get; set; }
        public Boolean fundMV { get; set; }
    }

    public class AnSerBond
    {
        public string cusip { get; set; }
        public double mv { get; set; }
        public Boolean fedElig { get; set; }
        public List<anserRMBSFlow> flows { get; set; }
    }


    /// <summary>
    ///  base class for a time Bucketing 
    /// </summary>
    public class Period
    {
        public string ID { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int Days()
        {
            return End.Subtract(Start).Days;
        }
    }


    /// <summary>
    /// ALMBucket inherits from Period base class
    /// </summary>
    public class ALMBucket : Period
    {
        public double Asset { get; set; }
        public double Recv { get; set; }

        public double ANSERAsset { get; set; }
        public double MVAsset { get; set; }
        public double ANSERRecv { get; set; }

        public double Liab { get; set; }
        public double Pay { get; set; }
        public double Net { get; set; }
        public double OptimalAdj { get; set; }

        /// <summary>
        /// flow principal and interest...
        /// </summary>
        /// <param name="flow"></param>
        public void FlowPI(mx_rloan_flow flow)
        {
            DateTime flowstart = (DateTime)flow.PeriodStart;
            DateTime flowend = (DateTime)flow.PeriodEnd;

            // this is the only test we need to know if the flow get applied to this period... (do periods overlap?)
            if (flowstart < this.End && flowend > this.Start)
            {
                if (flow.PayRec.Trim().Equals("Pay", StringComparison.InvariantCultureIgnoreCase)) //  trim just in case... older import didn't handle the padded spaces...
                {  // handle pay flows
                    if (flowstart > this.Start && flowend >= this.End)  // flow starts mid bucket to end or past the end..
                    {
                        this.Liab += (double)flow.Notional * (double)End.Subtract(flowstart).Days / this.Days();
                        Pay += (double)flow.Notional * (double)flow.TotalRate / 100 * (double)End.Subtract(flowstart).Days / this.Days();
                    }
                    else if (flowstart <= this.Start && flowend >= this.End)  // full period applies
                    {
                        this.Liab += (double)flow.Notional * (double)End.Subtract(Start).Days / this.Days();  // weighted for entire period
                        this.Pay += (double)flow.Notional * (double)flow.TotalRate / 100 * (double)End.Subtract(Start).Days / this.Days();  // weighted for entire period
                    }
                    else if (flowstart <= this.Start && flowend < this.End) // left part
                    {
                        this.Liab += (double)flow.Notional * (double)flowend.Subtract(this.Start).Days / this.Days();
                        this.Pay += (double)flow.Notional * (double)flow.TotalRate / 100 * (double)flowend.Subtract(this.Start).Days / this.Days();
                    }
                    else if (flowstart >= this.Start && flowend <= this.End)
                    {
                        this.Liab += (double)flow.Notional * (double)flowend.Subtract(flowstart).Days / this.Days();  // weighted for entire period
                        this.Pay += (double)flow.Notional * (double)flow.TotalRate / 100 * (double)flowend.Subtract(flowstart).Days / this.Days();  // weighted for entire period
                    }
                }
                else
                {  // handle receive flows
                    if (flowstart > this.Start && flowend >= this.End)  // flow starts mid bucket to end or past the end..
                    {
                        if (End > flowstart)
                        {
                            this.Asset += (double)flow.Notional * (double)End.Subtract(flowstart).Days / this.Days();
                            this.Recv += (double)flow.Notional * (double)flow.TotalRate / 100 * (double)End.Subtract(flowstart).Days / this.Days();
                        }

                    }
                    else if (flowstart <= this.Start && flowend >= this.End)  // full period applies
                    {
                        if (this.End > Start)
                        { // sort of an assert
                            this.Asset += (double)flow.Notional * (double)End.Subtract(Start).Days / this.Days();  // weighted for entire period
                            this.Recv += (double)flow.Notional * (double)flow.TotalRate / 100 * (double)this.End.Subtract(Start).Days / this.Days();
                        }
                    }
                    else if (flowstart <= this.Start && flowend < this.End) // left part
                    {
                        if (flowend > Start)  // another assert
                        {
                            this.Asset += (double)flow.Notional * (double)flowend.Subtract(this.Start).Days / this.Days();
                            this.Recv += (double)flow.Notional * (double)flow.TotalRate / 100 * (double)flowend.Subtract(this.Start).Days / this.Days();
                        }
                    }
                    else if (flowstart >= this.Start && flowend <= this.End)
                    {
                        if (flowend > flowstart)
                        {
                            this.Asset += (double)flow.Notional * (double)flowend.Subtract(flowstart).Days / this.Days();  // weighted for entire period
                            this.Recv += (double)flow.Notional * (double)flow.TotalRate / 100 * (double)flowend.Subtract(flowstart).Days / this.Days();  // weighted for entire period
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ANSER asset flows, have been morphed into mx_rlon_flow records the MARGIN field used for Market Value
        /// </summary>
        /// <param name="flow"></param>
        public void FlowANSERPI(mx_rloan_flow flow)
        {
            DateTime flowstart = (DateTime)flow.PeriodStart;
            DateTime flowend = (DateTime)flow.PeriodEnd;

            // this is the only test we need to know if the flow get applied to this period... (do periods overlap?)
            if (flowstart < this.End && flowend > this.Start)
            {
                if (flowstart > this.Start && flowend >= this.End)  // flow starts mid bucket to end or past the end..
                {
                    if (End > flowstart)
                    {
                        this.ANSERAsset += (double)flow.Notional * (double)End.Subtract(flowstart).Days / this.Days();
                        this.MVAsset += (double)flow.Margin * (double)End.Subtract(flowstart).Days / this.Days();
                        this.ANSERRecv += (double)flow.Notional * (double)flow.TotalRate / 100 * (double)End.Subtract(flowstart).Days / this.Days();
                    }

                }
                else if (flowstart <= this.Start && flowend >= this.End)  // full period applies
                {

                    if (this.End > Start)
                    { // sort of an assert
                        this.MVAsset += (double)flow.Margin * (double)End.Subtract(Start).Days / this.Days();  // weighted for entire period
                        this.ANSERAsset += (double)flow.Notional * (double)End.Subtract(Start).Days / this.Days();  // weighted for entire period
                        this.ANSERRecv += (double)flow.Notional * (double)flow.TotalRate / 100 * (double)this.End.Subtract(Start).Days / this.Days();
                    }

                }
                else if (flowstart <= this.Start && flowend < this.End) // left part
                {

                    if (flowend > Start)  // another assert
                    {
                        this.MVAsset += (double)flow.Margin * (double)flowend.Subtract(this.Start).Days / this.Days();
                        this.ANSERAsset += (double)flow.Notional * (double)flowend.Subtract(this.Start).Days / this.Days();
                        this.ANSERRecv += (double)flow.Notional * (double)flow.TotalRate / 100 * (double)flowend.Subtract(this.Start).Days / this.Days();
                    }

                }
                else if (flowstart >= this.Start && flowend <= this.End)
                {
                    if (flowend > flowstart)
                    {
                        this.MVAsset += (double)flow.Margin * (double)flowend.Subtract(flowstart).Days / this.Days();  // weighted for entire period
                        this.ANSERAsset += (double)flow.Notional * (double)flowend.Subtract(flowstart).Days / this.Days();  // weighted for entire period
                        this.ANSERRecv += (double)flow.Notional * (double)flow.TotalRate / 100 * (double)flowend.Subtract(flowstart).Days / this.Days();  // weighted for entire period
                    }

                }
            }
        }
    }


    /// <summary>
    /// dictionary/collection of ALMBuckets
    /// </summary>
    public class MaturitySet : List<ALMBucket>
    {
        /// <summary>
        /// for not this is a fixed type// we can extend, inherit from a base period class etc...
        /// generate 1-11M then 1Y through 10yr
        /// </summary>
        /// <param name="_date0"></param>
        public MaturitySet(DateTime _date0, TimeBuckets _tb)
        {

            CreditPricing.Calendar cal = new Calendar("USD", _date0);
            DateTime spotDate = cal.Workdays(_date0, 2);

            int[] days;
            int[] weeks;
            int[] months;
            int[] years;

            switch (_tb)
            {
                case TimeBuckets.ZD:
                    days = new int[] { 1, 2 };
                    weeks = new int[] { 1, 2, 3 };
                    months = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
                    years = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 30 };
                    break;
                case TimeBuckets.AnSer:
                    days = new int[] { };
                    weeks = new int[] { };
                    months = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
                    //months = new int[] {1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24, 25, 26, 27, 28, 29, 30, 31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50, 51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,
                    //73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120};
                    years = new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 30, 40, 50 };
                    break;
                case TimeBuckets.MM:
                    days = new int[] { 1, 2, 3 };
                    weeks = new int[] { 1 };
                    months = new int[] { 1, 2, 3, 6, 7, 8, 9, 10, 11 };
                    years = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 15, 20, 30, 40 };
                    break;
                default:
                    days = new int[] { 1, 2 };
                    weeks = new int[] { 1 };
                    months = new int[] { 1, 2, 3, 6, 9 };
                    years = new int[] { 1, 2, 3, 4, 5, 10, 20, 30 };
                    break;
            }



            // days
            DateTime prevEnd = _date0;
            for (int i = 0; i < days.Count(); i++)
            {
                DateTime end = cal.Workdays(_date0, days[i]);
                string id = string.Format("{0}D", days[i]);
                Add(new ALMBucket { ID = id, Start = prevEnd, End = end });
                prevEnd = end;
            }

            //weeks out of spot date
            for (int i = 0; i < weeks.Count(); i++)
            {
                string id = string.Format("{0}W", weeks[i]);
                DateTime end = cal.FarDate(spotDate, id); // _date0.AddDays(weeks[i] * 7);
                Add(new ALMBucket { ID = id, Start = prevEnd, End = end });
                prevEnd = end;
            }
            //months
            for (int i = 0; i < months.Count(); i++)
            {
                string id = string.Format("{0}M", months[i]);
                DateTime end = cal.FarDate(spotDate, id); // _date0.AddMonths(months[i]);
                Add(new ALMBucket { ID = id, Start = prevEnd, End = end });
                prevEnd = end;
            }
            //years
            for (int i = 0; i < years.Count(); i++)
            {
                string id = string.Format("{0}Y", years[i]);
                DateTime end = cal.FarDate(spotDate, id); // _date0.AddYears(years[i]);
                Add(new ALMBucket { ID = id, Start = prevEnd, End = end });
                prevEnd = end;
            }
        }

        /// <summary>
        /// apply a given periodic flow across ALMbuckets
        /// </summary>
        /// <param name="flow"></param>
        public void ApplyFlow(mx_rloan_flow flow)
        {
            foreach (var bucket in this) // values is the collection held in 'this' List<ALMBucket>
            {
                bucket.FlowPI(flow);
            }
        }


        public void ApplyANSERFlow(mx_rloan_flow flow)
        {
            foreach (var bucket in this) // values is the collection held in 'this' List<ALMBucket>
            {
                bucket.FlowANSERPI(flow);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public void FindOptimalANSERTrades()
        {
            double cumlTrade = 0;
            for (int i = this.Count - 1; i >= 0; i--)
            {
                ALMBucket bucket = this[i];
                double net_AL = bucket.Liab - (bucket.MVAsset + bucket.Asset);
                bucket.OptimalAdj = -(cumlTrade + net_AL);
                cumlTrade += bucket.OptimalAdj;
            }
        }


        public void FindOptimalTrades()
        {
            double cumlTrade = 0;
            for (int i = this.Count - 1; i >= 0; i--)
            {
                ALMBucket bucket = this[i];
                double net_AL = bucket.Liab - bucket.Asset;
                bucket.OptimalAdj = -(cumlTrade + net_AL);
                cumlTrade += bucket.OptimalAdj;
            }
        }
    }

    public class MurexFlows : List<mx_rloan_flow>
    {

        /// <summary>
        /// return a of flows, param driven, list of ports, ccy and date with boolean, USED internally
        /// </summary>
        /// <param name="_ports"></param>
        /// <param name="_ccy"></param>
        /// <param name="_date"></param>
        /// <param name="_onlycurrent"></param>
        /// <returns></returns>
        /// 
        public List<mx_rloan_flow> GetPortFlows(List<string> _ports, string _ccy, DateTime _date, Boolean _onlycurrent)
        {
            var ctx = new ZDEntities();
            ctx.CommandTimeout = 180;
            if (_onlycurrent)
            {
                return (from flow in ctx.mx_rloan_flow
                        where flow.SaveDate == _date && flow.PeriodEnd >= _date && flow.Current_period == "Y" && flow.CCY == _ccy && _ports.Contains(flow.Portfolio)
                        orderby flow.End, flow.PeriodEnd
                        select flow).ToList();
            }
            else
            {
                return (from flow in ctx.mx_rloan_flow
                        where flow.SaveDate == _date && flow.PeriodEnd >= _date && flow.CCY == _ccy && _ports.Contains(flow.Portfolio) // && flow.Counterpart!="DZBKNYK"
                        orderby flow.End, flow.PeriodEnd
                        select flow).ToList();
            }
        }


        /// <summary>
        /// returns flows withing a time period...
        /// </summary>
        /// <param name="_ports"></param>
        /// <param name="_ccy"></param>
        /// <param name="_date"></param>
        /// <param name="_periodStart"></param>
        /// <param name="_periodEnd"></param>
        /// <returns></returns>
        public List<mx_rloan_flow> GetPortFlows(List<string> _ports, string _ccy, DateTime _date, DateTime _periodStart, DateTime _periodEnd)
        {
            var ctx = new ZDEntities();
            {

                var qry = (from flow in ctx.mx_rloan_flow
                           //where flow.SaveDate == _date && flow.CCY == _ccy && _ports.Contains(flow.Portfolio) && flow.End >= _periodStart && flow.Current_period=="Y" // <= _periodEnd  && flow.PeriodEnd >= _periodStart || flow.PeriodStart >= _periodStart && flow.PeriodEnd <=_periodEnd) 
                           where flow.SaveDate == _date && flow.CCY == _ccy && _ports.Contains(flow.Portfolio) && flow.PeriodEnd > _periodStart && flow.PeriodStart <= _periodEnd
                           orderby flow.End
                           select flow).ToList();
                return qry; //.GroupBy(f => f.TradeID).Select(row=>row.FirstOrDefault()).ToList(); // getting unique flows per period...
            }
        }
    }


    public class AnserBonds : List<AnSerBond>
    {
        public List<AnSerBond> _AnSerBonds(Int32 _modelID)
        {
            var ctx = new ZDEntities();
            ctx.CommandTimeout = 180;
            DateTime? evalDate = (from r in ctx.anserModelIFaces
                                  where r.ID == _modelID
                                  select r.evalDate).FirstOrDefault();

            // there's probably a better way to do this?
            var q1 = (from c in ctx.SecurityFundingStates
                      where c.evalDate == (DateTime)evalDate
                      orderby c.CUSIP
                      select c).ToList();

            var q2 = from f in ctx.anserRMBSFlows
                     where f.iFaceID == _modelID
                     orderby f.CUSIP, f.PmtDate
                     select f;

            // create an ANSER bond object via this query?
            return (from c in q1
                    select new AnSerBond()
                    {
                        cusip = c.CUSIP,
                        mv = (double)c.marketValue,
                        fedElig = (Boolean)c.fedDWPPEligible,
                        flows = (from r in q2 where c.CUSIP == r.CUSIP && r.Balance > 0 select r).ToList()
                    }).ToList();
        }


        public List<mx_rloan_flow> _flowAnSerBonds(List<AnSerBond> _abonds, DateTime _evalDate, double _mvAdj)
        {
            List<mx_rloan_flow> rflows = new List<mx_rloan_flow>();  // note, flows are already sorted by CUSIP,PmtDate, this order is important for our processing 
            //string cusip = "";

            DateTime periodStart = _evalDate;

            foreach (var bond in _abonds)
            {
                if (!bond.fedElig)  // hard coded for now
                {

                    periodStart = _evalDate;
                    foreach (var flow in bond.flows)
                    {
                        mx_rloan_flow rflow = new mx_rloan_flow();
                        rflow.CCY = "USD";
                        rflow.Counterpart = bond.cusip;  // store CUSIP in counterpart field...
                        rflow.CreditIssuer = "RL LIQUID FLAT"; // n/a
                        rflow.Current_period = "N"; // n/a
                        rflow.End = flow.PmtDate; // n/a
                        rflow.Fix_date = _evalDate; // n/a
                        rflow.FixFloat = "Floating";
                        rflow.Fixing = 0;
                        rflow.Flow = flow.IPmt;
                        rflow.Group = "BOND";
                        rflow.Index = "USD LIBOR  3M";
                        rflow.Internal = "N";
                        rflow.Margin = (flow.Balance + flow.PPmt) * bond.mv * (1 + _mvAdj);
                        rflow.Notional = (flow.Balance + flow.PPmt); // current period notional = (end)balance + PPMT !!!  SCALE NOTIONAL AMOUNT BY MV of bond
                        rflow.PayRec = "Receive";
                        rflow.PeriodEnd = flow.PmtDate;
                        rflow.PeriodStart = periodStart;
                        rflow.Portfolio = "CI_ZD_RMBS_ANSER";
                        rflow.Start = _evalDate;
                        rflow.Status = "LIVE";
                        try
                        {
                            rflow.TotalRate = (flow.IPmt / (flow.Balance + flow.PPmt)) * 360 / flow.PmtDate.Value.Subtract(periodStart).Days * 100;
                        }
                        catch (Exception)
                        {
                            rflow.TotalRate = 0;
                        }

                        rflow.TradeDate = _evalDate;
                        rflows.Add(rflow);
                        periodStart = (DateTime)flow.PmtDate;
                    }
                }
            }
            return rflows;
        }
    }
}