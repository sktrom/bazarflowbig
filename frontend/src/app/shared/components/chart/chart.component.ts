import { Component, Input, OnInit, ElementRef, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, ChartConfiguration, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="relative w-full h-full min-h-[300px]">
      <canvas #chartCanvas></canvas>
    </div>
  `
})
export class ChartComponent implements AfterViewInit, OnDestroy {
  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;
  
  @Input() config!: ChartConfiguration;
  
  private chartInstance: Chart | null = null;

  ngAfterViewInit(): void {
    if (this.config) {
      this.initChart();
    }
  }

  ngOnChanges(): void {
    if (this.chartInstance && this.config) {
      this.chartInstance.data = this.config.data;
      this.chartInstance.options = this.config.options;
      this.chartInstance.update();
    }
  }

  private initChart(): void {
    const ctx = this.chartCanvas.nativeElement.getContext('2d');
    if (ctx) {
      this.chartInstance = new Chart(ctx, this.config);
    }
  }

  ngOnDestroy(): void {
    if (this.chartInstance) {
      this.chartInstance.destroy();
    }
  }
}
