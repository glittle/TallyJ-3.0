# Technical Specification: GreenAPI WhatsApp Integration

## Task Complexity Assessment
**Medium Complexity** (Updated: Core implementation complete, remaining work is moderate)

The task requires implementing a new messaging service integration following established patterns in the codebase. While it involves multiple files and integration points, the architecture is well-defined with clear examples (TwilioHelper) to follow. The main complexity comes from ensuring proper integration with existing voter authentication flows and notification systems.

## Implementation Status

### ✅ Completed
- **[./Site/CoreModels/Helper/WhatsAppHelper.cs](./Site/CoreModels/Helper/WhatsAppHelper.cs)**: Full GreenAPI integration
  - SendVerifyCodeToVoter for voter login
  - SendHeadTellerMessage for bulk notifications
  - SendWhatsAppMessage core API integration
  - Phone number formatting for GreenAPI
  - Error handling and logging
- **[./Site/CoreModels/Helper/VoterCodeHelper.cs](./Site/CoreModels/Helper/VoterCodeHelper.cs)**: WhatsApp routing (line 371-374)
- **[./Site/Controllers/SetupController.cs](./Site/Controllers/SetupController.cs)**: SendWhatsApp endpoint (line 292-295)
- **[./Site/Code/SettingsHelper.cs](./Site/Code/SettingsHelper.cs)**: GreenAPI configuration properties (lines 19-21)
- **[./Site/web.config](./Site/web.config)**: GreenAPI settings (lines 64-66)

### 🔄 Remaining Work
1. **CheckWhatsApp API integration**: Detect which phone numbers have WhatsApp
2. **Person WhatsApp tracking**: Add HasWhatsApp to Person extra settings
3. **OnlineVoter tracking**: Track WhatsApp registration method in OtherInfo
4. **Notify page updates**: Add WhatsApp detection and sending option
5. **Testing and verification**: Manual and automated testing

---

## Technical Context

### Technology Stack
- **Language**: C# (.NET Framework 4.8)
- **Framework**: ASP.NET MVC 5
- **ORM**: Entity Framework 6.5
- **HTTP Client**: System.Net.Http (built-in)
- **Existing Dependencies**: 
  - Twilio SDK (current SMS/WhatsApp provider)
  - Newtonsoft.Json for JSON serialization
  - SignalR for real-time updates

### Current Architecture
The application currently uses Twilio for all messaging (SMS, WhatsApp, Voice) through:
- **[./Site/CoreModels/Helper/TwilioHelper.cs](./Site/CoreModels/Helper/TwilioHelper.cs)**: Handles Twilio API integration
- **[./Site/CoreModels/Helper/VoterCodeHelper.cs](./Site/CoreModels/Helper/VoterCodeHelper.cs)**: Manages voter login code generation and delivery
- **[./Site/CoreModels/Helper/WhatsAppHelper.cs](./Site/CoreModels/Helper/WhatsAppHelper.cs)**: Empty stub class (ready for implementation)
- **[./Site/CoreModels/Helper/MessageHelperBase.cs](./Site/CoreModels/Helper/MessageHelperBase.cs)**: Base class for messaging helpers
- **[./Site/Code/SettingsHelper.cs](./Site/Code/SettingsHelper.cs)**: Configuration management

### Database Models
- **SmsLog**: Tracks message delivery status (SmsSid, Phone, SentDate, LastStatus, ErrorCode, ElectionGuid, PersonGuid)
- **OnlineVoter**: Stores voter authentication information (VoterId, VoterIdType, VerifyCode, etc.)
- **Person**: Contains voter contact information (Phone, Email, PersonGuid, ElectionGuid)
- **Election**: Election settings including message templates (SmsText)

### GreenAPI Overview
GreenAPI provides WhatsApp messaging via REST API without requiring the Twilio infrastructure.

**Key API Details:**
- **Base URL**: `https://api.green-api.com`
- **Send Message Endpoint**: `POST /waInstance{idInstance}/sendMessage/{apiTokenInstance}`
- **Request Format**: `{"chatId": "phonenumber@c.us", "message": "text"}`
- **Response Format**: `{"idMessage": "messageId"}`
- **Authentication**: Via URL parameters (idInstance, apiTokenInstance)
- **Phone Format**: International format without '+' sign, suffixed with `@c.us` (e.g., "12025551234@c.us")

