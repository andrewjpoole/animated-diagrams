class SVGAnimator {
    constructor() {
        this.paths = [];
        this.circles = [];
        this.allElements = []; // Combined paths and circles in document order
        this.sortedPaths = []; // Deprecated, keeping for compatibility
        this.handwritingPaths = [];
        this.structuralPaths = [];
        this.currentPathIndex = 0;
        this.isAnimating = false;
        this.isPaused = false;
        this.animationTimeout = null;
        this.animationSpeed = 500; // milliseconds per structural path
        this.animationDelay = 100; // delay between structural paths
        this.handwritingColor = '#000000'; // color to detect for handwriting
        this.lengthThreshold = 200; // max length for handwriting paths
        this.currentSVGFilename = 'aspire-wire-up.svg'; // Store current SVG filename
        this.selectedSVGFile = 'aspire-wire-up.svg'; // Store selected SVG file for localStorage
        this.currentSpeedMultiplier = 1.0; // Current speed hint multiplier that stays in effect
        
        // Load saved settings from localStorage
        this.loadSettings();
        
        this.initializeControls();
        this.populateSVGDropdown().then(() => {
            // Load the appropriate SVG file
            if (this.customSvgFile && this.selectedSVGFile === this.customSvgFile.name) {
                this.loadSVGFromContent(this.customSvgFile.content, this.customSvgFile.name);
            } else {
                this.loadSVG(this.selectedSVGFile);
            }
        });
    }

    // Save animation settings to localStorage
    saveSettings() {
        const settings = {
            animationSpeed: this.animationSpeed,
            animationDelay: this.animationDelay,
            selectedSVGFile: this.selectedSVGFile,
            customSvgFile: this.customSvgFile ? {
                name: this.customSvgFile.name,
                content: this.customSvgFile.content,
                lastModified: this.customSvgFile.lastModified
            } : null
        };
        localStorage.setItem('svgAnimatorSettings', JSON.stringify(settings));
        console.log('Settings saved:', settings);
    }

    // Load animation settings from localStorage
    loadSettings() {
        try {
            const savedSettings = localStorage.getItem('svgAnimatorSettings');
            if (savedSettings) {
                const settings = JSON.parse(savedSettings);
                this.animationSpeed = settings.animationSpeed !== undefined ? settings.animationSpeed : 500;
                this.animationDelay = settings.animationDelay !== undefined ? settings.animationDelay : 100;
                this.selectedSVGFile = settings.selectedSVGFile || 'aspire-wire-up.svg';
                
                // Restore custom SVG file if it exists
                if (settings.customSvgFile) {
                    this.customSvgFile = {
                        name: settings.customSvgFile.name,
                        content: settings.customSvgFile.content,
                        lastModified: settings.customSvgFile.lastModified,
                        blobUrl: URL.createObjectURL(new Blob([settings.customSvgFile.content], { type: 'image/svg+xml' }))
                    };
                }
                
                console.log('Settings loaded:', settings);
                console.log('Applied values - Speed:', this.animationSpeed, 'Delay:', this.animationDelay, 'SVG:', this.selectedSVGFile);
            }
        } catch (error) {
            console.warn('Failed to load settings from localStorage:', error);
        }
    }

    // Refresh control values from current settings (call this after DOM is ready)
    refreshControlValues() {
        const speedRange = document.getElementById('speed');
        const speedValue = document.getElementById('speedValue');
        const delayRange = document.getElementById('delay');
        const delayValue = document.getElementById('delayValue');
        
        if (speedRange && speedValue) {
            speedRange.value = this.animationSpeed;
            speedValue.value = this.animationSpeed;
            console.log('Speed controls updated to:', this.animationSpeed);
        }
        
        if (delayRange && delayValue) {
            delayRange.value = this.animationDelay;
            delayValue.value = this.animationDelay;
            console.log('Delay controls updated to:', this.animationDelay);
        }
    }

    // Discover available SVG files
    async discoverSVGFiles() {
        // List of SVG files to check for
        const possibleFiles = [
            'aspire-wire-up.svg',
            'concepts.svg', 
            'complex-scenario2.svg',
            'concepts_reordered.svg',
            'diagram1.svg',
            'diagram2.svg',
            'diagram3.svg',
            'example.svg',
            'test.svg'
        ];
        
        const availableFiles = [];
        
        for (const filename of possibleFiles) {
            try {
                const response = await fetch(`../svg-files/${filename}`, { method: 'HEAD' });
                if (response.ok) {
                    availableFiles.push(filename);
                    console.log(`Found SVG file: ${filename}`);
                }
            } catch (error) {
                // File doesn't exist, skip it
            }
        }
        
        console.log(`Discovered ${availableFiles.length} SVG files:`, availableFiles);
        return availableFiles;
    }

    // Populate SVG dropdown with available files
    async populateSVGDropdown() {
        const dropdown = document.getElementById('svgFile');
        if (!dropdown) {
            console.warn('SVG dropdown not found');
            return;
        }
        
        const availableFiles = await this.discoverSVGFiles();
        
        // Clear existing options
        dropdown.innerHTML = '';
        
        // Add available files as options
        availableFiles.forEach(filename => {
            const option = document.createElement('option');
            option.value = filename;
            option.textContent = filename;
            dropdown.appendChild(option);
        });
        
        // Add custom file if it was previously loaded
        if (this.customSvgFile) {
            const option = document.createElement('option');
            option.value = this.customSvgFile.name;
            option.textContent = `📁 ${this.customSvgFile.name}`;
            option.dataset.isCustomFile = 'true';
            dropdown.appendChild(option);
        }
        
        // Set selected file from localStorage
        if (this.customSvgFile && this.selectedSVGFile === this.customSvgFile.name) {
            dropdown.value = this.selectedSVGFile;
        } else if (availableFiles.includes(this.selectedSVGFile)) {
            dropdown.value = this.selectedSVGFile;
        } else if (availableFiles.length > 0) {
            // If saved file not available, use first available file
            this.selectedSVGFile = availableFiles[0];
            dropdown.value = this.selectedSVGFile;
        }
        
        // Add change event listener for auto-loading
        dropdown.addEventListener('change', (e) => {
            this.selectedSVGFile = e.target.value;
            this.saveSettings();
            
            // Check if this is a custom file
            const selectedOption = e.target.selectedOptions[0];
            if (selectedOption && selectedOption.dataset.isCustomFile === 'true') {
                // Load from stored custom file
                if (this.customSvgFile && this.customSvgFile.name === this.selectedSVGFile) {
                    this.loadSVGFromContent(this.customSvgFile.content, this.customSvgFile.name);
                } else {
                    console.warn('Custom file not found in memory, please browse again');
                }
            } else {
                // Load from regular SVG files directory
                this.loadSVG(this.selectedSVGFile);
            }
        });
        
        console.log(`SVG dropdown populated with ${availableFiles.length} files. Selected: ${this.selectedSVGFile}`);
    }

    // Handle SVG file selection from file browser
    async handleSvgFileSelection(event) {
        const file = event.target.files[0];
        if (!file) return;
        
        console.log('Selected file:', file.name, 'from path:', file.webkitRelativePath || 'local file');
        
        try {
            // Read the file content
            const fileContent = await this.readFileAsText(file);
            
            // Create a blob URL for the file
            const blob = new Blob([fileContent], { type: 'image/svg+xml' });
            const blobUrl = URL.createObjectURL(blob);
            
            // Store the file info for later use
            this.customSvgFile = {
                name: file.name,
                content: fileContent,
                blobUrl: blobUrl,
                lastModified: file.lastModified
            };
            
            // Add this file to the dropdown if it's not already there
            const dropdown = document.getElementById('svgFile');
            if (dropdown) {
                // Check if this file is already in the dropdown
                let existingOption = Array.from(dropdown.options).find(option => option.value === file.name);
                
                if (!existingOption) {
                    // Add new option for this file
                    const option = document.createElement('option');
                    option.value = file.name;
                    option.textContent = `📁 ${file.name}`;
                    option.dataset.isCustomFile = 'true';
                    dropdown.appendChild(option);
                }
                
                // Select this file
                dropdown.value = file.name;
                this.selectedSVGFile = file.name;
                this.saveSettings();
            }
            
            // Load the SVG content directly
            await this.loadSVGFromContent(fileContent, file.name);
            
        } catch (error) {
            console.error('Error loading selected SVG file:', error);
            alert(`Error loading SVG file: ${error.message}`);
        }
    }

    // Helper method to read file as text
    readFileAsText(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = (e) => resolve(e.target.result);
            reader.onerror = (e) => reject(new Error('Failed to read file'));
            reader.readAsText(file);
        });
    }

    // Load SVG from content string
    async loadSVGFromContent(svgContent, filename) {
        try {
            this.currentSVGFilename = filename;
            console.log(`Loading SVG from content: ${filename}`);
            
            // Show loading indicator
            const placeholder = document.getElementById('svg-placeholder');
            placeholder.innerHTML = '<p>Loading SVG...</p>';
            
            // Reset animation state
            this.resetAnimation();
            
            // Find the SVG container and replace content
            const svgContainer = document.querySelector('.svg-container');
            if (!svgContainer) {
                throw new Error('SVG container not found');
            }
            
            // Create a temporary container to parse the SVG
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = svgContent;
            
            const svgElement = tempDiv.querySelector('svg');
            if (!svgElement) {
                throw new Error('No SVG element found in the selected file');
            }
            
            // Clear the container and add the new SVG
            svgContainer.innerHTML = '';
            svgElement.id = 'animated-svg';
            svgContainer.appendChild(svgElement);
            
            // Initialize paths and animation
            this.initializePaths();
            
        } catch (error) {
            console.error('Error loading SVG from content:', error);
            const placeholder = document.getElementById('svg-placeholder');
            if (placeholder) {
                placeholder.innerHTML = 
                    `<p style="color: red;">Error loading SVG: ${error.message}<br>
                     Check browser console for more details.</p>`;
            }
            throw error;
        }
    }
    
    async loadSVG(filename = null) {
        try {
            // Use provided filename, or fall back to selectedSVGFile, or default
            filename = filename || this.selectedSVGFile || 'aspire-wire-up.svg';
            this.currentSVGFilename = filename; // Store the filename
            console.log(`Loading SVG: ${filename}`);
            
            // Show loading indicator
            const placeholder = document.getElementById('svg-placeholder');
            placeholder.innerHTML = '<p>Loading SVG...</p>';
            
            // Check if we're running from file:// protocol
            if (window.location.protocol === 'file:') {
                throw new Error('Cannot load SVG files when running from file:// protocol. Please use a local web server (e.g., Live Server extension in VS Code).');
            }
            
            const response = await fetch(`../svg-files/${filename}`);
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const svgContent = await response.text();
            
            // Reset animation state
            this.resetAnimation();
            
            placeholder.innerHTML = svgContent;
            
            // Get the SVG element and add an ID for styling
            const svgElement = placeholder.querySelector('svg');
            if (svgElement) {
                svgElement.id = 'animated-svg';
                this.initializePaths();
            } else {
                throw new Error('SVG element not found');
            }
            
        } catch (error) {
            console.error('Error loading SVG:', error);
            console.error('Attempted to load:', `../svg-files/${filename}`);
            const placeholder = document.getElementById('svg-placeholder');
            placeholder.innerHTML = 
                `<p style="color: red;">Error loading SVG: ${error.message}<br>
                 Attempted to load: ../svg-files/${filename}<br>
                 Check browser console for more details.</p>`;
        }
    }
    
    initializePaths() {
        const svgElement = document.getElementById('animated-svg');
        this.paths = Array.from(svgElement.querySelectorAll('path'));
        this.circles = Array.from(svgElement.querySelectorAll('circle'));
        
        console.log(`Found ${this.paths.length} paths and ${this.circles.length} circles to animate`);
        
        // Create a combined list of all drawable elements in document order
        this.allElements = [];
        const allSvgChildren = Array.from(svgElement.children);
        
        allSvgChildren.forEach(child => {
            if (child.tagName.toLowerCase() === 'path' || child.tagName.toLowerCase() === 'circle') {
                this.allElements.push(child);
            } else if (child.tagName.toLowerCase() === 'g') {
                // Handle groups that might contain paths or circles
                const groupElements = Array.from(child.querySelectorAll('path, circle'));
                this.allElements.push(...groupElements);
            }
        });
        
        console.log(`Total elements in drawing order: ${this.allElements.length}`);
        
        // Initialize all paths and categorize them using rules system
        this.handwritingPaths = [];
        this.structuralPaths = [];
        
        this.allElements.forEach((element, index) => {
            if (element.tagName.toLowerCase() === 'path') {
                const pathLength = element.getTotalLength();
                const strokeColor = element.getAttribute('stroke') || '#000000';
                
                element.style.strokeDasharray = pathLength;
                element.style.strokeDashoffset = pathLength;
                element.style.transition = 'none';
                element.dataset.pathLength = pathLength;
                element.dataset.elementIndex = index;
                element.dataset.strokeColor = strokeColor;
                element.dataset.elementType = 'path';
                
                // Add click handler for path editor selection
                element.addEventListener('click', (e) => {
                    e.stopPropagation(); // Prevent event bubbling
                    this.selectElementInPathEditor(index);
                });
                
                // Use rules system to determine if this is handwriting
                const isHandwriting = this.isHandwritingElement(element);
                element.dataset.isHandwriting = isHandwriting;
                
                if (isHandwriting) {
                    this.handwritingPaths.push(element);
                } else {
                    this.structuralPaths.push(element);
                }
            } else if (element.tagName.toLowerCase() === 'circle') {
                // Initialize circles to be invisible
                const fillColor = element.getAttribute('fill') || '#000000';
                const strokeColor = element.getAttribute('stroke') || fillColor;
                
                element.style.opacity = '0';
                element.style.transition = 'none';
                element.dataset.elementIndex = index;
                element.dataset.fillColor = fillColor;
                element.dataset.strokeColor = strokeColor;
                element.dataset.elementType = 'circle';
                
                // Add click handler for path editor selection
                element.addEventListener('click', (e) => {
                    e.stopPropagation(); // Prevent event bubbling
                    this.selectElementInPathEditor(index);
                });
                
                // Use rules system to determine if this is handwriting
                const isHandwriting = this.isHandwritingElement(element);
                element.dataset.isHandwriting = isHandwriting;
                
                if (isHandwriting) {
                    this.handwritingPaths.push(element);
                } else {
                    this.structuralPaths.push(element);
                }
            }
        });
        
        // Use the original file order for drawing sequence
        this.sortedPaths = [...this.allElements]; // Keep original order for compatibility
        
        console.log(`Categorized elements: ${this.handwritingPaths.length} handwriting, ${this.structuralPaths.length} structural`);
        
        // Apply any existing style rules
        this.applyStyleRules();
        
        this.updateProgressText();
        this.updatePathStats();
        
        // Refresh PathEditor if it's visible
        if (this.pathEditorVisible) {
            this.refreshPathList();
        }
    }
    
    isHandwritingElement(element) {
        // Get current handwriting rules from the global rules system if available
        const globalRules = typeof styleRules !== 'undefined' ? styleRules : [];
        
        // Check if any rule specifically targets this element for handwriting classification
        const handwritingRules = globalRules.filter(rule => {
            // Look for rules that might be handwriting-related by checking if they modify
            // typical handwriting properties with handwriting-like values
            const hasTypicalHandwritingActions = rule.actions && rule.actions.some(action =>
                (action.property === 'stroke-width' && parseFloat(action.value) <= 3.0) ||
                (action.property === 'opacity' && parseFloat(action.value) >= 0.8)
            );
            
            return hasTypicalHandwritingActions;
        });
        
        // Check if this element matches any handwriting rule conditions
        for (const rule of handwritingRules) {
            if (rule.conditions) {
                const matchesRule = rule.conditions.every(condition => {
                    let elementValue = element.getAttribute(condition.property) || element.style[condition.property] || '';
                    
                    // Handle special cases
                    if (!elementValue) {
                        const style = window.getComputedStyle(element);
                        if (condition.property === 'stroke-width') {
                            elementValue = style.strokeWidth || '';
                        } else if (condition.property === 'opacity') {
                            elementValue = style.opacity || '';
                        }
                    }
                    
                    // For path length condition, check against the stored path length
                    if (condition.property === 'path-length' && element.dataset.pathLength) {
                        const pathLength = parseFloat(element.dataset.pathLength);
                        const threshold = parseFloat(condition.value);
                        return pathLength <= threshold;
                    }
                    
                    // Special handling for path-length on circles (treat as very short paths)
                    if (condition.property === 'path-length' && element.tagName.toLowerCase() === 'circle') {
                        const threshold = parseFloat(condition.value);
                        // Circles are considered to have a very small "path length" equivalent
                        return 10 <= threshold; // Arbitrary small value for circles
                    }
                    
                    // Normalize values for comparison
                    const normalizedElementValue = elementValue.toString().trim();
                    const normalizedConditionValue = condition.value.toString().trim();
                    
                    return normalizedElementValue === normalizedConditionValue;
                });
                
                if (matchesRule) {
                    return true;
                }
            }
        }
        
        // Fallback to original hardcoded logic if no rules match
        if (element.tagName.toLowerCase() === 'path') {
            const pathLength = parseFloat(element.dataset.pathLength) || element.getTotalLength();
            const strokeColor = element.getAttribute('stroke') || '#000000';
            const normalizedColor = strokeColor.toLowerCase();
            const normalizedHandwritingColor = this.handwritingColor.toLowerCase();
            
            return normalizedColor === normalizedHandwritingColor && pathLength <= this.lengthThreshold;
        } else if (element.tagName.toLowerCase() === 'circle') {
            const fillColor = element.getAttribute('fill') || '#000000';
            return fillColor.toLowerCase() === this.handwritingColor.toLowerCase();
        }
        
        return false;
    }
    
    applyStyleRules(rules = null) {
        // Use provided rules or get from global window.styleRules variable
        const stylesToApply = rules || (typeof window.styleRules !== 'undefined' ? window.styleRules : []);
        
        if (stylesToApply.length === 0) {
            return;
        }
        
        console.log('Applying style rules:', stylesToApply);
        
        // Apply rules to all elements
        this.allElements.forEach(element => {
            if (typeof applyStyleRulesToElement === 'function') {
                applyStyleRulesToElement(element);
            } else {
                // Fallback implementation if function not available
                this.applyRulesToElement(element, stylesToApply);
            }
        });
    }
    
    applyRulesToElement(element, rules) {
        rules.forEach(rule => {
            // Handle different rule formats for backward compatibility
            let conditionsToCheck = [];
            let actionsToApply = [];
            
            if (rule.property && rule.value && rule.targetProperty && rule.targetValue) {
                // Old single condition/action format
                conditionsToCheck = [{ property: rule.property, value: rule.value }];
                actionsToApply = [{ property: rule.targetProperty, value: rule.targetValue }];
            } else if (rule.conditions && rule.targetProperty && rule.targetValue) {
                // Multi-condition, single action format
                conditionsToCheck = rule.conditions;
                actionsToApply = [{ property: rule.targetProperty, value: rule.targetValue }];
            } else if (rule.conditions && rule.actions) {
                // New multi-condition, multi-action format
                conditionsToCheck = rule.conditions;
                actionsToApply = rule.actions;
            } else {
                return; // Skip invalid rules
            }
            
            // Check if all conditions are met
            const allConditionsMet = conditionsToCheck.every(condition => {
                return this.checkCondition(element, condition);
            });
            
            // Apply all actions if all conditions are met
            if (allConditionsMet) {
                const conditionDesc = conditionsToCheck.map(c => `${c.property}=${c.value}`).join(' AND ');
                const actionDesc = actionsToApply.map(a => `${a.property}=${a.value}`).join(' AND ');
                console.log(`Applying multi-action rule: ${conditionDesc} -> ${actionDesc}`);
                
                actionsToApply.forEach(action => {
                    if (action.property === 'stroke-width') {
                        element.style.strokeWidth = action.value;
                    } else if (action.property === 'opacity') {
                        element.style.opacity = action.value;
                    } else if (action.property === 'stroke') {
                        element.style.stroke = action.value;
                    } else if (action.property === 'fill') {
                        element.style.fill = action.value;
                    } else if (action.property === 'stroke-opacity') {
                        element.style.strokeOpacity = action.value;
                    } else if (action.property === 'fill-opacity') {
                        element.style.fillOpacity = action.value;
                    } else if (action.property === 'animation-speed') {
                        // Store animation speed as a data attribute for use during animation
                        element.dataset.animationSpeed = action.value;
                        console.log(`Set animation speed multiplier ${action.value} on element:`, element);
                    } else if (action.property === 'glow-effect') {
                        // Apply glow effect with specified color and optional size
                        const actionValue = action.value || '#ffd700'; // Default to gold
                        
                        // Parse color and size from value (format: "color" or "color,size")
                        let glowColor = '#ffd700';
                        let glowSize = 8; // Default size
                        
                        if (actionValue.includes(',')) {
                            const parts = actionValue.split(',');
                            glowColor = parts[0].trim();
                            glowSize = parseInt(parts[1].trim()) || 8;
                        } else {
                            glowColor = actionValue;
                        }
                        
                        // Use multiple drop-shadows with configurable size
                        const size1 = Math.round(glowSize * 0.25);
                        const size2 = Math.round(glowSize * 0.5);
                        const size3 = glowSize;
                        const size4 = Math.round(glowSize * 1.5);
                        
                        const filterValue = `drop-shadow(0 0 ${size1}px ${glowColor}) drop-shadow(0 0 ${size2}px ${glowColor}) drop-shadow(0 0 ${size3}px ${glowColor}) drop-shadow(0 0 ${size4}px ${glowColor})`;
                        element.style.filter = filterValue;
                        element.style.webkitFilter = filterValue; // For Safari
                        
                        console.log(`Applied glow effect with color ${glowColor} and size ${glowSize}px to element:`, element);
                    }
                });
            }
        });
    }
    
    checkCondition(element, condition) {
        // Handle special condition types first
        if (condition.property === 'path-length' && element.dataset.pathLength) {
            const pathLength = parseFloat(element.dataset.pathLength);
            const threshold = parseFloat(condition.value);
            return pathLength <= threshold;
        }
        
        // Special handling for path-length on circles (treat as very short paths)
        if (condition.property === 'path-length' && element.tagName.toLowerCase() === 'circle') {
            const threshold = parseFloat(condition.value);
            return 10 <= threshold; // Arbitrary small value for circles
        }
        
        // Handle animation-speed conditions
        if (condition.property === 'animation-speed') {
            const elementSpeed = element.dataset.animationSpeed || '0';
            const conditionSpeed = condition.value.toString().trim();
            return elementSpeed === conditionSpeed;
        }
        
        // Handle regular properties
        let elementValue = element.getAttribute(condition.property) || element.style[condition.property] || '';
        
        // Handle special cases for computed styles
        if (!elementValue) {
            const style = window.getComputedStyle(element);
            if (condition.property === 'stroke-width') {
                elementValue = style.strokeWidth || '';
            } else if (condition.property === 'opacity') {
                elementValue = style.opacity || '';
            }
        }
        
        // Normalize values for comparison
        const normalizedElementValue = elementValue.toString().trim();
        const normalizedConditionValue = condition.value.toString().trim();
        
        return normalizedElementValue === normalizedConditionValue;
    }

    initializeControls() {
        // Speed control synchronization
        const speedRange = document.getElementById('speed');
        const speedValue = document.getElementById('speedValue');
        
        // Check if elements exist before trying to use them
        if (speedRange && speedValue) {
            // Set initial values from loaded settings
            speedRange.value = this.animationSpeed;
            speedValue.value = this.animationSpeed;
            console.log('Initialized speed controls with value:', this.animationSpeed);
            
            speedRange.addEventListener('input', (e) => {
                const value = e.target.value;
                speedValue.value = value;
                this.animationSpeed = parseInt(value);
                this.saveSettings(); // Save when changed
            });
            
            speedValue.addEventListener('input', (e) => {
                const value = e.target.value;
                speedRange.value = value;
                this.animationSpeed = parseInt(value);
                this.saveSettings(); // Save when changed
            });
        } else {
            console.warn('Speed controls not found in DOM');
        }
        
        // Delay control synchronization
        const delayRange = document.getElementById('delay');
        const delayValue = document.getElementById('delayValue');
        
        // Check if elements exist before trying to use them
        if (delayRange && delayValue) {
            // Set initial values from loaded settings
            delayRange.value = this.animationDelay;
            delayValue.value = this.animationDelay;
            console.log('Initialized delay controls with value:', this.animationDelay);
            
            delayRange.addEventListener('input', (e) => {
                const value = e.target.value;
                delayValue.value = value;
                this.animationDelay = parseInt(value);
                this.saveSettings(); // Save when changed
            });
            
            delayValue.addEventListener('input', (e) => {
                const value = e.target.value;
                delayRange.value = value;
                this.animationDelay = parseInt(value);
                this.saveSettings(); // Save when changed
            });
        } else {
            console.warn('Delay controls not found in DOM');
        }
        
        // Button controls
        const playPauseBtn = document.getElementById('playPauseBtn');
        const resetBtn = document.getElementById('resetBtn');
        const completeBtn = document.getElementById('completeBtn');
        const recordBtn = document.getElementById('recordBtn');
        
        if (playPauseBtn) {
            playPauseBtn.addEventListener('click', () => this.toggleAnimation());
        }
        if (resetBtn) {
            resetBtn.addEventListener('click', () => this.resetAnimation());
        }
        if (completeBtn) {
            completeBtn.addEventListener('click', () => this.showComplete());
        }
        if (recordBtn) {
            recordBtn.addEventListener('click', () => this.recordAnimation());
        }
        
        // File browser functionality
        const browseSvgBtn = document.getElementById('browseSvgBtn');
        const svgFileInput = document.getElementById('svgFileInput');
        
        if (browseSvgBtn && svgFileInput) {
            browseSvgBtn.addEventListener('click', () => {
                svgFileInput.click();
            });
            
            svgFileInput.addEventListener('change', (e) => {
                this.handleSvgFileSelection(e);
            });
        }
        
        // Initialize PathEditor
        this.initializePathEditor();
    }
    
    recategorizePaths() {
        if (this.allElements.length === 0) return;
        
        this.handwritingPaths = [];
        this.structuralPaths = [];
        
        this.allElements.forEach(element => {
            // Use the rule-based handwriting detection
            const isHandwriting = this.isHandwritingElement(element);
            element.dataset.isHandwriting = isHandwriting;
            
            if (isHandwriting) {
                this.handwritingPaths.push(element);
            } else {
                this.structuralPaths.push(element);
            }
        });
        
        // Keep the original file order for drawing sequence
        this.sortedPaths = [...this.allElements]; // Maintain original order
        
        console.log(`Recategorized elements: ${this.handwritingPaths.length} handwriting, ${this.structuralPaths.length} structural`);
        this.updatePathStats();
    }
    
    updatePathStats() {
        const pathStatsText = document.getElementById('pathStatsText');
        if (pathStatsText) {
            const totalPaths = this.paths.length;
            const totalCircles = this.circles.length;
            pathStatsText.textContent = 
                `${this.handwritingPaths.length} handwriting, ${this.structuralPaths.length} structural (${totalPaths} paths, ${totalCircles} circles)`;
        }
    }
    
    startAnimation() {
        if (this.sortedPaths.length === 0) {
            console.warn('No paths loaded yet');
            return;
        }
        
        // Clear any selected paths in PathEditor
        if (this.selectedPaths && this.selectedPaths.size > 0) {
            this.clearPathSelection();
        }
        
        if (this.isPaused) {
            this.isPaused = false;
            this.animateNextPath();
        } else if (!this.isAnimating) {
            // Check if this is a restart (animation completed)
            if (this.currentPathIndex >= this.sortedPaths.length) {
                this.resetAnimation(); // Clear the SVG first
            }
            
            this.isAnimating = true;
            this.currentPathIndex = 0;
            this.currentSpeedMultiplier = 1.0; // Reset speed multiplier at start
            this.animateNextPath();
        }
        
        this.updateButtonStates();
    }
    
    toggleAnimation() {
        if (this.isAnimating && !this.isPaused) {
            this.pauseAnimation();
        } else {
            this.startAnimation();
        }
        this.updateButtonStates();
    }
    
    pauseAnimation() {
        this.isPaused = true;
        if (this.animationTimeout) {
            clearTimeout(this.animationTimeout);
            this.animationTimeout = null;
        }
        this.updateButtonStates();
    }
    
    resetAnimation() {
        this.isAnimating = false;
        this.isPaused = false;
        this.currentPathIndex = 0;
        this.currentSpeedMultiplier = 1.0; // Reset speed multiplier
        
        if (this.animationTimeout) {
            clearTimeout(this.animationTimeout);
            this.animationTimeout = null;
        }
        
        // Reset all elements to invisible
        this.sortedPaths.forEach(element => {
            element.style.transition = 'none';
            if (element.dataset.elementType === 'path') {
                const pathLength = element.dataset.pathLength;
                element.style.strokeDashoffset = pathLength;
            } else if (element.dataset.elementType === 'circle') {
                element.style.opacity = '0';
            }
            // Clear any applied filters (like glow effects)
            element.style.filter = '';
            element.style.webkitFilter = '';
        });
        
        this.updateProgressText();
        this.updateButtonStates();
    }
    
    showComplete() {
        this.isAnimating = false;
        this.isPaused = false;
        this.currentPathIndex = this.sortedPaths.length;
        
        if (this.animationTimeout) {
            clearTimeout(this.animationTimeout);
            this.animationTimeout = null;
        }
        
        // Show all elements immediately
        this.sortedPaths.forEach(element => {
            element.style.transition = 'none';
            if (element.dataset.elementType === 'path') {
                element.style.strokeDashoffset = '0';
            } else if (element.dataset.elementType === 'circle') {
                element.style.opacity = '1';
            }
            // Apply rules to each element when showing complete
            if (typeof applyStyleRulesToElement === 'function') {
                applyStyleRulesToElement(element);
            }
        });
        
        this.updateProgressText();
        this.updateButtonStates();
    }
    
    animateNextPath() {
        if (!this.isAnimating || this.isPaused || this.currentPathIndex >= this.sortedPaths.length) {
            if (this.currentPathIndex >= this.sortedPaths.length) {
                this.isAnimating = false;
                this.updateButtonStates();
                this.updateProgressText();
            }
            return;
        }
        
        const currentElement = this.sortedPaths[this.currentPathIndex];
        const elementType = currentElement.dataset.elementType;
        const isHandwriting = currentElement.dataset.isHandwriting === 'true';
        
        // Debug: Log all data attributes on this element
        console.log(`Element ${this.currentPathIndex} data attributes:`, {
            elementType: currentElement.dataset.elementType,
            isHandwriting: currentElement.dataset.isHandwriting,
            animationSpeed: currentElement.dataset.animationSpeed,
            pathLength: currentElement.dataset.pathLength
        });
        
        // Calculate animation speed based on path length
        let baseSpeed = this.animationSpeed;
        let animationSpeed = baseSpeed;
        
        // Check for speed hint comment before this element and update current multiplier
        const elementIndexInAllElements = this.allElements.indexOf(currentElement);
        if (elementIndexInAllElements > 0) {
            // Check for speed hint after the previous element
            const speedHintMultiplier = this.checkForSpeedHintComment(elementIndexInAllElements - 1);
            if (speedHintMultiplier > 0) {
                this.currentSpeedMultiplier = speedHintMultiplier;
                console.log(`Updated current speed multiplier to ${this.currentSpeedMultiplier}x from speed hint`);
            }
        }
        
        // For paths, calculate speed based on length with square root scaling for more moderate effect
        if (elementType === 'path' && currentElement.dataset.pathLength) {
            const pathLength = parseFloat(currentElement.dataset.pathLength);
            // Base speed represents time per 100 units of path length
            // Use square root to reduce the impact of very long paths
            const baseUnits = 100; // Reference length
            const lengthMultiplier = Math.sqrt(pathLength / baseUnits);
            animationSpeed = Math.round(baseSpeed * lengthMultiplier);
            
            // Ensure reasonable bounds (50ms to 3000ms) - reduced upper bound
            animationSpeed = Math.max(50, Math.min(3000, animationSpeed));
            
            console.log(`Path-based speed: length=${pathLength.toFixed(1)}, base=${baseSpeed}ms, multiplier=${lengthMultiplier.toFixed(2)}x, final=${animationSpeed}ms`);
        }
        
        // Apply rule-based speed multiplier if present (this applies on top of length-based timing)
        if (currentElement.dataset.animationSpeed) {
            const multiplier = parseFloat(currentElement.dataset.animationSpeed);
            if (!isNaN(multiplier) && multiplier > 0) {
                // Divide by multiplier: 2x = half the time (faster), 0.5x = double the time (slower)
                animationSpeed = Math.round(animationSpeed / multiplier);
                console.log(`Rule speed applied: multiplier=${multiplier}x, final=${animationSpeed}ms (${multiplier > 1 ? 'faster' : 'slower'})`);
            }
        }
        
        // Apply current speed hint multiplier (this applies on top of all other timing calculations)
        if (this.currentSpeedMultiplier !== 1.0) {
            // Divide by multiplier: 2x = half the time (faster), 0.5x = double the time (slower)
            animationSpeed = Math.round(animationSpeed / this.currentSpeedMultiplier);
            console.log(`Current speed hint applied: multiplier=${this.currentSpeedMultiplier}x, final=${animationSpeed}ms (${this.currentSpeedMultiplier > 1 ? 'faster' : 'slower'})`);
        }
        
        const delay = isHandwriting ? 0 : this.animationDelay; // No delay for handwriting
        
        if (elementType === 'path') {
            // Animate path by stroking
            const pathLength = parseFloat(currentElement.dataset.pathLength);
            const transitionValue = `stroke-dashoffset ${animationSpeed}ms ease-in-out`;
            currentElement.style.setProperty('transition', transitionValue, 'important');
            currentElement.style.strokeDashoffset = '0';
            console.log(`Set path transition: ${transitionValue} (with !important)`);
        } else if (elementType === 'circle') {
            // Animate circle by fading in
            const transitionValue = `opacity ${animationSpeed}ms ease-in-out`;
            currentElement.style.setProperty('transition', transitionValue, 'important');
            currentElement.style.opacity = '1';
            console.log(`Set circle transition: ${transitionValue} (with !important)`);
        }
        
        // Apply rules to the current element during animation
        console.log(`Applying rules to element during animation: ${currentElement.tagName}`);
        if (typeof applyStyleRulesToElement === 'function') {
            applyStyleRulesToElement(currentElement);
        } else {
            console.log('applyStyleRulesToElement function not found');
        }
        
        this.currentPathIndex++;
        this.updateProgressText();
        
        // Check for pause comments before continuing
        const pauseDuration = this.checkForPauseComment(this.currentPathIndex - 1);
        
        // Schedule next element animation (including any pause duration)
        const totalDelay = animationSpeed + delay + pauseDuration;
        this.animationTimeout = setTimeout(() => {
            this.animateNextPath();
        }, totalDelay);
    }

    // Check for pause comments after the current element
    checkForPauseComment(elementIndex) {
        const svgElement = document.getElementById('animated-svg');
        if (!svgElement || elementIndex < 0 || elementIndex >= this.sortedPaths.length) {
            return 0;
        }
        
        const currentElement = this.sortedPaths[elementIndex];
        let nextNode = currentElement.nextSibling;
        
        console.log(`Checking for pause after element ${elementIndex}:`, currentElement);
        console.log('Element parent:', currentElement.parentNode);
        
        // Look through following nodes for pause comments
        while (nextNode) {
            console.log('Checking node:', nextNode, 'type:', nextNode.nodeType);
            
            if (nextNode.nodeType === Node.COMMENT_NODE) {
                const commentText = nextNode.textContent.trim();
                console.log('Found comment:', commentText);
                
                // Check if this is a pause comment (format: PAUSE:duration)
                const pauseMatch = commentText.match(/^PAUSE:(\d+)$/);
                if (pauseMatch) {
                    const pauseDuration = parseInt(pauseMatch[1]);
                    console.log(`Found pause comment: ${pauseDuration}ms after element ${elementIndex + 1}`);
                    return pauseDuration;
                }
            } else if (nextNode.nodeType === Node.ELEMENT_NODE) {
                // Stop looking when we hit the next path/circle element
                console.log('Hit next element, stopping search');
                break;
            }
            nextNode = nextNode.nextSibling;
        }
        
        return 0; // No pause found
    }

    checkForSpeedHintComment(elementIndex) {
        const svgElement = document.getElementById('animated-svg');
        if (!svgElement || elementIndex < 0 || elementIndex >= this.allElements.length) {
            return 0;
        }
        
        const currentElement = this.allElements[elementIndex];
        let nextNode = currentElement.nextSibling;
        
        console.log(`Checking for speed hint after element ${elementIndex}:`, currentElement);
        
        // Look through following nodes for speed hint comments
        while (nextNode) {
            if (nextNode.nodeType === Node.COMMENT_NODE) {
                const commentText = nextNode.textContent.trim();
                
                // Check if this is a speed hint comment (format: SPEED:multiplier)
                const speedMatch = commentText.match(/^SPEED:([\d.]+)$/);
                if (speedMatch) {
                    const speedMultiplier = parseFloat(speedMatch[1]);
                    console.log(`Found speed hint comment: ${speedMultiplier}x after element ${elementIndex + 1}`);
                    return speedMultiplier;
                }
            } else if (nextNode.nodeType === Node.ELEMENT_NODE) {
                // Stop looking when we hit the next path/circle element
                break;
            }
            nextNode = nextNode.nextSibling;
        }
        
        return 0; // No speed hint found
    }
        
    updateProgressText() {
        const progressText = document.getElementById('progressText');
        if (this.sortedPaths.length === 0) {
            progressText.textContent = 'Loading elements...';
        } else if (this.currentPathIndex === 0 && !this.isAnimating) {
            progressText.textContent = `Ready to animate ${this.sortedPaths.length} elements (paths & circles) in file order`;
        } else if (this.currentPathIndex >= this.sortedPaths.length) {
            progressText.textContent = `Animation complete! All ${this.sortedPaths.length} elements drawn in file order`;
        } else {
            const currentElement = this.sortedPaths[this.currentPathIndex - 1];
            if (currentElement) {
                const elementType = currentElement.dataset.elementType;
                const pathType = currentElement.dataset.isHandwriting === 'true' ? 'handwriting' : 'structural';
                progressText.textContent = `Drawing... ${this.currentPathIndex}/${this.sortedPaths.length} elements (last: ${elementType}, ${pathType})`;
            } else {
                progressText.textContent = `Drawing... ${this.currentPathIndex}/${this.sortedPaths.length} elements`;
            }
        }
    }
    
    updateButtonStates() {
        const playPauseBtn = document.getElementById('playPauseBtn');
        const resetBtn = document.getElementById('resetBtn');
        
        if (this.isAnimating && !this.isPaused) {
            // Currently playing
            playPauseBtn.textContent = '⏸ Pause';
            playPauseBtn.className = 'btn-secondary';
        } else if (this.isPaused) {
            // Currently paused
            playPauseBtn.textContent = '▶ Resume';
            playPauseBtn.className = 'btn-primary';
        } else if (this.currentPathIndex >= this.sortedPaths.length) {
            // Animation complete
            playPauseBtn.textContent = '⟲ Restart';
            playPauseBtn.className = 'btn-primary';
        } else {
            // Ready to start
            playPauseBtn.textContent = '▶ Start Animation';
            playPauseBtn.className = 'btn-primary';
        }
    }
    
    async recordAnimation() {
        const recordBtn = document.getElementById('recordBtn');
        const originalText = recordBtn.textContent;
        
        try {
            // Clear any selected paths in PathEditor
            if (this.selectedPaths && this.selectedPaths.size > 0) {
                this.clearPathSelection();
            }
            
            // Check if recording is supported
            if (!HTMLCanvasElement.prototype.captureStream) {
                alert('Canvas recording is not supported in this browser. Please use Chrome or Firefox.');
                return;
            }
            
            // Get recording settings from UI controls
            const frameRate = parseInt(document.getElementById('recordFrameRate').value);
            const resolutionPreset = document.getElementById('recordResolution').value;
            const bitrate = parseInt(document.getElementById('recordBitrate').value) * 1000000; // Convert to bps
            const endPause = parseInt(document.getElementById('recordEndPauseValue').value);
            const showCompleteStart = document.getElementById('recordShowCompleteStart').checked;
            
            recordBtn.textContent = '🎥 Rendering...';
            recordBtn.disabled = true;
            
            // Get the SVG container element
            const svgContainer = document.getElementById('svg-placeholder');
            const svgElement = svgContainer.querySelector('svg');
            if (!svgElement) {
                throw new Error('SVG element not found');
            }
            
            // Reset animation to beginning
            this.resetAnimation();
            
            // Get target resolution based on preset
            let svgWidth, svgHeight;
            switch(resolutionPreset) {
                case '720p':
                    svgWidth = 1280;
                    svgHeight = 720;
                    break;
                case '1080p':
                    svgWidth = 1920;
                    svgHeight = 1080;
                    break;
                case '1440p':
                    svgWidth = 2560;
                    svgHeight = 1440;
                    break;
                case '4k':
                    svgWidth = 3840;
                    svgHeight = 2160;
                    break;
                default:
                    // Fallback to current SVG dimensions
                    const svgRect = svgElement.getBoundingClientRect();
                    svgWidth = svgRect.width || 1920;
                    svgHeight = svgRect.height || 1080;
            }
            
            // Create high-resolution canvas for recording
            const canvas = document.createElement('canvas');
            canvas.width = svgWidth;
            canvas.height = svgHeight;
            const ctx = canvas.getContext('2d');
            
            // Enable high-quality rendering
            ctx.imageSmoothingEnabled = true;
            ctx.imageSmoothingQuality = 'high';
            
            // Set white background initially
            ctx.fillStyle = 'white';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            
            // Calculate total animation duration and create frame plan using UI framerate
            
            // Get animation timing for each element (matches real animation logic)
            const animationPlan = [];
            let currentTime = 0;
            
            for (let i = 0; i < this.sortedPaths.length; i++) {
                const element = this.sortedPaths[i];
                const isHandwriting = element.dataset.isHandwriting === 'true';
                
                // Calculate speed for this element (same logic as animateNextPath)
                let animationSpeed = this.animationSpeed;
                const elementType = element.dataset.elementType;
                
                // For paths, calculate speed based on length with square root scaling for more moderate effect
                if (elementType === 'path' && element.dataset.pathLength) {
                    const pathLength = parseFloat(element.dataset.pathLength);
                    const baseUnits = 100; // Reference length
                    // Use square root to reduce the impact of very long paths
                    const lengthMultiplier = Math.sqrt(pathLength / baseUnits);
                    animationSpeed = Math.round(this.animationSpeed * lengthMultiplier);
                    
                    // Ensure reasonable bounds (50ms to 3000ms) - reduced upper bound
                    animationSpeed = Math.max(50, Math.min(3000, animationSpeed));
                }
                
                // Apply rule-based speed multiplier if present
                if (element.dataset.animationSpeed) {
                    const multiplier = parseFloat(element.dataset.animationSpeed);
                    if (!isNaN(multiplier) && multiplier > 0) {
                        animationSpeed = Math.round(animationSpeed / multiplier);
                    }
                }
                
                const delay = isHandwriting ? 0 : this.animationDelay;
                
                animationPlan.push({
                    elementIndex: i,
                    startTime: currentTime,
                    duration: animationSpeed,
                    endTime: currentTime + animationSpeed
                });
                
                // Total time includes both animation duration AND delay (matches real timing)
                currentTime += animationSpeed + delay;
            }
            
            const baseAnimationDuration = currentTime;
            const totalDuration = baseAnimationDuration + endPause; // Use UI setting for end pause
            const frameDuration = 1000 / frameRate; // Frame duration in ms
            const animationFrames = Math.ceil(totalDuration / frameDuration);
            
            // Total frames includes preview time if enabled
            const totalFrames = animationFrames + (showCompleteStart ? Math.ceil(200 / frameDuration) : 0);
            
            console.log(`Animation plan: ${animationPlan.length} elements, total duration: ${totalDuration}ms, ${totalFrames} frames at ${frameRate} FPS` + 
                       (showCompleteStart ? ` (includes 200ms complete preview)` : ''));
            
            recordBtn.textContent = `🎥 Rendering frame 0/${totalFrames}`;
            
            // Prepare for recording with maximum quality settings
            // IMPORTANT: Draw initial content to canvas BEFORE creating the stream
            ctx.fillStyle = 'white';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            
            // If preview enabled, immediately draw complete state
            if (showCompleteStart) {
                this.showComplete();
                await new Promise(resolve => setTimeout(resolve, 50));
                
                const svgData = new XMLSerializer().serializeToString(svgElement);
                const svgDataUrl = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svgData);
                
                await new Promise((resolve) => {
                    const img = new Image();
                    img.onload = () => {
                        ctx.fillStyle = 'white';
                        ctx.fillRect(0, 0, canvas.width, canvas.height);
                        ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                        resolve();
                    };
                    img.onerror = resolve;
                    img.src = svgDataUrl;
                });
            }
            
            // NOW create stream - canvas already has content
            const stream = canvas.captureStream(frameRate);
            
            // Try different codecs for best quality, fallback to VP9
            // Note: WebM format is used as most browsers don't support MP4 recording
            let mimeType = 'video/webm;codecs=vp9';
            let finalBitrate = bitrate; // Use UI setting
            
            if (MediaRecorder.isTypeSupported('video/webm;codecs=av01')) {
                mimeType = 'video/webm;codecs=av01'; // AV1 for best quality
            } else if (MediaRecorder.isTypeSupported('video/webm;codecs=vp9.0')) {
                mimeType = 'video/webm;codecs=vp9.0'; // VP9 profile 0
            }
            
            const mediaRecorder = new MediaRecorder(stream, {
                mimeType: mimeType,
                videoBitsPerSecond: finalBitrate
            });
            
            console.log(`Recording with codec: ${mimeType} at ${finalBitrate} bps, resolution: ${resolutionPreset} (${svgWidth}x${svgHeight}), ${frameRate} FPS`);
            
            const chunks = [];
            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    chunks.push(event.data);
                }
            };
            
            mediaRecorder.onstop = async () => {
                recordBtn.textContent = '🎥 Processing video...';
                
                try {
                    const blob = new Blob(chunks, { type: 'video/webm' });
                    
                    // Post-process to remove blank frames if enabled
                    let finalBlob = blob;
                    if (showCompleteStart) {
                        finalBlob = await this.removeBlankFrames(blob, recordBtn);
                    }
                    
                    const url = URL.createObjectURL(finalBlob);
                    
                    // Create download link
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = this.generateVideoFilename();
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    
                    // Clean up
                    URL.revokeObjectURL(url);
                    
                } catch (error) {
                    console.error('Video processing failed:', error);
                    // Fallback to original video
                    const blob = new Blob(chunks, { type: 'video/webm' });
                    const url = URL.createObjectURL(blob);
                    
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = this.generateVideoFilename();
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    
                    URL.revokeObjectURL(url);
                }
                
                recordBtn.textContent = originalText;
                recordBtn.disabled = false;
            };
            
            // Function to render SVG at specific animation state
            const renderFrame = async (frameTime) => {
                return new Promise((resolve) => {
                    try {
                        // Set animation state for this time
                        this.setAnimationState(frameTime, animationPlan);
                        
                        // Small delay to let DOM update
                        setTimeout(() => {
                            // Capture the SVG
                            const svgData = new XMLSerializer().serializeToString(svgElement);
                            const svgDataUrl = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svgData);
                            
                            const img = new Image();
                            img.onload = () => {
                                // Clear canvas with white background
                                ctx.fillStyle = 'white';
                                ctx.fillRect(0, 0, canvas.width, canvas.height);
                                
                                // Draw the SVG image at higher resolution
                                ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                                resolve();
                            };
                            
                            img.onerror = () => {
                                console.warn('Failed to load SVG frame');
                                resolve();
                            };
                            
                            img.src = svgDataUrl;
                        }, 5); // Reduced delay from 10ms to 5ms
                    } catch (error) {
                        console.warn('Error rendering frame:', error);
                        resolve();
                    }
                });
            };
            
            // Start recording - canvas already has correct initial content
            mediaRecorder.start();
            
            // If preview enabled, hold current state for 200ms
            if (showCompleteStart) {
                await new Promise(resolve => setTimeout(resolve, 200));
                this.resetAnimation();
                await new Promise(resolve => setTimeout(resolve, 50));
            }
            
            // Now render the main animation frames
            for (let frame = 0; frame < animationFrames; frame++) {
                const frameTime = frame * frameDuration;
                recordBtn.textContent = `🎥 Rendering frame ${frame + 1}/${animationFrames}`;
                
                await renderFrame(frameTime);
                
                // Reduced delay to speed up rendering process
                await new Promise(resolve => setTimeout(resolve, 15));
            }
            
            // Stop recording
            recordBtn.textContent = '🎥 Finishing...';
            setTimeout(() => {
                mediaRecorder.stop();
            }, 500);
            
        } catch (error) {
            console.error('Recording failed:', error);
            alert(`Recording failed: ${error.message}\n\nPlease ensure you're using a supported browser (Chrome or Firefox).`);
            recordBtn.textContent = originalText;
            recordBtn.disabled = false;
        }
    }
    
    // Helper method to set animation state at specific time
    setAnimationState(currentTime, animationPlan) {
        // Reset all elements to hidden state
        this.sortedPaths.forEach((element, index) => {
            const elementType = element.dataset.elementType;
            if (elementType === 'path') {
                const pathLength = parseFloat(element.dataset.pathLength);
                element.style.strokeDasharray = pathLength;
                element.style.strokeDashoffset = pathLength;
                element.style.transition = 'none';
            } else if (elementType === 'circle') {
                element.style.opacity = '0';
                element.style.transition = 'none';
            }
        });
        
        // Show elements that should be visible at this time
        animationPlan.forEach(plan => {
            if (currentTime >= plan.startTime) {
                const element = this.sortedPaths[plan.elementIndex];
                const elementType = element.dataset.elementType;
                
                if (currentTime >= plan.endTime) {
                    // Element is fully animated
                    if (elementType === 'path') {
                        element.style.strokeDashoffset = '0';
                    } else if (elementType === 'circle') {
                        element.style.opacity = '1';
                    }
                } else {
                    // Element is in progress
                    const progress = (currentTime - plan.startTime) / plan.duration;
                    if (elementType === 'path') {
                        const pathLength = parseFloat(element.dataset.pathLength);
                        const offset = pathLength * (1 - progress);
                        element.style.strokeDashoffset = offset;
                    } else if (elementType === 'circle') {
                        element.style.opacity = progress;
                    }
                }
            }
        });
    }
    
    generateVideoFilename() {
        // Get the SVG filename without extension
        const svgNameWithoutExt = this.currentSVGFilename.replace(/\.[^/.]+$/, "");
        
        // Create timestamp in format YYYY-MM-DD_HH-MM-SS
        const now = new Date();
        const timestamp = now.toISOString().slice(0, 19).replace(/:/g, '-').replace('T', '_');
        
        // Determine extension based on the MIME type used
        // For now, keep as webm since most browsers don't support MP4 recording
        // Users can convert to MP4 using online tools or video software
        return `${svgNameWithoutExt}_${timestamp}.webm`;
    }
    
    selectElementInPathEditor(elementIndex) {
        // Open path editor if it's not already open
        if (!this.pathEditorVisible) {
            const pathEditorToggle = document.getElementById('pathEditorToggle');
            const pathEditorContent = document.getElementById('pathEditorContent');
            
            this.pathEditorVisible = true;
            pathEditorToggle.textContent = 'Hide';
            pathEditorContent.classList.add('show');
            this.refreshPathList();
        }
        
        // Clear existing selection and select the clicked element
        this.selectedPaths.clear();
        this.selectedPaths.add(elementIndex);
        
        // Update UI
        this.updateSelectionUI();
        this.updatePropertiesPanel();
        this.highlightSelectedPaths();
        
        // Scroll to the selected item in the path list if needed
        const pathItem = document.querySelector(`[data-element-index="${elementIndex}"]`);
        if (pathItem) {
            pathItem.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }
    
    // PathEditor functionality
    initializePathEditor() {
        this.selectedPaths = new Set();
        this.pathEditorVisible = false;
        
        // Toggle PathEditor visibility
        const pathEditorToggle = document.getElementById('pathEditorToggle');
        const pathEditorContent = document.getElementById('pathEditorContent');
        
        pathEditorToggle.addEventListener('click', () => {
            this.pathEditorVisible = !this.pathEditorVisible;
            if (this.pathEditorVisible) {
                pathEditorContent.classList.add('show');
                pathEditorToggle.textContent = 'Hide';
                this.refreshPathList();
            } else {
                pathEditorContent.classList.remove('show');
                pathEditorToggle.textContent = 'Show';
                this.clearPathSelection();
            }
        });
        
        // Selection controls
        document.getElementById('selectAllBtn').addEventListener('click', () => this.selectAllPaths());
        document.getElementById('selectNoneBtn').addEventListener('click', () => this.selectNonePaths());
        document.getElementById('selectInvertBtn').addEventListener('click', () => this.invertPathSelection());
        
        // Action buttons
        document.getElementById('moveUpBtn').addEventListener('click', () => this.moveSelectedPaths('up'));
        document.getElementById('moveDownBtn').addEventListener('click', () => this.moveSelectedPaths('down'));
        document.getElementById('moveToTopBtn').addEventListener('click', () => this.moveSelectedPaths('top'));
        document.getElementById('moveToBottomBtn').addEventListener('click', () => this.moveSelectedPaths('bottom'));
        
        const insertPauseBtn = document.getElementById('insertPauseBtn');
        if (insertPauseBtn) {
            console.log('Insert pause button found, adding event listener');
            insertPauseBtn.addEventListener('click', () => {
                console.log('Insert pause button clicked');
                this.insertPauseAfterSelection();
            });
        } else {
            console.error('Insert pause button not found in DOM');
        }
        
        const insertSpeedHintBtn = document.getElementById('insertSpeedHintBtn');
        if (insertSpeedHintBtn) {
            console.log('Insert speed hint button found, adding event listener');
            insertSpeedHintBtn.addEventListener('click', () => {
                console.log('Insert speed hint button clicked');
                this.insertSpeedHintAfterSelection();
            });
        } else {
            console.error('Insert speed hint button not found in DOM');
        }
        
        document.getElementById('deletePathsBtn').addEventListener('click', () => this.deleteSelectedPaths());
        document.getElementById('exportSvgBtn').addEventListener('click', () => this.exportSVG());
    }
    
    refreshPathList() {
        if (!this.pathEditorVisible) return;
        
        const pathList = document.getElementById('pathList');
        pathList.innerHTML = '';
        
        this.allElements.forEach((element, index) => {
            const pathItem = this.createPathListItem(element, index);
            pathList.appendChild(pathItem);
            
            // Check for pause comment after this element
            const pauseDuration = this.checkForPauseComment(index);
            if (pauseDuration > 0) {
                const pauseItem = this.createPauseListItem(pauseDuration, index);
                pathList.appendChild(pauseItem);
            }
            
            // Check for speed hint comment after this element
            const speedMultiplier = this.checkForSpeedHintComment(index);
            if (speedMultiplier > 0) {
                const speedHintItem = this.createSpeedHintListItem(speedMultiplier, index);
                pathList.appendChild(speedHintItem);
            }
        });
        
        this.updateSelectionUI();
    }

    createPathListItem(element, index) {
        const item = document.createElement('div');
        item.className = 'path-item';
        item.dataset.elementIndex = index;
        
        const elementType = element.tagName.toLowerCase();
        const strokeColor = element.getAttribute('stroke') || element.style.stroke || '#000000';
        const pathLength = elementType === 'path' ? parseFloat(element.dataset.pathLength || 0) : 0;
        const isHandwriting = element.dataset.isHandwriting === 'true';
        
        // Create icon based on element type
        const icon = document.createElement('div');
        icon.className = 'path-item-icon';
        icon.style.backgroundColor = strokeColor;
        icon.style.color = this.getContrastColor(strokeColor);
        icon.textContent = elementType === 'path' ? 'P' : 'C';
        
        // Create info section
        const info = document.createElement('div');
        info.className = 'path-item-info';
        
        const name = document.createElement('div');
        name.className = 'path-item-name';
        name.textContent = `${elementType.toUpperCase()} ${index + 1}`;
        
        const details = document.createElement('div');
        details.className = 'path-item-details';
        details.textContent = elementType === 'path' 
            ? `Length: ${pathLength.toFixed(1)} | ${isHandwriting ? 'Handwriting' : 'Structural'}`
            : `${isHandwriting ? 'Handwriting' : 'Structural'}`;
        
        info.appendChild(name);
        info.appendChild(details);
        
        item.appendChild(icon);
        item.appendChild(info);
        
        // Add click handler for selection
        item.addEventListener('click', (e) => this.handlePathItemClick(e, index));
        
        // Add drag and drop functionality
        item.draggable = true;
        item.addEventListener('dragstart', (e) => this.handleDragStart(e, index));
        item.addEventListener('dragover', (e) => this.handleDragOver(e));
        item.addEventListener('drop', (e) => this.handleDrop(e, index));
        item.addEventListener('dragend', (e) => this.handleDragEnd(e));
        
        return item;
    }

    createPauseListItem(duration, afterIndex) {
        const item = document.createElement('div');
        item.className = 'pause-item';
        item.dataset.pauseDuration = duration;
        item.dataset.afterIndex = afterIndex;
        
        // Create pause icon
        const icon = document.createElement('div');
        icon.className = 'pause-item-icon';
        icon.style.backgroundColor = '#ffc107';
        icon.style.color = '#000';
        icon.textContent = '⏸️';
        
        // Create pause info
        const info = document.createElement('div');
        info.className = 'pause-item-info';
        
        const name = document.createElement('div');
        name.className = 'pause-item-name';
        name.textContent = `Pause`;
        
        const details = document.createElement('div');
        details.className = 'pause-item-details';
        details.innerHTML = `<input type="number" class="pause-duration-input" value="${duration}" min="0" step="100"> ms delay`;
        
        // Add event listener to duration input
        const durationInput = details.querySelector('.pause-duration-input');
        durationInput.addEventListener('change', (e) => {
            const newDuration = parseInt(e.target.value);
            if (!isNaN(newDuration) && newDuration >= 0) {
                this.updatePauseDuration(afterIndex, newDuration);
            } else {
                e.target.value = duration; // Reset to original value
            }
        });
        
        durationInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.target.blur(); // Trigger change event
                e.stopPropagation();
            }
        });
        
        info.appendChild(name);
        info.appendChild(details);
        
        // Create remove button
        const removeBtn = document.createElement('button');
        removeBtn.className = 'pause-remove-btn';
        removeBtn.textContent = '✕';
        removeBtn.title = 'Remove pause';
        removeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            this.removePauseComment(afterIndex);
        });
        
        item.appendChild(icon);
        item.appendChild(info);
        item.appendChild(removeBtn);
        
        return item;
    }

    createSpeedHintListItem(multiplier, afterIndex) {
        const item = document.createElement('div');
        item.className = 'speed-hint-item';
        item.dataset.speedMultiplier = multiplier;
        item.dataset.afterIndex = afterIndex;
        
        // Create speed hint icon
        const icon = document.createElement('div');
        icon.className = 'speed-hint-item-icon';
        icon.style.backgroundColor = '#28a745';
        icon.style.color = '#fff';
        icon.textContent = '⚡';
        
        // Create speed hint info
        const info = document.createElement('div');
        info.className = 'speed-hint-item-info';
        
        const name = document.createElement('div');
        name.className = 'speed-hint-item-name';
        name.textContent = `Speed Override`;
        
        const details = document.createElement('div');
        details.className = 'speed-hint-item-details';
        details.innerHTML = `<input type="number" class="speed-multiplier-input" value="${multiplier}" min="0.1" max="10" step="0.1">x speed`;
        
        // Add event listener to multiplier input
        const multiplierInput = details.querySelector('.speed-multiplier-input');
        multiplierInput.addEventListener('change', (e) => {
            const newMultiplier = parseFloat(e.target.value);
            if (!isNaN(newMultiplier) && newMultiplier > 0) {
                this.updateSpeedHint(afterIndex, newMultiplier);
            } else {
                e.target.value = multiplier; // Reset to original value
            }
        });
        
        multiplierInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.target.blur(); // Trigger change event
                e.stopPropagation();
            }
        });
        
        info.appendChild(name);
        info.appendChild(details);
        
        // Create remove button
        const removeBtn = document.createElement('button');
        removeBtn.className = 'speed-hint-remove-btn';
        removeBtn.textContent = '✕';
        removeBtn.title = 'Remove speed hint';
        removeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            this.removeSpeedHint(afterIndex);
        });
        
        item.appendChild(icon);
        item.appendChild(info);
        item.appendChild(removeBtn);
        
        return item;
    }
    
    getContrastColor(hexColor) {
        // Convert hex to RGB
        const r = parseInt(hexColor.slice(1, 3), 16);
        const g = parseInt(hexColor.slice(3, 5), 16);
        const b = parseInt(hexColor.slice(5, 7), 16);
        
        // Calculate relative luminance
        const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
        
        return luminance > 0.5 ? '#000000' : '#FFFFFF';
    }
    
    handlePathItemClick(e, index) {
        console.log('Path item clicked, index:', index);
        console.log('Current selected paths before click:', this.selectedPaths);
        
        if (e.ctrlKey || e.metaKey) {
            // Multi-select with Ctrl/Cmd
            if (this.selectedPaths.has(index)) {
                this.selectedPaths.delete(index);
            } else {
                this.selectedPaths.add(index);
            }
        } else if (e.shiftKey && this.selectedPaths.size > 0) {
            // Range select with Shift
            const selectedIndices = Array.from(this.selectedPaths);
            const lastSelected = Math.max(...selectedIndices);
            const start = Math.min(lastSelected, index);
            const end = Math.max(lastSelected, index);
            
            for (let i = start; i <= end; i++) {
                this.selectedPaths.add(i);
            }
        } else {
            // Single select - clear selection if clicking on already selected item
            if (this.selectedPaths.size === 1 && this.selectedPaths.has(index)) {
                this.selectedPaths.clear();
            } else {
                this.selectedPaths.clear();
                this.selectedPaths.add(index);
            }
        }
        
        console.log('Selected paths after click:', this.selectedPaths);
        
        this.updateSelectionUI();
        this.updatePropertiesPanel();
        this.highlightSelectedPaths();
    }
    
    updateSelectionUI() {
        // Update path list items
        const pathItems = document.querySelectorAll('.path-item');
        pathItems.forEach((item, index) => {
            if (this.selectedPaths.has(index)) {
                item.classList.add('selected');
            } else {
                item.classList.remove('selected');
            }
        });
        
        // Update selection count
        const selectionCount = document.getElementById('selectionCount');
        const count = this.selectedPaths.size;
        selectionCount.textContent = count === 0 ? 'No selection' : 
            count === 1 ? '1 item selected' : `${count} items selected`;
        
        // Update action buttons
        const hasSelection = count > 0;
        document.getElementById('moveUpBtn').disabled = !hasSelection;
        document.getElementById('moveDownBtn').disabled = !hasSelection;
        document.getElementById('moveToTopBtn').disabled = !hasSelection;
        document.getElementById('moveToBottomBtn').disabled = !hasSelection;
        document.getElementById('insertPauseBtn').disabled = !hasSelection;
        document.getElementById('insertSpeedHintBtn').disabled = !hasSelection;
        document.getElementById('deletePathsBtn').disabled = !hasSelection;
        document.getElementById('exportSvgBtn').disabled = false; // Always allow export
    }
    
    updatePropertiesPanel() {
        const propertiesContainer = document.getElementById('pathProperties');
        
        if (this.selectedPaths.size === 0) {
            propertiesContainer.innerHTML = '<div class="no-selection">Select path(s) to view properties</div>';
            return;
        }
        
        const selectedElements = Array.from(this.selectedPaths).map(index => this.allElements[index]);
        
        if (this.selectedPaths.size === 1) {
            // Single selection - show detailed properties
            const element = selectedElements[0];
            propertiesContainer.innerHTML = this.generateSingleElementProperties(element);
        } else {
            // Multiple selection - show summary
            propertiesContainer.innerHTML = this.generateMultipleElementProperties(selectedElements);
        }
    }
    
    generateSingleElementProperties(element) {
        const elementType = element.tagName.toLowerCase();
        const strokeColor = element.getAttribute('stroke') || element.style.stroke || '#000000';
        const fillColor = element.getAttribute('fill') || element.style.fill || 'none';
        const strokeWidth = element.getAttribute('stroke-width') || element.style.strokeWidth || '1';
        const pathLength = elementType === 'path' ? parseFloat(element.dataset.pathLength || 0) : 0;
        const isHandwriting = element.dataset.isHandwriting === 'true';
        const animationSpeed = element.dataset.animationSpeed || 'Default';
        
        return `
            <div class="property-group">
                <div class="property-label">Element Type</div>
                <div class="property-value">${elementType.toUpperCase()}</div>
            </div>
            <div class="property-group">
                <div class="property-label">Stroke Color</div>
                <div class="property-value">${strokeColor}</div>
            </div>
            <div class="property-group">
                <div class="property-label">Fill Color</div>
                <div class="property-value">${fillColor}</div>
            </div>
            <div class="property-group">
                <div class="property-label">Stroke Width</div>
                <div class="property-value">${strokeWidth}</div>
            </div>
            ${elementType === 'path' ? `
            <div class="property-group">
                <div class="property-label">Path Length</div>
                <div class="property-value">${pathLength.toFixed(2)} units</div>
            </div>` : ''}
            <div class="property-group">
                <div class="property-label">Category</div>
                <div class="property-value">${isHandwriting ? 'Handwriting' : 'Structural'}</div>
            </div>
            <div class="property-group">
                <div class="property-label">Animation Speed</div>
                <div class="property-value">${animationSpeed}</div>
            </div>
            <div class="property-group">
                <div class="property-label">Element ID</div>
                <div class="property-value">${element.id || 'No ID'}</div>
            </div>
        `;
    }
    
    generateMultipleElementProperties(elements) {
        const pathCount = elements.filter(el => el.tagName.toLowerCase() === 'path').length;
        const circleCount = elements.filter(el => el.tagName.toLowerCase() === 'circle').length;
        const handwritingCount = elements.filter(el => el.dataset.isHandwriting === 'true').length;
        const structuralCount = elements.length - handwritingCount;
        
        return `
            <div class="property-group">
                <div class="property-label">Selection Summary</div>
                <div class="property-value">${elements.length} elements selected</div>
            </div>
            <div class="property-group">
                <div class="property-label">Element Types</div>
                <div class="property-value">
                    ${pathCount > 0 ? `${pathCount} path(s)` : ''}
                    ${pathCount > 0 && circleCount > 0 ? ', ' : ''}
                    ${circleCount > 0 ? `${circleCount} circle(s)` : ''}
                </div>
            </div>
            <div class="property-group">
                <div class="property-label">Categories</div>
                <div class="property-value">
                    ${handwritingCount > 0 ? `${handwritingCount} handwriting` : ''}
                    ${handwritingCount > 0 && structuralCount > 0 ? ', ' : ''}
                    ${structuralCount > 0 ? `${structuralCount} structural` : ''}
                </div>
            </div>
        `;
    }
    
    selectAllPaths() {
        this.selectedPaths.clear();
        for (let i = 0; i < this.allElements.length; i++) {
            this.selectedPaths.add(i);
        }
        this.updateSelectionUI();
        this.updatePropertiesPanel();
        this.highlightSelectedPaths();
    }
    
    selectNonePaths() {
        this.clearPathSelection();
    }
    
    invertPathSelection() {
        const newSelection = new Set();
        for (let i = 0; i < this.allElements.length; i++) {
            if (!this.selectedPaths.has(i)) {
                newSelection.add(i);
            }
        }
        this.selectedPaths = newSelection;
        this.updateSelectionUI();
        this.updatePropertiesPanel();
        this.highlightSelectedPaths();
    }
    
    clearPathSelection() {
        this.selectedPaths.clear();
        this.updateSelectionUI();
        this.updatePropertiesPanel();
        this.highlightSelectedPaths();
    }
    
    highlightSelectedPaths() {
        // Clear existing highlights
        this.allElements.forEach(element => {
            element.classList.remove('path-editor-highlight');
            element.style.filter = '';
            // Clear rule-style highlighting
            if (element.dataset.originalStroke !== undefined) {
                element.style.stroke = element.dataset.originalStroke || '';
                element.style.strokeWidth = element.dataset.originalStrokeWidth || '';
                element.style.fill = element.dataset.originalFill || '';
                element.style.opacity = element.dataset.originalOpacity || '';
                // Clean up stored original values
                delete element.dataset.originalStroke;
                delete element.dataset.originalStrokeWidth;
                delete element.dataset.originalFill;
                delete element.dataset.originalOpacity;
            }
        });
        
        // Add highlights to selected paths using same style as rules system
        this.selectedPaths.forEach(index => {
            const element = this.allElements[index];
            element.classList.add('path-editor-highlight');
            
            // Store original styles for restoration
            element.dataset.originalStroke = element.style.stroke || element.getAttribute('stroke') || '';
            element.dataset.originalStrokeWidth = element.style.strokeWidth || element.getAttribute('stroke-width') || '';
            element.dataset.originalFill = element.style.fill || element.getAttribute('fill') || '';
            element.dataset.originalOpacity = element.style.opacity || '';
            
            // Apply red highlight like rules system
            element.style.stroke = '#ff0000';
            element.style.strokeWidth = '3';
            element.style.fill = 'rgba(255, 0, 0, 0.2)';
            element.style.opacity = '1';
        });
    }
    
    // Drag and Drop functionality
    handleDragStart(e, index) {
        this.draggedIndex = index;
        e.dataTransfer.effectAllowed = 'move';
        
        // Initialize auto-scroll variables
        this.autoScrollTimer = null;
        this.autoScrollSpeed = 2; // Slower scroll speed (pixels per frame)
        
        // If dragged item is not selected, select it
        if (!this.selectedPaths.has(index)) {
            this.selectedPaths.clear();
            this.selectedPaths.add(index);
            this.updateSelectionUI();
        }
        
        // Add visual feedback to all selected items being dragged
        const pathItems = document.querySelectorAll('.path-item');
        this.selectedPaths.forEach(selectedIndex => {
            if (pathItems[selectedIndex]) {
                pathItems[selectedIndex].classList.add('dragging');
            }
        });
        
        // Set drag data to indicate number of items being moved
        const selectedCount = this.selectedPaths.size;
        e.dataTransfer.setData('text/plain', selectedCount > 1 ? 
            `Moving ${selectedCount} paths` : 'Moving 1 path');
    }
    
    handleDragOver(e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        
        // Add visual feedback
        const pathItems = document.querySelectorAll('.path-item');
        pathItems.forEach(item => item.classList.remove('drop-target'));
        e.target.closest('.path-item')?.classList.add('drop-target');
        
        // Handle auto-scrolling with controlled speed
        this.handleAutoScroll(e);
    }
    
    handleDrop(e, targetIndex) {
        e.preventDefault();
        
        if (this.draggedIndex !== undefined && this.draggedIndex !== targetIndex) {
            this.moveSelectedPathsToPosition(targetIndex);
        }
        
        // Clean up visual feedback
        const pathItems = document.querySelectorAll('.path-item');
        pathItems.forEach(item => {
            item.classList.remove('drop-target');
            item.classList.remove('dragging');
        });
    }
    
    handleDragEnd(e) {
        this.draggedIndex = undefined;
        
        // Clean up auto-scroll timer
        if (this.autoScrollTimer) {
            clearInterval(this.autoScrollTimer);
            this.autoScrollTimer = null;
        }
        
        // Clean up visual feedback from all items
        const pathItems = document.querySelectorAll('.path-item');
        pathItems.forEach(item => {
            item.classList.remove('drop-target');
            item.classList.remove('dragging');
        });
    }
    
    handleAutoScroll(e) {
        const container = document.getElementById('path-list');
        const rect = container.getBoundingClientRect();
        const scrollZone = 50; // pixels from edge where scrolling starts
        const y = e.clientY - rect.top;
        
        let scrollDirection = 0;
        
        // Check if cursor is in scroll zones
        if (y < scrollZone) {
            scrollDirection = -1; // scroll up
        } else if (y > rect.height - scrollZone) {
            scrollDirection = 1; // scroll down
        }
        
        // Clear existing timer
        if (this.autoScrollTimer) {
            clearInterval(this.autoScrollTimer);
            this.autoScrollTimer = null;
        }
        
        // Start new auto-scroll if needed
        if (scrollDirection !== 0) {
            this.autoScrollTimer = setInterval(() => {
                container.scrollTop += scrollDirection * this.autoScrollSpeed;
            }, 16); // ~60fps
        }
    }
    
    // Path reordering functionality
    moveSelectedPaths(direction) {
        if (this.selectedPaths.size === 0) return;
        
        const selectedIndices = Array.from(this.selectedPaths).sort((a, b) => a - b);
        
        switch (direction) {
            case 'up':
                this.movePathsUp(selectedIndices);
                break;
            case 'down':
                this.movePathsDown(selectedIndices);
                break;
            case 'top':
                this.movePathsToTop(selectedIndices);
                break;
            case 'bottom':
                this.movePathsToBottom(selectedIndices);
                break;
        }
    }
    
    movePathsUp(indices) {
        // Move each selected path up by one position, starting from the first
        for (let i = 0; i < indices.length; i++) {
            const currentIndex = indices[i];
            if (currentIndex > 0 && !indices.includes(currentIndex - 1)) {
                this.swapElements(currentIndex, currentIndex - 1);
                // Update indices for subsequent moves
                for (let j = i; j < indices.length; j++) {
                    indices[j]--;
                }
                // Update selection
                this.selectedPaths.delete(currentIndex);
                this.selectedPaths.add(currentIndex - 1);
            }
        }
        this.refreshPathList();
        this.updateAnimationSystem();
    }
    
    movePathsDown(indices) {
        // Move each selected path down by one position, starting from the last
        for (let i = indices.length - 1; i >= 0; i--) {
            const currentIndex = indices[i];
            if (currentIndex < this.allElements.length - 1 && !indices.includes(currentIndex + 1)) {
                this.swapElements(currentIndex, currentIndex + 1);
                // Update indices for subsequent moves
                for (let j = i; j < indices.length; j++) {
                    indices[j]++;
                }
                // Update selection
                this.selectedPaths.delete(currentIndex);
                this.selectedPaths.add(currentIndex + 1);
            }
        }
        this.refreshPathList();
        this.updateAnimationSystem();
    }
    
    movePathsToTop(indices) {
        // Move all selected paths to the top, maintaining their relative order
        const selectedElements = indices.map(index => this.allElements[index]);
        
        // Remove selected elements from their current positions
        for (let i = indices.length - 1; i >= 0; i--) {
            this.allElements.splice(indices[i], 1);
        }
        
        // Insert them at the beginning
        this.allElements.unshift(...selectedElements);
        
        // Update selection indices
        this.selectedPaths.clear();
        for (let i = 0; i < selectedElements.length; i++) {
            this.selectedPaths.add(i);
        }
        
        this.refreshPathList();
        this.updateAnimationSystem();
    }
    
    movePathsToBottom(indices) {
        // Move all selected paths to the bottom, maintaining their relative order
        const selectedElements = indices.map(index => this.allElements[index]);
        
        // Remove selected elements from their current positions
        for (let i = indices.length - 1; i >= 0; i--) {
            this.allElements.splice(indices[i], 1);
        }
        
        // Insert them at the end
        this.allElements.push(...selectedElements);
        
        // Update selection indices
        this.selectedPaths.clear();
        const startIndex = this.allElements.length - selectedElements.length;
        for (let i = 0; i < selectedElements.length; i++) {
            this.selectedPaths.add(startIndex + i);
        }
        
        this.refreshPathList();
        this.updateAnimationSystem();
    }
    
    moveSelectedPathsToPosition(targetIndex) {
        if (this.selectedPaths.size === 0) return;
        
        const selectedIndices = Array.from(this.selectedPaths).sort((a, b) => a - b);
        const selectedElements = selectedIndices.map(index => this.allElements[index]);
        
        // Remove selected elements from their current positions
        for (let i = selectedIndices.length - 1; i >= 0; i--) {
            this.allElements.splice(selectedIndices[i], 1);
            // Adjust target index if we removed elements before it
            if (selectedIndices[i] < targetIndex) {
                targetIndex--;
            }
        }
        
        // Insert them at the target position
        this.allElements.splice(targetIndex, 0, ...selectedElements);
        
        // Update selection indices
        this.selectedPaths.clear();
        for (let i = 0; i < selectedElements.length; i++) {
            this.selectedPaths.add(targetIndex + i);
        }
        
        this.refreshPathList();
        this.updateAnimationSystem();
    }
    
    deleteSelectedPaths() {
        if (this.selectedPaths.size === 0) return;
        
        // Confirm deletion
        const count = this.selectedPaths.size;
        const message = count === 1 ? 
            'Are you sure you want to delete this path?' : 
            `Are you sure you want to delete these ${count} paths?`;
        
        if (!confirm(message)) return;
        
        const selectedIndices = Array.from(this.selectedPaths).sort((a, b) => b - a); // Sort in reverse order
        const deletedElements = [];
        
        // Remove selected elements from DOM and arrays
        for (const index of selectedIndices) {
            const element = this.allElements[index];
            if (element) {
                // Remove from DOM
                if (element.parentNode) {
                    element.parentNode.removeChild(element);
                }
                
                // Store for potential undo (could be added later)
                deletedElements.push({
                    element: element,
                    originalIndex: index
                });
                
                // Remove from allElements array
                this.allElements.splice(index, 1);
            }
        }
        
        // Clear selection
        this.selectedPaths.clear();
        
        // Update the display and animation system
        this.refreshPathList();
        this.updateAnimationSystem();
        this.updateSelectionUI();
        
        console.log(`Deleted ${deletedElements.length} path(s)`);
    }

    // Insert pause comment after selected paths
    insertPauseAfterSelection() {
        console.log('insertPauseAfterSelection called');
        console.log('Selected paths:', this.selectedPaths);
        console.log('Selected paths size:', this.selectedPaths.size);
        console.log('pathEditorVisible:', this.pathEditorVisible);
        
        if (this.selectedPaths.size === 0) {
            console.log('No paths selected - cannot insert pause');
            return;
        }
        
        // Get the highest selected index for insertion point
        const selectedIndices = Array.from(this.selectedPaths).sort((a, b) => b - a);
        this.pendingPauseInsertionIndex = selectedIndices[0];
        console.log('Selected indices:', selectedIndices);
        console.log('Will insert after index:', this.pendingPauseInsertionIndex);
        
        // Insert with default duration that can be edited inline
        this.executePauseInsertion(1000); // Default 1 second pause
    }

    // Execute the actual pause insertion with the given duration
    executePauseInsertion(duration) {
        console.log('Executing pause insertion with duration:', duration);
        
        if (this.pendingPauseInsertionIndex === undefined) {
            console.error('No pending pause insertion index');
            return;
        }
        
        // Insert pause comment into SVG
        this.insertPauseComment(this.pendingPauseInsertionIndex, duration);
        
        // Refresh display
        this.refreshPathList();
        this.updateAnimationSystem();
        
        console.log(`Inserted ${duration}ms pause after path ${this.pendingPauseInsertionIndex + 1}`);
        
        // Clear pending insertion
        this.pendingPauseInsertionIndex = undefined;
    }

    insertSpeedHintAfterSelection() {
        console.log('insertSpeedHintAfterSelection called');
        console.log('Selected paths:', this.selectedPaths);
        console.log('Selected paths size:', this.selectedPaths.size);
        console.log('pathEditorVisible:', this.pathEditorVisible);
        
        if (this.selectedPaths.size === 0) {
            console.log('No paths selected - cannot insert speed hint');
            return;
        }
        
        // Get the highest selected index for insertion point
        const selectedIndices = Array.from(this.selectedPaths).sort((a, b) => b - a);
        this.pendingSpeedHintInsertionIndex = selectedIndices[0];
        console.log('Selected indices:', selectedIndices);
        console.log('Will insert after index:', this.pendingSpeedHintInsertionIndex);
        
        // Insert with default multiplier that can be edited inline
        this.executeSpeedHintInsertion(2.0); // Default 2x speed
    }

    // Execute the actual speed hint insertion with the given multiplier
    executeSpeedHintInsertion(multiplier) {
        console.log('Executing speed hint insertion with multiplier:', multiplier);
        
        if (this.pendingSpeedHintInsertionIndex === undefined) {
            console.error('No pending speed hint insertion index');
            return;
        }
        
        // Insert speed hint comment into SVG
        this.insertSpeedHint(this.pendingSpeedHintInsertionIndex, multiplier);
        
        // Refresh display
        this.refreshPathList();
        this.updateAnimationSystem();
        
        console.log(`Inserted ${multiplier}x speed hint after path ${this.pendingSpeedHintInsertionIndex + 1}`);
        
        // Clear pending insertion
        this.pendingSpeedHintInsertionIndex = undefined;
    }

    // Insert pause comment into the SVG DOM
    insertPauseComment(afterIndex, duration) {
        console.log('insertPauseComment called with afterIndex:', afterIndex, 'duration:', duration);
        
        const svgElement = document.getElementById('animated-svg');
        if (!svgElement) {
            console.log('No SVG element found');
            return;
        }
        
        console.log('SVG element found, allElements length:', this.allElements.length);
        
        // Create pause comment
        const pauseComment = document.createComment(`PAUSE:${duration}`);
        console.log('Created pause comment:', pauseComment);
        
        // Find the element to insert after
        if (afterIndex >= 0 && afterIndex < this.allElements.length) {
            const targetElement = this.allElements[afterIndex];
            console.log('Target element:', targetElement);
            console.log('Target element parent:', targetElement.parentNode);
            
            // Find the correct parent to insert into (could be SVG or a group)
            let insertParent = targetElement.parentNode;
            
            // If the target element is inside a group, we need to insert after the group
            // or find the appropriate location within the group
            if (insertParent !== svgElement) {
                // Element is inside a group, insert the comment in the same group
                console.log('Element is inside a group, inserting in group');
                if (targetElement.nextSibling) {
                    insertParent.insertBefore(pauseComment, targetElement.nextSibling);
                    console.log('Inserted pause comment before next sibling in group');
                } else {
                    insertParent.appendChild(pauseComment);
                    console.log('Appended pause comment to group');
                }
            } else {
                // Element is a direct child of SVG
                console.log('Element is direct child of SVG');
                if (targetElement.nextSibling) {
                    svgElement.insertBefore(pauseComment, targetElement.nextSibling);
                    console.log('Inserted pause comment before next sibling in SVG');
                } else {
                    svgElement.appendChild(pauseComment);
                    console.log('Appended pause comment to SVG');
                }
            }
        } else {
            // Insert at the end if no valid index
            svgElement.appendChild(pauseComment);
            console.log('Appended pause comment to end of SVG');
        }
    }

    // Remove pause comment from SVG
    removePauseComment(afterIndex) {
        const svgElement = document.getElementById('animated-svg');
        if (!svgElement || afterIndex < 0 || afterIndex >= this.allElements.length) {
            return;
        }
        
        const currentElement = this.allElements[afterIndex];
        let nextNode = currentElement.nextSibling;
        
        // Look for the pause comment to remove
        while (nextNode) {
            if (nextNode.nodeType === Node.COMMENT_NODE) {
                const commentText = nextNode.textContent.trim();
                
                // Check if this is a pause comment (format: PAUSE:duration)
                if (commentText.match(/^PAUSE:\d+$/)) {
                    const nodeToRemove = nextNode;
                    const parentNode = nodeToRemove.parentNode; // Get the actual parent
                    nextNode = nextNode.nextSibling; // Move to next before removing
                    parentNode.removeChild(nodeToRemove); // Remove from actual parent
                    console.log(`Removed pause comment after element ${afterIndex + 1}`);
                    
                    // Refresh the display
                    this.refreshPathList();
                    return;
                }
            } else if (nextNode.nodeType === Node.ELEMENT_NODE) {
                // Stop looking when we hit the next path/circle element
                break;
            }
            nextNode = nextNode.nextSibling;
        }
    }

    updatePauseDuration(afterIndex, newDuration) {
        const svgElement = document.getElementById('animated-svg');
        if (!svgElement || afterIndex < 0 || afterIndex >= this.allElements.length) {
            return;
        }
        
        const currentElement = this.allElements[afterIndex];
        let nextNode = currentElement.nextSibling;
        
        // Look for the pause comment to update
        while (nextNode) {
            if (nextNode.nodeType === Node.COMMENT_NODE) {
                const commentText = nextNode.textContent.trim();
                
                // Check if this is a pause comment (format: PAUSE:duration)
                if (commentText.match(/^PAUSE:\d+$/)) {
                    // Update the comment with new duration
                    nextNode.textContent = ` PAUSE:${newDuration} `;
                    console.log(`Updated pause duration to ${newDuration}ms after element ${afterIndex + 1}`);
                    
                    // Update the animation system
                    this.updateAnimationSystem();
                    return;
                }
            } else if (nextNode.nodeType === Node.ELEMENT_NODE) {
                // Stop looking when we hit the next path/circle element
                break;
            }
            nextNode = nextNode.nextSibling;
        }
    }

    insertSpeedHint(afterIndex, multiplier) {
        console.log('insertSpeedHint called with afterIndex:', afterIndex, 'multiplier:', multiplier);
        
        const svgElement = document.getElementById('animated-svg');
        if (!svgElement) {
            console.log('No SVG element found');
            return;
        }
        
        console.log('SVG element found, allElements length:', this.allElements.length);
        
        // Create speed hint comment
        const speedComment = document.createComment(` SPEED:${multiplier} `);
        console.log('Created speed hint comment:', speedComment);
        
        // Find the element to insert after
        if (afterIndex >= 0 && afterIndex < this.allElements.length) {
            const targetElement = this.allElements[afterIndex];
            console.log('Target element:', targetElement);
            console.log('Target element parent:', targetElement.parentNode);
            
            // Find the correct parent to insert into (could be SVG or a group)
            let insertParent = targetElement.parentNode;
            
            // If the target element is inside a group, we need to insert after the group
            // or find the appropriate location within the group
            if (insertParent !== svgElement) {
                // Element is inside a group, insert the comment in the same group
                console.log('Element is inside a group, inserting in group');
                if (targetElement.nextSibling) {
                    insertParent.insertBefore(speedComment, targetElement.nextSibling);
                    console.log('Inserted speed hint comment before next sibling in group');
                } else {
                    insertParent.appendChild(speedComment);
                    console.log('Appended speed hint comment to group');
                }
            } else {
                // Element is a direct child of SVG
                console.log('Element is direct child of SVG');
                if (targetElement.nextSibling) {
                    svgElement.insertBefore(speedComment, targetElement.nextSibling);
                    console.log('Inserted speed hint comment before next sibling in SVG');
                } else {
                    svgElement.appendChild(speedComment);
                    console.log('Appended speed hint comment to SVG');
                }
            }
        } else {
            // Insert at the end if no valid index
            svgElement.appendChild(speedComment);
            console.log('Appended speed hint comment to end of SVG');
        }
        
        console.log(`Inserted speed hint (${multiplier}x) after element ${afterIndex + 1}`);
    }

    updateSpeedHint(afterIndex, newMultiplier) {
        const svgElement = document.getElementById('animated-svg');
        if (!svgElement || afterIndex < 0 || afterIndex >= this.allElements.length) {
            return;
        }
        
        const currentElement = this.allElements[afterIndex];
        let nextNode = currentElement.nextSibling;
        
        // Look for the speed hint comment to update
        while (nextNode) {
            if (nextNode.nodeType === Node.COMMENT_NODE) {
                const commentText = nextNode.textContent.trim();
                
                // Check if this is a speed hint comment (format: SPEED:multiplier)
                if (commentText.match(/^SPEED:[\d.]+$/)) {
                    // Update the comment with new multiplier
                    nextNode.textContent = ` SPEED:${newMultiplier} `;
                    console.log(`Updated speed hint to ${newMultiplier}x after element ${afterIndex + 1}`);
                    
                    // Update the animation system
                    this.updateAnimationSystem();
                    return;
                }
            } else if (nextNode.nodeType === Node.ELEMENT_NODE) {
                // Stop looking when we hit the next path/circle element
                break;
            }
            nextNode = nextNode.nextSibling;
        }
    }

    removeSpeedHint(afterIndex) {
        const svgElement = document.getElementById('animated-svg');
        if (!svgElement || afterIndex < 0 || afterIndex >= this.allElements.length) {
            return;
        }
        
        const currentElement = this.allElements[afterIndex];
        let nextNode = currentElement.nextSibling;
        
        // Look for the speed hint comment to remove
        while (nextNode) {
            if (nextNode.nodeType === Node.COMMENT_NODE) {
                const commentText = nextNode.textContent.trim();
                
                // Check if this is a speed hint comment (format: SPEED:multiplier)
                if (commentText.match(/^SPEED:[\d.]+$/)) {
                    const nodeToRemove = nextNode;
                    const parentNode = nodeToRemove.parentNode; // Get the actual parent
                    nextNode = nextNode.nextSibling; // Move to next before removing
                    parentNode.removeChild(nodeToRemove); // Remove from actual parent
                    console.log(`Removed speed hint after element ${afterIndex + 1}`);
                    
                    // Refresh the display
                    this.refreshPathList();
                    return;
                }
            } else if (nextNode.nodeType === Node.ELEMENT_NODE) {
                // Stop looking when we hit the next path/circle element
                break;
            }
            nextNode = nextNode.nextSibling;
        }
    }

    swapElements(index1, index2) {
        const temp = this.allElements[index1];
        this.allElements[index1] = this.allElements[index2];
        this.allElements[index2] = temp;
    }
    
    updateAnimationSystem() {
        // Update the sortedPaths array used by the animation system
        this.sortedPaths = [...this.allElements];
        
        // Re-categorize paths based on the new order
        this.recategorizePaths();
        
        // If PathEditor is visible, update the display
        if (this.pathEditorVisible) {
            this.highlightSelectedPaths();
        }
    }
    
    // SVG Export functionality
    exportSVG() {
        try {
            const svgElement = document.getElementById('animated-svg');
            if (!svgElement) {
                alert('No SVG loaded to export');
                return;
            }
            
            // Clone the SVG to avoid modifying the original
            const clonedSVG = svgElement.cloneNode(true);
            
            // Clear any animation-related styles and classes
            const allElements = clonedSVG.querySelectorAll('path, circle');
            allElements.forEach(element => {
                element.style.strokeDasharray = '';
                element.style.strokeDashoffset = '';
                element.style.transition = '';
                element.style.filter = '';
                element.classList.remove('path-editor-highlight');
                
                // Remove animation-related data attributes
                delete element.dataset.pathLength;
                delete element.dataset.elementType;
                delete element.dataset.isHandwriting;
                delete element.dataset.animationSpeed;
                delete element.dataset.pathIndex;
                delete element.dataset.strokeColor;
            });
            
            // Create new SVG with reordered elements
            const newSVG = clonedSVG.cloneNode(false); // Clone without children
            
            // Copy attributes
            Array.from(clonedSVG.attributes).forEach(attr => {
                newSVG.setAttribute(attr.name, attr.value);
            });
            
            // Add elements in the new order
            this.allElements.forEach(originalElement => {
                // Find corresponding element in cloned SVG
                const clonedElement = Array.from(allElements).find(el => {
                    return el.tagName === originalElement.tagName &&
                           el.getAttribute('d') === originalElement.getAttribute('d') &&
                           el.getAttribute('cx') === originalElement.getAttribute('cx') &&
                           el.getAttribute('cy') === originalElement.getAttribute('cy');
                });
                
                if (clonedElement) {
                    newSVG.appendChild(clonedElement);
                }
            });
            
            // Create downloadable file
            const svgData = new XMLSerializer().serializeToString(newSVG);
            const blob = new Blob([svgData], { type: 'image/svg+xml' });
            const url = URL.createObjectURL(blob);
            
            // Create download link
            const link = document.createElement('a');
            link.href = url;
            link.download = `${this.currentSVGFilename.replace('.svg', '')}_reordered.svg`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            
            // Clean up
            URL.revokeObjectURL(url);
            
            console.log('SVG exported with reordered elements');
        } catch (error) {
            console.error('Error exporting SVG:', error);
            alert('Error exporting SVG. Please check the console for details.');
        }
    }
}

// Initialize the animator when the page loads
document.addEventListener('DOMContentLoaded', () => {
    window.svgAnimator = new SVGAnimator();
});