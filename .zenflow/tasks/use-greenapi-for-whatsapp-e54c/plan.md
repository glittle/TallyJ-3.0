# Spec and build

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Agent Instructions

Ask the user questions when anything is unclear or needs their input. This includes:
- Ambiguous or incomplete requirements
- Technical decisions that affect architecture or user experience
- Trade-offs that require business context

Do not make assumptions on important decisions — get clarification first.

---

## Workflow Steps

### [x] Step: Technical Specification
<!-- chat-id: 44965da7-7c32-4367-9621-341e2bb1a1dd -->

Assess the task's difficulty, as underestimating it leads to poor outcomes.
- easy: Straightforward implementation, trivial bug fix or feature
- medium: Moderate complexity, some edge cases or caveats to consider
- hard: Complex logic, many caveats, architectural considerations, or high-risk changes

Create a technical specification for the task that is appropriate for the complexity level:
- Review the existing codebase architecture and identify reusable components.
- Define the implementation approach based on established patterns in the project.
- Identify all source code files that will be created or modified.
- Define any necessary data model, API, or interface changes.
- Describe verification steps using the project's test and lint commands.

Save the output to `{@artifacts_path}/spec.md` with:
- Technical context (language, dependencies)
- Implementation approach
- Source code structure changes
- Data model / API / interface changes
- Verification approach

If the task is complex enough, create a detailed implementation plan based on `{@artifacts_path}/spec.md`:
- Break down the work into concrete tasks (incrementable, testable milestones)
- Each task should reference relevant contracts and include verification steps
- Replace the Implementation step below with the planned tasks

Rule of thumb for step size: each step should represent a coherent unit of work (e.g., implement a component, add an API endpoint, write tests for a module). Avoid steps that are too granular (single function).

Important: unit tests must be part of each implementation task, not separate tasks. Each task should implement the code and its tests together, if relevant.

Save to `{@artifacts_path}/plan.md`. If the feature is trivial and doesn't warrant this breakdown, keep the Implementation step below as is.

---

### [x] Step: Core Implementation
<!-- chat-id: a4bba515-643c-42bb-8571-624a7cb5af43 -->

**Status:** COMPLETE

Core GreenAPI WhatsApp integration implemented:
- ✅ WhatsAppHelper with GreenAPI SendMessage API
- ✅ VoterCodeHelper routing for WhatsApp login
- ✅ SetupController SendWhatsApp endpoint
- ✅ Configuration settings
- ✅ Message logging to SmsLog

---

### [x] Step: Add WhatsApp Detection API
<!-- chat-id: 76c23ae2-90a0-41ac-a661-7d99208220c9 -->

Implement GreenAPI CheckWhatsApp functionality in WhatsAppHelper.

**Changes:**
- Add `CheckWhatsApp(string phoneNumber)` method to WhatsAppHelper
- Add `CheckMultipleWhatsApp(List<string> phoneNumbers)` bulk check method  
- Handle API errors gracefully

**Verification:**
- Test with valid WhatsApp numbers
- Test with non-WhatsApp numbers

---

### [x] Step: Add WhatsApp Tracking to Person
<!-- chat-id: 9a69af7d-5989-4444-9322-8eb1ffb2ce83 -->

Add HasWhatsApp extra setting to Person entity.

**Changes:**
- Update [./Site/EF/Partials/Person.cs](./Site/EF/Partials/Person.cs) ExtraSettingKey enum to include `HasWA` key
- Add `HasWhatsApp` property using extra settings pattern (like RegistrationLog)

**Verification:**
- Test setting and retrieving HasWhatsApp value
- Verify persistence to CombinedSoundCodes column

---

### [x] Step: Track WhatsApp Registration in OnlineVoter  
<!-- chat-id: 6c47fed8-5692-493e-92e1-2570acdc1d55 -->

Update OnlineVoter OtherInfo to track WhatsApp usage.

**Changes:**
- Extend OnlineVoterOtherInfo class to include `UsedWhatsApp` bool
- Update VoterCodeHelper.CreateOrUpdateOnlineVoter to set UsedWhatsApp=true when method="whatsapp"

**Verification:**
- Login via WhatsApp
- Verify OtherInfo contains UsedWhatsApp flag

---

### [x] Step: Update Notify Page for WhatsApp
<!-- chat-id: e74d5054-93e7-4aa5-aee5-9d74bb652152 -->

Add WhatsApp detection and sending UI.

**Changes:**
- Add `CheckWhatsAppForContacts(string idList)` to SetupController
- Update [./Site/Views/Setup/Notify.cshtml](./Site/Views/Setup/Notify.cshtml) - add UI
- Update Notify.cshtml.js - add detection and send logic

**Verification:**
- Check for WhatsApp on selected contacts
- Send messages via WhatsApp

---

### [x] Step: Testing and Final Report
<!-- chat-id: dce63fed-ec4a-4638-9672-d413176bbd01 -->

**Status:** COMPLETE

Comprehensive testing documentation and final report completed.

**Completed:**
- ✅ Documented all implemented features (9 components)
- ✅ Code quality verification checklist
- ✅ Manual testing procedures (5 test scenarios)
- ✅ Known limitations documented
- ✅ Configuration requirements specified
- ✅ Deployment checklist provided

**Report:** See `{@artifacts_path}/report.md` for:
  - Complete feature list with file references
  - Testing summary and verification steps
  - Manual testing checklist
  - Configuration requirements
  - Known limitations
  - Deployment steps
