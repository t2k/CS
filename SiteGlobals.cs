using System;
using System.Web;
using System.Web.Configuration;


/// <summary>
/// static class to cache site globals as defined in web.config
/// </summary>
public static class SiteGlobal
{
    /// <summary>
    /// Full site title tag at root.
    /// </summary>
    static public string PASSWORDEXPIRYDAYS { get; set; }

    /// <summary>
    /// Header prefix on root page.
    /// </summary>
    static public string DOCSTOR { get; set; }

    /// <summary>
    /// Header main part on root page.
    /// </summary>
    static public string KYC_DOCS { get; set; }

    /// <summary>
    /// Main site Url with http://.
    /// </summary>
    static public string PF_DOCS { get; set; }
    static public string DOCSTOR_VPATH { get; set; }
    static public string AFIL { get; set; }
    static public string IPADDRESS { get; set; }
    static public string DATAINTERFACE { get; set; }
    static public string VERSION { get; set; }
    static public string DATAUploads { get; set; }

    static SiteGlobal()
    {
        // Cache all these values in static properties.
        DATAUploads = WebConfigurationManager.AppSettings["DATAUPLOADS"];
        DATAINTERFACE = WebConfigurationManager.AppSettings["DATAINTERFACE"];
        PASSWORDEXPIRYDAYS = WebConfigurationManager.AppSettings["PasswordExpiryInDays"];
        DOCSTOR = WebConfigurationManager.AppSettings["DOCSTOR"];
        KYC_DOCS = WebConfigurationManager.AppSettings["KYC_DOCS"];
        PF_DOCS = WebConfigurationManager.AppSettings["PF_DOCS"];
        AFIL = WebConfigurationManager.AppSettings["AFIL"];
        IPADDRESS = WebConfigurationManager.AppSettings["IPADDRESS"];
        VERSION = WebConfigurationManager.AppSettings["VERSION"];
    }

/*
  <appSettings>
    <add key="PasswordExpiryInDays" value="30" />
    <add key="DOCSTOR" value="d:\data\docstore\" />
    <add key="KYC_DOCS" value="d:\data\dznyintranetdata\kyc\" />
    <add key="PF_DOCS" value="d:\data\dznyintranetdata\pfcompliance\" />
    <add key="DOCSTOR_VPATH" value="\doc\" />
    <add key="AFIL" value="NY" />
    <add key="IPAddress" value="http://dnyias20" />
    <add key="dnyias16.CreditPricingWebService" value="http://dnyias20.dz.dzbank.vrnet:88/wsCredit/CreditPricingWebService.asmx" />
  </appSettings>
*/

}