# Componentes e referências

## Microsoft WebView2

O aplicativo usa Microsoft.Web.WebView2 1.0.4258.31. O SDK é baixado da NuGet oficial com verificação de SHA256 em `preparar-webview.ps1`. O pacote portátil inclui Core, WinForms e o carregador x64, com LICENSE e NOTICE fornecidos pela Microsoft na pasta `licenses`. O WebView2 Runtime é instalado separadamente.

Documentação: https://learn.microsoft.com/microsoft-edge/webview2/
Pacote: https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.4258.31

A licença MIT do projeto não substitui a licença desses componentes.

## Referências de protocolo

O conector foi implementado neste projeto. As implementações públicas abaixo foram consultadas para compreender o protocolo; não são instaladas, executadas ou incluídas como dependências:

- Alexa Remote: https://github.com/Apollon77/alexa-remote
- Alexa Cookie: https://github.com/Apollon77/alexa-cookie
- AlexaPy: https://gitlab.com/keatontaylor/alexapy
- Audible: https://github.com/mkb79/Audible

Amazon e Alexa são marcas de seus respectivos titulares. O projeto não é um cliente oficial da Amazon.
