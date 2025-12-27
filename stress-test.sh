#!/bin/bash
# Legacy script - redirects to new consolidated test suite  
echo "🔄 This script has been consolidated into the new test suite"
echo "   Redirecting to: ./devops/scripts/testing/test-suite.sh comprehensive (includes stress testing)"
echo ""
export STRESS_CONCURRENT_REQUESTS=10
export STRESS_TOTAL_REQUESTS=50
exec ./devops/scripts/testing/test-suite.sh comprehensive "$@"