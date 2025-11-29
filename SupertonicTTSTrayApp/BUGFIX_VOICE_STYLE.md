# Bug Fix: Voice Style Loading Error

## Problem

The application was failing during initialization with the following error:

```
KeyNotFoundException: The given key 'ttl' was not present in the dictionary.
```

## Root Cause

The voice style JSON files downloaded from Hugging Face have a different structure than what was documented in the integration guide:

### Expected (from documentation):
```json
{
  "ttl": {
    "data": [...],
    "shape": [1, 8, 16]
  },
  "dp": {
    "data": [...],
    "shape": [1, 8, 16]
  }
}
```

### Actual (in downloaded files):
```json
{
  "style_ttl": {
    "data": [[[nested arrays]]],
    "dims": [1, 8, 16],
    "type": "float32"
  },
  "style_dp": {
    "data": [[[nested arrays]]],
    "dims": [1, 8, 16],
    "type": "float32"
  },
  "metadata": {
    "source_file": "M1.wav",
    ...
  }
}
```

## Key Differences

1. **Field names**: `"style_ttl"` and `"style_dp"` instead of `"ttl"` and `"dp"`
2. **Dimension key**: `"dims"` instead of `"shape"`
3. **Data structure**: Multi-dimensional nested arrays instead of flat arrays
4. **Additional fields**: `"type"` and `"metadata"` present in actual files

## Solution

Updated `TTSHelper.LoadVoiceStyle()` method in `Core/TTSHelper.cs`:

### Changes Made

1. **Updated JSON key names**:
   - Changed `styleDict["ttl"]` → `styleDict["style_ttl"]`
   - Changed `styleDict["dp"]` → `styleDict["style_dp"]`

2. **Updated dimension key**:
   - Changed `.GetProperty("shape")` → `.GetProperty("dims")`

3. **Added array flattening**:
   ```csharp
   private static float[] FlattenJsonArray(JsonElement element)
   {
       var result = new List<float>();
       FlattenRecursive(element, result);
       return result.ToArray();
   }

   private static void FlattenRecursive(JsonElement element, List<float> result)
   {
       if (element.ValueKind == JsonValueKind.Array)
       {
           foreach (var item in element.EnumerateArray())
           {
               FlattenRecursive(item, result);
           }
       }
       else if (element.ValueKind == JsonValueKind.Number)
       {
           result.Add(element.GetSingle());
       }
   }
   ```

4. **Added detailed logging**:
   - Logs file path being loaded
   - Logs parsing progress for TTL and DP
   - Logs array dimensions and data counts

## Testing

After the fix, the initialization log should show:

```
[HH:mm:ss] Loading default voice style (Male1)...
[HH:mm:ss] Loading voice style from: C:\Users\...\voice_styles\M1.json
[HH:mm:ss] Parsing style_ttl...
[HH:mm:ss] TTL data: 1280 floats, dims: [1, 8, 160]
[HH:mm:ss] Parsing style_dp...
[HH:mm:ss] DP data: 128 floats, dims: [1, 8, 16]
[HH:mm:ss] Voice style loaded successfully
[HH:mm:ss] TTS initialization complete!
```

## Files Modified

- `Core/TTSHelper.cs` - Fixed `LoadVoiceStyle()` method, added helper methods
- `CHANGELOG.md` - Documented the fix

## How This Was Discovered

The enhanced logging system showed the exact error:
1. User reported "Initialization failed"
2. Log window showed `KeyNotFoundException: The given key 'ttl' was not present`
3. Examined actual downloaded JSON file structure
4. Discovered mismatch between documentation and reality
5. Fixed code to match actual file format

## Lesson Learned

Always verify actual file formats against documentation, especially when integrating with external models/data. The detailed logging we added made this bug trivial to diagnose and fix.
