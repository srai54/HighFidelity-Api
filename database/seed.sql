-- =============================================================================
-- HighFidelity dashboard schema + dummy data
-- Target: (localdb)\MSSQLLocalDB, database [HighFidelity]
-- Idempotent: safe to re-run (drops and recreates the five tables).
--
-- Run:
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -d HighFidelity -i Api\database\seed.sql
--
-- Icon columns hold Font Awesome solid glyphs stored as NCHAR codepoints
-- (private-use area) so the MAUI app renders them with the "FontAwesome" font.
-- =============================================================================

USE HighFidelity;
GO

DROP TABLE IF EXISTS dbo.DashboardCards;
DROP TABLE IF EXISTS dbo.RevenueCards;
DROP TABLE IF EXISTS dbo.Activities;
DROP TABLE IF EXISTS dbo.Orders;
DROP TABLE IF EXISTS dbo.TrafficSources;
GO

CREATE TABLE dbo.DashboardCards (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    Title         NVARCHAR(100)  NOT NULL,
    Amount        DECIMAL(18,2)  NOT NULL,
    AmountDisplay NVARCHAR(50)   NOT NULL,
    Icon          NVARCHAR(4)    NOT NULL,
    ThemeColorHex CHAR(7)        NOT NULL
);

CREATE TABLE dbo.RevenueCards (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    Title         NVARCHAR(100) NOT NULL,
    Value         NVARCHAR(50)  NOT NULL,
    Subtitle      NVARCHAR(100) NOT NULL DEFAULT (''),
    ChartType     NVARCHAR(10)  NOT NULL,   -- Bar | Area | Line
    BackgroundHex CHAR(7)       NOT NULL,
    AccentHex     CHAR(7)       NOT NULL
);

CREATE TABLE dbo.Activities (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Title        NVARCHAR(100) NOT NULL,
    Actor        NVARCHAR(100) NOT NULL,
    Action       NVARCHAR(200) NOT NULL,
    Time         NVARCHAR(50)  NOT NULL,
    Icon         NVARCHAR(4)   NOT NULL,
    IconColorHex CHAR(7)       NOT NULL
);

CREATE TABLE dbo.Orders (
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    Invoice  INT            NOT NULL,
    Customer NVARCHAR(100)  NOT NULL,
    Country  NVARCHAR(100)  NOT NULL,
    Price    DECIMAL(18,2)  NOT NULL,
    Status   NVARCHAR(20)   NOT NULL    -- Process | Open | On Hold
);

CREATE TABLE dbo.TrafficSources (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Source          NVARCHAR(100) NOT NULL,
    Percentage      FLOAT         NOT NULL,
    SegmentColorHex CHAR(7)       NOT NULL
);
GO

-- ---------------------------------------------------------------------------
-- Dummy data (mirrors the reference template / previous in-app static data)
-- ---------------------------------------------------------------------------

-- Summary cards: crown, heart, bullseye, dollar-sign
INSERT INTO dbo.DashboardCards (Title, Amount, AmountDisplay, Icon, ThemeColorHex) VALUES
(N'Wallet Ballance',  4567.53,  N'$4,567.53',  NCHAR(0xF521), '#F7284A'),
(N'Referral Earning', 1689.53,  N'$1689.53',   NCHAR(0xF004), '#7C60FA'),
(N'Estimate Sales',   2851.53,  N'$2851.53',   NCHAR(0xF140), '#2BC155'),
(N'Earning',          52567.53, N'$52,567.53', NCHAR(0xF155), '#FF5E9D');

INSERT INTO dbo.RevenueCards (Title, Value, Subtitle, ChartType, BackgroundHex, AccentHex) VALUES
(N'Revenue Status', N'$432', N'Jan 01 - Jan 10', N'Bar',  '#E1F0FF', '#2196F3'),
(N'Page View',      N'$432', N'',                N'Area', '#FFF8E1', '#FFB822'),
(N'Bounce Rate',    N'$432', N'',                N'Line', '#FBE4D7', '#ED5520'),
(N'Revenue Status', N'$432', N'Jan 01 - Jan 10', N'Bar',  '#F0DEFE', '#8214E8');

