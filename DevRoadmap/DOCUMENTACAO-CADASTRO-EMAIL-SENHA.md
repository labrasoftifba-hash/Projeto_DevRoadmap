# Documentação completa: cadastro, verificação de e-mail e recuperação de senha

**Data desta documentação:** 30/09/2026
**Escopo:** somente as implementações de cadastro de conta, verificação do endereço de e-mail e recuperação/redefinição de senha, incluindo telas, serviços, repositório, modelos, envio por Gmail e SQL.
**Situação informada pelo usuário:** os fluxos funcionaram nos testes. A melhoria de acessibilidade para avançar automaticamente entre os seis dígitos e a regra que impeça nomes duplicados ficam para uma etapa posterior e não fazem parte desta implementação.

---

## 1. Visão geral da arquitetura

O caminho de cada operação segue as camadas do projeto:

```text
Pessoa
  ↓
Página Blazor (Components/Pages/Auth)
  ↓
Service (validação e regras de negócio)
  ↓
UsuarioRepository (consultas e transações SQL)
  ↓
SQL Server
```

O envio de código atravessa uma ramificação adicional:

```text
CadastroService ou RecuperacaoSenhaService
  ↓
GmailEmailSender
  ↓
Gmail API (OAuth)
  ↓
Caixa de e-mail do destinatário
```

Responsabilidades de cada camada:

| Camada | Responsabilidade nesta implementação |
|---|---|
| Componentes Razor | Exibir formulários, validar campos para a interface, coletar os dados e exibir o resultado. |
| Services | Aplicar regras: gerar e validar códigos, limites de validade e tentativas, decidir qual finalidade consultar e coordenar e-mail e banco. |
| Repository | Executar SQL parametrizado, mapear registros para modelos e agrupar alterações relacionadas em transações. |
| Models | Representar usuário e código persistido entre as camadas. |
| GmailEmailSender | Preparar e enviar a mensagem por Gmail usando OAuth e credenciais armazenadas localmente. |
| SQL | Impor integridade referencial, unicidade do e-mail, finalidade válida, limite de tentativas e persistência. |

### Principais rotas

| Rota | Papel |
|---|---|
| `/cadastro` | Criar uma conta pendente e, na mesma página, mostrar os seis campos de confirmação. |
| `/confirmar-email` | Confirmar um cadastro pendente ou validar o código de recuperação. É uma página compartilhada; `fluxo=recuperacao` escolhe o modo da recuperação. |
| `/recuperar-senha` | Receber o e-mail e solicitar um código de recuperação. |
| `/redefinir-senha` | Receber e gravar uma nova senha após o código ser validado. |
| `/login` | Autenticar por e-mail e senha; contas explicitamente pendentes não entram. |

### Duas espécies diferentes de hash

É importante não confundi-las:

1. **Senha:** BCrypt. BCrypt é deliberadamente custoso para dificultar ataques de tentativa em massa caso a tabela de usuários seja exposta.
2. **Código de verificação:** SHA-256. O código é aleatório, curto e de uso temporário; no banco é guardado o hash de 32 bytes, nunca o código em texto. A comparação é feita em tempo constante.

O SHA-256 do código **não substitui** o BCrypt da senha. O hash da senha continua sendo gerado por `UsuarioService.GerarHash`.

---

## 2. Ciclo de vida dos dados

### Cadastro e verificação

```text
Nome, e-mail, senha
  → validar campos
  → BCrypt da senha
  → gerar código aleatório de seis dígitos
  → SHA-256 do código
  → transação: inserir usuário pendente + perfil Estudante + hash do código
  → enviar código puro somente por e-mail
  → pessoa informa os seis dígitos
  → calcular SHA-256 do que foi informado e comparar
  → transação: marcar e-mail verificado + apagar o código
```

### Recuperação de senha

```text
E-mail informado
  → resposta genérica independentemente de haver conta
  → se houver conta elegível: gerar código, persistir o hash e enviar
  → token Data Protection temporário mantém o contexto do fluxo sem revelar o e-mail na URL
  → página compartilhada recebe os seis dígitos
  → verificar código da finalidade RecuperacaoSenha
  → criar token de autorização temporário com ID, e-mail e hash do código
  → pessoa informa e confirma a nova senha
  → conferir senha atual, quando aplicável
  → transação: consumir o código + atualizar a senha para BCrypt
```

O código de recuperação não é apagado somente por ter sido validado: ele é consumido na mesma transação que grava a nova senha. Assim, se a atualização falhar, a transação desfaz também a remoção do código; se tiver sucesso, o mesmo código não pode ser reutilizado.

---

## 3. Arquivos e papel de cada um

### Interface

- [`Components/Pages/Auth/Cadastro.razor`](./Components/Pages/Auth/Cadastro.razor): cadastro e verificação inline.
- [`Components/Pages/Auth/ConfirmarEmail.razor`](./Components/Pages/Auth/ConfirmarEmail.razor): confirmação de cadastro e tela compartilhada de código da recuperação.
- [`Components/Pages/Auth/RecuperarSenha.razor`](./Components/Pages/Auth/RecuperarSenha.razor): solicitação de recuperação.
- [`Components/Pages/Auth/RedefinirSenha.razor`](./Components/Pages/Auth/RedefinirSenha.razor): formulário de senha nova.
- [`Components/Pages/Auth/Login.razor`](./Components/Pages/Auth/Login.razor): autenticação e links para cadastro e recuperação.
- Arquivos `.razor.css` de cada página: apresentação visual isolada; não implementam regras de segurança ou persistência.

### Regras e integração

- [`Services/CadastroService.cs`](./Services/CadastroService.cs): cadastro pendente, confirmação, reenvio e envio de código de cadastro.
- [`Services/RecuperacaoSenhaService.cs`](./Services/RecuperacaoSenhaService.cs): solicitação, confirmação de código e redefinição.
- [`Services/RecuperacaoSenhaTokenService.cs`](./Services/RecuperacaoSenhaTokenService.cs): proteger e recuperar os tokens de contexto e autorização.
- [`Services/CodigoVerificacaoEmail.cs`](./Services/CodigoVerificacaoEmail.cs): geração e hashing dos códigos.
- [`Services/UsuarioService.cs`](./Services/UsuarioService.cs): BCrypt e autenticação.
- [`Services/Email/GmailEmailSender.cs`](./Services/Email/GmailEmailSender.cs): criação e envio da mensagem pela Gmail API.
- [`Repositories/UsuarioRepository.cs`](./Repositories/UsuarioRepository.cs): persistência do usuário, perfil, senha e verificação.
- [`Repositories/BaseRepository.cs`](./Repositories/BaseRepository.cs): carrega `DefaultConnection` da configuração.
- [`Models/Usuario.cs`](./Models/Usuario.cs): dados do usuário, incluindo o estado nullable de verificação.
- [`Models/VerificacaoEmail.cs`](./Models/VerificacaoEmail.cs): dados de uma linha de verificação.
- [`Program.cs`](./Program.cs): registra serviços, Data Protection e componentes interativos.
- [`DevRoadmap.csproj`](./DevRoadmap.csproj): referências aos pacotes BCrypt, Gmail API e SQL Client.

