-- Run this in SSMS against your database.
-- Copy the "Results" tab output and paste it back to me.

SELECT 
    OBJECT_SCHEMA_NAME(t.object_id) AS [Schema],
    t.name AS [Table],
    c.name AS [Column],
    TYPE_NAME(c.system_type_id) AS [DataType],
    c.max_length AS [MaxLength],
    c.precision AS [Precision],
    c.scale AS [Scale],
    c.is_nullable AS [IsNullable],
    c.is_identity AS [IsIdentity],
    ep.value AS [DefaultValue]
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
LEFT JOIN sys.extended_properties ep ON ep.major_id = c.object_id AND ep.minor_id = c.column_id AND ep.name = 'Default'
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
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable,
    c.is_identity,
    NULL
FROM sys.views v
JOIN sys.columns c ON c.object_id = v.object_id
WHERE v.name IN (
    'VW_WH_CustomerCarsFull',
    'vw_CarsPlatesDashBoard',
    'vw_wh_ItemBarCodes',
    'vw_wh_ItemSubGroups'
)
ORDER BY [Table], c.column_id;
