-- ============================================================
-- Full structure dump for all tables/views used by the app.
-- Shows data types, required (NOT NULL) fields, PK/FK, defaults.
-- ============================================================

WITH SchemaInfo AS
(
    SELECT 
        OBJECT_SCHEMA_NAME(t.object_id) AS [Schema],
        t.name AS [Table],
        c.name AS [Column],
        TYPE_NAME(c.system_type_id) AS [DataType],
        CASE 
            WHEN c.max_length = -1 THEN 'MAX'
            ELSE CAST(c.max_length AS VARCHAR(10))
        END AS [MaxLength],
        c.precision AS [Precision],
        c.scale AS [Scale],
        CASE WHEN c.is_nullable = 0 THEN 'YES' ELSE 'NO' END AS [Required],
        CASE WHEN c.is_identity = 1 THEN 'YES' ELSE '' END AS [IsIdentity],
        CASE WHEN c.is_computed = 1 THEN 'YES' ELSE '' END AS [IsComputed],
        ISNULL(dc.definition, ep.value) AS [DefaultValue],
        c.column_id AS [ColumnId]
    FROM sys.tables t
    JOIN sys.columns c 
        ON c.object_id = t.object_id
    LEFT JOIN sys.default_constraints dc 
        ON dc.parent_object_id = c.object_id 
        AND dc.parent_column_id = c.column_id
    LEFT JOIN sys.extended_properties ep 
        ON ep.major_id = c.object_id 
        AND ep.minor_id = c.column_id 
        AND ep.name = 'Default'
    WHERE t.name IN (
        'wh_CarMakes',
        'wh_CarModels',
        'wh_CarsTechnician',
        'wh_WorkLocations',
        'wh_Customers',
        'wh_CustomerCars',
        'wh_CustomersBranch',
        'wh_ScanRecords',
        'wh_VehicleType',
        'wh_VehicleStatus',
        'wh_CarsEngineType',
        'wh_TransHeader',
        'wh_TransDetails',
        'wh_BillAttachments',
        'wh_PrTrans',
        'fw_Users',
        'fw_LOVStatments'
    )

    UNION ALL

    SELECT 
        OBJECT_SCHEMA_NAME(v.object_id),
        v.name,
        c.name,
        TYPE_NAME(c.system_type_id),
        CASE 
            WHEN c.max_length = -1 THEN 'MAX'
            ELSE CAST(c.max_length AS VARCHAR(10))
        END,
        c.precision,
        c.scale,
        CASE WHEN c.is_nullable = 0 THEN 'YES' ELSE 'NO' END,
        CASE WHEN c.is_identity = 1 THEN 'YES' ELSE '' END,
        CASE WHEN c.is_computed = 1 THEN 'YES' ELSE '' END,
        NULL,
        c.column_id
    FROM sys.views v
    JOIN sys.columns c 
        ON c.object_id = v.object_id
    WHERE v.name IN (
        'VW_WH_CustomerCarsFull',
        'vw_CarsPlatesDashBoard',
        'vw_wh_ItemBarCodes',
        'vw_wh_ItemSubGroups'
    )
)
SELECT
    [Schema],
    [Table],
    [Column],
    [DataType],
    [MaxLength],
    [Precision],
    [Scale],
    [Required],
    [IsIdentity],
    [IsComputed],
    [DefaultValue]
FROM SchemaInfo
ORDER BY [Table], [ColumnId];

-- ============================================================
-- Primary keys, unique keys, foreign keys (relationships)
-- ============================================================
SELECT
    OBJECT_SCHEMA_NAME(t.object_id) AS [Schema],
    t.name AS [Table],
    col.name AS [Column],
    'PRIMARY/UNIQUE KEY' AS [KeyType],
    kc.type_desc AS [KeyDesc],
    NULL AS [ReferencesTable],
    NULL AS [ReferencesColumn]
FROM sys.tables t
JOIN sys.key_constraints kc 
    ON kc.parent_object_id = t.object_id 
    AND kc.type IN ('PK', 'UQ')
JOIN sys.key_columns kcol 
    ON kcol.constraint_object_id = kc.object_id
JOIN sys.columns col 
    ON col.object_id = t.object_id 
    AND col.column_id = kcol.column_id
WHERE t.name IN (
    'wh_CarMakes', 'wh_CarModels', 'wh_CarsTechnician', 'wh_WorkLocations',
    'wh_Customers', 'wh_CustomerCars', 'wh_CustomersBranch', 'wh_ScanRecords',
    'wh_VehicleType', 'wh_VehicleStatus', 'wh_CarsEngineType',
    'wh_TransHeader', 'wh_TransDetails', 'wh_BillAttachments', 'wh_PrTrans',
    'fw_Users', 'fw_LOVStatments'
)
UNION ALL
SELECT
    OBJECT_SCHEMA_NAME(fk.parent_object_id),
    OBJECT_NAME(fk.parent_object_id) AS [Table],
    pc.name AS [Column],
    'FOREIGN KEY' AS [KeyType],
    fk.type_desc AS [KeyDesc],
    OBJECT_NAME(fk.referenced_object_id) AS [ReferencesTable],
    rc.name AS [ReferencesColumn]
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc 
    ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns pc 
    ON pc.object_id = fk.parent_object_id 
    AND pc.column_id = fkc.parent_column_id
JOIN sys.columns rc 
    ON rc.object_id = fk.referenced_object_id 
    AND rc.column_id = fkc.referenced_column_id
WHERE fk.parent_object_id IN (
    SELECT object_id FROM sys.tables 
    WHERE name IN (
        'wh_CarMakes', 'wh_CarModels', 'wh_CarsTechnician', 'wh_WorkLocations',
        'wh_Customers', 'wh_CustomerCars', 'wh_CustomersBranch', 'wh_ScanRecords',
        'wh_VehicleType', 'wh_VehicleStatus', 'wh_CarsEngineType',
        'wh_TransHeader', 'wh_TransDetails', 'wh_BillAttachments', 'wh_PrTrans',
        'fw_Users', 'fw_LOVStatments'
    )
)
ORDER BY [Table], [KeyType] DESC;