### SQL

- [`Database/01_Usuarios_E_Perfis.sql`](./Database/01_Usuarios_E_Perfis.sql): criação inicial de usuários, perfis e vínculo entre eles.
- [`Database/02_Verificacao_Email.sql`](./Database/02_Verificacao_Email.sql): extensão aditiva para estado de verificação e tabela dos códigos.
- [`Database/03_Finalidade_Verificacao_Email.sql`](./Database/03_Finalidade_Verificacao_Email.sql): separa os códigos de cadastro dos de recuperação. O usuário informou que esta migração foi executada.

---

## 4. Cadastro e confirmação de endereço de e-mail

### 4.1 `Cadastro.razor`

#### `CadastroModel`

O modelo local contém:

- `Etapa`: indica se a página está no cadastro ou na confirmação.
- `Nome`, `Email`, `Senha`: entrada de cadastro.
- `Codigo1` a `Codigo6`: seis posições do código.

As propriedades começam com valores vazios. Isso dá ao `EditForm` um modelo disponível antes do primeiro render e evita um `EditForm` sem `Model`/`EditContext`.

#### `EtapaConfirmacao`

Compara `Model.Etapa` com `"confirmacao"` e decide qual conjunto de campos mostrar. A tela continua na mesma rota e mantém o e-mail no modelo; não pede para digitá-lo de novo depois do cadastro.

#### `OnInitialized`

Executa `Model ??= new()`. No POST, o Blazor pode preencher o modelo por `[SupplyParameterFromForm]`; no primeiro GET, quando ainda não há dados submetidos, cria o modelo vazio.

#### `[SupplyParameterFromForm(FormName = "cadastro")]`

Associa os campos do POST ao modelo correto. Os dois `EditForm` condicionais — formulário de cadastro e de confirmação — usam o mesmo nome porque são etapas alternativas da mesma página, não formulários simultâneos.

O `FormName` no `EditForm` também é renderizado no formulário enviado. Isso permite ao endpoint Razor Components descobrir qual formulário recebeu o POST.

#### `HandleCadastro`

1. Exige que o modelo exista.
2. Marca o processamento como ativo para desabilitar o botão enquanto a operação ocorre.
3. Chama `CadastroService.CadastrarUsuarioAsync` com nome, e-mail e senha.
4. Se o serviço disser que enviou o código ou criou a conta mas falhou no envio, muda `Etapa` para `"confirmacao"`, apresenta a mensagem correspondente e limpa os dígitos.
5. Se o e-mail já estiver cadastrado, mostra a mensagem e habilita o caminho alternativo para abrir a confirmação.
6. Em `finally`, sempre desmarca o estado de processamento, inclusive se ocorrer uma exceção.

O caso `ContaCriadaEnvioFalhou` é importante: a falha de envio não apaga a conta nem finge que o e-mail saiu. A página explica que a conta foi criada e oferece o reenvio.

#### `HandleConfirmacao`

1. Junta as seis strings em ordem, sem converter para inteiro. Assim, um código que começa com zero continua tendo seis posições.
2. Chama `CadastroService.ConfirmarEmail(email, codigo)`.
3. Converte o enum de resultado em uma mensagem específica para sucesso, código inválido, expirado, limite de tentativas ou falha genérica.
4. Se a confirmação não deu certo, limpa as seis caixas para nova tentativa.
5. Usa `try/finally` para restabelecer o estado do botão.

#### `HandleReenvio`

Chama `CadastroService.ReenviarCodigoAsync` com o e-mail mantido no modelo. Informa se o código foi reenviado, se ainda não passou o intervalo mínimo, se o envio falhou ou se não foi possível atender aos dados. Quando um código novo é enviado, limpa as caixas; o código anterior já foi substituído no banco.

#### `LimparCodigo`

Zera `Codigo1` a `Codigo6`. Evita que um código inválido ou antigo permaneça preenchido depois de uma tentativa ou de um reenvio.

#### `CadastroModel.Validate`

Implementa validação condicional com `IValidatableObject`:

- Em ambas as etapas, valida o formato básico do e-mail e o limite de 250 caracteres.
- Na etapa de confirmação, exige que cada posição tenha exatamente um caractere ASCII entre `0` e `9`; depois encerra a validação sem exigir nome ou senha.
- Na etapa de cadastro, exige nome não vazio e até 150 caracteres depois de `Trim`.
- Exige senha não vazia e com pelo menos oito caracteres.

Essa validação de tela melhora o retorno ao usuário. O service e o banco continuam fazendo validações próprias; validação da interface não é uma barreira de segurança suficiente isoladamente.

### 4.2 `CadastroService`

#### Enums de resultado

- `ResultadoCadastro`: diferencia código enviado, e-mail já existente, conta criada mas envio falhou e dados inválidos.
- `ResultadoConfirmacaoEmail`: diferencia confirmação, código inválido/expirado, limite de tentativas, conta não pendente e resultados de reenvio.

Os enums permitem que a página mostre textos apropriados sem duplicar a lógica de SQL, e-mail ou expiração.

#### Construtor

Recebe por injeção de dependência `UsuarioRepository`, `GmailEmailSender` e `ILogger<CadastroService>`. Assim, o service orquestra as camadas; não cria manualmente conexões SQL nem clientes Gmail.

#### Constantes

- Validade: 15 minutos.
- Intervalo entre reenvios: 60 segundos.
- Máximo: cinco tentativas.
- Comprimento do código: seis dígitos.

O repositório também condiciona alterações diretamente ao prazo e ao limite, de modo que não se depende apenas da lógica da página.

#### `CadastrarUsuarioAsync`

