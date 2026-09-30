using DevRoadmap.Models;
using DevRoadmap.Repositories;

namespace DevRoadmap.Services
{
    public class UsuarioService
    {
        private readonly UsuarioRepository _usuarioRepository;

        public UsuarioService(UsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public Usuario? Autenticar(string email, string senha)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                return null;
            }

            var usuario = _usuarioRepository.ObterPorEmail(email.Trim());

            if (usuario == null)
            {
                return null;
            }

            if (usuario.Senha.StartsWith("$2", StringComparison.Ordinal)
                && BCrypt.Net.BCrypt.Verify(senha, usuario.Senha))
            {
                if (usuario.EmailVerificado == false)
                {
                    return null;
                }

                return usuario;
            }

            return null;
        }

        public static string GerarHash(string valor)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(valor);
            return BCrypt.Net.BCrypt.HashPassword(valor);
        }

    }
}