---

## Implementation Approach

### Architecture Decision
Implement GreenAPI as a **replacement for Twilio WhatsApp** functionality. The system will use:
- **Twilio**: For SMS and Voice
- **GreenAPI**: For WhatsApp messages only

This approach:
1. Follows the existing pattern (helper classes extending MessageHelperBase)
2. Allows gradual migration from Twilio WhatsApp to GreenAPI
3. Maintains existing SMS/Voice functionality
4. Reuses existing infrastructure (logging, error handling, UI)

### Configuration Strategy
Add new settings to [./Site/Code/SettingsHelper.cs](./Site/Code/SettingsHelper.cs) and web.config:
- `greenapi-IdInstance`: GreenAPI instance ID
- `greenapi-ApiTokenInstance`: GreenAPI API token
- `greenapi-ApiUrl`: GreenAPI base URL (default: "https://api.green-api.com")
- Keep existing `HostSupportsOnlineWhatsAppLogin` setting (line 18) to enable/disable WhatsApp feature

### Integration Points

#### 1. Voter Login Flow (Primary Use Case)
**File**: [./Site/CoreModels/Helper/VoterCodeHelper.cs](./Site/CoreModels/Helper/VoterCodeHelper.cs)

**Current Flow** (line 128-136):
```
User selects "WhatsApp" → IssueCode(type="phone", method="whatsapp", target=phoneNumber)
→ SendViaTwilio() → TwilioHelper.SendVerifyCodeToVoter()
```

**New Flow**:
```
User selects "WhatsApp" → IssueCode(type="phone", method="whatsapp", target=phoneNumber)
→ SendViaGreenApi() → GreenApiHelper.SendVerifyCodeToVoter()
```

**Changes Required**:
- Modify `VoterCodeHelper.SendViaTwilio()` to detect "whatsapp" method and route to GreenAPI
- OR create new `VoterCodeHelper.SendViaWhatsApp()` method that uses GreenAPI

#### 2. Bulk Notification Flow (Secondary Use Case)
**File**: [./Site/CoreModels/Helper/TwilioHelper.cs](./Site/CoreModels/Helper/TwilioHelper.cs:127)

**Current Flow**:
```
Head Teller → Notify Page → SendSms() → TwilioHelper.SendHeadTellerMessage()
→ Loops through selected voters → SendSmsAsync(method="sms")
```

**New Flow**:
```
Head Teller → Notify Page → SendSms() → Detect method
→ TwilioHelper for SMS OR GreenApiHelper for WhatsApp
→ Loops through selected voters
```

**Changes Required**:
- Add WhatsApp option to Notify page UI
- Create `GreenApiHelper.SendHeadTellerMessage()` similar to Twilio implementation
- Update controller to route based on selected method

---

## Source Code Structure Changes

### New Files to Create

#### 1. [./Site/CoreModels/Helper/GreenApiHelper.cs](./Site/CoreModels/Helper/GreenApiHelper.cs)
**Purpose**: Main GreenAPI integration service

**Key Methods**:
```csharp
public class GreenApiHelper : MessageHelperBase
{
    // Send verification code for voter login
    public bool SendVerifyCodeToVoter(string phone, string code, string hubKey, 
                                       Guid electionGuid, Guid personGuid, out string error);
    
    // Send bulk messages from Notify page
    public JsonResult SendHeadTellerMessage(string idList);
    
    // Core message sending logic
    private bool SendWhatsAppMessage(string phoneNumber, string messageText, 
                                      Guid personGuid, out string errorMessage, 
                                      Guid openElectionGuid);
    
    // Phone number formatting
    private string FormatPhoneNumberForGreenApi(string phoneNumber);
    
    // Status checking (if needed)
    public string GetMessageStatus(string messageId);
}
```

**Dependencies**:
- System.Net.Http.HttpClient for REST API calls
- Newtonsoft.Json for JSON serialization
- SettingsHelper for configuration
- Existing database context for logging

### Files to Modify

