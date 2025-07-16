#!/bin/bash

# Script to find and report inline JavaScript usage in the WorkoutTracker application
# This helps identify scripts that need nonce attributes for CSP compliance

echo "🔍 Scanning for inline JavaScript that needs CSP nonce attributes..."
echo "================================================================"

echo ""
echo "📄 Files with inline <script> tags:"
echo "-----------------------------------"
grep -r -n "<script>" --include="*.cshtml" Pages/ Views/ Areas/ 2>/dev/null | grep -v "src=" | grep -v "nonce=" | head -20

echo ""
echo "🖱️  Files with inline event handlers (onclick, onload, etc.):"
echo "------------------------------------------------------------" 
grep -r -n "on[a-z]*=" --include="*.cshtml" Pages/ Views/ Areas/ 2>/dev/null | head -15

echo ""
echo "🔗 Files with javascript: URLs:"
echo "------------------------------"
grep -r -n "javascript:" --include="*.cshtml" Pages/ Views/ Areas/ 2>/dev/null | head -10

echo ""
echo "✅ Files already using nonces:"
echo "-----------------------------"
grep -r -n 'nonce="@Html.GetScriptNonce()"' --include="*.cshtml" Pages/ Views/ Areas/ 2>/dev/null | head -10

echo ""
echo "📊 Summary:"
echo "----------"
TOTAL_SCRIPTS=$(grep -r "<script>" --include="*.cshtml" Pages/ Views/ Areas/ 2>/dev/null | wc -l)
SCRIPTS_WITH_SRC=$(grep -r "<script.*src=" --include="*.cshtml" Pages/ Views/ Areas/ 2>/dev/null | wc -l)
SCRIPTS_WITH_NONCE=$(grep -r 'nonce=' --include="*.cshtml" Pages/ Views/ Areas/ 2>/dev/null | wc -l)
INLINE_EVENTS=$(grep -r "on[a-z]*=" --include="*.cshtml" Pages/ Views/ Areas/ 2>/dev/null | wc -l)

echo "Total <script> tags found: $TOTAL_SCRIPTS"
echo "External scripts (with src): $SCRIPTS_WITH_SRC"
echo "Scripts with nonce: $SCRIPTS_WITH_NONCE"
echo "Inline event handlers: $INLINE_EVENTS"

POTENTIAL_INLINE=$((TOTAL_SCRIPTS - SCRIPTS_WITH_SRC))
echo "Potential inline scripts needing nonces: $POTENTIAL_INLINE"

echo ""
echo "🚀 Next Steps:"
echo "-------------"
echo "1. Add nonce='@Html.GetScriptNonce()' to any inline <script> tags"
echo "2. Replace inline event handlers with addEventListener in external .js files"
echo "3. Convert javascript: URLs to proper event handlers"
echo "4. Test the application to ensure functionality is preserved"
echo ""
echo "💡 Use this pattern for inline scripts:"
echo '   <script nonce="@Html.GetScriptNonce()">'
echo "   // your code here"
echo "   </script>"
