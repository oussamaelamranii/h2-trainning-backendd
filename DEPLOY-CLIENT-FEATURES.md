# Client profile editing and communication

Deploy the backend and database changes before the frontend.

1. Take a fresh production database backup.
2. Confirm that the production connection is selected in Visual Studio and that `AddHealthProfileAndProgressActivity` is already applied.
3. In Package Manager Console, run `Update-Database -Migration AddClientCommunicationAndCoachWeights` against the backend project. This adds `BodyWeightEntries`, `CoachMessages`, and `PostReads`; it does not remove existing tables or columns.
4. Publish the backend to SmarterASP with the existing publish profile.
5. Deploy the frontend after the backend is available.

Coach: open Clients, open a client's details, choose **Edit profile & weight**, or use **Direct messages to client**. Changing email also changes the client's login email. Only the owning coach can edit/send.

Client: messages appear on the dashboard and the current-program score appears above the program. The profile retains its progress card. Advice has an unread badge and a persistent notification banner until the client selects **Mark as read**. Message read receipts are also explicit. Notifications refresh on login, window focus, and every 30 seconds while the page is visible. Read state is stored in SQL Server and survives refreshes and device changes. Existing advice starts unread until acknowledged.

Coach weight entries retain their own dates and do not fabricate sleep, mood, or energy check-ins. Current weight/BMI use whichever is newer: a client check-in with weight or a coach weight entry.

## Local integration verification

`tests/Verify-ClientFeatures.ps1` exercises the HTTP API on `http://127.0.0.1:5189` and rejects remote URLs. Run against a backend configured to use a dedicated local test database only. It creates isolated test accounts and checks profile/weight changes, email/login changes, validation, coach ownership, message access/read receipts, advice notifications/read persistence, program progression, and deletion regressions. It leaves remaining test data in the local verification database.

Production migration/publish are not performed by this verification script.