1. Recusa nome, e-mail ou senha vazios.
2. Gera código de seis dígitos por `CodigoVerificacaoEmail.GerarCodigo`.
3. Obtém a hora em UTC e calcula expiração.
4. Cria `Usuario`, removendo espaços externos de nome e e-mail e usando `UsuarioService.GerarHash` para a senha.
5. Calcula o hash SHA-256 do código e chama `CadastrarPendente`.
6. Se o repositório retornar `0`, informa que o endereço já está cadastrado (colisão com restrição única).
7. Caso contrário, chama `EnviarCodigoComResultadoAsync`.

A persistência precede o envio porque o e-mail só deve ser enviado depois que o hash e o usuário já existem. O destinatário recebe o código puro, mas ele não é persistido.

#### `ConfirmarEmail`

1. Rejeita e-mail vazio.
2. Procura o usuário por e-mail.
3. Só prossegue se `EmailVerificado` for explicitamente `false`, isto é, conta pendente de confirmação.
4. Busca a linha de finalidade `Cadastro`.
5. Confere expiração e máximo de tentativas.
6. Valida comprimento e caracteres ASCII numéricos.
7. Compara o SHA-256 informado com o hash armazenado usando `FixedTimeEquals`.
8. Em caso de divergência, chama `RegistrarTentativaInvalida`.
9. Em caso de correspondência, chama `ConfirmarEmailEConsumirCodigo`, que atualiza e consome sob uma transação no SQL Server.

A segunda validação no repositório protege contra corrida: entre a leitura do código no service e a transação final, ele pode ter expirado, sido substituído ou ter alcançado o limite.

#### `ReenviarCodigoAsync`

1. Respeita cancelamento solicitado.
2. Rejeita e-mail vazio, usuário inexistente ou conta que não está pendente.
3. Gera outro código; calcula hash e nova expiração.
4. Chama `AtualizarCodigoVerificacaoEmail`, passando o instante-limite (`agora - 60 segundos`).
5. O SQL só atualiza a linha se o envio anterior já tiver idade de pelo menos 60 segundos; atualização também zera tentativas e troca hash/expiração.
6. Se o update foi aceito, envia o novo código.

O valor de retorno diferencia espera, envio concluído e falha de envio. Um código novo invalida o anterior, porque apenas o hash novo fica no banco.

#### `RegistrarTentativaInvalida`

Pede ao repositório para incrementar `Tentativas` apenas se ainda não atingiu cinco e se não expirou. Depois relê o contador para distinguir código inválido do limite recém-alcançado. Se o update não ocorrer, relê para distinguir limite de expiração/ausência da linha.

#### `EnviarCodigoComResultadoAsync`

Cria assunto e corpo do e-mail, envia por `GmailEmailSender.EnviarEmailAsync` e transforma os erros operacionais conhecidos em logs associados ao ID do usuário. Retorna `CodigoEnviado` somente quando a API terminou o envio; nos erros conhecidos retorna o resultado de falha usado para apresentar a mensagem correta.

As exceções tratadas são `GoogleApiException`, `IOException`, `HttpRequestException` e `InvalidOperationException`. Não há um `catch` geral que esconda erros inesperados.

---

## 5. Geração e comparação do código

### `CodigoVerificacaoEmail.GerarCodigo`

Usa `RandomNumberGenerator.GetInt32(0, 1_000_000)`, gerador criptograficamente seguro, e formata com `"D6"`. `"D6"` completa com zeros à esquerda: o intervalo representa todos os códigos de `000000` a `999999`.

O código continua sendo string para não perder zeros iniciais.

### `CalcularHash`

Codifica os seis dígitos em UTF-8 e calcula `SHA256.HashData`. O resultado tem 32 bytes, compatível com `VARBINARY(32)`.

### `Corresponde`

Calcula novamente o SHA-256 e compara com o valor do banco usando `CryptographicOperations.FixedTimeEquals`. A comparação em tempo constante reduz diferenças temporais que poderiam revelar quantos bytes coincidem.

### `ValidarHashCodigo` no repositório

Recusa `null` e qualquer hash que não tenha exatamente 32 bytes antes de enviá-lo ao SQL. Essa checagem previne persistir valores com forma inesperada.

---

## 6. Envio da mensagem com Gmail

### `GmailEmailSender` e segurança das credenciais

