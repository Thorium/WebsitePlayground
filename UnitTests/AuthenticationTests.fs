module AuthenticationTests

open NUnit.Framework
open Authentication

let private settingsOf (pairs: (string * string) list) =
    let values = Map.ofList pairs
    fun key -> values |> Map.tryFind key |> Option.defaultValue null

[<Test>]
let ``no AuthProvider setting means local sign-in`` () =
    Assert.That(readMode (settingsOf []), Is.EqualTo Local)
    Assert.That(readMode (settingsOf [ "AuthProvider", "local" ]), Is.EqualTo Local)

[<Test>]
let ``Cognito needs only the pool and the client`` () =
    let mode =
        readMode (settingsOf [ "AuthProvider", "Cognito"; "AuthCognitoUserPoolId", "eu-west-2_AbC123"; "AuthClientId", "client-1" ])

    Assert.That(mode, Is.EqualTo(External { Provider = Cognito "eu-west-2_AbC123"; ClientId = "client-1"; ClientSecret = None }))
    Assert.That(authority (Cognito "eu-west-2_AbC123"), Is.EqualTo "https://cognito-idp.eu-west-2.amazonaws.com/eu-west-2_AbC123")

[<Test>]
let ``a missing or malformed setting stops the start-up with its name`` () =
    let missing = Assert.Throws<exn>(fun () -> readMode (settingsOf [ "AuthProvider", "Auth0"; "AuthClientId", "client-1" ]) |> ignore)
    Assert.That(missing.Message, Does.Contain "AuthDomain")
    let malformed = Assert.Throws<exn>(fun () -> readMode (settingsOf [ "AuthProvider", "Cognito"; "AuthCognitoUserPoolId", "AbC123"; "AuthClientId", "c" ]) |> ignore)
    Assert.That(malformed.Message, Does.Contain "AuthCognitoUserPoolId")
    let unknown = Assert.Throws<exn>(fun () -> readMode (settingsOf [ "AuthProvider", "Okta" ]) |> ignore)
    Assert.That(unknown.Message, Does.Contain "Local, Cognito, Auth0 or Oidc")

[<Test>]
let ``sign-out ends the provider session on the sign-in host`` () =
    let cognito = { Provider = Cognito "eu-west-2_AbC123"; ClientId = "client-1"; ClientSecret = None }

    Assert.That(
        signOutUri cognito "https://auth.example.com/oauth2/authorize" "https://site.example/login.html",
        Is.EqualTo(Some "https://auth.example.com/logout?client_id=client-1&logout_uri=https%3A%2F%2Fsite.example%2Flogin.html")
    )

    let auth0 = { Provider = Auth0("myapp.eu.auth0.com", Some "https://api.example.com"); ClientId = "client-2"; ClientSecret = Some "secret" }

    Assert.That(
        signOutUri auth0 "https://myapp.eu.auth0.com/authorize" "https://site.example/login.html",
        Is.EqualTo(Some "https://myapp.eu.auth0.com/v2/logout?client_id=client-2&returnTo=https%3A%2F%2Fsite.example%2Flogin.html")
    )

    // A standard provider publishes end_session_endpoint, which the handler uses itself
    let oidc = { Provider = Oidc "https://login.example.com"; ClientId = "client-3"; ClientSecret = None }
    Assert.That(signOutUri oidc "https://login.example.com/authorize" "https://site.example/login.html", Is.EqualTo None)
