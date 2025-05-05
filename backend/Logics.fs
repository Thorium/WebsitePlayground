// --- Some logics, because it's not good to add everyting to the SignalRHub, just as if you want to share it with e.g. a WebApi --------------------------------c----
module Logics

    open System
    open System.Linq
    open FSharp.Data
    open FSharp.Data.Sql

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
                do! dbContext.SubmitUpdates2 ()
                return entity.ColumnValues
            | None -> return Seq.empty
        }
