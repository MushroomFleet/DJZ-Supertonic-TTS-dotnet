# Changelog

## [Latest] - ONNX Tensor Input Names Fix

### Bug Fixes

#### Fixed ONNX Model Input Tensor Names
- **Issue**: Text Encoder model needs `"style_ttl"` input (for speech_prompted_text_encoder)
- **Issue**: Duration Predictor needs `"style_dp"` input
- **Issue**: Vector Estimator needs `"style_ttl"` input
- **Error**: `RuntimeException: Missing Input: style_ttl` in text encoder
- **Fix**: Added style tensors to all models that need them

#### Changes Made
- Modified `Core/TextToSpeech.cs`:
  - Moved style tensor creation to top of `Call()` method
  - Added `"style_ttl"` input to Text Encoder
  - Changed Duration Predictor style input from `"style"` → `"style_dp"`
  - Changed Vector Estimator style input from `"style"` → `"style_ttl"`
  - Removed duplicate tensor creations

## Voice Style Loading Fix

### Bug Fixes

#### Fixed Voice Style JSON Parsing
- **Issue**: Voice style files use `"style_ttl"` and `"style_dp"` keys, not `"ttl"` and `"dp"`
- **Issue**: Voice style files use `"dims"` instead of `"shape"` for dimension arrays
- **Issue**: Data is nested in multi-dimensional arrays requiring flattening
- **Fix**: Updated `LoadVoiceStyle()` method to use correct JSON keys
- **Fix**: Implemented recursive array flattening for nested data structures
- **Fix**: Added detailed logging during voice style parsing

#### Changes Made
- Modified `TTSHelper.LoadVoiceStyle()` to read:
  - `"style_ttl"` instead of `"ttl"`
  - `"style_dp"` instead of `"dp"`
  - `"dims"` instead of `"shape"`
- Added `FlattenJsonArray()` helper method
- Added `FlattenRecursive()` helper method for nested array processing
- Added console logging for debugging voice style loading

## Enhanced Error Handling and Logging

### New Features Added

#### 1. Initialization Log Window
- Added a scrollable log text box at the bottom of the main window
- Shows real-time initialization progress with timestamps
- Displays all console output including:
  - Model download progress
  - File paths being used
  - Detailed error messages with stack traces
  - Step-by-step initialization progress

#### 2. Retry Button
- Appears next to the progress bar when initialization fails
- Allows users to retry initialization without restarting the app
- Automatically hides when initialization is in progress
- Re-enables after a failure

#### 3. Enhanced Error Messages
- All exceptions now show:
  - Exception type
  - Error message
  - Inner exception details (if any)
  - Full stack trace
- Console output redirected to the log window
- Detailed logging at each initialization step

#### 4. UI Improvements
- Increased window height to 550px to accommodate log window
- Added "Initialization Log:" label
- Progress bar resized to make room for Retry button
- Better status messages indicating where to look for details

### Technical Changes

#### TTSTrayForm.cs
- Added `_logTextBox` and `_retryButton` controls
- Implemented `LogMessage()` method with thread-safe logging
- Added `LogTextWriter` class to redirect Console output to UI
- Enhanced `InitializeTTSAsync()` with detailed logging at each step
- Better exception handling with full error details

#### SupertonicTTSService.cs
- Changed exception handling to re-throw instead of returning false
- Added Console.WriteLine at each initialization step
- More descriptive error messages
- Detailed logging of paths and progress

#### ModelDownloader.cs
- Enhanced exception logging with type, message, and stack trace
- Added inner exception details

### User Benefits

1. **Easier Troubleshooting**: Users can now see exactly where initialization fails
2. **No Restart Required**: Retry button allows fixing issues and retrying
3. **Better Error Reporting**: Log can be copied and shared when reporting issues
4. **Transparency**: Users know what the app is doing at each step
5. **Self-Service**: Many issues can be diagnosed without developer help

### Example Log Output

```
[12:34:56] Starting TTS initialization...
[12:34:56] Models directory: C:\Users\User\AppData\Local\SupertonicTTS\Models
[12:34:56] Calling InitializeAsync...
[12:34:56] Starting TTS initialization...
[12:34:56] Checking/downloading models...
[12:34:56] Models already downloaded.
[12:34:57] Models ready. Loading TTS engine...
[12:34:57] ONNX directory: C:\Users\User\AppData\Local\SupertonicTTS\Models\onnx
[12:34:58] TTS engine loaded successfully
[12:34:58] Loading default voice style (Male1)...
[12:34:58] Voice style loaded successfully
[12:34:58] TTS initialization complete!
[12:34:58] InitializeAsync returned: True
[12:34:58] TTS initialization successful!
```

### Common Issues Now Visible in Log

1. **Network errors** - Shows if Hugging Face is unreachable
2. **File permission errors** - Shows if can't write to AppData
3. **Missing files** - Shows which model files are missing
4. **ONNX runtime errors** - Shows model loading failures
5. **Memory errors** - Shows out-of-memory exceptions
