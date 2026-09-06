# TallyJ v3 SignalR Hubs Reference

This document describes every real-time **hub** in TallyJ v3: purpose, group model, who joins, who sends, client handlers, and security. It is intended to support a rewrite (v4) so no hub behavior is lost.

**Stack:** ASP.NET SignalR 2 (`Microsoft.AspNet.SignalR`), mapped in `Site/OwinStartup.cs` via `app.MapSignalR()` (default path `/signalr`).

**Source folder:** `Site/CoreModels/Hubs/`

---

## Architecture pattern

Each hub is implemented as **two classes**:

| Class | Role |
|--------|------|
| `XxxHub` | Server-side helper used by C# code. Obtains `IHubContext` via `GlobalHost.ConnectionManager.GetHubContext<XxxHubCore>()`, manages groups, and pushes messages to clients. |
| `XxxHubCore : Hub` | Empty (or nearly empty) SignalR hub class. Required so SignalR can expose the hub endpoint and so the JavaScript proxy is generated as `$.connection.xxxHubCore`. |

**Clients never call hub methods on the server for business logic.** Flow is:

1. Browser starts the shared SignalR connection (`$.connection.hub.start()` in `Site/Scripts/site.js` → `startSignalR`).
2. Browser POSTs to an MVC action with `connId` (and sometimes other keys).
3. That action calls `XxxHub.Join(...)`, which adds the connection to a **named group**.
4. Server code later broadcasts with `CoreHub.Clients.Group(groupName).clientMethod(...)`.

Security is **not** enforced with `[Authorize]` on hub classes. Isolation comes from:

- MVC authorization on **join** endpoints
- Session checks when joining
- **Group names** that encode election, login, voter, or ephemeral keys

If a client is not in a group, it does not receive that group’s messages.

### Shared client infrastructure

| Piece | Location | Role |
|--------|----------|------|
| `startSignalR(callback)` | `Site/Scripts/site.js` | Starts hub, stores `site.signalrConnectionId`, runs deferred join callbacks |
| `connectToElectionHub([electionGuidList])` | `Site/Scripts/site.js` | Joins MainHub for current election, or all listed elections (known teller dashboard) |
| `logoffSignalR()` | `Site/Scripts/site.js` | Disconnects on logout / form submit |

Auto-join MainHub: any page with `site.electionGuid` set runs `connectToElectionHub()` on load.

---

## Authorization roles used with hubs

| Role / flag | Meaning |
|-------------|---------|
| **Known teller** | `UserSession.IsKnownTeller` — authenticated account teller |
| **Guest teller** | `UserSession.IsGuestTeller` — joined via public listing + passcode, not a full account |
| **Voter** | `UserSession.IsVoter` — online voter (email / phone / kiosk identity) |
| **Anonymous public** | No session election/teller identity |

Relevant MVC attributes:

| Attribute | Effect |
|-----------|--------|
| `[AllowTellersInActiveElection]` | Known or guest teller; guest only if guests still allowed |
| `[ForAuthenticatedTeller]` | Known teller only |
| `[AllowVoter]` | Logged-in voter **and** host supports online elections |

---

## Quick index

