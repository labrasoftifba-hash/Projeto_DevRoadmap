# Resumo de retomada — DevRoadmap

Este documento registra o estado do projeto e o contexto necessário para continuar o trabalho em outro chat. Não contém senhas, tokens OAuth nem o conteúdo do JSON de credenciais do Google.

## Como continuar o trabalho

- Trabalhar como tutor: explicar o objetivo, os arquivos envolvidos e o motivo de cada mudança.
- Avançar com calma, em passos pequenos, deixando o usuário entender e participar.
- Não implementar uma funcionalidade inteira sem solicitação explícita.
- Seguir o fluxo definido para cada funcionalidade:

  ```text
  Necessidade → Banco → Model → Repository → Service → Tela → Teste
  ```

- Antes de começar novas implementações, explicar o próximo passo.
- Preservar as mudanças existentes no worktree; não descartar alterações do usuário.

## Ordem dos requisitos do projeto

### Fase 1

1. RF01 — Cadastro de usuários
2. RF02 — Login
3. RF04 — Permissões
4. RF03 — Recuperação de senha
5. RF27 — Gerenciamento de usuários

### Fases seguintes

- Fase 2: RF05 Trilhas, RF06 Módulos, RF07 Habilidades, RF08 Checklist, RF21 Gerenciamento de trilhas.
- Fase 3: RF09 Adesão, RF10 Progresso, RF22 Recomendações.
- Fase 4: RF11 Avaliações, RF26 Gerenciamento, RF12 Resultados, RF24 Critérios, RF25 Feedback.
- Fase 5: RF13 Projetos, RF14 Entregas, RF23 Aprovação.
- Fase 6: RF17 Certificados, RF18 Notificações, RF19 Comunicação.
- Fase 7: RF15 Dashboard estudante, RF16 Dashboard professor, RF20 Relatórios.
- Fase 8: requisitos futuros.

## Estado funcional confirmado

