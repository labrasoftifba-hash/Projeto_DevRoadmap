IF COL_LENGTH('dbo.VerificacoesEmail', 'Finalidade') IS NULL
BEGIN
    ALTER TABLE dbo.VerificacoesEmail
    ADD Finalidade VARCHAR(20) NOT NULL
        CONSTRAINT DF_VerificacoesEmail_Finalidade DEFAULT ('Cadastro') WITH VALUES;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.key_constraints AS constraints
    INNER JOIN
    (
        SELECT
            index_columns.object_id,
            index_columns.index_id,
            COUNT(*) AS ColumnCount,
            MAX(CASE WHEN columns.name = 'UsuarioId' THEN 1 ELSE 0 END) AS HasUsuarioId,
            MAX(CASE WHEN columns.name = 'Finalidade' THEN 1 ELSE 0 END) AS HasFinalidade
        FROM sys.index_columns AS index_columns
        INNER JOIN sys.columns AS columns
            ON columns.object_id = index_columns.object_id
            AND columns.column_id = index_columns.column_id
        WHERE index_columns.key_ordinal > 0
            AND index_columns.object_id = OBJECT_ID('dbo.VerificacoesEmail')
        GROUP BY index_columns.object_id, index_columns.index_id
    ) AS key_columns
        ON key_columns.object_id = constraints.parent_object_id
        AND key_columns.index_id = constraints.unique_index_id
    WHERE constraints.parent_object_id = OBJECT_ID('dbo.VerificacoesEmail')
        AND constraints.type = 'PK'
        AND key_columns.ColumnCount = 2
        AND key_columns.HasUsuarioId = 1
        AND key_columns.HasFinalidade = 1
)
BEGIN
    DECLARE @PrimaryKeyName SYSNAME;

    SELECT @PrimaryKeyName = name
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.VerificacoesEmail')
        AND type = 'PK';

    IF @PrimaryKeyName IS NOT NULL
    BEGIN
        DECLARE @DropPrimaryKeySql NVARCHAR(MAX);
        SET @DropPrimaryKeySql =
            N'ALTER TABLE dbo.VerificacoesEmail DROP CONSTRAINT '
            + QUOTENAME(@PrimaryKeyName);

        EXEC sys.sp_executesql @DropPrimaryKeySql;
    END;

    ALTER TABLE dbo.VerificacoesEmail
    ADD CONSTRAINT PK_VerificacoesEmail
        PRIMARY KEY (UsuarioId, Finalidade);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.VerificacoesEmail')
        AND name = 'CK_VerificacoesEmail_Finalidade'
)
BEGIN
    ALTER TABLE dbo.VerificacoesEmail
    ADD CONSTRAINT CK_VerificacoesEmail_Finalidade
        CHECK (Finalidade IN ('Cadastro', 'RecuperacaoSenha'));
END;
GO
