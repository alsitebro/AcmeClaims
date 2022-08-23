namespace AcmeClaims

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Caching.Memory
open Microsoft.Extensions.Logging
open Newtonsoft.Json
open Twilio.AspNet.Core
open System.Collections.Generic
open Twilio.TwiML
open Twilio.Http
open Twilio.TwiML.Voice
open MongoDB.Driver
open MongoDB.Bson


[<ApiController>]
[<Route("claims")>]
type ClaimsController (logger: ILogger<ClaimsController>, signatureValidator: TwilioSignatureValidator, cache: IMemoryCache, db: IMongoDatabase) =
    inherit TwilioController()
    
    let AddToCache(session: SessionModel) =
        cache.Set(session.CallSid, session, absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15))

    [<HttpPost>]
    [<Route("start")>]
    member _.Start() =
        let baseUri = $"{base.Request.Scheme}://{base.Request.Host}"
        
        if signatureValidator.Validate (base.Request) <> true then 
           TwiMLResult (VoiceResponse().Redirect(Uri $"{baseUri}/claims/fail", HttpMethod.Post))
        else
            let parameters = Helpers.ToDictionary(base.Request.Form)
            logger.LogTrace("Payload received from Twilio", parameters)
            let session: SessionModel = cache.Set(parameters["CallSid"], 
                    {CallSid=parameters["CallSid"]; From=parameters["From"]; AccountNumber=""; Retries=0; Option = ""}, 
                    absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15))
            let gatherPrompt =
                Gather(action = Uri ($"{baseUri}/claims/options"), input = List<Gather.InputEnum>([|Gather.InputEnum.Dtmf; Gather.InputEnum.Speech|]), method = HttpMethod.Post, numDigits = 1,
                        timeout = 10)
                    .Say("Hi. Welcome to Acme Claims. To begin, please listen to the following options. To start a new claim press 1. Or for an existing claim press 2.", 
                        Say.VoiceEnum.PollyAmy, 
                        language = Say.LanguageEnum.EnGb)
            let response = VoiceResponse().Append(gatherPrompt).Say("As I haven't receive any input from you, I am going to hang up now. Goodbye.", Say.VoiceEnum.PollyAmy, 
                        language = Say.LanguageEnum.EnGb)
            
            TwiMLResult (response)
    
    [<HttpPost>]
    [<Route("options")>]
    member _.Options() =
       let baseUri = $"{base.Request.Scheme}://{base.Request.Host}"
       
       if signatureValidator.Validate (base.Request) <> true then 
          TwiMLResult (VoiceResponse().Redirect(Uri $"{baseUri}/claims/fail", HttpMethod.Post))
       else
           let form: IDictionary<string,string> = Helpers.ToDictionary(base.Request.Form)
           let response: VoiceResponse = VoiceResponse()
           let session = cache.Get<SessionModel>(form.["CallSid"])
           let baseUri = $"{base.Request.Scheme}://{base.Request.Host}"
           let digits = form.["Digits"]
           if digits = "1" then
             cache.Set(session.CallSid, { session with Option = "NewClaim" }, absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15)) |> ignore
             
             TwiMLResult (response.Redirect(Uri $"{baseUri}/identity/gather/account", HttpMethod.Post))
           elif digits = "2" then
                cache.Set(session.CallSid, { session with Option = "ExistingClaim" }, absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15)) |> ignore
             
                TwiMLResult (response.Redirect(Uri $"{baseUri}/identity/gather/account", HttpMethod.Post))
            else
            if session.Retries >= 3 then
                TwiMLResult (response.Redirect(Uri $"{baseUri}/claims/fail", HttpMethod.Post))
            else
                cache.Set(session.CallSid, { session with Retries = session.Retries + 1 }, absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15)) |> ignore
                
                TwiMLResult (response.Say($"Sorry I was unable to process your request. Please try again.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb))
           
    
    [<HttpPost>]
    [<Route("fail")>]
    member _.Fail() =
        let response: VoiceResponse = VoiceResponse().Say("Unfortunately, I was unable to complete your request. Please try again later. Good bye.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)
        
        TwiMLResult (response)
    
    [<HttpPost>]
    [<Route("underwriter")>]
    member _.RedirectToUnderwriter() =
        let baseUri = $"{base.Request.Scheme}://{base.Request.Host}"
       
        if signatureValidator.Validate (base.Request) <> true then 
            TwiMLResult (VoiceResponse().Redirect(Uri $"{baseUri}/claims/fail", HttpMethod.Post))
        else
          let response: VoiceResponse = VoiceResponse()
          let form: IDictionary<string,string> = Helpers.ToDictionary(base.Request.Form)
          let callSid = form.["CallSid"]
          let session = cache.Get<SessionModel>(callSid) // what happens if session is null
          let doc = {| _id = Guid.NewGuid().ToString("N").ToUpper().Substring(0, 5); data = { session with From = String.Empty }; timestamp = DateTime.UtcNow |} //anonymous record
          let result = db.GetCollection("calls_routed_to_underwriter").InsertOne(doc.ToBsonDocument())
      
          TwiMLResult (response.Say($"Your call will now be transferred to your underwriter to handle your claim.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb).Say($"Thank you for calling Acme Claims. Good bye.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb))
    
    [<HttpPost>]
    [<Route("register")>]
    member _.Register() =
       let baseUri = $"{base.Request.Scheme}://{base.Request.Host}"
       
       if signatureValidator.Validate (base.Request) <> true then 
          TwiMLResult (VoiceResponse().Redirect(Uri $"{baseUri}/claims/fail", HttpMethod.Post))
       else
          let form: IDictionary<string,string> = Helpers.ToDictionary(base.Request.Form)
          let callSid = form.["CallSid"]
          let session = cache.Get<SessionModel>(callSid) // what happens if session is null
          let claimId = Guid.NewGuid().ToString("N").ToUpper().Substring(0, 5)
          let doc = {| _id = claimId; data = { session with From = String.Empty }; timestamp = DateTime.UtcNow |} //anonymous record
          let result = db.GetCollection("claims").InsertOne(doc.ToBsonDocument())
          let response: VoiceResponse = 
            VoiceResponse().Say($"Your claim has now been registered. Your claim reference is {claimId.[0]} {claimId.[1]} {claimId.[2]} {claimId.[3]} {claimId.[4]}", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)
                .Say($"You will receive a call from your underwriter. Make sure you have your claim reference to hand whenever you make correspondence.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)
                .Say($"Thank you for calling Acme Claims. Good bye.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)

          TwiMLResult (response)

    [<HttpPost>]
    [<Route("done")>]
    member _.Done() =
      let response: VoiceResponse = 
        VoiceResponse().Say($"Thank you for calling Acme Claims. Good bye.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)

      TwiMLResult (response)