/// Sign-in options. The AuthProvider setting in app.config picks one; the rest of the site works the same for each.
module Authentication

open System
open System.Configuration
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open System.Threading.Tasks
open Microsoft.IdentityModel.Protocols.OpenIdConnect
open Microsoft.IdentityModel.Tokens
open Microsoft.Owin
open Microsoft.Owin.Security
open Microsoft.Owin.Security.Cookies
open Microsoft.Owin.Security.Notifications
open Microsoft.Owin.Security.OpenIdConnect
open Owin


/// An OpenID Connect provider the site redirects to for sign-in
type ExternalProvider =
    /// Amazon Cognito user pool with a Hosted UI domain; the pool's discovery document names the domain
    | Cognito of userPoolId: string
    /// Auth0 tenant (myapp.eu.auth0.com); the audience is the API identifier, when the site calls an API
    | Auth0 of domain: string * audience: string option
    /// Any other OpenID Connect provider (Entra ID, Keycloak, Okta, Google), by its issuer URL
    | Oidc of authority: string

type ExternalSettings = {
    Provider: ExternalProvider
    ClientId: string
    /// None for a public client, which then relies on PKCE alone
    ClientSecret: string option
}

type AuthMode =
    /// Email and password in the users table
    | Local
    | External of ExternalSettings

let externalScheme = OpenIdConnectAuthenticationDefaults.AuthenticationType

let providerName mode =
    match mode with
    | Local -> "Local"
    | External { Provider = Cognito _ } -> "Cognito"
    | External { Provider = Auth0 _ } -> "Auth0"
    | External { Provider = Oidc _ } -> "Oidc"

/// Reads the sign-in mode. A setting the chosen provider needs but lacks stops the start-up.
let readMode (setting: string -> string) =
    let value key =
        setting key |> Option.ofObj |> Option.map _.Trim() |> Option.filter (String.IsNullOrEmpty >> not)

    let provider = value "AuthProvider" |> Option.defaultValue "Local"

    let required key =
        match value key with
        | Some found -> found
        | None -> failwith $"The app.config setting {key} is required when AuthProvider is {provider}."

    let external chosen =
        External {
            Provider = chosen
            ClientId = required "AuthClientId"
            ClientSecret = value "AuthClientSecret"
        }

    match provider.ToLowerInvariant() with
    | "local" -> Local
    | "cognito" ->
        let userPoolId = required "AuthCognitoUserPoolId"
        if not (userPoolId.Contains "_") then
            failwith $"AuthCognitoUserPoolId {userPoolId} is not a user pool id such as eu-west-2_AbC123."
        external (Cognito userPoolId)
    | "auth0" -> external (Auth0(required "AuthDomain", value "AuthAudience"))
    | "oidc" -> external (Oidc(required "AuthAuthority"))
    | _ -> failwith $"AuthProvider {provider} is not one of Local, Cognito, Auth0 or Oidc."

let authMode = readMode (fun key -> ConfigurationManager.AppSettings[key])

/// The issuer whose discovery document configures the sign-in
let authority provider =
    match provider with
    | Cognito userPoolId ->
        // A pool id starts with its region: eu-west-2_AbC123
        let region = userPoolId.Substring(0, userPoolId.IndexOf '_')
        $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}"
    | Auth0(domain, _) -> $"https://{domain}/"
    | Oidc issuer -> issuer

/// The provider's own sign-out page, for providers whose discovery document has no end_session_endpoint.
/// It ends the provider's browser session; without it the next sign-in completes as the same user.
let signOutUri settings (authorizationEndpoint: string) (returnTo: string) =
    let clientId = Uri.EscapeDataString settings.ClientId
    let back = Uri.EscapeDataString returnTo

    match settings.Provider with
    // Next to the authorize endpoint: a pool with a custom domain sets its session cookie there
    | Cognito _ ->
        let hostedUi = Uri(authorizationEndpoint).GetLeftPart UriPartial.Authority
        Some $"{hostedUi}/logout?client_id={clientId}&logout_uri={back}"
    | Auth0(domain, _) -> Some $"https://{domain}/v2/logout?client_id={clientId}&returnTo={back}"
    | Oidc _ -> None