#### 2. [./Site/CoreModels/Helper/VoterCodeHelper.cs](./Site/CoreModels/Helper/VoterCodeHelper.cs:359)
**Changes**:
- Line 359-383: Modify `SendViaTwilio()` method or add `SendViaWhatsApp()` method
- Route "whatsapp" method to GreenApiHelper instead of TwilioHelper
- Maintain existing error handling and status monitoring patterns

**Approach**: 
```csharp
private bool SendViaWhatsApp(string phoneNumber, string newCode, 
                              Guid openElectionGuid, Guid personGuid, out string message)
{
    var greenApiHelper = new GreenApiHelper();
    var sent = greenApiHelper.SendVerifyCodeToVoter(phoneNumber, newCode, _hubKey, 
                                                      openElectionGuid, personGuid, out message);
    return sent;
}

// Update SendViaTwilio to exclude whatsapp:
private bool SendViaTwilio(string phoneNumber, string method, string newCode, ...)
{
    // ... existing code ...
    switch (method)
    {
        case "sms":
            // existing SMS code
            break;
        case "whatsapp":
            return SendViaWhatsApp(phoneNumber, newCode, openElectionGuid, personGuid, out message);
        case "voice":
            // existing voice code
            break;
    }
}
```

#### 3. [./Site/Code/SettingsHelper.cs](./Site/Code/SettingsHelper.cs)
**Changes**:
- Add GreenAPI configuration property accessors
- Keep existing WhatsApp feature flag (line 18: `HostSupportsOnlineWhatsAppLogin`)

```csharp
// Add after line 18:
public static string GreenApiIdInstance => Get("greenapi-IdInstance", "");
public static string GreenApiTokenInstance => Get("greenapi-ApiTokenInstance", "");
public static string GreenApiUrl => Get("greenapi-ApiUrl", "https://api.green-api.com");
```

#### 4. [./Site/Controllers/SetupController.cs](./Site/Controllers/SetupController.cs:286)
**Changes**:
- Line 286-289: `SendSms()` method - add support for WhatsApp via GreenAPI
- Add new `SendWhatsApp()` endpoint OR modify existing to accept method parameter

```csharp
[ForAuthenticatedTeller]
public JsonResult SendWhatsApp(string list)
{
    return new GreenApiHelper().SendHeadTellerMessage(list);
}
```

#### 5. [./Site/Views/Setup/Notify.cshtml](./Site/Views/Setup/Notify.cshtml)
**Changes**:
- Add UI option to send via WhatsApp (in addition to Email/SMS)
- Note: User mentioned "I don't expect any changes to the front end" but this may need clarification
- **Decision needed**: Can we determine WhatsApp availability automatically, or does user select "Send via WhatsApp"?

#### 6. [./Site/web.config](./Site/web.config)
**Changes**:
- Add GreenAPI configuration settings in `<appSettings>` section

```xml
<add key="greenapi-IdInstance" value="" />
<add key="greenapi-ApiTokenInstance" value="" />
<add key="greenapi-ApiUrl" value="https://api.green-api.com" />
```

### Files to Consider (Optional)

#### 7. Database Schema
**Current**: [./Site/EF/SmsLog.cs](./Site/EF/SmsLog.cs) table tracks all SMS/WhatsApp messages

**Decision**: Reuse existing `SmsLog` table for GreenAPI messages
- `SmsSid` field will store GreenAPI `idMessage` response
- `LastStatus` will store GreenAPI-specific statuses
- **Alternative**: Create separate `WhatsAppLog` table if more GreenAPI-specific fields needed

**No database changes required initially** - existing schema is sufficient.

---

## Data Model / API / Interface Changes

### Configuration Schema
New web.config appSettings:
```xml
<appSettings>
  <!-- Existing settings... -->
  <add key="greenapi-IdInstance" value="1101234567" />
  <add key="greenapi-ApiTokenInstance" value="abc123def456..." />
  <add key="greenapi-ApiUrl" value="https://api.green-api.com" />
</appSettings>
```

### API Request/Response Models

#### Send Message Request
```csharp
public class GreenApiSendMessageRequest
{
    public string chatId { get; set; }      // "12025551234@c.us"
    public string message { get; set; }      // Message text
}
```

