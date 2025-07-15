# Content Security Policy (CSP) Fixes and Recommendations

## Issues Fixed

### 1. Aikido Security Badge Image Loading
**Issue**: The Zen Security/Aikido badge image from `https://app.aikido.dev/` was blocked by CSP.
**Fix**: Added `https://app.aikido.dev` to the `img-src` directive in `ContentSecurityPolicyMiddleware.cs`.

### 2. Inline Event Handlers (onerror)
**Issue**: The Aikido badge had an `onerror` inline event handler that violated CSP.
**Fix**: 
- Removed `onerror` attributes from all Aikido badge images in layout files
- Created `aikido-badge.js` with CSP-compliant error handling using `addEventListener`
- Added the script to all layout files

## Current CSP Configuration

```csp
default-src 'self';
script-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://static.cloudflareinsights.com https://challenges.cloudflare.com 'unsafe-inline' 'unsafe-eval';
style-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://fonts.googleapis.com 'unsafe-inline';
img-src 'self' data: blob: https://cdn.jsdelivr.net https://challenges.cloudflare.com https://avatars.githubusercontent.com https://app.aikido.dev;
font-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.gstatic.com data:;
connect-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net [allowed domains] https://challenges.cloudflare.com [websocket urls];
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
