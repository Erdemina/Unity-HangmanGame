# Unity Hangman Game

A classic Hangman game built with Unity. This project demonstrates fundamental Unity features, C# scripting, and simple game logic suitable for beginners and as a foundation for more complex word games.

## Features

- Interactive Hangman gameplay
- Random word selection
- Visual hangman drawing that updates on wrong guesses
- Game over and win conditions with simple UI feedback
- Easily extendable word list

## Getting Started

### Prerequisites

- [Unity](https://unity.com/) (Recommended version: 2021.3 LTS or later)
- [Git](https://git-scm.com/) (optional, for cloning)

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Erdemina/Unity-HangmanGame.git
   ```
2. **Open the project in Unity:**
   - Launch Unity Hub.
   - Click on "Open" and select the cloned repository folder.

3. **Run the game:**
   - Open the `Scenes` folder and double-click the main scene (e.g., `Hangman.unity`).
   - Press the Play button in the Unity Editor.

## Project Structure

```
Unity-HangmanGame/
├── Assets/
│   ├── Scripts/          # C# scripts for game logic
│   ├── Scenes/           # Unity scene files
│   ├── Prefabs/
│   ├── Sprites/          # Game art and images
│   └── ...               # Other assets
├── ProjectSettings/
├── README.md
└── ...
```

## How to Play

- The game will randomly select a word.
- Guess one letter at a time.
- Each incorrect guess draws a part of the hangman.
- Win by guessing all letters before the hangman is fully drawn.
- Lose if the hangman is completed before the word is guessed.

## Customization

- **Add Words:**  
  Edit the word list in the script (usually `WordList.cs`) to add or remove words.
- **Change Art:**  
  Replace sprites in the `Sprites/` folder for a custom look.

## Contributing

Pull requests are welcome! For major changes, please open an issue first to discuss what you would like to change.

## License

[MIT](LICENSE)

## Credits

- Developed by [Erdemina](https://github.com/Erdemina)
- Developed by [baslarbatuhan](https://github.com/baslarbatuhan)
- Made with [Unity](https://unity.com/)

---
Happy Coding & Have Fun!
