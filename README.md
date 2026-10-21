# room-pilot

**Português** | [English](README.en.md)

Controle aparelhos vinculados à Alexa por botões, atalhos globais de teclado e um menu perto do relógio do Windows. Comece com Luz, Tomada e Fita LED e adicione quantos controles precisar. Cada um pode ser associado a um aparelho compatível da sua conta.

O aplicativo é gratuito e usa um conector próprio, não oficial, para os serviços da Amazon. Precisa de internet e pode precisar de ajustes se a Amazon alterar esses serviços. O app Alexa pode ficar fechado. Não é necessário configurar Tuya, chaves locais ou servidor próprio.

## Usar no Windows

Requisitos: Windows 64 bits, .NET Framework 4.8 e [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/). O pacote portátil inclui o aplicativo e as bibliotecas do WebView2; o Runtime precisa estar instalado no computador.

1. Extraia todo o pacote portátil e abra **room-pilot.exe**. Mantenha os arquivos DLL junto dele.
2. Abra **Configurar → Conectar Alexa → Entrar na Amazon**. Selecione a região da conta; o padrão é `amazon.com.br`.
3. Entre na mesma conta usada na Alexa, na página da Amazon, e conclua a verificação solicitada. A página de login usa `www.amazon.com`, inclusive para a região brasileira.
4. Na aba **Aparelhos**, confira as correspondências sugeridas ou selecione os aparelhos manualmente. Somente entidades com controle de energia compatível aparecem.
5. Use **Adicionar aparelho** para criar outros controles. Edite os nomes nos campos e escolha a correspondência na Alexa. **Remover** retira o controle e os atalhos associados ao salvar.
6. Clique em **Salvar**. Confira o estado no painel e use **Ligar** ou **Desligar**.

O painel e o editor usam listas com rolagem, sem limitar o cadastro aos três controles iniciais. Renomear um controle preserva sua identidade e seus atalhos. Configurações anteriores são convertidas automaticamente ao abrir, sem precisar refazer o login.

O painel consulta os estados a cada dois minutos. Antes de alternar, consulta novamente o estado; depois de enviar um comando, tenta confirmar o resultado. A consulta e a configuração não acionam aparelhos.

## Idioma / Language

Em **Configurar → Preferências → Idioma**, escolha **Português (Brasil)** ou **English** e clique em **Salvar**. O painel, o menu, os botões e as mensagens mudam sem precisar refazer o login. Os nomes dos aparelhos e os identificadores dos comandos são preservados.

Instalações novas escolhem português para um Windows em português e inglês para outros idiomas. Configurações das versões anteriores permanecem em português. Para uma execução temporária em outro idioma, use `--language en-US` ou `--language pt-BR`; essa opção não grava a preferência.

Os catálogos de tradução ficam em `src/Localization`. Veja [como contribuir com traduções](docs/I18N.md). Eles são incorporados ao executável e também estão no código-fonte.

## Teclado e menu

Em **Configurar → Atalhos**, personalize as combinações e atribua ações de Ligar, Desligar, Alternar ou Abrir painel. Os padrões são:

| Atalho | Ação |
| --- | --- |
| Ctrl + Alt + 0 | Abrir painel |
| Ctrl + Alt + 1 | Alternar luz |
| Ctrl + Alt + 2 | Alternar tomada |
| Ctrl + Alt + 3 | Alternar fita LED |

Use Ctrl ou Alt, opcionalmente Shift, ou uma tecla F1 a F24. Conflitos com outros programas são informados. Os atalhos ficam suspensos enquanto a configuração está aberta.

Fechar o painel mantém o programa na área de notificação. Clique no ícone de casinha para abrir o menu; um clique duplo abre o painel. Escolha **Sair** para encerrar. Se necessário, arraste o ícone de dentro da seta `^` para perto do relógio. A inicialização com o Windows é opcional em **Preferências**.

## Dados da conta

