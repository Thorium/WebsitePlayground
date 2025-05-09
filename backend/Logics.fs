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
