/**
 * Test data and constants for E2E tests
 * Update these credentials to match your test environment
 * Note: Test users use the password set by the cleanup SQL script (Test@123)
 * Admin uses the original password (AppTime!1)
 */

export const ADMIN_PASSWORD = 'AppTime!1';
export const TEST_PASSWORD = 'Test@123'; // Password set by cleanup SQL script

export const TestUsers = {
  // Administrator
  admin: {
    email: 'perry.kistruck@gmail.com',
    password: ADMIN_PASSWORD,
    role: 'Administrator',
    displayName: 'Perry Kistruck'
  },
  
  // Managers
  manager1: {
    email: 'sarah.mitchell@company.com',
    password: TEST_PASSWORD,
    role: 'Manager',
    displayName: 'Sarah Mitchell'
  },
  manager2: {
    email: 'david.chen@company.com',
    password: TEST_PASSWORD,
    role: 'Manager',
    displayName: 'David Chen'
  },
  
  // Agents
  agent1: {
    email: 'james.taylor@company.com',
    password: TEST_PASSWORD,
    role: 'Agent',
    displayName: 'James Taylor'
  },
  agent2: {
    email: 'olivia.brown@company.com',
    password: TEST_PASSWORD,
    role: 'Agent',
    displayName: 'Olivia Brown'
  },
  
  // Regular Users
  user1: {
    email: 'john.smith@company.com',
    password: TEST_PASSWORD,
    role: 'User',
    displayName: 'John Smith'
  },
  user2: {
    email: 'emily.johnson@company.com',
    password: TEST_PASSWORD,
    role: 'User',
    displayName: 'Emily Johnson'
  }
};

export const TestTicket = {
  title: 'Test Ticket - Automated E2E Test',
  description: 'This is an automated test ticket created by Playwright E2E tests.',
  category: 'Software',
  priority: 'Medium'
};

export const MaliciousPayloads = {
  // XSS Attack Payloads
  xss: [
    '<script>alert("XSS")</script>',
    '<img src=x onerror=alert("XSS")>',
    '"><script>alert(document.cookie)</script>',
    "javascript:alert('XSS')",
    '<svg onload=alert("XSS")>',
    '<body onload=alert("XSS")>',
    '<iframe src="javascript:alert(\'XSS\')">',
    "'-alert(1)-'",
    '<img src="x" onerror="eval(atob(\'YWxlcnQoMSk=\'))">',
  ],
  
  // SQL Injection Payloads
  sqlInjection: [
    "' OR '1'='1",
    "'; DROP TABLE Users; --",
    "1'; DELETE FROM Tickets WHERE '1'='1",
    "' UNION SELECT * FROM Users --",
    "'; INSERT INTO Users (email) VALUES ('hacked@test.com'); --",
    "1 OR 1=1",
    "admin'--",
    "' OR 1=1; --",
    "1; UPDATE Users SET IsActive = 0; --",
  ],
  
  // Path Traversal Payloads
  pathTraversal: [
    '../../../etc/passwd',
    '..\\..\\..\\windows\\system32\\config\\sam',
    '....//....//....//etc/passwd',
    '%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd',
    '..%252f..%252f..%252fetc/passwd',
  ],
  
  // Command Injection Payloads
  commandInjection: [
    '; ls -la',
    '| cat /etc/passwd',
    '`whoami`',
    '$(whoami)',
    '; rm -rf /',
    '& dir',
    '| type C:\\Windows\\System32\\config\\SAM',
  ],
  
  // LDAP Injection Payloads
  ldapInjection: [
    '*)(objectClass=*',
    'admin)(&)',
    '*)(uid=*))(|(uid=*',
  ],
  
  // Header Injection Payloads
  headerInjection: [
    'value\r\nHeader-Injection: true',
    'value%0d%0aSet-Cookie:%20malicious=true',
    'value\nLocation: http://evil.com',
  ],
  
  // CSRF Token Bypass Attempts
  csrfBypass: [
    '',
    'null',
    'undefined',
    '{{csrf_token}}',
    '%00',
  ],
};

export const SecurityHeaders = {
  expected: [
    'X-Content-Type-Options',
    'X-Frame-Options',
    'X-XSS-Protection',
    'Content-Security-Policy',
    'Strict-Transport-Security',
    'Referrer-Policy',
  ],
  forbidden: [
    'Server', // Should not expose server info
    'X-Powered-By', // Should not expose technology stack
    'X-AspNet-Version', // Should not expose ASP.NET version
  ],
};

export const SensitiveRoutes = {
  adminOnly: [
    '/Users',
    '/Users/Create',
    '/Teams',
    '/Teams/Create',
  ],
  managerOrAdmin: [
    '/Reports',
  ],
  agentOrAbove: [
    '/Reports/Dashboard',
    '/Tickets/Edit/1',
  ],
  authenticated: [
    '/Tickets',
    '/Tickets/Create',
    '/Account/Profile',
    '/Account/ChangePassword',
  ],
};
