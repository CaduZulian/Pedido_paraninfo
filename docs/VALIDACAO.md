# Validação e ensaio

## Verificações automatizadas

Os testes em `Paraninfo.Tests` verificam, sem abrir a interface:

- ordem completa dos estados e espera sem resposta automática;
- aceite único, recusa e prevenção de execução duplicada;
- salto durante cada etapa e salto antes da inicialização;
- cancelamento durante cada etapa, incluindo convite e comemoração;
- prioridade do encerramento sobre um salto simultâneo;
- substituição de nomes e demais tokens;
- JSON ausente, malformado, parcial, nulo ou com tipos incompatíveis;
- reparação de campos, preservação de roteiros e limitação dos parâmetros.
- identificação de novos caracteres desenhados, sem sons duplicados em redesenhos,
  pausas, limpezas ou quebras de linha; retomada correta na próxima etapa.

A compilação WPF verifica XAML, namespaces e referências; a publicação produz o
executável self-contained. Essas verificações não reproduzem um projetor ou uma
placa de áudio. A interface não foi executada durante esta implementação.

## Matriz visual e de hardware — pendente de ensaio

| Ambiente | 100% DPI | 125% DPI | 150% DPI |
| --- | --- | --- | --- |
| Windows 10 x64, 1024×768 | Pendente | Pendente | Pendente |
| Windows 10 x64, 1366×768 | Pendente | Pendente | Pendente |
| Windows 11 x64, 1920×1080 | Pendente | Pendente | Pendente |
| Windows 11 x64, 1280×800 | Pendente | Pendente | Pendente |
| Computador e projetor da homenagem | Pendente | Pendente | Pendente |

O terminal preenche toda a área da janela, sem margens externas ou rodapés.
Convite e encerramento usam uma área lógica escalada uniformemente, preservando
proporções e coordenadas dos botões em 4:3, 16:9 e 16:10; nessas telas pode haver margens. O manifesto usa
PerMonitorV2 para o Windows notificar mudanças de DPI. Isso foi implementado;
a leitura à distância e a troca entre monitores precisam ser observadas no ensaio.

## Roteiro manual

1. Edite o JSON com nomes reais. Abra em janela e depois em apresentação.
2. Cronometre do primeiro log à pergunta visível: aproximadamente 35–36 s nos padrões.
3. Confira a aba e o prompt do terminal, digitação, cursor junto ao texto e rolagem.
   Confira a fonte VT323 sem instalação, incluindo Instituição, Educação, ação e NÃO.
   Verifique a leitura com o CRT ligado e desligado; scanlines e brilho não devem esconder letras.
   Ao entrar na anomalia, o erro deve aparecer de imediato, com um impacto visual.
   Reinicie duas vezes: posições dos alertas e ritmos das rajadas devem variar.
4. Confira o corte seco do terminal para CRITICAL SYSTEM REQUIREMENT sobre preto,
   sem escurecimento gradual ou fade do título. Depois deve haver tela inteira preta antes do convite.
   O nome do professor deve aparecer sem vírgula acrescentada ao final.
5. Não responda por um minuto: o convite deve permanecer aguardando.
6. Aproxime o mouse do NÃO três vezes; confira que não cobre o SIM e não sai dos limites.
   As posições devem variar sem rota fixa. Frases de `mensagens.noHints` devem aparecer
   na ordem configurada, sem contador ou aviso de parada.
   Confira o texto escuro do SIM sobre o fundo verde, inclusive ao focar, passar o mouse e pressionar.
   Depois clique no NÃO e confirme o encerramento respeitoso com um som curto de fracasso,
   uma única vez. Repita usando Tab/Enter e com M em mudo. Fechar com ESC não deve tocar fracasso.
7. Reinicie, aceite e confira confetes, áudio e agradecimento persistente após seis segundos.
   Confira VT323 no convite, nas frases do NÃO e no encerramento, com CRT discreto,
   alinhamento de console e sem cards, ícones circulares ou títulos em outra fonte.
8. Feche com ESC no boot, anomalia, colapso, convite, comemoração e tela final.
9. Use F2 em cada etapa da introdução e confira a interrupção imediata dos efeitos e sons.
10. Confira Alt+Tab, tecla Windows e Alt+F4; o controle normal do computador deve continuar.
11. Teste M e volume baixo; sem dispositivo de áudio, o fluxo deve continuar silenciosamente.
    Cada popup novo deve tocar um alerta; redesenhar um popup existente ou uma rajada
    sem popup não deve tocar outro alerta. O corte para CRITICAL deve tocar uma única falha.
    A digitação deve tocar somente junto ao aparecimento de caracteres, inclusive após o boot,
    e parar ao cortar para a tela preta. Confira digitação e alertas simultâneos sem cortes mútuos.
    Compare usando a saída de áudio prevista para a homenagem; Bluetooth pode adicionar latência.
12. Com o programa fechado, experimente JSON ausente, inválido, parcial e campos nulos.
13. Experimente `visual.reducedMotion: true`, `tentativasNao: 0` e nomes mais longos.
14. Execute a pasta portátil em Windows x64 sem runtime .NET instalado e sem internet.
15. Repita a sequência completa no projetor, inclusive com o áudio de apresentação escolhido.

Nenhum item manual desta lista deve ser considerado aprovado sem executar o ensaio.
