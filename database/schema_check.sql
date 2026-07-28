-- ============================================================
-- Schema verification / migration script
-- Run this to see if your tables match what the code expects.
-- ============================================================

-- 1. wh_CarMakes
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('wh_CarMakes') AND name = 'Name_ar')
    ALTER TABLE wh_CarMakes ADD Name_ar NVARCHAR(255) NOT NULL DEFAULT '';
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('wh_CarMakes') AND name = 'Name_en')
    ALTER TABLE wh_CarMakes ADD Name_en NVARCHAR(255) NOT NULL DEFAULT '';
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('wh_CarMakes') AND name = 'Status' AND system_type_id = 48 /* tinyint */)
    ALTER TABLE wh_CarMakes ALTER COLUMN Status INT NOT NULL;

-- 2. wh_CarModels
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('wh_CarModels') AND name = 'Name_ar')
    ALTER TABLE wh_CarModels ADD Name_ar NVARCHAR(255) NOT NULL DEFAULT '';
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('wh_CarModels') AND name = 'Name_en')
    ALTER TABLE wh_CarModels ADD Name_en NVARCHAR(255) NOT NULL DEFAULT '';
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('wh_CarModels') AND name = 'Status' AND system_type_id = 48 /* tinyint */)
    ALTER TABLE wh_CarModels ALTER COLUMN Status INT NOT NULL;

-- 3. wh_CarsTechnician (table, not view)
IF OBJECT_ID('wh_CarsTechnicians', 'V') IS NOT NULL
    DROP VIEW wh_CarsTechnicians;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('wh_CarsTechnician') AND name = 'Status' AND system_type_id = 48 /* tinyint */)
    ALTER TABLE wh_CarsTechnician ALTER COLUMN Status INT NOT NULL;

-- 4. wh_WorkLocations (table, not view)
IF OBJECT_ID('wh_Cars_WorkLocations', 'V') IS NOT NULL
    DROP VIEW wh_Cars_WorkLocations;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('wh_WorkLocations') AND name = 'Status' AND system_type_id = 48 /* tinyint */)
    ALTER TABLE wh_WorkLocations ALTER COLUMN Status INT NOT NULL;

-- 5. wh_Customers -- ensure Status is INT
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('wh_Customers') AND name = 'Status' AND system_type_id = 48 /* tinyint */)
    ALTER TABLE wh_Customers ALTER COLUMN Status INT NOT NULL;

-- 6. wh_ScanRecords -- ensure Status is INT
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('wh_ScanRecords') AND name = 'Status' AND system_type_id = 48 /* tinyint */)
    ALTER TABLE wh_ScanRecords ALTER COLUMN Status INT NOT NULL;

-- 7. wh_CustomerCars -- ensure Status is INT
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('wh_CustomerCars') AND name = 'Status' AND system_type_id = 48 /* tinyint */)
    ALTER TABLE wh_CustomerCars ALTER COLUMN Status INT NOT NULL;

PRINT 'Schema check complete.';
