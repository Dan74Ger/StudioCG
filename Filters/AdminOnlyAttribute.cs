using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace StudioCG.Web.Filters
{
    /// <summary>
    /// Attributo che permette l'accesso SOLO all'utente "admin" principale.
    /// Gli altri amministratori non possono accedere.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminOnlyAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Verifica se l'utente è autenticato
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Verifica se l'utente è esattamente "admin"
            var username = user.Identity.Name;
            if (!string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }
        }
    }
}

