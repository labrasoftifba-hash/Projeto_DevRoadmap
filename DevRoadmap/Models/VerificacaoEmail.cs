namespace DevRoadmap.Models
{
    public enum FinalidadeVerificacaoEmail
    {
        Cadastro,
        RecuperacaoSenha
    }

    public class VerificacaoEmail
    {
        public int UsuarioId { get; set; }
        public FinalidadeVerificacaoEmail Finalidade { get; set; }
        public byte[] CodigoHash { get; set; } = [];
        public DateTime ExpiraEm { get; set; }
        public byte Tentativas { get; set; }
    }
}
