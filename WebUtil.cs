using System;
using System.Web.Security;
using System.Net.Mail;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using System.Web;



namespace CS
{
    /// <summary>
    /// Summary description for webutil
    /// global class packed with useful functions
    /// </summary>
    public class WebUtil
    {
        /// <summary>
        /// write page visit data to the weblog
        /// </summary>
        /// <param name="req"></param>
        public static void WebLog(HttpRequest req)
        {
            // use EF4 UserProfileEntities to update the weblog
            
            try
            {
                UserProfileEntities ctx = new UserProfileEntities();
                WebLog log = new WebLog() { TimeStamp = DateTime.Now, UserName = req.RequestContext.HttpContext.User.Identity.Name.ToUpper(), UserHostAddress = req.UserHostAddress, Page = req.AppRelativeCurrentExecutionFilePath, Browser=req.Browser.Id};
                ctx.WebLogs.AddObject(log);
                ctx.SaveChanges();
            }
            catch { }
            finally { }
        }


        /// <summary>
        /// write info to the Interface Log table
        /// </summary>
        /// <param name="logDate"></param>
        /// <param name="ifaceName"></param>
        /// <param name="ifaceCount"></param>
        /// <param name="iFaceNote"></param>
        /// <param name="LogStatus"></param>
        public static void IFaceLog(System.DateTime logDate, string ifaceName, int ifaceCount, string iFaceNote, bool LogStatus)
        {
            // use EF4 InterfaceEntities to update the IfaceLog table
            try
            {
                InterfaceEntities ctx = new InterfaceEntities();
                CS.IFaceLog iLog = new CS.IFaceLog()
                {  // inline constructor
                    IFaceDate = logDate,
                    IFaceName = ifaceName,
                    IFaceCount = ifaceCount,
                    IFaceTimeStamp = DateTime.Now,
                    IFaceNote = iFaceNote.Length > 150 ? iFaceNote.Substring(0, 150) : iFaceNote,   // this is why I like the C# language 
                    IFaceUser = Membership.GetUser().UserName.ToUpper(),
                    IFaceStatus = LogStatus
                };
                ctx.IFaceLogs.AddObject(iLog);
                ctx.SaveChanges();
            }
            catch
            {
            }
        }


        /// <summary>
        /// RLOAN utility
        /// </summary>
        /// <param name="_AFIL"></param>
        /// <returns></returns>
        public static DateTime PreviousRLOANReconByAFIL(string _AFIL)
        {
            try
            {
                var ctx = new InterfaceEntities();
                DateTime maxDate = (from l in ctx.IFaceLogs
                                    where l.IFaceName == "mxRLOAN" && l.IFaceStatus == true && l.IFaceNote.StartsWith("AFIL="+_AFIL)
                                    select l.IFaceDate).Max();


                return (from l in ctx.IFaceLogs
                        where l.IFaceName == "mxRLOAN" && l.IFaceStatus==true && l.IFaceDate < maxDate && l.IFaceNote.StartsWith("AFIL="+_AFIL)
                        select l.IFaceDate).Max();
            }
            catch
            {
                return MinSQLDate();
            }
        }


        /// <summary>
        /// Max Markit CDS Curve Date
        /// </summary>
        /// <returns></returns>
        public static DateTime LatestPricing()
        {
            var ctx = new InterfaceEntities();
            return (from m in ctx.mpCDSCurves
                    select m.EvalDate).Max();

        }





