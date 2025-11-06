# Shell WebP Converter

## Overview
Shell WEBP Converter application designed to simplify the conversion of image files to the WebP format. The program integrates with the Windows context menu for quick access and supports custom conversion settings.

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
2. Configure presets, file extensions, and compression settings (see configuration section lower).
3. Use the "Update Menu" button to apply changes to the context menu.
4. Use the "Clear Menu" button to remove context menu entries.

### Context Menu
1. Right-click on a file or folder in Windows Explorer.
2. Select "Convert to WebP" from the context menu.
3. Choose a preset or custom quality option.

### CLI
Conversion module can be used from CLI. Type ***"Shell WebP Converter.exe" --help*** in CMD to see the required arguments.
## Configuration via GUI
### Basic mode
- **Presets**: Define quality levels (0-100%) for quick access trough the context menu. 100 — losless, 85 — 85% quality, -1 — custom mode, shows GUI dialog with settings when called trough the context menu.
### Advanced mode
- **Processing mode**: choose one of the modes: "Compress to N quality", "Compress to N size", "Customizable" (equivalent of -1 in basic mode).
- **Settings**: For "Compress to N quality mode" — quality (0-100%) and compression, for "Compress to N size mode" — size.
- **Preset name**: Name that displays in the explorer context menu.
- **Postfix**: String added after original file name after conversion.
### Both modes
- **Extensions**: Specify file types to include in the context menu. Supports anything [supported by ImageMagick library](https://imagemagick.com/script/formats.php).
- **Compression**: Adjust compression levels for balancing quality/size ratio and performance. 0 — worst and fastest, 6 — best and slowest. 
- **Delete Original**: Optionally delete original file(-s) after conversion.

## Limitations
- **WebP format**: All WebP file format limitations, like maximum width or height of 16383 pixels.
-  **Silmultaneous processing**: Due to the limitations of adding entries to Windows Explorer context menu via registry editing, only one conversion with custom setting at the time allowed. If you need to apply the same setting to a lot of files, put them in a same folder and call conversion for the folder itself.
-  **Huge processing batches**: If task waits its queue for 8 hours, it automatically gets cancelled.
-  **Compression level for large images (>15 megapixels)**: Depending on the amount of pixels, application may ignore the compresion level you set and automatically increase it to prevent "partition 0 overflow" error (WebP format limitation).

## Error Handling
- Errors are logged to `ExceptionLog [date].txt` in the application directory.
- Logs older than 7 days are automatically deleted when new logs are created.

## Requirements
- .NET 9.0 Runtime

## License
This project is licensed under the Apache 2.0 License. See the `LICENSE.txt` file for details.
