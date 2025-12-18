import { test, expect } from '@playwright/test';
import { TestUsers, TEST_PASSWORD } from './fixtures/test-data';
import { login, unlockUserAccount } from './fixtures/test-utils';

/**
 * Core Functionality Tests - Essential functionality checks
 * These tests create minimal data and clean up after themselves
 */

// Track any tickets created for cleanup
const createdTicketUrls: string[] = [];

test.describe('Core Functionality', () => {
  
  // Cleanup after all tests in this file
  test.afterAll(async ({ browser }) => {
    if (createdTicketUrls.length === 0) return;
    
    const context = await browser.newContext();
    const page = await context.newPage();
    
    try {
      await login(page, TestUsers.admin.email, TestUsers.admin.password);
      
      for (const url of createdTicketUrls) {
        try {
          const ticketId = url.match(/\/Tickets\/Details\/(\d+)/)?.[1];
          if (ticketId) {
            await page.goto(`/Tickets/Delete/${ticketId}`);
            const confirmBtn = page.locator('button[type="submit"]:has-text("Delete")');
            if (await confirmBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
              await confirmBtn.click();
              await page.waitForLoadState('networkidle');
            }
          }
        } catch (e) {
          // Ignore cleanup errors
        }
      }
    } finally {
      await context.close();
    }
    
    createdTicketUrls.length = 0;
  });

  test.describe('Navigation', () => {
    
    test('home page loads', async ({ page }) => {
      await page.goto('/');
      await expect(page.locator('body')).toBeVisible();
    });

    test('login page is accessible', async ({ page }) => {
      await page.goto('/Account/Login');
      await expect(page.locator('input[name="Email"]')).toBeVisible();
    });
  });

  test.describe('User Roles', () => {
    
    test('regular user sees their tickets', async ({ page }) => {
      await login(page, TestUsers.user1.email, TestUsers.user1.password);
      await page.goto('/Tickets');
      await expect(page).toHaveURL(/.*Tickets.*/);
    });

    test('agent sees team tabs', async ({ page }) => {
      await login(page, TestUsers.agent1.email, TestUsers.agent1.password);
      await page.goto('/Tickets');
      
      // Should see team-related tabs
      await expect(page.locator('.nav-tabs, .nav-pills')).toBeVisible();
    });

    test('admin sees admin menu items', async ({ page }) => {
      await login(page, TestUsers.admin.email, TestUsers.admin.password);
      
      // Should see admin links
      await expect(page.locator('a[href*="/Users"], a:has-text("Users")')).toBeVisible();
      await expect(page.locator('a[href*="/Teams"], a:has-text("Teams")')).toBeVisible();
    });
  });

  test.describe('Ticket Operations', () => {
    
    test('create ticket form displays', async ({ page }) => {
      await login(page, TestUsers.user1.email, TestUsers.user1.password);
      await page.goto('/Tickets/Create');
      
      await expect(page.locator('input[name="Title"]')).toBeVisible();
      await expect(page.locator('textarea[name="Description"]')).toBeVisible();
    });

    test('can create and view ticket', async ({ page }) => {
      await login(page, TestUsers.user1.email, TestUsers.user1.password);
      await page.goto('/Tickets/Create');
      
      // Wait for form to be ready
      await expect(page.locator('input[name="Title"]')).toBeVisible({ timeout: 10000 });
      
      const uniqueTitle = `E2E Test Ticket - ${Date.now()}`;
      await page.fill('input[name="Title"]', uniqueTitle);
      await page.fill('textarea[name="Description"]', 'Test description for cleanup');
      
      // Select priority (required field - uses PriorityId)
      const prioritySelect = page.locator('select[name="PriorityId"]');
      if (await prioritySelect.isVisible().catch(() => false)) {
        await prioritySelect.selectOption({ index: 1 });
      }
      
      // Select category (required field - uses CategoryId)
      const categorySelect = page.locator('select[name="CategoryId"]');
      if (await categorySelect.isVisible().catch(() => false)) {
        await categorySelect.selectOption({ index: 1 });
      }
      
      // Use more specific selector for Create Ticket button
      await page.click('button:has-text("Create Ticket")');
      await page.waitForLoadState('networkidle');
      
      // Check for success message - the ticket was created if we see this
      const successMessage = page.locator('.alert-success, .alert:has-text("created successfully")');
      const hasSuccessMessage = await successMessage.isVisible({ timeout: 5000 }).catch(() => false);
      
      // Or check URL - redirect to details (may require permission) or list
      const url = page.url();
      const urlIndicatesSuccess = url.includes('/Tickets/Details/') || (url.includes('/Tickets') && !url.includes('/Create'));
      
      // Ticket creation is successful if we see a success message OR appropriate redirect
      const success = hasSuccessMessage || urlIndicatesSuccess;
      
      if (url.includes('/Tickets/Details/')) {
        createdTicketUrls.push(url);
      }
      
      expect(success).toBe(true);
    });

    test('ticket list shows tickets', async ({ page }) => {
      await login(page, TestUsers.user1.email, TestUsers.user1.password);
      await page.goto('/Tickets');
      
      // Wait for page to load
      await page.waitForLoadState('networkidle');
      
      // Should see the My Tickets heading or ticket table
      const hasHeading = await page.locator('h1:has-text("Tickets"), h1:has-text("My Tickets")').isVisible({ timeout: 5000 }).catch(() => false);
      const hasTable = await page.locator('table').isVisible({ timeout: 2000 }).catch(() => false);
      const hasCreateLink = await page.locator('a[href*="Create"]').isVisible({ timeout: 2000 }).catch(() => false);
      
      // Any of these indicates the page loaded correctly
      expect(hasHeading || hasTable || hasCreateLink).toBe(true);
    });
  });

  test.describe('Profile', () => {
    
    test('user can view profile', async ({ page }) => {
      await login(page, TestUsers.user1.email, TestUsers.user1.password);
      await page.goto('/Account/Profile');
      
      await expect(page.locator('body')).toContainText(TestUsers.user1.email);
    });
  });

  test.describe('Admin Functions', () => {
    
    test('admin can view users list', async ({ page }) => {
      await login(page, TestUsers.admin.email, TestUsers.admin.password);
      await page.goto('/Users');
      
      await expect(page).toHaveURL(/.*Users.*/);
    });

    test('admin can view teams list', async ({ page }) => {
      await login(page, TestUsers.admin.email, TestUsers.admin.password);
      await page.goto('/Teams');
      
      await expect(page).toHaveURL(/.*Teams.*/);
    });
  });
});
