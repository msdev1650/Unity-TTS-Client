# TTSClient for Unity

TTSClient is a powerful Unity plugin that integrates Google Cloud Text-to-Speech API into your Unity projects. It allows for seamless conversion of text to lifelike speech, enhancing the audio experience in games, applications, and interactive experiences.

![Screenshot_TTSClient](https://github.com/user-attachments/assets/4eabc071-bbe5-47d0-868f-7036f9357648)

## Features

- Easy integration with Google Cloud Text-to-Speech API
- Support for multiple languages and voices
- Real-time text-to-speech conversion
- Customizable audio settings (sample rate, encoding)
- Secure API key encryption
- Editor tools for easy setup and management
- Option to trim audio clip ends
- Automatic audio playback functionality

## Installation

1. Clone this repository or download the latest release.
2. Import the TTSClient folder into your Unity project's Assets directory.
3. Unity will automatically compile the scripts and make the tool available.

## Setup

1. Obtain a Google Cloud API key with access to the Text-to-Speech API.
2. In Unity, select the GameObject you want to add the TTSClient to.
3. Add the TTSClient component to the GameObject.
4. In the Inspector, click on "Encrypt and Set API Key" to securely store your API key.

## Usage

### Basic Usage

1. In the TTSClient component, set your desired language, voice, and audio settings.
2. Use one of the following methods to generate speech:
   - Call `SynthesizeTextFromInput()` to use text from a linked TMP_InputField.
   - Call `SynthesizeTextFromConfig()` to use text from the TTSText field in the Inspector.
   - Call `SynthesizeText(string text)` from your scripts to generate speech programmatically.

### Advanced Usage

- Adjust `trimEnds`, `trimStart`, and `trimEnd` to fine-tune audio clip trimming.
- Set `playWhenGenerated` to true for automatic audio playback upon generation.
- Use `SetLanguage(int language)` to switch between supported languages dynamically.

## API Reference

### Public Methods

- `SynthesizeTextFromInput()`: Synthesizes speech from the linked TMP_InputField.
- `SynthesizeTextFromConfig()`: Synthesizes speech from the TTSText field in the Inspector.
- `SynthesizeText(string text, string ssmlGender = null, Action<AudioClip> callback = null)`: Synthesizes speech from the provided text.
- `SetLanguage(int language)`: Sets the language for TTS (0 for English, 1 for German).

### Public Properties

- `EncryptedApiKey`: Gets or sets the encrypted Google Cloud API key.
- `AesKey`: Gets or sets the AES encryption key (stored as Base64 string).
- `AesIV`: Gets or sets the AES encryption IV (stored as Base64 string).

## Contributing

Contributions to TTSClient are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Google Cloud Text-to-Speech API
- Unity Technologies

## Disclaimer

This tool interacts with Google Cloud services. Ensure you comply with Google Cloud's terms of service and pricing when using this plugin in your projects.
