using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Seaboard.Intranet.Web
{
    public class WindowsLoginHandler : HttpTaskAsyncHandler, System.Web.SessionState.IRequiresSessionState
    {
        public HttpContext Context { get; set; }
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            Context = context;

            if (context.Request.IsAuthenticated)
            {
                this.SaveUserIdToSession(context.User.Identity.GetUserId());

                await WinLogoffAsync(context);

                context.RequestChallenge();
            }
            else if (!context.Request.LogonUserIdentity.IsAuthenticated)
                context.RequestChallenge();
            else
            {
                if (this.SessionHasUserId())
                {
                    var userId = this.ReadUserIdFromSession();
                    this.SaveUserIdToContext(userId);
                    await WinLinkLoginAsync(context);
                }
                else
                    await WinLoginAsync(context);
            }
        }

        #region helpers

        private async Task WinLoginAsync(HttpContext context)
        {
            var routeData = this.CreateRouteData(Action.Login);

            routeData.Values.Add("returnUrl", context.Request["returnUrl"]);
            routeData.Values.Add("userName", context.Request.Form["UserName"]);
            await ExecuteController(context, routeData);
        }

        private async Task WinLinkLoginAsync(HttpContext context)
        {
            var routeData = this.CreateRouteData(Action.Link);
            await ExecuteController(context, routeData);
        }

        private async Task WinLogoffAsync(HttpContext context)
        {
            var routeData = this.CreateRouteData(Action.Logoff);

            await ExecuteController(context, routeData);
        }

        private async Task ExecuteController(HttpContext context, RouteData routeData)
        {
            var wrapper = new HttpContextWrapper(context);
            MvcHandler handler = new MvcHandler(new RequestContext(wrapper, routeData));

            IHttpAsyncHandler asyncHandler = ((IHttpAsyncHandler)handler);
            await Task.Factory.FromAsync(asyncHandler.BeginProcessRequest, asyncHandler.EndProcessRequest, context, null);
        }

        #endregion
    }
}