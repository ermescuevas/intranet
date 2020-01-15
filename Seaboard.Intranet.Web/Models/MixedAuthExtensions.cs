using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;

namespace Seaboard.Intranet.Web
{
    public enum Action { Login, Link, Logoff };

    public static class MixedAuthExtensions
    {
        const string UserIdKey = "windows.userId";
        const int FakeStatusCode = 418;

        const string ControllerName = "Account";
        const string LoginActionName = "WindowsLogin";
        const string LinkActionName = "LinkWindowsLogin";
        const string LogoffActionName = "WindowsLogoff";
        const string WindowsLoginRouteName = "Windows/Login";

        public static void RegisterWindowsAuthentication(this MvcApplication app)
        {
            app.EndRequest += (object sender, EventArgs e) =>{HttpContext.Current.ApplyChallenge();};
        }

        public static void IgnoreWindowsLoginRoute(this RouteCollection routes)
        {
            routes.IgnoreRoute(WindowsLoginRouteName);
        }

        public static void RequestChallenge(this HttpContext context)
        {
            context.Response.StatusCode = FakeStatusCode;
        }

        public static void ApplyChallenge(this HttpContext context)
        {
            if (context.Response.StatusCode == FakeStatusCode)
            {
                context.Response.StatusCode = 401;
                context.Response.SubStatusCode = 2;
            }
        }

        public static RouteData CreateRouteData(this WindowsLoginHandler handler, Action action)
        {
            RouteData routeData = new RouteData { RouteHandler = new MvcRouteHandler() };

            switch (action)
            {
                case Action.Login:
                    routeData.Values.Add("controller", ControllerName);
                    routeData.Values.Add("action", LoginActionName);
                    break;
                case Action.Link:
                    routeData.Values.Add("controller", ControllerName);
                    routeData.Values.Add("action", LinkActionName);
                    break;
                case Action.Logoff:
                    routeData.Values.Add("controller", ControllerName);
                    routeData.Values.Add("action", LogoffActionName);
                    break;
                default:
                    throw new NotSupportedException(string.Format("unknonw action value '{0}'.", action));
            }
            return routeData;
        }

        public static void SaveUserIdToContext(this WindowsLoginHandler handler, string userId)
        {
            if (handler.Context.Items.Contains(UserIdKey))
                throw new ApplicationException("Id already exists in context.");
            handler.Context.Items.Add("windows.userId", userId);
        }

        public static string ReadUserId(this HttpContextBase context)
        {
            if (!context.Items.Contains(UserIdKey))
                throw new ApplicationException("Id not found in context.");

            string userId = context.Items[UserIdKey] as string;
            context.Items.Remove(UserIdKey);
            return userId;
        }

        public static bool SessionHasUserId(this WindowsLoginHandler handler)
        {
            return handler.Context.Session[UserIdKey] != null;
        }

        public static void SaveUserIdToSession(this WindowsLoginHandler handler, string userId)
        {
            if (handler.SessionHasUserId())
                throw new ApplicationException("Id already exists in session.");
            handler.Context.Session[UserIdKey] = userId;
        }

        public static string ReadUserIdFromSession(this WindowsLoginHandler handler)
        {
            string userId = handler.Context.Session[UserIdKey] as string;

            if (string.IsNullOrEmpty(UserIdKey))
                throw new ApplicationException("Id not found in session.");

            handler.Context.Session.Remove(UserIdKey);
            return userId;
        }

        public static MvcForm BeginWindowsAuthForm(this HtmlHelper htmlHelper, object htmlAttributes)
        {
            return htmlHelper.BeginForm("Login", "Windows", FormMethod.Post, htmlAttributes);
        }

        public static MvcForm BeginWindowsAuthForm(this HtmlHelper htmlHelper, object routeValues, object htmlAttributes)
        {
            return htmlHelper.BeginForm("Login", "Windows", FormMethod.Post, htmlAttributes);
        }
    }
}