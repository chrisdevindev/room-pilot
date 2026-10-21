# Contribuir

Use Windows 64 bits com .NET Framework 4.8 e WebView2 Runtime. Execute `preparar-webview.ps1` e `build.ps1`. Os testes automatizados usam um serviço simulado e dados fictícios; não exigem conta Amazon.

Use `--data-dir` com uma pasta privada separada para testes manuais. Nunca inclua tokens, cookies, identidades reais de aparelhos, configurações pessoais ou perfis de navegador em testes, capturas, logs ou pull requests.

Antes de enviar alterações, execute `build.ps1` e `verificar-publicacao.ps1`. Caso acrescente arquivos públicos, atualize `public-files.txt`. Preserve o filtro de domínios, o login isolado, a proteção dos dados e a confirmação do estado após comandos.

Contribuições são distribuídas sob a licença MIT do projeto. Componentes de terceiros conservam suas próprias licenças.

Consulte `docs/ARCHITECTURE.md` antes de acrescentar funcionalidades. Mantenha o padrão de `.editorconfig`, use identificadores estáveis e teste listas vazias, muitos aparelhos e referências dos atalhos ao alterar o cadastro.

Traduções: consulte `docs/I18N.md`. Adicione qualquer chave nova a todos os catálogos e mantenha nomes de aparelhos e campos de protocolo fora da tradução.
