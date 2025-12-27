// Global teardown for Playwright tests

async function globalTeardown(config) {
  console.log('\n🏁 UI Test Suite Completed');
  console.log('==========================');
  
  // Add any cleanup logic here if needed
  console.log('✅ Test cleanup completed');
  console.log('📊 Results available in test-results/ directory');
}

module.exports = globalTeardown;