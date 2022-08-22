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
        cache.Set(session.CallSid, session, absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15) )
    
    let RetrieveFromCache(id: string) =
        cache.Get<SessionModel>(id)

    [<HttpPost>]
    [<Route("start")>]
    member _.Start() =
        if signatureValidator.Validate (base.Request) <> true then
            TwiMLResult (RedirectToClaimsFail(base.Request))
        else
            let parameters = Helpers.ToDictionary(base.Request.Form)
            let session: SessionModel = AddToCache({CallSid=parameters["CallSid"]; From=parameters["From"]; AccountNumber=""; PostCode=""})
            TwiMLResult (ClaimStartResponse(base.Request))
    
    [<HttpPost>]
    [<Route("gather/option")>]
    member _.GatherOptions() =
       if signatureValidator.Validate (base.Request) <> true then TwiMLResult (RedirectToClaimsFail(base.Request))
       else
           let parameters = Helpers.ToDictionary(base.Request.Form)
           let session = RetrieveFromCache(parameters["CallSid"])
                      
           TwiMLResult (ClaimStartResponse(base.Request))
    
    [<HttpPost>]
    [<Route("fail")>]
    member _.Fail() =
        ""
    
    [<HttpPost>]
    [<Route("redirect/underwriter")>]
    member _.RedirectToUnderwriter() =
        ""
    
    [<HttpPost>]
    [<Route("complete")>]
    member _.Complete() =
        ""