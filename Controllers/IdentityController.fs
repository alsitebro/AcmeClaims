namespace AcmeClaims.Controllers

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open AcmeClaims


[<ApiController>]
[<Route("identity")>]
type IdentityController (logger: ILogger<IdentityController>) =
    inherit ControllerBase()
    
    [<HttpPost>]
    [<Route("gather/account")>]
    member _.GatherAccountNumber() =
        ""
    
    [<HttpPost>]
    [<Route("gather/postcode")>]
    member _.GatherCode() =
        ""
    
    [<HttpPost>]
    [<Route("fail")>]
    member _.Fail() =
        ""
    
    [<HttpPost>]
    [<Route("verify")>]
    member _.Verify() =
        ""