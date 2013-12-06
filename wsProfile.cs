using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Security;
using System.Web.Script.Services;






/// <summary>
/// Summary description for wsProfile
/// </summary>
[WebService(Namespace = "zirpen_wsProfile")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
[ScriptService]
public class wsProfile : System.Web.Services.WebService {

    public wsProfile () {
        //InitializeComponent(); 
    }


    /// <summary>
    /// jSend API: return JSON current logged in user object
    /// </summary>
    /// <returns></returns>
    [WebMethod]
	public dynamic getCurrentUser()
	{
		try
		{
			var loginID = HttpContext.Current.User.Identity.Name;
			var user = Membership.GetUser(loginID);
			var profile = DZUserProfile.GetUserProfile(loginID);
			return new
			{
				status = "success",
				data = new
				{
					loginID = loginID,
					fullName = string.Format("{0} {1}",profile.FirstName,profile.LastName),
					department = profile.Department,
					culture = profile.Culture,
					bankingCenter = profile.BankingCenter,
					email = user.Email,
					timeZone = profile.TimeZone,
					roles = Roles.GetRolesForUser(loginID)
				}
			};
		}
		catch (Exception ex)
		{
			return new
			{
				status = "error",
				data = "",
				message = ex.Message
			};
		}
	
	}
	/// <summary>
	/// jSend API: return JSON current logged in user object
	/// </summary>
	/// <returns></returns>
	[WebMethod]
    public dynamic UserName()
    {
        try
        {

            return new
            {
                status = "success",
                data = new
                {
                    userName = HttpContext.Current.User.Identity.Name
                }
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

    /// <summary>
    /// jSend API: return culture
    /// </summary>
    /// <returns></returns>
    [WebMethod]
    public dynamic Culture()
    {
        try
        {

            return new
            {
                status = "success",
                data = new
                {
                    culture = DZUserProfile.GetUserProfile(HttpContext.Current.User.Identity.Name).Culture
                }
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

    
    [WebMethod]
    public dynamic Center()
    {
        try
        {

            return new
            {
                status = "success",
                data = new
                {
                    center= DZUserProfile.GetUserProfile(HttpContext.Current.User.Identity.Name).BankingCenter
                }
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


    [WebMethod]
    public dynamic TimeZone()
    {
        try
        {

            return new
            {
                status = "success",
                data = new
                {
                    timezone = DZUserProfile.GetUserProfile(HttpContext.Current.User.Identity.Name).TimeZone
                }
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
