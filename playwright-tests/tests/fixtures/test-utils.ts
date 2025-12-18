import { test as base, expect, Page, BrowserContext } from '@playwright/test';
import { TestUsers, TEST_PASSWORD } from './test-data';
import path from 'path';

/**
 * Custom test fixtures for authentication and common operations
 */

type User = typeof TestUsers.admin;

interface TestFixtures {
  authenticatedPage: Page;
  adminPage: Page;
  managerPage: Page;
  agentPage: Page;
  userPage: Page;
}

// Auth state file paths
const authDir = path.join(__dirname, '../../.auth');
export const AuthFiles = {
  admin: path.join(authDir, 'admin.json'),
  manager: path.join(authDir, 'manager.json'),
  agent: path.join(authDir, 'agent.json'),
  user: path.join(authDir, 'user.json'),
};

/**
 * Login helper function
 * Used when a test needs to login as a different user than the default storage state
 */
export async function login(page: Page, email: string, password: string): Promise<void> {
  // Navigate to a page to check current state
  const currentUrl = page.url();
  
  // If we're on the login page already, just fill the form
  if (currentUrl.includes('/Login') || currentUrl.includes('/Account/Login')) {
    await fillLoginForm(page, email, password);
    return;
  }
  
  // Try to access a protected page to verify current login state
  await page.goto('/Account/Profile', { waitUntil: 'domcontentloaded', timeout: 15000 });
  
  if (page.url().includes('/Login')) {
    // Not logged in, proceed with login
    await fillLoginForm(page, email, password);
    return;
  }
  
  // Check if we're logged in as the right user by checking the email on profile
  const emailField = page.locator('input[name="Email"], #Email, [data-email]');
  if (await emailField.isVisible({ timeout: 3000 }).catch(() => false)) {
    const currentEmail = await emailField.inputValue().catch(() => '');
    if (currentEmail.toLowerCase() === email.toLowerCase()) {
      // Already logged in as the right user
      return;
    }
  }
  
  // Need to logout and login as different user
  await logoutIfNeeded(page);
  await fillLoginForm(page, email, password);
}

async function logoutIfNeeded(page: Page): Promise<void> {
  try {
    // Try to find and click logout
    const logoutForm = page.locator('form[action*="Logout"] button[type="submit"]');
    if (await logoutForm.isVisible({ timeout: 2000 })) {
      await logoutForm.click();
      await page.waitForURL('**/Account/Login**', { timeout: 10000 });
      return;
    }
    
    // Try dropdown logout
    const dropdown = page.locator('.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 1000 })) {
      await dropdown.click();
      const logoutButton = page.locator('button:has-text("Logout"), a:has-text("Logout")');
      if (await logoutButton.isVisible({ timeout: 1000 })) {
        await logoutButton.click();
        await page.waitForURL('**/Account/Login**', { timeout: 10000 });
      }
    }
  } catch {
    // If logout fails, go directly to login page - the server will handle the session
    await page.goto('/Account/Login', { waitUntil: 'networkidle' });
  }
}

