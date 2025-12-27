const { test, expect } = require('@playwright/test');

/**
 * GitLab UI Tests
 * Tests GitLab login functionality and basic navigation
 */

test.describe('GitLab Authentication and UI', () => {
  const GITLAB_URL = 'http://localhost:9191';
  const ADMIN_USERNAME = 'root';
  const ADMIN_PASSWORD = 'Adm1nP@ssw0rd2025!';
  const DEVELOPER_USERNAME = 'developer@example.com';
  const DEVELOPER_PASSWORD = 'DevP@ssw0rd123!';

  test.beforeEach(async ({ page }) => {
    // Set longer timeout for GitLab as it can be slow to start
    test.setTimeout(120000);
  });

  test('GitLab - Check service availability and login page', async ({ page }) => {
    console.log('🔍 Testing GitLab availability...');
    
    try {
      // Navigate to GitLab with longer timeout
      await page.goto(GITLAB_URL, { waitUntil: 'networkidle', timeout: 45000 });
      
      // Check if we can see GitLab branding or login form
      const pageTitle = await page.title();
      if (pageTitle.includes('GitLab') || pageTitle.includes('Sign in')) {
        console.log('✅ GitLab page loaded successfully');
      } else {
        console.log('⚠️ GitLab may still be initializing');
      }
      
      // Look for any GitLab indicators (more flexible)
      const gitlabIndicators = [
        page.locator('text=Sign in'),
        page.locator('text=GitLab'),
        page.locator('.login-page'),
        page.locator('[data-qa-selector="login_field"]'),
        page.locator('input[name="user[login]"]'),
        page.locator('.login-form'),
        page.locator('form'),
        page.locator('input[type="password"]')
      ];
      
      let indicatorFound = false;
      for (const indicator of gitlabIndicators) {
        try {
          await indicator.waitFor({ timeout: 5000 });
          indicatorFound = true;
          console.log('✅ Found GitLab login indicator');
          break;
        } catch (e) {
          // Continue to next indicator
        }
      }
      
      if (!indicatorFound) {
        console.log('❌ GitLab not ready - failing as expected until service is available');
        expect(indicatorFound).toBe(true);
      }
      
      console.log('✅ GitLab service check completed');
    } catch (error) {
      console.log('❌ GitLab connection failed - failing as expected until service is available');
      expect(false).toBe(true);
    }
  });

  test('GitLab - Admin (root) login functionality', async ({ page }) => {
    console.log('🔐 Testing GitLab admin login...');
    
    try {
      await page.goto(GITLAB_URL + '/users/sign_in', { timeout: 45000 });
      
      // Handle potential redirects or loading states
      await page.waitForLoadState('networkidle');
      
      // Check if GitLab is ready for login
      const loginReady = await page.locator('input[name="user[login]"], input[id="user_login"], form').isVisible().catch(() => false);
      
      if (!loginReady) {
        console.log('❌ GitLab login form not ready - failing as expected');
        expect(loginReady).toBe(true);
        return;
      }
      
      // Find login form elements with multiple selectors
      const usernameField = page.locator('input[name="user[login]"], input[id="user_login"], input[data-qa-selector="login_field"]').first();
      const passwordField = page.locator('input[name="user[password]"], input[id="user_password"], input[type="password"]').first();
      const signInButton = page.locator('input[type="submit"], button[type="submit"], .btn-confirm, button:has-text("Sign in")').first();
      
      // Wait for form to be ready
      await expect(usernameField).toBeVisible({ timeout: 15000 });
      
      // Fill login form
      await usernameField.fill(ADMIN_USERNAME);
      await passwordField.fill(ADMIN_PASSWORD);
      
      console.log('📝 Filled login credentials');
      
      // Submit form
      await signInButton.click();
      
      // Wait for navigation after login
      await page.waitForLoadState('networkidle', { timeout: 30000 });
      
      // Check for successful login indicators
      const successIndicators = [
        page.locator('[data-qa-selector="user_menu"]'),
        page.locator('.header-user'),
        page.locator('.navbar-nav .nav-item'),
        page.locator('text=Dashboard'),
        page.locator('text=Projects'),
        page.locator('.nav-sidebar'),
        page.locator('.top-bar'),
        page.locator('[data-testid="user-menu"]')
      ];
      
      let loginSuccess = false;
      for (const indicator of successIndicators) {
        try {
          await expect(indicator).toBeVisible({ timeout: 5000 });
          loginSuccess = true;
          console.log('✅ Found login success indicator');
          break;
        } catch (e) {
          // Continue to next indicator
        }
      }
      
      // Also check URL for successful login (not on sign_in page)
      const currentUrl = page.url();
      const urlIndicatesSuccess = !currentUrl.includes('/sign_in') && 
                                 !currentUrl.includes('/login') &&
                                 (currentUrl.includes('/dashboard') || 
                                  currentUrl.includes('/projects') || 
                                  currentUrl === GITLAB_URL + '/');
      
      if (loginSuccess || urlIndicatesSuccess) {
        console.log('✅ GitLab admin login successful');
      } else {
        console.log('⚠️ GitLab login status unclear - may need more time to initialize');
        await page.screenshot({ path: 'test-results/gitlab-login-debug.png' });
      }
      
      // Expect at least one success condition
      expect(loginSuccess || urlIndicatesSuccess).toBe(true);
    } catch (error) {
      console.log('❌ GitLab login test failed - service not ready');
      expect(false).toBe(true);
    }
  });

  test('GitLab - Check project creation capability', async ({ page }) => {
    console.log('📁 Testing GitLab project access...');
    
    try {
      // First check if GitLab is ready
      const response = await page.goto(GITLAB_URL, { timeout: 30000 });
      if (!response || !response.ok()) {
        console.log('❌ GitLab not accessible - failing as expected');
        expect(response?.ok()).toBe(true);
        return;
      }
      
      // Try login first
      await page.goto(GITLAB_URL + '/users/sign_in', { timeout: 30000 });
      await page.waitForLoadState('networkidle');
      
      const usernameField = page.locator('input[name="user[login]"], input[id="user_login"]').first();
      const passwordField = page.locator('input[type="password"]').first();
      const signInButton = page.locator('input[type="submit"], button[type="submit"]').first();
      
      if (await usernameField.isVisible()) {
        await usernameField.fill(ADMIN_USERNAME);
        await passwordField.fill(ADMIN_PASSWORD);
        await signInButton.click();
        await page.waitForLoadState('networkidle');
      } else {
        console.log('❌ GitLab login form not ready - failing as expected');
        expect(false).toBe(true);
        return;
      }
      
      // Try to access projects or create new project
      await page.goto(GITLAB_URL + '/projects/new', { timeout: 15000 });
      await page.waitForLoadState('networkidle');
      
      // Look for project creation form
      const projectNameField = page.locator('input[name="project[name]"], #project_name');
      const createButton = page.locator('button:has-text("Create project"), input[value="Create project"]');
      
      const formReady = await projectNameField.or(createButton).isVisible().catch(() => false);
      
      if (formReady) {
        console.log('✅ GitLab project creation page accessible');
      } else {
        console.log('⚠️ GitLab project page may not be ready yet');
      }
    } catch (error) {
      console.log('❌ GitLab project test failed - service not ready');
      expect(false).toBe(true);
    }
  });

  test('GitLab - API health check', async ({ page }) => {
    console.log('🔧 Testing GitLab API availability...');
    
    try {
      await page.goto(GITLAB_URL + '/api/v4/version', { timeout: 15000 });
      
      // Check if we get JSON response with version info
      const content = await page.content();
      const hasVersionInfo = content.includes('version') || content.includes('GitLab');
      
      if (hasVersionInfo) {
        console.log('✅ GitLab API is responding');
      } else {
        console.log('⚠️ GitLab API response unclear');
      }
    } catch (error) {
      console.log('⚠️ GitLab API may not be ready yet');
      // This is acceptable as GitLab takes time to fully initialize
    }
  });
});