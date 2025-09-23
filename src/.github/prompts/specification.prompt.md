---
mode: agent
---
# Spec for an animated diagraming tool

The tool should be able to be deployed as a static webapp, which imports and exports local files and makes use of browser local storage to remember the user's preferences.
The UI should be visually consistent and appealling. It should be easily usable on pen tablet displays of any size, but especially those of small size, where the space should be used efficiently.
There should be a theme system with light/dark mode or system etc.
All of the controls should be easily available in a full view as a pane on the left, with collapsable sections and scroll bar, but normally collapsed to a narrow pane with just a few essential buttons on it, possibly using a hamburger style menu and keyboard binding to toggle between full and collapsed.
When the controls pane is in full view it should push the canvas/viewport over to the right.
Saved Colours should be definable in System Settings for quick access later, all colours should be pickable or entered via testbox and hex values.

It should be a blazor wasm application with clean, modular code, using components and all styles in css files. Also minimal use of js interop.

The tool should always either be in drawing mode when all elements/paths are visible OR animation mode when the visibility of elements/paths is controlled by the animation system.

Playwright tests must be used to ensure previously implemented functionality still works when adding new features.

## Path Editor

The heart of the system is a path editor, which contains SVG elements like paths and circles and also animation hints like Pause and SpeedHint which take the form of xml comments interpretted by the animation system. one or more items in the PathEditor can be selected and moved up or down using buttons or by dragging with the mouse. Items can be deleted. Pause and SpeedHints can be added by pushing dedicated buttons casusing hints to be added above the currently selected PathEditor item. The hint item's UI enables the user to type the pause duration or new speed hint inline. When selecting items in the PathEditor they should be highlighted on the canvas, when in select mode and selecting items on the canvas, they should also be selected in the PathEditor. A Properties box shows properties of the currently selected item in the PathEditor.

It should be possible to browse for SVG files which will be loaded into the PathEditor causing them to be shown fully rendered on the canvas. 
It should be possible to create a new file by pushing a New button which causes the PathEditor and therefore canvas to be cleared ready for new paths to be drawn. The system should track the saved/unsaved status and prompt before allowing new or browsed files to be loaded.

We should be able to save an opened, but edited SVG file or export an SVG file from the current state of the PathEditor, complete with xml comments for the hints.

## Drawing

The system should allow the drawing of new paths by mouse or pen/tablet.
The viewport should always start at the full size of the canvas.
There should be buttons for zooming in and out, a button for changing to select mode and a button which changes to drawing mode and cycles through pens if already in drawing mode, with a visual indicator of which mode we are in and which pen is selected if in drawing mode. 
There should be an undo/redo system, again with buttons. 
All of these buttons should be mapped to keyboard combinations, configured in a Systems Settings UI.
The mouse wheel should also zoom.
Zoom should be zoom in or out at the current mouse position.
Newly drawn paths should always appear directly under the mouse or pen, regardless of zoom or canvas pan.
The drawing experience should be smooth and fluid, not choppy and result in smooth paths with a reasonably small number of nodes and use of curves etc.
It should be possible to do handwriting and hand drawing of boxes, lines, arrows etc to form diagrams.

## Style Rules

There should be a section of controls for creating and editing and deleting Style Override Rules
Each rule should have one or more Conditions i.e ['stroke width' | 'opacity' | 'stroke opacity' | 'path length' ] [ = | < | > | <= | >= ] [editable value] these should be AND'd together
Each rule should have one or more Actions i.e. ['stroke width' | 'opacity' | 'stroke opacity'  | 'glow effect' | 'animation speed' ] = [editable value(s)]
There should be a Show Matches button which highlights matching paths both in the PathEditor and on the canvas.
There should be a Duplicate button which duplicates the selected Rules
Rules should have editable names
Rules should be stored in local storage
The set of Rules should be exportable to json file and importable from json file.
These rules make it possible to tweak elements of SVGs created in other software e.g. Concepts, where opacities and widths can be a bit strange.

## Animation

The should be an animation system which draws the diagrams line-by-line as if watching someone draw lines in real life.
Sometimes dots appear as circles rather than paths, these cannot be stroked but should still appear at the correct time according to the order defined in the PathEditor. 
The tool should have a slider for base speed, speed adjustments can then be made using mulipliers relative to this base speed.
The should be buttons for start/pause, reset and 'show all'.
The Start button will cause the canvas to clear and then one-by-one the current paths in the PathEditor will be animated onto the canvas as a stroke of the path. Pressing this button again during animation will Pause the animation.
The Reset button will clear the canvas ready for the Start button to be pressed.
The Show All button will immediately render the whole diagram to the canvas.
If a Pause hint is encountered while working through the items in the PathEditor then the animation should pause for the configured amount of milliseconds.
If a SpeedHint is encountered while working through the items in the PathEditor then the animation should alter its relative speed according to the configured multiplier.

### Recording

It should be possible to export a video in mp4 or webm format of the animation, there should be controls for FPS, resolution and quality. Also configurable options for a pause atthe end of the video. There should be an option to insert 50ms of the final rendered diagram at the very start of the video (i.e. including the very first frame) as a kind of thumbnail for use in pptx presentations etc, after the 50ms the video should show blank white frames for a configurable amount of time before the actual animation begins. A Record button will start the process, where the video is recorded, doesn't have to be in real time, can be slower and after which a file will be downloaded with the name of [svg-filename][timestamp].webm|mp4