# TicketingASP Playwright E2E Test Suite

A focused end-to-end test suite for the TicketingASP application using Playwright. This test suite focuses on **essential security testing** and core functionality verification.

## 📁 Project Structure

```
playwright-tests/
├── package.json                    # Project dependencies and scripts
├── playwright.config.ts            # Playwright configuration
├── global-setup.ts                 # Pre-test setup (unlock accounts, verify app)
├── global-teardown.ts              # Post-test cleanup (delete test data, unlock accounts)
├── tests/
│   ├── fixtures/
│   │   ├── test-data.ts           # Test users, payloads, and constants
│   │   └── test-utils.ts          # Helper functions and custom fixtures
│   ├── core-security.spec.ts      # Essential security tests (authentication, authorization, XSS, SQL injection, CSRF, etc.)
│   ├── core-functionality.spec.ts # Core functionality tests (navigation, tickets, admin)
│   ├── auth/                      # (Legacy) Additional auth tests
│   ├── security/                  # (Legacy) Additional security tests
│   ├── tickets/                   # (Legacy) Additional ticket tests
│   ├── admin/                     # (Legacy) Additional admin tests
│   └── navigation/                # (Legacy) Additional navigation tests
```

## ✨ Key Features

- **30 focused tests** covering essential security and functionality
- **Single browser** (Chromium) for fast execution
- **Automatic cleanup** - all test data is cleaned up after tests run
- **Account recovery** - locked accounts are automatically unlocked

## 🚀 Getting Started

### Prerequisites

- Node.js 18+ 
- npm or yarn
- The TicketingASP application running locally

### Installation

```bash
cd playwright-tests
npm install
npx playwright install
```

### Running the Application

Make sure the TicketingASP application is running:

```bash
cd ..
dotnet run
```

The default base URL is `http://localhost:5050`. You can change this by setting the `BASE_URL` environment variable.

## 🧪 Running Tests

### Run All Tests

```bash
npm test
```

### Run Tests with UI

```bash
npm run test:ui
```

### Run Tests in Headed Mode (Watch)

```bash
npm run test:headed
```

### Run Tests with Debug Mode

```bash
npm run test:debug
```

### Run Specific Test Categories

```bash
# Security tests only
npm run test:security

# Authentication tests only
npm run test:auth

# Ticket management tests only
npm run test:tickets

# Admin panel tests only
npm run test:admin
```

### Run Specific Test File

```bash
npx playwright test tests/security/xss.spec.ts
```

### Run Tests with Specific Browser

```bash
npx playwright test --project=chromium
npx playwright test --project=firefox
npx playwright test --project=webkit
```

## 📊 Test Reports

After running tests, view the HTML report:

```bash
npm run report
```

## 🔐 Security Test Coverage

The test suite includes comprehensive security testing:

### 1. Authentication Security
- ✅ Login validation
- ✅ Password requirements enforcement
- ✅ Session management
- ✅ Logout functionality
- ✅ Remember me functionality

### 2. Authorization & Access Control
- ✅ Role-based access control (User, Agent, Manager, Admin)
- ✅ Horizontal access control (users can't access others' data)
- ✅ Protected route enforcement
- ✅ Navigation element visibility by role

### 3. XSS Prevention
- ✅ Reflected XSS in form fields
- ✅ Stored XSS in ticket fields
- ✅ DOM-based XSS
- ✅ Event handler injection prevention
- ✅ Content Security Policy validation

### 4. CSRF Protection
- ✅ Anti-forgery token presence
- ✅ Token validation
- ✅ Cross-origin request blocking
- ✅ SameSite cookie attributes

### 5. SQL Injection Prevention
- ✅ Login form injection
- ✅ Search/filter injection
- ✅ ID parameter injection
- ✅ Form input injection
- ✅ Blind SQL injection prevention

### 6. Session Security
- ✅ Session fixation prevention
- ✅ Session timeout validation
- ✅ Cookie security attributes (HttpOnly, Secure, SameSite)
- ✅ Session invalidation on logout

