using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SkillHub.Web.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var token = context.HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }

        base.OnActionExecuting(context);
    }
}
