using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Script.Services;



/// <summary>
/// Summary description for CreditService
/// </summary>
[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
[ScriptService]
public class CreditService : WebService {

    public CreditService () {
        //Uncomment the following line if using designed components 
        //InitializeComponent(); 
    }

    [WebMethod]
    public  DateTime MaxDate()
    {
        var ctx = new CS.InterfaceEntities();
        return (from m in ctx.mpCDSCurves
                select m.EvalDate).Max();
    }


    [WebMethod]
    public dynamic GetCUList()
    {
        var data = new CS.CreditTradingEntities();

        var qry = from u in data.CreditUniverses
                  orderby u.CreditUniverseName
                  select new
                  {
                      CreditUniverse = u.CreditUniverseName,
                      u.BatchDate,
                      u.Batchable,
                      u.IsDefault
                  };
        return new
        {
            count = qry.Count(),
            rows = qry.ToList()
        };
    }
}