- Aplicação ASP.NET Core + Blazor em .NET 10, estruturada em camadas.
- Banco SQL Server local em instância `localhost\SQLEXPRESS`, banco `DevRoadmap`.
- SQL Server identificado pelo usuário como SQL Server 2025, nível de compatibilidade 170.
- Cadastro e login básicos foram confirmados pelo usuário como funcionando com o banco, antes da integração de confirmação de e-mail.
- Senhas novas são gravadas com BCrypt; o fallback antigo SHA-256/texto puro foi removido.
- Cadastro associa o usuário ao perfil padrão `Estudante`.
- Cadastro e login têm `FormName` diferentes e recebem o POST por `[SupplyParameterFromForm]`.
- A tela de cadastro e a de login usam um único modelo para formulário, campos e handler.
- O cadastro foi testado e concluiu a inserção após ajustar instância SQL e ordem da cláusula `OUTPUT`.
- A migração para confirmação de e-mail foi executada pelo usuário; ele informou que o resultado no banco foi o esperado.
- A aplicação compilou após a implementação de confirmação de e-mail, com saída redirecionada para uma pasta temporária para evitar a colisão com uma instância em execução: 0 erros e 0 avisos.
- O usuário confirmou em 2026-09-30 que cadastro, verificação de e-mail e recuperação de senha funcionaram nos testes.
- Em 2026-09-30, foi implementada a recuperação de senha e as alterações compilaram com 0 erros e 0 avisos. O usuário confirmou que aplicou `Database/03_Finalidade_Verificacao_Email.sql` no SQL Server.
- Em 2026-09-30, durante os testes, o usuário reportou que o POST do código dizia não identificar o formulário e que “Voltar ao cadastro” não funcionava. Os `EditForm` têm nomes únicos. A configuração de interatividade foi inicialmente colocada em `Components/Routes.razor`, mas a captura seguinte ainda mostrava o postback estático. A configuração foi então movida para o ponto padrão do template Blazor, `<Routes @rendermode="InteractiveServer" />` em `Components/App.razor`; removido o `@rendermode` de `Routes.razor`. A captura posterior ainda mostrou o erro no endpoint `/confirmar-email`; foi mudado “Voltar ao cadastro” para link nativo `/cadastro`, sem dependência de evento interativo. A última compilação foi feita no diretório normal `bin/Debug/net10.0` e passou com 0 erros e 0 avisos. A confirmar com o usuário: parar toda instância antiga, iniciar com `dotnet run --project "C:\\Labrasoft(DevRoadMap)\\DevRoadmap\\DevRoadmap.csproj"` e retestar. Se o erro continuar, inspecionar a marcação HTML servida e a versão efetivamente iniciada antes de novas mudanças no formulário.
- Investigação seguinte: a aplicação já em execução em `localhost:5052` serve `/confirmar-email` sem `action` e sem o campo oculto `_handler` que identifica `FormName`. Uma build isolada do código atual, iniciada temporariamente em `localhost:5053`, renderiza `_handler=confirmar-codigo`; ao enviar um código inválido para um e-mail de teste inexistente, o POST foi aceito e chegou à lógica do serviço (resposta normal da aplicação, não erro HTTP 400 de formulário). Isso comprova que `localhost:5052` está servindo uma versão/build antiga em relação ao código atual. A build isolada foi encerrada após o teste; não encerrar a instância que o usuário mantém em execução. Próximo passo: o usuário deve parar a própria instância/depurador e iniciá-la novamente a partir do projeto atualizado; depois testar cadastro e confirmação na porta normal. Ainda não houve teste de envio real de e-mail nem de cadastro real nesta execução.
- Em 2026-09-30, foi reproduzida na build atual a perda do estado scoped no POST tradicional da recuperação. A recuperação foi ajustada para usar tokens protegidos pelo ASP.NET Core Data Protection, com validade limitada: o e-mail não fica legível na URL e a autorização de redefinição contém o ID do usuário e o hash do código, não o código puro. A confirmação do código e a redefinição agora dependem também da verificação transacional do código ainda válido no banco; não dependem do circuito Blazor permanecer vivo entre POSTs.
- Em 2026-09-30, `FormName` e `[SupplyParameterFromForm]` foram conferidos em cadastro, login, confirmação, solicitação de recuperação e redefinição; o modelo de login agora declara também `FormName="login"` explicitamente. A build isolada atual compilou com 0 erros e 0 avisos. Teste de navegação/POST na porta temporária confirmou que a solicitação de recuperação chega à página compartilhada, preserva o fluxo/e-mail por campos ocultos, aceita um POST de código inválido sem erro de formulário e continua exibindo o retorno esperado. Esse teste usou endereço `.invalid` e não envia e-mail nem altera usuário existente.
- Em 2026-09-30, o usuário compartilhou exceção durante digitação das caixas de código: `ChangeEventArgs` não podia ser convertido para `String`. A causa estava em `@bind-Value:event="oninput"` nas instâncias de `InputText` das páginas de cadastro e confirmação. O evento explícito foi removido das doze caixas para usar o evento `onchange` padrão compatível com `InputText`. O build isolado passou com 0 erros e 0 avisos; o teste interativo na página compartilhada `/confirmar-email` digitou e tabulou os seis dígitos, sem erros no console/circuito, e o POST chegou à lógica de confirmação. O usuário confirmou depois o funcionamento dos fluxos.
- Em 2026-09-30, o usuário reportou erro de sintaxe próximo a `QUOTENAME` ao executar `Database/03_Finalidade_Verificacao_Email.sql`. O bloco de remoção dinâmica da chave primária foi corrigido para montar uma variável `NVARCHAR(MAX)` e executá-la com `sys.sp_executesql`.
- O usuário informou em 2026-09-30 que executou a versão corrigida da migração de finalidade no banco. A migração `Database/03_Finalidade_Verificacao_Email.sql` está aplicada; não pedir para executar novamente salvo se algum teste revelar necessidade.
- A documentação completa solicitada após a confirmação dos testes foi criada em `DOCUMENTACAO-CADASTRO-EMAIL-SENHA.md`, incluindo arquitetura, funções, SQL, fluxos e observações técnicas.
- Melhorias deixadas para uma etapa futura: foco automático/acessibilidade das seis caixas e definição/regra de unicidade para nomes.

## Autenticação: arquivos importantes

