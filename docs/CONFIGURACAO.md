# Configuração do Paraninfo.exe

## Arquivos públicos e pessoais

- `Paraninfo.App/config.example.json`: modelo genérico versionável. É a origem
  padrão do `config.json` entregue em qualquer build ou publicação.
- `Paraninfo.App/config.json`: personalização local ignorada pelo Git. Só entra
  na saída com a propriedade explícita `-p:UsePrivateConfig=true`.
- `config.json` ao lado de `Paraninfo.exe`: arquivo que o aplicativo lê ao abrir.
  Pode ser editado sem recompilar. Feche e reabra para aplicar as alterações.

Não são lidas configurações pessoais de outros diretórios. A versão portátil
recebe o arquivo selecionado na compilação e o carrega de sua própria pasta.
Nenhum JSON é incorporado ao executável; os padrões internos também são genéricos.

```powershell
# Criar uma cópia pessoal (ignorada pelo Git).
Copy-Item .\Paraninfo.App\config.example.json .\Paraninfo.App\config.json

# Publicação genérica, inclusive quando existe uma cópia pessoal.
dotnet publish .\Paraninfo.App\Paraninfo.App.csproj -p:PublishProfile=Portable

# Publicação pessoal, somente quando solicitada explicitamente.
dotnet publish .\Paraninfo.App\Paraninfo.App.csproj -p:PublishProfile=Portable -p:UsePrivateConfig=true
```

## Identificação e apresentação

| Campo | Uso |
| --- | --- |
| `professor` | Nome mostrado no convite e no encerramento |
| `disciplina` | Disciplina usada nos logs |
| `turma` | Curso/turma nos logs e nos créditos |
| `instituicao` | Instituição mostrada na apresentação |
| `ano` | Ano da turma; valores de 1 a 9999 |
| `telaCheia` | Tela cheia em Release; padrão `true` |
| `tentativasNao` | Quantas vezes o NÃO foge; de 0 a 6 |

Debug inicia em janela. `--presentation` força tela cheia e `--windowed` tem
precedência para abrir em janela. `--skip` pula a introdução.

## Tempos

| Campo em `duracao` | Padrão |
| --- | --- |
| `boot` | 8000 ms |
| `anomaly` | 12000 ms |
| `collapse` | 15000 ms |
| `celebration` | 6000 ms |

Cada etapa aceita de 1 a 300000 ms. Fora desse intervalo, utiliza o padrão.
As três primeiras etapas somam 35 segundos. Mantenha a soma entre 35 e 45 segundos
para uma apresentação curta; a espera pela resposta não entra nessa contagem.

## Mensagens

| Campo em `mensagens` | Uso |
| --- | --- |
| `boot`, `anomaly` | Mensagens-base dos logs |
| `invitation` | Pergunta do convite |
| `success`, `declined` | Mensagens de aceite e recusa |
| `thanks` | Agradecimento final |
| `invitationHint` | Frase inicial abaixo dos botões |
| `noHints` | Frases na ordem da lista, uma por fuga do NÃO |
| `bootLines`, `anomalyLines`, `collapseLines` | Roteiros das três etapas |
| `alertLines` | Mensagens dos popups simulados |
| `collapseTitle`, `collapseMessage` | Textos do alerta crítico |

Os textos aceitam `{professor}`, `{disciplina}`, `{turma}`, `{instituicao}`,
`{ano}`, `{boot}` e `{anomaly}`. Os tokens são substituições de texto; não executam código.
As listas aceitam de 1 a 40 linhas não vazias, com até 500 caracteres por linha.
Se `noHints` for menor que o número de fugas, a lista recomeça ao chegar ao fim.
Não há contador visível de tentativas. Pelo teclado o NÃO pode ser usado desde o início.

## Áudio e visual

| Campo | Uso / padrão |
| --- | --- |
| `audio.enabled` | Áudio inicialmente ligado: `true` |
| `audio.volume` | Volume de 0 a 1: `0.2`; não altera o volume do Windows |
| `visual.terminalFont` | `VT323` usa a fonte incorporada; aceita outras famílias instaladas |
| `visual.crtEnabled` | Acabamento CRT: `true` |
| `visual.crtScanlineOpacity` | Scanlines de 0 a 0.3: `0.1` |
| `visual.crtGlow` | Brilho de 0 a 0.6: `0.25` |
| `visual.reducedMotion` | Remove deslocamentos, partículas, fuga do NÃO e fades de entrada |
| `visual.particleCount` | Partículas de 0 a 200: `100` |
| `visual.particleColor` | Cor `#RRGGBB` ou `#AARRGGBB`: `#70FFAA` |
| `visual.particleSpeed` | Velocidade de 0.5 a 2: `1` |

`M` alterna o áudio durante a execução. Digitação, popups, alerta crítico,
aceite e recusa usam os efeitos WAV incorporados. Falhas no áudio não interrompem
a apresentação. Saídas Bluetooth podem adicionar latência.

## Valores inválidos

Arquivo ausente, ilegível ou JSON inválido utiliza os padrões. Campos ausentes,
nulos, vazios ou listas inválidas recebem padrões; os parâmetros numéricos são
limitados aos intervalos aceitos. Um tipo JSON incompatível, como texto no lugar
de um objeto, faz o arquivo inteiro usar os padrões.

## Preparar para GitHub e portfólio

Mantenha nomes e frases particulares somente no arquivo ignorado `config.json`.
Não adicione esse arquivo com `git add -f`. Use o modelo genérico para releases
públicos e os prints do portfólio. Pastas de saída e pacotes são ignorados pelo Git.
Os pacotes pessoais antigos em `artifacts` continuam locais; o pacote de demonstração
genérico é o indicado em [.github/README.md](../.github/README.md).
