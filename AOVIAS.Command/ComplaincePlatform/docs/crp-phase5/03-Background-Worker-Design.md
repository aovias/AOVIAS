# Background Worker Design

TrialLifecycleHostedService (BackgroundService): 1m startup delay, EvaluateAllAsync(Daily), interval EvaluationIntervalHours. Cursor-batched by LicenseKey.Id. Failures recorded on TrialLifecycleRun.

