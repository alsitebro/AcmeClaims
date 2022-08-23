namespace AcmeClaims

open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Twilio.AspNet.Core
open Microsoft.Extensions.Caching.Memory
open Twilio.TwiML
open Twilio.Http
open Twilio.TwiML.Voice
open System
open System.Collections.Generic
open System.Linq
open MongoDB.Driver
open MongoDB.Bson
open MongoDB.Driver.Linq

[<ApiController>]
[<Route("identity")>]
type IdentityController (logger: ILogger<IdentityController>, signatureValidator: TwilioSignatureValidator, cache: IMemoryCache, db: IMongoDatabase) =
    inherit ControllerBase()
    
    [<HttpPost>]
    [<Route("gather/account")>]
    member _.GatherAccountNumber() =
        let baseUri = $"{base.Request.Scheme}://{base.Request.Host}"
        if signatureValidator.Validate (base.Request) <> true then
           TwiMLResult (VoiceResponse().Redirect(Uri $"{baseUri}/claims/fail", HttpMethod.Post))
        else
           let gatherPrompt = 
               Gather(action = Uri ($"{baseUri}/identity/verify"), input = List<Gather.InputEnum>([|Gather.InputEnum.Dtmf; Gather.InputEnum.Speech|]), method = HttpMethod.Post, numDigits = 5, finishOnKey = "#", timeout = 10)
                    .Say("Please enter your account number.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)
           let response = VoiceResponse().Append(gatherPrompt).Say("We didn't receive any input from you. Goodbye!", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)

           TwiMLResult (response)
    
    [<HttpPost>]
    [<Route("verify")>]
    member _.Verify() =
        let baseUri = $"{base.Request.Scheme}://{base.Request.Host}"
        if signatureValidator.Validate (base.Request) <> true then
           TwiMLResult (VoiceResponse().Redirect(Uri $"{baseUri}/claims/fail", HttpMethod.Post))
        else
          let form: IDictionary<string,string> = Helpers.ToDictionary(base.Request.Form)
          let callSid = form.["CallSid"]
          let session = cache.Get<SessionModel>(callSid) // what happens if session is null
          if obj.ReferenceEquals(session, null) then
                TwiMLResult (VoiceResponse().Say("There was no session in cache", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb))
          else 
            let accountNumber = form.["Digits"]
            cache.Set(callSid, {session with AccountNumber = accountNumber}, absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15)) |> ignore
            let doc = db.GetCollection<BsonDocument>("customers").Find(BsonDocument("_id", session.AccountNumber)).ToList()
            let response = VoiceResponse()
            if doc.Any() = false then
                TwiMLResult (response.Redirect(Uri $"{baseUri}/identity/fail", HttpMethod.Post))
            else
                match session.Option with
                | "NewClaim" -> TwiMLResult (response.Redirect(Uri $"{baseUri}/claims/register", HttpMethod.Post))
                | "ExistingClaim" -> TwiMLResult (response.Redirect(Uri $"{baseUri}/claims/underwriter", HttpMethod.Post))
                | _ -> TwiMLResult (response.Redirect(Uri $"{baseUri}/claims/done", HttpMethod.Post))
    
    [<HttpPost>]
    [<Route("fail")>]
    member _.Fail() =
        let response: VoiceResponse = VoiceResponse().Say("Unfortunately, I was unable to complete your request. Please try again later. Good bye.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)
        
        TwiMLResult (response)