async function fillLoginForm(page: Page, email: string, password: string): Promise<void> {
  // Navigate to login if not already there
  if (!page.url().includes('/Login')) {
    await page.goto('/Account/Login', { waitUntil: 'domcontentloaded' });
  }
  
  // Wait for form to be ready
  await page.waitForSelector('input[name="Email"]', { state: 'visible', timeout: 15000 });
  
  // Clear and fill the form fields
  await page.locator('input[name="Email"]').clear();
  await page.locator('input[name="Email"]').fill(email);
  await page.locator('input[name="Password"]').clear();
  await page.locator('input[name="Password"]').fill(password);
  
  // Find the login button within the form specifically
  const loginButton = page.locator('form button[type="submit"]:has-text("Sign In")').first();
  
  // Click the button
  await loginButton.click();
  
  // Wait for either navigation away from login OR an error message to appear
  try {
    await Promise.race([
      page.waitForURL(url => !url.pathname.includes('/Login'), { timeout: 30000 }),
      page.waitForSelector('.alert-danger, .validation-summary-errors', { state: 'visible', timeout: 5000 })
        .then(async () => {
          // Error appeared - read it and throw
          const errorText = await page.locator('.alert-danger, .validation-summary-errors').textContent();
          throw new Error(`Login failed: ${errorText?.trim() || 'Unknown error'}`);
        })
    ]);
  } catch (error) {
    // Re-throw login errors
    if (error instanceof Error && error.message.includes('Login failed')) {
      throw error;
    }
    // Check final state
    const currentUrl = page.url();
    if (currentUrl.includes('/Login')) {
      // Check for any visible error
      const errorVisible = await page.locator('.alert-danger, .validation-summary-errors').isVisible();
      if (errorVisible) {
        const errorText = await page.locator('.alert-danger, .validation-summary-errors').textContent();
        throw new Error(`Login failed: ${errorText?.trim() || 'Unknown error'}`);
      }
      throw new Error(`Login did not redirect. Still on: ${currentUrl}`);
    }
  }
  
  // Additional wait to ensure page is stable after navigation
  await page.waitForLoadState('domcontentloaded');
}

/**
 * Logout helper function
 */
export async function logout(page: Page): Promise<void> {
  // Click on user dropdown and logout
  await page.click('.nav-link.dropdown-toggle:has-text("@")');
  await page.click('button:has-text("Logout")');
  await page.waitForURL('**/Account/Login**');
}

/**
 * Create a new authenticated context for a specific user
 */
export async function createAuthenticatedContext(
  browser: any, 
  user: User
): Promise<{ context: BrowserContext; page: Page }> {
  const context = await browser.newContext();
  const page = await context.newPage();
  await login(page, user.email, user.password);
  return { context, page };
}

/**
 * Extended test with authentication fixtures
 */
export const test = base.extend<TestFixtures>({
  authenticatedPage: async ({ browser }, use) => {
    const context = await browser.newContext();
    const page = await context.newPage();
    await login(page, TestUsers.user1.email, TestUsers.user1.password);
    await use(page);
    await context.close();
  },

  adminPage: async ({ browser }, use) => {
    const context = await browser.newContext();
    const page = await context.newPage();
    await login(page, TestUsers.admin.email, TestUsers.admin.password);
    await use(page);
    await context.close();
  },

  managerPage: async ({ browser }, use) => {
    const context = await browser.newContext();
    const page = await context.newPage();
    await login(page, TestUsers.manager1.email, TestUsers.manager1.password);
    await use(page);
    await context.close();
  },

  agentPage: async ({ browser }, use) => {
    const context = await browser.newContext();
    const page = await context.newPage();
    await login(page, TestUsers.agent1.email, TestUsers.agent1.password);
    await use(page);
    await context.close();
  },

  userPage: async ({ browser }, use) => {
    const context = await browser.newContext();
    const page = await context.newPage();
    await login(page, TestUsers.user1.email, TestUsers.user1.password);
    await use(page);
    await context.close();
  },
});

export { expect };

/**
 * Click the primary submit button within a form context.
 * Avoids clicking the Logout button by using more specific selectors.
 * @param page - Playwright page
 * @param context - Optional context selector (e.g., '.card-body', '#filterDrawer', 'form')
 */
export async function clickFormSubmit(page: Page, context?: string): Promise<void> {
  let selector: string;
  
  if (context) {
    // Use the provided context to scope the submit button
    selector = `${context} button[type="submit"]:visible`;
  } else {
    // Try to find a visible submit button that's not the Logout button
    // Prioritize main content area buttons over navbar buttons
    selector = 'main button[type="submit"], .card-body button[type="submit"], .container button[type="submit"]:not(.dropdown-item)';
  }
  
  // Try to click the first visible matching element
  const buttons = page.locator(selector);
  const count = await buttons.count();
  
  if (count > 0) {
    await buttons.first().click();
  } else {
    // Fallback: find any visible submit button that's not in the navbar
    await page.locator('button[type="submit"]:visible').filter({ hasNotText: 'Logout' }).first().click();
  }
}

