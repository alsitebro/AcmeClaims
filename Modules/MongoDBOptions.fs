namespace AcmeClaims

open System

type MongoDBOptions() =
    let mutable connectionString: string = String.Empty

    member _.ConnectionString
        with get() = connectionString
        and set(value: string) = connectionString <- value
