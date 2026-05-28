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

    let private maxFailedAttempts = 5
    let private lockoutMinutes = 15

    let private openConnection() =
        task {
            let connection = new global.MySqlConnector.MySqlConnection(cstr)
            do! connection.OpenAsync()
            return connection
        }

    let ``register user`` (request:RegisterRequest) =
        task {
            let normalizedEmail = request.Email |> ``normalize email``
            match ``validate password strength`` request.Password with
            | Some reason -> return WeakPassword reason
            | None ->
                use! connection = openConnection()
                use existingCommand = new global.MySqlConnector.MySqlCommand("SELECT 1 FROM users WHERE Email = @email LIMIT 1", connection)
                existingCommand.Parameters.AddWithValue("@email", normalizedEmail) |> ignore
                let! existingUser = existingCommand.ExecuteScalarAsync()

                if isNull existingUser then
                    let passwordHash = ``hash password`` request.Password
                    use insertCommand = new global.MySqlConnector.MySqlCommand("INSERT INTO users (Email, PasswordHash, IsActive, FailedLoginAttempts, CreatedAt) VALUES (@email, @passwordHash, @isActive, @failedLoginAttempts, @createdAt)", connection)
                    insertCommand.Parameters.AddWithValue("@email", normalizedEmail) |> ignore
                    insertCommand.Parameters.AddWithValue("@passwordHash", passwordHash) |> ignore
                    insertCommand.Parameters.AddWithValue("@isActive", true) |> ignore
                    insertCommand.Parameters.AddWithValue("@failedLoginAttempts", 0) |> ignore
                    insertCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow) |> ignore
                    let! _ = insertCommand.ExecuteNonQueryAsync()
                    return RegistrationSuccess
                else
                    return EmailExists
        }

    let ``authenticate user`` (request:LoginRequest) =
        task {
            let normalizedEmail = request.Email |> ``normalize email``
            use! connection = openConnection()
            use selectCommand = new global.MySqlConnector.MySqlCommand("SELECT Id, Email, PasswordHash, IsActive, FailedLoginAttempts, LockedUntil FROM users WHERE Email = @email LIMIT 1", connection)
            selectCommand.Parameters.AddWithValue("@email", normalizedEmail) |> ignore
            use! reader = selectCommand.ExecuteReaderAsync()
            let! hasUser = reader.ReadAsync()

            if not hasUser then
                return InvalidCredentials
            else
                let userId = reader.GetInt32(0)
                let email = reader.GetString(1)
                let passwordHash = reader.GetString(2)
                let isActive = reader.GetBoolean(3)
                let failedAttempts = reader.GetInt32(4)
                let lockedUntil =
                    if reader.IsDBNull(5) then ValueNone else ValueSome (reader.GetDateTime(5))
                do reader.Close() //do! reader.CloseAsync()

                if not isActive then
                    return AccountInactive
                else
                    match lockedUntil with
                    | ValueSome lockedAt when lockedAt > DateTime.UtcNow ->
                        return AccountLocked lockedAt
                    | _ ->
                        let passwordValid = ``verify password`` request.Password passwordHash
                        if passwordValid then
                            use resetCommand = new global.MySqlConnector.MySqlCommand("UPDATE users SET FailedLoginAttempts = 0, LastFailedLogin = NULL, LockedUntil = NULL, LastLoginAt = @lastLoginAt WHERE Id = @id", connection)
                            resetCommand.Parameters.AddWithValue("@id", userId) |> ignore
                            resetCommand.Parameters.AddWithValue("@lastLoginAt", DateTime.UtcNow) |> ignore
                            let! _ = resetCommand.ExecuteNonQueryAsync()
                            return Success (userId, email)
                        else
                            let nextFailedAttempts = failedAttempts + 1
                            let lockExpiresAt =
                                if nextFailedAttempts >= maxFailedAttempts then
                                    ValueSome (DateTime.UtcNow.AddMinutes(float lockoutMinutes))
                                else
                                    ValueNone
                            use failCommand = new global.MySqlConnector.MySqlCommand("UPDATE users SET FailedLoginAttempts = @failedLoginAttempts, LastFailedLogin = @lastFailedLogin, LockedUntil = @lockedUntil WHERE Id = @id", connection)
                            failCommand.Parameters.AddWithValue("@id", userId) |> ignore
                            failCommand.Parameters.AddWithValue("@failedLoginAttempts", nextFailedAttempts) |> ignore
                            failCommand.Parameters.AddWithValue("@lastFailedLogin", DateTime.UtcNow) |> ignore
                            match lockExpiresAt with
                            | ValueSome lockedAt -> failCommand.Parameters.AddWithValue("@lockedUntil", lockedAt) |> ignore
                            | ValueNone -> failCommand.Parameters.AddWithValue("@lockedUntil", DBNull.Value) |> ignore
                            let! _ = failCommand.ExecuteNonQueryAsync()
                            match lockExpiresAt with
                            | ValueSome lockedAt -> return AccountLocked lockedAt
                            | ValueNone -> return InvalidCredentials
        }