-- Activity feed: list, plus, file-lines, pen, reply
INSERT INTO dbo.Activities (Title, Actor, Action, Time, Icon, IconColorHex) VALUES
(N'Task Updated',      N'Nikolai',  N'Updated a Task',      N'42 Mins Ago', NCHAR(0xF03A), '#6259CE'),
(N'Deal Added',        N'Panshi',   N'Updated a Task',      N'1 Day Ago',   NCHAR(0xF067), '#EC407A'),
(N'Published Article', N'Rasel',    N'Published a Article', N'42 Mins Ago', NCHAR(0xF15C), '#29B6F6'),
(N'Dock Updated',      N'Reshmi',   N'Updated a Dock',      N'1 Day Ago',   NCHAR(0xF304), '#FFB822'),
(N'Replyed Comment',   N'Jenathon', N'Added a Comment',     N'1 Day Ago',   NCHAR(0xF3E5), '#2BC155');

INSERT INTO dbo.Orders (Invoice, Customer, Country, Price, Status) VALUES
(12386, N'Charly Dues',     N'Brazil',    299,  N'Process'),
(12386, N'Marko',           N'Italy',     2642, N'Open'),
(12386, N'Deniyel Onak',    N'Russia',    981,  N'On Hold'),
(12386, N'Belgiri Bastana', N'Korea',     369,  N'Process'),
(12386, N'Sarti Gnuska',    N'Japan',     1240, N'Open'),
(12387, N'Amara Okafor',    N'Nigeria',   754,  N'Open'),
(12388, N'Liam Carter',     N'USA',       1899, N'Process'),
(12389, N'Sofia Reyes',     N'Mexico',    432,  N'On Hold'),
(12390, N'Hans Meyer',      N'Germany',   3110, N'Open'),
(12391, N'Yuki Tanaka',     N'Japan',     587,  N'Process'),
(12392, N'Priya Sharma',    N'India',     1456, N'Open'),
(12393, N'Lucas Silva',     N'Brazil',    823,  N'On Hold'),
(12394, N'Emma Wilson',     N'UK',        2075, N'Open'),
(12395, N'Omar Haddad',     N'Egypt',     640,  N'Process'),
(12396, N'Chen Wei',        N'China',     1785, N'Open'),
(12397, N'Anna Kowalski',   N'Poland',    912,  N'On Hold'),
(12398, N'Pierre Dubois',   N'France',    1330, N'Process'),
(12399, N'Elena Petrova',   N'Russia',    468,  N'Open'),
(12400, N'Marco Rossi',     N'Italy',     2210, N'Open'),
(12401, N'Kim Min-jun',     N'Korea',     795,  N'Process'),
(12402, N'Sara Lindqvist',  N'Sweden',    1120, N'On Hold'),
(12403, N'David Cohen',     N'Israel',    356,  N'Open'),
(12404, N'Fatima Zahra',    N'Morocco',   1670, N'Process'),
(12405, N'Jack Thompson',   N'Australia', 940,  N'Open'),
(12406, N'Isabella Cruz',   N'Spain',     2380, N'On Hold'),
(12407, N'Noah Brown',      N'Canada',    515,  N'Open'),
(12408, N'Aisha Bello',     N'Ghana',     1245, N'Process'),
(12409, N'Mateus Costa',    N'Portugal',  860,  N'Open'),
(12410, N'Olga Ivanova',    N'Ukraine',   1990, N'On Hold'),
(12411, N'Tom Becker',      N'Austria',   730,  N'Process');

INSERT INTO dbo.TrafficSources (Source, Percentage, SegmentColorHex) VALUES
(N'Facebook',      34, '#2196F3'),
(N'Youtube',       55, '#FF5722'),
(N'Direct Search', 11, '#FFC107');
GO

SELECT 'DashboardCards' AS TableName, COUNT(*) AS Rows FROM dbo.DashboardCards
UNION ALL SELECT 'RevenueCards',   COUNT(*) FROM dbo.RevenueCards
UNION ALL SELECT 'Activities',     COUNT(*) FROM dbo.Activities
UNION ALL SELECT 'Orders',         COUNT(*) FROM dbo.Orders
UNION ALL SELECT 'TrafficSources', COUNT(*) FROM dbo.TrafficSources;
GO