O remetente guarda os dados locais em `%LOCALAPPDATA%\DevRoadmap\Google\`:

- `web-client-secret.json`: credenciais OAuth do cliente.
- `tokens\`: armazenamento local de tokens autorizados, por `FileDataStore`.

Esses arquivos contêm material sensível e não devem ser copiados para o repositório ou compartilhados. Esta documentação não inclui seus conteúdos.

#### Construtor

Monta a pasta local dos tokens e cria um `FileDataStore`. Não coloca segredos no código-fonte.

#### `ObterCaminhoCredenciais`

Monta o caminho esperado do arquivo OAuth e verifica que ele existe. Se não existir, lança `FileNotFoundException` com o local esperado, em vez de prosseguir com uma configuração incompleta.

#### `EnviarEmailAsync`

1. Valida destinatário com `MailAddress.TryCreate`.
2. Exige assunto e corpo não vazios.
3. Recusa quebras de linha no assunto para não permitir injeção de cabeçalhos.
4. Cria o fluxo OAuth limitado ao escopo `GmailSend`.
5. Carrega o `TokenResponse` do armazenamento local e exige refresh token autorizado.
6. Cria `UserCredential` e `GmailService`.
7. Prepara o MIME e invoca `Users.Messages.Send(..., "me").ExecuteAsync(cancellationToken)`.

`"me"` representa a conta Gmail que autorizou o OAuth; não é o destinatário.

#### `CriarFluxoAutorizacao`

Abre o arquivo de credenciais e configura `GoogleAuthorizationCodeFlow` com o escopo de envio e o data store local. É separado para que a configuração OAuth fique concentrada.

#### `CriarMensagemRaw`

Codifica assunto e corpo em UTF-8/Base64, monta cabeçalhos MIME (`From`, `To`, `Subject`, tipo de conteúdo e transferência) e converte a mensagem inteira para Base64 URL-safe, formato requerido pela API Gmail.

O corpo é texto simples; assunto e corpo aceitam UTF-8.

---

## 7. SQL: tabelas e migrações

### 7.1 `01_Usuarios_E_Perfis.sql`

#### `Perfis`

```sql
CREATE TABLE Perfis (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Cargo NVARCHAR(50) NOT NULL UNIQUE
);
```

- `IDENTITY` gera ID crescente.
- `PRIMARY KEY` identifica cada perfil.
- `Cargo` não pode ser nulo e é único.

#### `Usuarios`

```sql
CREATE TABLE Usuarios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nome NVARCHAR(150) NOT NULL,
    Email NVARCHAR(250) NOT NULL UNIQUE,
    Senha NVARCHAR(255) NOT NULL,
    DataCadastro DATETIME NOT NULL DEFAULT GETDATE()
);
```

- Um e-mail só pode aparecer uma vez por causa de `UNIQUE`.
- Não há restrição de unicidade para `Nome`; nomes repetidos são permitidos neste estado.
- `Senha` guarda o hash BCrypt, não a senha digitada.
- `GETDATE()` fornece data local do servidor SQL para o cadastro.

#### `UsuariosPerfis`

```sql
CREATE TABLE UsuariosPerfis (
    UsuarioId INT NOT NULL,
    PerfilId INT NOT NULL,
    CONSTRAINT PK_UsuariosPerfis PRIMARY KEY (UsuarioId, PerfilId),
    CONSTRAINT FK_UsuariosPerfis_Usuario FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
    CONSTRAINT FK_UsuariosPerfis_Perfil FOREIGN KEY (PerfilId) REFERENCES Perfis(Id)
);
```

A chave composta impede duplicar o mesmo vínculo usuário/perfil. As chaves estrangeiras impedem vínculos a entidades inexistentes.

O seed insere `Estudante`, `Professor` e `Administrador`. O fluxo de cadastro busca o ID de `Estudante` e o associa à conta nova.

### 7.2 `02_Verificacao_Email.sql`

#### Coluna de verificação

`COL_LENGTH` verifica se `EmailVerificado` já existe. Só então é adicionada:

```sql
ALTER TABLE dbo.Usuarios
ADD EmailVerificado BIT NULL;
```

O tipo `BIT` representa verdadeiro/falso. A coluna é nullable deliberadamente para preservar o estado de contas anteriores à funcionalidade: `NULL` significa que a conta é antiga e ainda não foi classificada pelo novo campo; `0` significa pendente; `1` significa verificada.

O bloco seguinte consulta `sys.default_constraints` e `sys.columns` antes de criar o default:

```sql
ALTER TABLE dbo.Usuarios
ADD CONSTRAINT DF_Usuarios_EmailVerificado
    DEFAULT (0) FOR EmailVerificado;
```

Esse default aplica-se a inserções futuras que omitam a coluna. Como a coluna é nullable e não se executa um `UPDATE` geral sobre os registros antigos, contas legadas ficam com `NULL`; isso evita tratar automaticamente todos os usuários antigos como não verificados.

#### Tabela de verificação

```sql
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
```

- `CodigoHash VARBINARY(32)`: guarda os 32 bytes SHA-256.
- `ExpiraEm`: instante até o qual o código é aceito.
- `Tentativas`: contador, com default zero e limite máximo no banco.
- `CriadoEm`: usado para controlar quando pode haver novo envio.
- A chave inicial permitia apenas uma verificação por usuário; foi ampliada na migração 03.
- A foreign key liga cada código a um usuário real.

### 7.3 `03_Finalidade_Verificacao_Email.sql`

#### Adição da finalidade

Se ainda não existir, adiciona:

```sql
Finalidade VARCHAR(20) NOT NULL
    CONSTRAINT DF_VerificacoesEmail_Finalidade DEFAULT ('Cadastro') WITH VALUES
```

`WITH VALUES` atribui `'Cadastro'` às linhas que já estavam na tabela. Portanto, códigos antigos continuam classificados como códigos de cadastro; não são reinterpretados como recuperação.

#### Mudança da chave primária

O script consulta `sys.key_constraints`, `sys.index_columns` e `sys.columns` para saber se já existe uma chave primária que contenha exatamente as duas colunas `UsuarioId` e `Finalidade`.

Se essa chave composta não existir:

1. Lê o nome real da PK atual em `sys.key_constraints`.
2. Monta `ALTER TABLE ... DROP CONSTRAINT` usando `QUOTENAME` para delimitar o nome.
3. Executa o comando dinâmico com `sys.sp_executesql`.
4. Cria a chave:

```sql
PRIMARY KEY (UsuarioId, Finalidade)
```

A finalidade passa a permitir, por usuário, uma linha para cadastro e outra para recuperação. O uso de SQL dinâmico é necessário porque o nome da restrição varia; `QUOTENAME` protege o identificador e `sp_executesql` executa o texto construído.

#### Restrição de domínio

O bloco consulta `sys.check_constraints` para não duplicar a restrição. Depois, se necessário, adiciona:

```sql
CHECK (Finalidade IN ('Cadastro', 'RecuperacaoSenha'))
```

Impede que uma linha seja gravada com grafia ou finalidade não reconhecida. O código C# faz mapeamento explícito do enum para exatamente essas strings.

#### `GO`

`GO` separa lotes de execução do cliente SQL; não é uma instrução T-SQL executada pelo servidor. O script foi dividido em lotes para que cada verificação/alteração rode na ordem apropriada.

---

## 8. SQL executado pelo `UsuarioRepository`

Todas as consultas relevantes usam parâmetros SQL (`@Nome`, `@Email` etc.), em vez de concatenar valores recebidos da página na string SQL. O SQL Server recebe tipos definidos no código para hash, datas e IDs; isso também evita que uma entrada seja interpretada como parte da instrução.

### 8.1 `CadastrarPendente` e método privado `Cadastrar`

O caminho de cadastro usa `CadastrarPendente`, que valida que o hash tem 32 bytes e delega ao método privado comum com hash e expiração.

#### Inserir usuário e devolver a chave

```sql
INSERT INTO Usuarios (Nome, Email, Senha)
OUTPUT INSERTED.Id
VALUES (@Nome, @Email, @Senha)
```

`OUTPUT INSERTED.Id` devolve o ID criado no mesmo comando. O serviço precisa dele para inserir perfil e verificação sem fazer uma segunda busca por e-mail.

Se SQL Server reportar os números `2601` ou `2627` — violação de índice/restrição única, neste caso o e-mail — o repository retorna `0`, e o service converte isso em `EmailJaCadastrado`.

#### Encontrar perfil padrão

```sql
SELECT Id FROM Perfis WHERE Cargo = @Cargo
```

O parâmetro recebe `"Estudante"`. Se o seed estiver ausente, o repositório lança um erro explícito em vez de criar uma conta sem o perfil exigido.

#### Vincular usuário ao perfil

```sql
INSERT INTO UsuariosPerfis (UsuarioId, PerfilId)
VALUES (@UsuarioId, @PerfilId)
```

#### Inserir verificação inicial

```sql
INSERT INTO VerificacoesEmail (UsuarioId, Finalidade, CodigoHash, ExpiraEm)
VALUES (@UsuarioId, 'Cadastro', @CodigoHash, @ExpiraEm)
```

O hash é enviado como `VARBINARY(32)`. A finalidade é literal `'Cadastro'`, porque esse método registra apenas a confirmação inicial.

Os três passos — usuário, vínculo e código — ocorrem dentro da mesma transação. `Commit` só acontece depois dos três; se uma operação falhar, a transação é descartada quando fecha, evitando usuário criado pela metade.

### 8.2 `ObterPorEmail`

```sql
SELECT Id, Nome, Email, Senha, DataCadastro, EmailVerificado
FROM Usuarios
WHERE Email = @Email
```

Mapeia os campos para `Usuario`. Para `EmailVerificado`, consulta `IsDBNull` e preserva `NULL` como `bool? null`; isso é necessário para distinguir conta antiga (`NULL`) de conta pendente (`false`).

Usado pelo login, cadastro repetido, verificação de e-mail e recuperação de senha.

### 8.3 `AtualizarCodigoVerificacaoEmail`

```sql
UPDATE VerificacoesEmail
SET CodigoHash = @CodigoHash,
    ExpiraEm = @ExpiraEm,
    Tentativas = 0,
    CriadoEm = SYSUTCDATETIME()
