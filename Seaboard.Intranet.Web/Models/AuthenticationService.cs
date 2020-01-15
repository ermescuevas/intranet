using Microsoft.Owin.Security;
using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Claims;

namespace Seaboard.Intranet.Web
{
    public class AdAuthenticationService
    {
        public class AuthenticationResult
        {
            public AuthenticationResult(string errorMessage = null)
            {
                ErrorMessage = errorMessage;
            }

            public String ErrorMessage { get; private set; }
            public Boolean IsSuccess => String.IsNullOrEmpty(ErrorMessage);
        }

        private readonly IAuthenticationManager _authenticationManager;

        public AdAuthenticationService(IAuthenticationManager authenticationManager)
        {
            this._authenticationManager = authenticationManager;
        }

        public AuthenticationResult SignIn(string username, string password)
        {
            ContextType authenticationType = ContextType.Domain;
            PrincipalContext principalContext = new PrincipalContext(authenticationType);
            bool isAuthenticated = false;
            UserPrincipal userPrincipal = null;
            try
            {
                isAuthenticated = principalContext.ValidateCredentials(username, password, ContextOptions.Negotiate);
                if (isAuthenticated)
                    userPrincipal = UserPrincipal.FindByIdentity(principalContext, username);
            }
            catch (Exception)
            {
                isAuthenticated = false;
                userPrincipal = null;
            }

            if (!isAuthenticated || userPrincipal == null)
                return new AuthenticationResult("Usuario o contraseña son incorrectos");

            if (userPrincipal.IsAccountLockedOut())
                return new AuthenticationResult("Su usuario esta bloqueado");

            if (userPrincipal.Enabled.HasValue && userPrincipal.Enabled.Value == false)
               return new AuthenticationResult("Su usuario esta deshabilitado");

            var identity = CreateIdentity(userPrincipal);
            _authenticationManager.SignOut(ActiveDirectoryAuthentication.ApplicationCookie);
            _authenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = false }, identity);

            return new AuthenticationResult();
        }

        private ClaimsIdentity CreateIdentity(UserPrincipal userPrincipal)
        {
            var identity = new ClaimsIdentity(ActiveDirectoryAuthentication.ApplicationCookie, ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            identity.AddClaim(new Claim("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider", "Active Directory"));
            identity.AddClaim(new Claim(ClaimTypes.Name, userPrincipal.SamAccountName));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userPrincipal.SamAccountName));
            if (!string.IsNullOrEmpty(userPrincipal.EmailAddress))
                identity.AddClaim(new Claim(ClaimTypes.Email, userPrincipal.EmailAddress));
            return identity;
        }
    }
}