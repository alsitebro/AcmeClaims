namespace AcmeClaims

open System

type TwilioOptions() =
    let mutable accountSid: string = String.Empty
    let mutable authToken: string = String.Empty

    member _.AccountSid 
        with get() = accountSid 
        and set(value: string) = accountSid <- value
    member _.AuthToken 
        with get() = authToken 
        and set(value: string) = authToken <- value