#### Send Message Response
```csharp
public class GreenApiSendMessageResponse
{
    public string idMessage { get; set; }    // Message ID for tracking
}
```

#### Error Response
```csharp
public class GreenApiErrorResponse
{
    public string error { get; set; }
    public int code { get; set; }
}
```

### Phone Number Format Conversion
**Input**: `+1234567890` or `1234567890` (from database)
**Output**: `1234567890@c.us` (for GreenAPI)

Conversion logic:
1. Remove '+' prefix if present
2. Remove any non-digit characters
3. Append `@c.us` suffix

### Logging Strategy
Reuse existing `SmsLog` table with these mappings:
- `SmsSid` → GreenAPI `idMessage`
- `Phone` → Phone number (without @c.us suffix for consistency)
- `SentDate` → UTC timestamp
- `LastStatus` → "submitted", "sent", "delivered", "failed"
- `ErrorCode` → GreenAPI error codes (if provided)
- `ElectionGuid`, `PersonGuid` → Same as existing

---

## Verification Approach

### Unit Testing Strategy
The project uses the Tests project at [./Tests](./Tests).

**Test Files to Create**:
1. `Tests/BusinessTests/GreenApiHelperTests.cs`
   - Test phone number formatting
   - Test message sending logic (with mocked HTTP client)
   - Test error handling scenarios
   - Test configuration validation

**Test Scenarios**:
```csharp
[TestMethod]
public void FormatPhoneNumber_RemovesPlusAndAddsAtCus()
{
    // Test: "+1234567890" → "1234567890@c.us"
}

[TestMethod]
public void SendMessage_InvalidConfig_ReturnsError()
{
    // Test: Missing IdInstance returns appropriate error
}

[TestMethod]
public void SendMessage_ValidRequest_LogsToDatabase()
{
    // Test: Successful send creates SmsLog entry
}
```

### Manual Testing Checklist

#### Voter Login Flow
1. **Setup**: Configure GreenAPI credentials in web.config
2. **Enable**: Set `SupportOnlineWhatsAppLogin = true`
3. **Test Steps**:
   - Navigate to voter login page
   - Select WhatsApp login method
   - Enter phone number (international format)
   - Verify code is sent via GreenAPI (check logs)
   - Receive code in WhatsApp
   - Enter code and complete login
   - Verify logged in as WhatsApp user

#### Bulk Notification Flow  
1. **Setup**: Election with voters having phone numbers
2. **Test Steps**:
   - Navigate to Setup → Notify page
   - Select voters to notify
   - Choose WhatsApp as method
   - Send test message
   - Verify messages sent to all selected voters
   - Check SmsLog for delivery status

#### Error Scenarios
1. Missing/invalid GreenAPI credentials → User-friendly error
2. Invalid phone number format → Validation error
3. GreenAPI service unavailable → Graceful degradation
4. Non-WhatsApp phone number → Appropriate error message

### Integration Testing
1. **Configuration Validation**: 
   - Test with missing credentials
   - Test with invalid credentials
   - Test with valid credentials

2. **Message Delivery**:
   - Send to single recipient
   - Send to multiple recipients
   - Send with special characters in message
   - Send with emoji in message

3. **Status Tracking**:
   - Verify SmsLog entries created
   - Check status updates (if webhook implemented)

### Test Commands
**Note**: Need to identify test runner commands from project structure.
- Standard ASP.NET: `MSTest.exe` or Visual Studio Test Explorer
- Check [./Tests/Tests.csproj](./Tests/Tests.csproj) for test framework

**Lint/Typecheck Commands**: 
- C# projects typically use:
  - Visual Studio Code Analysis
  - ReSharper (if installed)
  - FxCop / Roslyn analyzers
- Check project settings for enabled analyzers

---

## Design Decisions (Confirmed)

### ✅ 1. Frontend Changes for Notify Page
**Decision**: Reuse and adjust existing Home.cshtml WhatsApp UI code (line 296-299). Add WhatsApp option to Notify page.

### ✅ 2. Method Detection Strategy  
**Decision**: Use GreenAPI's CheckWhatsApp API method to detect WhatsApp availability before sending bulk messages.

