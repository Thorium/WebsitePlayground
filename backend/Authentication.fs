/// Sign-in options. The AuthProvider setting in app.config picks one; the rest of the site works the same for each.
module Authentication

open System
open System.Configuration
open System.Security.Claims
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.IdentityModel.Protocols.OpenIdConnect

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

let externalScheme = OpenIdConnectDefaults.AuthenticationScheme

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
        if not (userPoolId.Contains '_') then
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

/// Plain-http metadata is accepted only from this machine, such as a Keycloak container in development
let requiresHttpsMetadata provider = not (Uri(authority provider).IsLoopback)

/// Adds the OpenID Connect handler: authorization code flow with PKCE, signing in to the cookie scheme.
let addExternal (authentication: AuthenticationBuilder) settings =
    authentication.AddOpenIdConnect(externalScheme, fun options ->
        options.Authority <- authority settings.Provider
        options.RequireHttpsMetadata <- requiresHttpsMetadata settings.Provider
        options.ClientId <- settings.ClientId
        settings.ClientSecret |> Option.iter (fun secret -> options.ClientSecret <- secret)
        options.ResponseType <- OpenIdConnectResponseType.Code
        // Cognito returns the code only in the query string; a top-level GET also keeps SameSite=Lax cookies
        options.ResponseMode <- OpenIdConnectResponseMode.Query
        options.UsePkce <- true
        options.SignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
        options.CallbackPath <- PathString "/signin-oidc"
        options.SignedOutCallbackPath <- PathString "/signout-callback-oidc"
        options.SignedOutRedirectUri <- signedOutPath
        options.CorrelationCookie.SameSite <- SameSiteMode.Lax
        options.NonceCookie.SameSite <- SameSiteMode.Lax
#if DEBUG
        options.CorrelationCookie.SecurePolicy <- CookieSecurePolicy.SameAsRequest
        options.NonceCookie.SecurePolicy <- CookieSecurePolicy.SameAsRequest
#endif
        // Keep the providers' claim names: sub, email, cognito:groups
        options.MapInboundClaims <- false
        options.TokenValidationParameters.NameClaimType <- "email"
        options.TokenValidationParameters.RoleClaimType <- ClaimTypes.Role
        options.Scope.Clear()
        [ "openid"; "email"; "profile" ] |> List.iter options.Scope.Add

        options.Events.OnRedirectToIdentityProvider <-
            Func<RedirectContext, Task>(fun context ->
                match settings.Provider with
                | Auth0(_, Some audience) -> context.ProtocolMessage.SetParameter("audience", audience)
                | Auth0(_, None)
                | Cognito _
                | Oidc _ -> ()
                Task.CompletedTask)

        options.Events.OnRedirectToIdentityProviderForSignOut <-
            Func<RedirectContext, Task>(fun context ->
                task {
                    let returnTo = $"{context.Request.Scheme}://{context.Request.Host.Value}{signedOutPath}"
                    let! configuration = context.Options.ConfigurationManager.GetConfigurationAsync context.HttpContext.RequestAborted

                    match signOutUri settings configuration.AuthorizationEndpoint returnTo with
                    | Some uri ->
                        context.Response.Redirect uri
                        context.HandleResponse()
                    | None ->
                        // Straight back to the login page, the URL registered for every provider; client_id
                        // identifies the app to providers such as Keycloak that need it without an id_token_hint
                        context.ProtocolMessage.PostLogoutRedirectUri <- returnTo
                        context.ProtocolMessage.ClientId <- settings.ClientId
                })

        options.Events.OnTokenValidated <-
            Func<TokenValidatedContext, Task>(fun context ->
                // The role a local sign-in gets, plus the user's Cognito groups as roles
                match context.Principal.Identity with
                | :? ClaimsIdentity as identity ->
                    identity.FindAll "cognito:groups"
                    |> Seq.map (fun group -> Claim(ClaimTypes.Role, group.Value))
                    |> Seq.append [ Claim(ClaimTypes.Role, "AuthenticatedUser") ]
                    |> Seq.toList
                    |> List.iter identity.AddClaim
                | _ -> ()

                Task.CompletedTask))
    |> ignore
