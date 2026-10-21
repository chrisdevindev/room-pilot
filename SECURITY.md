# Segurança e privacidade

## Proteções

- A sessão Amazon e toda a configuração, incluindo identidades dos aparelhos, são protegidas por DPAPI para o usuário atual do Windows.
- Os dados padrão ficam em `%LOCALAPPDATA%\CasaInteligente`, fora do repositório. A pasta permite acesso ao usuário atual e ao SYSTEM do Windows. O perfil WebView2 herda essas permissões; seus arquivos são gerenciados pelo próprio WebView2.
- O login usa um perfil isolado. O código não lê campos de senha. A autorização usa PKCE e os destinos de rede do conector são limitados a domínios Amazon conhecidos com HTTPS. Requisições autenticadas não seguem redirecionamentos.
- Diagnósticos persistem somente campos permitidos; corpos de respostas, URLs de autorização, tokens e cookies não são gravados em logs.
- O empacotamento usa listas explícitas. Dados locais, arquivos temporários, perfis de navegador e diagnósticos não entram nos pacotes.

## Limites

DPAPI e permissões de pasta não protegem contra programas maliciosos executados como o mesmo usuário, administradores ou um Windows comprometido. Tokens existem em memória durante o uso. Não compartilhe os arquivos privados, mesmo protegidos. O perfil do navegador contém dados de autenticação gerenciados pelo WebView2 e também deve permanecer privado.

A integração Alexa é não oficial, depende da Amazon e não oferece garantia de disponibilidade. Comandos podem controlar aparelhos físicos; confira a associação de cada controle antes de usá-lo. Não há auditoria de segurança independente declarada pelo projeto.

Sair no aplicativo remove a sessão do conector, mas não revoga a autorização na Amazon nem elimina todo o perfil do navegador. Para uma remoção completa: encerre o aplicativo, remova a pasta privada e gerencie o registro na conta Amazon. Caso exponha credenciais, revogue a sessão/registro afetado; somente apagar o arquivo publicado não resolve.

## Relatar uma falha

Não publique senhas, códigos de verificação, arquivos de sessão, capturas da conta ou respostas completas da Amazon em issues. Quando disponível no repositório, use **Security → Report a vulnerability** para relato privado. Se esse canal não estiver habilitado, abra apenas uma solicitação de contato privado sem detalhes exploráveis ou dados de conta.

## Antes de publicar

Execute `verificar-publicacao.ps1` e `package.ps1`. A verificação detecta arquivos fora da lista pública e alguns padrões comuns de credenciais; ela não substitui revisão humana nem garante a detecção de todo segredo possível. Revise também qualquer imagem ou arquivo novo adicionado à lista pública.
