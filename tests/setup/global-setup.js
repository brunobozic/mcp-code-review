// Global setup for Playwright tests
const { chromium } = require('@playwright/test');

async function globalSetup(config) {
  console.log('🚀 Starting MCP Code Review UI Test Suite');
  console.log('==========================================');
  
  // Check if services are running
  const services = [
    { name: 'GitLab', url: 'http://localhost:9191' },
    { name: 'Grafana', url: 'http://localhost:19192' },
    { name: 'Prometheus', url: 'http://localhost:9090' },
    { name: 'MCP Server', url: 'http://localhost:5000/health' }
  ];

  console.log('\n🔍 Checking service availability...');
  
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();

  for (const service of services) {
    try {
      const response = await page.goto(service.url, { 
        waitUntil: 'networkidle',
        timeout: 10000 
      });
      
      if (response && response.ok()) {
        console.log(`✅ ${service.name}: Available`);
      } else {
        console.log(`⚠️  ${service.name}: Responding but not ready`);
      }
    } catch (error) {
      console.log(`❌ ${service.name}: Not available - ${error.message.slice(0, 50)}...`);
    }
  }

  await browser.close();
  console.log('\n🧪 Starting UI tests...\n');
}

module.exports = globalSetup;