WHERE UsuarioId = @UsuarioId
  AND Finalidade = @Finalidade
  AND CriadoEm <= @ReenviarAntesDe
```

Troca o hash, renova a validade, zera tentativas e registra o instante UTC. O predicado da última linha só permite a atualização se o código anterior foi criado há pelo menos 60 segundos. O retorno é verdadeiro se exatamente uma linha foi alterada.

### 8.4 `ObterCodigoVerificacaoEmail`

```sql
SELECT UsuarioId, Finalidade, CodigoHash, ExpiraEm, Tentativas
FROM VerificacoesEmail
WHERE UsuarioId = @UsuarioId
  AND Finalidade = @Finalidade
```

Busca uma finalidade específica e mapeia a string do banco de volta para `FinalidadeVerificacaoEmail`. Se a finalidade armazenada for desconhecida, lança erro explícito. Se não encontrar linha, retorna `null`.

### 8.5 `CriarCodigoRecuperacaoSenha`

O método abre uma transação `Serializable` e realiza leituras com `UPDLOCK, HOLDLOCK`. Isso reduz a possibilidade de duas solicitações simultâneas ultrapassarem a verificação de intervalo ou tentarem inserir a mesma chave composta.

#### Elegibilidade da conta

```sql
SELECT EmailVerificado
FROM Usuarios WITH (UPDLOCK, HOLDLOCK)
WHERE Id = @UsuarioId
  AND (EmailVerificado IS NULL OR EmailVerificado = 1)
```

A intenção do filtro é admitir conta verificada e conta legada nullable, rejeitando `0` (conta pendente de confirmação). O resultado lido é usado antes de prosseguir.

#### Ler instante da solicitação de recuperação anterior

```sql
SELECT CriadoEm
FROM VerificacoesEmail WITH (UPDLOCK, HOLDLOCK)
WHERE UsuarioId = @UsuarioId
  AND Finalidade = 'RecuperacaoSenha'
```

Se já existir registro recente, o método faz `Rollback` e informa que não criou novo código. Se já se passaram pelo menos 60 segundos, atualiza o registro existente; se não existir, insere a primeira linha.

#### Atualizar código existente

```sql
UPDATE VerificacoesEmail
SET CodigoHash = @CodigoHash,
    ExpiraEm = @ExpiraEm,
    Tentativas = 0,
    CriadoEm = SYSUTCDATETIME()
WHERE UsuarioId = @UsuarioId
  AND Finalidade = 'RecuperacaoSenha'
```

#### Inserir primeiro código de recuperação

```sql
INSERT INTO VerificacoesEmail (UsuarioId, Finalidade, CodigoHash, ExpiraEm)
VALUES (@UsuarioId, 'RecuperacaoSenha', @CodigoHash, @ExpiraEm)
```

O `Commit` publica a alteração depois que o registro ficou consistente; os retornos que recusam conta/intervalo fazem `Rollback`.

### 8.6 `IncrementarTentativasVerificacaoEmail`

```sql
UPDATE VerificacoesEmail
SET Tentativas = Tentativas + 1
WHERE UsuarioId = @UsuarioId
  AND Finalidade = @Finalidade
  AND Tentativas < 5
  AND ExpiraEm > @Agora
```

Um único `UPDATE` atômico incrementa somente código existente, não expirado e abaixo do limite. O número de linhas alteradas informa ao service se a tentativa foi contabilizada.

### 8.7 `ConfirmarEmailEConsumirCodigo`

Executa uma transação `Serializable`.

Primeiro tenta marcar a conta pendente:

```sql
UPDATE Usuarios
SET EmailVerificado = 1
WHERE Id = @UsuarioId
  AND EmailVerificado = 0
```

Depois consome exatamente o código de cadastro:

```sql
DELETE FROM VerificacoesEmail
WHERE UsuarioId = @UsuarioId
  AND Finalidade = 'Cadastro'
  AND CodigoHash = @CodigoHash
  AND ExpiraEm > @Agora
  AND Tentativas < 5
```

Os dois comandos precisam afetar exatamente uma linha. Caso contrário, o repository chama `Rollback`, que desfaz também a primeira alteração se ela já ocorreu. Em caso de sucesso, `Commit` grava a verificação e a remoção do código juntas.

### 8.8 `RedefinirSenhaComCodigo`

Também executa uma transação `Serializable`.

Consome o código de recuperação:

```sql
DELETE FROM VerificacoesEmail
WHERE UsuarioId = @UsuarioId
  AND Finalidade = 'RecuperacaoSenha'
  AND CodigoHash = @CodigoHash
  AND ExpiraEm > @Agora
  AND Tentativas < 5
```

Em seguida grava o hash BCrypt:

```sql
UPDATE Usuarios
SET Senha = @Senha,
    EmailVerificado = 1
WHERE Id = @UsuarioId
  AND (EmailVerificado IS NULL OR EmailVerificado = 1)
