CREATE VIEW [dbo].[VW_WH_CustomerCarsFull]  
AS  
SELECT  
    cc.[ID]                    AS Id,  
    cc.[PlateNumber]           AS PlateNumber,  
    cc.[VIN]                   AS VIN,  
    cc.[Color]                 AS Color,  
    cc.[VehicleYear]           AS VehicleYear,  
    cc.[Distance]              AS Distance,  
    cc.[Status]                AS CarStatus,  
    cc.PlateType               As PlateType,  
    cc.[CarMakesID]            AS CarMakesID,  
    mk.[Name_ar]               AS MakeName_Ar,
    mk.[Name_en]               AS MakeName_En,
    ISNULL(mk.[Name_en], mk.[Name_ar]) AS MakeName,  
  
    cc.[CarModelID]            AS CarModelID,  
    md.[Name_ar]               AS ModelName_Ar,
    md.[Name_en]               AS ModelName_En,
    ISNULL(md.[Name_en], md.[Name_ar]) AS ModelName,  
  
    cc.[VehicleType]           AS VehicleTypeID,  
    vt.[Name_ar]                AS VehicleTypeName_Ar,  
    vt.[Name_en]                AS VehicleTypeName_En,  
  
    cc.[VehicleStatus]         AS VehicleStatusID,  
    vs.[Name_ar]                AS VehicleStatusName_Ar,  
    vs.[Name_en]                AS VehicleStatusName_En,  
  
    cc.[EngineType]            AS EngineTypeID,  
    et.[Name_ar]                AS EngineTypeName_Ar,  
    et.[Name_en]                AS EngineTypeName_En,  
  
    cc.[CustomerID]            AS CustomerID,  
    cu.[Code]                   AS CustomerCode,  
    cu.[Name_Ar]                AS CustomerName_Ar,  
    cu.[Name_En]                AS CustomerName_En,  
    cu.[Phone1]                 AS CustomerPhone1,  
    cu.[Phone2]                 AS CustomerPhone2,  
    cu.[Mobile]                 AS CustomerMobile,  
    cu.[email]                  AS CustomerEmail,  
    cu.[Address]                 AS CustomerAddress,  
    cu.[CityID]                  AS CustomerCityID,  
    cu.[RegionID]                AS CustomerRegionID,  
    cu.[Inactive]                AS CustomerInactive,  
    cu.[StoreID]                 AS CustomerStoreID,  
  
    cc.[InsertUserID]          AS InsertUserID,  
    cc.[UpdateUserID]          AS UpdateUserID,  
    cc.[InsertDateTime]        AS InsertDateTime,  
    cc.[UpdateDateTime]        AS UpdateDateTime  
FROM [dbo].[wh_CustomerCars] cc  
LEFT JOIN [dbo].[wh_Customers] cu      ON cu.[ID] = cc.[CustomerID]  
LEFT JOIN [dbo].[wh_CarMakes] mk        ON mk.[MakeID] = cc.[CarMakesID]  
LEFT JOIN [dbo].[wh_CarModels] md       ON md.[ModelID] = cc.[CarModelID]  
LEFT JOIN [dbo].[wh_VehicleType] vt     ON vt.[ID] = cc.[VehicleType]  
LEFT JOIN [dbo].[wh_VehicleStatus] vs   ON vs.[ID] = cc.[VehicleStatus]  
LEFT JOIN [dbo].[wh_CarsEngineType] et  ON et.[ID] = cc.[EngineType]  
WHERE cc.[Status] = 1  
