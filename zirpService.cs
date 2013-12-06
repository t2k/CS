using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.Profile;
using System.Web.Services;
using System.Web.Script.Services;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Converters;
using System.Data.Objects;
using api;
using CS;




/// <summary>
/// Summary description for zerpService
/// </summary>
[WebService(Namespace = "http://dnyias20/zirpService")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
[ScriptService]
public class zirpService : WebService {
    private zirpenEntities ctx;

    public zirpService () {
        this.ctx = new zirpenEntities();
    }


    /// <summary>
    ///  jSend API
    /// </summary>
    /// <returns></returns>
    [WebMethod]
    public dynamic following(zRequest request)
    {
        List<string> following = (from f in ctx.zirpenFollows
                                  where f.userID.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase)
                                  select f.following).ToList();
        //following.Add(HttpContext.Current.User.Identity.Name);  // add requesting user

        try
        {
            var qry = (from p in ctx.zirpenProfiles
                       where following.Contains(p.userID) && p.auth == true
                       orderby p.last, p.userID
                       select new
                       {
                           p.ID,
                           uid = p.userID,
                           fn = p.first + " " + p.last,
                           dpt=p.dept,
                           ctr=p.center,
                           p.bio,
                           img = p.imgPic,
                           follow=1  // ! used by template
                       }).Skip(request.pg * request.sz).Take(request.sz).ToList();
            return new
            {
                status = "success",
                data = new
                {
                    users = qry
                }
            };
        }
        catch (Exception e)
        {
            return new
            {
                status = "error",
                data = new { },
                message = e.Message
            };
        }
        finally
        {
            ctx.Dispose();
        }
    }


    /// <summary>
    ///  jSend API  data.users
    /// </summary>
    /// <returns></returns>
    [WebMethod]
    public dynamic followers(zRequest request)
    {
        //var ctx = new zirpenEntities();
        var followers = (from f in ctx.zirpenFollows
                                  where f.following.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase)
                                  orderby f.userID
                                  select f.userID);

        try
        {
            var qry = (from p in ctx.zirpenProfiles
                       where followers.Contains(p.userID) && p.auth == true
                       orderby p.last, p.userID
                       select new
                       {
                           p.ID,
                           uid = p.userID,
                           fn = p.first + " " + p.last,
                           dpt = p.dept,
                           ctr = p.center,
                           p.bio,
                           img = p.imgPic,
                           follow = (from f in ctx.zirpenFollows where f.userID.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase) && f.following.Equals(p.userID, StringComparison.InvariantCultureIgnoreCase) select f).Count()
                       }).Skip(request.pg * request.sz).Take(request.sz);

            return new
            {
                status = "success",
                data = new
                {
                    users = qry.ToList()
                }
            };
        }
        catch (Exception e)
        {
            return new
            {
                status = "error",
                data = new { },
                message = e.Message
            };
        }
        finally
        {
            ctx.Dispose();
        }
    }



    /// <summary>
    ///  jSend API
    /// </summary>
    /// <returns></returns>
    [WebMethod]
    public dynamic whoToFollow(zRequest request)
    {
        // var ctx = new zirpenEntities();
        List<string> following = (from f in ctx.zirpenFollows
                                  where f.userID.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase)
                                  select f.following).ToList();
        following.Add(HttpContext.Current.User.Identity.Name);  // add requesting user

        try
        {
            var qry = (from p in ctx.zirpenProfiles
                       where p.auth==true && !following.Contains(p.userID)
                       orderby p.zirpenCount descending
                       select new
                       {
                           p.ID,
                           uid=p.userID,
                           fn = p.first + " " + p.last,
                           dpt=p.dept,
                           ctr=p.center,
                           p.bio,
                           img=p.imgPic

                       }).Skip(request.pg * request.sz).Take(request.sz).ToList();
            return new
            {
                status = "success",
                data = new
                {
                    users = qry
                }
            };
        }
        catch (Exception e)
        {
            return new
            {
                status = "error",
                data = new { },
                message = e.Message
            };
        }
        finally
        {
            ctx.Dispose();
        }
    }

    /// <summary>
    /// jSend  search who to follow?
    /// </summary>
    /// <param name="request">request.pg, request.sz , request.ns</param>
    /// <returns>jSend</returns>
    [WebMethod]
    public dynamic searchWhoToFollow(zirpNameSearchRequest request)
    {
        //var ctx = new zirpenEntities();
        object qry;
        try
        {
            if (request.ns == null)  // if no name search pattern is passed in just return first sz users orderd by last name
            {

                qry = (from p in ctx.zirpenProfiles
                           where p.auth==true 
                           orderby p.last,p.userID
                           select new
                           {
                               p.ID,
                               uid=p.userID,
                               fn = p.first + " " + p.last,
                               ctr = p.center,
                               dpt = p.dept,
                               p.bio,
                               img=p.imgPic,
                               follow = (from f in ctx.zirpenFollows where f.userID.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase) && f.following.Equals(p.userID,StringComparison.InvariantCultureIgnoreCase) select f).Count()
                           }).Skip(request.pg * request.sz).Take(request.sz).ToList();

            }
            else
            {
                qry = (from p in ctx.zirpenProfiles
                           where p.first.Contains(request.ns) || p.last.Contains(request.ns) || p.userID.Contains(request.ns) && p.auth==true
                           orderby p.last, p.userID
                           select new
                           {
                               p.ID,
                               uid=p.userID,
                               fn = p.first + " " + p.last,
                               ctr = p.center,
                               dpt = p.dept,
                               p.bio,
                               img=p.imgPic,
                               follow = (from f in ctx.zirpenFollows where f.userID.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase) && f.following.Equals(p.userID, StringComparison.InvariantCultureIgnoreCase) select f).Count()
                           }).Take(request.sz).ToList();  // note... no paging here...
            }

            return new
            {
                status = "success",
                data = new
                {
                    users = qry
                }
            };
        }
        catch (Exception e)
        {
            return new
            {
                status = "error",
                data = new { },
                message = e.Message
            };
        }
        finally
        {
            ctx.Dispose();
        }
    }


    /// <summary>
    /// return the requesting users tweets
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [WebMethod]
    public dynamic myZirpens(zirpRequest request)
    {
        //var ctx = new zirpenEntities();
        
        try
        {
            var myZirps = (from d in ctx.zirpens
                           where d.userID.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase) && d.live==true
                           orderby d.timestamp descending
                           select d).Skip(request.pg * request.sz).Take(request.sz);

            // same as above
            var result = (from z in myZirps
                          select new
                          {
                              id = z.Id,
                              ts = z.timestamp,
                              uid = z.userID,
                              z.zirp,
                              pro = (from p in ctx.zirpenProfiles
                                     where p.userID.Equals(z.userID, StringComparison.InvariantCultureIgnoreCase)
                                     select new
                                     {
                                         fn = p.first + " " + p.last,
                                         img = p.imgPic
                                     }).FirstOrDefault()
                          }).ToList();

            int iCount = result.Count;
            return new
            {
                status = "success",
                data = new
                {
                    count = iCount,
                    ts = iCount > 0 ? result[0].ts : DateTime.UtcNow, // timestamp from most recent tweet
                    tsmin = iCount > 0 ? result[iCount - 1].ts : DateTime.UtcNow, // timestamp from most recent tweet
                    zirpens = result
                }
            };
        }
        catch (Exception e)
        {
            return new
            {
                status = "error",
                data = new { },
                message = e.Message
            };
        }
        finally
        {
            ctx.Dispose();
        }
    }

    /// <summary>
    /// return the REQUESTED users tweets
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [WebMethod]
    public dynamic userZirpens(zirpRequest request)
    {
        //var ctx = new zirpenEntities();

        try
        {
            var myZirps = (from d in ctx.zirpens
                           where d.userID.Equals(request.uid, StringComparison.InvariantCultureIgnoreCase) && d.live == true
                           orderby d.timestamp descending
                           select d).Skip(request.pg * request.sz).Take(request.sz);

            // same as above
            var result = (from z in myZirps
                          select new
                          {
                              id = z.Id,
                              ts = z.timestamp,
                              uid = z.userID,
                              z.zirp,
                              pro = (from p in ctx.zirpenProfiles
                                     where p.userID.Equals(z.userID, StringComparison.InvariantCultureIgnoreCase)
                                     select new
                                     {
                                         fn = p.first + " " + p.last,
                                         img = p.imgPic
                                     }).FirstOrDefault()
                          }).ToList();

            int iCount = result.Count;
            return new
            {
                status = "success",
                data = new
                {
                    count = iCount,
                    ts = iCount > 0 ? result[0].ts : DateTime.UtcNow, // timestamp from most recent tweet
                    tsmin = iCount > 0 ? result[iCount - 1].ts : DateTime.UtcNow, // timestamp from most recent tweet
                    zirpens = result
                }
            };
        }
        catch (Exception e)
        {
            return new
            {
                status = "error",
                data = new { },
                message = e.Message
            };
        }
        finally
        {
            ctx.Dispose();
        }
    }

    
    /// <summary>
    ///  return all tweets for the requesting user and his/her folloers:
    /// </summary>
    /// <returns>JSON/JSend package</returns>
    [WebMethod]
    public dynamic getZirpen(zirpRequest request)
    {
        //var ctx = new zirpenEntities();
        try
        {

            // add 1 millisecond to passed in request timestamp otherwise we seem to be getting the last item, seems to be a very slight precision error between SQL/JavaScript date types.. no biggie...
            request.ts = request.ts.AddMilliseconds(100); 

            // make a list of the users the requestor is following
            List<string> following = (from f in ctx.zirpenFollows
                                      where f.userID.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase)
                                      select f.following).ToList();

            following.Add(HttpContext.Current.User.Identity.Name); // add the callers name
            // tweets from the folks you follow and yourself


            var follows = (from d in ctx.zirpens
                       where d.timestamp > request.ts && d.live == true && following.Contains(d.userID)
                       orderby d.timestamp descending
                       select d).Take(request.sz).ToList();
            
            // explicitly find tweets where our followers have been mentiond
            var mentions = (from m in ctx.zirpenMentions
                        where m.zirpen.timestamp > request.ts && following.Contains(m.UserID)
                        orderby m.zirpen.timestamp descending
                        select m.zirpen).Take(request.sz).ToList();
            
            follows.AddRange(mentions);

            // strip out duplicate ID's from follows and mentions...
            var qry = follows.GroupBy(x=> x.Id).Select(y => y.First());


            // same as above
            var result = (from d in qry
                            orderby d.timestamp descending
                            select new
                            {                           
                                id = d.Id,
                                ts = d.timestamp,
                                uid = d.userID,
                                d.zirp,
                                pro = (from p in ctx.zirpenProfiles
                                  where p.userID.Equals(d.userID, StringComparison.InvariantCultureIgnoreCase)
                                  select new
                                  {
                                      fn = p.first + " " + p.last,
                                      img = p.imgPic
                                  }).FirstOrDefault()
                            }).ToList();

            int iCount = result.Count;
            return new
            {
                status = "success",
                data= new
                {
                    count = iCount,
                    ts = iCount > 0 ? result[0].ts : DateTime.UtcNow, // timestamp from most recent tweet
                    tsmin = iCount > 0 ? result[iCount-1].ts : DateTime.UtcNow, // timestamp from most recent tweet
                    zirpens = result
                }
            };
        }
        catch (Exception e)
        {
            return new
            {
                status = "error",
                data = new {},
                message = e.Message
            };
        }
        finally
        {
            ctx.Dispose();
        }
    }

    /// <summary>
    /// request.ts is important here!  must be passed in from the client looking for older (MORE) items
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [WebMethod]
    public dynamic getMoreZirpen(zirpRequest request)
    {
        //var ctx = new zirpenEntities();
        try
        {

            // subtract 10 milliseconds to passed in request timestamp otherwise we seem to be getting the last item, seems to be a very slight precision error between SQL/JavaScript date types.. no biggie...
            request.ts = request.ts.AddMilliseconds(-10);

            // make a list of the users the requestor is following
            List<string> following = (from f in ctx.zirpenFollows
                                      where f.userID.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase)
                                      select f.following).ToList();

            following.Add(HttpContext.Current.User.Identity.Name); // add the callers name
            // tweets from the folks you follow and yourself


            var follows = (from d in ctx.zirpens
                           where d.timestamp < request.ts && following.Contains(d.userID) && d.live == true   // NOTE: here we want < for older items !IMPORTANT
                           orderby d.timestamp descending
                           select d).Take(request.sz).ToList();

            // explicitly find tweets where requestors followers have been mentioned...
            var mentions = (from m in ctx.zirpenMentions
                            where m.zirpen.timestamp < request.ts && following.Contains(m.UserID)  // NOTE: here we want < for older items  !IMPORTANT
                            orderby m.zirpen.timestamp descending
                            select m.zirpen).Take(request.sz).ToList();

            // merge followed tweets with mentioned tweets...
            follows.AddRange(mentions);

            // strip out duplicate ID's from follows and mentions...
            var qry = follows.GroupBy(x => x.Id).Select(y => y.First());


            var result = (from d in qry
                          orderby d.timestamp descending
                          select new
                          {
                              id = d.Id,
                              ts = d.timestamp,
                              uid = d.userID,
                              d.zirp,
                              pro = (from p in ctx.zirpenProfiles
                                     where p.userID.Equals(d.userID, StringComparison.InvariantCultureIgnoreCase)
                                     select new
                                     {
                                         fn = p.first + " " + p.last,
                                         img = p.imgPic
                                     }).FirstOrDefault()
                          }).ToList();

            
            int iCount = result.Count; // cache this...
            return new
            {
                status = "success",
                data = new
                {
                    count=iCount,
                    ts = result.Count > 0 ? result[0].ts : DateTime.UtcNow, // timestamp from most recent tweet or now if none...
                    tsmin = iCount > 0 ? result[iCount - 1].ts : DateTime.UtcNow, // timestamp from oldest tweet of this batch
                    zirpens = result
                }
            };
        }
        catch (Exception e)
        {
            return new
            {
                status = "error",
                data = new { },
                message = e.Message
            };
        }
        finally
        {
            ctx.Dispose();
        }
    }




    /// <summary>
    /// add a zirpen to the datastore
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [WebMethod]
    public dynamic postZirpen(zirpPostRequest request)
    {
        //var ctx = new zirpenEntities();
        try
        {
            // linq 2 entity  a zirpen has associated mentions and topics sort of a parent/child relation
            zirpen z = new zirpen();
            z.userID = HttpContext.Current.User.Identity.Name;  // users xn
            z.timestamp = new DateTimeOffset(DateTime.UtcNow);  // store UTC time into a datetimeoffset
            z.zirp = request.zirpen;  // passed in
            z.live = true;

            // parse all words
            var words = from word in z.zirp.Split()
                        select word;

            // grab hashtags
            var hashtags = from word in words
                        where word.StartsWith("#")
                        select word;

            // add associated hashtags
            foreach (string word in hashtags)
            {
                z.zirpenTopics.Add(new zirpenTopic()  {topic=word});
            }

            //grab mentions (
            var mentions = from word in words
                           where word.StartsWith("@")
                           select word;

            // add associated mentions
            foreach (string word in mentions)
            {
                z.zirpenMentions.Add(new zirpenMention() { UserID = word.Replace("@","") });
            }
            ctx.zirpens.AddObject(z);

            // incr users zirpen/tweet count
            var zd = ctx.zirpenProfiles.Where(p => p.userID.Equals(z.userID, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault().zirpenCount++;

            //theUser.zirpenCount++;
            ctx.SaveChanges();

            // return 
            List<zirpen> zlist = new List<zirpen>();
            zlist.Add(z);
            var result = (from i in zlist
                          select new
                          {
                              id = i.Id,
                              ts = i.timestamp,
                              uid = i.userID,
                              i.zirp,
                       
                              pro = (from p in ctx.zirpenProfiles
                                     where p.userID.Equals(i.userID, StringComparison.InvariantCultureIgnoreCase)
                                     select new
                                     {
                                         fn = p.first + " " + p.last,
                                         img = p.imgPic
                                     }).FirstOrDefault()
                          }).ToList();
            return new
            {
                status = "success",
                data = new
                {
                    ts = result.Count > 0 ? result[0].ts : DateTime.UtcNow, // timestamp from last tweet or now if none...
                    zc = ++zd,  // new zd
                    zirpens = result
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
        finally
        {
            ctx.Dispose();
        }
    }

    /// <summary>
    ///  follow/unfollow a user by userID
    /// </summary>
    /// <param name="request">zirpUserFollowRequest userid and follow a t/f </param>
    /// <returns>jSend.data.fc//  updated followcount</returns>
    [WebMethod]
    public dynamic follow(zirpUserFollowRequest request)
    {
        
        //var ctx = new zirpenEntities();
        Int64? fcount;
        try
        {
            
            zirpenFollow newf = new zirpenFollow() { userID = HttpContext.Current.User.Identity.Name, following = request.userid };


            if (request.follow)
            {
                ctx.zirpenFollows.AddObject(newf);
                // incr counter in the datasource
                fcount = ctx.zirpenProfiles.Where(p => p.userID.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault().followingCount++;

            }
            else
            {
                //unfollow  remove the row from zirpenfollows and decrease the counter in the user profile...
                zirpenFollow existf = ctx.zirpenFollows.Where(x => x.userID.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase) && x.following.Equals(request.userid, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault();
                ctx.zirpenFollows.DeleteObject(existf);
                fcount = ctx.zirpenProfiles.Where(p => p.userID.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault().followingCount--;
                // incr counter in the datasource
                
            }
            // add the follow and update the followcount
            ctx.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);

            return new
            {
                status = "success",
                data = new
                {
                    fc = request.follow==true ? ++fcount: --fcount  // need to update again...
                }
            };
        }
        catch (Exception e)
        {
            return new
            {
                status = "error",
                message = e.InnerException
            };
        }
        finally
        {
            ctx.Dispose();
        }
    }




    /// <summary>
    /// admin: sync .dotNetProfiles w/ zirpenProfiles
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [WebMethod]
    public dynamic syncZirpenProfiles()
    {
        //var ctx = new zirpenEntities();
    
        try
        {
            int i=0;
            foreach (MembershipUser u in Membership.GetAllUsers())
            {
                int pcount = (from p in ctx.zirpenProfiles where p.userID.Equals(u.UserName, StringComparison.InvariantCultureIgnoreCase) select p).Count();
                if (pcount == 0)  // we need to add dotnet profile to zirpen profile...
                {
                    zirpenProfile p = new zirpenProfile();
                    p.userID = u.UserName;
                    p.auth = true;
                    p.datecreated = DateTime.UtcNow;
                    p.center = DZUserProfile.GetUserProfile(u.UserName).BankingCenter;
                    p.dept = DZUserProfile.GetUserProfile(u.UserName).Department;
                    p.email = u.Email;
                    p.culture = DZUserProfile.GetUserProfile(u.UserName).Culture;
                    p.first = DZUserProfile.GetUserProfile(u.UserName).FirstName;
                    p.last = DZUserProfile.GetUserProfile(u.UserName).LastName;
                    p.timezone = DZUserProfile.GetUserProfile(u.UserName).TimeZone;
                    p.imgPic = DZUserProfile.GetUserProfile(u.UserName).Picture;
                    ctx.zirpenProfiles.AddObject(p);
                    i++;
                }
                else
                {
                    ctx.zirpenProfiles.Where(p => p.userID.Equals(u.UserName, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault().auth = u.IsApproved;
                    
                }
            }

            var updQ = from p in ctx.zirpenProfiles
                       where p.zirpenCount == null || p.followersCount == null || p.bio==null
                       orderby p.followingCount == null
                       select p;
            foreach (zirpenProfile p in updQ)
            {
                p.zirpenCount = p.zirpenCount == null ? 0 : p.zirpenCount;
                p.followingCount = p.followingCount == null ? 0 : p.followingCount;
                p.followersCount = p.followersCount == null ? 0 : p.followersCount;
                p.bio = "please provide a little info about yourself so users in other departments or countries might learn something about you and perhaps @connect";
            }

            // save to our datastore...
            ctx.SaveChanges();
            return new
            {
                status = "success",
                data = new
                {
                    ts = DateTime.UtcNow,
                    added = i
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
        finally
        {
            ctx.Dispose();
        }
    }


    /// <summary>
    /// userProfile  jSend package containing userprofile of requesting/authenticated user
    /// </summary>
    /// <returns>jSend API</returns>
    [WebMethod]
    public dynamic zirpenUserProfile()
    {
        //var ctx = new zirpenEntities();
        zirpenProfile usr;

        try
        {
            var qry = from p in ctx.zirpenProfiles where p.userID.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase) select p;
            if (!qry.Any())
            {
                // cpy the user profile from .net
                MembershipUser u = Membership.GetUser(HttpContext.Current.User.Identity.Name);
                usr = new zirpenProfile();
                usr.userID = u.UserName;
                usr.auth = true;
                usr.datecreated = DateTime.UtcNow;
                usr.center = DZUserProfile.GetUserProfile(u.UserName).BankingCenter;
                usr.dept = DZUserProfile.GetUserProfile(u.UserName).Department;
                usr.email = u.Email;
                usr.culture = DZUserProfile.GetUserProfile(u.UserName).Culture;
                usr.first = DZUserProfile.GetUserProfile(u.UserName).FirstName;
                usr.last = DZUserProfile.GetUserProfile(u.UserName).LastName;
                usr.timezone = DZUserProfile.GetUserProfile(u.UserName).TimeZone;
                usr.imgPic = DZUserProfile.GetUserProfile(u.UserName).Picture;
                ctx.zirpenProfiles.AddObject(usr);
                ctx.SaveChanges();
            }
            else
            {
                usr = qry.First();
            }
            usr.imgPic = usr.imgPic.Replace("~", "");
            return new
            {
                status = "success",
                data = new
                {
                    id = usr.ID,
                    uid = usr.userID,
                    fn = usr.first + " " + usr.last,
                    img = usr.imgPic,
                    bio = usr.bio,
                    zc = (from u in ctx.zirpens where u.userID.Equals(usr.userID,StringComparison.InvariantCultureIgnoreCase) && u.live==true select u.Id).Count(),
                    fc = (from u in ctx.zirpenFollows where u.userID.Equals(usr.userID,StringComparison.InvariantCultureIgnoreCase) select u.following).Count(),
                    f2 = (from u in ctx.zirpenFollows where u.following.Equals(usr.userID, StringComparison.InvariantCultureIgnoreCase) select u.userID).Count()
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
        finally
        {
            ctx.Dispose();
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [WebMethod]
    public dynamic getMiniProfile(zUserRequest request)
    {
        //var ctx = new zirpenEntities();
        try
        {

            var qry = (from p in ctx.zirpenProfiles
                       where p.userID.Equals(request.userid, StringComparison.InvariantCultureIgnoreCase) && p.auth == true
                       select new
                       {
                           p.ID,
                           uid = p.userID,
                           fn = p.first + " " + p.last,
                           ctr = p.center,
                           dpt = p.dept,
                           p.bio,
                           img = p.imgPic,
                           isMe = request.userid.Equals(HttpContext.Current.User.Identity.Name,StringComparison.InvariantCultureIgnoreCase),
                           follow = (from f in ctx.zirpenFollows where f.userID.Equals(HttpContext.Current.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase) && f.following.Equals(p.userID, StringComparison.InvariantCultureIgnoreCase) select f).Count(),
                           zc = (from u in ctx.zirpens where u.userID.Equals(request.userid, StringComparison.InvariantCultureIgnoreCase) && u.live == true select u.Id).Count(),
                           fc = (from u in ctx.zirpenFollows where u.userID.Equals(request.userid, StringComparison.InvariantCultureIgnoreCase) select u.following).Count(),
                           f2 = (from u in ctx.zirpenFollows where u.following.Equals(request.userid, StringComparison.InvariantCultureIgnoreCase) select u.userID).Count()
                       }).SingleOrDefault();


            return new
            {
                status = "success",
                data = new
                {
                    user = qry
                }
            };
        }
        catch (Exception e)
        {
            return new
            {
                status = "error",
                data = new { },
                message = e.Message
            };
        }
        finally
        {
            ctx.Dispose();
        }
    }



}


namespace CS
{
    // base request with page# and page size
    public class zRequest{
        /// <summary>
        /// page number
        /// </summary>
        public Int32 pg { get; set; }  // PaGe#
        /// <summary>
        /// page size/ per page
        /// </summary>
        public Int32 sz { get; set; } // page SiZe
    }
    /// <summary>
    /// request a userid
    /// </summary>
    public class zUserRequest
    {
        /// <summary>
        /// user id  (DZ xn#####)
        /// </summary>
        public string userid { get; set; }
    }

    // zirp request with timestamp and paging
    public class zirpRequest : zRequest
    {
        /// <summary>
        /// timestamp
        /// </summary>
        public DateTime ts { get; set; }
        public string uid { get; set; }
    }


    // name search request with timestamp and paging
    public class zirpNameSearchRequest : zRequest
    {
        /// <summary>
        /// name search
        /// </summary>
        public String ns { get; set; }   // name search  
    }

    /// <summary>
    /// post new zirpen
    /// </summary>
    public class zirpPostRequest
    {
        public String zirpen { get; set; }
    }

    public class zirpUserFollowRequest 
    {
        /// <summary>
        /// user id
        /// </summary>
        public String userid { get; set; }
        /// <summary>
        /// boolean follow
        /// </summary>
        public Boolean follow { get; set; }
    }
}
