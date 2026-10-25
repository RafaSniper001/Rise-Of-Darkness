# Rise of Darkness (Ascensão das Trevas)

Jogo de ação em terceira pessoa feito na Unity (6000.3.3f1, URP) para a disciplina de Jogos Digitais (Entrega 2).

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

## Ferramentas de editor

Em `Assets/Scripts/Editor` ficam ferramentas que montam partes do jogo pelos menus **Tools**
(texturas e decoração da nave, NavMesh da cena 1, interface, efeitos de aura, abertura, boss no
NavMesh e configuração dos personagens animados). Elas só rodam no editor e não entram na build.

## Recursos de terceiros

- Personagens e animações humanoides: Mixamo (Adobe).
- Kits de modelos Kenney (Space Station, Space Kit, Graveyard e Furniture), licença CC0.
- Texturas de metal: ambientCG, licença CC0.
- Fontes Orbitron e Rajdhani: Google Fonts, licença SIL Open Font License.
