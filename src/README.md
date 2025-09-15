# Animated SVG Diagram

This project creates an animated version of an SVG file, preferably a hand drawn one, where each path is drawn sequentially with a stroke animation effect.

## Files

- `animated-diagram.html` - Main HTML file with the animation interface
- `animation.js` - JavaScript code that handles the SVG animation logic
- `../svg-files/aspire-wire-up.svg` - Source SVG file (201 paths)

## Features

### Animation Controls
- **Speed Control**: Adjust how fast each structural path is drawn (50ms - 2000ms per path)
- **Delay Control**: Set the delay between structural path animations (0ms - 1000ms)
- **Start/Resume**: Begin the animation or resume from pause
- **Pause**: Temporarily stop the animation
- **Reset**: Return all paths to their initial hidden state
- **Show Complete**: Instantly display the complete diagram

### Path Editor
- **Visual Path Management**: Interactive list showing all paths and circles in drawing order
- **Drag & Drop Reordering**: Change animation sequence by dragging paths up/down
- **Multi-Selection**: Select multiple paths for batch operations (Ctrl+click, Shift+click)
- **Path Actions**: Move selected paths to top/bottom, delete paths, export modified SVG
- **Pause Insertion**: Add animation pauses at any point in the sequence
- **Path Properties**: View detailed information about selected paths (length, color, type)

### Animation Pauses
- **Insert Pauses**: Add timed pauses between any paths using the Path Editor
- **Custom Duration**: Set pause duration in milliseconds (e.g., 1000ms = 1 second)
- **Visual Indicators**: Pause points are shown in the Path Editor with duration and location
- **Easy Management**: Remove pauses with a single click using the ✕ button
- **XML Comments**: Pauses are stored as `<!--PAUSE:duration-->` comments in the SVG file

### Handwriting Detection
- **Color Selection**: Choose which stroke color represents handwriting (default: black #000000)
- **Length Threshold**: Set maximum path length for handwriting detection (50-1000 pixels)
- **Handwriting Speed**: Control how fast handwriting paths are drawn (10-500ms per path)

The system automatically categorizes paths into two types:
1. **Handwriting Paths**: Paths matching the specified color and under the length threshold
2. **Structural Paths**: All other paths (typically diagrams, shapes, or longer strokes)

### Sequential Animation Mode
The animation runs a single sequential drawing sequence that follows the original creation order:
- **File Order Drawing**: Paths are drawn in the exact order they appear in the SVG file
- **Original Sequence**: Preserves the intended drawing order from the original creation
- **Dynamic Timing**: Each path uses different speed and delay based on its type:
  - **Structural paths**: Use the main speed setting with delays between paths
  - **Handwriting paths**: Draw faster with no delays for natural writing flow
- **Single Hand Simulation**: Like a person drawing with one pen, only one path animates at a time

### Animation Technique
The animation uses SVG `stroke-dasharray` and `stroke-dashoffset` properties to create a "drawing" effect:
1. Each path's length is calculated using `getTotalLength()`
2. `stroke-dasharray` is set to the path length (creating one dash the length of the entire path)
3. `stroke-dashoffset` is initially set to the path length (hiding the stroke)
4. Animation gradually reduces `stroke-dashoffset` to 0 (revealing the stroke)

## Usage

1. Open `animated-diagram.html` in a web browser
2. The SVG will load automatically from the `../svg-files/` directory
3. Adjust the speed and delay settings as desired
4. Click "Start Animation" to begin the path-by-path drawing animation
5. Use the other controls to pause, reset, or show the complete diagram

## Browser Compatibility

This animation works in all modern browsers that support:
- ES6+ JavaScript features (classes, async/await, arrow functions)
- SVG manipulation via DOM
- CSS transitions on SVG properties

## Technical Details

- **Total Paths**: 201 paths in the SVG
- **Animation Method**: CSS transitions on `stroke-dashoffset`
- **Path Detection**: Automatically finds all `<path>` elements in the loaded SVG
- **Path Categorization**: Analyzes stroke color and path length to separate handwriting from structural elements
- **Sequential Animation**: Single drawing sequence that follows the original file order
- **File Order Preservation**: Paths are drawn in the exact sequence they were defined in the SVG
- **Dynamic Timing**: Each path uses appropriate speed and delay based on its type within the sequence
- **Progress Tracking**: Real-time display of animation progress for both path types
- **Memory Management**: Proper cleanup of timeouts and event listeners
- **Dynamic Recategorization**: Paths are re-categorized when detection settings change

## Customization

You can easily adapt this system for other SVG files by:
1. Replacing the SVG file in the `svg-files` directory
2. Updating the fetch URL in `animation.js` if needed
3. Adjusting the default speed/delay values in the JavaScript

The system automatically detects and animates all paths in any SVG file, making it highly reusable.

## ToDo

* ✅ ability to insert a pause into the SVG file, probably an xml comment which is interpretted by the animation as a pause

* ✅ ability to insert main speed hints, again probably xml comments again probably xml comments, which allow a specific part of the diagram to be sped up or slowed down.

* ability to set a viewport to naviagate to, again probably xml comments again probably xml comments

* investigate how hard it would be to add a page to actually draw the diagrams with a drawing tablet and simple tool pallet

* undo/redo buffer