### ✅ 3. Voter Registration Tracking
**Decision**: 
- Track in `OnlineVoter.OtherInfo` JSON field for voter login
- Track in `Person` extra settings (similar to RegistrationLog pattern) for bulk notifications

### ✅ 4. Message Template Reuse
**Decision**: Reuse existing SMS templates (`Election.SmsText`) for WhatsApp messages.

### ✅ 5. Status Webhook Implementation
**Decision**: Start without webhooks. Can add later if needed.

---

## Implementation Phases

### ✅ Phase 1: Core GreenAPI Integration (COMPLETE)
1. ✅ Create `WhatsAppHelper` class with message sending
2. ✅ Add configuration settings
3. ✅ Implement phone number formatting
4. ✅ Add basic error handling
5. ✅ Integrate with `VoterCodeHelper` for login flow
6. ✅ Implement SmsLog integration
7. ✅ Add message status tracking
8. ✅ Implement `SendHeadTellerMessage` for bulk notifications
9. ✅ Add controller endpoint

### 🔄 Phase 2: WhatsApp Detection & Tracking (IN PROGRESS)
1. Add CheckWhatsApp method to WhatsAppHelper
2. Add HasWhatsApp property to Person extra settings
3. Update OnlineVoter to track WhatsApp registration in OtherInfo JSON
4. Add bulk WhatsApp detection method for Notify page

### 🔄 Phase 3: Frontend Integration (PENDING)
1. Update Notify.cshtml to add WhatsApp send option
2. Update Notify.cshtml.js to call CheckWhatsApp before sending
3. Add UI to show which contacts have WhatsApp
4. Test voter login UI (already has WhatsApp button)

### 🔄 Phase 4: Testing & Verification (PENDING)
1. Manual testing of voter login via WhatsApp
2. Manual testing of bulk WhatsApp notifications
3. Test WhatsApp detection on Notify page
4. Verify logging and error handling
5. Configuration validation testing

---

## Risk Assessment

### Technical Risks
1. **GreenAPI Rate Limits**: Unknown rate limiting behavior
   - **Mitigation**: Implement delay between messages, monitor API responses
   
2. **Phone Number Format Variations**: Different countries may have different formats
   - **Mitigation**: Robust formatting logic, validation, error handling

3. **Configuration Errors**: Missing or invalid GreenAPI credentials
   - **Mitigation**: Configuration validation on startup, clear error messages

### Functional Risks
1. **WhatsApp Account Detection**: Can't reliably detect if phone has WhatsApp
   - **Mitigation**: Only send to users who registered via WhatsApp

2. **Message Delivery Failures**: No real-time status updates without webhooks
   - **Mitigation**: Log all attempts, provide feedback to users

### Security Risks
1. **Credential Exposure**: GreenAPI tokens in web.config
   - **Mitigation**: Use encrypted configuration, environment variables in production

2. **Message Logging**: Phone numbers and messages stored in database
   - **Mitigation**: Follow existing patterns, ensure proper access controls

---

## Success Criteria

### Must Have
- ✅ Voters can log in via WhatsApp (receive verification codes)
- ✅ Messages sent via GreenAPI are logged in SmsLog
- ✅ Configuration properly validates credentials
- ✅ Errors are handled gracefully with user-friendly messages
- ✅ Existing SMS/Email functionality remains unchanged

### Should Have
- ✅ Bulk notifications can be sent via WhatsApp (if UI changes acceptable)
- ✅ Unit tests cover core functionality
- ✅ Phone number formatting handles international formats

### Nice to Have
- Status webhooks for delivery confirmation
- WhatsApp account detection before sending
- Separate message templates for WhatsApp

---

## Estimated Effort

- **Phase 1 (Core Integration)**: 6-8 hours
- **Phase 2 (Logging/Tracking)**: 2-3 hours  
- **Phase 3 (Bulk Notifications)**: 3-4 hours (if needed)
- **Phase 4 (Testing/Polish)**: 3-4 hours
- **Total**: 14-19 hours

**Complexity Factors**:
- Clear existing patterns to follow (+)
- Well-documented GreenAPI (+)
- Need clarification on frontend changes (-)
- International phone number handling (-)
- Testing in production environment required (-)
