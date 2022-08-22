namespace AcmeClaims

open System
open System.Collections.Generic
open Microsoft.AspNetCore.Http
open Twilio.Http
open Twilio.TwiML
open Twilio.TwiML.Voice

module TwiMLResponseBuilders =
   
   let ClaimStartResponse(request: HttpRequest): VoiceResponse =
      let mutable response: VoiceResponse = VoiceResponse()
      
      let gather = 
            Gather(action = Uri ($"{request.Scheme}://{request.Host}/voice/claims/option"), 
               input = List<Gather.InputEnum>([|Gather.InputEnum.Dtmf; Gather.InputEnum.Speech|]), 
               method = HttpMethod.Post, 
               numDigits = 1, 
               finishOnKey = "#")
               .Say("Hi, welcome to Acme Claims. To start a new claim press 1, or for an existing claim press 2. Followed by the hash key.", 
                  Say.VoiceEnum.PollyBianca, 
                  loop = 0, 
                  language = Say.LanguageEnum.EnGb)
      response <- response.Append(gather)
      response <- response.Say("We didn't receive any input from you. Goodbye!")
      response

   let RedirectToClaimsFail(request: HttpRequest) : VoiceResponse =
      VoiceResponse().Redirect(Uri $"{request.Scheme}://{request.Host}/claims/fail", HttpMethod.Post)