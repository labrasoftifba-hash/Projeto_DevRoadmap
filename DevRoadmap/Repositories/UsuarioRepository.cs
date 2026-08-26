using DevRoadmap.Models;
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
            using(var conn = new SqlConnection(_stringConnection))
            {
                string query = "INSERT INTO Usuarios (Nome, Email, Senha) VALUES (@Nome, @Email, @Senha) OUTPUT INSERTED.ID";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@email", user.Email);
                cmd.Parameters.AddWithValue("@senha", user.Senha);
                cmd.Parameters.AddWithValue("@nome", user.Nome);
                conn.Open();

                int id = Convert.ToInt32(cmd.ExecuteScalar());

                if(id > 0)
                {
                    return id;
                }
                else
                {
                    return 0;
                }
            }
        }

        public bool Cadastrar_Relacao_Perfil(int Id, string Cargo)
        {
            using(var conn = new SqlConnection(_stringConnection))
            {
                string query = "INSERT INTO Perfis_Usuarios (UsuarioID, PerfilID) VALUES (@UsuarioID, @PerfilID)";
                int perfilID = Pegar_PerfilID(Cargo);

                if(perfilID > 0)
                {
                    var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@UsuarioID", Id);
                    cmd.Parameters.AddWithValue("@PerfilID", perfilID);
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
                string query = "SELECT ID FROM Perfis WHERE Perfil = @Perfil";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Perfil", NomePerfil);
                conn.Open();

                var perfil = cmd.ExecuteScalar();

                return perfil == null ? 0 :Convert.ToInt32(perfil);
            }
        }
    }
}