# Troubleshooting Guide

## Using the Initialization Log

The application now has a built-in **Initialization Log** window at the bottom that shows detailed information about what's happening during startup. This is your primary tool for diagnosing issues.

### How to Read the Log

Each log entry has a timestamp in `[HH:mm:ss]` format:

```
[12:34:56] Starting TTS initialization...
[12:34:56] Models directory: C:\Users\YourName\AppData\Local\SupertonicTTS\Models
```

### What to Look For

1. **Success Pattern**:
```
[12:34:56] Starting TTS initialization...
[12:34:56] Checking/downloading models...
[12:34:56] Models already downloaded.
[12:34:57] Models ready. Loading TTS engine...
[12:34:58] TTS engine loaded successfully
[12:34:58] Loading default voice style (Male1)...
[12:34:58] Voice style loaded successfully
[12:34:58] TTS initialization complete!
[12:34:58] TTS initialization successful!
```

2. **Download Pattern** (first run):
```
[12:34:56] Downloading Supertonic TTS models...
[12:34:57] [1/10] Downloading duration_predictor.onnx...
[12:34:58] Download progress: duration_predictor.onnx - 10% (1/10)
[12:35:00] Download progress: text_encoder.onnx - 20% (2/10)
...
[12:37:30] All models downloaded successfully!
```

## Common Error Patterns and Solutions

### Error: Failed to download models

**Log shows:**
```
[12:34:56] ERROR: Initialization returned false
[12:34:56] Check internet connection and firewall settings
```

**Solutions:**
1. Check your internet connection
2. Verify you can access https://huggingface.co in a browser
3. Check firewall settings - allow the app to access the internet
4. Check proxy settings if behind a corporate proxy
5. Click the **Retry** button after fixing the issue

### Error: Cannot access models directory

**Log shows:**
```
[12:34:56] EXCEPTION: UnauthorizedAccessException: Access denied
[12:34:56] Stack trace: at System.IO.Directory.CreateDirectory(...)
```

**Solutions:**
1. Check if you have write permissions to `%LocalAppData%`
2. Run the application as a normal user (not as administrator)
3. Check if antivirus is blocking access to AppData
4. Manually create the directory: `%LocalAppData%\SupertonicTTS\Models`

### Error: Model file not found

**Log shows:**
```
[12:34:56] EXCEPTION: FileNotFoundException: Could not find file 'text_encoder.onnx'
[12:34:56] ONNX directory: C:\Users\...\SupertonicTTS\Models\onnx
```

**Solutions:**
1. Delete the models directory: `%LocalAppData%\SupertonicTTS\Models`
2. Click the **Retry** button to re-download all models
3. Check if antivirus quarantined any files

### Error: ONNX Runtime error

**Log shows:**
```
[12:34:56] EXCEPTION: OnnxRuntimeException: Failed to load model
[12:34:56] Inner exception: Invalid model file
```

**Solutions:**
1. Models may be corrupted during download
2. Delete models directory: `%LocalAppData%\SupertonicTTS\Models`
3. Click **Retry** to re-download
4. Ensure you have enough free disk space (~300MB)

### Error: Out of memory

**Log shows:**
```
[12:34:56] EXCEPTION: OutOfMemoryException
```

**Solutions:**
1. Close other applications to free up RAM
2. Restart your computer
3. Ensure you have at least 2GB of available RAM
4. Check if running a 32-bit version of .NET (should be 64-bit)

### Error: Timeout downloading models

**Log shows:**
```
[12:40:00] EXCEPTION: TaskCanceledException: A task was canceled
[12:40:00] Inner exception: The operation has timed out
```

**Solutions:**
1. Your internet connection may be too slow
2. Hugging Face servers may be experiencing issues
3. Try again later
4. Use the **Retry** button
5. Check if download speed is being throttled

## Manual Model Download

If automatic download repeatedly fails, you can manually download models:

1. **Visit**: https://huggingface.co/Supertone/supertonic/tree/main

2. **Download these files**:
   - `onnx/duration_predictor.onnx` (1.6 MB)
   - `onnx/text_encoder.onnx` (28 MB)
   - `onnx/vector_estimator.onnx` (132.5 MB)
   - `onnx/vocoder.onnx` (101.4 MB)
   - `onnx/tts.json` (< 1 KB)
   - `onnx/unicode_indexer.json` (< 1 KB)
   - `voice_styles/M1.json` (< 1 KB)
   - `voice_styles/M2.json` (< 1 KB)
   - `voice_styles/F1.json` (< 1 KB)
   - `voice_styles/F2.json` (< 1 KB)

3. **Place them in**:
   ```
   C:\Users\YourName\AppData\Local\SupertonicTTS\Models\
   ├── onnx\
   │   ├── duration_predictor.onnx
   │   ├── text_encoder.onnx
   │   ├── vector_estimator.onnx
   │   ├── vocoder.onnx
   │   ├── tts.json
   │   └── unicode_indexer.json
   └── voice_styles\
       ├── M1.json
       ├── M2.json
       ├── F1.json
       └── F2.json
   ```

4. **Restart the application** or click **Retry**

## Getting Help

When reporting an issue, please include:

1. **Full log contents** - Copy everything from the Initialization Log window
2. **Windows version** - Run `winver` to check
3. **.NET version** - Run `dotnet --version` in Command Prompt
4. **Steps to reproduce** - What you did before the error occurred
5. **Error screenshot** - If applicable

### Where to Report Issues

Include the log contents when reporting to help diagnose the issue faster.

## Quick Checklist

Before reporting an issue, try these steps:

- [ ] Check the Initialization Log for specific error messages
- [ ] Click the **Retry** button
- [ ] Verify internet connection
- [ ] Check if `%LocalAppData%` is accessible
- [ ] Ensure at least 300MB free disk space
- [ ] Check if antivirus is blocking the app
- [ ] Try manual model download
- [ ] Restart the application
- [ ] Restart your computer

## Advanced Diagnostics

### Check Model Files

Open Command Prompt and run:
```cmd
dir "%LocalAppData%\SupertonicTTS\Models\onnx"
dir "%LocalAppData%\SupertonicTTS\Models\voice_styles"
```

You should see all 10 files listed.

### Verify .NET Installation

```cmd
dotnet --list-runtimes
```

Should show `Microsoft.NETCore.App 8.0.x` or later.

### Check Disk Space

```cmd
wmic logicaldisk get caption,freespace
```

Ensure the drive with AppData has at least 300MB free.
