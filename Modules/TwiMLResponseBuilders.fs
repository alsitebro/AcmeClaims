namespace AcmeClaims

open System
open System.Collections.Generic
open Microsoft.AspNetCore.Http
open Twilio.Http
open Twilio.TwiML
open Twilio.TwiML.Voice
open Microsoft.Extensions.Caching.Memory

module TwiMLResponseBuilders =
   
   let ClaimStartResponse(request: HttpRequest): VoiceResponse =
      let mutable response: VoiceResponse = VoiceResponse()
      
      let gather = 
            Gather(action = Uri ($"{request.Scheme}://{request.Host}/claims/gather/option"), 
               input = List<Gather.InputEnum>([|Gather.InputEnum.Dtmf; Gather.InputEnum.Speech|]), 
               method = HttpMethod.Post, 
               numDigits = 1, 
               finishOnKey = "#")
               .Say("Hi. Welcome to Acme Claims. To start a new claim press 1, followed by the hash key. Or for an existing claim press 2, followed by the hash key.", 
                  Say.VoiceEnum.PollyAmy, 
                  language = Say.LanguageEnum.EnGb)
      response <- response.Append(gather)
      response <- response.Say("We didn't receive any input from you. Goodbye!")
      response

   let ProcessClaimOption(request: HttpRequest, cache: IMemoryCache) : VoiceResponse =
      let response: VoiceResponse = VoiceResponse()
      let form: IDictionary<string,string> = Helpers.ToDictionary(request.Form)
      let session = cache.Get<SessionModel>(form.["CallSid"])
      
      let digits = form.["Digits"]
      if digits = "1" then
        cache.Set(session.CallSid, { session with Option = "NewClaim" }, absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15)) |> ignore
        response.Redirect(Uri $"{request.Scheme}://{request.Host}/identity/gather/account", HttpMethod.Post)
      elif digits = "2" then
        cache.Set(session.CallSid, { session with Option = "ExistingClaim" }, absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15)) |> ignore
        response.Redirect(Uri $"{request.Scheme}://{request.Host}/identity/gather/account", HttpMethod.Post)
      else
        if session.Retries >= 3 then
          VoiceResponse().Redirect(Uri $"{request.Scheme}://{request.Host}/claims/fail", HttpMethod.Post)
        else
          cache.Set(session.CallSid, { session with Retries = session.Retries + 1 }, absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15)) |> ignore
          response.Say($"Sorry I was unable to process your request. Please try again.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)
   
   let GatherAccountNumber (request: HttpRequest, cache: IMemoryCache) =
      let mutable response: VoiceResponse = VoiceResponse()
      
      let gather = 
            Gather(action = Uri ($"{request.Scheme}://{request.Host}/identity/gather/postcode"), 
               input = List<Gather.InputEnum>([|Gather.InputEnum.Dtmf; Gather.InputEnum.Speech|]), 
               method = HttpMethod.Post, 
               numDigits = 5, 
               finishOnKey = "#")
               .Say("Please enter your account number, followed by the hash key.", 
                  Say.VoiceEnum.PollyAmy, 
                  language = Say.LanguageEnum.EnGb)
      response <- response.Append(gather)
      response <- response.Say("We didn't receive any input from you. Goodbye!")
      response

   let GatherPostCode (request: HttpRequest, cache: IMemoryCache) =
      let mutable response: VoiceResponse = VoiceResponse()
      //get account number from Digits field
      let form: IDictionary<string,string> = Helpers.ToDictionary(request.Form)
      let callSid = form.["CallSid"]
      let session = cache.Get<SessionModel>(callSid) // what happens if session is null
      let digits = form.["Digits"]
      // add account number to cache, and gather postcode
      cache.Set(callSid, {session with AccountNumber = digits}, absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15)) |> ignore
      let gather = 
            Gather(action = Uri ($"{request.Scheme}://{request.Host}/identity/verify"), 
               input = List<Gather.InputEnum>([|Gather.InputEnum.Dtmf; Gather.InputEnum.Speech|]), 
               method = HttpMethod.Post, 
               numDigits = 5, 
               finishOnKey = "#")
               .Say("Please enter your post code, followed by the hash key.", 
                  Say.VoiceEnum.PollyAmy, 
                  language = Say.LanguageEnum.EnGb)
      response <- response.Append(gather)
      response <- response.Say("We didn't receive any input from you. Goodbye!")
      response

   let VerifyIdentity (request: HttpRequest, cache: IMemoryCache) =
      let response: VoiceResponse = VoiceResponse()
      let form: IDictionary<string,string> = Helpers.ToDictionary(request.Form)
      let callSid = form.["CallSid"]
      let session = cache.Get<SessionModel>(callSid) // what happens if session is null
      let digits = form.["Digits"]
      cache.Set(callSid, {session with PostCode = digits}, absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15)) |> ignore
      // Try get from database (Azure Cosmos DB Table Storage) using account number and post code.
      //if input are valid, check session.Option, use to determine redirect, or redirect to fail
      response.Redirect(Uri $"{request.Scheme}://{request.Host}/identity/fail", HttpMethod.Post)
      // or response.Redirect(Uri $"{request.Scheme}://{request.Host}/claims/complete", HttpMethod.Post)
      // or response.Redirect(Uri $"{request.Scheme}://{request.Host}/claims/underwriter", HttpMethod.Post)

   let RedirectToUnderwriter (request: HttpRequest, cache: IMemoryCache) =
      let response: VoiceResponse = VoiceResponse()
      let form: IDictionary<string,string> = Helpers.ToDictionary(request.Form)
      let callSid = form.["CallSid"]
      let session = cache.Get<SessionModel>(callSid) // what happens if session is null
      let digits = form.["Digits"]
      cache.Set(callSid, {session with PostCode = digits}, absoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15)) |> ignore
      response.Say($"This is the point where your call would be transferred to your underwriter to handle your claim.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)

   let RegisterClaim (request: HttpRequest, cache: IMemoryCache) =
      let response: VoiceResponse = VoiceResponse()
      let form: IDictionary<string,string> = Helpers.ToDictionary(request.Form)
      let callSid = form.["CallSid"]
      let session = cache.Get<SessionModel>(callSid) // what happens if session is null
      let claimId = Guid.NewGuid().ToString("N").ToUpper().Substring(0, 4)
      response.Say($"Your claim has now been registered. Your claim reference is {claimId.[0]} {claimId.[1]} {claimId.[2]} {claimId.[3]} {claimId.[4]}", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)
        .Say($"You will receive a call from your underwriter. Make sure you have your claim reference to hand whenever you make correspondence relating to your claim.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)
        .Say($"Thank you for calling Acme Claims. Good bye.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)
        .Hangup()
      

   let RedirectToClaimsFail(request: HttpRequest) : VoiceResponse =
      VoiceResponse().Redirect(Uri $"{request.Scheme}://{request.Host}/claims/fail", HttpMethod.Post)
   
   let ReturnFailedClaimsCall(request: HttpRequest) : VoiceResponse =
      let response: VoiceResponse = 
        VoiceResponse()
            .Say("Unfortunately, I was unable to complete your request. Please try again later. Good bye.", Say.VoiceEnum.PollyAmy, language = Say.LanguageEnum.EnGb)
      response