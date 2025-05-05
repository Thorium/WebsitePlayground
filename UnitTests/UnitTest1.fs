module UnitTests

open System
open System.Reflection
open NUnit.Framework

module MockDatabase =

    /// This is a helper-class to replace the real database-connection
    type MockDatabaseContext(demodata:Map<string,obj>) =

        let mutable initialContextHolders = None

        /// This will repalce dbReadContext() with a mock data
        let setupMockContext mockContext =
            let lazyReadContext = lazy mockContext
            let _ = lazyReadContext.Force()
            initialContextHolders <- Some (dbReadContext())
            typeof<Domain.ContextHolderMarker>.DeclaringType.GetProperty("contextHolder", (BindingFlags.NonPublic ||| BindingFlags.Static)).SetValue(null, lazyReadContext)

        /// This will return your original dbReadContext(), just in case some other unit-test actually uses the real database.
        let teardownMockContext() =
            match initialContextHolders with
            | Some realConnection->
                let lazyReadContext = lazy realConnection
                typeof<Domain.ContextHolderMarker>.DeclaringType.GetProperty("contextHolder", BindingFlags.NonPublic ||| BindingFlags.Static).SetValue(null, lazyReadContext)
            | None -> ()

        let mockContext =
            let readContext = FSharp.Data.Sql.Common.OfflineTools.CreateMockSqlDataContext<ReadDataContext> demodata
            setupMockContext readContext
            readContext

        /// ReadDataContext
        member __.ReadDataContext = mockContext

        interface IDisposable with
            member x.Dispose() =
                teardownMockContext()

        member this.Dispose() =
            (this :> IDisposable).Dispose()

open MockDatabase

[<TestFixture>]
type ``Program logic tests fixture``() =

    //[<SetUp>]
    //member this.Setup () =
    //    ()

    /// This example unit-test will create a mock-database and execute real search logic to test that the result is what is expeced.
    [<Test>]
    member  this.``Test company search LINQ logic`` () =
        task {
            let uid1 = System.Guid.NewGuid()
            let uid2 = System.Guid.NewGuid()
            let today = DateTime.Today

            // Mock database
            let test_data = // Note case-sensitivity: has to match DB in both table names and column names.
                [ "companyweb.company",
                    [|  {| Id = uid1; Founded = DateTime(2000,01,01); Name = "TESTCORP 1"; Ceo = "Mr Boss" |}
                        {| Id = uid1; Founded = DateTime(2010,01,01); Name = "TESTCORP 2"; Ceo = "Mr Boss" |}
                        |] :> obj
                    // other mock tables here...
                    "companyweb.login",
                    [|  {| UserId = uid1; IP = "1.2.3.4"; |}
                        {| UserId = uid2; IP = "255.255.255.10"; |}
                        |] :> obj
                ] |> Map.ofList

            use mockContext = new MockDatabaseContext(test_data)

            let searchTest = { FoundedAfter = DateTime(1980,01,01); FoundedBefore = DateTime(2005,01,01); CompanyName  = "Test"; CEOName = None}
            let! foundItems = Logics.executeSearch (mockContext.ReadDataContext) searchTest

            mockContext.Dispose()

            Assert.That(1 = foundItems.Length, $"Expected 1 but found {foundItems.Length}")
            Assert.That("TESTCORP 1" = foundItems[0].CompanyName, $"Name was unexpected {foundItems[0].CompanyName}")
        }
