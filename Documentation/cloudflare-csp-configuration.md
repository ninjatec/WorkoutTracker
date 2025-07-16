# Cloudflare Configuration for CSP Security

## CSP Security Issues Resolution

Based on your security scan findings, here are the key improvements made and Cloudflare settings needed:

### 1. **Enhanced CSP Configuration**

✅ **Improvements Made:**
- Removed `'unsafe-inline'` from production script-src
- Removed wildcard WebSocket connections (`wss://* ws://*`)
- Added missing CSP directives: `worker-src`, `manifest-src`, `media-src`
- Added CSP violation reporting endpoint
- Added environment-specific CSP policies
- Enhanced security headers (HSTS, COEP, COOP)

### 2. **Cloudflare Settings Required**

#### **Security > WAF > Custom Rules**
Enable these rules to complement your CSP:
```
Field: URI Path
Operator: equals
Value: /api/CspReport/violations
Action: Allow
```

#### **Security > Headers**
Configure these headers in Cloudflare (they will complement, not override your app headers):
- **Strict-Transport-Security**: `max-age=31536000; includeSubDomains; preload`
- **X-Frame-Options**: `SAMEORIGIN`
- **X-Content-Type-Options**: `nosniff`

#### **Security > Bot Fight Mode**
- Enable "Bot Fight Mode" or "Super Bot Fight Mode"
- This helps with CSP by blocking malicious scripts

### 3. **Required Cloudflare Settings for CSP**

#### **SSL/TLS Settings:**
1. **SSL/TLS encryption mode**: "Full (strict)"
2. **Always Use HTTPS**: ON
3. **HTTP Strict Transport Security (HSTS)**: Enable with these settings:
   - Max Age: 12 months
   - Include subdomains: ON
   - Preload: ON

#### **Security Level:**
1. Go to **Security > Settings**
2. Set Security Level to "Medium" or "High"
3. Enable "Browser Integrity Check"

#### **Page Rules (if needed):**
Create page rules for your CSP reporting endpoint:
```
URL: yoursite.com/api/CspReport/violations
Settings:
- Security Level: Essentially Off (to allow violation reports)
- Cache Level: Bypass
```

### 4. **Cloudflare Workers (Optional Enhancement)**

If you want additional CSP protection, you can add a Cloudflare Worker:

```javascript
addEventListener('fetch', event => {
  event.respondWith(handleRequest(event.request))
})

async function handleRequest(request) {
  const response = await fetch(request)
  const newResponse = new Response(response.body, response)
  
  // Add additional CSP headers if needed
  newResponse.headers.set('Content-Security-Policy-Report-Only', 
    'default-src \'self\'; report-uri /api/CspReport/violations')
  
  return newResponse
}
```

### 5. **Testing Your CSP Configuration**

Use these endpoints to test:
- `/api/SecurityTest/csp-test` - General CSP test
- `/api/SecurityTest/cloudflare-csp-check` - Cloudflare-specific test
- `/api/CspReport/violations/summary` - Check violation reports

### 6. **Common Security Scan Issues Resolved**

✅ **Unsafe CSP directives** - Removed unsafe-eval and minimized unsafe-inline usage
✅ **Missing security headers** - Added HSTS, COEP, COOP
✅ **Wildcard sources** - Removed WebSocket wildcards
✅ **Missing CSP directives** - Added worker-src, manifest-src, media-src
✅ **No CSP reporting** - Added violation reporting endpoint
✅ **Weak frame protection** - Enhanced frame-ancestors and X-Frame-Options

### 7. **Google Analytics CSP Configuration**

✅ **Google Analytics Support Added:**
- Added comprehensive Google Analytics domains to script-src
- Added Google Analytics tracking domains to connect-src  
- Added Google Analytics image tracking to img-src
- Supports both Universal Analytics and Google Analytics 4

**Included domains:**
- `https://www.googletagmanager.com` - Google Tag Manager
- `https://googletagmanager.com` - Alternative GTM domain
- `https://www.google-analytics.com` - Google Analytics
- `https://ssl.google-analytics.com` - Secure Google Analytics
- `https://tagmanager.google.com` - Tag Manager API
- `https://analytics.google.com` - Analytics API
- `https://region1.google-analytics.com` - Regional Analytics
- `https://region1.analytics.google.com` - Regional Analytics API
- `https://stats.g.doubleclick.net` - DoubleClick tracking

### 8. **Monitoring CSP Violations**

Check your application logs for CSP violations:
```bash
grep -i "CSP Violation" logs/workouttracker-*.log
```

Or use the admin endpoint:
`GET /api/CspReport/violations/summary`

### 8. **Next Steps**

1. Deploy these changes to your production environment
2. Configure the Cloudflare settings listed above
3. Monitor CSP violation reports for 1-2 weeks
4. Further tighten CSP policies based on violation reports
5. Consider implementing Content Security Policy Level 3 features like 'strict-dynamic'

### 9. **Emergency CSP Disable**

If CSP causes issues, you can temporarily disable it by setting:
```json
"Security": {
  "ContentSecurityPolicy": {
    "Enabled": false
  }
}
```

This comprehensive approach should resolve most CSP-related security scan issues while maintaining compatibility with Cloudflare's proxy services.