- `Components/Pages/Auth/Cadastro.razor`: formulário de cadastro, validações e submissão.
- `Components/Pages/Auth/Cadastro.razor`: após cadastro bem-sucedido, troca para a confirmação do código na mesma rota, mantendo o e-mail e exibindo seis campos de dígito.
- `Components/Pages/Auth/ConfirmarEmail.razor`: rota alternativa `/confirmar-email`, acessível pelo login para confirmar um cadastro pendente sem o fluxo imediato.
- `Components/Pages/Auth/ConfirmarEmail.razor.css`: estilos isolados da página de confirmação.
- `Components/Pages/Auth/RecuperarSenha.razor`: solicitação de código de recuperação em `/recuperar-senha`, com resposta genérica que não revela se o e-mail está cadastrado.
- `Components/Pages/Auth/RedefinirSenha.razor`: definição de nova senha após validação do código.
- `Components/Pages/Auth/Login.razor`: formulário de login, link para `/cadastro`, `/confirmar-email` e `/recuperar-senha`.
- `Services/CadastroService.cs`: cadastro pendente, geração segura do código, hash SHA-256 do código, validade, contagem de tentativas, confirmação, reenvio e solicitação de envio pelo Gmail.
- `Services/RecuperacaoSenhaService.cs`: solicitação genérica, validação do código de recuperação, limite de tentativas, comparação com a senha atual via BCrypt e redefinição de uso único.
- `Services/RecuperacaoSenhaTokenService.cs`: protege tokens de e-mail/fluxo e de autorização com ASP.NET Core Data Protection e prazo limitado, evitando depender de estado scoped entre postbacks e evitando expor e-mail/código em texto legível na URL.
- `Services/CodigoVerificacaoEmail.cs`: geração criptograficamente segura do código de seis dígitos, hash SHA-256 e comparação de hash em tempo constante.
- `Services/UsuarioService.cs`: autentica com BCrypt; bloqueia contas com `EmailVerificado == false`, mantendo o acesso de contas antigas cujo campo é `NULL`.
- `Services/Email/GmailEmailSender.cs`: envio pela Gmail API usando credenciais e token locais.
- `Repositories/UsuarioRepository.cs`: persiste usuário, perfil e verificação; consulta código, conta tentativas, limita reenvio e confirma/consome o código transacionalmente.
- `Models/VerificacaoEmail.cs`: representa os dados persistidos para a verificação.
- `Repositories/BaseRepository.cs`: carrega e valida a connection string.
- `Models/Usuario.cs` e `Models/Perfis.cs`: modelos.
- `Database/01_Usuarios_E_Perfis.sql`: schema inicial de usuários, perfis e vínculo.
- `Database/02_Verificacao_Email.sql`: migração aditiva para `EmailVerificado` e a tabela `VerificacoesEmail`; já executada pelo usuário.
- `Database/03_Finalidade_Verificacao_Email.sql`: migração de finalidade (`Cadastro` ou `RecuperacaoSenha`) e chave primária composta; o usuário informou que já a executou.
- `Program.cs`: registra os repositórios/serviços, ASP.NET Core Data Protection e o serviço de tokens de recuperação.
- `appsettings.json`: connection string para a instância local; não copie segredos para este resumo.

## Progresso de 2026-09-29 e 2026-09-30 — confirmação de e-mail no cadastro

### Fluxo implementado

1. `CadastroService.CadastrarUsuarioAsync` valida os campos essenciais, gera o código com `RandomNumberGenerator.GetInt32` e o formata com seis dígitos.
2. A senha continua sendo armazenada como hash BCrypt. O código de confirmação não é armazenado em texto: o serviço calcula SHA-256 e envia apenas o hash ao repositório.
3. `UsuarioRepository.CadastrarPendente` grava a conta, o vínculo com `Estudante` e a verificação (hash e vencimento) dentro da mesma transação SQL. A validade do código é de 15 minutos.
4. Depois da persistência, `GmailEmailSender` envia o código ao endereço cadastrado. Erros conhecidos de Gmail, acesso ao arquivo de credenciais, rede e configuração do sender são registrados em log; a conta permanece pendente se o envio falhar, com uma mensagem para tentar o reenvio.
5. A tela `/cadastro` muda para a confirmação do código na própria página assim que a conta é criada, sem exigir que a pessoa informe o e-mail de novo ou navegue manualmente para outra rota. O e-mail permanece associado ao modelo da etapa e é mostrado na tela.
6. A etapa inline apresenta seis campos numéricos, validação por dígito, estado de envio, opção de reenvio e ação para voltar ao cadastro. Após sucesso, oferece navegação ao login. A rota alternativa `ConfirmarEmail.razor` continua disponível por meio do login.
7. O serviço rejeita contas que não estejam pendentes, códigos expirados, formato incorreto e tentativas acima do limite. Códigos incorretos incrementam o contador, limitado a cinco tentativas.
8. A comparação dos hashes usa `CryptographicOperations.FixedTimeEquals`. Em caso de sucesso, o repositório marca `EmailVerificado = 1` e apaga a verificação em uma transação; o código não pode ser reutilizado.
9. O reenvio tem intervalo mínimo de 60 segundos, troca o hash anterior, reinicia as tentativas e renova a validade. A rota alternativa continua disponível na tela de login.
10. `UsuarioService.Autenticar` nega login quando `EmailVerificado` é `false`; valores `NULL` de contas anteriores à migração continuam permitidos.