Sessão, preferências e identidades dos aparelhos são salvas em `%LOCALAPPDATA%\CasaInteligente`, fora da pasta do projeto. Essa pasta antiga é mantida por compatibilidade após a mudança de nome. `settings.dat` e `amazon-session.dat` usam a proteção DPAPI do Windows para o usuário atual. O perfil privado de login fica nessa mesma pasta. Não compartilhe essa pasta nem seus arquivos, mesmo protegidos.

A senha e a verificação são preenchidas na página HTTPS da Amazon. O conector recebe o código de autorização e usa PKCE. Não lê campos de senha nem usa o perfil normal do navegador. O registro na Amazon se chama **room-pilot**. A integração segue o protocolo de um cliente móvel Alexa.

**Sair deste PC**, seguido de **Salvar**, apaga o arquivo de sessão do conector. Isso não revoga o registro na Amazon nem apaga todo o perfil do navegador. Para remover todos os dados locais, encerre o programa e remova a pasta privada indicada acima. Gerencie os registros na própria conta Amazon.

O diagnóstico `amazon-status.json` contém somente versão, etapa, status HTTP, código de erro reconhecido e horário. Não registra o corpo das respostas, senhas, cookies ou tokens. Veja [SECURITY.md](SECURITY.md) para os limites dessa proteção.

## Compilar e gerar pacotes

No PowerShell, a partir da pasta do projeto:

```powershell
.\preparar-webview.ps1
.\build.ps1
.\verificar-publicacao.ps1
.\package.ps1
```

O primeiro script baixa o SDK oficial Microsoft.Web.WebView2 1.0.4258.31 da NuGet e verifica o SHA256 fixado. A compilação usa o compilador do .NET Framework disponível no Windows e executa testes com dados fictícios, sem login e sem acionar aparelhos reais. O resultado fica em `outputs`.

`package.ps1` gera um ZIP portátil e outro com o código-fonte. Somente os arquivos da lista explícita entram nos pacotes. Nenhum dado de conta, perfil de navegador, cache ou diagnóstico é incluído. Para publicar no GitHub, use o conteúdo do pacote de código-fonte ou os arquivos listados em `public-files.txt`. Cada pessoa entra com sua própria conta no computador dela.

Para atalhos externos:

```text
"room-pilot.exe" --device tomada --command on
"room-pilot.exe" --device tomada --command off
"room-pilot.exe" --device fita --command toggle
"room-pilot.exe" --device lamparina --command status
"room-pilot.exe" --list-devices
"room-pilot.exe" --device "Luz do escritório" --command on
```

Os identificadores iniciais são `lamparina`, `tomada` e `fita`. Novos controles recebem identificadores estáveis. Use `--list-devices` para listar nomes e identificadores, sem expor as identidades Amazon. Você também pode selecionar pelo nome quando ele for único. `--result caminho.json` salva o resultado sem credenciais; `--connect` abre a configuração. Para desenvolvimento, `--data-dir pasta` isola os dados; use apenas uma pasta privada. Instalações antigas com `data-alexa` junto do executável podem importar a configuração automaticamente sem apagar os originais.

## Código e licença

- `src/Domain`: dados dos aparelhos e ações de energia.
- `src/Application`: cadastro, validação e controle com confirmação de estado.
- `src/Infrastructure`: conexão Alexa, armazenamento protegido e integração Windows.
- `src/UI`: janelas e componentes reutilizáveis.
- `src/CLI`: comandos externos e listagem dos controles.

Veja [a arquitetura e as orientações para expansão](docs/ARCHITECTURE.md). A versão 5.1 adota o nome room-pilot e inclui português e inglês, com atualização da preferência de idioma sem alterar a conta ou os aparelhos. O cadastro dinâmico e as proteções da versão anterior continuam disponíveis.

Licença [MIT](LICENSE). As bibliotecas Microsoft seguem suas próprias licenças, incluídas no pacote portátil. Referências e componentes estão em [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). O projeto não é afiliado à Amazon ou à Microsoft. Não utiliza os pacotes Alexa Remote ou Alexa Cookie.