```

Se a remoção ou o update afetar zero ou mais de uma linha, há rollback. Só o `Commit` final torna simultaneamente efetivos o consumo do código e a senha nova.

### 8.9 Métodos auxiliares

- `ObterNomeFinalidade`: traduz `Cadastro` para `'Cadastro'` e `RecuperacaoSenha` para `'RecuperacaoSenha'`; enum não reconhecido gera `ArgumentOutOfRangeException`.
- `ObterFinalidade`: faz o mapeamento inverso; string no banco fora do domínio gera `InvalidOperationException`.
- `ValidarHashCodigo`: exige bytes não nulos e comprimento 32.
- `Cadastrar`: transação compartilhada por cadastro normal e cadastro pendente. Na implementação atual da confirmação, o service usa o caminho pendente.
- `AtualizarSenha`: atualização simples antiga por ID, com checagem de exatamente uma linha. A redefinição de senha com código usa `RedefinirSenhaComCodigo` para garantir atomicidade; não usa este método simples.
- `Cadastrar_Relacao_Perfil` e `Pegar_PerfilID`: helpers preexistentes de vínculo de perfil. O novo caminho transacional de cadastro faz as operações diretamente para manter usuário, perfil e código na mesma transação.

---

## 9. Login e armazenamento de senha

### `UsuarioService.GerarHash`

Confere que o valor não é nulo/vazio e chama `BCrypt.Net.BCrypt.HashPassword`. O resultado BCrypt inclui salt e parâmetros de custo; não se armazena a senha original.

### `UsuarioService.Autenticar`

1. Recusa e-mail ou senha vazios.
2. Busca o usuário com `ObterPorEmail`.
3. Exige hash que comece com o prefixo BCrypt esperado (`$2`) e valida a senha por `BCrypt.Verify`.
4. Se a senha confere, recusa somente `EmailVerificado == false`.
5. `EmailVerificado == true` é verificado e `null` é tratado como conta antiga, mantendo compatibilidade com registros criados antes da coluna.
6. Retorna `null` para credenciais inválidas ou conta pendente, de modo que a tela não revela qual dos dados estava errado.

O fallback antigo de SHA-256/texto puro para senha foi removido; SHA-256 permanece apenas no código temporário de e-mail.

---

## 10. Recuperação e redefinição de senha

### 10.1 `RecuperarSenha.razor`

#### `RecoveryModel`

Tem `Email` com `[Required]`, `[EmailAddress]` e limite de 250 caracteres. Essas anotações fornecem validação antes do handler e mensagens associadas ao campo.

#### `OnInitialized`

Cria um modelo quando a página abre por GET; no POST, o modelo é preenchido por `[SupplyParameterFromForm(FormName = "solicitar-recuperacao")]`.

#### `EnviarCodigo`

1. Chama `RecuperacaoSenhaService.SolicitarCodigoAsync`.
2. Gera token protegido de fluxo a partir do e-mail submetido.
3. Navega para `/confirmar-email?fluxo=recuperacao&token=...`.

O token de e-mail é opaco, protegido pelo Data Protection e válido por 15 minutos. O endereço original não aparece como texto na URL. `Uri.EscapeDataString` codifica o token para que possa ser transportado como valor de query string.

### 10.2 `RecuperacaoSenhaService`

#### Enums e resultado

- `ResultadoCodigoRecuperacao`: confirmado, inválido, expirado ou limite alcançado.
- `ResultadoRedefinicaoSenha`: redefinida, igual à senha atual ou autorização/código expirado.
- `ResultadoConfirmacaoRecuperacao`: encapsula o estado da validação e, se confirmado, o token de autorização a levar à tela seguinte.

#### `SolicitarCodigoAsync`

1. Confere cancelamento e e-mail não vazio.
2. Busca a conta pelo e-mail.
3. Não envia para conta inexistente nem conta com `EmailVerificado == false`.
4. Gera o código e o seu hash, define a validade de 15 minutos e pede ao repository para criar/atualizar sob transação.
5. Se o repository não aceitar (por exemplo, intervalo de 60 segundos), retorna sem enviar.
6. Se persistiu, envia assunto e mensagem de recuperação pelo Gmail.
7. Registra erros conhecidos com ID de usuário, sem registrar código ou senha.

A página sempre segue para a confirmação e mostra uma mensagem genérica, mesmo que a conta não exista; isso evita usar a interface para enumerar endereços cadastrados.

#### `CriarTokenFluxo` e `TentarObterEmailFluxo`

São delegações para `RecuperacaoSenhaTokenService`: uma protege o e-mail para carregar o contexto entre a solicitação e a confirmação; a outra valida/desprotege o token no GET inicial da página compartilhada.

#### `ConfirmarCodigo`

1. Busca conta e linha de finalidade `RecuperacaoSenha`.
2. Rejeita conta inexistente/não elegível, código ausente, expiração e limite.
3. Valida seis dígitos e compara SHA-256.
4. Em entrada incorreta, atualiza as tentativas e calcula o resultado.
5. Em sucesso, cria token de autorização Data Protection, de validade igual ao tempo restante até a expiração da linha.
6. Retorna estado e token para a página.

O token de autorização carrega ID, e-mail e hash SHA-256 já validado, nunca o código em texto. A autorização ainda não altera a senha; a operação final revalida e consome a linha de banco.

#### `TokenAutorizacaoValido`

Testa se o token existe, pode ser decodificado, continua válido no Data Protection e possui o conteúdo esperado. Serve à tela para decidir se pode mostrar o formulário da nova senha; a transação de SQL é a autoridade final.

#### `RedefinirSenha`

1. Exige senha nova.
2. Valida/desprotege token e obtém ID, e-mail e hash de código.
3. Busca usuário e confirma que o ID corresponde ao token e que a conta não está pendente.
4. Se a senha atual é BCrypt, usa `BCrypt.Verify` para impedir definir a mesma senha.
5. Gera o BCrypt da senha nova.
6. Chama `RedefinirSenhaComCodigo`, que no banco verifica validade/tentativas, apaga o código e atualiza a senha sob uma transação.
7. Retorna `Redefinida` ou `CodigoExpirado`.

Se a senha submetida for igual à atual, retorna antes do repositório; a pessoa pode escolher outra sem consumir o código de recuperação válido.

#### `RegistrarTentativaInvalida`

Incrementa tentativas para finalidade `RecuperacaoSenha`, relê a linha e distingue limite alcançado de inválido/expirado.

### 10.3 `RecuperacaoSenhaTokenService`

Usa `IDataProtectionProvider` para obter dois protetores com propósitos separados:

- `DevRoadmap.RecuperacaoSenha.Email.v1`
- `DevRoadmap.RecuperacaoSenha.Autorizacao.v1`

Separar os propósitos impede que um token de um tipo seja aceito como se fosse o outro.

#### `ProtegerEmail`

Serializa um `EmailPayload` em JSON UTF-8, protege por 15 minutos e codifica os bytes protegidos em Base64 para transporte na URL.

#### `TentarObterEmail`

Decodifica Base64, pede ao Data Protection para validar e desproteger dentro do prazo, desserializa o e-mail e retorna `false` em token inválido, expirado ou malformado. Não transforma uma falha em um endereço fictício.

#### `CriarTokenAutorizacao`

1. Valida e-mail e hash de exatamente 32 bytes.
2. Calcula validade restante usando `expiraEm - UtcNow`; não cria token se o código já expirou.
3. Serializa ID do usuário, e-mail e hash em payload.
4. Protege pelo tempo restante e retorna Base64.

#### `TentarObterAutorizacao`

Decodifica e desprotege o token; verifica ID positivo, e-mail e hash presentes; decodifica o hash e exige 32 bytes. Trata falhas criptográficas, JSON malformado e Base64 inválida com retorno `false`.

#### Registros de payload

- `EmailPayload`: e-mail da pessoa durante a passagem para a página compartilhada.
- `AuthorizationPayload`: ID, e-mail e hash do código validado.
- `AutorizacaoRecuperacao`: tipo interno fortemente tipado que o service recebe após validar o token.

### 10.4 `ConfirmarEmail.razor` como tela compartilhada

#### Seleção de modo

`Fluxo` é lido da query string; `fluxo=recuperacao` seleciona recuperação. O modelo também tem `FluxoRecuperacao`, enviado em campo oculto para que o modo não se perca no POST tradicional.

#### `OnInitialized`

- Cria `ConfirmacaoModel` se necessário.
- No GET inicial de recuperação, valida o token protegido e extrai o e-mail.
- Se token for inválido ou expirado, informa que a solicitação deve ser reiniciada.
- No cadastro, a pessoa informa o e-mail na própria página de confirmação.

#### `ConfirmacaoModel`

Contém o modo, o e-mail e seis dígitos. Cada dígito tem `[Required]` e `RegularExpression` para um dígito (`^\d$`). É o mesmo modelo visual, mas o service escolhido muda conforme o modo.

#### `HandleConfirmacao`

1. Junta as posições.
2. Se o modo for recuperação, chama `RecuperacaoSenhaService.ConfirmarCodigo`; em sucesso navega para redefinição com token de autorização protegido na query string; em falha apresenta o resultado e limpa os dígitos.
3. Caso contrário, chama `CadastroService.ConfirmarEmail`, apresenta a mensagem correspondente e marca sucesso na interface.
4. Usa `finally` para liberar o estado de processamento.

O token que vai para a redefinição é protegido e não contém a senha nova. A senha ainda será digitada no próximo formulário.

#### `HandleReenvio`

- Recuperação: chama novamente `SolicitarCodigoAsync` usando o e-mail do modelo e mantém mensagem genérica.
- Cadastro: verifica formato do e-mail, chama `ReenviarCodigoAsync` e exibe resultado específico.
- Limpa dígitos após reenvio bem-sucedido ou solicitação de recuperação.

#### Correção da interação dos seis campos

As caixas são `InputText` vinculadas a strings. Foi removida a configuração `@bind-Value:event="oninput"`, que produzia incompatibilidade entre `ChangeEventArgs` e o callback string do `InputText`, derrubando o circuito. As caixas usam agora o evento padrão do componente (`onchange`). Isso permite digitar e submeter corretamente; avançar automaticamente ao próximo campo é uma melhoria de acessibilidade/UX planejada para depois.

### 10.5 `RedefinirSenha.razor`

#### `ResetModel`

- `TokenAutorizacao`: token protegido enviado como campo oculto.
- `SenhaNova`: obrigatória e com mínimo de oito caracteres.
- `ConfirmacaoSenha`: obrigatória e deve ser igual a `SenhaNova`, por `[Compare]`.

#### `OnInitialized`

Cria o modelo e copia o token recebido pela query string para ele. Após o POST, o campo oculto mantém o token associado aos dados submetidos.

#### `TokenAutorizacaoValido`

Consulta o service antes de mostrar o formulário. Token ausente/inválido/expirado mostra um link para iniciar nova recuperação.

#### `SalvarNovaSenha`

Chama o service com token e senha nova, mostra sucesso ou mensagens de senha igual/expiração e, se expirou, encaminha a pessoa para iniciar outro fluxo.

---

## 11. Registro e configuração em `Program.cs`

- `AddDataProtection()`: registra a infraestrutura que assina/protege os tokens do fluxo de recuperação.
- `AddRazorComponents().AddInteractiveServerComponents()`: configura componentes Blazor interativos no servidor.
- `AddScoped<UsuarioRepository>()`: um repository por escopo/request/circuito.
- `AddScoped<UsuarioService>()`, `AddScoped<CadastroService>()`, `AddScoped<RecuperacaoSenhaService>()`: regras de negócio por escopo.
- `AddSingleton<RecuperacaoSenhaTokenService>()`: serviço sem estado por usuário; recebe protetores do provider e pode ser compartilhado com segurança entre chamadas.
- `AddSingleton<GmailEmailSender>()`: remetente sem estado da requisição; usa token store local e cliente Gmail criado para a operação.
- `UseAntiforgery()`: habilita proteção antifalsificação para POSTs dos formulários.
- `MapRazorComponents<App>().AddInteractiveServerRenderMode()`: mapeia a aplicação e o modo interativo.

Em implantação com múltiplas instâncias ou servidores, as chaves do Data Protection precisam ser persistidas e compartilhadas de forma segura. Se o conjunto de chaves for perdido ou diferente, tokens de recuperação ainda dentro do prazo deixam de ser válidos; isso não altera senhas, apenas exige iniciar novamente o fluxo.

---

## 12. Formulários Razor e o erro anterior

Todo `EditForm` de POST precisa ter um nome (`FormName`) que corresponda a `[SupplyParameterFromForm]` na propriedade do modelo:

| Tela | Nome do formulário | Propriedade alimentada |
|---|---|---|
| Cadastro e etapa de código | `cadastro` | `CadastroModel` |
| Login | `login` | `LoginModel` |
| Código (cadastro/recuperação) | `confirmar-codigo` | `ConfirmacaoModel` |
| Solicitação de recuperação | `solicitar-recuperacao` | `RecoveryModel` |
| Senha nova | `redefinir-senha` | `ResetModel` |

A página não deve conter dois formulários com o mesmo nome ao mesmo tempo. No cadastro eles estão em blocos condicionais e apenas um é renderizado por vez.

O erro observado tinha duas causas diferentes em momentos distintos:

1. POST sem nome identificador: a versão antiga servida não mostrava `_handler`; a versão atual renderizava o nome do formulário.
2. Exceção ao digitar código: `InputText` estava vinculado a `oninput` de forma incompatível; removemos o evento explícito e testamos a interação nos seis campos.

Separar as causas evitou tratar uma falha de evento como se fosse necessariamente falha de formulário.

---

## 13. Segurança e limites conhecidos

1. **Código aleatório:** gerado por fonte criptográfica; não é sequencial.
2. **Código em repouso:** somente SHA-256 no banco; código puro só transita para o e-mail e para o formulário digitado.
3. **Comparação:** `FixedTimeEquals`.
4. **Senha:** BCrypt; nunca é enviada por e-mail.
5. **Uso único:** confirmação de cadastro e conclusão da recuperação apagam o código dentro da transação.
6. **Prazo:** código do banco expira em 15 minutos; tokens de e-mail também em 15 minutos; token de autorização dura no máximo o tempo restante do código.
7. **Tentativas:** cinco no máximo, limitadas tanto no service quanto na restrição/consulta SQL.
8. **Reenvio:** intervalo de 60 segundos; novo hash invalida o anterior.
9. **Enumeração:** solicitação de recuperação apresenta resultado genérico, quer a conta exista ou não.
10. **Parâmetros SQL:** valores do usuário são enviados por parâmetros, não concatenados à query.
11. **Transações:** usuário/perfil/verificação de cadastro; confirmação do endereço; consumo do código/alteração da senha são agrupados em transações.
12. **Dados sensíveis de Gmail:** OAuth e tokens ficam fora do projeto em armazenamento local; não compartilhar esses arquivos.
13. **Tokens na URL:** o e-mail não aparece em texto legível, mas tokens protegidos podem ficar no histórico do navegador ou em logs de URL. Eles têm prazo limitado e não contêm o código puro; ainda assim, não compartilhar links completos.

### Compatibilidade com contas antigas: observação precisa

O login permite `EmailVerificado == null` para manter contas criadas antes da migração. No service de recuperação, a condição também não rejeita explicitamente `null`, e o SQL de elegibilidade inclui `EmailVerificado IS NULL`. Porém, o método C# `CriarCodigoRecuperacaoSenha` lê esse `NULL` como `DBNull`, e o teste `emailVerificado is not bool verificado` rejeita o resultado. Portanto, **o código atual não conclui a solicitação de recuperação para uma conta legada com `EmailVerificado = NULL`**, apesar do predicado SQL incluí-la. As contas com `EmailVerificado = 1` podem seguir o fluxo normal. A compatibilidade de recuperação para contas antigas deve ser corrigida e testada separadamente se for requisito; não alterei essa lógica ao preparar esta documentação.

### Nomes duplicados

O schema atual só impõe `UNIQUE` a `Usuarios.Email`. Não existe restrição nem validação de unicidade para `Usuarios.Nome`; portanto, nomes idênticos podem ser cadastrados. Impedir isso foi deixado para uma etapa futura, conforme combinado. A definição da regra (nomes idênticos em qualquer caso ou apenas após normalizar maiúsculas/espaços) deverá ser decidida antes de criar a restrição correspondente.

### Acessibilidade das seis caixas

Cada caixa aceita um dígito e a validação exige seis dígitos numéricos. Ainda não há avanço automático de foco após digitar nem comportamento completo para colar um código inteiro. São melhorias futuras de acessibilidade/experiência, não pré-requisito da persistência ou validação no service.

---

## 14. Testes e estado final

O usuário confirmou que os sistemas funcionaram perfeitamente nos testes. Durante as correções, também foram verificados:

- build com zero erros e zero avisos;
- formulários com nomes e tokens antifalsificação renderizados;
- POST do cadastro de código chegando à lógica de confirmação;
- solicitação de recuperação chegando à página compartilhada;
- seis campos digitáveis sem a exceção `ChangeEventArgs`/`String`;
- ausência do erro HTTP “The POST request does not specify which form is being submitted” na versão de teste.

O teste de endereço `.invalid` usado durante o diagnóstico serviu apenas para comprovar que os formulários e handlers funcionavam; não enviou e-mail nem modificou uma conta real. O teste real de funcionamento foi confirmado posteriormente pelo usuário.

### Próximas melhorias fora do escopo desta documentação

1. Melhorar a acessibilidade dos campos de código: foco automático, colagem dos seis dígitos, navegação por teclado e anúncios para leitor de tela.
2. Decidir a regra para nomes duplicados e implementá-la de forma coerente na validação da interface, service e banco.
3. Se contas antigas com `EmailVerificado = NULL` precisarem recuperar senha, corrigir a leitura do `DBNull` em `CriarCodigoRecuperacaoSenha` e validar esse caso.

---

## 15. Glossário rápido

- **`EditForm`**: componente Blazor que gera e coordena um formulário.
- **`FormName`**: identificador do formulário para o POST do Razor Components.
- **`SupplyParameterFromForm`**: informa ao Blazor onde preencher o modelo a partir do POST.
- **`Service`**: camada que concentra regras de negócio e coordena repository/e-mail.
- **`Repository`**: camada que traduz operações de domínio em consultas SQL.
- **`Transação`**: conjunto de comandos que são confirmados juntos (`Commit`) ou desfeitos juntos (`Rollback`).
- **`Data Protection`**: mecanismo ASP.NET Core para proteger conteúdo contra leitura e adulteração e aplicar prazo de expiração.
- **`Finalidade`**: identifica se uma linha de verificação pertence a `Cadastro` ou `RecuperacaoSenha`.
- **`Hash`**: representação derivada; não é o código/senha original e não permite validar sem recalcular a partir do valor recebido.
- **`BCrypt`**: algoritmo de hash de senha apropriado para tornar tentativas offline mais custosas.
- **`SHA-256`**: hash fixo de 32 bytes usado aqui para comparar os códigos temporários.