/**
 * Helper to check if an element contains XSS payload (not escaped properly)
 */
export async function checkForXSSExecution(page: Page): Promise<boolean> {
  // Check if any dialog was triggered
  let dialogTriggered = false;
  page.on('dialog', async dialog => {
    dialogTriggered = true;
    await dialog.dismiss();
  });
  
  // Wait a moment for any XSS to potentially execute
  await page.waitForTimeout(1000);
  
  return dialogTriggered;
}

/**
 * Helper to extract anti-forgery token from page
 */
export async function getAntiForgeryToken(page: Page): Promise<string | null> {
  const token = await page.inputValue('input[name="__RequestVerificationToken"]');
  return token;
}

/**
 * Helper to make a POST request without anti-forgery token
 */
export async function postWithoutAntiForgery(
  page: Page, 
  url: string, 
  data: Record<string, string>
) {
  const response = await page.request.post(url, {
    form: data,
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
    },
  });
  return response;
}

/**
 * Helper to check response headers for security
 */
export function checkSecurityHeaders(headers: { [key: string]: string }): {
  present: string[];
  missing: string[];
  forbidden: string[];
} {
  const expectedHeaders = [
    'x-content-type-options',
    'x-frame-options',
  ];
  
  const forbiddenHeaders = [
    'x-powered-by',
    'x-aspnet-version',
  ];
  
  const headerKeys = Object.keys(headers).map(h => h.toLowerCase());
  
  return {
    present: expectedHeaders.filter(h => headerKeys.includes(h)),
    missing: expectedHeaders.filter(h => !headerKeys.includes(h)),
    forbidden: forbiddenHeaders.filter(h => headerKeys.includes(h)),
  };
}

/**
 * Unlock a user account that may have been locked due to failed login attempts.
 * This function logs in as admin, finds the user, and unlocks them if locked.
 * Use this in afterEach() for tests that intentionally test failed logins.
 * 
 * @param page - Playwright page (a new page/context is recommended)
 * @param userEmail - Email of the user to unlock
 * @param newPassword - Optional new password to set (defaults to TEST_PASSWORD)
 */
export async function unlockUserAccount(page: Page, userEmail: string, newPassword: string = TEST_PASSWORD): Promise<void> {
  try {
    // Login as admin
    await page.goto('/Account/Login', { waitUntil: 'domcontentloaded' });
    await page.fill('input[name="Email"]', TestUsers.admin.email);
    await page.fill('input[name="Password"]', TestUsers.admin.password);
    await page.click('button[type="submit"]');
    await page.waitForLoadState('networkidle');
    
    // Check if login succeeded
    if (page.url().includes('/Login')) {
      console.log('⚠️  Admin login failed - cannot unlock user');
      return;
    }
    
    // Search for the user
    await page.goto(`/Users?search=${encodeURIComponent(userEmail)}`, { waitUntil: 'networkidle' });
    
    // Find the user row and click to view details
    const userLink = page.locator(`a[href*="/Users/Details/"]`).first();
    if (await userLink.isVisible({ timeout: 3000 })) {
      await userLink.click();
      await page.waitForLoadState('networkidle');
      
      // Check if the account is locked and unlock it
      const unlockButton = page.locator('form[action*="Unlock"] button[type="submit"]');
      if (await unlockButton.isVisible({ timeout: 1000 }).catch(() => false)) {
        await unlockButton.click();
        await page.waitForLoadState('networkidle');
        console.log(`   🔓 Unlocked account: ${userEmail}`);
      }
      
      // Reset password to ensure it's correct
      const resetPasswordInput = page.locator('input[name="newPassword"]');
      const resetButton = page.locator('form[action*="ResetPassword"] button[type="submit"]');
      
      if (await resetPasswordInput.isVisible({ timeout: 1000 }).catch(() => false)) {
        await resetPasswordInput.fill(newPassword);
        await resetButton.click();
        await page.waitForLoadState('networkidle');
      }
    }
  } catch (error) {
    console.log(`⚠️  Error unlocking account ${userEmail}: ${error}`);
  }
}
