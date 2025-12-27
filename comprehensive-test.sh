#!/bin/bash
# Legacy script - redirects to new consolidated test suite
echo "🔄 This script has been consolidated into the new test suite"
echo "   Redirecting to: ./devops/scripts/testing/test-suite.sh comprehensive"
echo ""
exec ./devops/scripts/testing/test-suite.sh comprehensive "$@"