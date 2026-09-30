using DevRoadmap.Models;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DevRoadmap.Repositories
{
    public class UsuarioRepository : BaseRepository
    {
        public UsuarioRepository(IConfiguration configuration)
            : base(configuration)
        {
        }

        public int Cadastrar(Usuario user)
        {
            return Cadastrar(user, null, null);
        }

        public int CadastrarPendente(Usuario user, byte[] codigoHash, DateTime expiraEm)
        {
            ValidarHashCodigo(codigoHash);
            return Cadastrar(user, codigoHash, expiraEm);
        }

        private int Cadastrar(Usuario user, byte[]? codigoHash, DateTime? expiraEm)
        {
            using var conn = new SqlConnection(_stringConnection);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            const string inserirUsuario =
                "INSERT INTO Usuarios (Nome, Email, Senha) OUTPUT INSERTED.Id VALUES (@Nome, @Email, @Senha)";

            int id;
            try
            {
                using var cmd = new SqlCommand(inserirUsuario, conn, transaction);
                cmd.Parameters.AddWithValue("@Nome", user.Nome);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Senha", user.Senha);
                id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (SqlException exception) when (exception.Number is 2601 or 2627)
            {
                return 0;
            }

            const string buscarPerfil = "SELECT Id FROM Perfis WHERE Cargo = @Cargo";
            using var perfilCmd = new SqlCommand(buscarPerfil, conn, transaction);
            perfilCmd.Parameters.AddWithValue("@Cargo", "Estudante");
            var perfilId = perfilCmd.ExecuteScalar();
            if (perfilId is null)
            {
                throw new InvalidOperationException("O perfil 'Estudante' não existe no banco de dados.");
            }

            const string inserirPerfil =
                "INSERT INTO UsuariosPerfis (UsuarioId, PerfilId) VALUES (@UsuarioId, @PerfilId)";
            using var vinculoCmd = new SqlCommand(inserirPerfil, conn, transaction);
            vinculoCmd.Parameters.AddWithValue("@UsuarioId", id);
            vinculoCmd.Parameters.AddWithValue("@PerfilId", Convert.ToInt32(perfilId));
            vinculoCmd.ExecuteNonQuery();

            if (codigoHash is not null && expiraEm.HasValue)
            {
                const string inserirVerificacao =
                    "INSERT INTO VerificacoesEmail (UsuarioId, Finalidade, CodigoHash, ExpiraEm) " +
                    "VALUES (@UsuarioId, 'Cadastro', @CodigoHash, @ExpiraEm)";

                using var verificacaoCmd = new SqlCommand(inserirVerificacao, conn, transaction);
                verificacaoCmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = id;
                verificacaoCmd.Parameters.Add("@CodigoHash", SqlDbType.VarBinary, 32).Value = codigoHash;
                verificacaoCmd.Parameters.Add("@ExpiraEm", SqlDbType.DateTime2).Value = expiraEm.Value;
                verificacaoCmd.ExecuteNonQuery();
            }

            transaction.Commit();
            return id;
        }

        public void AtualizarSenha(int usuarioId, string senhaHash)
        {
            using var conn = new SqlConnection(_stringConnection);
            const string query = "UPDATE Usuarios SET Senha = @Senha WHERE Id = @Id";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Senha", senhaHash);
            cmd.Parameters.AddWithValue("@Id", usuarioId);

            conn.Open();
            if (cmd.ExecuteNonQuery() != 1)
            {
                throw new InvalidOperationException("Não foi possível atualizar a senha do usuário.");
            }
        }

        public Usuario? ObterPorEmail(string email)
        {
            using var conn = new SqlConnection(_stringConnection);
            const string query =
                "SELECT Id, Nome, Email, Senha, DataCadastro, EmailVerificado FROM Usuarios WHERE Email = @Email";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Email", email);

            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            var usuario = new Usuario
            {
                Id = reader.GetInt32(0),
                Nome = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Senha = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                DataCadastro = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4),
                EmailVerificado = reader.IsDBNull(5) ? null : reader.GetBoolean(5),
            };

            return usuario;
        }

        public bool AtualizarCodigoVerificacaoEmail(
            int usuarioId,
            FinalidadeVerificacaoEmail finalidade,
            byte[] codigoHash,
            DateTime expiraEm,
            DateTime reenviarAntesDe)
        {
            ValidarHashCodigo(codigoHash);

            using var conn = new SqlConnection(_stringConnection);
            const string query =
                "UPDATE VerificacoesEmail SET CodigoHash = @CodigoHash, ExpiraEm = @ExpiraEm, " +
                "Tentativas = 0, CriadoEm = SYSUTCDATETIME() " +
                "WHERE UsuarioId = @UsuarioId AND Finalidade = @Finalidade " +
                "AND CriadoEm <= @ReenviarAntesDe";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@CodigoHash", SqlDbType.VarBinary, 32).Value = codigoHash;
            cmd.Parameters.Add("@ExpiraEm", SqlDbType.DateTime2).Value = expiraEm;
            cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            cmd.Parameters.Add("@Finalidade", SqlDbType.VarChar, 20).Value = ObterNomeFinalidade(finalidade);
            cmd.Parameters.Add("@ReenviarAntesDe", SqlDbType.DateTime2).Value = reenviarAntesDe;

            conn.Open();
            return cmd.ExecuteNonQuery() == 1;
        }

        public VerificacaoEmail? ObterCodigoVerificacaoEmail(
            int usuarioId,
            FinalidadeVerificacaoEmail finalidade)
        {
            using var conn = new SqlConnection(_stringConnection);
            const string query =
                "SELECT UsuarioId, Finalidade, CodigoHash, ExpiraEm, Tentativas " +
                "FROM VerificacoesEmail WHERE UsuarioId = @UsuarioId AND Finalidade = @Finalidade";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            cmd.Parameters.Add("@Finalidade", SqlDbType.VarChar, 20).Value = ObterNomeFinalidade(finalidade);

            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new VerificacaoEmail
            {
                UsuarioId = reader.GetInt32(0),
                Finalidade = ObterFinalidade(reader.GetString(1)),
                CodigoHash = reader.GetFieldValue<byte[]>(2),
                ExpiraEm = reader.GetDateTime(3),
                Tentativas = reader.GetByte(4),
            };
        }

        public bool CriarCodigoRecuperacaoSenha(
            int usuarioId,
            byte[] codigoHash,
            DateTime expiraEm,
            DateTime reenviarAntesDe)
        {
            ValidarHashCodigo(codigoHash);

            using var conn = new SqlConnection(_stringConnection);
            conn.Open();
            using var transaction = conn.BeginTransaction(IsolationLevel.Serializable);

            const string verificarConta =
                "SELECT EmailVerificado FROM Usuarios WITH (UPDLOCK, HOLDLOCK) " +
                "WHERE Id = @UsuarioId AND (EmailVerificado IS NULL OR EmailVerificado = 1)";

            using var contaCmd = new SqlCommand(verificarConta, conn, transaction);
            contaCmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            var emailVerificado = contaCmd.ExecuteScalar();
            if (emailVerificado is not bool verificado || !verificado)
            {
                transaction.Rollback();
                return false;
            }

            const string obterCriado =
                "SELECT CriadoEm FROM VerificacoesEmail WITH (UPDLOCK, HOLDLOCK) " +
                "WHERE UsuarioId = @UsuarioId AND Finalidade = 'RecuperacaoSenha'";

            using var obterCmd = new SqlCommand(obterCriado, conn, transaction);
            obterCmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            var criadoEm = obterCmd.ExecuteScalar();

            if (criadoEm is DateTime dataCriacao)
            {
                if (dataCriacao > reenviarAntesDe)
                {
                    transaction.Rollback();
                    return false;
                }

                const string atualizar =
                    "UPDATE VerificacoesEmail SET CodigoHash = @CodigoHash, ExpiraEm = @ExpiraEm, " +
                    "Tentativas = 0, CriadoEm = SYSUTCDATETIME() " +
                    "WHERE UsuarioId = @UsuarioId AND Finalidade = 'RecuperacaoSenha'";

                using var atualizarCmd = new SqlCommand(atualizar, conn, transaction);
                atualizarCmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
                atualizarCmd.Parameters.Add("@CodigoHash", SqlDbType.VarBinary, 32).Value = codigoHash;
                atualizarCmd.Parameters.Add("@ExpiraEm", SqlDbType.DateTime2).Value = expiraEm;
                atualizarCmd.ExecuteNonQuery();
            }
            else
            {
                const string inserir =
                    "INSERT INTO VerificacoesEmail (UsuarioId, Finalidade, CodigoHash, ExpiraEm) " +
                    "VALUES (@UsuarioId, 'RecuperacaoSenha', @CodigoHash, @ExpiraEm)";

                using var inserirCmd = new SqlCommand(inserir, conn, transaction);
                inserirCmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
                inserirCmd.Parameters.Add("@CodigoHash", SqlDbType.VarBinary, 32).Value = codigoHash;
                inserirCmd.Parameters.Add("@ExpiraEm", SqlDbType.DateTime2).Value = expiraEm;
                inserirCmd.ExecuteNonQuery();
            }

            transaction.Commit();
            return true;
        }

        public bool IncrementarTentativasVerificacaoEmail(
            int usuarioId,
            FinalidadeVerificacaoEmail finalidade,
            DateTime agora)
        {
            using var conn = new SqlConnection(_stringConnection);
            const string query =
                "UPDATE VerificacoesEmail SET Tentativas = Tentativas + 1 " +
                "WHERE UsuarioId = @UsuarioId AND Finalidade = @Finalidade " +
                "AND Tentativas < 5 AND ExpiraEm > @Agora";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            cmd.Parameters.Add("@Finalidade", SqlDbType.VarChar, 20).Value = ObterNomeFinalidade(finalidade);
            cmd.Parameters.Add("@Agora", SqlDbType.DateTime2).Value = agora;

            conn.Open();
            return cmd.ExecuteNonQuery() == 1;
        }

        public bool ConfirmarEmailEConsumirCodigo(int usuarioId, byte[] codigoHash, DateTime agora)
        {
            ValidarHashCodigo(codigoHash);

            using var conn = new SqlConnection(_stringConnection);
            conn.Open();
            using var transaction = conn.BeginTransaction(IsolationLevel.Serializable);

            const string atualizarUsuario =
                "UPDATE Usuarios SET EmailVerificado = 1 " +
                "WHERE Id = @UsuarioId AND EmailVerificado = 0";

            using var usuarioCmd = new SqlCommand(atualizarUsuario, conn, transaction);
            usuarioCmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;

            if (usuarioCmd.ExecuteNonQuery() != 1)
            {
                transaction.Rollback();
                return false;
            }

            const string consumirCodigo =
                "DELETE FROM VerificacoesEmail WHERE UsuarioId = @UsuarioId AND Finalidade = 'Cadastro' " +
                "AND CodigoHash = @CodigoHash AND ExpiraEm > @Agora AND Tentativas < 5";

            using var codigoCmd = new SqlCommand(consumirCodigo, conn, transaction);
            codigoCmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            codigoCmd.Parameters.Add("@CodigoHash", SqlDbType.VarBinary, 32).Value = codigoHash;
            codigoCmd.Parameters.Add("@Agora", SqlDbType.DateTime2).Value = agora;

            if (codigoCmd.ExecuteNonQuery() != 1)
            {
                transaction.Rollback();
                return false;
            }

            transaction.Commit();
            return true;
        }

        public bool RedefinirSenhaComCodigo(
            int usuarioId,
            byte[] codigoHash,
            string senhaHash,
            DateTime agora)
        {
            ValidarHashCodigo(codigoHash);

            using var conn = new SqlConnection(_stringConnection);
            conn.Open();
            using var transaction = conn.BeginTransaction(IsolationLevel.Serializable);

            const string consumirCodigo =
                "DELETE FROM VerificacoesEmail WHERE UsuarioId = @UsuarioId " +
                "AND Finalidade = 'RecuperacaoSenha' AND CodigoHash = @CodigoHash " +
                "AND ExpiraEm > @Agora AND Tentativas < 5";

            using var codigoCmd = new SqlCommand(consumirCodigo, conn, transaction);
            codigoCmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            codigoCmd.Parameters.Add("@CodigoHash", SqlDbType.VarBinary, 32).Value = codigoHash;
            codigoCmd.Parameters.Add("@Agora", SqlDbType.DateTime2).Value = agora;

            if (codigoCmd.ExecuteNonQuery() != 1)
            {
                transaction.Rollback();
                return false;
            }

            const string atualizarSenha =
                "UPDATE Usuarios SET Senha = @Senha, EmailVerificado = 1 " +
                "WHERE Id = @UsuarioId AND (EmailVerificado IS NULL OR EmailVerificado = 1)";

            using var senhaCmd = new SqlCommand(atualizarSenha, conn, transaction);
            senhaCmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            senhaCmd.Parameters.Add("@Senha", SqlDbType.NVarChar, -1).Value = senhaHash;

            if (senhaCmd.ExecuteNonQuery() != 1)
            {
                transaction.Rollback();
                return false;
            }

            transaction.Commit();
            return true;
        }

        private static string ObterNomeFinalidade(FinalidadeVerificacaoEmail finalidade)
        {
            return finalidade switch
            {
                FinalidadeVerificacaoEmail.Cadastro => "Cadastro",
                FinalidadeVerificacaoEmail.RecuperacaoSenha => "RecuperacaoSenha",
                _ => throw new ArgumentOutOfRangeException(nameof(finalidade), finalidade, "Finalidade inválida.")
            };
        }

        private static FinalidadeVerificacaoEmail ObterFinalidade(string finalidade)
        {
            return finalidade switch
            {
                "Cadastro" => FinalidadeVerificacaoEmail.Cadastro,
                "RecuperacaoSenha" => FinalidadeVerificacaoEmail.RecuperacaoSenha,
                _ => throw new InvalidOperationException("A finalidade armazenada para verificação de e-mail é inválida.")
            };
        }

        private static void ValidarHashCodigo(byte[] codigoHash)
        {
            ArgumentNullException.ThrowIfNull(codigoHash);
            if (codigoHash.Length != 32)
            {
                throw new ArgumentException("O hash do código deve conter 32 bytes.", nameof(codigoHash));
            }
        }

        public bool Cadastrar_Relacao_Perfil(int Id, string Cargo)
        {
            using(var conn = new SqlConnection(_stringConnection))
            {
                string query = "INSERT INTO UsuariosPerfis (UsuarioId, PerfilId) VALUES (@UsuarioId, @PerfilId)";
                int perfilID = Pegar_PerfilID(Cargo);

                if(perfilID > 0)
                {
                    var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@UsuarioId", Id);
                    cmd.Parameters.AddWithValue("@PerfilId", perfilID);
                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return true;
                }
                else
                {
                    return false;
                }
                    
            }
        }

        public int Pegar_PerfilID(string NomePerfil)
        {
            using(var conn = new SqlConnection(_stringConnection))
            {
                string query = "SELECT Id FROM Perfis WHERE Cargo = @Cargo";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Cargo", NomePerfil);
                conn.Open();

                var perfil = cmd.ExecuteScalar();

                return perfil == null ? 0 : Convert.ToInt32(perfil);
            }
        }
    }
}