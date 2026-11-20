# Rise of Darkness (Ascensão das Trevas)

Jogo de ação em terceira pessoa feito na Unity (6000.3.3f1, URP) para a disciplina de Jogos Digitais.

**Autores:** Mateus Artur Santos; Rafael Araújo Nascimento

O documento de design completo está em
[`GDD - Rise of Darkness - Atualizado v2 - Entrga Final - Jogos 1.docx`](GDD%20-%20Rise%20of%20Darkness%20-%20Atualizado%20v2%20-%20Entrga%20Final%20-%20Jogos%201.docx).

## Sobre o jogo

Valek, um guerreiro das estrelas com um sabre de energia vermelho, invade uma nave inimiga e
a conquista sala a sala, eliminando os soldados Klonar, Verminox e Noctar. Depois de encontrar o
caixão da esposa, sai para a área externa, derrota o guerreiro fantasma Veyron e alcança a
própria nave para vencer.

- **Combate com sabre**, com combo de dois golpes.
- **Stamina** para correr.
- **Aura**: carregada pelos acertos do sabre; aumenta vida, velocidade e dano e libera uma
  explosão que causa dano em área e empurra os inimigos.
- **Progressão por fases**, com checkpoints salvos, 3 vidas e dificuldade ajustável na pausa.
- **Inimigos com IA no NavMesh**: robôs com patrulha, visão por raycast, perseguição, tiro e
  busca; o boss com máquina de estados (Idle, Chase, Approach, Attack).
- **Cenário destrutível**, armadilhas (lasers, laser móvel, torretas) e portas abertas por scanners.

## Controles

| Tecla | Ação |
|---|---|
| W, A, S, D | Mover |
| Mouse | Câmera |
| Botão esquerdo | Atacar com o sabre (2º clique: combo) |
| Shift (segurar) | Correr usando stamina |
| Espaço | Explosão de aura (com a aura carregada; interrompe o golpe) |
| Esc | Pausar |

## Cenas

Na ordem da build: `Intro` (logo da FANS, depois da tela do Unity), `MainMenu`, `Scene 1`
(nave inimiga), `Scene 2` (área externa e boss), `VictoryScene` e `DefeatScene`.

## Como abrir e gerar a build

1. Abrir a pasta do projeto no Unity Hub com a versão **6000.3.3f1**.
2. Para jogar no editor, abrir `Assets/Scenes/Intro.unity` (ou `MainMenu.unity`) e dar Play.
3. Para a build de Windows: **File > Build Profiles**, plataforma Windows, **Build**. As cenas já
   estão na lista na ordem certa.

## Otimização de desempenho

Trabalho avaliativo "Otimização de Projetos na Unity 6". Foram aplicadas e medidas estas técnicas:

- **Occlusion Area** nas cenas 1 e 2, cobrindo a nave inteira.
- **Occlusion Culling**: objetos parados marcados como Occluder/Occludee Static (170 na cena 1 e
  10 na cena 2) e bake feito nas duas cenas (`Assets/Scenes/Scene 1/` e `Scene 2/`).
- **Occlusion Portals** nas 2 portas da cena 1, abertos pelo `FinalButton` junto com a
  animação da porta.
- **Sombras das luzes do sabre desligadas** nos prefabs `Player` e `Enemy 4` (14 luzes pontuais).
  O brilho continua; só deixam de projetar sombra.
- **Câmera** com Avoid Obstacles (Cinemachine), para não atravessar paredes.

Resultado medido com o Stats e o Frame Debugger, antes e depois, na porta, nas fases 1 a 3 e na
cena 2: de 52% a 88% menos batches e de 61% a 75% menos eventos de desenho por quadro.
O relatório completo, com as capturas e a análise, está em
[`Docs/Relatorio_Otimizacao_Rise_of_Darkness.pdf`](Docs/Relatorio_Otimizacao_Rise_of_Darkness.pdf).
A análise de desempenho inicial está em
[`Docs/Analise_de_Desempenho_Rise_of_Darkness.pdf`](Docs/Analise_de_Desempenho_Rise_of_Darkness.pdf).

## Ferramentas de editor

Em `Assets/Scripts/Editor` ficam ferramentas que montam partes do jogo pelos menus **Tools**
(texturas e decoração da nave, NavMesh da cena 1, interface, efeitos de aura, abertura, boss no
NavMesh, configuração dos personagens animados e **Tools > Occlusion Culling**, que marca os
objetos estáticos, coloca os portais e a área e refaz o bake das cenas 1 e 2). Elas só rodam no
editor e não entram na build.

## Recursos de terceiros

- Personagens e animações humanoides: Mixamo (Adobe).
- Kits de modelos Kenney (Space Station, Space Kit, Graveyard e Furniture), licença CC0.
- Texturas de metal: ambientCG, licença CC0.
- Fontes Orbitron e Rajdhani: Google Fonts, licença SIL Open Font License.
