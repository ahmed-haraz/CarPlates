-- Adds the SessionId column to the RefreshTokens table.
--
-- SessionId groups refresh tokens into login sessions. Every access token carries
-- a sessionId claim, and the API rejects any access token whose session no longer
-- has a live (non-revoked) refresh token. Logging in on another device revokes all
-- other sessions for the user, so the previous device is signed out immediately.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.RefreshTokens') AND name = N'SessionId'
)
BEGIN
    ALTER TABLE dbo.RefreshTokens
        ADD SessionId uniqueidentifier NOT NULL
        CONSTRAINT DF_RefreshTokens_SessionId
            DEFAULT ('00000000-0000-0000-0000-000000000000');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_RefreshTokens_UserId_SessionId'
      AND object_id = OBJECT_ID(N'dbo.RefreshTokens')
)
BEGIN
    CREATE INDEX IX_RefreshTokens_UserId_SessionId
        ON dbo.RefreshTokens (UserId, SessionId);
END
GO