        /// <summary>
        /// return a date from a Loan Neu Packed-Decimal value ie 20081031 is YYYYMMDD  
        /// </summary>
        /// <param name="_lndate"></param>
        /// <returns></returns>
        public static DateTime LoanNeuDate(decimal _lndate)
        {
            try
            {
                if (_lndate.ToString().Length != 8)
                {
                    return DateTime.MinValue;
                }
                else
                {
                    return new DateTime(Int32.Parse(_lndate.ToString().Substring(0, 4)), Int32.Parse(_lndate.ToString().Substring(4, 2)), Int32.Parse(_lndate.ToString().Substring(6, 2)));
                }
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// more RLOAN utility return the last RLOAN interface proccessed date 
        /// query the IFaceLog table for 
        /// </summary>
        /// <param name="_AFIL"></param>
        /// <returns></returns>
        public static DateTime LatestRLOANReconByAFIL(string _AFIL)
        {
            // 
            try
            {
                InterfaceEntities ctx = new InterfaceEntities();
                return (from i in ctx.IFaceLogs
                        where i.IFaceName == "mxRLOAN" && i.IFaceStatus == true && i.IFaceNote.StartsWith("AFIL="+_AFIL)
                        select i.IFaceDate).Max();
            }
            catch (Exception ex)
            {
                return MinSQLDate();
            }
        }



        /// <summary>
        /// utility stuff
        /// </summary>
        /// <returns></returns>
        public static DateTime MinSQLDate()
        {
            return new DateTime(1753, 1, 1);
        }

        /// <summary>
        /// utility stuff
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static string PageAddress(HttpRequest req)
        {
            return string.Format("{0}{1}", System.Configuration.ConfigurationManager.AppSettings["IPAddress"], req.RawUrl);
        }




        /// <summary>
        /// new June 24 2009
        /// Ted Killilea
        /// fix up gridviews to create thead/tbody/tfoot sections for standards based CSS 
        /// also allows various JQUERY plugin to sort, search etc. the gridview outputted table on the client side
        /// </summary>
        /// <param name="gv"></param>
        public static void MakeAccessible(GridView gv)
        {
            if (gv.Rows.Count > 0)
            {
                // This replaces <td> with <th> and adds the scope attribute
                gv.UseAccessibleHeader = true;
                //   //This will add the <thead> and <tbody> elements
                gv.HeaderRow.TableSection = TableRowSection.TableHeader;
                //   //This adds the <tfoot> element. Remove if you don't have a footer row
                if (gv.ShowFooter)
                {
                    gv.FooterRow.TableSection = TableRowSection.TableFooter;
                }
            }
        }



    //    /// <summary>
    //    ///  global function new Dec 09 , DOCSTORE using a centrally assigned DOCID via the DOCUMENTS tableAdapter (SQL) 
    //    ///  a static function...  create a class around this to accomodate more functionality
    //    ///  test the value of the functions return value anything other than 0 means the file is stored and 
    //    ///  info is contained in the database to enable document retrieval
    //    /// </summary>
    //    /// <param name="_fu"></param>
    //    /// <returns></returns>
    //    public static Int32 DocumentStore(FileUpload _fu)
    //    {
    //        var ctx = new ResearchEntities();
    //        Document doc = new Document();
    //        doc.UserName = Membership.GetUser().UserName;
    //        doc.TimeStamp = DateTime.Now;
    //        doc.Title = _fu.FileName;
    //        ctx.Documents.AddObject(doc);
    //        ctx.SaveChanges();

    //        //<configuration>
    //        //<appSettings>
    //        //  <add key="DOCSTOR" value="C:\DATA\DOCSTORE\" />
    //        //  ...
    //        //  ...
    //        //</appSettings>

    //        // this global setting is located in the web.config file in the AppSettings section (see above)
    //        string strDOCSTORE = System.Configuration.ConfigurationManager.AppSettings["DOCSTOR"];

    //        //ds_DOCSTORETableAdapters.DocumentsTableAdapter ta = new ds_DOCSTORETableAdapters.DocumentsTableAdapter();

    //        //Decimal intDocID = 0;
    //        // the .InsertID method is scaler and returns and (decimal) of the new DOCID added to the DOCUMENTS table...
    //        //intDocID = (Decimal)ta.InsertID(Membership.GetUser().UserName, DateTime.Now, _fu.FileName);
    //        try
    //        {
    //            // save the file to the global location...
    //            _fu.SaveAs(string.Format(strDOCSTORE + "{0}_{1}", doc.DocID, _fu.FileName));
    //            return doc.DocID;
    //        }
    //        catch
    //        {
    //            return 0;
    //        }
    //        finally
    //        {
    //            ctx.Dispose();
    //        }
    //    }
}


    public class fMsg
    {
        public int ID { get; set; }
        public string Text { get; set; }
    }

}