### Decisões de compatibilidade e proteção

- Contas existentes não foram atualizadas pela migração nem pelo novo fluxo. `NULL` é preservado como estado de conta antiga e pode autenticar.
- Novos cadastros recebem `EmailVerificado = 0` pelo default do banco e só podem autenticar após a confirmação.
- A senha usa BCrypt; SHA-256 é usado somente no código de verificação.
- O limite de cinco tentativas corresponde ao `CHECK` definido na tabela e à condição do `UPDATE`.
- As exceções tratadas no envio são registradas; erros não previstos não são convertidos silenciosamente em sucesso.

## Progresso de 2026-09-30 — recuperação de senha

### Fluxo implementado

1. Foi criada a migração `Database/03_Finalidade_Verificacao_Email.sql`. Ela adiciona `Finalidade` à tabela existente, marca linhas antigas como `Cadastro` e muda a chave para `(UsuarioId, Finalidade)`. Isso impede que o código do cadastro seja aceito na recuperação de senha.
2. A tela `/recuperar-senha` segue o primeiro exemplo enviado: cartão central escuro, título “Redefinição de senha”, campo de e-mail, botão “Enviar código” e retorno ao login.
3. O envio leva a `/confirmar-email?fluxo=recuperacao`. A rota de confirmação existente foi preparada para dois contextos: confirmação de cadastro e validação para redefinição. O formulário de código é compartilhado; serviço e coluna de finalidade mantêm os propósitos isolados.
4. A solicitação de recuperação sempre mostra uma resposta genérica, exista ou não uma conta correspondente. Apenas contas não pendentes de confirmação podem solicitar o código; contas antigas com `EmailVerificado = NULL` continuam elegíveis porque o acesso ao endereço informado será provado pelo código enviado.
5. O código tem seis dígitos, SHA-256 armazenado no banco, 15 minutos de validade, cinco tentativas máximas e intervalo mínimo de 60 segundos entre envios. O reenvio também mantém a resposta genérica.
6. A URL da confirmação recebe um token Data Protection com validade limitada, que protege o e-mail enquanto o fluxo atravessa o postback. O token de autorização após confirmar o código contém o ID do usuário e o hash do código (não o código puro); a redefinição só funciona se o repositório ainda consumir esse mesmo código dentro da validade e da transação SQL.
7. A tela `/redefinir-senha` segue o segundo exemplo enviado: nova senha, confirmação, botão redefinir e link de login. Valida comprimento mínimo de oito caracteres e igualdade entre os dois campos.
8. Antes de gravar, o serviço usa `BCrypt.Verify` para impedir que a nova senha seja igual à atual. O repositório consome o código e atualiza a senha na mesma transação; código ou autorização não podem ser reutilizados.
9. `Program.cs` registra Data Protection e o serviço de tokens. O fluxo não depende de um objeto scoped sobreviver à transição entre POST e GET nem entre solicitações HTTP.

### Estado do teste ponta a ponta

- `Database/03_Finalidade_Verificacao_Email.sql` já foi executada pelo usuário no banco `DevRoadmap`. É uma migração aditiva que preserva os códigos existentes como finalidade `Cadastro` e mantém as contas e senhas atuais; não pedir nova execução sem evidência de problema no banco.
- Em 2026-09-30, o usuário confirmou que os sistemas de cadastro, verificação de e-mail e recuperação de senha funcionaram perfeitamente nos testes.
- O teste técnico com endereço `.invalid` descrito acima apenas validou formulário e POST; não enviou e-mail nem modificou uma conta. A confirmação de funcionamento ponta a ponta veio depois, nos testes do usuário.

### Cenários úteis em uma futura regressão

- Confirmar o cadastro de um endereço de e-mail acessível e a chegada do código.
- Verificar que a linha em `VerificacoesEmail` contém apenas o hash, a validade e contador, não o código puro.
- Tentar login antes da confirmação e verificar que é recusado.
- Confirmar com o código recebido e verificar `EmailVerificado = 1` e remoção da linha de `VerificacoesEmail`.
- Testar código incorreto, expirado, reutilizado, cinco tentativas e bloqueio de reenvio por 60 segundos.
- Confirmar que uma conta antiga com `EmailVerificado = NULL` continua conseguindo entrar.
- O build normal pode falhar ao substituir `DevRoadmap.exe`/DLL se a aplicação estiver rodando. Parar a instância com Ctrl+C antes do `dotnet build`, ou redirecionar a saída para uma pasta temporária. Não encerrar processos sem confirmar que são da aplicação.
- Em 2026-09-30, a atualização da tela inline compilou em pasta temporária com 0 erros e 0 avisos. O usuário confirmou depois que os fluxos funcionaram.

