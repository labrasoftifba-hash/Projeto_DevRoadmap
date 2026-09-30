CREATE TABLE Perfis (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Cargo NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE Usuarios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nome NVARCHAR(150) NOT NULL,
    Email NVARCHAR(250) NOT NULL UNIQUE,
    Senha NVARCHAR(255) NOT NULL,
    DataCadastro DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE UsuariosPerfis (
    UsuarioId INT NOT NULL,
    PerfilId INT NOT NULL,
    CONSTRAINT PK_UsuariosPerfis PRIMARY KEY (UsuarioId, PerfilId),
    CONSTRAINT FK_UsuariosPerfis_Usuario FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
    CONSTRAINT FK_UsuariosPerfis_Perfil FOREIGN KEY (PerfilId) REFERENCES Perfis(Id)
);

INSERT INTO Perfis (Cargo)
VALUES ('Estudante'), ('Professor'), ('Administrador');
