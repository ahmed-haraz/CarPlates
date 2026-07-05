# CarPlates Database Schema

## SQLite Database
File: `carplates.db3`

## Tables

### ScanRecords
| Column | Type | Constraints |
|--------|------|-------------|
| Id | GUID | PRIMARY KEY |
| PlateNumber | TEXT | NOT NULL |
| PlateType | TEXT | NOT NULL |
| Confidence | REAL | NOT NULL |
| PhotoPath | TEXT | NULL |
| ScanTime | DATETIME | NOT NULL |
| VehicleBrand | TEXT | NULL |
| VehicleModel | TEXT | NULL |
| VehicleColor | TEXT | NULL |
| OwnerName | TEXT | NULL |
| AccessStatus | TEXT | NULL |
| SyncStatus | INTEGER | DEFAULT 0 |
| RetryCount | INTEGER | DEFAULT 0 |
| ApiError | TEXT | NULL |
| CreatedAt | DATETIME | NOT NULL |
| UpdatedAt | DATETIME | NULL |
| IsDeleted | INTEGER | DEFAULT 0 |

### PendingUploads
| Column | Type | Constraints |
|--------|------|-------------|
| Id | GUID | PRIMARY KEY |
| ScanRecordId | GUID | NOT NULL |
| PlateNumber | TEXT | NOT NULL |
| PhotoPath | TEXT | NULL |
| CreatedAt | DATETIME | NOT NULL |
| RetryCount | INTEGER | DEFAULT 0 |
| LastError | TEXT | NULL |
| Status | INTEGER | DEFAULT 0 |

## Indexes
- `IX_ScanRecords_PlateNumber` on `PlateNumber`
- `IX_ScanRecords_ScanTime` on `ScanTime`
- `IX_ScanRecords_SyncStatus` on `SyncStatus`
- `IX_PendingUploads_Status` on `Status`

## Sync Strategy
1. Scan records are created with `SyncStatus = Pending`
2. Background sync service processes pending uploads
3. On success: `SyncStatus = Synced`, vehicle info updated
4. On failure: `SyncStatus = Failed`, `RetryCount++`
5. Max retries: 3, then manual intervention required