### Erros resolvidos durante cadastro/login

1. **POST sem nome do formulário:** corrigido com `FormName="cadastro"` e `FormName="login"`.
2. **Campos do POST apareciam vazios:** os dados fornecidos no POST, o modelo do `EditForm`, os campos e o handler chegaram a usar propriedades/instâncias diferentes. As páginas foram alinhadas para o mesmo modelo com `[SupplyParameterFromForm]`.
3. **`EditForm` sem Model/EditContext:** o `Model` podia ser nulo antes da inicialização. Foi inicializado em `OnInitialized`.
4. **SQL Server não encontrado:** a configuração inicial apontava para `localhost`; a instância usada no SSMS era `localhost\SQLEXPRESS`.
5. **Erro de sintaxe próximo a `OUTPUT`:** a cláusula estava na posição errada. A consulta atual é:

   ```sql
   INSERT INTO Usuarios (Nome, Email, Senha)
   OUTPUT INSERTED.Id
   VALUES (@Nome, @Email, @Senha)
   ```

   `OUTPUT` é suportado pela versão e pelo nível de compatibilidade informados.
6. **Build não conseguia substituir executável/DLL:** a aplicação estava em execução e mantinha os arquivos bloqueados. Para build normal, parar com Ctrl+C; em investigação foi usada uma pasta temporária de saída.

## Estilo das páginas de autenticação

- `Components/Pages/Auth/Login.razor.css`
- `Components/Pages/Auth/Cadastro.razor.css`
- Cada folha `.razor.css` fica ao lado do componente e é carregada pelo Blazor CSS isolation.
- As páginas usam variáveis globais de cor de `wwwroot/css/variables.css`.
- O CSS global `wwwroot/css/auth.css` foi removido e sua importação removida de `wwwroot/app.css`.
- O gradiente dourado central foi intensificado nos dois arquivos isolados.
- As regras usam `::deep` para alcançar os elementos `<input>` gerados por `InputText`.
- O build foi validado após isolar os estilos: sucesso, zero warnings e zero errors.

## Outro ajuste de interface

- O círculo decorativo atrás do indicador `78%` no dashboard foi removido de `Components/Layout/Student/Dashboard/NextStop.razor.css`, retirando `.next-stop::before`.
- O indicador circular SVG e o texto `78%` foram preservados.

## Próximos passos

1. Reiniciar a aplicação para carregar as alterações compiladas e executar os testes reais pendentes acima, começando por cadastro + recebimento + confirmação + login.
2. Resolver quaisquer falhas reveladas pelo teste antes de iniciar outra funcionalidade.
3. Em seguida, conforme a ordem do projeto, implementar RF04 — permissões.
4. Antes do teste de RF03, executar `Database/03_Finalidade_Verificacao_Email.sql`; então testar o fluxo de recuperação completo.
5. Após concluir os testes de autenticação e permissões, continuar a próxima fase na ordem definida pelo projeto.
6. O histórico completo de senhas antigas não está decidido nem implementado; apenas a senha atual é comparada.

### Conta remetente / Google API

- O usuário criou a conta remetente dedicada `confirmacaolabra@gmail.com`.
- O usuário já autorizou o Gmail e confirmou que recebeu o e-mail de teste.
- O `GmailEmailSender` lê o arquivo OAuth e o token sob `%LOCALAPPDATA%\DevRoadmap\Google\`; os segredos e tokens não devem entrar no repositório nem nos logs.
- O arquivo de credenciais OAuth usado atualmente é esperado neste local:

  ```text
  %LOCALAPPDATA%\DevRoadmap\Google\web-client-secret.json
  ```

- O token é salvo em `%LOCALAPPDATA%\DevRoadmap\Google\tokens`.
- O escopo utilizado é `GmailSend`; a consulta de perfil Gmail foi removida porque exigia escopo adicional.
- Não pedir ao usuário para colar ou enviar JSON, segredo OAuth ou token.
- Para implantação em rede/produção, mover credenciais e token para armazenamento seguro do servidor e revisar a configuração OAuth para o ambiente publicado. A configuração local atual não basta para hospedagem.

## Estado do worktree no último registro

Havia várias alterações não commitadas, incluindo páginas/serviços de autenticação, estilos, modelos, repositório, configuração SQL e arquivos de database. Não sobrescrever ou descartar essas mudanças. O build da implementação de confirmação de e-mail foi validado em pasta temporária com sucesso, sem avisos nem erros; o fluxo ainda aguarda teste funcional real.
