import { chromium, FullConfig } from '@playwright/test';
import { TestUsers, TEST_PASSWORD } from './tests/fixtures/test-data';

// Get base URL from environment or default to localhost
const BASE_URL = process.env.BASE_URL || process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5050';

// List of test user emails that need to be unlocked before tests
const TEST_USER_EMAILS = [
  TestUsers.manager1.email,
  TestUsers.manager2.email,
  TestUsers.agent1.email,
  TestUsers.agent2.email,
  TestUsers.user1.email,
  TestUsers.user2.email,
];

/**
 * Global setup - runs once before all tests start
 * Ensures test accounts are unlocked and ready
 */
async function globalSetup(config: FullConfig) {
  console.log('\n[Setup] Running global setup...');
  console.log(`   Base URL: ${BASE_URL}`);
  
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();
  
  try {
    // Try to login as admin to verify the app is running
    await page.goto(`${BASE_URL}/Account/Login`, { timeout: 30000 });
    await page.fill('input[name="Email"]', TestUsers.admin.email);
    await page.fill('input[name="Password"]', TestUsers.admin.password);
    await page.click('button[type="submit"]');
    await page.waitForLoadState('networkidle');
    
    // Check if login succeeded
    if (page.url().includes('/Login')) {
      const errorText = await page.locator('.alert-danger').textContent().catch(() => '');
      console.log(`[Warning] Admin login issue: ${errorText || 'Unknown error'}`);
      console.log('   You may need to run the cleanup SQL script to reset passwords');
    } else {
      console.log('[OK] Application is running and admin can login');
      
      // Unlock and reset passwords for all test user accounts via the UI
      await unlockAndResetTestUsers(page);
    }
    
  } catch (error) {
    console.log(`[Warning] Setup error: ${error}`);
    console.log(`   Make sure the application is running on ${BASE_URL}`);
  } finally {
    await browser.close();
  }
  
  console.log('[OK] Global setup complete\n');
}

/**
 * Unlock and reset passwords for all test user accounts
 */
async function unlockAndResetTestUsers(page: any) {
  console.log('[Setup] Preparing test user accounts...');
  
  for (const email of TEST_USER_EMAILS) {
    try {
      // Search for the user
      await page.goto(`${BASE_URL}/Users?search=${encodeURIComponent(email)}`, { waitUntil: 'networkidle' });
      
      // Find the user row and click to view details
      const userLink = page.locator(`a[href*="/Users/Details/"]`).first();
      if (await userLink.isVisible({ timeout: 3000 })) {
        await userLink.click();
        await page.waitForLoadState('networkidle');
        
        // Reset password using the reset form
        const resetPasswordInput = page.locator('input[name="newPassword"]');
        const resetButton = page.locator('form[action*="ResetPassword"] button[type="submit"]');
        
        if (await resetPasswordInput.isVisible({ timeout: 2000 })) {
          await resetPasswordInput.fill(TEST_PASSWORD);
          await resetButton.click();
          await page.waitForLoadState('networkidle');
          
          // Check for success message
          const successMsg = await page.locator('.alert-success').textContent().catch(() => '');
          if (successMsg.includes('reset') || successMsg.includes('unlocked')) {
            console.log(`   - Reset password for: ${email}`);
          } else {
            console.log(`   - Password reset submitted for: ${email}`);
          }
        } else {
          // No reset password form - just check if locked
          const unlockButton = page.locator('form[action*="Unlock"] button[type="submit"]');
          if (await unlockButton.isVisible({ timeout: 1000 })) {
            await unlockButton.click();
            await page.waitForLoadState('networkidle');
            console.log(`   - Unlocked: ${email}`);
          } else {
            console.log(`   - Already ready: ${email}`);
          }
        }
      } else {
        console.log(`   [Warning] User not found: ${email}`);
      }
    } catch (error) {
      console.log(`   [Warning] Error preparing ${email}: ${error}`);
    }
  }
  
  console.log('[OK] Test user accounts ready');
}

export default globalSetup;
