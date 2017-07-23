module Scheduler

open System
open System.Threading
open FSharp.Data.Sql

type ScheduledEventType =
| Once
| Recurring

/// Async timer to perform actions
let timer eventType interval scheduledAction = async {
    match eventType with
    | Once ->
        do! interval |> Async.Sleep
        do! scheduledAction()
    | Recurring ->
        while true do
            do! interval |> Async.Sleep
            try
                do! scheduledAction()
            with // if error on recurring action, just log and skip one
            | e -> Logary.Message.eventError "Scheduler failure: {err} \r\n\r\n {stack}"
                   |> Logary.Message.setField "err" (e.ToString())
                   |> Logary.Message.setField "stack" (System.Diagnostics.StackTrace(1, true).ToString())
                   |> writeLog
}

// Basic idea from: http://msdn.microsoft.com/en-us/library/ee370246.aspx

/// Agent to maintain a queue of scheduled tasks that can be canceled.
/// It never runs its processor function, so it doesn't do anything.
let scheduleAgent = new MailboxProcessor<Guid * CancellationTokenSource>(fun _ -> async { () })

/// Add action to timer, return guid to use to cancel the job.
let scheduleAction eventType interval scheduledAction =
    let id = Guid.NewGuid()
    let cancel = new CancellationTokenSource()
    Async.Start (timer eventType interval scheduledAction, cancel.Token)
    scheduleAgent.Post(id, cancel)
    id

let cancelAction id =
    scheduleAgent.TryScan((fun (aId, source) ->
        let action =
            async {
                source.Cancel()
                return id
            }
        if (id = aId) then
            Some(action)
        else
            None), 3000) // timeout: if queue is empty, wait 3000ms to get a cancelation request.
    |> Async.RunSynchronously

// Testing:
(*
let someAction() = async { Console.WriteLine "hello1" } |> asyncScheduleErrorHandling
let someAction2() = async { Console.WriteLine "hello2" } |> asyncScheduleErrorHandling

let jobId1 = scheduleAction ScheduledEventType.Recurring 3000 someAction
let jobId2 = scheduleAction ScheduledEventType.Recurring 5000 someAction2
cancelAction jobId1
*)
