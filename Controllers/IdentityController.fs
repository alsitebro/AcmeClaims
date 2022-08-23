namespace AcmeClaims

open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Twilio.AspNet.Core
open Microsoft.Extensions.Caching.Memory
open TwiMLResponseBuilders

[<ApiController>]
[<Route("identity")>]
type IdentityController (logger: ILogger<IdentityController>, signatureValidator: TwilioSignatureValidator, cache: IMemoryCache) =
    inherit ControllerBase()
    
    [<HttpPost>]
    [<Route("gather/account")>]
    member _.GatherAccountNumber() =
        if signatureValidator.Validate (base.Request) <> true then
           TwiMLResult (RedirectToClaimsFail(base.Request))
        else
           TwiMLResult (GatherAccountNumber(base.Request, cache))
    
    [<HttpPost>]
    [<Route("gather/postcode")>]
    member _.GatherCode() =
        if signatureValidator.Validate (base.Request) <> true then
           TwiMLResult (RedirectToClaimsFail(base.Request))
        else
           TwiMLResult (GatherPostCode(base.Request, cache))
    
    [<HttpPost>]
    [<Route("verify")>]
    member _.Verify() =
        if signatureValidator.Validate (base.Request) <> true then
           TwiMLResult (RedirectToClaimsFail(base.Request))
        else
           TwiMLResult (VerifyIdentity(base.Request, cache))
    
    [<HttpPost>]
    [<Route("fail")>]
    member _.Fail() =
        TwiMLResult (ReturnFailedClaimsCall(base.Request))