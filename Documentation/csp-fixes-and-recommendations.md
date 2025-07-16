# Content Security Policy (CSP) Fixes and Recommendations

## Critical Security Fix: Removed 'unsafe-inline' for Scripts

### 🔒 **DAST Security Issue Fixed**: CSP now blocks inline JavaScript

**Previous Vulnerability**: The CSP configuration allowed `'unsafe-inline'` in `script-src`, which permitted arbitrary inline JavaScript execution and created a significant security risk.

**Fix Applied** (July 2025):
1. **Removed `'unsafe-inline'` from all script-src directives**:
   - `appsettings.json`
   - `appsettings.Production.json` 
   - `SecurityTestController.cs`

2. **Implemented nonce-based CSP**:
   - Created `ScriptNonceTagHelper` for automatic nonce addition
   - Added `Html.GetScriptNonce()` extension method
   - Updated `ContentSecurityPolicyMiddleware.cs` to generate unique nonces per request

3. **Updated critical inline scripts**:
   - Google Analytics initialization script in `_Layout.cshtml`
   - jQuery fallback script in `_Layout.cshtml`
   - Charts initialization in `Reports/Index.cshtml`

### How to Add Nonces to New Inline Scripts

**Method 1: Using Extension Method (Recommended)**
```html
<script nonce="@Html.GetScriptNonce()">
    // Your JavaScript code here
    console.log('This script uses CSP nonce');
</script>
```

**Method 2: Using TagHelper**
```html
<script add-nonce="true">
    // Your JavaScript code here
    console.log('This script uses CSP nonce via TagHelper');
</script>
```

**Method 3: For developers - direct access**
```html
<script nonce="@Context.Items["CSP-Script-Nonce"]">
    // Your JavaScript code here
</script>
```

### For Existing Inline Scripts

**⚠️ Action Required**: All remaining inline scripts in the application need to be updated with nonces. Search for:
- `<script>` tags without nonce attributes
- `onclick=`, `onload=`, and other inline event handlers
- `javascript:` URLs in href attributes

**Find inline scripts**: Use this command:
```bash
grep -r "<script>" --include="*.cshtml" Pages/ Views/ Areas/
grep -r "onclick=" --include="*.cshtml" Pages/ Views/ Areas/
```

## Current CSP Configuration (Secure)

**Script Sources** (no more 'unsafe-inline'):
```csp
script-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://static.cloudflareinsights.com https://challenges.cloudflare.com https://www.googletagmanager.com https://www.google-analytics.com https://googletagmanager.com https://ssl.google-analytics.com https://tagmanager.google.com 'nonce-{GENERATED_NONCE}'
```

**Complete CSP Header**:
```csp
default-src 'self';
script-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://static.cloudflareinsights.com https://challenges.cloudflare.com https://www.googletagmanager.com https://www.google-analytics.com 'nonce-{GENERATED_NONCE}';
style-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://fonts.googleapis.com 'nonce-{GENERATED_NONCE}';
img-src 'self' data: blob: https://cdn.jsdelivr.net https://challenges.cloudflare.com https://avatars.githubusercontent.com https://app.aikido.dev https://www.google-analytics.com https://ssl.google-analytics.com https://stats.g.doubleclick.net;
font-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.gstatic.com data:;
connect-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://www.google-analytics.com https://analytics.google.com https://region1.google-analytics.com [allowed domains] https://challenges.cloudflare.com [websocket urls];
frame-src 'self' https://challenges.cloudflare.com https://www.youtube.com;
frame-ancestors 'self' [allowed domains];
form-action 'self' [allowed domains] https://challenges.cloudflare.com;
base-uri 'self';
object-src 'none';
media-src 'self' https://cdn.jsdelivr.net;
worker-src 'self' blob:;
manifest-src 'self';
upgrade-insecure-requests
```

## Recent Security Improvements

### Removed 'unsafe-eval' from script-src (Security Fix)

**Issue**: The CSP policy previously included `'unsafe-eval'` in the `script-src` directive, which allows the use of `eval()` and `new Function()`. This creates a security vulnerability as it can enable code injection attacks.

**Fix Applied**: 
- Removed `'unsafe-eval'` from the CSP `script-src` directive
- Updated all configuration files and documentation to reflect this change
- Tested application functionality to ensure no critical features are broken

**Impact**: 
- ✅ Blocks potential code injection via `eval()` function calls
- ✅ Improves security scan compliance (DAST scan should now pass)
- ⚠️ Some legacy libraries may need updates if they rely on `eval()`

