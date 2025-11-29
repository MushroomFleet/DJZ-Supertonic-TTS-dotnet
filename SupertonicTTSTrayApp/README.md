# Supertonic TTS System Tray App

**Version 0.5.0** - A fully functional Windows Forms system tray application that uses Supertonic TTS to convert text to speech and save as WAV files.

![License](https://img.shields.io/badge/license-Follows%20Supertonic%20TTS-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Status](https://img.shields.io/badge/status-working-brightgreen)

## Features

- ✅ **Working System Tray Application** - Runs quietly in the background
- ✅ **Simple User Interface** - Easy text-to-speech conversion
- ✅ **4 Voice Options** - Male 1, Male 2, Female 1, Female 2
- ✅ **Automatic Model Downloading** - First-run downloads from Hugging Face
- ✅ **Detailed Logging** - Real-time initialization and error tracking
- ✅ **Retry Functionality** - Recover from initialization failures
- ✅ **High-Quality Output** - 44100 Hz, 16-bit PCM WAV files
- ✅ **On-Device Processing** - No cloud API calls required
- ✅ **Production Ready** - Fully tested and debugged

## Requirements

- .NET 8.0 SDK or later
- Windows OS (WinForms application)
- ~300MB disk space for TTS models
- 2GB+ available RAM
- Internet connection (first run only, for model download)

## Installation

### Quick Start

1. **Clone the repository**:
   ```bash
   git clone https://github.com/MushroomFleet/DJZ-Supertonic-TTS-dotnet.git
   cd DJZ-Supertonic-TTS-dotnet/SupertonicTTSTrayApp
   ```

2. **Build the application**:
   ```bash
   dotnet build -c Release
   ```

3. **Run the application**:
   ```bash
   dotnet run
   ```
   Or run the executable:
   ```bash
   .\bin\Release\net8.0-windows\SupertonicTTSTrayApp.exe
   ```

## First Run

On the first run, the application will:
1. Display a progress bar with status updates
2. Download required TTS models (~260MB) from Hugging Face
3. Initialize the TTS engine
4. Display "Ready to generate speech!" when complete

**Models are downloaded to**: `%LocalAppData%\SupertonicTTS\Models`

The initialization log window shows real-time progress and any errors.

## Usage

1. **Launch** the application - it will appear in the system tray
2. **Double-click** the tray icon to show the main window
3. **Select a voice** from the dropdown (Male 1, Male 2, Female 1, Female 2)
4. **Enter text** in the text box
5. **Click "Generate Speech"**
6. **Save location**: WAV files are saved to your Documents folder
7. **Optional**: Click "Yes" to open the folder containing the generated file

### System Tray Features

- **Double-click icon**: Show/restore the main window
- **Right-click menu**:
  - Show - Show the main window
  - Exit - Close the application

## Output Files

Generated WAV files are automatically named with timestamps:
- **Format**: `supertonic_tts_YYYYMMDD_HHMMSS.wav`
- **Location**: Your Documents folder (`%USERPROFILE%\Documents`)
- **Sample Rate**: 44100 Hz
- **Bit Depth**: 16-bit PCM
- **Channels**: Mono

## Voice Options

1. **Male 1** (`M1.json`) - Natural male voice
2. **Male 2** (`M2.json`) - Alternative male voice
3. **Female 1** (`F1.json`) - Natural female voice
4. **Female 2** (`F2.json`) - Alternative female voice

## Application Structure

```
SupertonicTTSTrayApp/
├── Core/                       # Core TTS engine classes
│   ├── TTSConfig.cs           # Configuration classes
│   ├── UnicodeProcessor.cs    # Text preprocessing and normalization
│   ├── TextToSpeech.cs        # Main TTS engine with flow-matching
│   └── TTSHelper.cs           # Helper utilities and model loading
├── Services/                   # Service layer
│   ├── ModelDownloader.cs     # Automatic model downloading from HuggingFace
│   └── SupertonicTTSService.cs # High-level TTS service wrapper
├── TTSTrayForm.cs             # Main UI form with system tray integration
└── Program.cs                 # Application entry point
```

## Technology Stack

- **.NET 8.0** - Application framework
- **Windows Forms** - UI framework
- **Microsoft.ML.OnnxRuntime 1.20.1** - ONNX model inference
- **Supertonic TTS Models** - High-quality open-source TTS models

## Architecture

The TTS pipeline uses a flow-matching approach with iterative denoising:

1. **Text Processing** → Unicode normalization and indexing
2. **Duration Predictor** → Predicts speech duration from text
3. **Text Encoder** → Generates text embeddings with style conditioning
4. **Vector Estimator** → Iterative denoising (5 steps default)
5. **Vocoder** → Converts latent representation to audio waveform

## Troubleshooting

The application includes a detailed **Initialization Log** window that shows exactly what's happening during startup and speech generation.

### Common Issues

#### Initialization Fails
1. **Check the log window** - Shows detailed error messages with stack traces
2. **Click the Retry button** - Attempt initialization again
3. **Common causes**:
   - No internet connection (models must download on first run)
   - Firewall blocking access to huggingface.co
   - Insufficient disk space (~300MB required)
   - Antivirus blocking downloads

#### Models Fail to Download
- Check initialization log for specific errors
- Verify internet connection
- Ensure Hugging Face (huggingface.co) is accessible
- Check firewall/proxy settings
- Use Retry button after fixing connection
- As last resort, manually download models (see TROUBLESHOOTING.md)

#### "Attempted to divide by zero" Error
- This was a bug in v0.4.0 and earlier
- Fixed in v0.5.0 by properly parsing tts.json configuration
- Update to latest version if you see this error

#### Application Won't Start
- Ensure .NET 8.0 Runtime is installed
- Run `dotnet --version` to verify (should show 8.0.x)
- Check system requirements
- Look for error details in initialization log

### Reading the Log

The initialization log shows:
- Model download progress with percentages
- File paths being used
- Configuration values (sample rate, chunk size, etc.)
- Any errors with full stack traces
- Step-by-step initialization progress

Copy log contents when reporting issues.

## Performance

- **Initialization**: ~2-3 seconds (after models downloaded)
- **Speech Generation**: ~1-2 seconds per sentence (CPU, 5 denoising steps)
- **Memory Usage**: ~500MB during synthesis
- **Disk Space**: ~260MB for models

## Development

### Building from Source

```bash
cd SupertonicTTSTrayApp
dotnet restore
dotnet build
```

### Creating a Release Build

```bash
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained
```

## Version History

### v0.5.0 (Current) - Production Ready ✅
- ✅ **FIXED**: Divide by zero error in TTS pipeline
- ✅ **FIXED**: Configuration loading from tts.json
- ✅ **FIXED**: Correct ONNX model input/output names
- ✅ **FIXED**: Voice style JSON parsing (style_ttl, style_dp)
- ✅ **ADDED**: Comprehensive error logging and diagnostics
- ✅ **ADDED**: Iterative denoising with flow-matching
- ✅ **VERIFIED**: Full end-to-end TTS pipeline working

### v0.4.0 - Architecture Rewrite
- Rewrote TTS pipeline to match official reference implementation
- Changed model execution order
- Added proper latent noise sampling with Box-Muller transform
- Implemented iterative denoising loop

### v0.3.0 - Voice Style Fixes
- Fixed voice style JSON key names (style_ttl, style_dp)
- Implemented recursive array flattening
- Added detailed voice style parsing logs

### v0.2.0 - Enhanced Error Handling
- Added initialization log window
- Added retry button for failed initialization
- Console output redirection to UI
- Comprehensive exception logging

### v0.1.0 - Initial Release
- Basic system tray application
- Automatic model downloading
- Four voice options

## Known Issues

None! The application is fully functional in v0.5.0.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## Credits

Based on the Supertonic TTS project:
- **Official Repository**: https://github.com/supertone-inc/supertonic
- **Official C# Example**: https://github.com/supertone-inc/supertonic (see csharp/ folder)
- **This Implementation**: https://github.com/MushroomFleet/DJZ-Supertonic-TTS-dotnet

## License

This project follows the Supertonic TTS license. See the official repository for details.

## Support

For issues, questions, or contributions:
- **GitHub Issues**: https://github.com/MushroomFleet/DJZ-Supertonic-TTS-dotnet/issues
- **Documentation**: See TROUBLESHOOTING.md and BUGFIX_VOICE_STYLE.md

---

**Status**: ✅ Production Ready - v0.5.0 fully tested and working!
