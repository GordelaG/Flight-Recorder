/**
 * Canvas Flight Profile Chart
 * Renders elevation profile with ground roll vs airborne colors and interactive playhead
 */

class FlightChart {
  constructor(canvasElement, onSeekCallback) {
    this.canvas = canvasElement;
    this.ctx = canvasElement.getContext('2d');
    this.onSeek = onSeekCallback;

    this.records = []; // Array of { time, altitude, isOnGround }
    this.currentFrame = 0;
    this.isDragging = false;

    this.setupListeners();
    this.resize();
  }

  setupListeners() {
    window.addEventListener('resize', () => this.resize());

    const handlePointer = (e) => {
      if (!this.records || this.records.length === 0) return;
      const rect = this.canvas.getBoundingClientRect();
      const x = Math.max(0, Math.min(rect.width, e.clientX - rect.left));
      const percentage = x / rect.width;
      const targetFrame = Math.round(percentage * (this.records.length - 1));
      if (this.onSeek) {
        this.onSeek(targetFrame);
      }
    };

    this.canvas.addEventListener('mousedown', (e) => {
      this.isDragging = true;
      handlePointer(e);
    });

    window.addEventListener('mousemove', (e) => {
      if (this.isDragging) {
        handlePointer(e);
      }
    });

    window.addEventListener('mouseup', () => {
      this.isDragging = false;
    });

    // Touch support
    this.canvas.addEventListener('touchstart', (e) => {
      this.isDragging = true;
      if (e.touches.length > 0) handlePointer(e.touches[0]);
    });
    window.addEventListener('touchmove', (e) => {
      if (this.isDragging && e.touches.length > 0) handlePointer(e.touches[0]);
    });
    window.addEventListener('touchend', () => {
      this.isDragging = false;
    });
  }

  resize() {
    const rect = this.canvas.parentElement.getBoundingClientRect();
    const dpr = window.devicePixelRatio || 1;
    this.canvas.width = rect.width * dpr;
    this.canvas.height = rect.height * dpr;
    this.ctx.scale(dpr, dpr);
    this.width = rect.width;
    this.height = rect.height;
    this.render();
  }

  setData(records) {
    this.records = records || [];
    this.render();
  }

  setCurrentFrame(frame) {
    this.currentFrame = frame;
    this.render();
  }

  render() {
    if (!this.ctx || !this.width || !this.height) return;

    const ctx = this.ctx;
    const w = this.width;
    const h = this.height;

    ctx.clearRect(0, 0, w, h);

    if (!this.records || this.records.length === 0) {
      // Empty background grid
      ctx.strokeStyle = 'rgba(255, 255, 255, 0.04)';
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.moveTo(0, h / 2);
      ctx.lineTo(w, h / 2);
      ctx.stroke();
      return;
    }

    const n = this.records.length;
    let minAlt = Infinity;
    let maxAlt = -Infinity;

    for (let i = 0; i < n; i++) {
      const alt = this.records[i].altitude;
      if (alt < minAlt) minAlt = alt;
      if (alt > maxAlt) maxAlt = alt;
    }

    const altRange = Math.max(10, maxAlt - minAlt);
    const paddingY = 8;
    const chartHeight = h - paddingY * 2;

    const getY = (alt) => {
      const normalized = (alt - minAlt) / altRange;
      return h - paddingY - normalized * chartHeight;
    };

    // Draw background subtle gradient
    const bgGrad = ctx.createLinearGradient(0, 0, 0, h);
    bgGrad.addColorStop(0, 'rgba(6, 182, 212, 0.12)');
    bgGrad.addColorStop(1, 'rgba(6, 182, 212, 0.01)');

    ctx.beginPath();
    ctx.moveTo(0, h);
    for (let i = 0; i < n; i++) {
      const x = (i / (n - 1)) * w;
      const y = getY(this.records[i].altitude);
      ctx.lineTo(x, y);
    }
    ctx.lineTo(w, h);
    ctx.closePath();
    ctx.fillStyle = bgGrad;
    ctx.fill();

    // Draw trajectory lines colored by Ground (Amber) vs Airborne (Cyan)
    for (let i = 0; i < n - 1; i++) {
      const x1 = (i / (n - 1)) * w;
      const y1 = getY(this.records[i].altitude);
      const x2 = ((i + 1) / (n - 1)) * w;
      const y2 = getY(this.records[i + 1].altitude);

      const onGround = this.records[i].isOnGround || this.records[i + 1].isOnGround;

      ctx.beginPath();
      ctx.moveTo(x1, y1);
      ctx.lineTo(x2, y2);
      ctx.strokeStyle = onGround ? '#f59e0b' : '#06b6d4';
      ctx.lineWidth = 2;
      ctx.stroke();
    }

    // Draw Current Playhead
    if (n > 1) {
      const cursorX = Math.max(0, Math.min(w, (this.currentFrame / (n - 1)) * w));

      // Vertical glow line
      ctx.beginPath();
      ctx.moveTo(cursorX, 0);
      ctx.lineTo(cursorX, h);
      ctx.strokeStyle = 'rgba(239, 68, 68, 0.85)';
      ctx.lineWidth = 2;
      ctx.shadowColor = '#ef4444';
      ctx.shadowBlur = 8;
      ctx.stroke();
      ctx.shadowBlur = 0;

      // Playhead bead at current altitude
      const currentRecord = this.records[Math.min(n - 1, Math.max(0, this.currentFrame))];
      if (currentRecord) {
        const cursorY = getY(currentRecord.altitude);
        ctx.beginPath();
        ctx.arc(cursorX, cursorY, 4, 0, Math.PI * 2);
        ctx.fillStyle = '#ffffff';
        ctx.shadowColor = '#ef4444';
        ctx.shadowBlur = 6;
        ctx.fill();
        ctx.shadowBlur = 0;
      }
    }
  }
}

window.FlightChart = FlightChart;