**Libraries Verified**:
- jQuery validation unobtrusive: Uses `new Function()` for regex patterns but functionality maintained
- SignalR: Uses `new Function()` for global object detection but has fallbacks
- DataTables, Chart.js, Bootstrap: Do not require `eval()`

If any JavaScript functionality is broken after this change, consider:
1. Updating the library to a newer version that doesn't use `eval()`
2. Implementing nonce-based CSP for specific scripts
3. Using alternative libraries that don't require `eval()`

## Remaining Issues and Recommendations

### High Priority

1. **Remove 'unsafe-inline' from script-src**
   - **Current State**: Temporarily allowed in production due to existing inline handlers
   - **Action Required**: Refactor all inline event handlers to use `addEventListener`
   - **Files with inline handlers**:
     - `/Pages/Reports/Index.cshtml` - `onchange="this.form.submit()"`
     - `/Pages/BackgroundJobs/JobHistory.cshtml` - Multiple `onclick` handlers
     - `/Pages/Help/Article.cshtml` - Multiple function calls
     - `/Pages/Account/Manage/ShareTokens.cshtml` - Confirmation dialogs
     - `/Pages/Workouts/QuickWorkout.cshtml` - Exercise selection
     - And many more (see grep results)

2. **Implement Nonce-Based CSP**
   - Generate unique nonces for each request
   - Replace `'unsafe-inline'` with nonce values
   - Update middleware to inject nonces into script and style tags

### Medium Priority

3. **Add CSP Violation Reporting**
   - Configure `report-uri` directive
   - Implement `/api/CspReport/violations` endpoint
   - Monitor and analyze violations

4. **Strengthen Image Sources**
   - Review and minimize external image sources
   - Consider using a proxy for external images
   - Add specific hashes for inline styles if needed

### Low Priority

5. **Implement CSP Level 3 Features**
   - Consider using `'strict-dynamic'` directive
   - Implement script hash validation
   - Add `'unsafe-hashes'` for specific inline event handlers if needed

## Implementation Steps

### Phase 1: Remove Inline Event Handlers
1. Create dedicated JavaScript modules for each page type
2. Replace `onclick`, `onchange`, `onsubmit` with event listeners
3. Use data attributes to pass configuration to JavaScript
4. Test thoroughly on all affected pages

### Phase 2: Implement Nonce System
1. Generate unique nonces per request in middleware
2. Add nonce to ViewData for use in Razor pages
3. Update script and style tags to use nonces
4. Remove `'unsafe-inline'` from CSP

### Phase 3: Add Violation Reporting
1. Implement CSP violation endpoint
2. Add violation logging and monitoring
3. Set up alerts for unusual violation patterns

## Example Refactoring

### Before (Inline Handler)
```html
<button onclick="return confirm('Are you sure?');">Delete</button>
```

### After (Event Listener)
```html
<button class="confirm-delete" data-message="Are you sure?">Delete</button>

<script>
document.addEventListener('DOMContentLoaded', function() {
    document.querySelectorAll('.confirm-delete').forEach(button => {
        button.addEventListener('click', function(e) {
            const message = this.dataset.message || 'Are you sure?';
            if (!confirm(message)) {
                e.preventDefault();
                return false;
            }
        });
    });
});
</script>
```

## Testing

After implementing changes, test the CSP using:

1. **Browser DevTools**: Check for CSP violations in console
2. **Test Endpoints**: 
   - `/api/securitytest/csp-test`
   - `/api/securitytest/cloudflare-csp-check`
3. **External Scanners**: Re-run security scans to verify improvements

## Files Modified

- `Middleware/ContentSecurityPolicyMiddleware.cs` - Added Aikido domain to img-src
- `Pages/Shared/_Layout.cshtml` - Removed inline onerror, added aikido-badge.js
- `Views/Shared/_Layout.cshtml` - Removed inline onerror, added aikido-badge.js  
- `Areas/Coach/Pages/Shared/_Layout.cshtml` - Removed inline onerror, added aikido-badge.js
- `wwwroot/js/aikido-badge.js` - New CSP-compliant error handling

## Security Benefits

- ✅ Aikido security badge loads properly
- ✅ No more CSP violations for badge error handling
- ✅ Maintained compatibility with Cloudflare services
- ✅ Enhanced protection against XSS attacks
- ⚠️ Still allows inline scripts (temporary)

## Next Steps

1. **Immediate**: Monitor for new CSP violations in production
2. **Short-term**: Begin refactoring inline event handlers (highest traffic pages first)
3. **Long-term**: Implement nonce-based CSP for maximum security