| Hub | JS proxy | Group key pattern | Primary audience | Join endpoint auth |
|-----|----------|-------------------|------------------|--------------------|
| [MainHub](#1-mainhub) | `mainHubCore` | `Main{electionGuid}Known` / `…Guest` | Tellers in an election | Session election match; `JoinAll` known only |
| [PublicHub](#2-publichub) | `publicHubCore` | `Public` | Anonymous home page | Public (none) |
| [FrontDeskHub](#3-frontdeskhub) | `frontDeskHubCore` | `FrontDesk{electionGuid}` | Tellers (front desk, ballots, monitor) | Tellers in active election |
| [RollCallHub](#4-rollcallhub) | `rollCallHubCore` | `RollCall{electionGuid}` | Tellers on roll-call display | Tellers in active election |
| [AllVotersHub](#5-allvotershub) | `allVotersHubCore` | `AllVoters` | All connected online voters | Voter only |
| [VoterPersonalHub](#6-voterpersonalhub) | `voterPersonalHubCore` | `Voter{voterId}` | One voter’s sessions | Voter only |
| [VoterCodeHub](#7-votercodehub) | `voterCodeHubCore` | Client-supplied `key` | Voter login (code delivery) | Public + opaque key |
| [ImportHub](#8-importhub) | `importHubCore` | `Import{loginId}` | Known teller (CSV / election load) | Known teller |
| [BallotImportHub](#9-ballotimporthub) | `ballotImportHubCore` | `Import{loginId}` | Known teller (ballot import) | Known teller |
| [AnalyzeHub](#10-analyzehub) | `analyzeHubCore` | `Analyze{electionGuid}` | Known teller (analysis progress) | Known teller |

---

## 1. MainHub

**Files:** `Site/CoreModels/Hubs/MainHub.cs`

### Purpose

Broadcast **election-level status** to tellers working in an election (and to the elections list for known tellers). Kick **guest** tellers when an election is closed or unlisted.

### Group model

- Per election, two groups:
  - `Main{electionGuid}Known` — known tellers
  - `Main{electionGuid}Guest` — guest tellers
- `Join` places the connection in Known or Guest based on `UserSession.IsKnownTeller`.
- `JoinAll` (known tellers only) adds the connection to the **Known** group for each election GUID in a comma-separated list (dashboard multi-election listen).

Also refreshes computer “last contact” on join (`ComputerModel.RefreshLastContact()`).

### Server API (`MainHub`)

| Method | Effect |
|--------|--------|
| `Join(connectionId)` | Add to current election Known or Guest group |
| `JoinAll(connectionId, electionGuidList)` | Known teller only; join many `Main{guid}Known` groups |
| `StatusChanged(infoForKnown, infoForGuest)` | Push `statusChanged` to both groups of current election |
| `StatusChangedForElection(electionGuid, infoForKnown, infoForGuest)` | Same for a specific election |
| `CloseOutGuestTellers()` / `CloseOutGuestTellers(electionGuid)` | Push `electionClosed` to **Guest** group only |

### Who joins (clients)

| Client | How |
|--------|-----|
| Any teller page with `site.electionGuid` | `site.js` → `connectToElectionHub()` → `Public/JoinMainHub` |
| Elections list (dashboard) | `ElectionList.cshtml.js` → `connectToElectionHub(electionGuidList)` → `Public/JoinMainHubAll` |

### Join endpoints

| Action | Auth notes |
|--------|------------|
| `PublicController.JoinMainHub(connId, electionGuid)` | **No** `[Authorize]`. Silently returns if no current election, or if `electionGuid` ≠ `UserSession.CurrentElectionGuid`. |
| `PublicController.JoinMainHubAll(connId, electionGuidList)` | **No** `[Authorize]`. Silently returns unless `UserSession.IsKnownTeller`. |

### Client methods

| Method | Handler | Behavior |
|--------|---------|----------|
| `statusChanged(info)` | `site.js` | `site.broadcast(electionStatusChanged, info)` — UI updates status/online/passcode/listed |
| `electionClosed()` | `site.js` | Alert, log off SignalR, redirect to `Account/Logoff` |

Typical `info` shape (from `ElectionHelper.UpdateStatusInBrowsers`):

```text
{ ElectionGuid, StateName, Online, Passcode, Listed }
```

### Who sends (server)

| Caller | When |
|--------|------|
| `ElectionHelper.UpdateStatusInBrowsers` | Status / online / listing changes |
| `ElectionHelper.CloseElection` | Close → guest kick + public list refresh |
| `DashboardController.UpdateListingForElection` | Unlist election → close guests; always push status for that election |

### Security notes

- Separation of Known vs Guest allows **closing guests only** while known tellers stay connected.
- Join is soft-gated by session (not hard 401); wrong/missing session → no group membership.
- Payload for known vs guest can differ (`infoForKnown` / `infoForGuest`); today many callers send the same object to both.

### Rewrite checklist

- [ ] Election status push to all open teller UIs for that election  
- [ ] Multi-election listen on dashboard for known tellers  
- [ ] Guest-only force logout when election closed/unlisted  
- [ ] Distinct known/guest channels if different payloads are needed later  

---

## 2. PublicHub

**Files:** `Site/CoreModels/Hubs/PublicHub.cs`

### Purpose

Keep the **public home page** election dropdown current: which elections are open for guest tellers to join.

### Group model

- Single global group: `Public`

### Server API

| Method | Effect |
|--------|--------|
| `Join(connectionId)` | Add to `Public` |
| `TellPublicAboutVisibleElections()` | Rebuild list via `PublicElectionLister.RefreshAndGetListOfAvailableElections()` and push `ElectionsListUpdated(list)` |

### Who joins

| Client | How |
|--------|-----|
| Public home | `Views/Public/Home.cshtml.js` → `Public/PublicHub` |

### Join endpoint

| Action | Auth |
|--------|------|
| `PublicController.PublicHub(connId)` | Public. Also returns open elections HTML (`OpenElections()`). |

### Client methods

| Method | Behavior |
|--------|----------|
| `electionsListUpdated(listing)` | Rebuild `#ddlElections` options |

### Who sends

Triggered whenever listing visibility may change, including:

- `AllowTellersInActiveElectionAttribute` (known teller traffic keeps `ListedForPublicAsOf` fresh and notifies public)
- `DashboardController` (index, update listing)
- `ElectionHelper` (save election, close election, related paths)
- `UserSession` / `ComputerCacher` (session/computer listing side effects)

### Security notes

- Intentionally public: only **public listing HTML/data**, not private election data.
- Anyone can join the group; content must stay non-sensitive.

### Rewrite checklist

- [ ] Live refresh of “joinable elections” on the landing page  
- [ ] Same triggers when listing flags / passcodes / open windows change  

---

## 3. FrontDeskHub

**Files:** `Site/CoreModels/Hubs/FrontDeskHub.cs`

### Purpose

Real-time updates for **front desk** and related teller screens: person registration lines, full page reload after ballot import, and online election open/close times on monitor.

### Group model

- Per election: `FrontDesk{electionGuid}`  
- Requires `UserSession.CurrentElectionGuid` (asserted non-empty)

### Server API

| Method | Client event |
|--------|----------------|
| `Join(connectionId)` | — |
| `UpdatePeople(message)` | `updatePeople` |
| `ReloadPage()` | `reloadPage` |
| `UpdateOnlineElection(message)` | `updateOnlineElection` |

### Who joins

All via `BeforeController.JoinFrontDeskHub` (class: `[AllowTellersInActiveElection]`):

| Client page | File |
|-------------|------|
| Front desk | `Views/Before/FrontDesk.cshtml.js` |
| Ballot entry (normal / single) | `Views/Ballots/BallotNormal.cshtml.js`, `BallotSingle.cshtml.js` |
| Sort ballots | `Views/Ballots/SortBallots.cshtml.js` |
| Reconcile | `Views/Ballots/Reconcile.cshtml.js` |
| Monitor | `Views/After/Monitor.cshtml.js` |

### Client methods

| Method | Typical behavior |
|--------|------------------|
| `updatePeople(info)` | Merge person line changes into local lists / people helper; front desk redraws rows; monitor may refresh if online ballots affected |
| `reloadPage()` | `location.reload()` (front desk; used after ballot import) |
| `updateOnlineElection(info)` | Monitor updates online open/close display |

`UpdatePeople` payload (from `PeopleModel.UpdateFrontDeskListing`):

```text
{ PersonLines: [...], LastRowVersion }
```

### Who sends

| Caller | When |
|--------|------|
| `PeopleModel.UpdateFrontDeskListing` | Voting method / flag / person registration changes |
| `ElectionHelper` (save online settings, extend close, etc.) | `UpdateOnlineElection` with open/close times |
| `ImportBallotsModel` | `ReloadPage` after import/remove so front desk reloads |

### Security notes

- Join requires teller in active election (known or guest, subject to guest rules).
- Group is election-scoped; clients only see their election’s front-desk traffic.

### Rewrite checklist

- [ ] Live person registration updates across front desk, ballot entry, sort, reconcile, monitor  
- [ ] Online window time updates on monitor  
- [ ] Force front-desk reload after bulk ballot import  

---

## 4. RollCallHub

**Files:** `Site/CoreModels/Hubs/RollCallHub.cs`

### Purpose

Keep the **roll call** display in sync when people are registered / unregistered at the front desk.

### Group model

- Per election: `RollCall{electionGuid}`

### Server API

| Method | Client event |
|--------|----------------|
| `Join(connectionId)` | — |
| `UpdateAllConnectedClients(message)` | `updatePeople` |

### Who joins

| Client | How |
|--------|-----|
| Roll call page | `Views/Before/RollCall.cshtml.js` → `Before/JoinRollCallHub` |

Controller: `BeforeController` → `[AllowTellersInActiveElection]`.

### Client methods

| Method | Behavior |
|--------|----------|
| `updatePeople(info)` | Apply `changed` people, `removedId`, update stamp |

Payload from `PeopleModel.UpdateFrontDeskListing` (when roll-call data changes):

```text
{ changed, removedId, newStamp }
```

### Who sends

- `PeopleModel.UpdateFrontDeskListing` (alongside FrontDesk + VoterPersonal updates)

### Security notes

- Same teller-in-election gate as FrontDesk.
- Election-scoped group.

### Rewrite checklist

- [ ] Live roll-call add/remove as voting methods change  

---

## 5. AllVotersHub

**Files:** `Site/CoreModels/Hubs/AllVotersHub.cs`

### Purpose

Notify **all currently connected online voters** of election-wide online settings changes (open/close window, selection process).

### Group model

- Single global group: `AllVoters`  
- Not partitioned by election — **every** connected voter client receives every update.

### Server API

| Method | Client event |
|--------|----------------|
| `Join(connectionId)` | — |
| `UpdateVoters(message)` | `updateVoters` |

### Who joins

| Client | How |
|--------|-----|
| Voter home | `Views/Vote/VoteHome.cshtml.js` → `Vote/JoinVoterHubs` (also joins VoterPersonalHub) |

Controller: `VoteController` → `[AllowVoter]` (and host must support online elections).

### Client methods

| Method | Behavior |
|--------|----------|
| `updateVoters(info)` | Refresh election list; if `OnlineSelectionProcess` present, update Vue selection process |

Example payloads from `ElectionHelper`:

```text
{ changed, OnlineWhenClose, OnlineWhenOpen, OnlineCloseIsEstimate, OnlineSelectionProcess }
{ OnlineWhenClose, OnlineWhenOpen, OnlineCloseIsEstimate }
```

### Who sends

- `ElectionHelper` when online election settings or close time change

### Security notes

- Join restricted to authenticated voters.
- Broadcast is global: clients must ignore elections they are not in (UI refreshes “my elections” list rather than trusting arbitrary election data).
- **Rewrite consideration:** global group is simple but chatty/leaky; per-election voter groups may be cleaner if voters should only hear about their elections.

### Rewrite checklist

- [ ] Push online open/close / process changes to connected voters  
- [ ] Re-evaluate global vs per-election groups  

---

## 6. VoterPersonalHub

**Files:** `Site/CoreModels/Hubs/VoterPersonalHub.cs`

### Purpose

Person-specific voter updates: registration/voting method changes and “logged in elsewhere” notice.

### Group model

- On join: `Voter{UserSession.VoterId}` where `VoterId` is email, phone, or kiosk code for the logged-in voter.
- On update: groups `Voter{person.Email}` and/or `Voter{person.Phone}` (not kiosk in `Update`).

### Server API

| Method | Client event |
|--------|----------------|
| `Join(connectionId)` | Group = current session voter id |
| `Update(Person person)` | `updateVoter` with registration fields to email/phone groups |
| `Login(string voterId)` | `updateVoter` with `{ login: true }` |

### Who joins

Same as AllVotersHub: `Vote/JoinVoterHubs` under `[AllowVoter]`.

### Client methods (`VoteHome.cshtml.js`)

| Payload | Behavior |
|---------|----------|
| `{ updateRegistration, VotingMethod, RegistrationTime, ElectionGuid }` | Update registration UI + refresh election list |
| `{ login: true }` | Status: logged in on another browser; refresh login history |

### Who sends

| Caller | When |
|--------|------|
| `PeopleModel.UpdateFrontDeskListing` | Front desk changes person registration |
| `ElectionHelper` (process online ballots) | After processing a person’s online ballot |
| `VoterCodeHelper` | After successful code login (`Login(voterId)`) |

### Security notes

- Join uses session `VoterId` only (client cannot pick another voter’s group via join API).
- `Update` addresses groups by person email/phone; only clients who joined with that id receive messages.
- Kiosk identities join as `Voter{kioskCode}` but `Update(person)` currently only notifies email/phone groups.

### Rewrite checklist

- [ ] Per-voter registration push when tellers/process change their status  
- [ ] Multi-device login notification  
- [ ] Decide kiosk identity parity for personal updates  

---

## 7. VoterCodeHub

**Files:** `Site/CoreModels/Hubs/VoterCodeHub.cs`

### Purpose

Live status during **voter code login** (email/SMS/voice): “code sent”, Twilio delivery status, errors. Bridges async delivery to the browser that requested the code.

### Group model

- Group name = client-generated **opaque key** (home page: `Math.random().toString().slice(-5)`).
- Same key is passed to `IssueCode` / `LoginWithCode` so server can push to the right browser.

### Server API

| Method | Client event |
|--------|----------------|
| `Join(connectionId, key)` | Add to group `key` |
| `SetStatus(key, message, twilioStatus?)` | `setStatus` (no-op if key empty) |
| `Final(key, okay, message)` | `final` |

### Who joins

| Client | How |
|--------|-----|
| Public home voter login UI | `Home.cshtml.js` → `Public/VoterCodeHub` with `{ connId, key }` |

### Join endpoint

| Action | Auth |
|--------|------|
| `PublicController.VoterCodeHub(connId, key)` | Public |

### Client methods

| Method | Behavior |
|--------|----------|
| `setStatus(message, sendingStatus)` | Update Vue status; map Twilio/email statuses (`delivered`, `emailSent`, `completed`, etc.) |
| `final(okay, message)` | Final success/fail message |

### Who sends

- `VoterCodeHelper` during issue-code and delivery status handling

### Security notes

- No account required; **security is the unguessability of `key`** (short 5-digit random fragment is relatively weak).
- Knowing another user’s key would allow joining their status channel (status messages only, not codes if codes stay out of hub payloads — verify rewrite keeps codes out of hub messages).
- Prefer longer, server-issued channel tokens in a rewrite.

### Rewrite checklist

- [ ] Real-time code-delivery status for email/SMS/voice login  
- [ ] Stronger channel id than 5-digit random  
- [ ] Keep one-time codes off the wire where possible  

---

## 8. ImportHub

**Files:** `Site/CoreModels/Hubs/ImportHub.cs`  
**Implements:** `IStatusUpdateHub` (defined in `AnalyzeHub.cs`)

### Purpose

Progress UI for **people CSV import** and **election package load** (long-running imports).

### Group model

- Per login: `Import{UserSession.LoginId}`  
- Scopes messages to the teller’s account, not the election.

### Server API

| Method | Client event |
|--------|----------------|
| `Join(connectionId)` | — |
| `ImportInfo(linesProcessed, peopleAdded)` | `importInfo` |
| `StatusUpdate(msg, msgIsTemp)` | `loaderStatus` |

### Who joins

| Client | How |
|--------|-----|
| Import CSV | `Views/Setup/ImportCsv.cshtml.js` → `Elections/JoinImportHub` |
| Elections list (load election) | `Views/Dashboard/ElectionList.cshtml.js` → same hub for `loaderStatus` |

### Join endpoint

| Action | Auth |
|--------|------|
| `ElectionsController.JoinImportHub` | `[ForAuthenticatedTeller]` on action; controller also has `[AllowTellersInActiveElection]` |

### Client methods

| Method | Page | Behavior |
|--------|------|----------|
| `importInfo(lines, people)` | Import CSV | Progress message / results |
| `loaderStatus(msg, isTemp)` | Election list load | Scrollable log; temp vs permanent lines |

### Who sends

| Caller | When |
|--------|------|
| `ImportCsvModel` | CSV import progress |
| `ExportImport/ElectionLoader` | Loading an election package |

### Security notes

- Known teller only to join.
- Group uses `LoginId` so two tellers do not share progress streams.
- **Note:** `BallotImportHub` uses the **same group name pattern** `Import{LoginId}` but a **different hub type** (`BallotImportHubCore` vs `ImportHubCore`). They are separate SignalR hubs; group names do not collide across hub types.

### Rewrite checklist

- [ ] Streaming progress for people import  
- [ ] Streaming progress for election load on dashboard  

---

## 9. BallotImportHub

**Files:** `Site/CoreModels/Hubs/BallotImportHub.cs`  
**Implements:** `IStatusUpdateHub`

### Purpose

Progress log while **importing ballots** (e.g. CDN / external ballot files).

### Group model

- Per login: `Import{UserSession.LoginId}` (same string pattern as ImportHub; different hub)

### Server API

| Method | Client event |
|--------|----------------|
| `Join(connectionId)` | — |
| `StatusUpdate(msg, msgIsTemp)` | `StatusUpdate` (PascalCase in C# → client handler name as registered) |

### Who joins

| Client | How |
|--------|-----|
| Import ballots page | `Views/Setup/ImportBallots.cshtml.js` → `Setup/JoinBallotImportHub` |

### Join endpoint

| Action | Auth |
|--------|------|
| `SetupController.JoinBallotImportHub` | `[ForAuthenticatedTeller]` (+ controller `[AllowTellersInActiveElection]`) |

### Client methods

| Method | Behavior |
|--------|----------|
| `StatusUpdate(msg, isTemp)` | Append to `#log` / `#tempLog` |

### Who sends

- `ImportBallotsModel` during import (and related status lines)

Related side effect (not this hub): after import, `FrontDeskHub.ReloadPage()` so open front desks refresh.

### Security notes

- Known teller only.
- Login-scoped group.

### Rewrite checklist

- [ ] Ballot import progress streaming  
- [ ] Trigger front desk refresh after successful import  

---

## 10. AnalyzeHub

**Files:** `Site/CoreModels/Hubs/AnalyzeHub.cs`  
**Also defines:** `IStatusUpdateHub`

### Purpose

Live **analysis log** while results analysis runs (can take a while on large elections).

### Group model

- Per election: `Analyze{electionGuid}`  
- Requires current election GUID.

### Server API

| Method | Client event |
|--------|----------------|
| `Join(connectionId)` | — |
| `StatusUpdate(msg, msgIsTemp)` | `LoadStatus` |

### Who joins

| Client | How |
|--------|-----|
| Analyze page | `Views/After/Analyze.cshtml.js` → `Elections/JoinAnalyzeHub` |

### Join endpoint

| Action | Auth |
|--------|------|
| `ElectionsController.JoinAnalyzeHub` | `[ForAuthenticatedTeller]` |

### Client methods

| Method | Behavior |
|--------|----------|
| `loadStatus(msg, isTemp)` | Clear log on “Starting Analysis…”; append permanent lines; temp line in `#tempLog` |

### Who sends

Analysis pipeline implementing `IStatusUpdateHub`:

- `ElectionAnalyzerCore` / `ElectionAnalyzerNormal` / `ElectionAnalyzerSingleName`
- Messages such as: starting analysis, reviewing votes/ballots, processing counts, ties, saving, etc.

Tests use `FakeHub : IStatusUpdateHub` (`Tests/BusinessTests/AnalyzerFakes.cs`).

### Security notes

- Known teller only to join.
- Election-scoped group (all known tellers analyzing the same election share the log stream).

### Rewrite checklist

- [ ] Streaming analysis progress log  
- [ ] Keep `IStatusUpdateHub` (or equivalent) injectable for tests  

---

## Shared interface: `IStatusUpdateHub`

Defined in `AnalyzeHub.cs`:

```csharp
public interface IStatusUpdateHub {
  void StatusUpdate(string msg, bool msgIsTemp = false);
}
```

Implemented by:

- `AnalyzeHub` → client `LoadStatus`
- `ImportHub` → client `LoaderStatus`
- `BallotImportHub` → client `StatusUpdate`

Same server abstraction, different client event names — preserve mapping if reusing one progress abstraction in v4.

---

## Connection lifecycle (rewrite guidance)

```text
Browser page load
  → startSignalR()  // shared connection
  → AJAX Join* (connId [+ key/list])
  → Groups.Add(connectionId, groupName)

Server domain event
  → new XxxHub().SomePush(...)
  → Clients.Group(groupName).clientMethod(payload)

Logout / election closed (guests)
  → logoffSignalR / electionClosed → disconnect / redirect
```

Reconnect behavior: `site.js` shows user-facing messages on disconnect / slow connection / inactivity; join is re-invoked only when pages re-run their connect helpers after a full reconnect path.

---

## Security summary for the rewrite

| Principle | v3 behavior | Recommendation for v4 |
|-----------|-------------|------------------------|
| Hub class auth | None on `*HubCore` | Prefer hub `[Authorize]` + policies **or** keep join-via-API + server-only groups |
| Isolation | SignalR groups | Keep group-based isolation; prefer server-derived group names only |
| Election scope | Most teller hubs use `CurrentElectionGuid` | Bind groups to authorized election membership |
| Guest vs known | MainHub split | Preserve guest kick without disconnecting known tellers |
| Public data | PublicHub only | Never put private person/ballot data on public groups |
| Voter channels | Session voter id | Never allow client-supplied voter id for personal groups |
| Login codes | Client random key | Use longer server-issued channel tokens |
| Progress hubs | Per `LoginId` | Keep user-scoped so concurrent tellers do not share logs |

---

## File map

| Path | Notes |
|------|--------|
| `Site/CoreModels/Hubs/*.cs` | All hub helpers + core classes |
| `Site/OwinStartup.cs` | `app.MapSignalR()` |
| `Site/Scripts/site.js` | Connection helpers, MainHub clients |
| `Site/Scripts/jquery.signalR-2.4.3*.js` | SignalR client library |
| `Site/Controllers/PublicController.cs` | Public, VoterCode, Main joins |
| `Site/Controllers/BeforeController.cs` | FrontDesk, RollCall joins |
| `Site/Controllers/VoteController.cs` | AllVoters + VoterPersonal join |
| `Site/Controllers/ElectionsController.cs` | Import, Analyze joins |
| `Site/Controllers/SetupController.cs` | BallotImport join |
| `Site/CoreModels/ElectionHelper.cs` | Many broadcasts (Main, Public, AllVoters, FrontDesk, VoterPersonal) |
| `Site/CoreModels/PeopleModel.cs` | FrontDesk, RollCall, VoterPersonal on person changes |
| `Site/CoreModels/Helper/VoterCodeHelper.cs` | VoterCode + VoterPersonal.Login |
| `Site/CoreModels/ImportCsvModel.cs` / `ImportBallotsModel.cs` / `ExportImport/ElectionLoader.cs` | Import progress |
| `Site/CoreModels/ElectionAnalyzer*.cs` | Analyze progress |

---

## Minimal functional matrix (must not lose in rewrite)

| Capability | Hub(s) |
|------------|--------|
| Teller UI sees live election status (open/listed/passcode/tally state) | MainHub |
| Guest tellers forced out when election closed/unlisted | MainHub |
| Public landing page election list stays current | PublicHub |
| Front desk / ballot screens update when someone is registered | FrontDeskHub |
| Roll call display tracks registrations | RollCallHub |
| Monitor shows online window changes | FrontDeskHub |
| Voters see online open/close / process changes | AllVotersHub |
| Voter sees their own registration / multi-login | VoterPersonalHub |
| Voter login code delivery status | VoterCodeHub |
| CSV / election-load progress | ImportHub |
| Ballot import progress + front desk reload | BallotImportHub (+ FrontDeskHub) |
| Results analysis progress log | AnalyzeHub |

---

*Generated from TallyJ v3 source for rewrite reference. Update this document if hub behavior changes before v4 ships.*
