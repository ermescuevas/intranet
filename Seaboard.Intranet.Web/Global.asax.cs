using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Domain;
using System;
using System.Globalization;
using System.Threading;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Seaboard.Intranet.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public MvcApplication()
        {
            this.RegisterWindowsAuthentication();
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            Helpers.InterCompanyId = WebConfigurationManager.AppSettings["INTERID"].ToString();
            Helpers.CompanyId = Convert.ToInt32(WebConfigurationManager.AppSettings["CompanyId"]);
            Helpers.CompanyIdWebServices = Convert.ToInt32(WebConfigurationManager.AppSettings["CompanyIdWebServices"]);
            Helpers.ReportPath = WebConfigurationManager.AppSettings["ReportPath"].ToString();
            Helpers.ConnectionStrings = WebConfigurationManager.ConnectionStrings["SeaboContext"].ConnectionString;
            Helpers.PublicDocumentsPath = WebConfigurationManager.AppSettings["PublicDocumentsPath"].ToString();
            Helpers.MailPort = WebConfigurationManager.AppSettings["MailPort"].ToString();
            Helpers.MailServer = WebConfigurationManager.AppSettings["MailServer"].ToString();
            Helpers.HelpdeskMail = WebConfigurationManager.AppSettings["HelpdeskMail"].ToString();
            Helpers.VendorClass = WebConfigurationManager.AppSettings["VendorClass"].ToString();

            WebTaskScheduler.Lock();
            try { WebTaskScheduler.Add("RunTasks", RunTasks, new TimeSpan(0, 0, 2, 0)); } finally { WebTaskScheduler.Unlock(); }
        }
        private static void RunTasks(WebTaskEventArgs e)
        {
            e.CanContinue = true;
        }
    }
}
