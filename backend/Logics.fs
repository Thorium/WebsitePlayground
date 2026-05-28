// --- Some logics, because it's not good to add everyting to the SignalRHub, just as if you want to share it with e.g. a WebApi ---
// If you need independent functions, this place is good to seek: https://fssnip.net/tags/
module Logics

    open System
    open System.Linq
    open FSharp.Data
    open FSharp.Data.Sql

    /// Example of database query operation
    let executeSearch (dbContext:ReadDataContext) (searchparams:SearchObject) =
        let ceoFilter =
            match searchparams.CEOName with
            | None -> ""
            | Some(ceo) -> ceo

        task {
            let! companies =
                query {
                    for c in dbContext.Companyweb.Company do
                    where (
                        c.Founded < searchparams.FoundedBefore
                        && c.Founded > searchparams.FoundedAfter
                        && c.Name.Contains (searchparams.CompanyName.ToUpper())
                        && c.Ceo.Contains ceoFilter
                    )
                    select {
                        CompanyName = c.Name;
                        Url = c.WebSite;
                        Image = c.LogoUrl
                    }
                } |> Array.executeQueryAsync

            return companies
        }

    let executeCrud (dbContext:WriteDataContext) itemId actionToEntity =
        task {
            let! fetched =
                query {
                    for u in dbContext.Companyweb.Company do
                    where (u.Id = itemId)
                } |> Seq.tryHeadAsync
            match fetched with
            | Some entity ->
                entity |> actionToEntity

                // You could also modify entities like this:
                //entity.Founded <- DateTime(1992, 2, 2)

                do! dbContext.SubmitUpdates2 ()

                return entity.ColumnValues
            | None -> return Seq.empty
        }

    // Expample of JSON parsing:
    // Some list of data samples from whatever API docs:

    open FSharp.Data.JsonProvider

    type SampleJson = FSharp.Data.JsonProvider<"""[
        { "name" : "Tuomas", "age" : 30 },
        { "name" : "Seppo" },
        { "error" : "no name "}
    ]""", SampleIsList=true>

    let getData() =

        let someApiResponse = """{ "name" : "Jaakko" }"""
        let item = SampleJson.Load (Serializer.Deserialize someApiResponse)
        let x = item.Name // Strongly typed

        let item2 = SampleJson.Root(Some "Pekka", Some 20, None)

        [| item; item2 |]

        // ...and ToString() via: FSharp.Data.JsonProvider.Serializer.Serialize item.JsonValue

    // --- Authentication logic -----------------------------

    let private maxFailedAttempts = 5
    let private lockoutMinutes = 15

    let ``register user`` (dbContext:WriteDataContext) (request:RegisterRequest) =
        task {
            let normalizedEmail = request.Email |> ``normalize email``
            match ``validate password strength`` request.Password with
            | Some reason -> return WeakPassword reason
            | None ->
                let! existingEmail =
                    query {
                        for u in dbContext.Companyweb.Users do
                        where (u.Email = normalizedEmail)
                    } |> Seq.tryHeadAsync

                match existingEmail with
                | Some _ -> return EmailExists
                | None ->
                    let passwordHash = ``hash password`` request.Password
                    let newUser = dbContext.Companyweb.Users.Create()
                    newUser.Email <- normalizedEmail
                    newUser.PasswordHash <- passwordHash
                    newUser.IsActive <- true
                    newUser.FailedLoginAttempts <- 0
                    newUser.CreatedAt <- DateTime.UtcNow

                    do! dbContext.SubmitUpdates2()
                    return RegistrationSuccess
        }

    let private ``reset failed attempts`` (user:WriteDataContext.``Companyweb.usersEntity``) =
        user.FailedLoginAttempts <- 0
        user.LastFailedLogin <- ValueNone
        user.LockedUntil <- ValueNone

    let private ``increment failed attempts`` (user:WriteDataContext.``Companyweb.usersEntity``) =
        user.FailedLoginAttempts <- user.FailedLoginAttempts + 1
        user.LastFailedLogin <- ValueSome DateTime.UtcNow
        if user.FailedLoginAttempts >= maxFailedAttempts then
            user.LockedUntil <- ValueSome (DateTime.UtcNow.AddMinutes(float lockoutMinutes))

    let ``authenticate user`` (dbContext:WriteDataContext) (request:LoginRequest) =
        task {
            let normalizedEmail = request.Email |> ``normalize email``
            // Note: a more hardened login flow would also record failed attempts by IP/email for audit,
            // add protection for dictionary attacks, and throttle repeated login attempts.
            let! userOpt =
                query {
                    for u in dbContext.Companyweb.Users do
                    where (u.Email = normalizedEmail)
                } |> Seq.tryHeadAsync

            match userOpt with
            | None ->
                return InvalidCredentials
            | Some user ->
                if not user.IsActive then
                    return AccountInactive
                else
                    match user.LockedUntil with
                    | ValueSome lockedUntil when lockedUntil > DateTime.UtcNow ->
                        return AccountLocked lockedUntil
                    | _ ->
                        let passwordValid = ``verify password`` request.Password user.PasswordHash
                        if passwordValid then
                            ``reset failed attempts`` user
                            user.LastLoginAt <- ValueSome DateTime.UtcNow
                            do! dbContext.SubmitUpdates2()
                            return Success (user.Id, user.Email)
                        else
                            ``increment failed attempts`` user
                            do! dbContext.SubmitUpdates2()
                            match user.LockedUntil with
                            | ValueSome lockedUntil -> return AccountLocked lockedUntil
                            | ValueNone -> return InvalidCredentials
        }

    let ``get user by id`` (dbContext:ReadDataContext) (userId:int) =
        task {
            let! userOpt =
                query {
                    for u in dbContext.Companyweb.Users do
                    where (u.Id = userId)
                    select (u.Id, u.Email, u.IsActive)
                } |> Seq.tryHeadAsync
            return userOpt
        }
