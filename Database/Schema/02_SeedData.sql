-- =====================================================
-- SEED DATA FOR TICKETING SYSTEM
-- =====================================================

-- Insert Default Roles
INSERT INTO Roles (Name, Description, Permissions) VALUES
    ('Administrator', 'Full system access with all permissions', '{"all": true}'),
    ('Manager', 'Team management and reporting access', '{"tickets": {"read": true, "write": true, "delete": true}, "teams": {"read": true, "write": true}, "reports": {"read": true, "write": true}, "users": {"read": true}}'),
    ('Agent', 'Support agent - can work on tickets', '{"tickets": {"read": true, "write": true}, "teams": {"read": true}, "reports": {"read": true}}'),
    ('User', 'Regular user - can create and view own tickets', '{"tickets": {"read": "own", "write": "own"}}')
ON CONFLICT (Name) DO NOTHING;

-- Insert Default Priorities
INSERT INTO Priorities (Name, Description, Level, Color, SlaResponseHours, SlaResolutionHours) VALUES
    ('Critical', 'System down or major business impact', 1, '#DC2626', 1, 4),
    ('High', 'Significant impact on business operations', 2, '#EA580C', 4, 24),
    ('Medium', 'Moderate impact with workaround available', 3, '#CA8A04', 8, 72),
    ('Low', 'Minor issue or enhancement request', 4, '#16A34A', 24, 168)
ON CONFLICT (Name) DO NOTHING;

-- Insert Default Statuses
INSERT INTO Statuses (Name, Description, DisplayOrder, Color, IsDefault, IsClosed) VALUES
    ('New', 'Newly created ticket awaiting review', 1, '#3B82F6', TRUE, FALSE),
    ('Open', 'Ticket is open and being worked on', 2, '#8B5CF6', FALSE, FALSE),
    ('In Progress', 'Actively being worked on', 3, '#F59E0B', FALSE, FALSE),
    ('Pending', 'Waiting for customer response', 4, '#6B7280', FALSE, FALSE),
    ('On Hold', 'Temporarily on hold', 5, '#EF4444', FALSE, FALSE),
    ('Resolved', 'Issue has been resolved', 6, '#10B981', FALSE, FALSE),
    ('Closed', 'Ticket is closed', 7, '#1F2937', FALSE, TRUE),
    ('Cancelled', 'Ticket was cancelled', 8, '#9CA3AF', FALSE, TRUE)
ON CONFLICT (Name) DO NOTHING;

-- Insert Default Categories
INSERT INTO Categories (Name, Description) VALUES
    ('Hardware', 'Hardware related issues'),
    ('Software', 'Software and application issues'),
    ('Network', 'Network and connectivity issues'),
    ('Account', 'User account and access issues'),
    ('Other', 'General inquiries and other issues')
ON CONFLICT (Name, ParentId) DO NOTHING;

-- Insert Sub-Categories
INSERT INTO Categories (Name, Description, ParentId)
SELECT 'Desktop', 'Desktop computer issues', Id FROM Categories WHERE Name = 'Hardware' AND ParentId IS NULL
ON CONFLICT (Name, ParentId) DO NOTHING;

INSERT INTO Categories (Name, Description, ParentId)
SELECT 'Laptop', 'Laptop issues', Id FROM Categories WHERE Name = 'Hardware' AND ParentId IS NULL
ON CONFLICT (Name, ParentId) DO NOTHING;

INSERT INTO Categories (Name, Description, ParentId)
SELECT 'Printer', 'Printer and printing issues', Id FROM Categories WHERE Name = 'Hardware' AND ParentId IS NULL
ON CONFLICT (Name, ParentId) DO NOTHING;

INSERT INTO Categories (Name, Description, ParentId)
SELECT 'Email', 'Email client and configuration', Id FROM Categories WHERE Name = 'Software' AND ParentId IS NULL
ON CONFLICT (Name, ParentId) DO NOTHING;

INSERT INTO Categories (Name, Description, ParentId)
SELECT 'Office Suite', 'Microsoft Office and productivity apps', Id FROM Categories WHERE Name = 'Software' AND ParentId IS NULL
ON CONFLICT (Name, ParentId) DO NOTHING;

INSERT INTO Categories (Name, Description, ParentId)
SELECT 'VPN', 'VPN connectivity issues', Id FROM Categories WHERE Name = 'Network' AND ParentId IS NULL
ON CONFLICT (Name, ParentId) DO NOTHING;

INSERT INTO Categories (Name, Description, ParentId)
SELECT 'Password Reset', 'Password reset requests', Id FROM Categories WHERE Name = 'Account' AND ParentId IS NULL
ON CONFLICT (Name, ParentId) DO NOTHING;

INSERT INTO Categories (Name, Description, ParentId)
SELECT 'Access Request', 'System access requests', Id FROM Categories WHERE Name = 'Account' AND ParentId IS NULL
ON CONFLICT (Name, ParentId) DO NOTHING;

-- Insert Default Teams
INSERT INTO Teams (Name, Description, Email, IsActive, CreatedBy) VALUES
    ('IT Support', 'General IT support team', 'it-support@company.com', TRUE, 1),
    ('Network Team', 'Network and infrastructure support', 'network@company.com', TRUE, 1),
    ('Application Support', 'Business application support', 'app-support@company.com', TRUE, 1),
    ('Help Desk', 'First-level support desk', 'helpdesk@company.com', TRUE, 1)
ON CONFLICT DO NOTHING;
