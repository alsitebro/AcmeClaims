namespace AcmeClaims

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Caching.Memory
open Microsoft.Extensions.Logging
open Twilio.AspNet.Core
open TwiMLResponseBuilders


[<ApiController>]
[<Route("claims")>]
type ClaimsController (logger: ILogger<ClaimsController>, signatureValidator: TwilioSignatureValidator, cache: IMemoryCache) =
    inherit TwilioController()
    
    let AddToCache(session: SessionModel) =
        cache.Set(session.CallSid, session, absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15))
    
    let RetrieveFromCache(id: string) =
        cache.Get<SessionModel>(id)

    [<HttpPost>]
    [<Route("start")>]
    member _.Start() =
        if signatureValidator.Validate (base.Request) <> true then
            TwiMLResult (RedirectToClaimsFail(base.Request))
        else
            let parameters = Helpers.ToDictionary(base.Request.Form)
            logger.LogTrace("Payload received from Twilio", parameters)
            let session: SessionModel = AddToCache({CallSid=parameters["CallSid"]; From=parameters["From"]; AccountNumber=""; PostCode=""; Retries=0; Option = ""})
            TwiMLResult (ClaimStartResponse(base.Request))
    
    [<HttpPost>]
    [<Route("gather/option")>]
    member _.GatherOptions() =
       if signatureValidator.Validate (base.Request) <> true then 
            TwiMLResult (RedirectToClaimsFail(base.Request))
       else
           TwiMLResult (ProcessClaimOption(base.Request, cache))
    
    [<HttpPost>]
    [<Route("fail")>]
    member _.Fail() =
        TwiMLResult (ReturnFailedClaimsCall(base.Request))
    
    [<HttpPost>]
    [<Route("underwriter")>]
    member _.RedirectToUnderwriter() =
        TwiMLResult (RedirectToUnderwriter(base.Request, cache))
    
    [<HttpPost>]
    [<Route("complete")>]
    member _.Complete() =
        TwiMLResult (RegisterClaim(base.Request, cache))