/// Where the provider returns the browser after sign-out. Register it with the provider as an allowed sign-out URL.
let signedOutPath = "/login.html"

/// Where the provider returns the browser after sign-in. Register it with the provider as an allowed callback URL.
let callbackPath = "/signin-oidc"

/// Plain-http metadata is accepted only from this machine, such as a Keycloak container in development
let requiresHttpsMetadata provider = not (Uri(authority provider).IsLoopback)

/// A path on this site, or the home page: "//host" and "/\host" would leave the site
let localPath (returnUrl: string) =
    match returnUrl with
    | null -> "/index.html"
    | url when url.StartsWith "/" && not (url.StartsWith "//") && not (url.StartsWith "/\\") -> url
    | _ -> "/index.html"

let private siteRoot (request: IOwinRequest) = $"{request.Scheme}://{request.Host.Value}"

/// Adds the OpenID Connect middleware: authorization code flow with PKCE, signing in to the cookie.
let useExternal (app: IAppBuilder) settings =
    let options =
        OpenIdConnectAuthenticationOptions(
            AuthenticationType = externalScheme,
            // Passive: only the sign-in route starts a provider redirect, not every 401 from the Web API
            AuthenticationMode = AuthenticationMode.Passive,
            Authority = authority settings.Provider,
            RequireHttpsMetadata = requiresHttpsMetadata settings.Provider,
            ClientId = settings.ClientId,
            ResponseType = OpenIdConnectResponseType.Code,
            // Cognito returns the code only in the query string
            ResponseMode = OpenIdConnectResponseMode.Query,
            RedeemCode = true,
            UsePkce = true,
            Scope = "openid email profile",
            CallbackPath = PathString callbackPath,
            SignInAsAuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
            // Keep the providers' claim names: sub, email, cognito:groups
            SecurityTokenValidator = JwtSecurityTokenHandler(MapInboundClaims = false),
            TokenValidationParameters = TokenValidationParameters(NameClaimType = "email", RoleClaimType = ClaimTypes.Role))

    settings.ClientSecret |> Option.iter (fun secret -> options.ClientSecret <- secret)

    options.Notifications <-
        OpenIdConnectAuthenticationNotifications(
            RedirectToIdentityProvider =
                Func<RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>, Task>(fun notification ->
                    task {
                        let site = siteRoot notification.Request

                        if notification.ProtocolMessage.RequestType = OpenIdConnectRequestType.Logout then
                            let returnTo = site + signedOutPath
                            notification.ProtocolMessage.PostLogoutRedirectUri <- returnTo
                            notification.ProtocolMessage.ClientId <- settings.ClientId
                            let! configuration = notification.Options.ConfigurationManager.GetConfigurationAsync notification.Request.CallCancelled

                            match signOutUri settings configuration.AuthorizationEndpoint returnTo with
                            | Some uri ->
                                notification.Response.Redirect uri
                                notification.HandleResponse()
                            | None -> ()
                        else
                            notification.ProtocolMessage.RedirectUri <- site + callbackPath

                            match settings.Provider with
                            | Auth0(_, Some audience) -> notification.ProtocolMessage.SetParameter("audience", audience)
                            | Auth0(_, None)
                            | Cognito _
                            | Oidc _ -> ()
                    }
                    :> Task),
            // The token request must repeat the redirect_uri of the sign-in request exactly
            AuthorizationCodeReceived =
                Func<AuthorizationCodeReceivedNotification, Task>(fun notification ->
                    notification.TokenEndpointRequest.RedirectUri <- siteRoot notification.Request + callbackPath
                    Task.CompletedTask),
            SecurityTokenValidated =
                Func<SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>, Task>(fun notification ->
                    // The role a local sign-in gets, plus the user's Cognito groups as roles
                    let identity = notification.AuthenticationTicket.Identity

                    identity.FindAll "cognito:groups"
                    |> Seq.map (fun group -> Claim(ClaimTypes.Role, group.Value))
                    |> Seq.append [ Claim(ClaimTypes.Role, "AuthenticatedUser") ]
                    |> Seq.toList
                    |> List.iter identity.AddClaim

                    Task.CompletedTask))

    app.UseOpenIdConnectAuthentication options |> ignore
