# Shell WebP Converter

## Overview
Shell WEBP Converter application designed to simplify the conversion of image files to/from the WebP format. It integrates with the Windows Explorer context menu for quick access and supports custom conversion settings.

### Presets example: 
<img width="923" height="752" alt="image" src="https://github.com/user-attachments/assets/e1ab2a31-0fd3-4219-89e1-02af2f3621a6" />

### Supports the following languages:
English, Russian, Arabic, German, Spanish, French, Hindi, Hungarian, Italian, Japanese, Korean, Portuguese, Slovak, Turkish, Chinese.
## Features
- **GUI Mode**: Provides graphical interface for managing presets, file extensions, and conversion settings.
- **Context Menu Integration**: Adds "Convert to WebP" options to the Windows Explorer context menu.
- **Size trheshold compresison mode**: Allows to compress images to a certain file size.
- **Thread Management**: Optimized for multi-threaded processing.

## Installation
Put the application anywhere you want and launch. 

## Usage
### GUI
1. Launch the application.
2. Configure presets, file extensions, and other settings (see configuration section lower).
3. Use the "Update Menu" button to apply changes to the context menu.
4. Use the "Clear Menu" button to remove context menu entries.

### Context Menu
1. Right-click on a file or folder in Windows Explorer.
2. Select "Convert to WebP" from the context menu.
3. Choose a preset or custom quality option.

### CLI
Conversion module can be used from CLI. Type ***"Shell WebP Converter.exe" --help*** in CMD to see the required arguments.
## Configuration via GUI
- **Processing mode**: choose one of the modes: "Convert to N quality", "Compress to N size", "Convert to SSIM of N" or "Customizable".
- **Settings**: For "Compress to N quality" mode — quality (0-100%) and compression, for "Compress to N size" mode — size, for "To SSIM of N" mode — SSIM (0.0-1.0).
- **Preset name**: Name that displays in the explorer context menu.
- **Postfix**: String added after original file name after conversion.
- **Extensions**: Specify file types to include in the context menu. Supports anything [supported by ImageMagick library](https://imagemagick.com/script/formats.php).
- **Compression**: Adjust compression levels for balancing quality/size ratio and performance. 0 — worst and fastest, 6 — best and slowest. 
- **Delete Original**: Optionally delete original file(-s) after conversion.

## Limitations
- **WebP format**: All WebP file format limitations, like maximum width or height of 16383 pixels.
-  **Silmultaneous processing**: Due to the limitations of adding entries to Windows Explorer context menu via registry editing, only one conversion with custom setting GUI dialog at the time allowed. Same for the folder processing. If you need to apply the same setting trough a custom dialog to a lot of files, put them in a same folder and call conversion for the folder itself. Conversion queue of single files trough presets in unlimited.
-  **Huge processing batches**: If task waits its queue for 8 hours, it automatically gets cancelled.
-  **Compression level for large images (>15 megapixels)**: Depending on the amount of pixels, application may ignore the compresion level you set and automatically increase it to prevent "partition 0 overflow" error (WebP format limitation).
-  **Animated files**: Supported only by "To N Quality" and "To N Size" modes. Also they have mediocre quality because of the file format limitations, use only if audioless video GIF isn't an option.
-  **Conversion from WebP to PNG/JPG**:  Doesn't support custom presets, only built-in ones (however, you can use custom conversion settings from the CLI). Works only for single files, not folders.

## Error Handling
- Errors are logged to `ExceptionLog [date].txt` in the application directory.
- Logs older than 7 days are automatically deleted when new logs are created.

## Requirements
- .NET 10.0 Runtime

## License
This project is licensed under the Apache 2.0 License. See the `LICENSE.txt` file for details.
