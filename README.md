# Mira Pontinho — mira de ponto para CS 1.6

[🇧🇷 Português](#-português) · [🇬🇧 English](#-english)

[![Release](https://img.shields.io/github/v/release/taquinoc/cs16-dot-crosshair?label=download&color=brightgreen)](https://github.com/taquinoc/cs16-dot-crosshair/releases/latest)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

![Interface do Mira Pontinho](docs/gui.png)

Um overlay **externo** que desenha uma mira (ponto, cruz, T, círculo ou X) no centro da tela, por cima do **Counter-Strike 1.6**.

> **Sem risco de VAC:** o programa **não modifica** nenhum arquivo do jogo e **não injeta** nada no processo do CS. Ele apenas desenha uma janela por cima — igual às miras nativas de alguns monitores gamer. A única alteração (opcional) é um arquivo de configuração (`userconfig.cfg`), que também não é verificado pelo VAC.

---

## 🇧🇷 Português

### ✨ Recursos
- Formas: **Ponto, Cruz (+), T, Círculo e X**
- Cor, tamanho, espessura, vão e contorno ajustáveis
- **Pré-visualização ao vivo** enquanto você ajusta
- Segue a janela do CS (funciona em janela e em **tela cheia sem borda**)
- **Oculta a mira nativa do CS** (opção automática + ajuda manual com botão **?**)
- Atalho global **Ctrl + Alt + D** para ligar/desligar
- Ícone na bandeja do sistema
- Salva as configurações em `MiraPontinho.ini`
- **Portátil**: é só um `.exe`, não instala nada

### 🚀 Como usar
1. Baixe o `MiraPontinho.exe` na página de **[Releases](https://github.com/taquinoc/cs16-dot-crosshair/releases/latest)** (ou compile com `build.bat`).
2. Abra o `MiraPontinho.exe`.
3. Ajuste a mira na janela de configuração.
4. Abra o CS 1.6 — a mira aparece no centro quando o CS está em foco.

**Atalhos:** `Ctrl + Alt + D` liga/desliga · ícone perto do relógio → *Configurar / Sair*.

### 🎯 Ocultar a mira nativa do CS
O CS 1.6 desenha a mira dele por cima de tudo, então fica feio com o ponto em cima. Marque a opção **"Ocultar a mira nativa do CS"** — o programa grava no `userconfig.cfg` do CS:

```
cl_crosshair_color "0 0 0"
cl_crosshair_translucent "1"
```

Como a mira do CS fica em modo aditivo, o preto deixa ela invisível. **Reinicie o CS** para aplicar.

Não funcionou? Clique no botão **?** ao lado da opção — abre um passo a passo para ocultar na mão, com botão de **copiar os comandos** e **abrir a pasta do CS**:

![Ajuda para ocultar a mira](docs/help.png)

### 🖥️ Tela cheia
O Windows não deixa janelas comuns aparecerem por cima de jogos em "tela cheia exclusiva". Por isso o programa deixa o CS em **tela cheia sem borda** (opção ligada por padrão) — fica idêntico à tela cheia, e a mira aparece por cima. Se ainda assim não aparecer, deixe o CS em modo janela uma vez (*Options → Video → "Run in a window"*).

### ⌨️ Linha de comando
```
MiraPontinho.exe --silent              só a mira (sem abrir a configuração)
MiraPontinho.exe --r 255 --g 0 --b 0   cor inicial
MiraPontinho.exe --size 8              tamanho inicial
MiraPontinho.exe --hide-cs             oculta a mira nativa do CS
MiraPontinho.exe --show-cs             restaura a mira nativa do CS
MiraPontinho.exe --help-manual         abre a ajuda manual
MiraPontinho.exe --no-borderless       não mexe na janela do CS
```

### 🔧 Compilar
Precisa apenas do .NET Framework (já vem no Windows). Rode:

```
build.bat
```

### ⚠️ Aviso
É uma mira cosmética **externa**. O VAC não detecta (não injeta nada no jogo), mas alguns administradores de servidor podem não gostar por ser um recurso externo. Use por sua conta.

---

## 🇬🇧 English

### What is it?
An **external overlay** that draws a crosshair (dot, cross, T, circle or X) in the center of the screen, on top of **Counter-Strike 1.6**.

> **No VAC risk:** it does **not** modify any game file and does **not** inject anything into the CS process. It just draws a window on top (like the built-in crosshair of some gaming monitors). The only (optional) change is a config file (`userconfig.cfg`), which VAC does not check either.

### Features
- Shapes: **Dot, Cross (+), T, Circle and X**
- Adjustable color, size, thickness, gap and outline
- **Live preview**
- Follows the CS window (works windowed and in **borderless fullscreen**)
- **Hides the native CS crosshair** (automatic option + manual help with a **?** button)
- Global hotkey **Ctrl + Alt + D** to toggle
- System tray icon
- Saves settings to `MiraPontinho.ini`
- **Portable**: a single `.exe`, no install

### Usage
1. Download `MiraPontinho.exe` from the **[Releases](https://github.com/taquinoc/cs16-dot-crosshair/releases/latest)** page (or build with `build.bat`).
2. Run `MiraPontinho.exe`.
3. Adjust the crosshair in the settings window.
4. Launch CS 1.6 — the crosshair appears in the center while CS is focused.

### Command line
```
MiraPontinho.exe --silent              overlay only (no settings window)
MiraPontinho.exe --r 255 --g 0 --b 0   initial color
MiraPontinho.exe --size 8              initial size
MiraPontinho.exe --hide-cs             hide the native CS crosshair
MiraPontinho.exe --show-cs             restore the native CS crosshair
MiraPontinho.exe --help-manual         open the manual help
MiraPontinho.exe --no-borderless       do not touch the CS window
```

### Build
Requires only the .NET Framework (bundled with Windows):

```
build.bat
```

### Disclaimer
It is an **external cosmetic** crosshair. VAC does not detect it (nothing is injected into the game), but some server admins may dislike it for being an external tool. Use at your own risk.

---

## 📄 Licença / License
MIT — see [LICENSE](LICENSE).