### 7. Security Headers
- ✅ X-Content-Type-Options
- ✅ X-Frame-Options
- ✅ Referrer-Policy
- ✅ Server information disclosure prevention

### 8. Additional Security
- ✅ Open redirect prevention
- ✅ Clickjacking prevention
- ✅ Information disclosure prevention
- ✅ HTTP methods security
- ✅ Password masking

## 👤 Test Users

The test suite uses predefined test users from the database:

| Role | Email | Password |
|------|-------|----------|
| Administrator | admin@igdgroup.com.au | Test@123 |
| Manager | sarah.mitchell@company.com | Test@123 |
| Agent | james.taylor@company.com | Test@123 |
| User | john.smith@testcorp.com | Test@123 |

## ⚙️ Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `PLAYWRIGHT_BASE_URL` | Application base URL | `http://localhost:5050` |
| `TEST_CLEANUP_API_KEY` | API key for cleanup endpoint | `test-cleanup-key-change-in-production` |
| `DB_PASSWORD` | Database password (local dev only) | `Ticketing@123!` |
| `CI` | Running in CI environment | - |

### Cleanup Strategy

The test suite uses a dual cleanup strategy:

1. **API Cleanup (CI/CD - Preferred)**: Calls `POST /api/TestCleanup/cleanup` on the running application
2. **Direct DB Cleanup (Local Dev - Fallback)**: Connects directly to PostgreSQL via SSH tunnel

For GitHub Actions / Azure DevOps, configure these secrets:
- `TEST_CLEANUP_API_KEY`: A strong API key matching the value in your Azure App Service configuration

### GitHub Actions Example

```yaml
name: Playwright Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 18
      
      - name: Install dependencies
        working-directory: playwright-tests
        run: npm ci
      
      - name: Install Playwright
        working-directory: playwright-tests
        run: npx playwright install --with-deps chromium
      
      - name: Run Playwright tests
        working-directory: playwright-tests
        run: npx playwright test
        env:
          PLAYWRIGHT_BASE_URL: https://your-app.azurewebsites.net
          TEST_CLEANUP_API_KEY: ${{ secrets.TEST_CLEANUP_API_KEY }}
```

### Azure App Service Configuration

Add these application settings to your Azure App Service:

| Setting | Value |
|---------|-------|
| `TestCleanup__ApiKey` | Your secure API key (match GitHub secret) |

### Playwright Configuration

Edit `playwright.config.ts` to customize:
- Browser settings
- Timeouts
- Parallel execution
- Screenshots and video recording
- Web server startup

## 🔧 Code Generation

Generate test code by recording browser actions:

```bash
npm run codegen
```

## 📝 Writing New Tests

### Basic Test Structure

```typescript
import { test, expect } from '@playwright/test';
import { TestUsers } from '../fixtures/test-data';
import { login } from '../fixtures/test-utils';

test.describe('Feature Name', () => {
  test('should do something', async ({ page }) => {
    await login(page, TestUsers.user1.email, TestUsers.user1.password);
    await page.goto('/some-page');
    
    await expect(page.locator('h1')).toContainText('Expected Text');
  });
});
```

### Using Custom Fixtures

```typescript
import { test, expect } from '../fixtures/test-utils';

test.describe('Admin Tests', () => {
  test('admin can access users', async ({ adminPage }) => {
    await adminPage.goto('/Users');
    await expect(adminPage).toHaveURL(/.*Users.*/);
  });
});
```

## 🐛 Troubleshooting

### Tests Failing with Timeout
- Increase timeout in `playwright.config.ts`
- Check if the application is running
- Verify the BASE_URL is correct

### Authentication Issues
- Ensure test users exist in the database
- Verify passwords match (default: `Test@123`)
- Check if the database has been seeded with test data

### Selector Issues
- Use Playwright Inspector: `npx playwright test --debug`
- Use codegen to find correct selectors: `npm run codegen`

## 📜 License

This test suite is part of the TicketingASP project.
