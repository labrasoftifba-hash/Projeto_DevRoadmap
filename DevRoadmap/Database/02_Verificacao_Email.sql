IF COL_LENGTH('dbo.Usuarios', 'EmailVerificado') IS NULL
BEGIN
    ALTER TABLE dbo.Usuarios
    ADD EmailVerificado BIT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints AS defaults
    INNER JOIN sys.columns AS columns
        ON columns.object_id = defaults.parent_object_id
        AND columns.column_id = defaults.parent_column_id
    WHERE defaults.parent_object_id = OBJECT_ID('dbo.Usuarios')
        AND columns.name = 'EmailVerificado'
)
BEGIN
    ALTER TABLE dbo.Usuarios
    ADD CONSTRAINT DF_Usuarios_EmailVerificado
        DEFAULT (0) FOR EmailVerificado;
END;
GO

IF OBJECT_ID('dbo.VerificacoesEmail', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VerificacoesEmail
    (
        UsuarioId INT NOT NULL,
        CodigoHash VARBINARY(32) NOT NULL,
        ExpiraEm DATETIME2(0) NOT NULL,
        Tentativas TINYINT NOT NULL
            CONSTRAINT DF_VerificacoesEmail_Tentativas DEFAULT (0),
        CriadoEm DATETIME2(0) NOT NULL
            CONSTRAINT DF_VerificacoesEmail_CriadoEm DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_VerificacoesEmail PRIMARY KEY (UsuarioId),
        CONSTRAINT FK_VerificacoesEmail_Usuarios
            FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id),
        CONSTRAINT CK_VerificacoesEmail_Tentativas
            CHECK (Tentativas <= 5)
    );
END;
GO
