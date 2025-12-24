# Quant Helper

Quant Helper is a WPF-based automation tool designed to assist with repetitive tasks through computer vision and input simulation. The application provides multiple automated scripts that can be triggered via keyboard shortcuts.

## Features

### Automation Scripts

The application includes several specialized automation scripts:

- **🔌 Electric Script (Електрик)** - `Ctrl + Q` to select
  - Uses OpenCV template matching to detect damaged fuse elements
  - Automatically clicks on detected elements with configurable precision
  - Supports screen scaling and filtering of overlapping detection points

- **⛏️ Mine Script (Каменяр)** - `Ctrl + U` to select
  - Multi-template stone detection using computer vision
  - Detects 4 different stone types from embedded image resources
  - Performs automated clicking with movement patterns
  - Includes smart filtering to avoid duplicate clicks on the same targets

- **🌳 Tree Chop Script (Лісоруб)** - `Ctrl + T` to select
  - Automated tree chopping simulation
  - Performs repetitive mouse clicking patterns
  - Configurable timing and click sequences

**Note**: Use `F5` to execute any selected script.

### Technical Features

- **Computer Vision Integration**: Uses OpenCV.Sharp for image processing and template matching
- **Embedded Resources**: All image templates are embedded within the executable for self-contained deployment
- **Global Hotkeys**: Uses SharpHook for system-wide keyboard event handling
  - **`Ctrl + Script Key`**: Select/deselect scripts (Q, U, T)
  - **`F5`**: Execute the currently selected script
  - **`Ctrl + H`**: Toggle window transparency and click-through mode
- **Input Simulation**: WindowsInput library for precise mouse and keyboard automation
- **Async/Await Pattern**: All scripts support cancellation tokens for clean shutdown
- **Logging System**: Built-in logging for debugging and monitoring script execution
- **Flexible UI**: Draggable, always-on-top window with optional transparency

## Technology Stack

- **.NET 10** - Latest .NET framework
- **WPF** - Windows Presentation Foundation for the user interface
- **OpenCV Sharp** - Computer vision and image processing
- **SharpHook** - Global keyboard hook implementation
- **H.InputSimulator** - Windows input simulation
- **C# 14.0** - Latest C# language features

## Architecture

### Script System
- **IScript Interface**: Base contract for all automation scripts
- **LoopingScriptBase**: Abstract base class providing common functionality
- **Individual Scripts**: Specialized implementations for different automation tasks

### Resource Management
- **ResourceHelper**: Utility class for accessing embedded image resources
- **Template Matching**: Advanced computer vision algorithms for object detection
- **Memory Management**: Proper disposal patterns for OpenCV resources

### User Interface
- **MainWindow**: WPF-based control interface
- **Real-time Logging**: Live display of script execution status
- **Keyboard Shortcuts**: Global hotkey support for script activation

## Building and Deployment

The application is configured for:
- **Self-contained deployment**: Single executable with all dependencies
- **Windows x64 target**: Optimized for 64-bit Windows systems
- **Ready-to-run compilation**: Enhanced startup performance
- **Embedded resources**: No external file dependencies

## Usage

### Script Selection and Execution

1. **Launch the application**
2. **Select a script** using one of these methods:
   - Click on a script in the GUI list
   - Use global hotkeys: `Ctrl` + script key to select/deselect:
     - `Ctrl + Q` - Select Electric automation
     - `Ctrl + U` - Select Mining automation  
     - `Ctrl + T` - Select Tree chopping automation
3. **Execute the selected script**:
   - **`F5`** - Start/Execute the currently selected script
   - **GUI Button** - Click "?????????" (Start) button in the interface
4. **Monitor execution**:
   - Watch the real-time logging output for script status and results
   - Script status is displayed in the interface
5. **Stop execution**:
   - Press the script's hotkey again (e.g., `Ctrl + Q` to stop Electric script)
   - Use the GUI stop button
   - Close the application

### Additional Controls

- **`Ctrl + H`** - Toggle window transparency and click-through mode
- **Window Dragging** - Click and drag anywhere on the window to move it
- **Always on Top** - Window stays above other applications

### Workflow Example

1. Press `Ctrl + Q` to select the Electric script
2. Press `F5` to start execution
3. Monitor the logging output for detected elements
4. Press `Ctrl + Q` again to stop the script

## Safety and Considerations

This tool is designed for automation tasks and should be used responsibly. Ensure compliance with any applicable terms of service for applications where this automation is used.
