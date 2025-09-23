   
// canvasAnimationInterop.js
// Animate SVG paths by stroking along their length using stroke-dasharray and stroke-dashoffset
// Record the animations to webm videos and stream file for user to download

window.canvasAnimationInterop = {
    animatePaths: function(svgSelector, duration = 1200, callback) {
        console.log('[canvasAnimationInterop] animatePaths called', { svgSelector, duration });
        const svg = document.querySelector(svgSelector);
        if (!svg) return;

        // Recursively gather all animatable elements and comments in order
        function getAnimatableElements(node) {
            const elements = [];
            function traverse(n) {
                if (n.nodeType === Node.ELEMENT_NODE && n.hasAttribute('data-element-type')) {
                    elements.push(n);
                } else if (n.nodeType === Node.COMMENT_NODE) {
                    const comment = n.nodeValue.trim();
                    if (comment.startsWith('Pause:')) {
                        elements.push({ type: 'pause', ms: parseInt(comment.substring(6)) });
                    } else if (comment.startsWith('Speed:')) {
                        elements.push({ type: 'speed', mult: parseFloat(comment.substring(6)) });
                    }
                }
                if (n.childNodes) {
                    n.childNodes.forEach(traverse);
                }
            }
            traverse(node);
            return elements;
        }
        const elements = getAnimatableElements(svg);

        // Reset visual styling of all elements to hidden state
        elements.forEach((el, index) => {
            if (!el || !el.style) return; // skip non-DOM elements (Pause/Speed)
            el.style.transition = 'none';
            el.style.filter = '';
            el.style.webkitFilter = '';
            if (el.dataset.elementType === 'path') {
                let pathLength = el.dataset.pathLength ? parseFloat(el.dataset.pathLength) : 0;
                try {
                    pathLength = el.getTotalLength();
                } catch {}
                el.style.strokeDasharray = pathLength;
                el.style.strokeDashoffset = pathLength;
                el.style.display = 'none';
            } else if (el.dataset.elementType === 'circle') {
                el.style.display = 'none';
            }
        });

        let idx = 0;
        let speedMultiplier = 1.0;
        function animateNext() {
            if (idx >= elements.length) {
                // Show all at the end
                elements.forEach(el => {
                    if (el.type === 'pause' || el.type === 'speed') return;
                    if (el.dataset.elementType === 'path') {
                        el.style.strokeDasharray = '';
                        el.style.strokeDashoffset = '';
                        el.style.transition = '';
                        el.style.display = '';
                    } else if (el.dataset.elementType === 'circle') {
                        el.style.display = '';
                    }
                });
                if (callback) callback();
                return;
            }

            const el = elements[idx];
            if (el.type === 'pause') {
                setTimeout(() => {
                    idx++;
                    animateNext();
                }, el.ms);
                return;
            }
            if (el.type === 'speed') {
                // Clamp multiplier to minimum 0.01 and divide
                if (typeof el.mult === 'number' && el.mult > 0.01) {
                    speedMultiplier /= el.mult;
                }
                idx++;
                animateNext();
                return;
            }
            el.style.display = '';
            let animationSpeed = duration;
            if (el.dataset.elementType === 'path') {
                let pathLength = el.dataset.pathLength ? parseFloat(el.dataset.pathLength) : 0;
                try {
                    pathLength = el.getTotalLength();
                } catch {
                    console.warn('[canvasAnimationInterop] Error getting path length', el);
                }
                // Scale speed: longer paths animate 50% slower
                const baseLength = 200; // reference length for scaling
                const lengthMultiplier = 1 + Math.max(0, (pathLength - baseLength) / baseLength) * 0.5;
                animationSpeed = Math.round(duration * lengthMultiplier * speedMultiplier);
                // Use per-element speed if present
                if (el.dataset.animationSpeed) {
                    const speed = parseInt(el.dataset.animationSpeed);
                    if (!isNaN(speed) && speed > 0) animationSpeed = speed;
                }
                el.style.strokeDasharray = pathLength;
                el.style.strokeDashoffset = pathLength;
                el.style.transition = `stroke-dashoffset ${animationSpeed}ms linear`;
                setTimeout(() => {
                    el.style.strokeDashoffset = 0;
                    // Wait for the transition to finish before starting the next animation
                    el.addEventListener('transitionend', function handler() {
                        el.removeEventListener('transitionend', handler);
                        el.style.strokeDasharray = '';
                        el.style.strokeDashoffset = '';
                        el.style.transition = '';
                        idx++;
                        // Do not reset speedMultiplier; persist until next SpeedHint
                        animateNext();
                    });
                }, 50);
            } else if (el.dataset.elementType === 'circle') {
                el.style.display = '';
                idx++;
                setTimeout(() => {
                    // Do not reset speedMultiplier; persist until next SpeedHint
                    animateNext();
                }, Math.round(animationSpeed * speedMultiplier));
            }
        }
        animateNext();
    },
    /**
     * Record a webm video of the animated diagram using MediaRecorder.
     * @param {string} svgSelector - CSS selector for the SVG element
     * @param {number} fps - Frames per second
     * @param {number} width - Output width
     * @param {number} height - Output height
     * @param {number} quality - Video quality (bitrate)
     * @param {number} endPauseMs - Pause at end in ms
     * @param {boolean} insertInitialThumbnail - Whether to insert a short thumbnail of the completed diagram at the start
     * @param {number} preAnimationBlankMs - Duration of blank frames before animation begins in ms
     */
    recordWebm: async function(svgSelector, fps, width, height, quality, endPauseMs, insertInitialThumbnail, preAnimationBlankMs, animationSpeed, diagramName) {
        const svg = document.querySelector(svgSelector);
        if (!svg) {
            alert('SVG element not found for recording');
            return;
        }
        // Create a canvas for rendering
        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        const ctx = canvas.getContext('2d');
        // Prepare video stream
        const stream = canvas.captureStream(fps);
        const chunks = [];
        const recorder = new MediaRecorder(stream, {
            mimeType: 'video/webm;codecs=vp9',
            videoBitsPerSecond: Math.max(quality || 5000000, 5000000) // 5Mbps minimum for high quality
        });
        recorder.ondataavailable = e => { if (e.data.size > 0) chunks.push(e.data); };
        recorder.onstop = () => {
            const blob = new Blob(chunks, { type: 'video/webm' });
            const url = URL.createObjectURL(blob);
            // Download the video with diagram name and sortable timestamp
            const timestamp = new Date().toISOString().replace(/[-:T]/g, '').slice(0, 15);
            const safeName = (diagramName && diagramName.trim()) ? diagramName.trim().replace(/[^a-zA-Z0-9-_]/g, '_') : 'diagram';
            const filename = `${safeName}-${timestamp}.webm`;
            const a = document.createElement('a');
            a.href = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            setTimeout(() => { document.body.removeChild(a); URL.revokeObjectURL(url); }, 1000);
        };
        recorder.start();


        // 1. Show final rendered image for first ~50ms worth of frames if thumbnailStart is ticked
        if (insertInitialThumbnail) {
            const img = new Image();
            const svgData = new XMLSerializer().serializeToString(svg);
            img.src = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgData)));
            await new Promise(resolve => { img.onload = resolve; });
            const frameCount = Math.max(1, Math.round(fps * 0.05)); // 50ms worth of frames
            for (let i = 0; i < frameCount; i++) {
                ctx.clearRect(0, 0, width, height);
                ctx.fillStyle = '#fff';
                ctx.fillRect(0, 0, width, height);
                ctx.drawImage(img, 0, 0, width, height);
                await new Promise(r => setTimeout(r, 1000 / fps));
            }
        }

        // 2. Preblank ms of white frames
        if (preAnimationBlankMs > 0) {
            const frameCount = Math.max(1, Math.round(fps * (preAnimationBlankMs / 1000)));
            for (let i = 0; i < frameCount; i++) {
                ctx.clearRect(0, 0, width, height);
                ctx.fillStyle = '#fff';
                ctx.fillRect(0, 0, width, height);
                await new Promise(r => setTimeout(r, 1000 / fps));
            }
        }

        // 3. Animate and record frames
        await window.canvasAnimationInterop.animateAndRecord(svg, ctx, width, height, fps, animationSpeed);

        // Pause at end
        if (endPauseMs > 0) {
            await new Promise(r => setTimeout(r, endPauseMs));
        }
        recorder.stop();
    },

    /**
     * Animate SVG and record frames to canvas
     */
    animateAndRecord: async function(svg, ctx, width, height, fps, animationSpeed) {
        // Animate SVG and record each frame to canvas
        // We'll use the same logic as animatePaths, but add canvas drawing at each frame
        const speed = animationSpeed > 0 ? animationSpeed : 5.0;
        const duration = 1200 / speed;
        const svgSelector = '#' + (svg.id || svg.getAttribute('id') || 'svg-canvas');
        const elements = (function getAnimatableElements(node) {
            const elements = [];
            function traverse(n) {
                if (n.nodeType === Node.ELEMENT_NODE && n.hasAttribute('data-element-type')) {
                    elements.push(n);
                } else if (n.nodeType === Node.COMMENT_NODE) {
                    const comment = n.nodeValue.trim();
                    if (comment.startsWith('Pause:')) {
                        elements.push({ type: 'pause', ms: parseInt(comment.substring(6)) });
                    } else if (comment.startsWith('Speed:')) {
                        elements.push({ type: 'speed', mult: parseFloat(comment.substring(6)) });
                    }
                }
                if (n.childNodes) {
                    n.childNodes.forEach(traverse);
                }
            }
            traverse(node);
            return elements;
        })(svg);

        // Reset visual styling of all elements to hidden state
        elements.forEach((el, index) => {
            if (!el || !el.style) return; // skip non-DOM elements (Pause/Speed)
            el.style.transition = 'none';
            el.style.filter = '';
            el.style.webkitFilter = '';
            if (el.dataset && el.dataset.elementType === 'path') {
                let pathLength = el.dataset.pathLength ? parseFloat(el.dataset.pathLength) : 0;
                try { pathLength = el.getTotalLength(); } catch {}
                el.style.strokeDasharray = pathLength;
                el.style.strokeDashoffset = pathLength;
                el.style.display = 'none';
            } else if (el.dataset && el.dataset.elementType === 'circle') {
                el.style.display = 'none';
            }
        });

        let idx = 0;
        let speedMultiplier = 1.0;
        function drawFrame() {
            // Draw current SVG state to canvas with white background
            const img = new Image();
            const svgData = new XMLSerializer().serializeToString(svg);
            img.src = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgData)));
            img.onload = function() {
                ctx.clearRect(0, 0, width, height);
                ctx.fillStyle = '#fff';
                ctx.fillRect(0, 0, width, height);
                ctx.drawImage(img, 0, 0, width, height);
            };
        }

        async function animateAndRecordNext() {
            if (idx >= elements.length) {
                // Show all at the end
                elements.forEach(el => {
                    if (el.type === 'pause' || el.type === 'speed') return;
                    if (el.dataset && el.dataset.elementType === 'path') {
                        el.style.strokeDasharray = '';
                        el.style.strokeDashoffset = '';
                        el.style.transition = '';
                        el.style.display = '';
                    } else if (el.dataset && el.dataset.elementType === 'circle') {
                        el.style.display = '';
                    }
                });
                drawFrame();
                return;
            }
            const el = elements[idx];
            if (el.type === 'pause') {
                drawFrame();
                await new Promise(r => setTimeout(r, el.ms));
                idx++;
                await animateAndRecordNext();
                return;
            }
            if (el.type === 'speed') {
                if (typeof el.mult === 'number' && el.mult > 0.01) {
                    speedMultiplier /= el.mult;
                }
                idx++;
                await animateAndRecordNext();
                return;
            }
            el.style.display = '';
            let animationSpeed = duration;
            if (el.dataset && el.dataset.elementType === 'path') {
                let pathLength = el.dataset.pathLength ? parseFloat(el.dataset.pathLength) : 0;
                try { pathLength = el.getTotalLength(); } catch {}
                const baseLength = 200;
                const lengthMultiplier = 1 + Math.max(0, (pathLength - baseLength) / baseLength) * 0.5;
                animationSpeed = Math.round(duration * lengthMultiplier * speedMultiplier);
                if (el.dataset.animationSpeed) {
                    const speed = parseInt(el.dataset.animationSpeed);
                    if (!isNaN(speed) && speed > 0) animationSpeed = speed;
                }
                el.style.strokeDasharray = pathLength;
                el.style.strokeDashoffset = pathLength;
                el.style.transition = '';
                // Animate dashoffset frame-by-frame
                let start = null;
                await new Promise(resolve => {
                    function step(ts) {
                        if (!start) start = ts;
                        const elapsed = ts - start;
                        const progress = Math.min(elapsed / animationSpeed, 1);
                        el.style.strokeDashoffset = pathLength * (1 - progress);
                        drawFrame();
                        // Match frame rate to requested fps
                        if (progress < 1) {
                            setTimeout(() => requestAnimationFrame(step), 1000 / fps);
                        } else {
                            el.style.strokeDashoffset = 0;
                            el.style.strokeDasharray = '';
                            el.style.transition = '';
                            idx++;
                            resolve();
                        }
                    }
                    requestAnimationFrame(step);
                });
                await animateAndRecordNext();
            } else if (el.dataset && el.dataset.elementType === 'circle') {
                el.style.display = '';
                drawFrame();
                idx++;
                await new Promise(r => setTimeout(r, Math.round(animationSpeed * speedMultiplier)));
                await animateAndRecordNext();
            }
        }
        await animateAndRecordNext();
    },
};
